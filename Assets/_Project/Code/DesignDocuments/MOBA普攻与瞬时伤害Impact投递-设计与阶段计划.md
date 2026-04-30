# MOBA 普攻（含瞬时伤害）投递 Impact 管线 — 设计与阶段落地计划

| 项 | 内容 |
|----|------|
| **文档版本** | 2.0.1 |
| **定型方案** | **A + C**：**选敌服务 `ITargetAcquisitionService` → 填入 `SkillCastContext`（方案 A）** + **`CombatBoardLite` 管单体战术状态 / Context 管本次施法事务（方案 C）** |
| 关联文档 | 《Impact系统使用方法指导文档》《MOBA局内单位模块ECS设计文档》《MOBA局内单位生成与战斗接入-进度与MVP计划》；技能侧 `SkillCastContext`、`SkillCastPipelineSystem` |
| 适用 | Unity 2022；毕设向「可走通链路 + 可写进论文的职责划分」 |

---

## 1. 背景与问题

防御塔已通过 **`TowerCombatCycleSystem`** 在 Strike 节拍落地时调用 **`ImpactManager.CreateImpactEvent`**，经 **`ImpactSystem`**（`UpdateOrder ≈ 31`）结算 HP，并向目标 **`CombatBoardLiteComponent.LastDamageFromEntityId`** 写入；**`UnitVitalitySystem`** 可据此补 **`KillerEntityId`**。

**英雄侧缺少稳定触发层**：没有统一在合法时机把 **单体普攻** 与 **技能（含未来 AoE）** 接到 **`CreateImpactEvent`** 或 **技能 Buff 流水线**。

**战斗真理源**：对 **HP 的修改** 仍推荐走 **`ImpactSystem`**（瞬时伤害）或 **Buff/Opcode 内部再调 Impact**；禁止双通道手写改血（迁移期须标注例外）。

---

## 2. 定型架构：方案 A + C（总则）

### 2.1 职责划分（C：黑板 vs 施法事务）

| 载体 | 语义 | 生命周期 | 典型写入方 |
|------|------|----------|------------|
| **`CombatBoardLiteComponent`** | **战术/UI 层「当前锁谁打」**（单体） | **持续**，随点选/AI 切换 | 玩家输入、`CombatTargetAcquire` 式 AI |
| **`SkillCastContext`** | **本次 cast 的实例数据**：`PrimaryTarget`、`SecondaryTargets`、（P2 可扩）几何意图 | **单次施法**，与 `TryBeginCast` session 对齐 | 施法闸门在 **确认施法** 时统一组装 |
| **`ITargetAcquisitionService`（A）** | **几何/规则 → 命中实体列表** | **无状态服务**，随调随用 | 施法确认前、普攻出手前 |

**硬规则**：**AoE / Cone / Line 的批量命中只进入 `SkillCastContext.SecondaryTargets`（及必要的 `PrimaryTarget` 约定），不写入黑板多槽。** 黑板至多维护 **一个** `AttackTargetEntityId` 作为 **主指示/UI/普攻默认目标**。

### 2.2 执行顺序（口述）

1. **输入**：点选单位 → 写 **`AttackTargetEntityId`**；范围技能 → UI 给出几何参数（P1+）。  
2. **施法/普攻确认**：调用 **`ITargetAcquisitionService.Acquire`** → 得到 **`TargetAcquisitionResult`**。  
3. **拼装 Context**：`PrimaryTarget` / `Clear + AddRange(SecondaryTargets)` 按 **§3.6** 约定写入。  
4. **单体普攻**：可直接 **`ICombatImpactDispatch.TryDispatchNormalAttack`**（MVP）或走极短技能定义（P1）。  
5. **技能**：**`SkillExecutionFacade.TryBeginCast`** → 现有 **`DefaultTargetResolver`** 消费 Context（不变更解析器语义）。

### 2.3 NPC / 玩家与写入权

| 通道 | 黑板 | Context |
|------|------|---------|
| **NPC AI** | 索敌后写 `AttackTargetEntityId` | 托管施法时由 AI 调 `Acquire` 再 `TryBeginCast` |
| **玩家** | 点选写 `AttackTargetEntityId` | 按键确认时 **从黑板可选回填 Primary**，再 `Acquire` 补 **`SecondaryTargets`** |

**写入权**：玩家操控期间 **仅输入层** 写黑板 `AttackTarget`；切 **AI 接管** 后仅 AI 写，避免抖动（与 §2 原约定一致）。

---

## 3. 接口与数据契约（A + C）

> 以下为 **设计层契约**；命名空间实现时可选用 `Gameplay.Combat.Targeting`、`Core.Entity` 等与工程规范一致即可。

### 3.1 `TargetingShapeKind`（请求形态枚举）

