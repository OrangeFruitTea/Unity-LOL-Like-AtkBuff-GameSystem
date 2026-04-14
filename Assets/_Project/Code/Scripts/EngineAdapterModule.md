# 引擎适配模块技术文档

## 概述

引擎适配模块是基础设施层的核心组件，负责封装Unity引擎核心API，提供标准化的接口库，实现项目与Unity引擎的解耦。该模块通过适配器模式，将Unity引擎的特定API转换为项目统一的接口，便于后续引擎替换或升级。

## 模块架构设计

### 1. 设计目标

- **解耦Unity引擎**：将Unity引擎API与业务逻辑分离，降低耦合度
- **提供统一接口**：封装Unity核心功能，提供标准化的访问接口
- **支持引擎替换**：便于未来替换为其他游戏引擎（如Unreal、Godot）
- **提升可测试性**：通过接口抽象，便于单元测试和Mock
- **简化开发流程**：封装复杂操作，提供简化的开发接口

### 2. 架构分层

```
┌─────────────────────────────────────────────────────────────┐
│                    业务逻辑层                               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  │
│  │ 角色系统    │  │ 对战规则    │  │ 经济系统    │  │
│  └─────────────┘  └─────────────┘  └─────────────┘  │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│                    引擎适配层                               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  │
│  │ IEngineCore │  │ IResource   │  │ IInput      │  │
│  │ ITransform  │  │ IAudio      │  │ IPhysics    │  │
│  │ IUI         │  │ INetwork    │  │ ITime       │  │
│  └─────────────┘  └─────────────┘  └─────────────┘  │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│                    Unity实现层                              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  │
│  │UnityEngine  │  │UnityResource│  │UnityInput   │  │
│  │UnityTransform│ │UnityAudio   │  │UnityPhysics │  │
│  │UnityUI      │  │UnityNetwork │  │UnityTime    │  │
│  └─────────────┘  └─────────────┘  └─────────────┘  │
└─────────────────────────────────────────────────────────────┘
                           ↓
                    ┌─────────────┐
                    │Unity Engine │
                    └─────────────┘
```

### 3. 核心组件

#### 3.1 引擎核心接口 (IEngineCore)

```csharp
namespace Basement.Engine
{
    /// <summary>
    /// 引擎核心接口，提供引擎级别的通用功能
    /// </summary>
    public interface IEngineCore
    {
        /// <summary>
        /// 获取当前引擎版本
        /// </summary>
        string EngineVersion { get; }

        /// <summary>
        /// 获取当前平台
        /// </summary>
        RuntimePlatform CurrentPlatform { get; }

        /// <summary>
        /// 获取应用程序数据路径
        /// </summary>
        string DataPath { get; }

        /// <summary>
        /// 获取持久化数据路径
        /// </summary>
        string PersistentDataPath { get; }

        /// <summary>
        /// 获取临时缓存路径
        /// </summary>
        string TemporaryCachePath { get; }

        /// <summary>
        /// 初始化引擎
        /// </summary>
        void Initialize();

        /// <summary>
        /// 更新引擎状态
        /// </summary>
        void Update(float deltaTime);

        /// <summary>
        /// 清理引擎资源
        /// </summary>
        void Cleanup();
    }
}
```

#### 3.2 Unity引擎核心实现

```csharp
using UnityEngine;

namespace Basement.Engine
{
    /// <summary>
    /// Unity引擎核心实现
    /// </summary>
    public class UnityEngineCore : IEngineCore
    {
        public string EngineVersion => Application.unityVersion;
        public RuntimePlatform CurrentPlatform => Application.platform;
        public string DataPath => Application.dataPath;
        public string PersistentDataPath => Application.persistentDataPath;
        public string TemporaryCachePath => Application.temporaryCachePath;

        public void Initialize()
        {
            Debug.Log($"Unity引擎初始化 - 版本: {EngineVersion}, 平台: {CurrentPlatform}");
        }

        public void Update(float deltaTime)
        {
            // Unity引擎更新逻辑由主循环处理
        }

        public void Cleanup()
        {
            Debug.Log("Unity引擎清理完成");
        }
    }
}
```

