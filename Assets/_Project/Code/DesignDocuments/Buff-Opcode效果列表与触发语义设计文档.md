# Buff Opcode 式效果列表与触发语义设计文档

| 项 | 内容 |
|----|------|
| 文档版本 | 1.1 |
| 关联文档 | 《基于Buff与ECS的技能系统设计文档》、《MOBA局内单位模块ECS设计文档》、Impact 使用指南 |
| 适用引擎 | Unity 2022 |

---

## 1. 「Opcode」是什么（术语）

**Opcode（Operation Code，操作码）** 在本设计中指：**用一个可序列化的代号（枚举整型或短字符串）表示一类「可对单位做的原子动作」**，配套 **固定形状的参数字段**（如伤害基数、持续时间、位移向量 id）。运行时由一个 **小而稳定的解释器（Dispatcher）** 根据 opcode 分发到已有子系统（如 `ImpactManager`、属性修改、`BuffManager` 再入等）。

它解决的是：**少写成百上千个 `BuffBase` 子类**，而把差异留在 **表里**（JSON、ScriptableObject、策划表）。

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
| `OnPeriodicTick` | 每 `frequency` 秒执行一轮（可配置首跳是否延迟） | `BuffManager` 侧累计时间或 `BuffBase.FixedUpdate` 中计时后调 Dispatcher |
| `OnRemove` | 移除时执行（常用于「结束时净化」或还原） | `OnLost` |
| `OnStackChanged` | 叠层变化时执行（可选，二期） | `OnLevelChange` 或专用钩子 |

**原则**：内层触发类型 **一期控制在 3～4 个**，避免与技能外层重复造轮子。

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

- **中毒**：`OnPeriodicTick` + `[ImpactDamage(魔法/真实), …]`（系数从 level 来）。
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

---

## 7. 与 Impact、ECS、黑板

- **伤害 opcode** 必须带 **有效 Provider/Owner**；若需 `LastDamageFrom` / 击杀链，目标需挂 **`CombatBoardLiteComponent`**（见局内单位 ECS 文档）。
- **技能与普攻统一**：普攻 = 低阶 `SkillDefinition` 或单步施加「伤害载体 Buff」；不在 opcode 层区分，仅在 `ImpactSourceType` 等区分。

---

## 8. 实施顺序建议（毕设）

1. 启动时 **`BuffTypeRegistry.Register<MetaBuff>(...)`** 或按 id 范围注册；保证技能表 **能真正落下 Buff**。
2. 实现 **Dispatcher**：解析 `execution` → 执行 `ImpactDamage` + 简单 `StatModify`。
3. 补 **OnPeriodicTick** 与 `BuffConfig.frequency` 对齐。
4. 再补 **ControlLock / ForcedDisplacement**（与输入、移动桥对接）。
5. 技能侧 **OnEvent** 与 Basement 事件 **二期**。

---

## 9. 文档修订记录

| 版本 | 日期 | 说明 |
|------|------|------|
| 1.1 | 2026-04-17 | 与实现对齐：废止扁平 `BuffEffect`，补充 §2.3 代码模型（`BuffEffectOpcode`、组合与配方子类）。 |
| 1.0 | 2026-04-17 | 初稿：opcode 定义、两层触发、现状评估、最小 opcode 集与配置示意 |
