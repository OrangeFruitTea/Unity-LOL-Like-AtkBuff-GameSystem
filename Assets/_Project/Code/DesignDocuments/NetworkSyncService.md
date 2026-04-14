# 网络同步服务技术文档

## 概述

网络同步服务是核心业务层的核心组件，负责实现基于Photon PUN2框架的多人在线游戏网络同步功能。该服务采用混合同步策略，结合状态同步与事件同步的优势，实现高效、稳定的多端数据同步机制，支持连接管理、房间管理、状态同步、事件同步、延迟处理等核心功能。

## 模块架构设计

### 1. 设计目标

- **混合同步策略**：结合状态同步与事件同步的优势，优化网络传输效率
- **延迟补偿**：实现延迟补偿机制，减少网络延迟对游戏体验的影响
- **冲突解决**：实现冲突解决机制，确保多端状态的一致性
- **性能优化**：通过数据压缩、同步频率优化等手段，提高网络性能
- **易于扩展**：支持自定义同步策略和网络事件处理

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
                    │Photon PUN2  │
                    │   服务器    │
                    └─────────────┘
```

### 3. 核心组件

#### 3.1 网络管理器

```csharp
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Basement.Utils;

namespace Network.Core
{
    /// <summary>
    /// Photon网络管理器
    /// 负责与Photon服务器的连接管理
    /// </summary>
    public class PhotonNetworkManager : MonoBehaviourPunCallbacks, IInitializable
    {
        private static PhotonNetworkManager _instance;

        public static PhotonNetworkManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject obj = new GameObject("[PhotonNetworkManager]");
                    _instance = obj.AddComponent<PhotonNetworkManager>();
                    DontDestroyOnLoad(obj);
                }
                return _instance;
            }
        }

        public bool IsConnected => PhotonNetwork.IsConnected;
        public bool IsInRoom => PhotonNetwork.InRoom;
        public Room CurrentRoom => PhotonNetwork.CurrentRoom;
        public Player LocalPlayer => PhotonNetwork.LocalPlayer;

        private PhotonRoomManager _roomManager;
        private PhotonStateSyncManager _stateSyncManager;
        private PhotonEventSyncManager _eventSyncManager;

        public void Initialize()
        {
            // 初始化Photon设置
            PhotonNetwork.AutomaticallySyncScene = false;
            PhotonNetwork.SendRate = 20;
            PhotonNetwork.SerializationRate = 10;

            // 初始化子管理器
            _roomManager = PhotonRoomManager.Instance;
            _stateSyncManager = PhotonStateSyncManager.Instance;
            _eventSyncManager = PhotonEventSyncManager.Instance;

            Debug.Log("Photon网络管理器初始化完成");
        }

        public void Connect(string appId, string version)
        {
            PhotonNetwork.GameVersion = version;
            PhotonNetwork.ConnectUsingSettings();
            PhotonNetwork.PhotonServerSettings.AppSettings.AppID = appId;

            Debug.Log($"正在连接Photon服务器... (版本: {version})");
        }

        public void Disconnect()
        {
            PhotonNetwork.Disconnect();
            Debug.Log("已断开Photon服务器连接");
        }

        public override void OnConnectedToMaster()
        {
            Debug.Log("已连接到Photon主服务器");
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.LogError($"与Photon服务器断开连接: {cause}");
        }

        public override void OnJoinedRoom()
        {
            Debug.Log($"已加入房间: {CurrentRoom.Name}");
            _stateSyncManager.StartSync();
        }

        public override void OnLeftRoom()
        {
            Debug.Log("已离开房间");
            _stateSyncManager.StopSync();
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            Debug.Log($"玩家加入房间: {newPlayer.NickName}");
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log($"玩家离开房间: {otherPlayer.NickName}");
        }
    }
}
```

#### 3.2 房间管理器

```csharp
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Basement.Utils;
using System.Collections.Generic;

