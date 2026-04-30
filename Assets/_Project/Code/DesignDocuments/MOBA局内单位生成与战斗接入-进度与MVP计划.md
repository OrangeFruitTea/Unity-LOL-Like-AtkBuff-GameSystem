# MOBA 局内单位生成与战斗接入 — 现状、差距与编码阶段计划

| 项 | 内容 |
|----|------|
| 文档版本 | 1.1 |
| 关联文档 | 《MOBA局内单位模块ECS设计文档》、《Buff-Opcode效果列表与触发语义设计文档》、《MOBA普攻与瞬时伤害Impact投递-设计与阶段计划》 |
| 适用 | Unity 2022，毕设体量 |

---

## 1. 目的

在 **「可信的局内单位生成」** 前提下，说明当前工程 **相对完整 MOBA 运行时的差距**，并给出 **MVP / P1 / P2** 的 **编码交付物**（组件 / 接口 / 脚本 / 流程）。与《局内单位 ECS 设计文档》互为补充：设计文档偏 **理想组件表**；本文偏 **落地顺序与缺口盘点**。

---

## 2. 当前实现（截至本文档编写时）

| 环节 | 现状 |
|------|------|
| **入口** | **`EntitySpawnSystem.AddPendingEntity`** → 下一帧 **`SpawnEcsEntity`**：创建 **`EcsEntity`**，写 **`EntityBase` / `EcsEntityBridge`**，**`AddBaseComponents`**。 |
| **基础属性** | **`EntityDataComponent`** 必挂，`InitializeDefaults`。 |
| **阵营 / 原型 / 黑板** | 若场景实例上存在 **`CombatEntitySpawnProfile`**（新增），则按勾选挂 **`FactionComponent`、`UnitArchetypeComponent`、`CombatBoardLiteComponent`**。无 Profile 时行为与历史一致：仅属性，**不适配**塔/兵/野怪的专精组件。 |
| **注册表** | **`EntityEcsLinkRegistry.Register`**，供坐标查询、Buff 目标校验等。 |
| **测试 Spawner** | **`TestPlayerSpawner`**：**`Instantiate(prefab)`** 得到 **场景实例** 再入队；支持 **父节点、局部偏移**。 |

---

## 3. 相对「可跑 MOBA 对局」的差距分析（毕设视角）

以下按 **对局是否跑得通** 分层，**不**苛求商业级网络与大地图。

### 3.1 已具备或可快速接上的「共享层」

| 内容 | 说明 |
|------|------|
| **`EntityDataComponent`** | 生命、攻防等真理源，接 **Buff / Impact**。 |
| **`FactionComponent` + `CombatBoardLiteComponent`** | 在挂 **Profile** 后存在；接 **索敌、Impact 承伤黑板、UnitVitality 击杀者**。 |
| **`UnitArchetypeComponent`** | 兵种标签 + `ConfigId` 挂钩表。 |
| **塔 / 兵线 / 野怪 组件定义** | 设计文档与代码中已有 **struct**；**未**默认同英雄一起生成。 |
| **系统** | **`TowerCombatCycleSystem`、`ImpactSystem`、`UnitVitalitySystem`**（及 Buff Opcode MVP）已注册；英雄需 **正确 Buff/技能表** 才有伤害行为。 |

### 3.2 仍缺的「MOBA 运行时关键件」（按优先级）

| 缺口 | 后果 | 典型归属阶段 |
|------|------|----------------|
| **专精组件按需挂载** | 塔无 **`Tower*`** 则 **塔不攻击**；兵无 **`LaneMinion*`** 则无兵线 AI；野怪无 **`Jungle*`** 则无租赁/追逐。 | P1 |
| **专用 Spawner / Factory** | 设计文档推荐 **TowerSpawner / MinionWaveSpawner / JungleCampSpawner**；当前仅 **通用 EntitySpawn + Profile**。 | P1 |
| **生成后初始化** | 如：从表覆写 **`EntityData`**、挂 **技能书**、注册 **小地图**；未做则「空壳英雄」。 | MVP+ |
| **`UnitVitality` 之后表现** | ECS 仅写 **Killer**；**销毁 GameObject、掉落、事件总线**未统一。 | MVP / P1 |
| **普攻/技能与目标** | 需 **`SkillCastContext.PrimaryTarget`** + 可选写 **黑板 `AttackTarget`**；与生成独立但依赖 **场上单位齐全**。 | MVP / P1 |
| **移动与权威位置** | **`MovementController` 与 ECS 未绑定**；近战贴脸、塔射程依赖 **Transform**（`EntityEcsLinkRegistry`）——可毕设级简化。 | P1～P2 |
| **波次 / 经济 / UI** | 兵线 **`WaveSpawnId`**、击杀经济、血条刷新策略；分系统文档。 | P2 |

