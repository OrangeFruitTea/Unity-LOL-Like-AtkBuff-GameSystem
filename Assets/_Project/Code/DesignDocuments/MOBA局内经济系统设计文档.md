# MOBA 局内经济系统设计文档

| 项 | 内容 |
|----|------|
| 文档版本 | 1.0 |
| 适用引擎 | Unity 2022 |
| 文档类型 | 模块设计（Economy / MOBA In-Match） |
| 关联系统 | **ECS**（`Core.ECS`、`EcsEntity`、`IEcsComponent`）、`EntityBase` 与 **实体桥接**、结算与战斗事件、**商店/货币门面**（`ICurrencyWallet`、`PurchaseService`）；与《MOBA局内装备与商店模块设计文档》**单局金币消费侧**对接 |
| 核心决策 | **局内金币增减**与「谁持币、谁入账」在逻辑上集中为**可测试的经济服务**；**可被击杀单位**的掉落/赏金参数尽量以 **ECS 组件 + 静态/表配置 id** 驱动，避免散落在各 `MonoBehaviour` |

---

## 1. 引言

### 1.1 目的

本文档约定 **MOBA 单局内经济子系统**的职责边界、数据模型与推荐实现路径，用于：

- 将 **击杀/助攻等带来的金币收入** 与 **商店购买、出售退票** 等对接到**同一货币语义**（局内通常为「金币」）；
- 说明如何利用 **ECS 组件** 在角色或可击杀单位上挂载**赏金/掉落相关数据**，便于与现有 **`EntitySpawnSystem`**、`EntityBase.BoundEcsEntity` 架构一致；
- 为多人对局预留 **权威结算、回放与校验** 的扩展点。

### 1.2 读者对象

- Gameplay / 战斗与结算程序员；
- 数值与关卡策划；
- 网络同步（主机/服务端）设计者。

### 1.3 术语表

| 术语 | 定义 |
|------|------|
| 局内金币（Gold） | 单局流通货币；用于商店等消耗；存档规则由产品另行规定。 |
| 钱包（Wallet） | `ICurrencyWallet` 抽象的扣费/退款与余额快照；可多实例（每名玩家/每名英雄）。 |
| 赏金主体（Bounty Subject） | 被击杀时可产生「基础击杀金 + 可变部分」的规则承载者（常见为英雄）。 |
| 掉落配置引用（Economy Profile Id） | 指向静态表的 id，解析出基础赏金、成长项、是否需要分摊助攻等 **与关卡强相关** 的数值集合。 |

---

## 2. 范围与约束

### 2.1 范围内（In Scope）

- 局内金币的 **入账**（击杀、助攻分成、兵线/野怪/工资等在架构上的统一归类）；
- **击杀类收入**的实现要点：如何从 **受害者** 读出赏金上下文、如何分配给 **击杀者与助攻者**；
- ECS 组件与系统更新的大致挂载点（与 `EntitySpawnSystem`、`EcsWorld` 一致），**不写死**具体帧序与 Tick（由实现选型）；
- 与商店模块的 **`ICurrencyWallet`** 在「单英雄/玩家钱包」语义上的对齐与迁移路径。

### 2.2 范围外（Out of Scope）

- 局外商业化、账号货币、开箱与付费链路；
- 完整反作弊与经济风控策略细则；
- 具体网络协议字段与序列化格式（仅保留**权威顺序、幂等、重放**等设计约束）。

### 2.3 设计原则

1. **入账与扣费对称**：任何 `TrySpend` 失败不得改变余额；入账应可解释（来源类型、参数快照），便于日志与复盘。
2. **配置驱动**：基础赏金、助攻比例、补刀/工资表等以 **JSON/表** 为主，逻辑层只做解析与执行。
3. **战斗事实单一来源**：「谁击杀了谁」应以**战斗结算**或 **权威伤害/最后一击判定**为准，经济模块不重复推导击杀关系。
4. **ECS 作数据挂载、系统作批量规则**：击杀金可随单位类型变化；用 **组件存 id/轻量状态**，用 **EconomySettlementSystem（建议名）** 订阅死亡事件并调用钱包。

---

## 3. 与现有工程的关系

### 3.1 商店与货币（现状）