| 值 | 含义 | MVP | P1 | P2 |
|----|------|-----|----|----|
| `PointEntity` | 明确单体（黑板 id 或射线命中） | ✓ | ✓ | ✓ |
| `NearestInSphere` | 以施法者为球心、`AtkDistance` 最近敌 | ✓（Debug/AI） | ✓ | ✓ |
| `GroundCircle` | 地面圆心 + 半径 | — | ✓ | ✓ |
| `Cone` | 原点 + 朝向 + 角 + 深 | — | 可选 | ✓ |
| `Line` | 线段/扫掠（宽厚或胶囊） | — | 可选 | ✓ |

### 3.2 `TargetAcquisitionRequest`（请求 DTO，建议 `readonly struct`）

| 字段 | 类型 | 说明 |
|------|------|------|
| `Shape` | `TargetingShapeKind` | 决定解释方式 |
| `Caster` | `EntityBase` | 非空；用于位置、阵营、`BoundEcsEntity` |
| `PrimaryEntityIdHint` | `long` | 可选；`**AttackTargetEntityId**` 回填，0 表示无 |
| `WorldOrigin` | `Vector3` | Caster 脚下 / 地面落点 / 弧形原点 |
| `WorldDirection` | `Vector3` | Cone/Line 朝向（建议归一化） |
| `RangeOrRadius` | `float` | 普攻距离 / 圆半径 / 锥深 / 线段长 |
| `SecondaryParam` | `float` | 圆锥半角（度）、线宽等 |
| `IncludeDead` | `bool` | 默认 false；筛 `CrtHp > ε` |

### 3.3 `TargetAcquisitionResult`（解析结果）

| 成员 | 类型 | 说明 |
|------|------|------|
| `Succeeded` | `bool` | 是否可作后续拼装 |
| `Error` | `string` | 失败原因（超出距离、无合法目标等） |
| `Hits` | `IReadOnlyList<EntityBase>` | 去重后的命中集合（**顺序稳定**，供表驱动多段伤害） |
| `SuggestedPrimary` | `EntityBase` | 可选；**指向「主目标」**（如离圆心最近敌），供填 `PrimaryTarget` |

### 3.4 `ITargetAcquisitionService`

```csharp
public interface ITargetAcquisitionService
{
    /// <summary>无状态解析；不写黑板、不写 Context。</summary>
    TargetAcquisitionResult Acquire(in TargetAcquisitionRequest request);
}
```

**实现备注**：内部依赖 **`EntityEcsLinkRegistry` + `Transform.position`**、`CombatHostility`、`**EntityDataComponent**`；P1 起与 **`SkillDefinition.CastRange`** 做交叉校验。

### 3.5 `ICombatBoardTargetSync`（黑板 ↔ Mono，可选拆分静态类）

| 方法 | 职责 |
|------|------|
| `TryGetPrimaryAttackTarget(EntityBase caster, out EntityBase target)` | `caster.BoundEcsEntity` → 读 **`CombatBoardLite.AttackTargetEntityId`** → `TryGetEntityBase` |
| `SetPrimaryAttackTarget(EntityBase caster, long targetEcsId)` | 写己方 ECS **`AttackTargetEntityId`**（**需 `HasComponent`**） |

### 3.6 `SkillCastContext` 拼装约定（确认施法瞬间）

| 场景 | `PrimaryTarget` | `SecondaryTargets` |
|------|-----------------|-------------------|
| **单体技能** | **SuggestedPrimary 或 Hits[0]** | **清空** |
| **纯 AoE（无主目标）** | **null** 或Caster（按表意图，须在 **SkillDefinition/README** 明示） | **Hits** |
| **主目标 + 溅射（P2）** | 主单体 | **副列表**（`Acquire` 拆两次或单次返回主子结构） |

**与黑板**：若本次为 **点选延续的单体技能**，在拼装前可先 **`TryGetPrimaryAttackTarget`**，把 **`PrimaryEntityIdHint`** 填入 Request；**AoE 仍不以黑板存列表**。

### 3.7 `ICombatImpactDispatch`（普攻 / 瞬时物伤出站）

```csharp
public interface ICombatImpactDispatch
{
    /// <summary>单体普攻；内部 CreateImpactEvent，参数与 Tower Strike 对齐。</summary>
    bool TryDispatchNormalAttack(EntityBase attacker, EntityBase victim, out string error);
}
```

---

## 4. MVP 阶段 — A+C 最小闭环

**目标**：**一条**从「单体目标」到 **`ImpactSystem`** 的路径；选敌接口 **已实现 `PointEntity` + `NearestInSphere` 两样即可**。

