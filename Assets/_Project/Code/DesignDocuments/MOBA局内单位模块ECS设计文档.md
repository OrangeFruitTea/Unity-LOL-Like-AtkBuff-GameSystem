# MOBA 局内单位模块（ECS Component）设计文档

| 项 | 内容 |
|----|------|
| 文档版本 | 1.6 |
| 适用引擎 | Unity 2022 |
| 文档类型 | 模块设计（MOBA 局内单位 · ECS） |
| 关联实现 | `Core.ECS`、`EcsWorld`、`EcsEntity`、`IEcsComponent`；现有 **`EntitySpawnSystem`** 已为 `EntityBase` 创建 ECS 并挂载 **`EntityDataComponent`**（属性真理源） |
| 核心决策 | **伤血**：`EntityDataComponent` + Buff/`Impact`；**战斗会话黑板** **`CombatBoardLiteComponent`**：`AttackTarget`（主攻/瞄准）、`ThreatTarget`（仇恨）、承伤来源、击杀者与最多 3 名助攻占位（§8.1）；**生死**：`UnitVitalitySystem`；野怪：`JungleAiSystem`；塔：**`TowerCombatCycleSystem`**；NavMesh：**§8.3**。旧名 **`AiBlackboardLite*`** **已废止** |

---

## 1. 引言

### 1.1 目的

界定局内 **非英雄** 或可独立刷新的单位（防御塔、野区单位、兵线单位等）在 ECS 中的：

- **原型（Archetype）** 如何与 **已有基础组件**拆分；
- **哪些字段适合放在 Component**，哪些必须留在配置表或服务；
- **系统（System）** 的职责边界与推荐更新顺序。

便于单人或小团队在毕设体量下 **逐项实现**：先兵种 id + 空组件占位，再接 AI / 兵线波次。

### 1.2 读者对象

- Gameplay / 客户端逻辑；
- 与关卡、兵线、野区策划对接的研发。

### 1.3 术语

| 术语 | 定义 |
|------|------|
| 单位原型（Archetype） | 逻辑上的「塔 / 近战兵 / 野怪」类型，用 **枚举 + 配置 id** 区分，而非仅 Prefab 名。 |
| 阵营（Faction / Team） | 蓝/红/中立等，用于目标筛选、仇恨、伤害免疫规则。 |
| 租赁（Leash） | 野怪离开出生区过远则脱战回位；由 **中心点 + 半径** 描述。 |
| 波次（Wave） | 兵线生成批次与类型组合，由 **波次 id** 与 **时间线** 驱动（本文件只定义单位侧需携带什么）。 |

---

## 2. 设计原则

1. **属性不重复**：生命、攻击、移速等 **一律走 `EntityDataComponent`**（及后续 Buff 修改）；单位专用组件只存 **身份、规则参数、状态机用的轻量字段**。
2. **组件纯数据**：`struct` 实现 `IEcsComponent`，`InitializeDefaults()` 给合理默认；**不在组件内**写 `Update`、不持有 `UnityEngine.Object` 引用（避免与 ECS 数据布局纠缠；场景引用走 `EntityBase` / 侧表）。
3. **塔 / 兵 / 野怪分组件**：共享部分用 **共享组件**（阵营、原型标签）；差异部分用 **可选组件**（仅塔有 `TowerModuleComponent`），查询时 `HasComponent<T>` 分支。
4. **与场景对象关系**：每个局内单位仍有 **`EntityBase` + `BoundEcsEntity`**；ECS 为 **逻辑与查询**，Transform 移动可在 System 中写 `entityBridge` 或 **统一 MovementFacade**（实现阶段定稿）。
5. **可扩展**：新增野怪变种时优先 **加枚举 / 配置 id**，而不是复制整类脚本。

---

## 3. 单位分层：共享层与专精层

