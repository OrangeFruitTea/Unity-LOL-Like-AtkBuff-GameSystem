# MOBA 水晶（核心防御塔）与对局终局裁定 — 设计（轻量化）

| 项 | 内容 |
|----|------|
| 文档版本 | 1.0 |
| 排期假定 | **约 1 日**：仅实现「水晶 hp 归零 → 裁定胜负」最短路径 |
| 引擎与架构 | Unity；局内 ECS 参见 **`MOBA局内单位模块ECS设计文档.md`**；击倒链见 **`UnitVitalitySystem`** → **`CombatUnitDeathRelay`** |
| 关联文档 | **`MOBA对局信息与胜负流程模块设计文档.md`**（**V1**、阶段 `Playing→Settling`）；**`毕设演示-端到端场景与界面流转.md`**；塔挂载模式见 **`TowerEcsAttachments.cs`** |

---

## 1. 设计意图（一句话）

**水晶**仍是 **防御塔Prefab 的一种配置变体**：在已有 **塔 ECS（`TowerModuleComponent` + 节拍组件）与 `EntityData` 生命值** 上，额外挂 **极小「水晶裁定」ECS 标记 + 单场桥接**，当 **`CrtHp` 降至阈值以下**并经 **`CombatUnitDeathRelay`** 播报死亡时，**触发对局胜负（敌方水晶毁 → 本方胜）**，且全局 **只可裁定一次**。

---

## 2. 与现有击倒链对齐（必选理解）

无需为水晶单独写「死亡系统」，复用以下顺序（工程中已存在）：

1. **`ImpactSystem`** 等扣 **`EntityDataComponent`** 生命值；
2. **`UnitVitalitySystem`**（`UpdateOrder≈32`）发现 **`CrtHp≤ε`**，回填 **`CombatBoardLiteComponent`** 击杀者占位并 **首次** 调用 **`CombatUnitDeathRelay.AnnounceFirstDeath`**；
3. **`AnnounceFirstDeath`** 触发 **`UnitDeathEventHub`** 与 **`GameEventBus`** 上的 **`CombatUnitDiedGameEvent`**（含 **`VictimEntityId`**、**`KillerEntityId`**）。

**水晶终局监听方**应挂载在 **`CombatUnitDeathRelay` 成功之后可读 ECS 的一致性时刻**：推荐 **订阅 `CombatUnitDiedGameEvent`**（与同帧 **`GameEventBus` 派发** 对齐）；若暂未接总线，可退化为 **`UnitDeathEventHub` C# event**（与文档 **`MOBA胜负`** §单一裁定入口 一致思想）。

```
UnitVitalitySystem → CombatUnitDeathRelay → GameEventBus(CombatUnitDiedGameEvent)
                              ↘ UnitDeathEventHub（可选第二条路径）
水晶裁定桥接订阅上述事件之一 ──► Match/V1 裁剪实现
```

---

## 3. 胜负语义（两方 MOBA）

| 规则项 | 约定 |
|--------|------|
| **裁定条件** | 实体 **同时具备**：① **`CrystalCoreObjectiveComponent`**（或小名 **`CrystalObjectiveTag`**）；② **`EntityData` 击倒**已由 **`AnnounceFirstDeath` 派发**。**一次对局至多裁定一次**（重复事件忽略）。 |
| **落败方** | 水晶 ECS 上所写的 **`OwningTeamId`**（与本实体 **`FactionComponent.TeamId`** 一致，便于策划自检）。 |
| **获胜方** | 当前对局仅存 **`1`/`2` 两方**时：`winnerTeam = opponent(OwningTeamId)`。**中立水晶**不适用（毕设裁剪掉）。 |
| **阶段门控** | 仅在 **`Playing`**（或等价：未处于 `Settling`/`Finished`/`Aborted`）时接受裁定；与 **`MOBA胜负`** §4.1 一致。 |
| **`KillerEntityId==0`** | 仍可裁定 **陨落阵营**（如毒圈/dot 无显式凶手）；胜者队伍若无法从 ECS 推导，可先记 **`winnerTeam`** 仅凭水晶所属对方，**不向论文承诺击杀牌面**。 |

---

## 4. ECS 组件（最小）

