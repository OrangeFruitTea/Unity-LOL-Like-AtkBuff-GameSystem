# 基于 Buff 与 ECS 的技能系统设计文档

## 1. 文档目的与适用范围

本文档在现有 **`BuffSystem`**（`Assets/_Project/Code/Scripts/Gameplay/BuffSystem`）与 **`Core.ECS`**（`Assets/_Project/Code/Scripts/Core/Patterns/ECS`）之上，定义一套**技能（Skill）模块**的架构与数据契约。设计约束如下：

- **技能的一切“效果”均落实为对目标的 Buff 施加**（调用与 `BuffManager.AddBuff` 等价的施加语义），不另建平行“效果执行器”树；数值、控制、链式触发等继续由具体 `BuffBase` 子类与 `BuffConfig` / `BuffRuntimeData` 表达。
- **技能效果可装配、可修改**：同一技能在配置层由有序/有分支的 **Buff 施加步骤（Buff Application Step）** 组成；运行时支持等级、表驱动参数、可选修饰器（如统一缩放持续时间、改写目标集合），**便于仅改外置配置完成装配**，并保留 **C# 侧扩展钩子**。
- **施加时序**：支持**同一时刻并行施加**多条 Buff，也支持**延迟**（固定秒数、时间轴锚点）与**条件满足后**再施加后续 Buff；条件既可来自 ECS 组件状态，也可来自游戏事件（与 `Basement` 事件/任务模块协作）。
- 运行环境：**Unity 2022**；实体侧延续 **`EntityBase` + `EcsEntityBridge` + `EcsEntity`** 的既有桥接方式（见 `EntitySpawnSystem`）。

本文档为**模块设计**，不替代《技能逻辑与战斗判定构思文档》中关于弹道、碰撞、范围判定的细节，但**规定技能在“效果落地”阶段如何统一回落到 Buff 施加**。

---

## 2. 与现有代码的对应关系

### 2.1 Buff 子系统（效果真源）

| 现有类型 | 设计中的角色 |
|---------|-------------|
| `BuffManager` | **唯一推荐入口**：技能执行器最终调用其“施加 Buff”API（保持对象池、冲突解决、观察者通知等行为一致）。 |
| `BuffBase` / `BuffConfig` / `BuffRuntimeData` | 单个 Buff 的生命周期与数值；技能只负责**在正确时间、对正确目标、带正确参数**调用施加。 |
| `BuffConflictResolution` | 多段技能重复施加同一 Buff 时，行为与普攻/环境 Buff 一致，无需技能层重复实现。 |
| `BuffDataLoader` + JSON | Buff **静态定义**来源；技能配置中引用 `BuffConfig.id`（或等价标识），由注册表解析为具体 `BuffBase` 类型或工厂。 |
| `BuffEffectOpcode`（及 `BuffOpcodeInstruction`/`BuffEffectComposition`/配方子类）（含链子 Buff 语义） | 表明 Buff 可由 **多条 opcode 指令**组合；Dispatcher 将实现具体行为；技能系统仍**不禁止** Buff 配方内再挂 Buff，策划向编排仍以技能步骤 + opcode 表为主。 |

**说明**：遗留的扁平 **`BuffEffect` 枚举已移除**，避免与 **`BuffEffectOpcode` + 多重指令** dual-use；旧文档若提及 `BuffEffect` 请以代码为准。

### 2.2 ECS 子系统（施法者状态与调度载体）

| 现有类型 | 设计中的角色 |
|---------|-------------|
| `EcsWorld` / `EcsEntityManager` | 存储施法者、技能书、冷却、进行中的技能实例、待执行的 Buff 施加计划等 **struct 组件**。 |
| `IEcsSystem` + `UpdateOrder` | **`SkillSystem` 组**：解析意图、推进冷却、驱动技能时间轴、将到期的步骤提交给 `BuffManager`。 |
| `EcsEntityBridge` / `EntityBase` | 技能目标解析时，优先得到 `EntityBase`（与 Buff 侧一致），必要时通过 `BoundEcsEntity` 做 ECS 查询。 |
| `EntitySpawnSystem` | 实体出生时挂载基础组件；技能相关组件可在出生流程中初始化，或由技能模块在首次需要时 `AddComponent`。 |

### 2.3 Basement（时序与条件）

