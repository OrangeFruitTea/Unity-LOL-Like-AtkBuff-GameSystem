# 网络同步服务技术文档

## 概述

网络同步服务是核心业务层的核心组件，负责实现基于Mirror 96.10.0框架的多人在线游戏网络同步功能。该服务采用混合同步策略，结合状态同步与事件同步的优势，实现高效、稳定的多端数据同步机制，支持连接管理、房间管理、状态同步、事件同步、延迟处理等核心功能。本设计专注于局域网联机功能，同时预留了正常联网服务的接口。

## 模块架构设计

### 1. 设计目标

- **混合同步策略**：结合状态同步与事件同步的优势，优化网络传输效率
- **延迟补偿**：实现延迟补偿机制，减少网络延迟对游戏体验的影响
- **冲突解决**：实现冲突解决机制，确保多端状态的一致性
- **性能优化**：通过数据压缩、同步频率优化等手段，提高网络性能
- **易于扩展**：支持自定义同步策略和网络事件处理
- **局域网优先**：优先支持局域网联机，同时预留联网服务接口

### 2. 架构分层

```
┌─────────────────────────────────────────────────────────────┐
│                    业务逻辑层                               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  │
│  │ 角色系统    │  │ 技能系统    │  │ 对战系统    │  │
│  └─────────────┘  └─────────────┘  └─────────────┘  │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│                    网络同步服务                           │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  │
│  │ 连接管理器   │  │ 房间管理器  │  │ 状态同步器  │  │
│  └─────────────┘  └─────────────┘  └─────────────┘  │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  │
│  │ 事件同步器  │  │ 延迟处理器  │  │ 冲突解决器  │  │
│  └─────────────┘  └─────────────┘  └─────────────┘  │
└─────────────────────────────────────────────────────────────┘
                           ↓
                    ┌─────────────┐
                    │  Mirror     │
                    │  网络框架   │
                    └─────────────┘
```

### 3. 核心组件

#### 3.1 网络管理器

```csharp
using Mirror;
using UnityEngine;
using Basement.Utils;
using System;

namespace Network.Core
{
    /// <summary>
    /// Mirror网络管理器
    /// 负责与Mirror网络的连接管理
    /// </summary>
    public class MirrorNetworkManager : NetworkManager, IInitializable
    {
        private static MirrorNetworkManager _instance;

        public static MirrorNetworkManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<MirrorNetworkManager>();
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("[MirrorNetworkManager]");
                        _instance = obj.AddComponent<MirrorNetworkManager>();
                        DontDestroyOnLoad(obj);
                    }
                }
                return _instance;
            }
        }

        public bool IsConnected => NetworkClient.isConnected;
        public bool IsServer => NetworkServer.active;
        public bool IsClient => NetworkClient.active;

        private MirrorRoomManager _roomManager;
        private MirrorStateSyncManager _stateSyncManager;
        private MirrorEventSyncManager _eventSyncManager;

        public void Initialize()
        {
            // 初始化网络设置
            networkAddress = "localhost"; // 默认为本地主机
            port = 7777;

            // 初始化子管理器
            _roomManager = MirrorRoomManager.Instance;
            _stateSyncManager = MirrorStateSyncManager.Instance;
            _eventSyncManager = MirrorEventSyncManager.Instance;

            Debug.Log("Mirror网络管理器初始化完成");
        }

        public void StartServer()
        {
            NetworkManager.singleton.StartHost();
            Debug.Log("已启动本地服务器");
        }

        public void StartClient(string address = "localhost")
        {
            networkAddress = address;
            NetworkManager.singleton.StartClient();
            Debug.Log($"正在连接到服务器: {address}");
        }

        public void StopNetwork()
        {
            if (IsServer)
            {
                NetworkManager.singleton.StopHost();
            }
            else if (IsClient)
            {
                NetworkManager.singleton.StopClient();
            }
            Debug.Log("网络已停止");
        }

        public override void OnServerConnect(NetworkConnection conn)
        {
            base.OnServerConnect(conn);
            Debug.Log($"客户端连接: {conn.connectionId}");
        }

        public override void OnServerDisconnect(NetworkConnection conn)
        {
            base.OnServerDisconnect(conn);
            Debug.Log($"客户端断开连接: {conn.connectionId}");
        }

        public override void OnClientConnect(NetworkConnection conn)
        {
            base.OnClientConnect(conn);
            Debug.Log("已连接到服务器");
            _stateSyncManager.StartSync();
        }

        public override void OnClientDisconnect(NetworkConnection conn)
        {
            base.OnClientDisconnect(conn);
            Debug.Log("与服务器断开连接");
            _stateSyncManager.StopSync();
        }

        public override void OnServerAddPlayer(NetworkConnection conn)
        {
            base.OnServerAddPlayer(conn);
            Debug.Log($"添加玩家: {conn.connectionId}");
        }
    }
}
```

