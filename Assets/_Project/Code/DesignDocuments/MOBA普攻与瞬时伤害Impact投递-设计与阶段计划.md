# MOBA 普攻（含瞬时伤害）投递 Impact 管线 — 设计与阶段落地计划

| 项 | 内容 |
|----|------|
| **文档版本** | **3.0.1** |
| **核心理念** | **普攻与技能共用「统一战斗行动」骨架**（选目标→闸门→产出效果）；**玩家侧只是操作与配置的差别**，详见 **§2** |
| **定型方案** | **A + C**：**`ITargetAcquisitionService` → `SkillCastContext`（技能）**；**黑板管持续主目标 / Context 管单次施法（含 AoE 列表）** |
| 关联文档 | 《Impact系统使用方法指导文档》《MOBA局内单位模块ECS设计文档》《MOBA局内单位生成与战斗接入-进度与MVP计划》；技能侧 `SkillCastContext`、`SkillCastPipelineSystem` |
| 适用 | Unity 2022；毕设「可走通 + 职责清晰 + **论文叙事统一」** |

---

## 1. 背景与问题

防御塔等已通过 **`TowerCombatCycleSystem`** 在节拍点调用 **`ImpactManager.CreateImpactEvent`**，经 **`ImpactSystem`** 改 HP 并驱动 **`CombatBoardLite.LastDamage*`**、`UnitVitality` 击倒链。

英雄侧需提供 **稳定触发层**，且与设计一致地处理 **普攻**与**技能**。本文在 **§2** 先说清 **二者的统一管线**，再以 **§2.4～§2.7** 展开 **分型（A+C、黑板对齐）**，避免「两套井水不犯河水」的叙事。

---

## 2. 统一战斗行动模型与架构（先读本节）

### 2.1 通俗：普攻管线 vs 技能管线（玩家体感）

两处流程在脑子里可以落成 **同一张照片**，只是 **后边走的「剧本」长短不同**：

| 阶段 | **普攻（通俗说法）** | **技能（通俗说法）** |
|------|----------------------|----------------------|
| **定目标** | 锁一个人（点敌 / 自动最近敌）；理想上还记在 **黑板主攻**（§2.7） | 单体 / 一群人 / 一块地：**选敌或几何算出名单**，装进 **`SkillCastContext`**（黑板只保「主」，列表放 Context） |
| **确认出手** | 距离、敌意、还活着、攻速间隔等 | 再加 CD、耗蓝、沉默等 **技能闸门** |
| **出效果** | 往往等价于「**一下**普攻 Impact」——短剧本 | **多步**：时间轴上 Buff / Opcode / 间接触发 Impact，长剧本 |

用一句话：**都是「决定要打谁→检查能不能打→让战斗系统结出结果」**；普攻是 **一页纸剧本**，常见技能是 **多页连续剧**。

### 2.2 设计结论：可以合并骨架，分叉在「配方」与「门面」

- **骨架统一（推荐写法）**：**解析目标**（`Acquire`）→ **（可选）写黑板主攻**→ **闸门**（资源/CD/沉默）→ **执行层**：要么是 **`ICombatImpactDispatch`/`CreateImpactEvent` 主导的短路径**，要么是 **`SkillCastPipelineSystem`** 主导的表驱动 Buff/Opcode。
- **给玩家暴露的差异**：主要在 **按键/指示器/表现**（前摇弹道、圈圈），底层不必维护两套互不交谈的流水线。
- **仍要分叉的语义（不是驳回合并，是实现时要留钩子）**：目标 **基数**（单体 vs AoE）、**伤害节奏**（单拍 vs 多段）、普攻常绑 **攻速** 技能常绑 **表里 CD**。

与工程现状的对应：**技能**已由 Context + Resolver + Pipeline 占位；**普攻**可走「同一套 Acquire + 再走短路径 Dispatch」，或通过 **普攻 = 特殊 SkillId（一步 Impact）** 完全收入技能门（毕设任选其一作为主路径并在文档/README 冻结）。

### 2.3 统一骨架（一张图）

```
                    ┌─────────────────────────────┐
                    │  UI / AI：操作与意图         │
                    └──────────────┬──────────────┘
                                   ▼
         ┌─────────────────────────────────────────────┐
         │  选敌：ITargetAcquisitionService.Acquire（可复用 CombatTargetAcquire）│
         └──────────────┬──────────────────────────────┘
                        ▼
         ┌──────────────────────────────┐      黑板：AttackTargetEntityId（单体/UI）
         │  可选 Commit（§2.7）           │ ◄──── Context：PrimaryTarget + SecondaryTargets
         └──────────────┬───────────────┘       （AoE / 列表，单次施法）
                        ▼
         ┌─────────────────────────────────────────────┐
         │  闸门：普攻间隔 / 技能 CD · 沉默 · CastRange …   │
         └──────────────┬──────────────────────────────┘
                        ▼
              ┌─────────┴─────────┐
              ▼                   ▼
      普攻短路径 · Impact    技能长路径 · TryBeginCast
                                   → Opcode/Buff→（可能）Impact
```