namespace Network.Core
{
    /// <summary>
    /// Photon房间管理器
    /// 负责房间的创建、加入、搜索、退出等功能
    /// </summary>
    public class PhotonRoomManager : MonoBehaviourPunCallbacks, IInitializable
    {
        private static PhotonRoomManager _instance;

        public static PhotonRoomManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject obj = new GameObject("[PhotonRoomManager]");
                    _instance = obj.AddComponent<PhotonRoomManager>();
                    DontDestroyOnLoad(obj);
                }
                return _instance;
            }
        }

        public bool IsInRoom => PhotonNetwork.InRoom;
        public Room CurrentRoom => PhotonNetwork.CurrentRoom;

        public void Initialize()
        {
            Debug.Log("Photon房间管理器初始化完成");
        }

        public void CreateRoom(string roomName, byte maxPlayers = 8, RoomOptions roomOptions = null)
        {
            if (PhotonNetwork.InRoom)
            {
                Debug.LogWarning("已经在房间中，无法创建新房间");
                return;
            }

            RoomOptions options = roomOptions ?? new RoomOptions
            {
                MaxPlayers = maxPlayers,
                IsVisible = true,
                IsOpen = true,
                CustomRoomProperties = new ExitGames.Client.Photon.Hashtable()
            };

            PhotonNetwork.CreateRoom(roomName, options);
            Debug.Log($"正在创建房间: {roomName}");
        }

        public void JoinRoom(string roomName)
        {
            if (PhotonNetwork.InRoom)
            {
                Debug.LogWarning("已经在房间中，无法加入新房间");
                return;
            }

            PhotonNetwork.JoinRoom(roomName);
            Debug.Log($"正在加入房间: {roomName}");
        }

        public void JoinRandomRoom(Hashtable expectedProperties = null)
        {
            if (PhotonNetwork.InRoom)
            {
                Debug.LogWarning("已经在房间中，无法加入随机房间");
                return;
            }

            PhotonNetwork.JoinRandomRoom(expectedProperties);
            Debug.Log("正在加入随机房间");
        }

        public void LeaveRoom()
        {
            if (!PhotonNetwork.InRoom)
            {
                Debug.LogWarning("不在房间中，无法离开房间");
                return;
            }

            PhotonNetwork.LeaveRoom();
            Debug.Log("正在离开房间");
        }

        public void SetRoomProperty(string key, object value)
        {
            if (!PhotonNetwork.InRoom) return;

            Hashtable properties = new Hashtable { { key, value } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(properties);
        }

        public object GetRoomProperty(string key)
        {
            if (!PhotonNetwork.InRoom) return null;

            return PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(key)
                ? PhotonNetwork.CurrentRoom.CustomProperties[key]
                : null;
        }

        public override void OnCreatedRoom()
        {
            Debug.Log($"房间创建成功: {CurrentRoom.Name}");
        }

        public override void OnJoinedRoom()
        {
            Debug.Log($"成功加入房间: {CurrentRoom.Name}");
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"加入房间失败: {message} (错误代码: {returnCode})");
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"创建房间失败: {message} (错误代码: {returnCode})");
        }

        public override void OnLeftRoom()
        {
            Debug.Log("已离开房间");
        }
    }
}
```

#### 3.3 状态同步管理器

```csharp
using Photon.Pun;
using UnityEngine;
using Basement.Utils;
using System.Collections.Generic;

namespace Network.Core
{
    /// <summary>
    /// Photon状态同步管理器
    /// 负责游戏对象状态的同步
    /// </summary>
    public class PhotonStateSyncManager : MonoBehaviourPunCallbacks, IInitializable
    {
        private static PhotonStateSyncManager _instance;

