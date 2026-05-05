# Buff Opcode 式效果列表与触发语义设计文档

| 项 | 内容 |
|----|------|
| 文档版本 | 1.4 |
| 关联文档 | 《基于Buff与ECS的技能系统设计文档》、《MOBA局内单位模块ECS设计文档》、Impact 使用指南 |
| 适用引擎 | Unity 2022 |

---

## 1. 「Opcode」是什么（术语）

**Opcode（Operation Code，操作码）** 在本设计中指：**用一个可序列化的代号（枚举整型或短字符串）表示一类「可对单位做的原子动作」**，配套 **固定形状的参数字段**（如伤害基数、持续时间、位移向量 id）。运行时由一个 **小而稳定的解释器（Dispatcher）** 根据 opcode 分发到已有子系统（如 `ImpactManager`、属性修改、`BuffManager` 再入等）。

它解决的是：**少写成百上千个 `BuffBase` 子类**，而把差异留在 **表里**（JSON、ScriptableObject、**等价外置表项**）。

---

## 2. 与现有工程的对应关系与本 Buff 成熟度评价

### 2.1 已具备的基础设施（可归为「中高完成度的壳」）

| 模块 | 水平 |
|------|------|
| `BuffManager` | **较完整**：池化、`BuffConflictResolution`、观察者、`FixedUpdate` 驱动、持续时间与降级规则。适合继续作为 Buff **宿主与调度外壳**。 |
| `BuffBase` / `BuffRuntimeData` | **完整**：Provider、Owner、等级、持续时间、自定义参数管道。 |
| `BuffConfig` / `BuffMetadata` + `BuffDataLoader` + JSON | **有路径**：StreamingAssets 加载静态条；与技能步骤通过 `buffId` 引用可对齐。 |
| 技能侧 `BuffApplicationStepDefinition` | **较完整**：`buffId`、目标选择、`TriggerKind`（立即/延迟/条件/事件占位）、`customArgs`。 |
| `PipelineScheduleBuilder` + `SkillCastPipelineSystem` | **部分落地**：**立即**与 **固定延迟** 步骤可批处理并调用 `BuffApplyService`；**条件**有 `SkillConditionRegistry`；**OnEvent** 当前为桩（日志提示未实现）。 |
| `BuffTypeRegistry` + `TypedBuffFactory<T>` | **机制有、注册空**：工程内 **未见** `BuffTypeRegistry.Register` 调用，表驱动施加在运行时 **易因「无工厂」失败**，需启动时注册或改为「元 Buff + opcode 表」单类型注册。 |

### 2.2 尚未形成「拆解」的部分（与 opcode 文档要补的缺口）

| 缺口 | 说明 |
|------|------|
| **`BuffEffectOpcode` + `BuffOpcodeInstruction` / `BuffEffectComposition` / `BuffOpcodeRecipe`** | 已由扁平 `BuffEffect` **迁移**为：**原子 opcode + 可多条的指令列表**；代码配方用 `BuffOpcodeRecipe` 派生（如中毒/恐惧示例）。需配套 **Dispatcher** 将 opcode 映射到 Impact/属性/CC（仍为落地项）。 |
| **效果组合的「执行回路」** | 已有数据结构 **`BuffEffectComposition`**；尚需 **Dispatcher** 在时间轴（OnApply/Periodic/Remove）上执行 opcode 列表。 |
| **伤害与 Impact** | **无**现成 Buff 调用 `ImpactManager.CreateImpactEvent`；塔等路径走独立系统。opcode 方案将把 **扣血类** 明确收口到 Impact（或经 Impact 统一结算）。 |
| **Buff 运行时触发语义** | 技能层已有 **步骤级** 触发；**Buff 自身**除 `OnGet` / `FixedUpdate` 外，尚未标准化 **OnTick（按 config.frequency）**、**OnStackChange** 等与 opcode 列表的绑定。 |

**总评（毕设尺度）**：**运行时容器与技能编排已有雏形**，**opcode 数据结构（`BuffEffectOpcode`、`BuffOpcodeInstruction`、`BuffEffectComposition`、`BuffOpcodeRecipe`）已进入代码仓**；“可配置的效果语言”尚需 **Dispatcher + JSON 载入 `Composition`**。

---

## 2.3 代码中的组合模型（与 §5、§6 对应）