**§3** 及以下接口表，均可视为对上述骨架的 **可替换零件**描述。

---

### 2.4 职责划分（方案 C）

| 载体 | 语义 | 生命周期 | 典型写入方 |
|------|------|----------|------------|
| **`CombatBoardLiteComponent`** | **「当前主攻锁谁」（单体/UI）** | **持续** | 点选、`CombatTargetAcquire` 类 AI |
| **`SkillCastContext`** | **本次行动**实例：`PrimaryTarget`、`SecondaryTargets` | **单次** cast | 普攻若走 Context 同上；技能在 `TryBeginCast` 前组装 |
| **`ITargetAcquisitionService`（方案 A）** | 几何/规则 → **Hits**（无状态，不写黑板不绑 Context） | 随调随用 | **普攻出手前 / 技能确认前**共用 |

**硬规则**：AoE / Cone / Line 批量命中落在 **`SecondaryTargets`**（含 Primary 约定）；**不写满黑板多槽**。

---

### 2.5 执行顺序（在统一骨架下的「分型」口述）

1. **输入/UI**：单体 → **黑板或 Hint**；范围 → UI 几何（P1+）。  
2. **`Acquire`** → `TargetAcquisitionResult`。  
3. **技能分枝**：拼装 **`SkillCastContext`** → **`TryBeginCast`** → Resolver → Pipeline。  
4. **普攻分枝**：**`Commit`**（§2.7）→ **`ICombatImpactDispatch`**（或等价 **一步技能表**）；目标态 Strike **读黑板** 或与 Commit **同一 id**。  
5. **`ImpactSystem`**：仍是 HP 与其它战斗真理源的 **汇合点**。

---

### 2.6 NPC / 玩家与写入权

| 通道 | 黑板 | Context |
|------|------|---------|
| **NPC** | 索敌后 Commit | AI 托管施法：**Acquire→Context→TryBeginCast** |
| **玩家** | 点选 Commit | **确认键**拼装 Context **或** 走普攻短路径 |

**写入权**：玩家操控帧内 **仅输入** 抢写黑板主攻；托管后仅 AI，防抖动。

---

### 2.7 主攻黑板与 Strike（与塔对齐，可选但很值得）

**意图**：**索敌先有结果 → 记在 `AttackTargetEntityId` → Strike 再读黑板**（或 Dispatch 与该 id **强制一致**），与塔、野区一致，便于 UI/Opcode/复盘。

#### 2.7.1 单位类型对照

| 单位 / 分枝 | 索敌 | 写 `AttackTargetEntityId` | Strike |
|-------------|------|---------------------------|--------|
| **塔** | `CombatTargetAcquire` | ✓ | 读板 → Impact |
| **野怪** | Leash 内选敌 | ✓ Pursue | 与板一致 |
| **英雄普攻（目标态）** | `Acquire` | ✓ Strike 前 **Commit** | **读板** **或** 与 Commit id 对齐 |
| **英雄（过渡期）** | 同左 | 可与 Dispatch 并行，**必须同 id**（技术债） |

#### 2.7.2 仓库现状核对

| 项 | 结论 |
|----|------|
| **塔 / 野区** | 已：写板 → 出手与板一致 |
| **`DefaultCombatImpactDispatch`** | 当前可能 **不写/不读**板，仅以入参 `victim` 投 Impact |
| **`DefaultTargetAcquisitionService`** | **无状态**不写板（OK）；应由 **分枝**调用 **Commit** |
| **点选写板** | `CombatBoardRaySelectTarget`、`CombatBoardTargetSync` 仅有 **点击** |

#### 2.7.3 推荐契约（仅文档）

| 契约 | 说明 |
|------|------|
| **`CommitPrimaryAttackTarget`** | 写 `AttackTargetEntityId`；可同步 `ThreatTargetEntityId`（毕设简化） |
| **`TryResolveStrikeVictimFromBoard`** | 读板 → Registry → 校验 → 供 ECS 节拍或 Dispatch |
| **`CombatTargetAcquire`** | **算法复用**：成功后可 **Commit**；或 Acquire 可选 **CommitToCombatBoard**（P1 选项） |

#### 2.7.4 分阶段（MVP′ / P1 / P2）

- **MVP′**：任一普攻路径在 **`CreateImpactEvent` 前** `AttackTargetEntityId = victim.Id`（与入参一致）；最近敌在 **Acquire 成功后立刻 Commit**。  
- **P1**：Dispatch **仅从黑板解 victim** **或** 入参与板不符则告警/失败；**Acquire+Commit** 文档化为 AI/输入共用步骤。  
- **P2**：普攻 **时间表 HitFrame** 只读板；Commit 与回放 tick 对齐。