仅 **一个** 标记型 **`struct`**，**不缓存 Unity 引用**、不写逻辑：

### 4.1 `CrystalCoreObjectiveComponent`

| 字段 | 类型 | 说明 |
|------|------|------|
| `OwningTeamId` | `byte` | **该水晶所属的防守阵营**（被摧毁则由 **另一方** 获胜）。与 **`FactionTeamId` 数值**（及 **`FactionComponent.TeamId`**）一致。 |

- 实现 **`IEcsComponent`**，提供 **`InitializeDefaults()`**（默认 **`OwningTeamId = (byte)FactionTeamId.Blue`**，水晶 Prefab 在 Inspector **覆盖**）。
- **不**在此处存最大血量（仍走 **`EntityData`**）；**不**存网络同步字段（单机 Demo 冻结）。

**查询**：击倒事件中用 **`VictimEntityId` → `EcsWorld.Lookup`/`GetEntity`**，**`TryGetComponent<CrystalCoreObjectiveComponent>(out var c)`** 即判定是否为水晶。

### 4.2 与一般防御塔的关系

| 原型 | ECS |
|------|-----|
| 普通防御塔 | 已有 **`TowerModuleComponent`** + **`TowerCombatCycleComponent`** + **`FactionComponent`** + **`EntityData`**…（见 **`TowerEcsAttachments`**） |
| 水晶 | **在上述基础上** **`AddComponent<CrystalCoreObjectiveComponent>`**；仍保留塔普攻逻辑（可配射程/伤害为 0 作纯木桩，由表驱动，**不属于本文档必选**）。 |

---

## 5. 「一般塔变成水晶」的挂载方式（二选一，1 日推荐 A）

### 方案 A（推荐）：独立薄 Mono **`CrystalObjectiveEcsAttachment`**

| 做法 | 说明 |
|------|------|
| 已实现 **`CrystalObjectiveEcsAttachment : MonoBehaviour, IEntitySpawnExtension`**（与 **`TowerEcsAttachments`** 同模式），**`OnAfterEcsBaseSpawned`** 内 **`EcsWorld.AddComponent(ecs, CrystalCoreObjectiveComponent)`** |
| **`owningTeam`**（Inspector） | 使用 **`FactionTeamId`**（`Blue`/`Red`），写入 **`OwningTeamId = (byte)FactionTeamId`）；**`Neutral`** 时会 **跳过挂载** 并打日志。 |
| **Prefab** | 通用塔 Prefab **拷贝**一份「水晶」，在水晶上 **额外挂** **`CrystalObjectiveEcsAttachment`**，并保留 **`TowerEcsAttachments`**（及阵营 Profile）；**不修改** `TowerEcsAttachments` 源码。 |

**优点**：一天内 **零侵入** 现有塔脚本；策划拖组件即可。

### 方案 B：扩展 **`TowerEcsAttachments`**

在 **`TowerEcsAttachments`** 增加 **`[SerializeField] bool registerAsCrystalCore`** + **`crystalOwningTeam`**，为真时在 **`OnAfterEcsBaseSpawned`** 末尾 **Add** 水晶组件。

**优点**：单组件Inspector；**缺点**：要改已通过测试的 **`TowerEcsAttachments`**，回归成本略高。

---

## 6. 终局桥接（非 ECS System，单日推荐）

为避免再注册 **`IEcsSystem`** 与 **`UpdateOrder` 耦合**，采用 **事件订阅 + 单次门闩**：

| 条目 | 约定 |
|------|------|
| 形态 | 场景常驻 **`CrystalMatchOutcomeBridge`**（`MonoBehaviour`）或 **`static`** 初始化 **`GameEventBus` 订阅**（二选一）；**不推荐**再在 **`UnitVitalitySystem` 内**写水晶分支（违反单一职责）。 |
| 输入 | **`CombatUnitDiedGameEvent.VictimEntityId`** |
| 步骤 | ① 查 victim 是否含 **`CrystalCoreObjectiveComponent`**；否 → 返回。② **若对局已裁定** → 返回。③ 读 **`OwningTeamId` → `losingTeam`**，算 **`winningTeam`**。④ 调 **`MatchFlow` / 占位 API**（见 §7）。 |
| 幂等 | **`Interlocked.CompareExchange` / `bool _ended`** 保证 **一次对局只进一次 `Settling`**。 |