| 类型 | 作用 |
|------|------|
| `BuffEffectOpcode` | 原子操作码枚举（伤害 / 属性 / CC / 位移 / 再挂 Buff）。 |
| `BuffOpcodeInstruction` | 单条 opcode + 通用参数字段，供表或序列化填充。 |
| `BuffEffectComposition` | **`OnApply` / `OnPeriodicTick` / `OnRemove` 三组列表**，每组内为**多重 opcode**。 |
| `BuffOpcodeRecipe`（抽象） | **子类内含**一组 `BuildInstructions` 规则（可多 opcode）；示例：`PoisonPeriodicDamageRecipe`、`FearMovementLockRecipe`。 |
| `BuffOpcodeListRecipe` | 不写子类时，直接用列表包装的配方。 |

---

## 3. 设计目标

1. **一条逻辑 Buff**（如「中毒」「恐惧」）在数据上表达为：**少量触发规则 + 有序的效果 opcode 列表**（及参数），优先 **JSON/表** 复用，减少 C# 子类爆炸。
2. **触发语义保持「少而够用」**：区分 **（A）技能管线何时把 Buff 挂上去** 与 **（B）Buff 挂上之后何时执行效果**；不在一期引入过多事件类型。
3. **与现有原则兼容**：伤害走 **Impact**；属性修改走 **`EntityDataComponent` 或既有 Impact 属性通道**（二选一对齐项目现状）；控制类尽量 **ECS 标记组件** 或 **集中 ControlState**（实现阶段定稿）。
4. **扩展口保留**：复杂机制仍允许 **专用 `BuffBase` 子类** 或 **自定义 opcode 处理委托**（混合策略）。

---

## 4. 两层触发语义

### 4.1 外层：技能 / 表驱动「何时施加 Buff」（已部分存在）

与现有 `BuffApplicationTriggerKind` 对齐：

| 值 | 含义 |
|----|------|
| `Immediate` | 施法流程到点即 `BuffApplyService.TryApply` |
| `AfterDelay` | 相对施法起点延迟后施加 |
| `OnCondition` | 条件满足时施加（与 `SkillConditionRegistry` 扩展） |
| `OnEvent` | 将来与 `GameEventBus` 等挂钩；**一期可仍用延迟 + 条件替代** |

本层 **不负责** 中毒跳伤害，只负责 **Buff 实例是否出现在目标身上**。

### 4.2 内层：Buff 实例「何时执行 opcode 列表」（本文档新增约定）

在 `BuffConfig` 或并列 **BuffExecutionProfile**（JSON）中定义：

| 触发键 | 建议语义 | 与现有代码的衔接 |
|--------|----------|------------------|
| `OnApply` | 施加成功瞬间执行一轮 opcode 列表（可空） | 对应 `BuffBase.OnGet` 末尾调用 **Dispatcher** |
| `OnPeriodicTick` | 按配置间隔执行 opcode 列表（如 DOT） | **由 §4.4 统一时间轴** 触发后调 Dispatcher；**不**建议与 **`ResidualDuration` 降级节拍**在无约定下混搭成两套独立钟表 |
| `OnRemove` | 移除时执行（常用于「结束时净化」或还原） | `OnLost` |
| `OnStackChanged` | 叠层变化时执行（可选，二期） | `OnLevelChange` 或专用钩子 |

**原则**：内层触发类型 **一期控制在 3～4 个**，避免与技能外层重复造轮子。

### 4.3 `BuffRuntimeData.CurrentLevel` 语义（与本项目对齐）

本项目与毕设交付范围内 **不做**「同一字段兼表技能成长 Buff 等级」的第二套含义。

| 约定 | 说明 |
|------|------|
| **`CurrentLevel` = 叠层（层数）** | 典型用例：**中毒层数**、可叠加减益层数；技能一次施加「x 层」即 **`TryApply`/`AddBuff` 写入的层级**或由 **`BuffConflictResolution.Combine`** 累加的结果。 |
| **无单独的「法术等级」字段需求** | 若日后需要「Buff 强度随技能等级变化」，**另增**字段（例如 `ResolvedSkillBuffGrade`）或把这些乘区 **写进 opcode 参数 / `CustomArgs`**，**不复用** `CurrentLevel` 表示层数之外的语义。 |

与现有 `BuffManager` 冲突解决、降级规则交互时：**凡文档与表称「层」**，均指 **`CurrentLevel`**。

### 4.4 DOT / 层衰减：单一时间轴（规避「双钟表」）