---

## 3. 接口与数据契约（A + C）

命名空间实现可与工程一致（如 **`Gameplay.Combat.Targeting`**）。

### 3.1 `TargetingShapeKind`

| 值 | 含义 | MVP | P1 | P2 |
|----|------|-----|----|----|
| `PointEntity` | 单体（黑板 id / 射线） | ✓ | ✓ | ✓ |
| `NearestInSphere` | 施法者球心 + `AtkDistance` 最近敌 | ✓ | ✓ | ✓ |
| `GroundCircle` | 地面圆 + 半径 | — | ✓ | ✓ |
| `Cone` | 原点 + 朝向 + 角 + 深 | — | 可选 | ✓ |
| `Line` | 线段/扫掠 | — | 可选 | ✓ |

### 3.2 `TargetAcquisitionRequest`（`readonly struct`）

| 字段 | 类型 | 说明 |
|------|------|------|
| `Shape` | `TargetingShapeKind` | 解释分支 |
| `Caster` | `EntityBase` | 非空 |
| `PrimaryEntityIdHint` | `long` | **`AttackTargetEntityId`** 提示，0 无 |
| `WorldOrigin` | `Vector3` | 脚点 / 落点 / 弧心 |
| `WorldDirection` | `Vector3` | Cone/Line 向 |
| `RangeOrRadius` | `float` | ≤0 常用 `AtkDistance` |
| `SecondaryParam` | `float` | 半角 / 线宽等 |
| `IncludeDead` | `bool` | 默认 false |

### 3.3 `TargetAcquisitionResult`

| 成员 | 类型 | 说明 |
|------|------|------|
| `Succeeded` | `bool` | — |
| `Error` | `string` | — |
| `Hits` | `IReadOnlyList<EntityBase>` | **稳定顺序** |
| `SuggestedPrimary` | `EntityBase` | 填 `PrimaryTarget` 用 |

### 3.4 `ITargetAcquisitionService`

```csharp
public interface ITargetAcquisitionService
{
    /// <summary>无状态；不写黑板、不写 Context。与 §2.3 前两步一致。</summary>
    TargetAcquisitionResult Acquire(in TargetAcquisitionRequest request);
}
```

### 3.5 `ICombatBoardTargetSync`

| 方法 | 职责 |
|------|------|
| `TryGetPrimaryAttackTarget` | 读 `AttackTargetEntityId` → Registry |
| `SetPrimaryAttackTarget` | 写 `AttackTargetEntityId`（需组件） |

MVP：**`CombatBoardTargetSync`** 静态类。

### 3.6 `SkillCastContext` 拼装（技能分枝 · 闸门后）

| 场景 | `PrimaryTarget` | `SecondaryTargets` |
|------|-----------------|-------------------|
| 单体技能 | `SuggestedPrimary` / `Hits[0]` | **清空** |
| 纯 AoE | **null** / Caster（表规定） | **`Hits`** |
| 主目标+溅射（P2） | 主 | 副列表 |

点选延续：**`TryGetPrimaryAttackTarget`** → `PrimaryEntityIdHint`；**AoE 列表不入黑板**。

### 3.7 `ICombatImpactDispatch`（普攻分枝 · 短路径）

```csharp
bool TryDispatchNormalAttack(EntityBase attacker, out string error);
```

仅读取攻击者 **`CombatBoardLiteComponent.AttackTargetEntityId`** 解析受害者；须经 **`MeleeStrikeRules`**（与索敌共用）；**Impact** 经由 **`EcsWorld.CombatImpacts`** 常驻实例。

与塔 Strike 参数对齐（物伤 / `NormalAtk` / `Subtract` / `Hp`）。**若普攻改为一技能一步表**，本接口可由表驱动替代，**须在项目 README 冻结唯一主路径**。

---

## 4. MVP 阶段 — A+C 最小闭环

**目标**：统一骨架下 **`Acquire` → 普攻分枝 `Dispatch` → Impact**；形状 **PointEntity + NearestInSphere**。

| 编号 | 交付物 | 脚本位置 / 类 |
|------|--------|----------------|
| M1 | `DefaultTargetAcquisitionService` | `.../Targeting/DefaultTargetAcquisitionService.cs` |
| M2 | `DefaultCombatImpactDispatch` | `.../DefaultCombatImpactDispatch.cs` |
| M3 | `MvpHeroBasicAttackDebugBridge` | …（默认 **Space**：最近敌；**Return**：读黑板 Hint；以 Inspector `KeyCode` 为准） |
| M4 | `CombatBoardRaySelectTarget` | **`LayerMask` + 射线**（默认掩码会去 **UI Layer**）；**`EventSystem` 上挡 UI**；需 **Collider** |
| — | **共用组件** | `CombatTargetingRange`、`HostileTargetPicker`、`MeleeStrikeRules`、`HostileAcquisitionCombatBoardAlign` |
| — | **常驻 Impact** | `EcsWorld.CombatImpacts`（与塔、野区共用 **`ImpactManager`**） |
| — | 类型与契约 | `TargetingShapeKind`、`TargetAcquisition*`、`CombatBoardTargetSync`、`SetAttackAndThreatSameTarget` |