#### 3.2 房间管理器

```csharp
using Mirror;
using UnityEngine;
using Basement.Utils;
using System.Collections.Generic;

namespace Network.Core
{
    /// <summary>
    /// Mirror房间管理器
    /// 负责房间的创建、加入、搜索、退出等功能
    /// </summary>
    public class MirrorRoomManager : MonoBehaviour, IInitializable
    {
        private static MirrorRoomManager _instance;

        public static MirrorRoomManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject obj = new GameObject("[MirrorRoomManager]");
                    _instance = obj.AddComponent<MirrorRoomManager>();
                    DontDestroyOnLoad(obj);
                }
                return _instance;
            }
        }

        public bool IsInRoom => NetworkClient.isConnected && NetworkServer.active;

        private Dictionary<string, object> _roomProperties = new Dictionary<string, object>();

        public void Initialize()
        {
            Debug.Log("Mirror房间管理器初始化完成");
        }

        public void CreateRoom(string roomName, int maxPlayers = 8)
        {
            // 在Mirror中，房间概念通过NetworkManager和场景管理实现
            // 启动主机模式即创建房间
            MirrorNetworkManager.Instance.StartServer();
            Debug.Log($"正在创建房间: {roomName}");
        }

        public void JoinRoom(string address)
        {
            // 加入指定地址的房间
            MirrorNetworkManager.Instance.StartClient(address);
            Debug.Log($"正在加入房间: {address}");
        }

        public void LeaveRoom()
        {
            // 离开房间
            MirrorNetworkManager.Instance.StopNetwork();
            Debug.Log("正在离开房间");
        }

        public void SetRoomProperty(string key, object value)
        {
            _roomProperties[key] = value;
        }

        public object GetRoomProperty(string key)
        {
            return _roomProperties.TryGetValue(key, out var value) ? value : null;
        }
    }
}
```

#### 3.3 状态同步管理器

```csharp
using Mirror;
using UnityEngine;
using Basement.Utils;
using System.Collections.Generic;

namespace Network.Core
{
    /// <summary>
    /// Mirror状态同步管理器
    /// 负责游戏对象状态的同步
    /// </summary>
    public class MirrorStateSyncManager : MonoBehaviour, IInitializable
    {
        private static MirrorStateSyncManager _instance;

        public static MirrorStateSyncManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject obj = new GameObject("[MirrorStateSyncManager]");
                    _instance = obj.AddComponent<MirrorStateSyncManager>();
                    DontDestroyOnLoad(obj);
                }
                return _instance;
            }
        }

        private readonly List<NetworkIdentity> _syncedObjects = new List<NetworkIdentity>();
        private float _syncInterval = 0.05f; // 20次/秒
        private float _lastSyncTime = 0f;
        private bool _isSyncing = false;

        public void Initialize()
        {
            Debug.Log("Mirror状态同步管理器初始化完成");
        }

        public void StartSync()
        {
            _isSyncing = true;
            Debug.Log("状态同步已启动");
        }

        public void StopSync()
        {
            _isSyncing = false;
            Debug.Log("状态同步已停止");
        }

        public void RegisterSyncObject(NetworkIdentity networkIdentity)
        {
            if (networkIdentity == null) return;

            if (!_syncedObjects.Contains(networkIdentity))
            {
                _syncedObjects.Add(networkIdentity);
                Debug.Log($"注册同步对象: {networkIdentity.name}");
            }
        }

        public void UnregisterSyncObject(NetworkIdentity networkIdentity)
        {
            if (networkIdentity == null) return;

            _syncedObjects.Remove(networkIdentity);
            Debug.Log($"注销同步对象: {networkIdentity.name}");
        }

        private void Update()
        {
            if (!_isSyncing) return;

            // 定期同步状态
            if (Time.time - _lastSyncTime >= _syncInterval)
            {
                SyncStates();
                _lastSyncTime = Time.time;
            }
        }

        private void SyncStates()
        {
            // Mirror会自动处理状态同步
            // 这里可以添加自定义同步逻辑
        }

        public void SetSyncInterval(float interval)
        {
            _syncInterval = Mathf.Max(0.01f, interval);
            Debug.Log($"同步间隔设置为: {_syncInterval}秒");
        }
    }
}
```