| 模块 | 设计中的角色 |
|-----|-------------|
| `Basement.Tasks.TimeTriggeredTask` | **延迟施加**：在 `delaySeconds` 后执行“提交 Buff 施加”动作。 |
| `Basement.Tasks.ConditionalTask` | **条件施加**：按间隔检测条件，满足后执行一次施加并联调完成态。 |
| `TimingTaskScheduler` / `TimingTaskManager`（见《TimingTaskScheduler.md》） | 与 Unity 时间、`timeScale` 对齐的调度；技能取消、死亡、眩晕打断时需**可取消**对应任务。 |

**原则**：ECS 负责**权威状态与查询**；**一次性延迟/条件**可委托 Basement 任务；**每帧需与多个实体同步推进**的技能时间轴（如引导条、多段连招窗口）优先用 **ECS 组件 + System** 推进，避免大量独立 Task。

---

## 3. 设计原则

1. **效果单一出口**：技能逻辑不直接改 `EntityDataComponent` 血量等（除非已有 Impact 等系统明确约定）；默认路径是 **加 Buff**，由 Buff 内聚修改属性或行为。
2. **数据驱动装配**：技能 = **元数据** + **Buff 施加管线（Pipeline）**；改技能≈改编排，而非必改代码。
3. **实例与定义分离**：`SkillDefinition`（静态/配置）与 `SkillInstance`（运行时等级、冷却剩余、当前管线游标）分离，便于存档与联机同步规划。
4. **时间与条件显式建模**：每一步携带 `BuffApplicationTrigger`（立即 / 延迟 / 条件 / 等待事件），避免隐式协程散落在各 `SkillBase` 子类中。
5. **与现有 MOBA 构思文档兼容**：前摇、选目标、碰撞仍可由原战斗管线完成；本模块接收“已锁定的目标集合 + 施法上下文”，进入 **Buff 施加阶段**。

---

## 4. 架构总览

```
                    ┌──────────────────────────────┐
                    │  战斗/选目标/弹道（既有或规划）  │
                    └──────────────┬───────────────┘
                                   │ SkillCastContext
                                   ▼
┌──────────────────────────────────────────────────────────────┐
│ SkillExecution（ECS Systems + 可选 TimingTask）               │
│  · 校验冷却 / 资源 / 沉默等（可读 ECS 组件或 Buff 存在性）        │
│  · 解析 SkillDefinition → 展开 BuffApplicationPipeline          │
│  · 按 Trigger 推进步骤；到期调用 BuffApplyService              │
└──────────────────────────────┬───────────────────────────────┘
                               │
                               ▼
                    ┌──────────────────────┐
                    │ BuffManager.AddBuff*  │
                    └──────────────────────┘
```

---

## 5. 核心概念与数据模型

### 5.1 SkillDefinition（技能定义，配置层）

建议字段（逻辑名，实现时可 JSON/ScriptableObject 二选一或与 `ConfigurationModule` 对齐）：

- **标识**：`skillId`、`displayName`、`maxLevel`。
- **施法约束**：冷却、消耗、施法距离、是否需要目标等（与战斗文档对齐部分略）。
- **管线**：`BuffApplicationPipeline` —— **有序**的步骤列表（见下）；可选 **并行组**（同一 tick 内多步同时施加）。
- **等级曲线**：每一步可引用 `levelScaling`（如随技能等级放大 Buff `level` 或自定义参数表）。

### 5.2 BuffApplicationStep（单步施加描述）

每一步描述“**对谁、施加哪类 Buff、带什么参数、何时触发**”：

| 字段 | 含义 |
|-----|------|
| `stepId` | 调试、联机对齐用稳定 id。 |
| `buffId` | 对应 `BuffDataLoader` 中 `BuffConfig.id`（或项目统一配置 id）。 |
| `targetSelector` | 目标解析策略：如 `Caster`、`PrimaryTarget`、`TargetsInArea`（依赖上下文已算好的实体列表）、`AllHitByLastImpact` 等。 |
| `buffLevel` | 基础等级；可与技能等级、属性联动（由 `SkillParameterResolver` 计算）。 |
| `durationOverride` | 可选；覆盖默认 `BuffConfig.maxDuration` 初始化到 `BuffRuntimeData.ResidualDuration`（与现有 `Init` 中 args 约定一致）。 |
| `customArgs` | 可选扩展参数，映射到 `BuffBase.HandleCustomArgs`（保持与现 `Init` 兼容）。 |
| `trigger` | `BuffApplicationTrigger`（见 5.3）。 |
| `parallelGroup` | 可选；同组步骤在同一调度 tick 执行（**同时施加**）。 |