**风险**：若在 **同一 Buff** 上同时用 **`BuffConfig.frequency`（或 Dispatcher 自定累加）** 驱动 DOT，又用 **`ResidualDuration` + `demotion`** 驱动「每 z 秒掉一层」，两套计时若分别绑定 **`Update` / `FixedUpdate` / 协程 Wait**，易出现 **漂移、同帧双跳/漏跳、再施加叠层后两套钟表不同步**。

**定稿（推荐实现形态）**：在 **`MetaBuff`（或挂载于该 Buff 的 Opcode 调度部件）内部**维护 **同一根时间基准**下的 **两个累积量**（名称可随代码调整，语义如下）：

| 状态变量（示例名） | 含义 |
|--------------------|------|
| `SinceLastDot` | 距上次 **DOT / `OnPeriodicTick`** 已过去的时间（秒） |
| `SinceLastStackDecay` | 距上次 **叠层衰减**（层数 −1）已过去的时间（秒） |

- **时间基准**：优先 **`Time.time`**；若要与局内权威时间对齐，改用项目已有 **`MatchTimeService` 等对局时钟**（与《对局时间模块设计文档》一致），**全 Buff 语义层统一选一者**，不在同一 Buff 上混用。  
- **节拍**：表中配置 **`dotIntervalSeconds`**、`**stackDecayIntervalSeconds**`（可映射自原 `frequency` / 毒每层间隔 z）；每帧或每固定推进步：**`elapsed = now - anchor`** 或对 **delta 累加**至阈值后：**执行 Dispatcher → `OnPeriodicTick` opcode**（如 `ImpactDamage`），或对 **`CurrentLevel` 递减**直至为 0 再整体移除 Buff。  
- **与 `BuffManager.ResidualDuration` / `demotion` 的关系**：实现 ** Opcode 毒害类**时，推荐 **本条路径以 §4.4 为唯一节拍源**，**暂不依赖** Manager 自带的「到期降级」承载掉层——避免与设计表中的 **`frequency`/DOT 节拍**混成两套；若坚持用 Manager 降级，须在表与代码注释中 **显式写清两套钟表如何对齐**（非默认推荐）。

**小结**：**一个 Buff 实例上，DOT 与掉层共用同一时间轴 + 两个累积器**；配置层仍可用 JSON 表达间隔，但 **运行层只维护一套推进逻辑**。

---

## 5. Opcode 效果列表（建议最小集）

下列为 **v1 建议 opcode**（整型枚举 + 参数块）；具体 JSON 字段名实现阶段可再冻结。

| Opcode | 职责 | 推荐落地 |
|--------|------|----------|
| `ImpactDamage` | 物理/魔法/真实伤害 | `ImpactManager.CreateImpactEvent`（Source=Provider ECS，Target=Owner ECS） |
| `StatModify` | 修改 `EntityData` 一项或多项 | 直接写组件 **或** 走 Impact 对属性的 `Add/Subtract`（与项目统一） |
| `ControlLock` | 沉默 / 缴械 / 禁锢 / 禁移动 等 bitmask | 写 **ECS 控制组件** 或单例 `CrowdControlState`（二选一定稿） |
| `ForcedDisplacement` | 击退/牵引/恐惧方向移动 | 写 **意向位移** 供 Motor 或 `NavMeshAgent` 桥读；或发 Basement 任务（短位移） |
| `ApplyChildBuff` | 再施加另一条 buffId（慎用） | `BuffApplyService`；与现有 `TriggerAnotherBuff` 意图一致，建议 **表驱动优先** |
| `ClearBuffByTag` | 驱散 | 按 `BuffType` / `dispellable` / tag 扫 `BuffManager` |

**组合示例（概念）**

- **中毒**：`CurrentLevel` 为 **层数**（§4.3）；**DOT 与每 z 秒掉一层** 均在 **§4.4 统一时间轴** 上推进；`OnPeriodicTick` 中 `[ImpactDamage…]`，伤害基数 **× 当前层数**（由 Dispatcher 读取 `CurrentLevel` × 表内每层系数）。
- **恐惧**：`OnApply` + `[ControlLock(禁普攻+禁技能), ForcedDisplacement(远离 Provider), StatModify(移速+Δ)]`；周期可空或仅位移刷新。

---

## 6. 配置形态（示意）

### 6.1 与现有 `BuffJsonData` 的关系

