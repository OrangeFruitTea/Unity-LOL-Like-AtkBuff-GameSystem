# 技能施法特效（SkillCastVfx）使用指南

本文说明 **`SkillCastVfxRelay`**（方案 A：施法者身上 Inspector 列表配置）的挂载方式、与 **`skillId`** / **`SkillCastContext`** 的对应关系，以及与 **`SkillExecutionFacade.TryBeginCast`** 的自动触发链路。**特效 Prefab 不参与 `SkillData.json` 序列化**，便于毕设阶段快速换资源而无需改表。

---

## 1. 代码位置与命名空间

| 内容 | 路径 |
|------|------|
| 枚举与数据结构 | `_Project/Code/Scripts/Presentation/SkillVfx/SkillCastVfxModels.cs`，命名空间 **`Gameplay.Presentation.SkillVfx`** |
| 播放中继组件 | `_Project/Code/Scripts/Presentation/SkillVfx/SkillCastVfxRelay.cs` |
| 触发入口 | `SkillExecutionFacade.TryBeginCast` 成功后：`context.Caster.GetComponent<SkillCastVfxRelay>()?.NotifyCastSucceeded(skillId, context)` |

---

## 2. 运行时何时播放

1. 调用方构造 **`SkillCastContext(caster)`**，按需设置 **`PrimaryTarget`**、等级等。  
2. 调用 **`SkillExecutionFacade.TryBeginCast(skillId, context, out error)`**。  
3. 仅当 **`TryBeginCast` 返回 `true`**（目录存在、校验通过、管线 `TryBegin` 成功）时：  
   - 先 **`UnitAnimDrv.NotifySkillCastStarted()`**；  
   - 再 **`SkillCastVfxRelay.NotifyCastSucceeded(skillId, context)`**。  

若施法失败（冷却、缺目标、管线错误等），**不会**播放列表中的特效。

---

## 3. Prefab 挂载步骤

1. 确认 **`context.Caster`** 指向的根物体与 **`EntityBase`** 一致（与现有技能上下文约定相同）。  
2. 在该根物体上添加 **`SkillCastVfxRelay`**（同物体可同时存在 **`UnitAnimDrv`**、**`MovementController`** 等）。  
3. **Hand 锚点（可选）**：将右手 / 左手骨骼下的空物体 **`Transform`** 拖到 **`Caster Hand Right` / `Caster Hand Left`**。  
   - 若某项使用 **`CasterHandRight` / `CasterHandLeft`** 而对应引用为空，则 **回退到施法者根 `Transform`**，避免空引用崩溃。  
4. **`Nav Mesh Sample Radius`**：用于 **`GroundUnderCaster`** / **`GroundUnderPrimaryTarget`** 时对 **`NavMesh.SamplePosition`** 的搜索半径（米），与场景烘焙尺度、`MovementController` 等保持同量级即可（默认 `4`）。  
5. **`Specs`**：按技能逐条增加 **`SkillVfxSpawnSpec`**（见 §4）。

**同一 `skillId` 可配置多条 Spec**（例如：起手手部闪光 + 延迟地面爆炸）。

---

## 4. `SkillVfxSpawnSpec` 字段说明

| 字段 | 含义 |
|------|------|
| **skillId** | 与 **`SkillCatalog`** / **`SkillData.json`** 中 **`skillId`** 完全一致；不匹配则本条被忽略。 |
| **fxPrefab** | 场景中 **`Instantiate`** 的预制体（粒子、嵌套 ParticleSystem 等）；为空则跳过。 |
| **moment** | **`OnCastSucceeded`**：本帧与施法成功对齐；**`AfterDelaySeconds`**：延迟 **`delaySeconds`** 秒后再生成（受 **`Time.timeScale`** 影响）。 |
| **delaySeconds** | 仅在 **`AfterDelaySeconds`** 时生效；≤0 等价于下一帧附近立刻播放。 |
| **attach** | 生成位置与朝向的语义锚点，见 §5。 |
| **localPositionOffset** | 相对锚点的局部偏移（根/手目标上用 **`TransformPoint`**；地面类在采样点前先做 **`TransformVector`** 偏移）。 |
| **localEulerAngles** | 与 **`multiplyAttachRotation`** 组合决定最终旋转，见 §6。 |
| **multiplyAttachRotation** | **`true`**：最终旋转 = **锚点世界旋转 × Quaternion.Euler(localEulerAngles)**；**`false`**：**仅** **`Quaternion.Euler(localEulerAngles)`**（世界空间欧拉角语义）。 |
| **parentToAttachTransform** | **`true`**：实例 **`SetParent(锚点, worldPositionStays:true)`**，跟随骨骼/单位移动；**`false`**：实例留在场景根下，位置朝向一次性对齐（适合爆炸、地面圈等）。 |