### 4. 变换系统适配

#### 4.1 变换接口

```csharp
using UnityEngine;

namespace Basement.Engine
{
    /// <summary>
    /// 变换系统接口
    /// </summary>
    public interface ITransform
    {
        /// <summary>
        /// 位置
        /// </summary>
        Vector3 Position { get; set; }

        /// <summary>
        /// 旋转
        /// </summary>
        Quaternion Rotation { get; set; }

        /// <summary>
        /// 缩放
        /// </summary>
        Vector3 Scale { get; set; }

        /// <summary>
        /// 向前方向
        /// </summary>
        Vector3 Forward { get; }

        /// <summary>
        /// 向上方向
        /// </summary>
        Vector3 Up { get; }

        /// <summary>
        /// 向右方向
        /// </summary>
        Vector3 Right { get; }

        /// <summary>
        /// 父对象
        /// </summary>
        ITransform Parent { get; set; }

        /// <summary>
        /// 移动到指定位置
        /// </summary>
        void MoveTo(Vector3 position);

        /// <summary>
        /// 相对移动
        /// </summary>
        void Translate(Vector3 translation, Space space = Space.Self);

        /// <summary>
        /// 旋转到指定角度
        /// </summary>
        void RotateTo(Quaternion rotation);

        /// <summary>
        /// 相对旋转
        /// </summary>
        void Rotate(Vector3 eulerAngles, Space space = Space.Self);

        /// <summary>
        /// 查看向目标
        /// </summary>
        void LookAt(Vector3 target);

        /// <summary>
        /// 设置父对象
        /// </summary>
        void SetParent(ITransform parent, bool worldPositionStays = true);
    }
}
```

#### 4.2 Unity变换实现

```csharp
using UnityEngine;

namespace Basement.Engine
{
    /// <summary>
    /// Unity变换实现
    /// </summary>
    public class UnityTransform : ITransform
    {
        private Transform _transform;

        public UnityTransform(Transform transform)
        {
            _transform = transform;
        }

        public Vector3 Position
        {
            get => _transform.position;
            set => _transform.position = value;
        }

        public Quaternion Rotation
        {
            get => _transform.rotation;
            set => _transform.rotation = value;
        }

        public Vector3 Scale
        {
            get => _transform.localScale;
            set => _transform.localScale = value;
        }

        public Vector3 Forward => _transform.forward;
        public Vector3 Up => _transform.up;
        public Vector3 Right => _transform.right;

        public ITransform Parent
        {
            get => _transform.parent != null ? new UnityTransform(_transform.parent) : null;
            set => _transform.SetParent(value != null ? ((UnityTransform)value)._transform : null);
        }

        public void MoveTo(Vector3 position)
        {
            _transform.position = position;
        }

        public void Translate(Vector3 translation, Space space = Space.Self)
        {
            _transform.Translate(translation, space);
        }

        public void RotateTo(Quaternion rotation)
        {
            _transform.rotation = rotation;
        }

        public void Rotate(Vector3 eulerAngles, Space space = Space.Self)
        {
            _transform.Rotate(eulerAngles, space);
        }

        public void LookAt(Vector3 target)
        {
            _transform.LookAt(target);
        }

        public void SetParent(ITransform parent, bool worldPositionStays = true)
        {
            _transform.SetParent(parent != null ? ((UnityTransform)value)._transform : null, worldPositionStays);
        }
    }
}
```

### 5. 时间系统适配

#### 5.1 时间接口

```csharp
namespace Basement.Engine
{
    /// <summary>
    /// 时间系统接口
    /// </summary>
    public interface ITime
    {
        /// <summary>
        /// 从游戏开始到现在的总时间（秒）
        /// </summary>
        float Time { get; }

        /// <summary>
        /// 上一帧到当前帧的时间间隔（秒）
        /// </summary>
        float DeltaTime { get; }

        /// <summary>
        /// 不受时间缩放影响的帧间隔
        /// </summary>
        float UnscaledDeltaTime { get; }

        /// <summary>
        /// 时间缩放因子
        /// </summary>
        float TimeScale { get; set; }

        /// <summary>
        /// 固定时间步长
        /// </summary>
        float FixedDeltaTime { get; }

        /// <summary>
        /// 帧计数
        /// </summary>
        int FrameCount { get; }

        /// <summary>
        /// 暂停游戏
        /// </summary>
        void Pause();

        /// <summary>
        /// 恢复游戏
        /// </summary>
        void Resume();

        /// <summary>
        /// 设置时间缩放
        /// </summary>
        void SetTimeScale(float scale);
    }
}
```