现有 JSON 为 `metadata` + `config`（`id/type/duration/frequency/...`）。建议 **扩展一节** `execution`（或独立 `BuffExecution.json` 按 id 关联），避免破坏旧文件：

```json
{
  "config": { "id": 1001, "maxDuration": 6, "frequency": 0.5, "resolution": "Separate" },
  "execution": {
    "onApply": [
      { "op": "ControlLock", "mask": "Silence", "durationFromBuff": true }
    ],
    "onPeriodicTick": [
      { "op": "ImpactDamage", "impactType": "Magical", "baseValue": 10, "scalePerLevel": 5 }
    ]
  }
}
```

实现时可用 **一份** `MetaBuff : BuffBase` 读表并注册到 `BuffTypeRegistry`（**仅注册一个泛型入口**）， opcode 由表驱动。  
涉及 **DOT + 周期性掉层** 的 Buff（如中毒），表中 **DOT / 衰减间隔** 建议作为 **`execution`** 或 profile 字段显式给出，并由 **§4.4** 统一推进；不必强依赖 **`config.frequency`** 与 **`ResidualDuration`/demotion** 隐含承担两套节拍。

---

## 7. 与 Impact、ECS、黑板

- **伤害 opcode** 必须带 **有效 Provider/Owner**；若需 `LastDamageFrom` / 击杀链，目标需挂 **`CombatBoardLiteComponent`**（见局内单位 ECS 文档）。
- **技能与普攻统一**：普攻 = 低阶 `SkillDefinition` 或单步施加「伤害载体 Buff」；不在 opcode 层区分，仅在 `ImpactSourceType` 等区分。

---

## 8. 实施顺序建议（毕设）

与 **§9（MVP / P1 / P2）**：本节为 **依赖先后**；§9 为 **每阶段做多少**。

1. 启动时 **`BuffTypeRegistry.Register<MetaBuff>(...)`** 或按 id 范围注册；保证技能表 **能真正落下 Buff**。
2. 实现 **Dispatcher**：解析 `execution` → 执行 `ImpactDamage` + 简单 `StatModify`。
3. 在 **MetaBuff（或等价宿主）** 内实现 **§4.4 单一时间轴**（`SinceLastDot` / `SinceLastStackDecay` 等），再由此调用 Dispatcher 执行 **`OnPeriodicTick`** 与可选的 **层衰减**；**避免**与 `ResidualDuration`/`demotion` **无文档地双轨并行**。
4. 再补 **ControlLock / ForcedDisplacement**（与输入、移动桥对接）。
5. 技能侧 **OnEvent** 与 Basement 事件 **二期**（与 **§9.3** 对应）。

---

## 9. MVP / P1 / P2 实施梯度

以下为按 **交付价值** 划分的阶段；**§8** 为技术依赖顺序提要，本节为 **范围裁剪**（可与毕设时间节点对齐）。

### 9.1 MVP（最短闭环）

**实现状态（代码）**：已实现 **§9.1 核心路径**——`Gameplay.Skill.Buff.BuffOpcodeMvpBootstrap` 于 `AfterSceneLoad` 向 **`BuffTypeRegistry`** 注册 **`MetaBuffApplyFactory`**（`buffId` **90001 / 90002**）。宿主 **`MetaBuff`**、**`BuffOpcodeDispatcher`**（仅 **`ImpactDamage` → `ImpactManager`**）、组合常量 **`BuffOpcodeMvpDefinitions`** 位于 **`Assets/.../Gameplay/BuffSystem/Buff/Opcode/`**；**`MetaBuffApplyFactory`** 与 **`BuffOpcodeMvpBootstrap`** 位于 **`Gameplay/Skill/Buff/`**。**90001**：施加时一次魔法伤（基数 25，`Skill` 源）；**90002**：无 OnApply，**每 1s** 周期魔法 DOT（单次 8）。伤害按 **`CurrentLevel` 叠层**倍增（不少于 1 倍）。若 **`BuffData.json`** 暂未配置上述 id，`BuffApplyService` 仍可施加（或对缺表现警）。 **`CombatBoardLiteComponent`** 仍存在时 **`ImpactSystem`** 会写 **`LastDamageFrom`**。**技能步骤**请将 **`buffId`** 设为 **90001 或 90002**。