#### 3.4 事件同步管理器

```csharp
using Mirror;
using UnityEngine;
using Basement.Utils;
using System;
using System.Collections.Generic;

namespace Network.Core
{
    /// <summary>
    /// Mirror事件同步管理器
    /// 负责游戏事件的同步
    /// </summary>
    public class MirrorEventSyncManager : MonoBehaviour, IInitializable
    {
        private static MirrorEventSyncManager _instance;

        public static MirrorEventSyncManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject obj = new GameObject("[MirrorEventSyncManager]");
                    _instance = obj.AddComponent<MirrorEventSyncManager>();
                    DontDestroyOnLoad(obj);
                }
                return _instance;
            }
        }

        private readonly Dictionary<short, Action<NetworkConnection, object[]>> _eventHandlers = new Dictionary<short, Action<NetworkConnection, object[]>>();

        public void Initialize()
        {
            Debug.Log("Mirror事件同步管理器初始化完成");
        }

        public void RegisterEventHandler(short eventCode, Action<NetworkConnection, object[]> handler)
        {
            if (_eventHandlers.ContainsKey(eventCode))
            {
                _eventHandlers[eventCode] += handler;
            }
            else
            {
                _eventHandlers[eventCode] = handler;
            }

            Debug.Log($"注册事件处理器: {eventCode}");
        }

        public void UnregisterEventHandler(short eventCode, Action<NetworkConnection, object[]> handler)
        {
            if (_eventHandlers.ContainsKey(eventCode))
            {
                _eventHandlers[eventCode] -= handler;

                if (_eventHandlers[eventCode] == null)
                {
                    _eventHandlers.Remove(eventCode);
                }

                Debug.Log($"注销事件处理器: {eventCode}");
            }
        }

        public void SendEvent(short eventCode, object[] data, int channelId = Channels.Default)
        {
            if (!NetworkClient.isConnected)
            {
                Debug.LogWarning("不在房间中，无法发送事件");
                return;
            }

            NetworkServer.SendToAll(eventCode, new EventMessage { eventCode = eventCode, data = data });
            Debug.Log($"发送事件: {eventCode}");
        }

        [Server]
        public void SendEventToClient(NetworkConnection conn, short eventCode, object[] data, int channelId = Channels.Default)
        {
            conn.Send(eventCode, new EventMessage { eventCode = eventCode, data = data });
        }

        public void HandleEvent(NetworkConnection conn, EventMessage message)
        {
            short eventCode = message.eventCode;

            if (_eventHandlers.TryGetValue(eventCode, out var handler))
            {
                handler?.Invoke(conn, message.data);
                Debug.Log($"处理事件: {eventCode}");
            }
        }
    }

    /// <summary>
    /// 事件消息结构
    /// </summary>
    public class EventMessage : MessageBase
    {
        public short eventCode;
        public object[] data;
    }
}
```

#### 3.5 延迟处理器