### 3.3 结论（毕设话术）

当前水平：**「单个带 Profile 的英雄式单位可以从零场景生成并完成 Impact/Buff 链路」** 已基本具备；距离 **「一条路上有塔、有兵、有野怪循环刷新的 Mini-MOBA」** 还差 **专精组件挂载 + 专用 Spawner + Vitality/销毁与波次的最小闭环**。

---

## 4. MVP 阶段 — 编码需求

**目标**：从零场景 **生成 1～2 名可互打/可吃技能 Buff 的单位**，**阵营/黑板/原型** 完整，**无明显错误 Spawner**。

| 交付物 | 类型 | 接口 / 逻辑要点 |
|--------|------|-----------------|
| **`CombatEntitySpawnProfile`** | Mono 组件 | Inspector：`TeamId`、`Archetype`、`ConfigId`、是否挂 **Faction / Archetype / CombatBoard**；**无则回退仅 EntityData**。 |
| **`EntitySpawnSystem.AddBaseComponents` 扩展** | 系统私有方法 | **`GetComponent<CombatEntitySpawnProfile>()`** → 按勾选 **`EcsWorld.AddComponent`**；顺序：Data → Faction → Archetype → CombatBoard。 |
| **`TestPlayerSpawner` 修正** | Debug 脚本 | **`Instantiate`** → **`GetComponent<EntityBase>`** → **`AddPendingEntity(instance)`**；可选 **parent / localOffset**。 |
| **Prefab 配置说明**（文档/注释） | 流程 | TestPlayerPrefab **根物体**须有 **`CombatEntitySpawnProfile`** + **`EntityBase`**；敌我 **TeamId** 区分。 |
| **`UnitVitality` 后置（最小）** | 可选 MVP+ | **`KillerEntityId != 0` 且 HP≤0**：`Debug.Log` 或 **单一 `UnityEvent`/静态事件**，**不强制**销毁（P1 再统一）。 |

**验收**：两实例，`Faction` 敌对，`Impact`/`Opcode Buff` 可扣血，**`CombatBoard.LastDamage`** 可走；无 Profile 的旧 Prefab **仍能**仅以属性出生（兼容）。

### 4.1 MVP 脚本需求（可按文件落地的规格）

| 脚本路径 | 类（命名空间） | 职责与调用约束 |
|---------|----------------|----------------|
| `Scripts/Gameplay/Entity/CombatEntitySpawnProfile.cs` | `CombatEntitySpawnProfile`（`Core.Entity`） | **`MonoBehaviour`**，挂Prefab根或子物体；Inspector 勾选 `AddFaction` / `AddArchetype` / `AddCombatBoardLite` 及 `TeamId`、`Archetype`、`ConfigId`。无此组件 ⇒ `EntitySpawnSystem` 不写阵营/黑板/原型。 |
| `Scripts/Gameplay/Entity/EntitySpawnSystem.cs` | `EntitySpawnSystem`（`Core.Entity`） | **`SpawnEcsEntity` 顺序**：`CreateEntity` → 绑定 `EntityBase`/Bridge → **`AddBaseComponents`**（必选 `EntityDataComponent.InitializeDefaults`，再 **`GetComponent<CombatEntitySpawnProfile>()`**，按勾选 `EcsWorld.AddComponent`：**Faction → Archetype → CombatBoard**）→ **`EntityEcsLinkRegistry.Register`** → **`RunSpawnExtensions`** →（可选调试 Log）。 **`AddPendingEntity(instance)`**：仅入队，`UpdateOrder=5` 的同一帧结束前出队并完成上述流程。 |
| `Scripts/Basement/Tools/Debug/TestPlayerSpawner.cs` | `TestPlayerSpawner`（`Core.Entity`） | 协程延后一帧：**`Instantiate(prefab.gameObject, parent,...).GetComponent<EntityBase>()`** → `EcsWorld.Instance.GetEcsSystem<EntitySpawnSystem>().AddPendingEntity(instance)`。**`spawnParent`、`localOffset`** 可选；Prefab 根需 **`EntityBase`（或子类）** + 建议 **`CombatEntitySpawnProfile`**。 |
| `Scripts/Gameplay/Entity/UnitDeathEventHub.cs` | **`UnitDeathEventHub`（静态）**（`Core.Entity`） | MVP+：`event Action<EcsEntity, long> UnitDied`。**仅派发**，不负责 `Destroy`。 |
| `Scripts/Gameplay/Entity/UnitVitalitySystem.cs` | `UnitVitalitySystem`（`Core.Entity`） | **`UpdateOrder=32`**（Impact 之后）。每帧：**`CrtHp>0`** 则从「已播报死亡集合」摘除该 ECS Id；否则若缺 `CombatBoardLite` 跳过；否则 **`KillerEntityId==0` 时用 `LastDamageFromEntityId` 补齐** → 若 ECS Id **首次**进入击杀态则 **`UnitDeathEventHub.Raise(victim, killer)`**（同名实体复活前不重复派发）。 |