#### 5.2 Unity时间实现

```csharp
using UnityEngine;

namespace Basement.Engine
{
    /// <summary>
    /// Unity时间系统实现
    /// </summary>
    public class UnityTime : ITime
    {
        public float Time => UnityEngine.Time.time;
        public float DeltaTime => UnityEngine.Time.deltaTime;
        public float UnscaledDeltaTime => UnityEngine.Time.unscaledDeltaTime;
        public float TimeScale
        {
            get => UnityEngine.Time.timeScale;
            set => UnityEngine.Time.timeScale = value;
        }
        public float FixedDeltaTime => UnityEngine.Time.fixedDeltaTime;
        public int FrameCount => UnityEngine.Time.frameCount;

        public void Pause()
        {
            UnityEngine.Time.timeScale = 0f;
        }

        public void Resume()
        {
            UnityEngine.Time.timeScale = 1f;
        }

        public void SetTimeScale(float scale)
        {
            UnityEngine.Time.timeScale = Mathf.Clamp01(scale);
        }
    }
}
```

### 6. 输入系统适配

#### 6.1 输入接口

```csharp
using UnityEngine;

namespace Basement.Engine
{
    /// <summary>
    /// 输入系统接口
    /// </summary>
    public interface IInput
    {
        /// <summary>
        /// 检查按键是否按下
        /// </summary>
        bool GetKeyDown(KeyCode key);

        /// <summary>
        /// 检查按键是否持续按下
        /// </summary>
        bool GetKey(KeyCode key);

        /// <summary>
        /// 检查按键是否抬起
        /// </summary>
        bool GetKeyUp(KeyCode key);

        /// <summary>
        /// 获取鼠标位置
        /// </summary>
        Vector2 MousePosition { get; }

        /// <summary>
        /// 检查鼠标按钮是否按下
        /// </summary>
        bool GetMouseButtonDown(int button);

        /// <summary>
        /// 检查鼠标按钮是否持续按下
        /// </summary>
        bool GetMouseButton(int button);

        /// <summary>
        /// 检查鼠标按钮是否抬起
        /// </summary>
        bool GetMouseButtonUp(int button);

        /// <summary>
        /// 获取鼠标滚轮值
        /// </summary>
        float MouseScrollDelta { get; }

        /// <summary>
        /// 获取轴输入
        /// </summary>
        float GetAxis(string axisName);

        /// <summary>
        /// 获取原始轴输入
        /// </summary>
        float GetAxisRaw(string axisName);

        /// <summary>
        /// 启用/禁用输入
        /// </summary>
        bool InputEnabled { get; set; }
    }
}
```

#### 6.2 Unity输入实现

```csharp
using UnityEngine;

namespace Basement.Engine
{
    /// <summary>
    /// Unity输入系统实现
    /// </summary>
    public class UnityInput : IInput
    {
        private bool _inputEnabled = true;

        public Vector2 MousePosition => Input.mousePosition;
        public float MouseScrollDelta => Input.mouseScrollDelta.y;
        public bool InputEnabled
        {
            get => _inputEnabled;
            set => _inputEnabled = value;
        }

        public bool GetKeyDown(KeyCode key)
        {
            return _inputEnabled && Input.GetKeyDown(key);
        }

        public bool GetKey(KeyCode key)
        {
            return _inputEnabled && Input.GetKey(key);
        }

        public bool GetKeyUp(KeyCode key)
        {
            return _inputEnabled && Input.GetKeyUp(key);
        }

        public bool GetMouseButtonDown(int button)
        {
            return _inputEnabled && Input.GetMouseButtonDown(button);
        }

        public bool GetMouseButton(int button)
        {
            return _inputEnabled && Input.GetMouseButton(button);
        }

        public bool GetMouseButtonUp(int button)
        {
            return _inputEnabled && Input.GetMouseButtonUp(button);
        }

        public float GetAxis(string axisName)
        {
            return _inputEnabled ? Input.GetAxis(axisName) : 0f;
        }

        public float GetAxisRaw(string axisName)
        {
            return _inputEnabled ? Input.GetAxisRaw(axisName) : 0f;
        }
    }
}
```