| 编号 | 交付物 | 脚本位置 / 类 | 说明 |
|------|--------|-----------------|------|
| M1 | **`DefaultTargetAcquisitionService`** | `Scripts/Gameplay/Combat/Targeting/DefaultTargetAcquisitionService.cs` | **`ITargetAcquisitionService`**：`NearestInSphere`、`PointEntity`。 |
| M2 | **`DefaultCombatImpactDispatch`** | `Scripts/Gameplay/Combat/Targeting/DefaultCombatImpactDispatch.cs` | **`ICombatImpactDispatch`**：`AtkAD`、`Subtract`、`Physical`、`NormalAtk`。 |
| M3 | **`MvpHeroBasicAttackDebugBridge`** | 同上目录 `MvpHeroBasicAttackDebugBridge.cs` | **`Space`**：`NearestInSphere`；**`Return`**：读黑板 `AttackTargetEntityId` 走 `PointEntity`。`attackCooldownSeconds` 节流。 |
| M4 | **`CombatBoardRaySelectTarget`** | `CombatBoardRaySelectTarget.cs` | 左键射线点敌对单位 → **`CombatBoardTargetSync.SetPrimaryAttackTarget`**（需 **Collider**）。 |
| — | 类型与接口 | `TargetingShapeKind`、`TargetAcquisitionRequest`、`TargetAcquisitionResult`、`ITargetAcquisitionService`、`ICombatImpactDispatch` | 同目录零散文件；**`CombatBoardTargetSync`** 静态黑板读写（可作为 P1 接口化前身）。 |

**场景挂载**：英雄根挂 **`EntityBase` + `CombatEntitySpawnProfile`（含 CombatBoardLite）**；挂 **`MvpHeroBasicAttackDebugBridge`**（`attacker` 可留空则用本物体 **`EntityBase`**）；若需点选再上 **`CombatBoardRaySelectTarget`**（`controlledUnit` 同上）。敌对单位须有 **Collider + `EntityBase` + ECS 阵营**。

**不包含（刻意推迟）**：`GroundCircle/Cone/Line`、 **`TryBeginCast` 串联 AoE**、前摇动画。

**验收**：两单位敌对；普攻 **经 `Acquire` + `Dispatch`** → HP、`LastDamage`、击倒链条与塔一致；**黑板与 Context 在未施法流水线时可不同步**，但 **点选单体**场景下建议 **Hints 与棋盘一致以便答辩演示**。

---

## 5. P1 阶段 — 拼装 Context + 单体技能并联

**目标**：**玩家点选 → 黑板**；**确认技能 → `Acquire` → 填 Context → `TryBeginCast`**；普攻继续使用 **`ICombatImpactDispatch`** 或 **skillId=普攻** 二选一并文档锁定。

| 编号 | 交付物 | 说明 |
|------|--------|------|
| P1 | **`ICombatBoardTargetSync`（正式接口 + 可多实现）** | MVP 已为静态 **`CombatBoardTargetSync`**；P1 可抽接口并接 **输入/UI**；补充死亡/失目标 **清零规则**。 |
| P2 | **`SkillCastGate`（或同等 Facade）** | 单入口：`BeginSkillCast(skillId, optional override request)`：读表 **`RequiresTarget`** → 决定 Request 形态；**`Acquire` → 填 `SkillCastContext` → `TryBeginCast`**。 |
| P3 | **`GroundCircle` 于 `ITargetAcquisitionService`** | **Request** 带落点+半径；**Hits → `SecondaryTargets`**；**`PrimaryTarget`** 按 §3.6 表。 |
| P4 | **与 `CastRange` 校验** | 施法者到 **WorldOrigin**（或到主目标）距离 ≤ **`SkillDefinition.CastRange`**。 |
| P5 | **`ICombatImpactDispatch` 与技能 Opcode** | 明确：**直接伤害** 走 Impact 的唯一入口清单（避免 Buff 与 Dispatch 双写）。 |

**验收**：至少 **1 个单体技能** + **1 个圆形地面 AoE**（无 Cone/Line 亦可）；黑板 **仅主目标**；**范围命中仅 Context 列表**。

---

## 6. P2 阶段 — 几何补全、时间轴与演进位

**目标**：可演示性、表驱动与 **B/D 方案可选演进**（不强制一次做完）。

| 编号 | 交付物 | 说明 |
|------|--------|------|
| R1 | **Cone / Line** | 扩展 **`TargetingShapeKind`** 与 **`Acquire`** 实现。 |
| R2 | **主目标 + 溅射** | `Acquire` 返回 **结构化主子** 或 **两次 Acquire**；表 **多步 Selector** 区分 Primary/Secondary。 |
| R3 | **普攻时间轴** | Windup → HitFrame → Recovery；**HitFrame** 才 `Dispatch`。 |
| R4 | **`SkillCastContext` 几何快照（方案 B 可选）** | 落点、形状枚举进 Context，**Hydrate** 与 **Acquire** 二选一为主路径。 |
| R5 | **策略式 Resolver（方案 D 可选）** | JSON 步骤绑定解析策略，减少硬编码技能。 |
| R6 | **Command / 预测占位（方案 E）** | 预览合法性与执行分离，作论文章节「扩展性」即可。 |