        public static PhotonStateSyncManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject obj = new GameObject("[PhotonStateSyncManager]");
                    _instance = obj.AddComponent<PhotonStateSyncManager>();
                    DontDestroyOnLoad(obj);
                }
                return _instance;
            }
        }

        private readonly List<PhotonView> _syncedObjects = new List<PhotonView>();
        private float _syncInterval = 0.05f; // 20次/秒
        private float _lastSyncTime = 0f;
        private bool _isSyncing = false;

        public void Initialize()
        {
            Debug.Log("Photon状态同步管理器初始化完成");
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

        public void RegisterSyncObject(PhotonView photonView)
        {
            if (photonView == null) return;

            if (!_syncedObjects.Contains(photonView))
            {
                _syncedObjects.Add(photonView);
                Debug.Log($"注册同步对象: {photonView.name}");
            }
        }

        public void UnregisterSyncObject(PhotonView photonView)
        {
            if (photonView == null) return;

            _syncedObjects.Remove(photonView);
            Debug.Log($"注销同步对象: {photonView.name}");
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
            foreach (var photonView in _syncedObjects)
            {
                if (photonView == null || !photonView.IsMine) continue;

                // 同步Transform状态
                if (photonView.ObservedComponents != null && photonView.ObservedComponents.Count > 0)
                {
                    photonView.ObservedComponents[0].OnPhotonSerializeView(
                        new PhotonStream(true, photonView),
                        photonView.Synchronization
                    );
                }
            }
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
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Basement.Utils;
using System;
using System.Collections.Generic;

namespace Network.Core
{
    /// <summary>
    /// Photon事件同步管理器
    /// 负责游戏事件的同步
    /// </summary>
    public class PhotonEventSyncManager : MonoBehaviourPunCallbacks, IInitializable
    {
        private static PhotonEventSyncManager _instance;

        public static PhotonEventSyncManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject obj = new GameObject("[PhotonEventSyncManager]");
                    _instance = obj.AddComponent<PhotonEventSyncManager>();
                    DontDestroyOnLoad(obj);
                }
                return _instance;
            }
        }

        private readonly Dictionary<byte, Action<object[]>> _eventHandlers = new Dictionary<byte, Action<object[]>>();

        public void Initialize()
        {
            Debug.Log("Photon事件同步管理器初始化完成");
        }

        public void RegisterEventHandler(byte eventCode, Action<object[]> handler)
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

        public void UnregisterEventHandler(byte eventCode, Action<object[]> handler)
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

        public void SendEvent(byte eventCode, object[] data, RaiseEventOptions options = null, SendOptions sendOptions = null)
        {
            if (!PhotonNetwork.InRoom)
            {
                Debug.LogWarning("不在房间中，无法发送事件");
                return;
            }

            PhotonNetwork.RaiseEvent(eventCode, data, options, sendOptions);
            Debug.Log($"发送事件: {eventCode}");
        }

        public override void OnEnable()
        {
            base.OnEnable();
            PhotonNetwork.AddCallbackTarget(this);
        }

        public override void OnDisable()
        {
            base.OnDisable();
            PhotonNetwork.RemoveCallbackTarget(this);
        }

        public void OnEvent(EventData photonEvent)
        {
            byte eventCode = photonEvent.Code;

            if (_eventHandlers.TryGetValue(eventCode, out var handler))
            {
                handler?.Invoke(photonEvent.CustomData as object[]);
                Debug.Log($"处理事件: {eventCode}");
            }
        }
    }
}
```

#### 3.5 延迟处理器

```csharp
using UnityEngine;
using Basement.Utils;
using System.Collections.Generic;

namespace Network.Core
{
    /// <summary>
    /// 延迟处理器
    /// 负责网络延迟的补偿与处理
    /// </summary>
    public class PhotonLatencyManager : MonoBehaviour, IInitializable
    {
        private static PhotonLatencyManager _instance;

        public static PhotonLatencyManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject obj = new GameObject("[PhotonLatencyManager]");
                    _instance = obj.AddComponent<PhotonLatencyManager>();
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
        public float CurrentLatency => PhotonNetwork.GetPing();