### 5.3 BuffApplicationTrigger（触发语义）

- **Immediate**：进入该步所在逻辑帧立即执行。
- **AfterDelay**：相对**上一步完成时刻**或**技能开始时刻**延迟 `delaySeconds`（需在文档中固定一种基准，推荐**相对技能 CastStart**，避免步骤间语义歧义）。
- **OnCondition**：引用 `ConditionId` 或内联谓词描述；由 `SkillConditionEvaluator` 周期性或在相关 System 中检测，满足后执行**一次**。
- **OnEvent**：订阅游戏事件（如“受击”“击杀”“碰撞体进入范围”），事件到达且可选过滤后执行；实现可与 `GameEventBus` 协作（Basement Events）。

**取消语义**：施法打断、死亡、沉默、强制位移等应 **取消** 未执行的延迟步与未完成的条件等待，并可选 **回滚**（若项目需要，回滚也表现为施加移除类 Buff 或调用 `RemoveBuff`，仍属 Buff 范畴）。

### 5.4 SkillCastContext（一次施法上下文）

在一次技能释放中构造，贯穿管线：

- `Caster`：`EntityBase`（及 `EcsEntity`）。
- `PrimaryTarget` / `SecondaryTargets`：由战斗层填入。
- `SkillLevel`、`RandomSeed`（联机与确定性复现）、`WorldTime` 快照等。

目标解析器（`ITargetResolver`）输入 Context + `targetSelector`，输出 `IReadOnlyList<EntityBase>`，再对列表逐项调用施加。

### 5.5 BuffApplyService（施加服务，薄封装）

职责：

- 根据 `buffId` **解析**具体施加方式（泛型 `T : BuffBase` 或委托工厂）。
- 组装 `BuffManager` 所需参数：`target`、`provider`（通常为 Caster）、`level`、`args`（含持续时间等，与 `BuffBase.Init` 约定一致）。
- 统一打日志、统计、**失败原因**（目标无效、Buff 未注册），便于调试。

**禁止**：业务层绕过 `BuffApplyService` 直接 `new Buff` 并挂载。

---

## 6. ECS 组件与系统建议

以下组件均为 **struct + `IEcsComponent`**，与现框架一致；命名可按项目命名空间落地（如 `Core.Gameplay.Skills`）。

### 6.1 组件（建议）

| 组件 | 职责 |
|-----|------|
| `SkillBookComponent` | 已学会技能 id 列表、技能等级；可选嵌入冷却数组或引用共享表。 |
| `SkillCooldownComponent` | 各 `skillId` 的下一次可施放时间戳或剩余 CD。 |
| `SkillCastStateComponent` | 当前是否施法中、引导剩余时间、打断优先级等（与战斗层共享时需约定单一写入者）。 |
| `SkillPipelineRuntimeComponent` | **进行中的技能实例**：当前 `pipeline` id、下一步索引、并行组状态、`castStartTime`、是否已取消。 |
| `BuffApplicationQueueItem`（可多条或内嵌列表） | 轻量队列：下一步执行时间、stepId、已序列化参数快照；或由独立 **队列实体** 承载（见下）。 |

**可选**：为简化生命周期，可为每次施法创建一个 **短生命周期 EcsEntity**（仅挂管线运行时组件），施法结束或取消后 `DestroyEntity`；施法者身上只存 `ActiveSkillInstanceEntityId`。

### 6.2 系统（建议，`IEcsSystem`）

| 系统 | `UpdateOrder` 建议 | 职责 |
|-----|---------------------|------|
| `SkillCooldownTickSystem` | 较早 | 更新冷却、同步 UI（如需要）。 |
| `SkillCastPipelineSystem` | 中 | 驱动 `SkillPipelineRuntimeComponent`：处理 Immediate/延迟时间累积；到期则调用 `BuffApplyService`。 |
| `SkillConditionWaitSystem` | 中与管线相邻 | 处理 `OnCondition` 步骤的评估（或把评估内联进管线系统以降低遍历次数）。 |

**与 Basement 的分工**：

- 若某技能步骤仅为 **单次延迟**，可在进入该步时注册 `TimeTriggeredTask`，回调中调用 `BuffApplyService`，并在取消施法时 `Cancel` 任务。
- 若管线 **复杂或实例极多**，优先 **ECS 内时间字段 + 单 System 批处理**，减少 Task 对象数量。

---

## 7. “装配与修改”机制

### 7.1 配置层装配