**场景挂载**：`EntityBase` + **`CombatEntitySpawnProfile`（黑板）** + Debug 桥；敌 **Collider + EntityBase + 阵营**；点选场景中需有 **`EventSystem`**（可无 UI Canvas，仅占位射线 UI 拦截）。  
**与 §2.7**：Debug 桥在 **Acquire 成功后 `SetAttackAndThreatSameTarget`** 再 **`TryDispatchNormalAttack(attacker)`**（Dispatch **只认黑板**）。  
**验收**：敌对互殴、`LastDamage`、击倒可查。

---

## 5. P1 阶段

**`SkillCastGate`**：`Acquire → Context → TryBeginCast`；**GroundCircle**；**CastRange**；黑板接口化、`Threat` 与 Opcodes 出站清单；与本节 **§2.3～2.7** **同一 Acquire 前缀**分叉到技能分枝。

---

## 6. P2 阶段

Cone/Line、主次溅射、**普攻 Timeline**（与塔类比）、Context 快照 B、Resolver D、Command E——均为 **§2.3** 执行层換件。


---

## 7. 端到端对照（统一到 §2 图）

### 7.1 分枝 A：普攻短路径（当前 MVP 主推）

```
Acquire → （§2.7 Commit）→ TryDispatchNormalAttack → ImpactSystem → LastDamage / Vitality
```

### 7.2 分枝 B：技能表驱动（已有 + P1 Gate）

```
Acquire → 填 SkillCastContext → TryBeginCast → Resolver → Pipeline → Opcode/Buff →（可选）Impact
```

两枝 **共用前两格**（输入意图 + Acquire），分叉在 **闸门之后的效果层**，与 **§2.2** 「配方不同」一致。

---

## 8. 与现有代码的映射

| 模块 | 角色 |
|------|------|
| `ImpactSystem` | 技能与普攻瞬时伤的 **汇合结算** |
| `SkillCastPipelineSystem` | **技能分枝**执行层 |
| `DefaultTargetResolver` | 消费 Context；几何不进 Resolver |
| `CombatBoardLite` | **持续主目标**（职责 **§2.4**；与塔 Strike 对齐 **§2.7**） |
| `Gameplay.Combat.Targeting` | **Picker + `MeleeStrikeRules`**；Dispatch **仅从黑板 Strike**；`HostileAcquisitionCombatBoardAlign`；点选 Layer（UI 过滤后续） |
| `EcsWorld.CombatImpacts` | 塔 / 野区 / 普攻 / Buff opcode **`ImpactManager`**；**首次访问**时创建（非 ECS System） |
| `TowerCombatCycleSystem` / `JungleAiSystem` | **`Initialize`** 绑定 **`CombatImpacts`** |

---

## 9. 仓库现状清点

技能管线 Context + Resolver 仍在。**英雄普攻 MVP**：已实现 **Acquire→黑板 Commit→Dispatch 读板**，与塔同构更近一步；**ImpactManager** 全工程经 **`EcsWorld.CombatImpacts`** / **`ImpactManager.Shared`**。**仍缺**：`SkillCastGate`、地面 AoE 几何。

---

## 10. 备选架构（简表）

| 方案 | 用途 |
|------|------|
| **B** | Context 内几何快照 + Hydrate |
| **D** | Resolver 策略与 JSON 绑定 |
| **E** | Command 预览/执行 |

均在 **§2.3** 骨架之外换「零件」，不推翻合并叙事。

---

## 11. 文档修订记录

| 版本 | 日期 | 说明 |
|------|------|------|
| … | （v1～v2.0.x：A+C、MVP代码、黑板 Strike 对齐等，见历次提交） |
| **3.0.0** | **2026-04-17** | **重构**：§2 统一战斗行动模型等（见前文）。 |
| **3.0.1** | **2026-04-17** | Dispatch **仅从黑板**；**Picker/Rules/Range** 合并校验；点选 UI/Layer；**CombatImpacts**；`SetAttackAndThreatSameTarget`。 |
| **3.0.2** | **2026-04-17** | **`CombatImpacts` 懒创建**；`BuffOpcodeDispatcher` 与 **`ImpactManager.Shared`** 对齐单例；点选暂不做 UI 拦截。 |