**Prefab 自检清单**：根节点 `EntityBase`；若要对打则 `CombatEntitySpawnProfile`：`AddFaction/AddCombatBoardLite`、敌我 **`TeamId` 敌对**、`Archetype` 常为 `Hero`；调试技能/Buff/Opcodes 照旧走表与桥接。

---

## 5. P1 阶段 — 编码需求

**目标**：**塔 / 兵线 / 野区** 至少各有一条 **可复制流程**（可极简数值）；与 **Waypoint / Nav（可选）** 对齐。

| 交付物 | 组件 / 脚本 | 逻辑 / 流程 |
|--------|-------------|-------------|
| **`TowerSpawner`（或等价工厂）** | Mono | **Instantiate** → **`AddPendingEntity`** → **`EcsWorld.AddComponent<TowerModuleComponent>`、`TowerCombatCycleComponent`** + **必选 Profile（阵营+黑板+Archetype=Tower）**；表或 Inspector 填 **射程/节拍**。 |
| **`MinionWaveSpawner`（简化）** | Mono + 协程/Timer | 按 **波次 id** 生成兵；挂 **`LaneMinionModuleComponent`** + Profile；**`LaneMinionMoveSystem`** 内 **沿 Waypoint 推进**（或直线插值）。 |
| **`JungleCampSpawner`** | Mono | 挂 **`JungleCreepModuleComponent`** + Profile；**`JungleAiSystem`** 内 **租赁 + 追打写黑板**。 |
| **生成管线约定** | 文档 + 代码注释 | **专精组件**在 **`EntitySpawnSystem` 完成基础注册之后** 由工厂 **`AddComponent`**（与设计文档 §9 一致）；或 **合并进 Profile 的「扩展接口」**（见下节）。 |
| **`IEntitySpawnExtension`（可选）** | C# 接口 | `void OnAfterEcsBaseSpawned(EcsEntity ecs, EntityBase host);` 由 **TowerSpawner** 等实现，**避免** `EntitySpawnSystem` 无限 `switch`；**毕设可省略**，全部写在工厂里。 |
| **死亡销毁** | 小模块 | 订阅 Vitality/HP：Host **`GameObject.SetActive(false)`** 或 **`Destroy`**，并 **`EcsManager.DestroyEntity`**（注意与 **EntityBase.OnDestroy** 顺序，防双删）。 |

**验收**：塔能 **IdleScan→Impact** 打进入射程的 **不同阵营** 单位；至少 **一波兵** 沿路径移动；**一只野怪** 超 **Leash** 回巢。

### 5.1 P1 脚本需求（专精挂载 + Spawner + 移动/野区 AI）

#### 管线约定（与代码一致）

**`Register` 完成之后**，`EntitySpawnSystem.RunSpawnExtensions` 对宿主及子树上的 **`MonoBehaviour`** 逐项检测：若 **`enabled` 且实现 `IEntitySpawnExtension`**，则调用 **`OnAfterEcsBaseSpawned(ecs, host)`**。专精 ECS 组件只应在此回调或其实现体内 `EcsWorld.AddComponent`，**不要**再改 `EntityData`/`Profile` 已写好的一批基础组件的顺序。