```csharp
using Mirror;
using UnityEngine;
using Basement.Utils;
using System.Collections.Generic;

namespace Network.Core
{
    /// <summary>
    /// 延迟处理器
    /// 负责网络延迟的补偿与处理
    /// </summary>
    public class MirrorLatencyManager : MonoBehaviour, IInitializable
    {
        private static MirrorLatencyManager _instance;

        public static MirrorLatencyManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject obj = new GameObject("[MirrorLatencyManager]");
                    _instance = obj.AddComponent<MirrorLatencyManager>();
                    DontDestroyOnLoad(obj);
                }
                return _instance;
            }
        }

        private readonly Queue<float> _latencyHistory = new Queue<float>();
        private const int MaxHistorySize = 30;
        private float _averageLatency = 0f;
        private float _maxLatency = 0f;
        private float _minLatency = float.MaxValue;

        public float AverageLatency => _averageLatency;
        public float MaxLatency => _maxLatency;
        public float MinLatency => _minLatency;
        public float CurrentLatency => NetworkTime.rtt * 1000f; // 转换为毫秒

        public void Initialize()
        {
            Debug.Log("Mirror延迟处理器初始化完成");
        }

        private void Update()
        {
            UpdateLatency();
        }

        private void UpdateLatency()
        {
            float currentLatency = CurrentLatency;

            if (currentLatency <= 0) return;

            // 更新延迟历史
            _latencyHistory.Enqueue(currentLatency);

            if (_latencyHistory.Count > MaxHistorySize)
            {
                _latencyHistory.Dequeue();
            }

            // 计算统计数据
            CalculateLatencyStats();
        }

        private void CalculateLatencyStats()
        {
            float sum = 0f;
            _maxLatency = 0f;
            _minLatency = float.MaxValue;

            foreach (float latency in _latencyHistory)
            {
                sum += latency;
                _maxLatency = Mathf.Max(_maxLatency, latency);
                _minLatency = Mathf.Min(_minLatency, latency);
            }

            _averageLatency = sum / _latencyHistory.Count;
        }

        public Vector3 PredictPosition(Vector3 currentPosition, Vector3 velocity, float predictionTime = 0f)
        {
            if (predictionTime <= 0f)
            {
                predictionTime = _averageLatency / 1000f;
            }

            return currentPosition + velocity * predictionTime;
        }

        public Vector3 InterpolatePosition(Vector3 startPos, Vector3 endPos, float t)
        {
            return Vector3.Lerp(startPos, endPos, t);
        }

        public Quaternion InterpolateRotation(Quaternion startRot, Quaternion endRot, float t)
        {
            return Quaternion.Slerp(startRot, endRot, t);
        }
    }
}
```

## 使用说明

### 1. 初始化网络服务

```csharp
using UnityEngine;
using Network.Core;

public class NetworkInitializer : MonoBehaviour
{
    [SerializeField] private string networkAddress = "localhost";
    [SerializeField] private int networkPort = 7777;

    private void Start()
    {
        // 初始化网络管理器
        MirrorNetworkManager.Instance.Initialize();
        MirrorRoomManager.Instance.Initialize();
        MirrorStateSyncManager.Instance.Initialize();
        MirrorEventSyncManager.Instance.Initialize();
        MirrorLatencyManager.Instance.Initialize();

        // 设置网络地址和端口
        MirrorNetworkManager.Instance.networkAddress = networkAddress;
        MirrorNetworkManager.Instance.port = networkPort;
    }

    public void StartHost()
    {
        // 启动主机模式（同时作为服务器和客户端）
        MirrorNetworkManager.Instance.StartServer();
    }

    public void StartClient(string address = "localhost")
    {
        // 启动客户端模式
        MirrorNetworkManager.Instance.StartClient(address);
    }

    public void StopNetwork()
    {
        // 停止网络
        MirrorNetworkManager.Instance.StopNetwork();
    }
}
```

### 2. 创建和加入房间