```text
                    ┌─────────────────────────────┐
                    │ EntityDataComponent （属性） │
                    └─────────────┬───────────────┘
                                  │
       ┌──────────────────────────┼──────────────────────────┐
       ▼                          ▼                          ▼
FactionComponent            UnitArchetypeComponent
CombatBoardLiteComponent   （当前目标/承伤源；§8.1）
TowerModuleComponent        TowerCombatCycleComponent   ← §5（仅时间轴阶段）
                            LaneMinionModuleComponent
                            JungleCreepModuleComponent
```

下文 **组件名为建议名**，实现时可微调命名空间（如 `Gameplay.Units.Ecs`），但语义建议保留。

---

## 4. 共享组件（建议所有局内 combat 单位）

### 4.1 `FactionComponent`（阵营）

| 字段（示例） | 类型 | 说明 |
|--------------|------|------|
| `TeamId` | byte / int | `0`=中立，`1`=蓝，`2`=红；与项目枚举对齐。 |

**用途**：寻敌、兵线互打、防御塔普攻目标筛选。

### 4.2 `UnitArchetypeComponent`（单位原型）

| 字段（示例） | 类型 | 说明 |
|--------------|------|------|
| `Archetype` | enum | 见 §4.3 |
| `ConfigId` | int | **静态表**主键——塔型、野怪组、兵种具体配置（伤害公式、赏金权重等可走表）。 |

**说明**：Prefab 艺术与 **`ConfigId`** 解耦——同 Prefab 可配不同表里行（测试友好）。

### 4.3 原型枚举（建议）

```csharp
public enum UnitArchetype : byte
{
    Hero = 0,          // 可选；若英雄不用此枚举可不注册
    LaneMinion,        // 兵线
    JungleMonster,     // 野怪
    Tower,             // 防御塔（含高地塔、门牙等仍以 ConfigId 区分）
    EpicMonster,       // 可选：大龙等
}
```

---

## 5. 防御塔专用：`TowerModuleComponent` 与攻防状态机（预警 / 锁定 / 攻击 / 冷却）

### 5.1 行为节拍（与设计要点）

防御塔普攻建议拆成 **四段循环**（可依产品删减，但字段上预留语义）：

| 阶段 | 玩家侧感受 | 逻辑要点 |
|------|------------|----------|
| **预警（Wind-up / Telegraph）** | 地面标线、激光瞄准、音效起 | **尚未扣血**；已选好目标并开始计时；可被用于「走出射程则打断」的规则 |
| **锁定（Lock）** | 目标圈高亮 | 本段内 **目标 id 固定**（或仅允许在规则下切换，如目标死亡） |
| **攻击（Strike / Hit frame）** | 弹道/落点 | **单帧或极短窗口**内产生 **伤害事实**（见 §5.4 与现有 Impact 管线对接） |
| **冷却（Cooldown）** | 塔「哑火」间隔 | 无有效目标可回到 **扫描**；有目标则进入下一轮或保持 idle，由 `TowerCombatCycleSystem` 状态决定 |

状态机在实现上宜用 **`TowerCombatPhase`** 枚举 + **阶段剩余时间或阶段结束时刻**（`float`，与 `UnityEngine.Time` 对齐即可），而非散落的 bool。

### 5.2 组件拆分建议

**配置型（常驻，偏静态表 / `TowerModuleComponent`）**

| 字段（示例） | 类型 | 说明 |
|--------------|------|------|
| `LaneSlotId` | int | 所属路与防御分区 id |
| `AggroAcquireRange` | float | 索敌球半径（可与普攻射程 **`EntityBaseData.AtkDistance`** 一致或略大） |
| `TargetingMode` | byte / enum | 兵优先 / 英雄优先 / 仇恨规则简版 |
| `PlatingStacks` | ushort | 镀层（可选） |
| `AggroHeroHint` | bool | 强塔下优先英雄等 |
| **`WarningDuration`** | float | **预警段时长（秒）** |
| **`LockDuration`** | float | **锁定段时长（秒）**（可与预警合并为同一「前摇」，则其一为 0） |
| **`StrikeHitDelay`** | float | 从锁定结束到 **`Impact`** 落地的延迟（若为 0 则锁定末尾即造成伤害） |
| **`AttackCooldown`** | float | **完整一轮普攻后的冷却**，亦可由 **`EntityBaseDataCore.AtkSpeed`** 推导（实现二选一并在表内注明） |

