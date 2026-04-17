# Impact系统使用方法指导文档

## 1. 系统概述

Impact系统是基于ECS（Entity Component System）架构实现的一套用于处理游戏中各种影响效果（如伤害、治疗、属性修改等）的系统。该系统设计用于MOBA游戏中，负责处理角色之间的交互效果，包括但不限于伤害计算、治疗效果、属性修改、状态施加等。

### 1.1 系统架构

Impact系统采用ECS架构，由以下核心部分组成：

- **组件（Component）**：存储数据，包括ImpactEventComponent、ImpactValueComponent、ImpactModifierComponent等
- **系统（System）**：处理逻辑，主要是ImpactSystem
- **管理器（Manager）**：提供外部接口，方便其他系统使用，即ImpactManager

### 1.2 核心功能

- 伤害/治疗效果处理
- 属性值修改
- 状态效果施加/移除
- 修饰器系统（用于伤害/治疗的修正）
- 战斗判定流程（命中、闪避、抵抗、暴击）
- 数值计算与结算

## 2. 核心功能说明

### 2.1 Impact事件

Impact事件是系统的核心，用于表示一次影响效果的触发。每个事件包含以下信息：

- 来源实体（Source）：效果的发起者
- 目标实体（Target）：效果的接受者
- 目标属性（TargetAttribute）：要修改的属性
- 操作类型（OperationType）：增加、减少或覆盖
- 影响类型（ImpactType）：物理、魔法或真实
- 来源类型（SourceType）：普通攻击、技能、Buff或环境

### 2.2 修饰器系统

修饰器用于修正Impact效果的数值，支持优先级排序，确保计算顺序正确。修饰器包含以下信息：

- 类型（Type）：修饰器类型
- 值（Value）：修饰值
- 来源（Source）：修饰器的来源
- 是否为百分比（IsPercentage）：是否为百分比修饰
- 优先级（Priority）：修饰器的优先级

### 2.3 战斗判定流程

系统会执行完整的战斗判定流程，包括：

- 命中/闪避/抵抗判断
- 暴击判断
- 伤害类型有效性判断
- 数值计算与修正
- 最终值应用

### 2.4 属性修改

系统支持修改多种属性，包括：

- 生命值（Hp）
- 魔法值（Mp）
- 物理攻击（AtkAD）
- 魔法攻击（AtkAP）
- 物理防御（DefenceAD）
- 魔法防御（DefenceAP）
- 攻击速度（AtkSpeed）
- 技能冷却（SkillCd）
- 暴击率（CriticalRate）
- 移动速度（MoveSpeed）
- 以及其他多种属性

## 3. 环境配置要求

### 3.1 依赖项

- Unity 2020.3或更高版本
- ECS框架（Core.ECS命名空间）
- 角色属性系统（Core.Entity命名空间）

### 3.2 系统集成

1. 确保ECS世界已正确初始化
2. 将ImpactSystem添加到ECS系统列表中
3. 创建ImpactManager实例并注入ECS世界

## 4. 基本操作流程

### 4.1 创建ImpactManager

```csharp
// 假设ecsWorld是已初始化的ECS世界实例
var impactManager = new Core.Gameplay.ImpactManager(ecsWorld);
```

### 4.2 创建Impact事件

```csharp
// 创建一个物理伤害事件
var impactEvent = impactManager.CreateImpactEvent(
    sourceEntity,          // 来源实体
    targetEntity,          // 目标实体
    Core.Combat.TargetAttribute.Hp,  // 目标属性
    100f,                  // 基础值
    Core.Combat.ImpactOperationType.Subtract,  // 操作类型（减少）
    Core.Combat.ImpactType.Physical,  // 伤害类型
    Core.Combat.ImpactSourceType.Skill  // 来源类型
);
```

### 4.3 添加修饰器

```csharp
// 创建一个增加伤害的修饰器
var damageModifier = new Core.Combat.ImpactModifier(
    "DamageBoost",  // 修饰器类型
    0.2f,           // 修饰值（20%）
    true,           // 是百分比修饰
    "SkillA",      // 来源
    Core.Combat.ModifierPriority.High  // 优先级
);

// 为目标实体添加修饰器
impactManager.AddModifier(targetEntity, damageModifier);
```

### 4.4 移除修饰器

```csharp
// 移除来自"SkillA"的所有修饰器
impactManager.RemoveModifier(targetEntity, "SkillA");
```

### 4.5 清除所有修饰器

```csharp
// 清除目标实体的所有修饰器
impactManager.ClearModifiers(targetEntity);
```

## 5. 高级功能使用方法

### 5.1 自定义修饰器

```csharp
// 创建一个自定义修饰器
var customModifier = new Core.Combat.ImpactModifier(
    "CustomModifier",  // 自定义类型
    50f,               // 固定值
    false,             // 非百分比
    "CustomSource",    // 来源
    Core.Combat.ModifierPriority.Medium  // 优先级
);

// 添加到目标实体
impactManager.AddModifier(targetEntity, customModifier);
```

### 5.2 处理特殊属性

```csharp
// 创建一个修改攻击速度的事件
var atkSpeedEvent = impactManager.CreateImpactEvent(
    sourceEntity,
    targetEntity,
    Core.Combat.TargetAttribute.AtkSpeed,
    0.5f,  // 增加0.5的攻击速度
    Core.Combat.ImpactOperationType.Add,
    Core.Combat.ImpactType.True,  // 真实类型，不受到防御影响
    Core.Combat.ImpactSourceType.Buff
);
```