```csharp
using UnityEngine;
using Network.Core;

public class RoomManager : MonoBehaviour
{
    public void CreateGameRoom()
    {
        // 创建房间（启动主机模式）
        MirrorRoomManager.Instance.CreateRoom(
            roomName: "MyRoom",
            maxPlayers: 8
        );
    }

    public void JoinGameRoom(string address)
    {
        // 加入指定房间
        MirrorRoomManager.Instance.JoinRoom(address);
    }

    public void LeaveGameRoom()
    {
        // 离开房间
        MirrorRoomManager.Instance.LeaveRoom();
    }
}
```

### 3. 同步游戏对象

```csharp
using Mirror;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnPositionChanged))] private Vector3 _networkPosition;
    [SyncVar(hook = nameof(OnRotationChanged))] private Quaternion _networkRotation;
    
    private MirrorLatencyManager _latencyManager;
    private Vector3 _velocity;

    private void Start()
    {
        _latencyManager = MirrorLatencyManager.Instance;

        // 注册同步对象
        if (netIdentity != null)
        {
            MirrorStateSyncManager.Instance.RegisterSyncObject(netIdentity);
        }
    }

    private void Update()
    {
        if (isLocalPlayer)
        {
            // 本地玩家控制逻辑
            _velocity = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")) * 5f;
            transform.position += _velocity * Time.deltaTime;
            
            // 更新同步变量
            _networkPosition = transform.position;
            _networkRotation = transform.rotation;
        }
        else
        {
            // 平滑插值
            transform.position = Vector3.Lerp(transform.position, _networkPosition, Time.deltaTime * 10f);
            transform.rotation = Quaternion.Slerp(transform.rotation, _networkRotation, Time.deltaTime * 10f);
        }
    }

    private void OnPositionChanged(Vector3 oldValue, Vector3 newValue)
    {
        // 位置变化回调
        _networkPosition = newValue;
    }

    private void OnRotationChanged(Quaternion oldValue, Quaternion newValue)
    {
        // 旋转变化回调
        _networkRotation = newValue;
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        // 注销同步对象
        if (netIdentity != null)
        {
            MirrorStateSyncManager.Instance.UnregisterSyncObject(netIdentity);
        }
    }
}
```

### 4. 发送和接收事件

```csharp
using Mirror;
using UnityEngine;
using Network.Core;

public class GameEventManager : NetworkBehaviour
{
    private const short EVENT_SKILL_CAST = 1;
    private const short EVENT_PLAYER_DAMAGED = 2;

    private void Start()
    {
        // 注册事件处理器
        MirrorEventSyncManager.Instance.RegisterEventHandler(EVENT_SKILL_CAST, OnSkillCastEvent);
        MirrorEventSyncManager.Instance.RegisterEventHandler(EVENT_PLAYER_DAMAGED, OnPlayerDamagedEvent);
    }

    [Command]
    public void CmdCastSkill(string skillId, Vector3 targetPosition)
    {
        // 发送技能释放事件
        object[] data = new object[] { skillId, targetPosition };
        MirrorEventSyncManager.Instance.SendEvent(EVENT_SKILL_CAST, data);
    }

    [Command]
    public void CmdDamagePlayer(int playerId, int damage)
    {
        // 发送玩家受伤事件
        object[] data = new object[] { playerId, damage };
        MirrorEventSyncManager.Instance.SendEvent(EVENT_PLAYER_DAMAGED, data);
    }

    private void OnSkillCastEvent(NetworkConnection conn, object[] data)
    {
        string skillId = (string)data[0];
        Vector3 targetPosition = (Vector3)data[1];

        Debug.Log($"玩家释放技能: {skillId}, 目标位置: {targetPosition}");

        // 处理技能释放逻辑
    }

    private void OnPlayerDamagedEvent(NetworkConnection conn, object[] data)
    {
        int playerId = (int)data[0];
        int damage = (int)data[1];

        Debug.Log($"玩家 {playerId} 受到 {damage} 点伤害");

        // 处理玩家受伤逻辑
    }
}
```

## 性能优化策略

### 1. 优化同步频率