### 7. 物理系统适配

#### 7.1 物理接口

```csharp
using UnityEngine;

namespace Basement.Engine
{
    /// <summary>
    /// 物理系统接口
    /// </summary>
    public interface IPhysics
    {
        /// <summary>
        /// 重力加速度
        /// </summary>
        Vector3 Gravity { get; set; }

        /// <summary>
        /// 射线检测
        /// </summary>
        bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers);

        /// <summary>
        /// 球体检测
        /// </summary>
        bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers);

        /// <summary>
        /// 检测指定范围内的碰撞体
        /// </summary>
        Collider[] OverlapSphere(Vector3 position, float radius, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal);

        /// <summary>
        /// 检测指定盒体内的碰撞体
        /// </summary>
        Collider[] OverlapBox(Vector3 center, Vector3 halfExtents, Quaternion orientation, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal);

        /// <summary>
        /// 检测指定胶囊体内的碰撞体
        /// </summary>
        Collider[] OverlapCapsule(Vector3 point0, Vector3 point1, float radius, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal);

        /// <summary>
        /// 忽略碰撞
        /// </summary>
        void IgnoreCollision(Collider collider1, Collider collider2, bool ignore = true);

        /// <summary>
        /// 忽略层碰撞
        /// </summary>
        void IgnoreLayerCollision(int layer1, int layer2, bool ignore = true);
    }
}
```

#### 7.2 Unity物理实现

```csharp
using UnityEngine;

namespace Basement.Engine
{
    /// <summary>
    /// Unity物理系统实现
    /// </summary>
    public class UnityPhysics : IPhysics
    {
        public Vector3 Gravity
        {
            get => Physics.gravity;
            set => Physics.gravity = value;
        }

        public bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hitInfo, float maxDistance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers)
        {
            return Physics.Raycast(origin, direction, out hitInfo, maxDistance, layerMask);
        }

        public bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hitInfo, float maxDistance = Mathf.Infinity, int layerMask = Physics.DefaultRaycastLayers)
        {
            return Physics.SphereCast(origin, radius, direction, out hitInfo, maxDistance, layerMask);
        }

        public Collider[] OverlapSphere(Vector3 position, float radius, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            return Physics.OverlapSphere(position, radius, layerMask, queryTriggerInteraction);
        }

        public Collider[] OverlapBox(Vector3 center, Vector3 halfExtents, Quaternion orientation, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            return Physics.OverlapBox(center, halfExtents, orientation, layerMask, queryTriggerInteraction);
        }

        public Collider[] OverlapCapsule(Vector3 point0, Vector3 point1, float radius, int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            return Physics.OverlapCapsule(point0, point1, radius, layerMask, queryTriggerInteraction);
        }

        public void IgnoreCollision(Collider collider1, Collider collider2, bool ignore = true)
        {
            Physics.IgnoreCollision(collider1, collider2, ignore);
        }

        public void IgnoreLayerCollision(int layer1, int layer2, bool ignore = true)
        {
            Physics.IgnoreLayerCollision(layer1, layer2, ignore);
        }
    }
}
```

### 8. 音频系统适配

#### 8.1 音频接口