---

## 7. 与 `MatchFlow` / 结算的对接（工程缺省时的降级）

| 工程状态 | 行为 |
|----------|------|
| **已有** `MatchPhase` + `MatchVictoryDecided` 事件 | 桥接内 **`Publish(MatchVictoryDecided{ WinnerTeam, Reason=CrystalDestroyed, LoserCrystalEntityId })`**，由 **`MatchFlow`** 转 **`Playing→Settling`**。 |
| **尚未实现** MatchFlow（仅论文有） | **P0 降级**：`Debug.Log` + **`Time.timeScale=0`** + **简单全屏 Text / UIManager 弹窗**；仍保持 **单一桥接类** 便于次日替换为正式编排。 |

**不得**在水晶 Prefab 上分散写 **`OnDestroy` 判负**（与 ECS 击倒顺序、对象池、特效延迟销毁均可能竞态）。

---

## 8. 与现有文档索引的修正

- **`MOBA对局信息与胜负流程模块设计文档.md`** §5.2 **V1** 中「[`MOBA水晶与对局终局裁定设计文档.md`](MOBA水晶与对局终局裁定设计文档.md)」即 **本文**。
- **事件名**：若论文需统一，可将 **`CoreObjectiveDestroyed`** 作为 **`MatchRules`** 层对 **`CombatUnitDiedGameEvent` + Crystal 组件** 的 **语义别名**（实现时一次 map 即可）。

---

## 9. 验收清单（1 日版）

| # | 项 |
|---|-----|
| 1 | 蓝方水晶 **`OwningTeamId=蓝`**，红方摧毁后进入 **胜/负** 或 **Settling**（与本地玩家阵营一致） |
| 2 | 普通外塔被毁 **不** 触发终局 |
| 3 | 双杀/多段伤害仅 **首次** `AnnounceFirstDeath` 对应 **一次** 终局（不重复弹窗） |
| 4 | **`Playing` 外** 不裁定（若 MatchFlow 未接，至少在桥接内做 **`bool matchActive`** 门控） |
| 5 | Prefab：水晶 = 塔Prefab + **`CrystalObjectiveEcsAttachment`**（方案 A） |

---

## 10. 可选扩展（明确不做，防范围膨胀）

- 双水晶同时毁、平局、暂停中毁塔 — **P2**；
- Mirror 主机唯一裁定 — 见 **`MOBA胜负`** §10.2；
- 水晶无敌盾、复活 — **非本日**。

---

## 11. 工程实现（已实现脚本）

| 文件（`Scripts/Gameplay/Entity/Crystal/`） | 说明 |
|------------------------------------------|------|
| `CrystalCoreObjectiveComponent.cs` | ECS 标记：`OwningTeamId`（byte，与 **`FactionTeamId`** 对齐） |
| `CrystalObjectiveEcsAttachment.cs` | 方案 A：生成后注入水晶组件；**`FactionTeamId owningTeam`** |
| `CrystalMatchOutcomeBridge.cs` | 订阅 **`CombatUnitDiedGameEvent`**，门闩 +（可选）**`timeScale=0`** + 发布 **`CrystalCoreDestroyedMatchEndGameEvent`**；**`ResetMatchEndLatch()`** 供新局重置 |
| `CrystalCoreDestroyedMatchEndGameEvent.cs` | 终局语义事件，供未来 MatchFlow 订阅 |

**场景步骤（需在 Unity 内）**：放 1 个 **`CrystalMatchOutcomeBridge`**；蓝/红水晶塔 Prefab 在原有塔扩展上 **加** **`CrystalObjectiveEcsAttachment`** 并选对阵营；新局开始前调用 **`ResetMatchEndLatch()`**（或由后续 MatchFlow 统一调用）。

---

## 12. 修订记录

| 版本 | 日期 | 摘要 |
|------|------|------|
| 1.0 | 2026-05-07 | 初稿：水晶 ECS 单组件、事件桥接、塔Prefab复用、与 V1/击倒链对齐 |
| 1.1 | 2026-05-07 | 增补 §11：`Crystal*` 脚本实现、`FactionTeamId` Inspector、终局 GameEvent |