**运行时状态（建议独立 `TowerCombatCycleComponent`，仅时间轴；目标 id 不写在此，见 §8.1）**

| 字段（示例） | 类型 | 说明 |
|--------------|------|------|
| `Phase` | `TowerCombatPhase` | `IdleScan` \| `Warning` \| `Lock` \| `StrikePending` \| `Cooldown` \| `Interrupted` |
| `PhaseEndsAt` 或 `PhaseTimeRemaining` | float | 与 `Time.time` 比较或每帧递减 |
| `PendingImpactAt`（可选） | float | Strike 与 Impact 入队不同时的排程时刻 |

**已移除**：不在此组件重复 **`AttackTargetEntityId`**；**攻击 / 瞄准唯一读 `CombatBoardLiteComponent.AttackTargetEntityId`**（与同实体上的 `TowerCombatCycle` 共存）。

`TowerCombatPhase` 枚举值与是否合并 **Warning+Lock** 由实现定稿，但 **文档层面**保留 **预警 / 锁定 / 攻击帧 / 冷却** 四类语义便于策划填表。

**不配移动组件**：塔仍无路径/velocity；**Transform** 只做旋转炮台；**射线/炮口指向** 由表现层读 **`CombatBoardLite`** + `EntityEcsLinkRegistry` 解析 **目标世界位置**。

### 5.3 系统职责：`TowerCombatCycleSystem`（建议名）

单机顺序（每个 tower 实例每帧一次或按管理器批处理）：

1. **`IdleScan`**：`AggroAcquireRange` 内按 `TargetingMode` 选目标 → **写入同实体 `CombatBoardLiteComponent.AttackTargetEntityId`**（`0`=无目标），转 **Warning**，设 `PhaseEndsAt`，并发布表现事件。
2. **`Warning`**：到时转 **Lock**（或直接进入 **StrikePending**）。
3. **`Lock`**：到时转 **StrikePending**，并安排 **`PendingImpactAt = now + StrikeHitDelay`**。
4. **`StrikePending`**：当 `Time.time >= PendingImpactAt`：按 **黑板**上 id **创建 Impact**（§5.4），然后转 **Cooldown**，并设 `PhaseEndsAt = now + AttackCooldown`（或由表配置）。
5. **`Cooldown`**：到时若黑板无有效目标 → **`IdleScan`**；若仍合法可依产品回到 **Warning** / **IdleScan**。

**打断**：目标无效时 **清黑板 `AttackTargetEntityId`**，`Phase`→`IdleScan` 等；**不得**在塔周期内另设第二份目标 id。

### 5.4 与现有战斗代码的对接（核心）

| 现有模块 | 塔攻防循环中的用法 |
|----------|---------------------|
| **`EntityDataComponent`** | 普攻伤害基数、**`AtkDistance` 射程**、**`AtkSpeed`**（若用于换算 `AttackCooldown`）；塔无 MP 需求可忽略蓝量。 |
| **`ImpactEventComponent` + `ImpactValueComponent` + `ImpactSystem`** | **Strike** 帧：由 `TowerCombatCycleSystem`/`TowerStrikeHelper` 建事件；**`Target`** 必须来自 **`CombatBoardLite.AttackTargetEntityId`**（与瞄准线一致）；`BaseValue` 来自塔配置或 `EntityData`。 |
| **`SkillCastPipelineSystem`** | **非必须**：塔无复杂技能段时可不走 `SkillDefinition`；若要与技能表统一可包成单段技能 + 定时 Batch（成本高）。默认：**塔独立节拍 + Strike 接轨 Impact**。 |
| **`CombatBoardLiteComponent`** | **必选**：非玩家 combat 共享；**索敌/换手/清目标** 只更新 `AttackTargetEntityId`。**`TowerCombatCycle` 不写平行目标 id**。 |