        public void Initialize()
        {
            Debug.Log("Photon延迟处理器初始化完成");
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
    [SerializeField] private string photonAppId = "YOUR_APP_ID";
    [SerializeField] private string gameVersion = "1.0.0";

    private void Start()
    {
        // 初始化网络管理器
        PhotonNetworkManager.Instance.Initialize();
        PhotonRoomManager.Instance.Initialize();
        PhotonStateSyncManager.Instance.Initialize();
        PhotonEventSyncManager.Instance.Initialize();
        PhotonLatencyManager.Instance.Initialize();

        // 连接到Photon服务器
        PhotonNetworkManager.Instance.Connect(photonAppId, gameVersion);
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
        // 创建房间
        PhotonRoomManager.Instance.CreateRoom(
            roomName: "MyRoom",
            maxPlayers: 8
        );
    }

    public void JoinGameRoom(string roomName)
    {
        // 加入指定房间
        PhotonRoomManager.Instance.JoinRoom(roomName);
    }

    public void JoinRandomGameRoom()
    {
        // 加入随机房间
        PhotonRoomManager.Instance.JoinRandomRoom();
    }

    public void LeaveGameRoom()
    {
        // 离开房间
        PhotonRoomManager.Instance.LeaveRoom();
    }
}
```

### 3. 同步游戏对象

```csharp
using Photon.Pun;
using UnityEngine;

public class NetworkPlayer : MonoBehaviourPun, IPunObservable
{
    private Vector3 _networkPosition;
    private Quaternion _networkRotation;
    private PhotonLatencyManager _latencyManager;

    private void Start()
    {
        _latencyManager = PhotonLatencyManager.Instance;

        // 注册同步对象
        if (photonView != null)
        {
            PhotonStateSyncManager.Instance.RegisterSyncObject(photonView);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 发送本地状态
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            // 接收远程状态
            _networkPosition = (Vector3)stream.ReceiveNext();
            _networkRotation = (Quaternion)stream.ReceiveNext();

            // 延迟补偿
            float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
            _networkPosition = _latencyManager.PredictPosition(_networkPosition, Vector3.zero, lag);
        }
    }

    private void Update()
    {
        if (!photonView.IsMine)
        {
            // 平滑插值
            transform.position = Vector3.Lerp(transform.position, _networkPosition, Time.deltaTime * 10f);
            transform.rotation = Quaternion.Slerp(transform.rotation, _networkRotation, Time.deltaTime * 10f);
        }
    }
}
```

### 4. 发送和接收事件

```csharp
using UnityEngine;
using Network.Core;

public class GameEventManager : MonoBehaviour
{
    private const byte EVENT_SKILL_CAST = 1;
    private const byte EVENT_PLAYER_DAMAGED = 2;

    private void Start()
    {
        // 注册事件处理器
        PhotonEventSyncManager.Instance.RegisterEventHandler(EVENT_SKILL_CAST, OnSkillCastEvent);
        PhotonEventSyncManager.Instance.RegisterEventHandler(EVENT_PLAYER_DAMAGED, OnPlayerDamagedEvent);
    }

    public void CastSkill(string skillId, Vector3 targetPosition)
    {
        // 发送技能释放事件
        object[] data = new object[] { skillId, targetPosition };
        PhotonEventSyncManager.Instance.SendEvent(EVENT_SKILL_CAST, data);
    }

    public void DamagePlayer(int playerId, int damage)
    {
        // 发送玩家受伤事件
        object[] data = new object[] { playerId, damage };
        PhotonEventSyncManager.Instance.SendEvent(EVENT_PLAYER_DAMAGED, data);
    }

    private void OnSkillCastEvent(object[] data)
    {
        string skillId = (string)data[0];
        Vector3 targetPosition = (Vector3)data[1];

        Debug.Log($"玩家释放技能: {skillId}, 目标位置: {targetPosition}");

        // 处理技能释放逻辑
    }