---

## 7. 端到端数据流

### 7.1 单体普攻（A + C + MVP 主力）

```
点选 / Nearest →（可选）SetPrimaryAttackTarget → AttackTargetEntityId
确认普攻 → TargetAcquisitionRequest(PointEntity 或 NearestInSphere, Hint=board)
        → ITargetAcquisitionService.Acquire
        → ICombatImpactDispatch.TryDispatchNormalAttack(attacker, primaryHit)
        → ImpactSystem → LastDamage / Vitality
```

### 7.2 技能（已有流水线 + A 注入）

```
UI 几何 / 点选 → Request（P1+ 含 GroundCircle）
        → Acquire → Hits
        → 填 SkillCastContext.PrimaryTarget / SecondaryTargets（§3.6）
        → SkillExecutionFacade.TryBeginCast
        → DefaultTargetResolver → BuffApplyService / Opcode →（可能）CreateImpactEvent
```

---

## 8. 与现有代码的映射

| 模块 | 在 A+C 中的角色 |
|------|-----------------|
| `CombatBoardLiteComponent` | **C**：**仅** `AttackTargetEntityId`（+ `Threat…`） |
| `SkillCastContext` | **C**：**本次** `PrimaryTarget` / `SecondaryTargets` |
| `SkillCastPipelineSystem` | **不修改**步骤解析语义；消费已填好的 Context |
| `DefaultTargetResolver` | **保持**；几何不进入 Resolver |
| `ImpactManager` + `ImpactSystem` | 普攻与技能瞬时伤的 **统一结算出口** |
| `CombatTargetAcquire` | 可 **被 `Acquire` 内部复用**（Nearest / 球心距离） |
| `Gameplay.Combat.Targeting`（MVP） | **`DefaultTargetAcquisitionService`、`DefaultCombatImpactDispatch`**、黑板 **`CombatBoardTargetSync`**、Debug **`MvpHeroBasicAttackDebugBridge`** |
| `TowerCombatCycleSystem` / `JungleAiSystem` | **不强制**走 `ITargetAcquisitionService`；若统一风格可逐步委托 |

---

## 9. 仓库现状清点（与定型方案的关系）

- **已有**：`SkillCastContext` 列表、`SkillCastPipelineSystem`、`DefaultTargetResolver`。**MVP 已增**：命名空间 **`Gameplay.Combat.Targeting`** 下 **`ITargetAcquisitionService` / `DefaultTargetAcquisitionService`（PointEntity、Nearest）**、`ICombatImpactDispatch` / **`DefaultCombatImpactDispatch`**、`CombatBoardTargetSync`、`MvpHeroBasicAttackDebugBridge`、`CombatBoardRaySelectTarget`。  
- **尚无**：GroundCircle/Cone/Line 几何、**`SkillCastGate`**、黑板 **正式接口封装**。

---

## 10. 备选架构（简表）

| 方案 | 要点 | 与 A+C 关系 |
|------|------|-------------|
| **B** | Context 内嵌 **几何快照 + Hydrate** | P2 可选，**替代** Request DTO 或与之并存 |
| **D** | 可插拔 **`ITargetResolver` 策略** | P2 表驱动加深时使用 |
| **E** | Command 预览/执行 | 论文扩展；与 A+C 正交 |

**全文主路径**：**A + C**（§2～§6）。

---

## 11. 文档修订记录

| 版本 | 日期 | 说明 |
|------|------|------|
| 1.0 | 2026-04-17 | 初稿：英雄侧 Impact 缺口；MVP/P1/P2 粗粒度。 |
| 1.1 | 2026-04-17 | §2.1：NPC/玩家与黑板。 |
| 1.2 | 2026-04-17 | 仓库现状 + 方案 A～E 对比。 |
| **2.0** | **2026-04-17** | **重构**：定型 **A+C**；新增 **§3 接口与数据契约**；**§4～§6** 按 A+C 重写 MVP/P1/P2；数据流与代码映射同步；备选方案缩为 §10。 |
| 2.0.1 | 2026-04-17 | §4：**MVP 脚本落地**：`Gameplay.Combat.Targeting` 选敌、`DefaultCombatImpactDispatch`、`MvpHeroBasicAttackDebugBridge`、`CombatBoardRaySelectTarget`、`CombatBoardTargetSync`。 |