```csharp
using Mirror;
using UnityEngine;

public class OptimizedNetworkPlayer : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnPositionChanged))] private Vector3 _networkPosition;
    [SyncVar(hook = nameof(OnRotationChanged))] private Quaternion _networkRotation;
    
    private Vector3 _lastPosition;
    private Quaternion _lastRotation;
    private float _syncThreshold = 0.01f;

    private void Update()
    {
        if (isLocalPlayer)
        {
            // 只在状态变化超过阈值时更新同步变量
            if (Vector3.Distance(transform.position, _lastPosition) > _syncThreshold ||
                Quaternion.Angle(transform.rotation, _lastRotation) > _syncThreshold)
            {
                _networkPosition = transform.position;
                _networkRotation = transform.rotation;

                _lastPosition = transform.position;
                _lastRotation = transform.rotation;
            }
        }
        else
        {
            // 平滑插值
            transform.position = Vector3.Lerp(transform.position, _networkPosition, Time.deltaTime * 10f);
            transform.rotation = Quaternion.Slerp(transform.rotation, _networkRotation, Time.deltaTime * 10f);
        }
    }

    private void OnPositionChanged(Vector3 oldValue, Vector3 newValue)
    {
        _networkPosition = newValue;
    }

    private void OnRotationChanged(Quaternion oldValue, Quaternion newValue)
    {
        _networkRotation = newValue;
    }
}
```

### 2. 数据压缩

```csharp
using Mirror;
using System;

public class CompressedNetworkData
{
    public static byte[] CompressVector3(Vector3 vector)
    {
        // 使用16位整数压缩Vector3
        short x = (short)(vector.x * 100);
        short y = (short)(vector.y * 100);
        short z = (short)(vector.z * 100);

        byte[] data = new byte[6];
        Buffer.BlockCopy(BitConverter.GetBytes(x), 0, data, 0, 2);
        Buffer.BlockCopy(BitConverter.GetBytes(y), 0, data, 2, 2);
        Buffer.BlockCopy(BitConverter.GetBytes(z), 0, data, 4, 2);

        return data;
    }

    public static Vector3 DecompressVector3(byte[] data)
    {
        short x = BitConverter.ToInt16(data, 0);
        short y = BitConverter.ToInt16(data, 2);
        short z = BitConverter.ToInt16(data, 4);

        return new Vector3(x / 100f, y / 100f, z / 100f);
    }
}

// 自定义消息示例
public class CompressedPositionMessage : MessageBase
{
    public byte[] compressedPosition;
    public int playerId;
}
```

### 3. 批量事件处理

```csharp
using Mirror;
using UnityEngine;
using System.Collections.Generic;

public class BatchEventProcessor : MonoBehaviour
{
    private readonly Queue<object[]> _eventQueue = new Queue<object[]>();
    private const int MaxBatchSize = 10;
    private float _batchInterval = 0.1f;
    private float _lastBatchTime = 0f;
    private const short EVENT_BATCH = 100;

    public void QueueEvent(object[] eventData)
    {
        _eventQueue.Enqueue(eventData);

        if (_eventQueue.Count >= MaxBatchSize ||
            Time.time - _lastBatchTime >= _batchInterval)
        {
            ProcessBatch();
        }
    }

    private void ProcessBatch()
    {
        if (_eventQueue.Count == 0) return;

        object[] batch = _eventQueue.ToArray();
        _eventQueue.Clear();

        // 发送批量事件
        MirrorEventSyncManager.Instance.SendEvent(EVENT_BATCH, batch);

        _lastBatchTime = Time.time;
    }
}
```

### 4. 自适应同步频率

