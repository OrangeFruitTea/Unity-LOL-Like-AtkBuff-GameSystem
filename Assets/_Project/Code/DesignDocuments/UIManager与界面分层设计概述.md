# UIManager 与界面分层 — 设计概述

本文档描述 **`Core.UI`** 命名空间下 **运行时 UI 装配管线**的职责、数据结构与典型调用关系，作为工程内 **UGUI** 与 **ECS 观测层**之间的设计说明。**不替代**毕设论文章节或具体界面美术方案；与相位（主菜单 / 局内 / 结算）相关的流程见《毕设演示-端到端场景与界面流转.md》等姊妹文档。

---

## 1. 设计目标

| 目标 | 做法摘要 |
|------|----------|
| **统一层级与遮盖顺序** | 按 **`UILayerType`** 分得若干 **全屏 Canvas 根**，`sortingOrder` 与枚举整型对齐，避免各 Prefab 私拉 Canvas 争抢排序 |
| **统一分辨率策略** | 每层根上集中挂 **`CanvasScaler`（ScaleWithScreenSize，1920×1080）**，Prefab 只做相对锚点布局 |
| **程序化生成而非场景硬摆** | 通过 **`Resources.Load`** + **`UIElement` 描述符**声明「路径、层、锚点、偏移、尺寸」，由 **`UIManager.GenerateUI`** 实例化 |
| **可销毁、可整块清空** | 用 **`Guid`** 登记实例，支持按实例销毁、按层清空、按层+锚点清空 |
| **与 ECS 解耦观测** | 需要读实体数据的控件实现 **`IEntityBridgeBindable`**，由 **`BindEcsBridgeConsumers`** 注入 **`EcsEntityBridge`**，**禁止**控件内直接耦合 `EcsWorld` |

---

## 2. 代码位置与命名空间

| 类型 | 路径 |
|------|------|
| **`UIManager`**、`UILayerContainer` | `_Project/Code/Scripts/View/UI/UIManager.cs` |
| **`UIElement`**、`IEntityBridgeBindable` | `_Project/Code/Scripts/View/UI/UIElement.cs` |
| **`UILayerType`**、`UIAnchorType` | `_Project/Code/Scripts/View/UI/UIAnchorType.cs` |
| 单例基类 | **`Basement.Utils.Singleton<T>`**（`UIManager` 继承之，需在场景中存在宿主 **GameObject**） |

**Widget 示例**（实现桥接）：`View/UI/Widgets/HUD/PlayerStatement/DetailStatItemManager.cs`（`Widgets.PlayerStatement` 命名空间）。

---

## 3. 全局根与分层容器

- **`[UIRoot]`**  
  `UIManager.Awake` 时 **`GameObject.Find("[UIRoot]")`**；不存在则 **创建**并 **`DontDestroyOnLoad`**。其 `RectTransform` 拉伸为全屏，**Layer = UI**。

- **`UILayerType`（与 `Canvas.sortingOrder` 一致）**  
  `Background = 10` → `MainUI = 20` → `HUD = 30` → `Popup = 40` → `TopTip = 50`。  
  数值越大，越靠近屏幕前层（越晚绘制、越易盖住下层）。

- **`CreateLayer`**（懒创建）  
  在 `[UIRoot]` 下创建 `Layer_{UILayerType}`，挂载：  
  **`Canvas`（ScreenSpaceOverlay、`overrideSorting = true`、`sortingOrder = (int)layerType`）**、**`CanvasScaler`**、**`GraphicRaycaster`**。  
  **`UILayerContainer`** 仅持有该层根 `GameObject` 与 `Canvas`。

- **实例挂载点**  
  Prefab 实例 **直接作为该层根的子物体**，**不再**为每种锚点建中间空节点；锚点只作用在实例根 **`RectTransform`** 上（见 §5）。

---

## 4. `UIElement`：一次生成的描述符

**`UIElement`** 为普通 C# 类（非 `MonoBehaviour`），字段含义：

| 字段 | 含义 |
|------|------|
| **`PrefabPath`** | 传入 **`Resources.Load<GameObject>`** 的路径（**无扩展名**），例如 `UI/Widgets/Game/Statement/DetailStatement` |
| **`LayerType`** | 目标 **`UILayerType`** |
| **`AnchorType`** | 在层内全屏区域上的锚点策略（见 §5） |
| **`Position`** | 相对锚点的 **`anchoredPosition` 偏移** |
| **`Size`** | 非 `Vector2.zero` 时写入根 **`sizeDelta`**；为零则保留 Prefab 默认尺寸 |
| **`DontDestroyOnLoad`** | 为真时对**该实例**再调用 `DontDestroyOnLoad`（与 `[UIRoot]` 的 DDoL 独立） |
| **`IsGenerated` / `UiInstanceId`** | 生成成功后由管理器置位；`UiInstanceId` 为新建 **Guid**，用于后续销毁 |

**`SetDefaults()`**：`MainUI` + `Center`，其余清零。