```csharp
using UnityEngine;

namespace Basement.Engine
{
    /// <summary>
    /// 音频系统接口
    /// </summary>
    public interface IAudio
    {
        /// <summary>
        /// 主音量
        /// </summary>
        float MasterVolume { get; set; }

        /// <summary>
        /// 背景音乐音量
        /// </summary>
        float MusicVolume { get; set; }

        /// <summary>
        /// 音效音量
        /// </summary>
        float SfxVolume { get; set; }

        /// <summary>
        /// 播放背景音乐
        /// </summary>
        void PlayMusic(AudioClip clip, bool loop = true, float fadeDuration = 0f);

        /// <summary>
        /// 停止背景音乐
        /// </summary>
        void StopMusic(float fadeDuration = 0f);

        /// <summary>
        /// 暂停背景音乐
        /// </summary>
        void PauseMusic();

        /// <summary>
        /// 恢复背景音乐
        /// </summary>
        void ResumeMusic();

        /// <summary>
        /// 播放音效
        /// </summary>
        void PlaySfx(AudioClip clip, Vector3 position = default, float volume = 1f);

        /// <summary>
        /// 播放3D音效
        /// </summary>
        void Play3DSfx(AudioClip clip, Vector3 position, float volume = 1f, float spatialBlend = 1f);

        /// <summary>
        /// 停止所有音效
        /// </summary>
        void StopAllSfx();
    }
}
```

#### 8.2 Unity音频实现

```csharp
using System.Collections;
using UnityEngine;

namespace Basement.Engine
{
    /// <summary>
    /// Unity音频系统实现
    /// </summary>
    public class UnityAudio : MonoBehaviour, IAudio
    {
        private AudioSource _musicSource;
        private AudioSource _sfxSource;
        private float _masterVolume = 1f;
        private float _musicVolume = 1f;
        private float _sfxVolume = 1f;

        public float MasterVolume
        {
            get => _masterVolume;
            set
            {
                _masterVolume = Mathf.Clamp01(value);
                UpdateVolumes();
            }
        }

        public float MusicVolume
        {
            get => _musicVolume;
            set
            {
                _musicVolume = Mathf.Clamp01(value);
                UpdateVolumes();
            }
        }

        public float SfxVolume
        {
            get => _sfxVolume;
            set
            {
                _sfxVolume = Mathf.Clamp01(value);
                UpdateVolumes();
            }
        }

        private void Awake()
        {
            // 创建音乐源
            GameObject musicObj = new GameObject("[MusicSource]");
            musicObj.transform.SetParent(transform);
            _musicSource = musicObj.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;

            // 创建音效源
            GameObject sfxObj = new GameObject("[SfxSource]");
            sfxObj.transform.SetParent(transform);
            _sfxSource = sfxObj.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;

            UpdateVolumes();
        }

        public void PlayMusic(AudioClip clip, bool loop = true, float fadeDuration = 0f)
        {
            if (clip == null) return;

            if (fadeDuration > 0f)
            {
                StartCoroutine(FadeInMusic(clip, loop, fadeDuration));
            }
            else
            {
                _musicSource.clip = clip;
                _musicSource.loop = loop;
                _musicSource.Play();
            }
        }

        public void StopMusic(float fadeDuration = 0f)
        {
            if (fadeDuration > 0f)
            {
                StartCoroutine(FadeOutMusic(fadeDuration));
            }
            else
            {
                _musicSource.Stop();
            }
        }

        public void PauseMusic()
        {
            _musicSource.Pause();
        }

        public void ResumeMusic()
        {
            _musicSource.UnPause();
        }

        public void PlaySfx(AudioClip clip, Vector3 position = default, float volume = 1f)
        {
            if (clip == null) return;

            _sfxSource.PlayOneShot(clip, volume * _sfxVolume * _masterVolume);
        }

        public void Play3DSfx(AudioClip clip, Vector3 position, float volume = 1f, float spatialBlend = 1f)
        {
            if (clip == null) return;

            GameObject sfxObj = new GameObject("[3DSfx]");
            sfxObj.transform.position = position;
            AudioSource source = sfxObj.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume * _sfxVolume * _masterVolume;
            source.spatialBlend = spatialBlend;
            source.Play();

            Destroy(sfxObj, clip.length + 0.1f);
        }

        public void StopAllSfx()
        {
            _sfxSource.Stop();
        }

        private void UpdateVolumes()
        {
            _musicSource.volume = _musicVolume * _masterVolume;
            _sfxSource.volume = _sfxVolume * _masterVolume;
        }

        private IEnumerator FadeInMusic(AudioClip clip, bool loop, float duration)
        {
            float startVolume = 0f;
            float targetVolume = _musicVolume * _masterVolume;

            _musicSource.clip = clip;
            _musicSource.loop = loop;
            _musicSource.volume = startVolume;
            _musicSource.Play();

            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                _musicSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsedTime / duration);
                yield return null;
            }

            _musicSource.volume = targetVolume;
        }

        private IEnumerator FadeOutMusic(float duration)
        {
            float startVolume = _musicSource.volume;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                _musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsedTime / duration);
                yield return null;
            }

            _musicSource.Stop();
            _musicSource.volume = _musicVolume * _masterVolume;
        }
    }
}
```