### 5.3 批量处理事件

```csharp
// 批量创建多个事件
for (int i = 0; i < targets.Count; i++)
{
    impactManager.CreateImpactEvent(
        sourceEntity,
        targets[i],
        Core.Combat.TargetAttribute.Hp,
        50f,
        Core.Combat.ImpactOperationType.Subtract,
        Core.Combat.ImpactType.Magical,
        Core.Combat.ImpactSourceType.Skill
    );
}
```

## 6. 常见问题排查

### 6.1 事件不被处理

**可能原因：**
- 来源或目标实体无效
- 目标实体没有EntityDataComponent
- 事件已经被标记为已处理

**解决方案：**
- 确保来源和目标实体有效
- 为目标实体添加EntityDataComponent
- 检查事件是否被重复处理

### 6.2 修饰器不生效

**可能原因：**
- 修饰器优先级设置错误
- 修饰器来源与移除时指定的来源不匹配
- 修饰器已过期（如果有时间限制）

**解决方案：**
- 检查修饰器优先级设置
- 确保移除修饰器时使用正确的来源
- 检查修饰器的时间有效性

### 6.3 数值计算错误

**可能原因：**
- 修饰器顺序不正确
- 防御计算逻辑有问题
- 属性映射错误

**解决方案：**
- 检查修饰器优先级设置
- 验证防御计算逻辑
- 确保属性映射正确

## 7. 示例代码

### 7.1 基本伤害处理

```csharp
// 初始化ImpactManager
var impactManager = new Core.Gameplay.ImpactManager(ecsWorld);

// 创建一个物理伤害事件
var damageEvent = impactManager.CreateImpactEvent(
    playerEntity,
    enemyEntity,
    Core.Combat.TargetAttribute.Hp,
    100f,
    Core.Combat.ImpactOperationType.Subtract,
    Core.Combat.ImpactType.Physical,
    Core.Combat.ImpactSourceType.NormalAtk
);

// 为敌人添加一个减伤buff
var defenseModifier = new Core.Combat.ImpactModifier(
    "DefenseBuff",
    -0.2f,  // 减少20%伤害
    true,
    "DefenseSkill",
    Core.Combat.ModifierPriority.High
);
impactManager.AddModifier(enemyEntity, defenseModifier);

// 系统会自动处理这个事件，计算最终伤害并应用到敌人身上
```

### 7.2 治疗效果

```csharp
// 创建一个治疗事件
var healEvent = impactManager.CreateImpactEvent(
    playerEntity,
    allyEntity,
    Core.Combat.TargetAttribute.Hp,
    50f,
    Core.Combat.ImpactOperationType.Add,
    Core.Combat.ImpactType.True,  // 真实类型，不受到防御影响
    Core.Combat.ImpactSourceType.Skill
);

// 为治疗者添加一个治疗加成buff
var healModifier = new Core.Combat.ImpactModifier(
    "HealBoost",
    0.3f,  // 增加30%治疗效果
    true,
    "HealSkill",
    Core.Combat.ModifierPriority.Medium
);
impactManager.AddModifier(playerEntity, healModifier);
```

### 7.3 属性修改

```csharp
// 创建一个增加攻击力的事件
var atkBoostEvent = impactManager.CreateImpactEvent(
    playerEntity,
    playerEntity,  // 给自己施加
    Core.Combat.TargetAttribute.AtkAD,
    20f,
    Core.Combat.ImpactOperationType.Add,
    Core.Combat.ImpactType.True,
    Core.Combat.ImpactSourceType.Buff
);

// 创建一个减少技能冷却的事件
var cdReductionEvent = impactManager.CreateImpactEvent(
    playerEntity,
    playerEntity,
    Core.Combat.TargetAttribute.SkillCd,
    0.8f,  // 技能冷却变为原来的80%
    Core.Combat.ImpactOperationType.Override,
    Core.Combat.ImpactType.True,
    Core.Combat.ImpactSourceType.Buff
);
```

## 8. 系统扩展

### 8.1 添加新属性

要添加新的目标属性，需要：

1. 在`TargetAttribute`枚举中添加新的属性
2. 在`ApplyImpactToTarget`方法中添加对应的处理逻辑

### 8.2 添加新的修饰器类型

要添加新的修饰器类型，只需要在创建修饰器时使用自定义的类型字符串即可，系统会自动处理。

### 8.3 添加新的状态效果

要添加新的状态效果，需要：

1. 在`TargetAttribute`枚举中添加新的状态
2. 在`ApplyImpactToTarget`方法中添加对应的处理逻辑
3. 可能需要创建对应的状态组件

## 9. 性能优化

### 9.1 修饰器管理

- 定期清理过期的修饰器
- 使用合适的优先级设置，避免不必要的计算
- 对于临时效果，使用正确的来源标识，以便及时移除

### 9.2 事件处理

- 避免创建过多的临时事件
- 批量处理相似的事件
- 合理设置事件的优先级

### 9.3 内存管理

- 重用事件实体
- 避免频繁创建和销毁修饰器
- 定期清理处理记录

## 10. 结论

Impact系统是一个灵活、高效的影响效果处理系统，基于ECS架构设计，支持多种游戏效果的处理。通过本文档的指导，开发人员可以快速上手并使用该系统，实现各种复杂的游戏效果。

系统的设计考虑了可扩展性和性能优化，能够适应不同规模的游戏需求。通过合理的配置和使用，可以为游戏提供流畅、准确的效果处理体验。