**注意**：`GenerateUI` 若成功会把传入的 **`element.IsGenerated`** 置为 **true** 并分配 **新 Guid**；**同一份 `UIElement` 对象不宜在未 `SetDefaults` 重置的情况下重复生成**。批量界面应 **每次 new `UIElement()`** 或封装工厂方法。

---

## 5. `UIAnchorType` 与 `SetAnchorRectTransform`

管理器内根据锚点类型设置实例根 **`RectTransform`** 的 **anchorMin/Max、pivot、anchoredPosition**（部分角点使用 **`Screen.width` / `Screen.height`** 参与偏移，属当前实现细节；换分辨率时以 **CanvasScaler** 与层根全屏拉伸为基准）。

枚举值：**`Center`**，**`TopLeft` / `TopCenter` / `TopRight`**，**`MiddleLeft` / `MiddleRight`**，**`BottomLeft` / `BottomCenter` / `BottomRight`**。

---

## 6. 核心 API 流程

### 6.1 `GenerateUI(UIElement element)`

1. 若已生成或路径为空 → 失败返回。  
2. **`Resources.Load`** 预制。  
3. 取或 **`CreateLayer`** 目标层。  
4. **`Instantiate`** 到层根下，实例名带 **8 位 Guid** 后缀。  
5. 取根 **`RectTransform`**：应用锚点、**`Position`**、可选 **`Size`**。  
6. 可选 **`DontDestroyOnLoad(uiInstance)`**。  
7. 登记 **`_activeUiInstances`**、**`_uiInstanceLayers`**、**`_uiInstanceAnchors`**，**`element.UiInstanceId`** 写入。

### 6.2 销毁与清理

- **`DestroyUI(Guid uiInstanceId)`**：销毁 GameObject 并移除三处字典项。  
- **`ClearLayer(UILayerType)`**：销毁该层所有已登记实例。  
- **`ClearLayerAnchor(UILayerType, UIAnchorType)`**：按层+锚点子集销毁。  
- **`GetLayerUIInstances(UILayerType)`**：查询该层当前实例列表。  
- **`Cleanup()`**：清空全部实例并销毁 **`[UIRoot]`** 与层缓存（适合进程退出或整包重置）。

### 6.3 业务封装示例：`TrySpawnDetailStatement`

**`UIManager`** 内声明常量 **`DetailStatementResourcePath = "UI/Widgets/Game/Statement/DetailStatement"`**（对应工程内 **`Assets/_Project/Resources/...`** 下的 Prefab）。  
**`TrySpawnDetailStatement(...)`** 内部 **new `UIElement` → `SetDefaults` → 覆写字段 → `GenerateUI`**，并 **out** 实例 **Guid** 与根 **GameObject**。  

其它界面可照此模式增加 **`TrySpawnXxx`** 或在外部拼 **`UIElement`** 后直接调用 **`GenerateUI`**。

---

## 7. ECS 桥接：`IEntityBridgeBindable` 与 `BindEcsBridgeConsumers`

**问题**：Unity 的 **`GetComponentsInChildren<T>`** 不能按 **接口** `T` 过滤。

**做法**：对 UI 根执行 **`GetComponentsInChildren<MonoBehaviour>(true)`**，对实现了 **`IEntityBridgeBindable`** 的组件调用 **`Bind(EcsEntityBridge bridge)`**。

**契约**：**`Bind` 内只读桥接提供的组件（如 `EntityDataComponent`）刷新表现**，**不写回**玩法规则；若需再次刷新，由业务在数据变更后调用控件上的 **`Refresh`** 类方法（如 **`DetailStatItemManager.RefreshFromBridge`**）。

**典型实现**：**`DetailStatItemManager`** 在 **`Bind`** 中缓存 **`EcsEntityBridge`**，从 **`EntityDataComponent`** 取 **`EntityBaseDataCore`** 各项属性并写入 **TMP**。

---

## 8. 与毕设/论文文档的关系

- 学位论文 **「交互表现与用户界面」** 成章对 **`UIManager` / `UIElement` / 分层 / Guid / 桥接** 有展开叙述，可与本文 **交叉引用**。  
- **`工程模块与命名空间-现行实现总览.md`** 可在后续修订中于命名空间表增加一行 **`Core.UI`** 指向本文与 `View/UI/` 目录。

---

## 9. 扩展与风险点（简记）

- **Resources 路径**：错路径会导致 **`GenerateUI` 失败**；建议路径常量化（如 `DetailStatementResourcePath`）。  
- **单例生命周期**：**`UIManager`** 依赖场景内 **`Singleton<UIManager>`** 宿主；切场景时需确认 **DontDestroy** 策略与 **`Cleanup`** 调用约定。  
- **多 Canvas 与射线**：一层一 **GraphicRaycaster**；复杂弹窗若需独立输入栈，可结合 **`EventSystem`** 与 Input 文档另述。  
- **锚点与 Safe Area**：当前实现未内置刘海屏安全区；若需可在外层再包一层 Layout 或在 Widget 内处理。

---

*文档版本：1.0 — 依据 `UIManager.cs`、`UIElement.cs`、`UIAnchorType.cs` 及 `DetailStatItemManager.cs` 整理。*