### 9. 引擎适配器管理器

```csharp
using UnityEngine;

namespace Basement.Engine
{
    /// <summary>
    /// 引擎适配器管理器
    /// 负责管理所有引擎适配器的实例化和生命周期
    /// </summary>
    public class EngineAdapterManager : MonoBehaviour
    {
        private static EngineAdapterManager _instance;

        public static EngineAdapterManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject obj = new GameObject("[EngineAdapterManager]");
                    _instance = obj.AddComponent<EngineAdapterManager>();
                    DontDestroyOnLoad(obj);
                }
                return _instance;
            }
        }

        public IEngineCore EngineCore { get; private set; }
        public ITime Time { get; private set; }
        public IInput Input { get; private set; }
        public IPhysics Physics { get; private set; }
        public IAudio Audio { get; private set; }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAdapters();
        }

        private void InitializeAdapters()
        {
            // 初始化引擎核心
            EngineCore = new UnityEngineCore();
            EngineCore.Initialize();

            // 初始化时间系统
            Time = new UnityTime();

            // 初始化输入系统
            Input = new UnityInput();

            // 初始化物理系统
            Physics = new UnityPhysics();

            // 初始化音频系统
            Audio = gameObject.AddComponent<UnityAudio>();

            Debug.Log("引擎适配器初始化完成");
        }

        private void Update()
        {
            EngineCore?.Update(Time.DeltaTime);
        }

        private void OnDestroy()
        {
            EngineCore?.Cleanup();
        }
    }
}
```

## 使用说明

### 1. 基础使用

```csharp
using Basement.Engine;

public class PlayerController : MonoBehaviour
{
    private ITime _time;
    private IInput _input;
    private ITransform _transform;

    private void Start()
    {
        // 获取引擎适配器实例
        var adapterManager = EngineAdapterManager.Instance;

        _time = adapterManager.Time;
        _input = adapterManager.Input;
        _transform = new UnityTransform(transform);
    }

    private void Update()
    {
        // 使用适配器接口
        float deltaTime = _time.DeltaTime;

        if (_input.GetKey(KeyCode.W))
        {
            _transform.Translate(Vector3.forward * deltaTime * 5f);
        }

        if (_input.GetKeyDown(KeyCode.Space))
        {
            _time.Pause();
        }
    }
}
```

### 2. 依赖注入使用

```csharp
using Basement.Engine;

public class EnemyAI : MonoBehaviour
{
    private ITime _time;
    private IPhysics _physics;

    public void Initialize(ITime time, IPhysics physics)
    {
        _time = time;
        _physics = physics;
    }

    public bool CanSeePlayer(Vector3 enemyPosition, Vector3 playerPosition)
    {
        Vector3 direction = (playerPosition - enemyPosition).normalized;
        return _physics.Raycast(enemyPosition, direction, out RaycastHit hit, 10f);
    }
}
```

### 3. 测试Mock使用

```csharp
using Basement.Engine;

// 测试用的Mock实现
public class MockTime : ITime
{
    public float Time { get; set; } = 0f;
    public float DeltaTime { get; set; } = 0.016f;
    public float UnscaledDeltaTime => DeltaTime;
    public float TimeScale { get; set; } = 1f;
    public float FixedDeltaTime => 0.02f;
    public int FrameCount { get; set; } = 0;

    public void Pause() => TimeScale = 0f;
    public void Resume() => TimeScale = 1f;
    public void SetTimeScale(float scale) => TimeScale = scale;
}

// 单元测试
[Test]
public void TestPlayerMovement()
{
    // 创建Mock对象
    var mockTime = new MockTime();
    var mockInput = new MockInput();

    // 注入Mock对象
    var player = new PlayerController();
    player.Initialize(mockTime, mockInput);

    // 测试逻辑
    mockInput.SimulateKeyPress(KeyCode.W);
    player.Update();

    Assert.AreEqual(expectedPosition, player.Position);
}
```