---

## 5. `SkillFxAttachKind`（锚点语义）

| 枚举值 | 位置计算概要 | 对 `PrimaryTarget` 的要求 |
|--------|----------------|---------------------------|
| **CasterRoot** | 施法者根变换 + 偏移 | 无 |
| **CasterHandRight** | 右手锚点（未赋值则用根）+ 偏移 | 无 |
| **CasterHandLeft** | 左手锚点（未赋值则用根）+ 偏移 | 无 |
| **GroundUnderCaster** | 施法者附近世界坐标经 **`NavMesh.SamplePosition`** | 无 |
| **PrimaryTargetRoot** | 主目标根变换 + 偏移 | **必填**；否则打 Warning 并跳过本条 |
| **GroundUnderPrimaryTarget** | 主目标位置附近经 **`NavMesh.SamplePosition`** | **必填**；否则打 Warning 并跳过本条 |

地面类锚点若采样失败，则 **退回未采样的原始竖直位置**，需注意出生点是否在 NavMesh 外。

---

## 6. 旋转配置建议（毕设常用）

- **扇形 / 角色朝向对齐**：**`multiplyAttachRotation = true`**，`localEulerAngles` 填较小修正角（如火花绕手腕微调）。  
- **世界朝上特效（地面圆环）**：**`multiplyAttachRotation = false`**，`localEulerAngles = (90, 0, 0)` 等按Prefab习惯调整。  
- **跟随骨骼的武器轨迹**：**`parentToAttachTransform = true`** + **`CasterHandRight`**。

---

## 7. 延时播放与生命周期

- **`AfterDelaySeconds`** 使用协程 **`WaitForSeconds`**；若在延时期间 **`Caster`** 被 **`SetActive(false)`** 或组件被禁用，协程会中止或 **`SpawnOne`** 不再执行（逻辑以 **`Caster.gameObject.activeInHierarchy`** 为准）。  
- **销毁**：中继 **不负责** 回收实例；请在 Prefab 上配置 **粒子 Stop Action Destroy**、或挂载 **`Destroy`**/`对象池回收脚本**，避免堆积。

---

## 8. 与 JSON 技能定义的关系

- **`SkillDefinition`**（JSON）仍只管数值、冷却、`requiresTarget`、Buff 步骤等。  
- **特效仅由 `SkillCastVfxRelay.specs` 驱动**，通过 **`skillId`** 对齐。  
- 调整 **`SkillData.json`** 中某技能的 **`skillId`** 时，务必同步修改 Inspector 列表中的 **`skillId`**。

---

## 9. 可选扩展（当前未实现）

- **对象池**：高频技能可将 **`Instantiate`** 改为池化，接口保持 **`NotifyCastSucceeded`** 不变。  
- **Animator 事件对齐**：需在 Clip 事件或 Timeline 中另行调用 **`NotifyCastSucceeded`** 的兄弟 API（若日后拆分）；当前仅有 **施法成功帧** 与 **固定延迟** 两种时机。  
- **无目标 AoE 落点**：若上下文后续扩展「世界目标点」字段，再在 **`SkillFxAttachKind`** 中增加枚举并实现解析即可。

---

## 10. 快速自检清单

- [ ] **`SkillCastVfxRelay`** 挂在 **`context.Caster`** 同一 GameObject 上。  
- [ ] **`skillId`** 与 **`SkillCatalog`** 一致。  
- [ ] 需要目标的锚点已设置 **`context.PrimaryTarget`**，且 **`requiresTarget`** 与逻辑一致。  
- [ ] 地面类锚点场景已烘焙 **NavMesh**，或接受采样失败时的位置回退。  
- [ ] Prefab 具备合理 **自动销毁** 或池化策略。  

---

*文档与实现同步：`Gameplay.Presentation.SkillVfx` 下 `SkillCastVfxRelay`、`SkillCastVfxModels`，以及 `SkillExecutionFacade`。*