`Gameplay.Shop` 中 **`PurchaseService.Currency`** 当前为全局 **`ICurrencyWallet`**（默认 `DebugCurrencyWallet`），适合单机验证商店链路。

**目标形态**：每名操作主体（常为 **操控的英雄 → 映射到本地/服务器玩家钱包**）持有一个 **`ICurrencyWallet`** 实例，或由 **`IPlayerEconomyRegistry`**（建议名）用 `heroEntityId` / `playerSlot` 映射到钱包。经济模块入账时写入**对应主体的钱包**，商店扣费与该钱包对齐。

### 3.2 ECS 挂载点（现状）

场景中实体通过 **`EntitySpawnSystem.SpawnEcsEntity`** 创建 `EcsEntity`，并添加 **`EntityDataComponent`**（见 `AddBaseComponents`）。**新增的与经济相关的组件**应在同一生命周期内挂载到同一 `EcsEntity`，或由专用初始化逻辑按需 `EcsWorld.AddComponent`，保证可被 `EcsEntityManager.GetEntitiesWithComponent<T>()` 等查询。

---

## 4. 金币来源的分类（概要）

下列来源在实践中可共用 **`EconomyGrantRequest`**（建议 DTO：`source`、`amount`、`reasonCode`、上下文 id）一类结构，便于日志与断言：

| 来源类型 | 说明 | 与 ECS 的常见关系 |
|---------|------|-------------------|
| 击杀/助攻 | 对英雄或可配置单位的击杀结算 | **受害者组件 + 击杀者身份**来自战斗事件载荷 |
| 兵线 / 防御塔赏金 | 单位死亡时派发 | 小兵/塔的 **EconomyProfileId** |
| 野怪 | 最后一击者与团队规则 | Profile + 可能与「团队平分」绑定 |
| 工资 / 周期性收入 | Tick 入账 | **不必然** ECS；可与玩家会话状态绑定 |

本文 **第四～五节**着重 **击杀**，其余来源在配置上复用同一套 **Profile**，仅触发系统不同。

---

## 5. 击杀掉落金币：ECS 组件方案（推荐）

### 5.1 思路

为避免在多个脚本上手写常量，建议在 **可被击杀、且参与赏金规则的实体**上挂载 **轻量 ECS 组件**，表达：

- 使用哪套 **经济配置**（`economyProfileId` / `bountyTableId`）；
- 运行时状态（如 **连杀层数**、**最近是否被同一英雄连续击杀** 等，若产品需要），或仅 **Profile + 战斗状态机** 外置。

**实现约束**：组件遵循项目 **`IEcsComponent`** 约定，含 **`InitializeDefaults()`**；数据以 **值类型 struct** 为主，与现有 `EntityDataComponent` 风格一致。

### 5.2 组件示例（逻辑字段名，非强制工程名）

| 组件（建议名） | 职责 |
|----------------|------|
| `EconomyKillBountyBindingComponent` | 绑定 **`economyProfileId`**（int），由 **静态表** 解析基础击杀金、死亡赏金下限/上限公式系数等。**小兵/英雄/精英**可共用不同类型 id。 |
| `EconomyKillBountyRuntimeComponent`（可选） | 运行时变更：本次对局内的 **连杀档位**、`LastKilledBy`、`Shutdown` 计数等仅在需要「动态赏金公式」时使用；若全部走表公式 + GlobalState，可不设。 |

**英雄**在 Spawn 或配置驱动下挂上 **`EconomyKillBountyBindingComponent`**；受害者死亡时，结算系统读取 **受害者绑定 id** + （可选）**受害者 runtime**，结合 **凶手**身份与助攻列表，算出 **总赏金与各分成**。

### 5.3 为何不直接把「掉落金币数额」写死在角色 Prefab

- 同一英雄在不同模式/赛季需调表，**ProfileId** 比到处改 Magic Number 更安全；
- 助攻比例、团队分享、塔下补刀修正等属于**跨系统规则**，放在 **EconomySettlementSystem + 表** 中比分散在角色脚本更易测。

若 **极度简化的 Demo** 仅需固定数，仍建议 **Profile 表里只有一行**，与正式方案同构。

---

## 6. 击杀结算流程（逻辑顺序）