**摘要**：**时间轴** → `TowerCombatCycle`；**打谁** → **黑板** → **Impact**。

### 5.5 表现与 ECS 数据的边界

- **瞄准线 / 地面 decal / 炮台指向**：**目标 id** 与 **塔顶事件** 同源——读 **`CombatBoardLite`** + `phase`（可选用 `GameEventBus`，`targetEntityId`=黑板 id）。
- **伤害数字**：仍可由 Impact 链路或 UI 监听 **伤血事件**。

### 5.6 与网络同步（提要）

- **权威端**维护 `TowerCombatPhase` 与 `PhaseEndsAt`（或序列化剩余时间）；客户端 **仅插值表现**（预警条、激光指向）。
- **命中帧**必须在 **服务端/主机** 与 **`Impact` 入队** 一致，避免「客户端先看到掉血而权威未确认」。

---

## 6. 兵线小兵专用：`LaneMinionModuleComponent`

| 字段（示例） | 类型 | 说明 |
|--------------|------|------|
| `WaveSpawnId` | int | **本波生成批次**，用于经济与统计。 |
| `LaneIndex` | byte | 上/中/下路等 |
| `PathwayId` | int | **路径资源 id**——指向 Waypoint 列表或样条线配置（由关卡或 ScriptableObject 提供）。 |
| `WaypointIndex` | ushort | **当前目标点序号** |
| `RepathReason` | byte | **可选**：被推挤、被封路时重置路径用 |

**系统职责**：沿 `WaypointIndex` 前进；**移动与索敌、NavMesh 分工** 见 **§8.3**；合并入口见 §10 表。

---

## 7. 野怪专用：`JungleCreepModuleComponent`

| 字段（示例） | 类型 | 说明 |
|--------------|------|------|
| `CampId` | int | **野区营地 id**，同营地野怪共用脱战逻辑。 |
| `LeashCenter` | Vector3（或仅存表 id） | 租赁圆心；亦可仅存 **偏移 id**，由 **`Camp`** 配置表读出世界坐标 |
| `LeashRadius` | float | 超出则脱战、回血重置（经典 MOBA）。 |
| `CurrentState` | byte / enum | Idle / Pursue / Returning |
| `AnchorSlotIndex` | byte | **大营地**内第几只（大龙坑多槽位） |

**系统职责**：**`JungleAiSystem`** 统一处理：**租赁（超出 `LeashRadius` 脱战回巢）**、`Idle`/`Pursue`/`Returning` **追逐与归位**。不再单独拆分 **`JungleLeashSystem`**。击杀与经济见《MOBA局内经济系统设计文档》。

---

## 8. `CombatBoardLiteComponent` 与移动/索敌分层（非玩家、`NavMesh` 协作）

### 8.1 `CombatBoardLiteComponent`：**毕设精简**黑板字段与职责

原名 **`AiBlackboardLiteComponent`** 已改名为 **`CombatBoardLiteComponent`**，语义从「仅有目标 id」扩展为「单局战斗中需随时读写的 **目标/仇恨/承伤/kill/kill-assist** 快照」。**仍为纯 `long`（逻辑实体 Id）**，`0`=无效；**不写** Unity 引用。

#### 字段表（与设计对齐）