| 目标 | 内容 |
|------|------|
| **技能能挂上「表驱动 Buff」** | 启动路径 **`BuffTypeRegistry.Register`**；**单个** **`MetaBuff`（或极少量占位子类）** 对应表内多条 `buffId`。 |
| **Opcode 可走通** | **Dispatcher**：至少 **`ImpactDamage` → `ImpactManager.CreateImpactEvent`**（Source=Provider ECS，Target=Owner ECS）；受击者有 **`CombatBoardLiteComponent`** 时 **承伤黑板**可被 **`ImpactSystem`** 更新。 |
| **配置最小集** | **`OnApply` 非空或可空** + **一条**瞬时伤害 Opcode 即可闭环；`execution` 可先 **手写进代码常量**再落 JSON，或 **只做 1～2 条 Buff 表**。 |
| **时间轴（精简版）** | MVP 可先 **仅 `OnApply` + 瞬时 Impact**，暂不强制 **§4.4 双累积器**；若需 **单层 DOT**：只实现 **`SinceLastDot`** 单曲式间隔，**不涉及周期性掉层**。 |
| **不纳入** | `ControlLock`、`ForcedDisplacement`、**§4.4** 双侧节拍 + 叠层递减、**`OnEvent`**、驱散链。 |

**验收**：选目标释放技能 → 目标 **扣血（经 Impact）** → 控制台 / UI 可看 **HP / LastDamageFrom** 变化。

### 9.2 P1（ opcode 与时间轴齐备 + 可做中毒_demo）

| 目标 | 内容 |
|------|------|
| **Execution 配置入表** | `BuffJsonData`（或并列 profile）挂载 **`BuffEffectComposition`** 或等价 JSON；Launcher 校验 **opcode 枚举与参数**。 |
| **§4.4 统一时间轴** | **`MetaBuff` 内**：`SinceLastDot`、`SinceLastStackDecay`（命名可置换）， **`Time.time` 或对局时钟** 二选一侧；**DOT**与**按间隔掉层**（**`CurrentLevel` 递减**）由 **同一宿主**调度。 |
| **Opcode 扩展** | **`StatModifyCore` / `StatModifyBonus`** 至少一通（与 **`EntityDataComponent`**；或统一走 Impact 改属性）；**简易 `ControlLock`**：写入 ** bitmask 占位组件**（或全局 CC 寄存），输入/普攻管线 **读标记**即可。 |
| **叠层语义** | 与 **§4.3** 一致：**`CurrentLevel` = 层**； **`BuffConflictResolution`** 与本条毒表 **对齐一条规则**（如 Combine）。 |
| **技能侧** | 保持 **`Immediate` / `AfterDelay` / `OnCondition`** 可用；事件驱动步骤仍可不实现。 |

**验收**：**中毒类**——每秒 **× 层 × 系数** DOT + **每 z 秒 −1 层**；层归零 Buff 移除或 OnRemove opcode 可走。

### 9.3 P2（表现力与工程化）

| 目标 | 内容 |
|------|------|
| **位移与复杂控制** | **`ForcedDisplacement`** 与 **`NavMotorBridge` / MovementFacade**（或 ECS 位移意向）对齐； **`ControlLock` 与 Fear/Taunt 等**拆分测试。 |
| **链式与子 Buff** | **`ApplyChildBuffById`、`ClearBuffByTag`**（驱散、净化）；**`OnStackChanged`** 钩子与 opcode（可选）。 |
| **技能节拍** | **`BuffApplicationTriggerKind.OnEvent`** 与 **`GameEventBus` / Basement** 打通；打断、死亡取消挂起会话。 |
| **观测与容错** | Buff 运行时 **校验 / 日志**（非法 opcode、缺 Provider）；**与 `ResidualDuration`/demotion 混用场景**若在旧 Buff 保留，须有 **单行设计说明**。 |

---

## 10. 文档修订记录

| 版本 | 日期 | 说明 |
|------|------|------|
| 1.4 | 2026-04-17 | §9.1 MVP 代码落地说明；`buffId` 90001/90002 与脚本路径提示。 |
| 1.2 | 2026-04-17 | §4.3 定稿 `CurrentLevel` 仅叠层；§4.4 单一时间轴驱动 DOT + 掉层；§4.2/§5/§8 联动修订。 |
| 1.1 | 2026-04-17 | 与实现对齐：废止扁平 `BuffEffect`，补充 §2.3 代码模型（`BuffEffectOpcode`、组合与配方子类）。 |
| 1.0 | 2026-04-17 | 初稿：opcode 定义、两层触发、现状评估、最小 opcode 集与配置示意 |