| 脚本路径 | 类（命名空间） | 要点 |
|---------|----------------|------|
| `Scripts/Gameplay/Entity/IEntitySpawnExtension.cs` | `IEntitySpawnExtension`（`Core.Entity`） | 方法：`void OnAfterEcsBaseSpawned(EcsEntity ecs, EntityBase host);`。 |
| `Scripts/Gameplay/Entity/Tower/TowerEcsAttachments.cs` | `TowerEcsAttachments : MonoBehaviour, IEntitySpawnExtension` | Inspector 可调 **`TowerModulePreset`** → 运行时 **`TowerModuleComponent`** + **`TowerCombatCycleComponent`（默认值）**。塔 Prefab 仍需 **`CombatEntitySpawnProfile`**（`Faction`、`CombatBoardLite`、`Archetype=Tower`、敌我分队）。 |
| `Scripts/Gameplay/Entity/Spawn/TowerSpawner.cs` | `TowerSpawner`（`Core.Entity.Spawn`） | **`Start`**：`Instantiate` → `GetComponent<EntityBase>` → `AddPendingEntity`；`towerPrefab`、`spawnParent`、`localOffset`。 |
| `Scripts/Gameplay/Entity/LaneMinion/LaneMinionEcsAttachments.cs` | `LaneMinionEcsAttachments`（`Core.Entity.Minions`） | 配置 `WaveSpawnId`、`LaneIndex`、`PathwayId`；回调里 **`LaneMinionModuleComponent.WaypointIndex=0`**。 |
| `Scripts/Gameplay/Entity/LaneMinion/LaneMinionWaypointRuntime.cs` | 静态 **`LaneMinionWaypointRuntime`**（`Core.Entity.Minions`） | 字段：**`Transform[] Waypoints`**。由 **`MinionWaveSpawner.Awake`** 从 **`waypointRoot` 的子节点顺序** 填充 **`SetWaypoints`**。 |
| `Scripts/Gameplay/Entity/LaneMinion/LaneMinionMoveSystem.cs` | `LaneMinionMoveSystem`（`IEcsSystem`，**`UpdateOrder=39`**） | 若 Waypoints 为空则 no-op；否则沿当前下标 **`MoveTowards`**，抵达阈值后 **`WaypointIndex++`**。**依赖** `EntityEcsLinkRegistry` 取 Transform；与 Nav 对齐时可替换为推进策略，接口保持读 `LaneMinionModuleComponent`。 |
| `Scripts/Gameplay/Entity/Spawn/MinionWaveSpawner.cs` | `MinionWaveSpawner`（协程）（`Core.Entity.Spawn`） | **`Awake`** 建立路径 **`LaneMinionWaypointRuntime`**；**`Start`** 循环：`minionsPerWave` 次 Instantiate + **`AddPendingEntity`**，`waveIntervalSeconds`；`startDelaySeconds`。 |
| `Scripts/Gameplay/Entity/Jungle/JungleCreepEcsAttachments.cs` | `JungleCreepEcsAttachments`（`Core.Entity.Jungle`） | **`JungleCreepModuleComponent`**：`CampId`、`LeashRadius`、`AnchorSlotIndex`；可选 **`leashCenterFromSpawnPosition`**（用宿主 `Transform.position` 写 **`LeashCenter`**）。仍需 Profile（阵营、黑板、原型 `JungleMonster`）。 |
| `Scripts/Gameplay/Entity/Spawn/JungleCampSpawner.cs` | `JungleCampSpawner`（`Core.Entity.Spawn`） | **`Start`** 单次生成并入队（与 `TowerSpawner` 同套路）。 |
| `Scripts/Gameplay/Entity/Jungle/JungleAiSystem.cs` | `JungleAiSystem`（**`UpdateOrder=38`**） | **`Idle`**：以 **`LeashCenter` 为球心、`LeashRadius` 为半径** 调用 **`CombatTargetAcquire.TryPickNearestHostileInRangeFromWorldPoint`** → `Pursue` 写 **`AttackTargetEntityId`**；**`Pursue`**：超出租赁距 → **`Returning`** 清黑板；追击 **MoveTowards**；近战间隔用 **`ImpactManager.CreateImpactEvent`**（`NormalAtk`/物理减 HP）；**`Returning`**：**MoveTowards(LeashCenter)**，贴近后 **`Idle`**。 |
| `Scripts/Gameplay/Entity/CombatTargetAcquire.cs` | 扩展方法所在静态类 **`CombatTargetAcquire`** | 新增 **`TryPickNearestHostileInRangeFromWorldPoint(origin, aggressor, faction, range, out target)`** 供野区与任意「非自身坐标为球心」的索敌。 |
| `Scripts/Gameplay/Entity/DestroyHostOnUnitDeath.cs` | `DestroyHostOnUnitDeath`（`Core.Entity`） | **`OnEnable` 订阅 `UnitDeathEventHub.UnitDied`**：若 **`host.BoundEcsEntity.Id == victim.Id`** 则 **`Destroy(gameObject)`** 或 **`SetActive(false)`**。**勿**在此处再调 `EcsManager.DestroyEntity`（ **`EntityBase.OnDestroy`** 已处理）。 |