```csharp
using UnityEngine;
using Network.Core;

public class AdaptiveSyncManager : MonoBehaviour
{
    private float _currentSyncInterval = 0.05f;
    private float _minSyncInterval = 0.02f;
    private float _maxSyncInterval = 0.1f;
    private float _latencyThreshold = 100f; // 100ms

    private void Update()
    {
        UpdateSyncInterval();
    }

    public void UpdateSyncInterval()
    {
        float latency = MirrorLatencyManager.Instance.AverageLatency;

        if (latency > _latencyThreshold)
        {
            // 延迟高，降低同步频率
            _currentSyncInterval = Mathf.Min(_currentSyncInterval * 1.1f, _maxSyncInterval);
        }
        else
        {
            // 延迟低，提高同步频率
            _currentSyncInterval = Mathf.Max(_currentSyncInterval * 0.9f, _minSyncInterval);
        }

        MirrorStateSyncManager.Instance.SetSyncInterval(_currentSyncInterval);
    }
}
```

## 与Unity引擎的结合点

### 1. NetworkIdentity组件集成

```csharp
[RequireComponent(typeof(NetworkIdentity))]
public class NetworkSyncObject : NetworkBehaviour
{
    private NetworkIdentity _networkIdentity;

    private void Awake()
    {
        _networkIdentity = GetComponent<NetworkIdentity>();
    }

    [SyncVar(hook = nameof(OnHealthChanged))] private float _health;

    private void OnHealthChanged(float oldValue, float newValue)
    {
        // 处理生命值变化
    }

    [Command]
    public void CmdTakeDamage(float damage)
    {
        _health -= damage;
        if (_health <= 0)
        {
            // 处理死亡逻辑
        }
    }
}
```

### 2. RPC调用

```csharp
public class NetworkRPCExample : NetworkBehaviour
{
    [ClientRpc]
    public void RpcPlaySound(string soundId)
    {
        // 在所有客户端播放音效
        AudioManager.Instance.PlaySound(soundId);
    }

    [ServerRpc]
    public void ServerRpcRequestRespawn()
    {
        // 服务器处理重生请求
        // ...
        RpcRespawn();
    }

    [ClientRpc]
    public void RpcRespawn()
    {
        // 在客户端执行重生逻辑
    }

    public void TriggerSound()
    {
        // 调用RPC
        RpcPlaySound("Explosion");
    }
}
```

### 3. 场景同步

```csharp
using Mirror;
using UnityEngine.SceneManagement;

public class NetworkSceneManager : NetworkBehaviour
{
    [Server]
    public void LoadScene(string sceneName)
    {
        NetworkManager.singleton.ServerChangeScene(sceneName);
    }

    public override void OnSceneChanged(Scene scene, Scene previousScene)
    {
        Debug.Log($"场景加载完成: {scene.name}");
    }
}
```

## 局域网联机实现

### 1. 局域网发现服务

```csharp
using Mirror;
using UnityEngine;
using System.Collections.Generic;

public class LANDiscovery : NetworkDiscoveryBase
{
    [Header("UI")]
    public UnityEngine.UI.Text statusText;
    public UnityEngine.UI.Button startServerButton;
    public UnityEngine.UI.Button joinButton;
    public UnityEngine.UI.Dropdown serverDropdown;

    private List<ServerResponse> discoveredServers = new List<ServerResponse>();

    public override void OnServerFound(ServerResponse info)
    {
        // 发现新服务器
        discoveredServers.Add(info);
        UpdateServerList();
    }

    public void StartHost()
    {
        // 启动主机模式
        NetworkManager.singleton.StartHost();
        StartDiscovery();
        statusText.text = "主机已启动，等待连接...";
    }

    public void JoinServer()
    {
        // 加入选中的服务器
        if (serverDropdown.value < discoveredServers.Count)
        {
            ServerResponse server = discoveredServers[serverDropdown.value];
            NetworkManager.singleton.networkAddress = server.serverAddress;
            NetworkManager.singleton.StartClient();
            statusText.text = $"正在连接到 {server.serverAddress}...";
        }
    }

    public void StartDiscovery()
    {
        // 开始局域网发现
        StartDiscoveryAsync();
        statusText.text = "正在搜索局域网服务器...";
    }

    private void UpdateServerList()
    {
        // 更新服务器列表
        serverDropdown.ClearOptions();
        foreach (var server in discoveredServers)
        {
            serverDropdown.options.Add(new UnityEngine.UI.Dropdown.OptionData($"{server.serverAddress}:{server.serverPort}"));
        }
        joinButton.interactable = discoveredServers.Count > 0;
    }
}
```