1. **战斗层**产生权威事实：`VictimRef`、`KillerRef`、`AssistList[]`、`KillTime`、`KillLocationType`（对线/野区等，可选）。
2. **EconomySettlementSystem（或等价）** 订阅该事件：
   - 从 **受害者 ECS** 读取 `EconomyKillBountyBindingComponent`（及可选 Runtime）；
   - 根据 **Profile 表 + 全局规则**（助攻分配、首杀、连杀奖励/终止）计算 **总金额**；
   - 将击杀者份额、助攻份额分别 **`TryRefund` 到对应玩家/英雄钱包**（入账语义上建议用统一 `GrantGold` API，内部仍可为 `TryRefund` 或扩展 `Wallet` 接口）。
3. **表现层**：飘字、音效可监听同一领域事件 **不反向改账**。

**禁止**：在未确定助攻列表与受害者身份前，仅从「伤害总量」臆造分配（与产品规则冲突时以策划表为准）。

---

## 7. 钱包接口与商店对接

### 7.1 接口形态

当前工程已有：

```text
ICurrencyWallet : TrySpend, TryRefund, CurrentBalanceSnapshot
```

**建议扩展（设计层，非强制本次编码）**：

- `TryGrant(int gold, GrantContext ctx)` 或至少统一 **入账** 命名，避免生产代码误用 `TryRefund` 语义表示「系统发钱」带来的阅读成本。
- 多钱包时：`PurchaseService` 不再使用**单一静态** `Currency`，改为 **`Func<EntityBase, ICurrencyWallet>`** 或 **注册表按 playerId 解析**（与装备模块的「谁付款」一致）。

### 7.2 与《MOBA局内装备与商店模块设计文档》的边界

- **商店文档**负责：购买/出售/合成链路上的 **扣费与失败回滚**；
- **本文档**负责：**金币从哪来**、**入哪个钱包**、**击杀与 ECS 数据怎么连**。

二者通过 **同一 `ICurrencyWallet` 抽象**在局内汇合。

---

## 8. 多人与权威

- **单机/ListenServer**：可在主机顺序执行结算与入账。
- **专用服务器**：仅 **服务器** 修改权威钱包；客户端可 **预测飘字**，但不得作为扣费/入账依据。
- **重连**：钱包余额应属于 **可同步快照** 的一部分；击杀事件需 **单调序号** 或 **事件 id** 防重复入账。

---

## 9. 配置与版本

- **Profile 表**建议 `StreamingAssets` + 与现有 Json 管线一致，包含 `schemaVersion`；
- **平衡性补丁**可与 **装备表** 同源发布，但在文档与 CI 上**分开校验**规则集。

---

## 10. 可测试性与可观测性

- **单元测试**：给定受害者 Profile + Killer/Assists → 分出金额恒定；
- **集成测试**：死亡事件 → 钱包余额增量与日志 `reasonCode` 一致；
- **日志必选字段**：`victimEntityId`、`killerEntityId`、`profileId`、`totalGold`、`economyEventId`。

---

## 11. 风险与缓解

| 风险 | 缓解 |
|------|------|
| 全局 Debug 钱包与多英雄错乱 | 尽快引入 **按 Hero/Player** 映射的 `ICurrencyWallet` |
| 助攻列表与击杀不同步 | 结算仅信 **权威载荷** |
| ECS 组件未随单位回收 | **DestroyEntity** 时清理或池化文档化 |

---

## 12. 文档维护与交叉引用

- 装备与商店：**《MOBA局内装备与商店模块设计文档》**；
- 属性与 ECS：**《角色属性系统设计文档》**、**《基于Buff与ECS的技能系统设计文档》**；
- 网络同步接口（若已落地）：**《网络同步服务脚本接口设计文档》**。

更新经济规则时，请同步 **Profile 表 schema** 与本节 **击杀流程** 的说明，避免实现与策划表漂移。

---

## 13. 实施路线（建议）

| 阶段 | 目标 |
|------|------|
| MVP | 单钱包或按玩家 id 分钱包；受害者 **EconomyKillBountyBindingComponent** + 常量表入账；监听死亡事件 |
| P1 | 助攻分成、连杀、首杀；入账 API 语义整理；UI 播报 |
| P2 | 兵线/野怪统一 Profile；联机快照与防双花 |