**场景挂载提示**：兵线 **`MinionWaveSpawner.waypointRoot`** 的子物体顺序即为路径；多路兵线可多个 Spawner **先后**写入同一静态 Waypoints——毕设单方推进时保持一个 Spawner 或扩展为通路 id。

---

## 6. P2 阶段 — 编码需求

**目标**：**可演示性**与 **可维护性** 增强；仍控制毕设范围。

| 交付物 | 说明 |
|--------|------|
| **JSON / ScriptableObject 生成表** | 单位种类 → **Profile 默认值 + 专精 payload**；减少 Inspector 误配。 |
| **对象池** | 兵线 **Despawn/Pool** 复用，避免频繁 Instantiate。 |
| **网络 / 存档占位** | 仅存 **权威组件快照**设计说明；不接完整 Mirror 逻辑亦可。 |
| **经济 / 记分** | 与《MOBA局内经济系统设计文档》对接 **`WaveSpawnId`、击杀事件**。 |
| **UI 绑定** | 血条 **订阅 ECS / 事件** 刷新（非仅 `Start` 读一次）。 |

---

## 7. 数据流小结（便于答辩口述）

```
Prefab + CombatEntitySpawnProfile
        → Instantiate(场景实例)
        → EntitySpawnSystem.AddPendingEntity
        → CreateEntity + EntityData (+ Faction / Archetype / CombatBoard?)
        → EntityEcsLinkRegistry.Register
        → MonoBehaviour Implement IEntitySpawnExtension → OnAfterEcsBaseSpawned（塔/兵/野专精）
        → 技能/Buff/塔系统读 ECS + Transform
```

---

## 8. 文档修订记录

| 版本 | 日期 | 说明 |
|------|------|------|
| 1.0 | 2026-04-17 | 初稿：差距分析 + MVP/P1/P2；与 CombatEntitySpawnProfile、Spawner 修正同步。 |
| 1.1 | 2026-04-17 | MVP/P1：**§4.1 / §5.1 脚本规格表**（路径、Inspector、管线顺序）；与 `IEntitySpawnExtension`、`UnitDeathEventHub`、三路 Spawner、`LaneMinionMoveSystem`、`JungleAiSystem`、`DestroyHostOnUnitDeath` 实现同步。 |

### 附录 A — MVP / P1 脚本交付 Checklist（对账）

- [ ] **`CombatEntitySpawnProfile`**：敌我塔/兵/野怪 **`TeamId`、`Archetype`** 配对正确，`AddCombatBoardLite` 为真方有 **`LastDamage` / Killer 链路**。  
- [ ] **`TestPlayerSpawner`**：确认为 **`Instantiate` 产出实例** 入队而非 Prefab 资产。  
- [ ] **`UnitDeathEventHub` + UnitVitalitySystem**：击倒只触发一次；可选 **`DestroyHostOnUnitDeath`** 挂要被销毁的根。  
- [ ] **`IEntitySpawnExtension`**：**塔 Prefab** 挂 **`TowerEcsAttachments`**；**兵线** 挂 **`LaneMinionEcsAttachments`**；**野怪** 挂 **`JungleCreepEcsAttachments`**。  
- [ ] **`TowerSpawner` / `MinionWaveSpawner` / `JungleCampSpawner`**：**Prefab 根带 `EntityBase`**，且 **`Start`/`Awake` 时机**早于或独立于首帧 ECS `Update`。  
- [ ] **`LaneMinionWaypointRuntime`**：`MinionWaveSpawner.Awake` 已跑过再出兵；或首波延迟 `startDelaySeconds`。  
- [ ] **`JungleAiSystem`**：**租赁圆心** 与 **`JungleCreepEcsAttachments.leashCenterFromSpawnPosition`** 一致；野区与兵线 **敌对 Faction** 能在 `CombatHostility` 下互打。