### 2. 本地服务器自动发现

```csharp
using Mirror;
using UnityEngine;
using System.Net;
using System.Net.Sockets;

public class LocalServerDiscovery : MonoBehaviour
{
    public static string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return "127.0.0.1";
    }

    public void StartLocalServer()
    {
        string localIP = GetLocalIPAddress();
        MirrorNetworkManager.Instance.networkAddress = localIP;
        MirrorNetworkManager.Instance.StartServer();
        Debug.Log($"本地服务器已启动，IP: {localIP}");
    }

    public void AutoJoinLocalServer()
    {
        string localIP = GetLocalIPAddress();
        MirrorNetworkManager.Instance.StartClient(localIP);
        Debug.Log($"正在连接到本地服务器: {localIP}");
    }
}
```

## 联网服务接口预留

### 1. 远程服务器连接接口

```csharp
using Mirror;
using UnityEngine;

public class RemoteServerManager : MonoBehaviour
{
    [SerializeField] private string remoteServerAddress = "your-remote-server.com";
    [SerializeField] private int remoteServerPort = 7777;

    public void ConnectToRemoteServer()
    {
        // 连接到远程服务器
        MirrorNetworkManager.Instance.networkAddress = remoteServerAddress;
        MirrorNetworkManager.Instance.port = remoteServerPort;
        MirrorNetworkManager.Instance.StartClient();
        Debug.Log($"正在连接到远程服务器: {remoteServerAddress}:{remoteServerPort}");
    }

    public void ConnectToLocalServer()
    {
        // 切换回本地服务器
        MirrorNetworkManager.Instance.networkAddress = "localhost";
        MirrorNetworkManager.Instance.port = 7777;
        MirrorNetworkManager.Instance.StartClient();
        Debug.Log("正在连接到本地服务器");
    }
}
```

### 2. 网络传输层抽象

```csharp
using Mirror;
using UnityEngine;

public class NetworkTransportManager : MonoBehaviour
{
    public enum TransportType
    {
        Telepathy, // 默认传输
        KCP,       // 可靠UDP
        SteamP2P   // Steam P2P
    }

    public void SetTransport(TransportType transportType)
    {
        // 根据类型设置不同的传输层
        switch (transportType)
        {
            case TransportType.Telepathy:
                NetworkManager.singleton.transport = GetComponent<TelepathyTransport>();
                break;
            case TransportType.KCP:
                NetworkManager.singleton.transport = GetComponent<KcpTransport>();
                break;
            case TransportType.SteamP2P:
                NetworkManager.singleton.transport = GetComponent<SteamP2PTransport>();
                break;
        }
        Debug.Log($"网络传输层已设置为: {transportType}");
    }
}
```

## 总结

网络同步服务通过基于Mirror 96.10.0框架的混合同步策略，实现了高效、稳定的多端数据同步机制，具有以下优势：

1. **混合同步策略**：结合状态同步与事件同步的优势，优化网络传输效率
2. **延迟补偿**：实现延迟补偿机制，减少网络延迟对游戏体验的影响
3. **冲突解决**：实现冲突解决机制，确保多端状态的一致性
4. **性能优化**：通过数据压缩、同步频率优化等手段，提高网络性能
5. **易于扩展**：支持自定义同步策略和网络事件处理
6. **局域网优先**：优先支持局域网联机，同时预留了联网服务接口

通过使用网络同步服务，项目可以实现高效的多人在线游戏功能，提升游戏体验和可玩性。

## 参考文档

- [Mirror Networking官方文档](https://mirror-networking.gitbook.io/docs/)
- [Mirror Networking GitHub仓库](https://github.com/MirrorNetworking/Mirror)
- [Unity Mirror Networking教程](https://docs.unity.com/ugs/en-us/manual/relay/manual/mirror)
- [Mirror Networking最佳实践](https://unitystation.github.io/unitystation/development/SyncVar-Best-Practices-for-Easy-Networking/)