| 字段 | 说明 | **典型写入方（示例）** |
|------|------|------------------------|
| `AttackTargetEntityId` | **主攻/普攻与瞄准**；与 **`Impact`、`TowerCombat Strike`** 必读一致 | **`TowerCombatCycleSystem` IdleScan**、兵线/野怪 Acquire |
| `ThreatTargetEntityId` | **仇恨首要对象**（单槽）；MVP 常与攻击目标相同 | AI；被 **嘲讽/Buff** 时可与 `Attack*` 分叉 |
| `LastDamageFromEntityId` | **最近一次对自己造成伤害的单位**（还击链、播报） | **`ImpactSystem`** 或受击管线 |
| `KillerEntityId` | **本实体被击倒时的击杀者**；存活中为 `0` | **`UnitVitalitySystem`**（死亡阈值达成时） |
| `AssistEntityId0` … `AssistEntityId2` | **至多 3 名助攻占位**（毕设足够展示经济瓜分） | **击杀结算**按规则填入；未用则为 `0` |

**与非玩家移动的边界**：走位、**NavMesh** 仍不按本组件解析；黑板 **只解决「谁和谁发生了战斗语义」**。助攻满员后若仍需更多名次，再在 **会话 DTO / 服务端**扩展，ECS 仍为轻表。

### 8.2 主攻目标与 Strike / **`TowerCombatCycle`** 的一致性

| 条目 | 约定 |
|------|------|
| **攻防主攻** | **`AttackTargetEntityId`**：**塔射线、小兵/野怪普攻、Impact Strike** 均读该字段（`0`=无）。 |
| **防御塔节拍** | **`TowerCombatCycleComponent`** **只存相位与时间**，在 **IdleScan/打断** 时写入 **`AttackTargetEntityId`**，**不写**并行栏位。 |
| **Strike / Impact** | **`Target`** 与表现层瞄准 **同源**。 |
| **仇恨** | **`ThreatTargetEntityId`** 可与主攻相同；需要 **单体嘲讽** 等与 AI 分叉时再独立维护。 |

**生成体挂载**：塔、兵线、野怪等 combat 实体均应挂载 **`CombatBoardLiteComponent`**（与 **`FactionComponent`** 并列）；**不得**再在别处 invent 一套「平行 currentTarget」。玩家可对齐同一套 **`TryPickHostileTarget`**（另文）。

### 8.3 非玩家单位：**移动（含 `NavMesh`）** 与 **索敌** 分层

本节描述职责分层。

```text
  [ 逻辑 ECS ]          [ 桥梁 / Mono ]              [ Unity 引擎 ]
  Blackboard.Target  →   「谁要打」                
  LaneMinion Wp Idx  →   Destination 策略   →       NavMeshAgent.SetDestination(...)
  Jungle State/Leash →   速度/Stopped 裁剪  →       (同上或 Transform 特例)
                         NavMesh.SamplePosition / obstacles
```

| 层级 | 职责 | 说明 |
|------|------|------|
| **索敌（Acquire）** | 在射程/优先级规则内选 **敌方 ECS id**，写入 **`CombatBoardLite.AttackTargetEntityId`**。塔由 **`TowerCombatCycleSystem`（IdleScan）**；兵线/野怪由 **`JungleAiSystem`**、兵线行为入口等调用 **共用/helper `TrySelectTarget(...)`**。 |
| **移动（Motor）** | **把单位送到某位置**：兵线可按 **`LaneMinionModule` 的 Waypath + WaypointIndex** 求得 **下一站世界坐标**，再 **`NavMeshAgent.SetDestination`**；野怪：追人或回巢。**ECS 仅存 path 索引/状态**，**不包含** `NavMeshAgent` 引用（挂在 `GameObject`）。 |
| **`NavMesh` 本体** | 挂在 **`EntityBase`/`GameObject`** 上；由 **`NavMotorBridge`**（或挂在 Prefab 上的薄组件）在每帧 **`LateUpdate`/`Update`**：读 ECS 输出的 **意向速度/停机 bool/目标点**，驱动 Agent。 |

**为何要这样拆**：  
- **索敌**产出 **「想打 entity A」**，与 **脚底走到哪** 正交：可能没有有效目标仍会 **沿兵线走**（黑板目标为 0）。  
- **`NavMesh` 只管「如何绕障碍到点」**，不应决定 **Faction 过滤后的攻击对象**——否则近战 AI 与引擎寻路耦合，难测、难网联机权威。