## 性能优化策略

### 1. 对象池优化

```csharp
// 对高频使用的适配器对象使用对象池
public class TransformPool
{
    private Stack<UnityTransform> _pool = new Stack<UnityTransform>();

    public UnityTransform Get(Transform transform)
    {
        if (_pool.Count > 0)
        {
            var adapter = _pool.Pop();
            adapter.SetTransform(transform);
            return adapter;
        }
        return new UnityTransform(transform);
    }

    public void Release(UnityTransform adapter)
    {
        _pool.Push(adapter);
    }
}
```

### 2. 缓存优化

```csharp
// 缓存常用接口引用
public class CachedEngineAdapter
{
    private static ITime _cachedTime;
    private static IInput _cachedInput;

    public static ITime Time
    {
        get
        {
            if (_cachedTime == null)
            {
                _cachedTime = EngineAdapterManager.Instance.Time;
            }
            return _cachedTime;
        }
    }

    public static IInput Input
    {
        get
        {
            if (_cachedInput == null)
            {
                _cachedInput = EngineAdapterManager.Instance.Input;
            }
            return _cachedInput;
        }
    }
}
```

### 3. 批处理优化

```csharp
// 批量处理物理检测
public class PhysicsBatchProcessor
{
    private List<RaycastCommand> _commands = new List<RaycastCommand>();
    private List<RaycastHit> _results = new List<RaycastHit>();

    public void AddRaycast(RaycastCommand command)
    {
        _commands.Add(command);
    }

    public void ExecuteBatch()
    {
        _results.Clear();
        _results.Capacity = _commands.Count;

        RaycastHit[] hits = new RaycastHit[_commands.Count];
        int hitCount = RaycastHitCommand.BatchExecute(_commands.ToArray(), hits);

        for (int i = 0; i < hitCount; i++)
        {
            _results.Add(hits[i]);
        }

        _commands.Clear();
    }
}
```

## 与Unity引擎的结合点

### 1. 生命周期管理

```csharp
public class EngineAdapterInitializer : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeEngineAdapters()
    {
        // 在场景加载前初始化引擎适配器
        EngineAdapterManager.Instance;
    }
}
```

### 2. 编辑器集成

```csharp
#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
public class EngineAdapterEditor
{
    static EngineAdapterEditor()
    {
        // 在编辑器启动时初始化
        if (!EditorApplication.isPlaying)
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            EngineAdapterManager.Instance;
        }
    }
}
#endif
```

### 3. 资源管理集成

```csharp
using Basement.ResourceManagement;

public class EngineResourceAdapter
{
    private IResourceLoader _resourceLoader;

    public void LoadEngineAssets()
    {
        // 使用资源管理器加载引擎资源
        _resourceLoader = ResourceLoader.Instance;

        // 异步加载音频资源
        _resourceLoader.LoadAsync<AudioClip>("Audio/Music/BGM", (clip) =>
        {
            EngineAdapterManager.Instance.Audio.PlayMusic(clip);
        });
    }
}
```

## 总结

引擎适配模块通过封装Unity引擎核心API，提供了标准化的接口库，实现了项目与Unity引擎的解耦。该模块具有以下优势：

1. **解耦性**：将Unity引擎API与业务逻辑分离，降低耦合度
2. **可测试性**：通过接口抽象，便于单元测试和Mock
3. **可扩展性**：支持自定义适配器实现，便于功能扩展
4. **可移植性**：便于未来替换为其他游戏引擎
5. **统一性**：提供标准化的访问接口，简化开发流程

通过使用引擎适配模块，项目可以更好地应对技术栈变化，提高代码质量和可维护性。