- 在 **`SkillData`/JSON（或等价外置步骤表）里维护** **有序步骤** + **并行组** + **触发器**，即可完成对技能的装配。
- Buff 本身仍由 `BuffData.json`（或等价）维护；技能只引用 id。

### 7.2 运行时修改（扩展点）

- **修饰器链**：在执行施加前，`IBuffApplicationModifier` 可调整 `buffLevel`、持续时间、`targetSelector` 结果（如“额外弹射一个目标”）。
- **来源**：装备、天赋、团队 Buff；可由 ECS 组件查询结果驱动 modifier 列表。
- **约束**：修饰器仍只能产生 **合法的 Buff 施加请求**，不直接改技能管线结构（避免难以同步）；若需动态插入步骤，应走 **显式 API** 并记录 `pipelineRevision` 供联机。

---

## 8. 典型流程示例

### 8.1 同时施加

- 步骤 A、B 标记为同一 `parallelGroup`，`trigger` 均为相对 `CastStart + 0s` 的 Immediate（或同一 tick）。
- `SkillCastPipelineSystem` 在同一更新周期对 A、B 调用两次 `BuffApplyService`。

### 8.2 延迟链式施加

- 步骤 1：Immediate，对目标施加 `Buff_Slow`。
- 步骤 2：`AfterDelay(2.0f)`，对同一目标施加 `Buff_DamageOverTime`。
- 实现：管线记录 `nextExecuteTime = castStart + 2.0f`；每帧或 Fixed 步进比较（注意与 `BuffManager.FixedDeltaTime` 时间基一致性问题，建议在技能侧统一用 `Time.time` 或项目统一 `CombatClock`）。

### 8.3 条件后施加

- 步骤 3：`OnCondition`，条件为“目标 `EntityDataComponent` 当前生命低于 30%”（需目标仍有 ECS 组件可读）。
- `SkillConditionWaitSystem` 检测到真后执行施加并标记步骤完成；超时可选放弃或默认失败。

---

## 9. Unity 2022 与工程实践注意点

- **时间基准**：Buff 持续时间衰减目前在 `BuffManager` 中使用 `Time.fixedDeltaTime`；技能延迟若用 `Update` 与 `FixedUpdate` 混用可能产生微小偏差，建议在文档实现阶段约定 **统一战斗时钟**（可由单一 `CombatTimeProvider` 提供）。
- **实体销毁**：`EntityBase.OnDestroy` 会销毁 ECS 实体；管线系统需检测 `EntityBase` 与 `EcsWorld.Exists`，避免对已销毁目标 `AddBuff`。
- **DontDestroyOnLoad**：`BuffManager` 为单例；技能系统若挂在场景对象上，需与场景切换策略一致（可与 `EcsWorld` 同为常驻）。
- **调试**：步骤 `stepId` + `skillId` + `castInstanceId` 日志关联，便于与 Buff 观察者 `StartObserving` 交叉验证。

---

## 10. 实现阶段划分（建议）

| 阶段 | 内容 |
|-----|------|
| P0 | `BuffApplyService` + Buff 注册表；`BuffApplicationStep` 配置结构与 JSON/SO 选其一；Immediate + AfterDelay 管线 + `SkillCastPipelineSystem` MVP。 |
| P1 | 并行组、`OnCondition`、与 `TimingTask` 互操作、施法取消与任务清理。 |
| P2 | `OnEvent`、运行时修饰器、与网络同步文档对齐（castInstanceId、步骤序号、随机种子）。 |

---

## 11. 与《技能逻辑与战斗判定构思文档》的衔接

- 该文档中的 **SkillBase / 冷却任务** 可演进为：施法前摇与目标选择仍由战斗层负责，**效果阶段**调用本设计的 **`SkillExecutionFacade`**（启动管线 + 注册 ECS 状态），从而满足“**所有效果属于 Buff 施加**”的统一约束。
- Impact、弹道命中回调应构造 `SkillCastContext` 并启动或推进管线，而不是在回调内散落 `AddBuff` 调用，以保持装配一致性。

---

## 12. 小结

本设计将 **技能** 定义为 **基于 ECS 状态机 + 可配置 Buff 施加管线**：技能系统负责**何时、对谁、以何参数**调用现有 **`BuffManager` 施加语义**；延迟与条件通过 **ECS 推进** 与 **`Basement.Tasks`** 组合实现；**装配与运行时修改**通过步骤表与修饰器扩展，满足 Unity 2022 项目内与现有 Buff、ECS、 Basement 模块的衔接。