**冗余警示**：若在 **`NavMeshAgent` 上层**又写了一套「追最近敌人」且不写黑板，就会与 ECS 侧 **Acquire** **双写目标**。正确姿势：**ECS/Helper 选人 → 黑板**；Motor 只做 **位移**；近战攻击帧再读黑板发 Impact。

**毕设轻量化**：兵线也可 **不用 NavMesh**、仅 **直线插值** 沿 waypoint；结构上仍保持 **黑板=目标**、`LaneMinion`=路径指针，与未来换 **`NavMesh` 不谋而合**。

---

## 9. 生成管线与 `EntitySpawnSystem` 的衔接

现有流程：**`EntitySpawnSystem.SpawnEcsEntity`** → 默认只加 **`EntityDataComponent`**。

**扩展约定**（任选其一）：

1. **在单位专属 Spawner MonoBehaviour / Factory** 中：**CreateEntity 后**紧接着 `EcsWorld.AddComponent<TowerModuleComponent>(...)` 等；
2. **扩展 `AddBaseComponents`**：根据 **`EntityUnitKind`（Mono 上 Serialized 字段）** 分支挂载（侵入性略大）。

推荐 **外部工厂**：`TowerSpawner`、`MinionWaveSpawner`、`JungleCampSpawner` 分别负责挂载专精组件，**保持 `EntitySpawnSystem` 变薄**。

---

## 10. System 拆分建议（与 `EcsWorld` 注册顺序）

| 系统（建议名） | 更新内容 | 依赖 |
|----------------|----------|------|
| `TowerCombatCycleSystem` | 塔节拍 + **Strike 读黑板目标**→ Impact | `Tower*` + **`CombatBoardLite`** + `Faction`；§5/§8 |
| `LaneMinionMoveSystem`（或 §8.3 **MinionLaneBehavior** 合并入口） | `Waypoint`/黑板/`Nav` 分工见 §8.3 | `LaneMinion` + **`CombatBoardLite`** |
| **`JungleAiSystem`** | 野怪 AI + Leash；**普攻目标→黑板** | `JungleCreepModule` + **`CombatBoardLite`** |
| **`UnitVitalitySystem`** | **局内共用**：所有带 `EntityDataComponent`（及扩展规则）的战斗单位——统一 **生与死门槛**（如 **`CrtHp`≤0**、斩杀、复活禁止）、**派发死亡/击倒事件**，并触发经济/播音/销毁等后续（与具体兵种无关） | **全单位** |

专名说明与冗余辨析见 **§14、§15**；**黑板 + NavMesh** 见 **§8**。

**顺序**（提要）：本帧内推荐 **`TowerCombatCycleSystem`**（按需投 **`Impact` 请求**）→ **`ImpactSystem`**（结算伤害、改 `CrtHp`）→ **`UnitVitalitySystem`**（统一检测死亡门槛并派发事件）。**`JungleAiSystem`** 与伤害帧的先后由「受击是否打断脱战」等规则决定；与 **`SkillCastPipelineSystem`**（按需）对齐；塔 **默认不重入** 技能管线（§5.4）。

---

## 11. 与网络 / 存档（提要）

单机毕设：**本机 ECS 即为权威**。  
多人扩展：仅 **服务端**增改 `TowerModule`、`LaneMinion` 等分量；客户端 **表现插值**，不篡改阵营与波次 id。

---

## 12. 实施路线（建议）

| 阶段 | 内容 |
|------|------|
| MVP | `Faction` + `UnitArchetype` + **`CombatBoardLite`**（目标写源）；**`UnitVitalitySystem`**；兵线 + Waypoint 或 **§8.3** 轻量 NavMesh |
| P1 | `TowerModule` + **`TowerCombatCycleComponent`**（§5）+ **`TowerCombatCycleSystem`**；Strike 帧对接 **`ImpactSystem`**（索敌与技能管线择一：§5.4） |
| P2 | 镀层、大龙、多兵线类型；与经济 Profile 对齐 |