    private void OnPlayerDamagedEvent(object[] data)
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
public class OptimizedStateSync : MonoBehaviourPun, IPunObservable
{
    private Vector3 _lastPosition;
    private Quaternion _lastRotation;
    private float _syncThreshold = 0.01f;

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 只在状态变化超过阈值时发送
            if (Vector3.Distance(transform.position, _lastPosition) > _syncThreshold ||
                Quaternion.Angle(transform.rotation, _lastRotation) > _syncThreshold)
            {
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);

                _lastPosition = transform.position;
                _lastRotation = transform.rotation;
            }
        }
        else
        {
            _lastPosition = (Vector3)stream.ReceiveNext();
            _lastRotation = (Quaternion)stream.ReceiveNext();
        }
    }
}
```

### 2. 数据压缩

```csharp
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
```

### 3. 批量事件处理

```csharp
public class BatchEventProcessor
{
    private readonly Queue<object[]> _eventQueue = new Queue<object[]>();
    private const int MaxBatchSize = 10;
    private float _batchInterval = 0.1f;
    private float _lastBatchTime = 0f;

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
        PhotonEventSyncManager.Instance.SendEvent(EVENT_BATCH, batch);

        _lastBatchTime = Time.time;
    }
}
```

### 4. 自适应同步频率

```csharp
public class AdaptiveSyncManager
{
    private float _currentSyncInterval = 0.05f;
    private float _minSyncInterval = 0.02f;
    private float _maxSyncInterval = 0.1f;
    private float _latencyThreshold = 100f; // 100ms

    public void UpdateSyncInterval()
    {
        float latency = PhotonLatencyManager.Instance.AverageLatency;

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

        PhotonStateSyncManager.Instance.SetSyncInterval(_currentSyncInterval);
    }
}
```

## 与Unity引擎的结合点

### 1. PhotonView组件集成

```csharp
[RequireComponent(typeof(PhotonView))]
public class NetworkSyncObject : MonoBehaviourPun, IPunObservable
{
    private PhotonView _photonView;

    private void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        _photonView.ObservedComponents.Add(this);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // 实现状态同步逻辑
    }
}
```

### 2. RPC调用

```csharp
public class NetworkRPCExample : MonoBehaviourPun
{
    [PunRPC]
    public void PlaySound(string soundId)
    {
        // 在所有客户端播放音效
        AudioManager.Instance.PlaySound(soundId);
    }

    public void TriggerSound()
    {
        // 调用RPC
        photonView.RPC(nameof(PlaySound), RpcTarget.All, "Explosion");
    }
}
```

### 3. 场景同步

```csharp
public class NetworkSceneManager : MonoBehaviourPunCallbacks
{
    public void LoadScene(string sceneName)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(sceneName);
        }
    }

    public override void OnLevelWasLoaded(int level)
    {
        Debug.Log($"场景加载完成: {SceneManager.GetSceneByBuildIndex(level).name}");
    }
}
```

## 总结

网络同步服务通过基于Photon PUN2框架的混合同步策略，实现了高效、稳定的多端数据同步机制，具有以下优势：

1. **混合同步策略**：结合状态同步与事件同步的优势，优化网络传输效率
2. **延迟补偿**：实现延迟补偿机制，减少网络延迟对游戏体验的影响
3. **冲突解决**：实现冲突解决机制，确保多端状态的一致性
4. **性能优化**：通过数据压缩、同步频率优化等手段，提高网络性能
5. **易于扩展**：支持自定义同步策略和网络事件处理

通过使用网络同步服务，项目可以实现高效的多人在线游戏功能，提升游戏体验和可玩性。

## 参考文档

- [Photon PUN2官方文档](https://doc.photonengine.com/en-us/pun/v2)
- [PhotonNetworkSystemImplementation.md](../Network/PhotonNetworkSystemImplementation.md) - 详细的网络同步系统实现方案