---

## 13. 风险与规避

| 风险 | 规避 |
|------|------|
| 组件里塞引用类型导致 GC/难序列化 | 仅存 id 与值类型；Unity 引用走 `EntityBase` |
| 塔与兵逻辑纠缠 | **索敌与伤害**走统一 Impact，塔只多索敌规则 |
| 野怪与英雄 AI 混写 | 野怪 **仅** `JungleCreepModule` 驱动，不复制英雄技能树 |

---

## 14. 非塔子系统的冗余与合并建议（审阅用）

除 **`TowerCombatCycleSystem`** 外，其余条目容易出现 **「组件重复 / 系统过碎 / 与全局模块边界重叠」**。下表供实现前裁切范围。

| 模块 | 冗余/重叠性质 | 精简建议 |
|------|----------------|----------|
| **`FactionComponent` + `UnitArchetypeComponent`** | **非冗余**：前者是 **阵营/队伍**，后者是 **兵种模板**；正交维度。 | 保留；勿用 `ConfigId` 再藏一份阵营以免双源。 |
| **`CombatBoardLite` vs（历史）塔内第二目标字段** | ~~双字段~~ → **已废止** | **唯一写源** §8.1；周期组件见 §5.2 |
| **`JungleLeashSystem`（废弃）** vs **`JungleAiSystem`** | 追逃、归巢与 **租赁半径** 属同一 AI 流程 | **仅保留 `JungleAiSystem`**，Leash 逻辑写在其内；不设 **`JungleLeashSystem`**。 |
| **`LaneMinionMoveSystem` vs「近战索敌」（§6）** | 两套 System 易 **双遍遍历**与 **走位/开火规则割裂** | **§8.3**（+ §15.2 索引） |
| **`UnitDeath` 分散实现** | 各兵种各写 HP≤0 | **统一 `UnitVitalitySystem`**（§10）。 |
| **`JungleCreepModuleComponent.CurrentState`** | 与 **系统在内存中的状态机** 可能 **双写** | 若 AI 全在 System 内维护 FSM，可把 `CurrentState` 减为 **调试/网络同步**才写；否则保留组件作 **权威快照**（二选一定稿）。 |

**原则**：塔因 **四阶段时间轴** 独立；兵线/野怪 **少 System、多共用 helper**。详见 **§5**（塔节拍）与 **§8.3**（兵线/野怪移动与索敌分层）。

---

## 15. 与 §8 的补充说明（冗余辨析）

### 15.1 `CombatBoardLite` 唯一写源

**已定稿**：瞄准/当前攻击目标 **只用** `CombatBoardLiteComponent.AttackTargetEntityId`；**`TowerCombatCycle` 不再存放** 平行 `LockedTarget*` 字段（见 **§5.2、§8.1**）。此前「塔黑板二选一」的讨论以此为准收尾。

### 15.2 兵线：移动与索敌为何不拆成两套顶层 System

说理与 **NavMesh 分工**已并入 **§8.3**（**索敌写黑板** ↔ **NavMesh 写到点**）；本节不再重复实现细节。

---

## 16. 文档交叉引用

- **局内生命与死亡**：**`UnitVitalitySystem`**（§10，与 `EntityDataComponent` 等配合；实现可落在 `Gameplay`/`Core` 未定稿）
- 属性：`EntityDataComponent`、《角色属性系统设计文档》
- Impact 与 ECS 事件：`Core.Combat.ImpactSystem`、`Gameplay.Skill.Ecs.SkillCastPipelineSystem`（塔为可选）；《基于Buff与ECS的技能系统设计文档》
- 击杀经济：《MOBA局内经济系统设计文档》
- **Unity AI / NavMesh**（实现侧）：[Navigation](https://docs.unity3d.com/Manual/Navigation.html)（官方手册；与 ECS 分工见 §8.3）

