# MOBA 局内装备与商店模块设计文档

| 项 | 内容 |
|----|------|
| 文档版本 | 1.3 |
| 适用引擎 | Unity 2022 |
| 文档类型 | 模块设计（Module Design） |
| 关联系统 | 角色属性（如 `EntityDataComponent`）、**Buff（效果真理源）**、**技能系统（同类 JSON + 施加路径）**、ECS 实体桥接；**局内金币来源与击杀赏金**见《**MOBA局内经济系统设计文档**》 |
| 核心决策 | **装备在底层逻辑上 = 挂载若干 Buff 的物品**；表驱动效果以 **JSON** 配置为主，便于快速调试与迭代 |

---

## 1. 引言

### 1.1 目的

本文档对 **MOBA 单局内「商店购买」与「装备/道具对角色影响」** 进行模块级设计，用于：

- 统一 **外置配置的维护方式、代码实现边界与自动化／手工验证范围**对**能力边界**与**数据契约**的认识；
- 指导实现拆分（子模块、接口、配置表）与迭代顺序（MVP → 完整形态）；
- 与项目内已有 **属性 / Buff / 技能** 设计对齐：**装备效果与技能效果同源——均通过 `buffId` 与 `BuffManager` 施加语义落地**，避免多套数值真理源；
- 约定 **装备配置与技能配置同类工具链**（如 `StreamingAssets` + `JsonSerializerProfile.GameContent` + `JsonReadResult`），支持快速改表、热更与联调。

### 1.2 读者对象

- **实现局内Gameplay与商店／装备战斗链路的读者**；
- **维护商店与装备 JSON 表项（价格、前置、Buff 绑定等）且尽量减少改脚本频率的读者**；
- **维护验收清单、回放或脚本化冒烟用例的读者**；
- **规划多人权威顺序、快照与重连契约的读者**（预留扩展点）。

### 1.3 术语表

| 术语 | 定义 |
|------|------|
| 商品（Shop Item） | 商店中可展示的条目，对应一条配置，含价格、分类、效果引用等。 |
| 装备实例（Equipment Instance） | 玩家拥有的一件具体物品，含配置 id、实例 id（联机/存档用）、可选层数/充能。 |
| 装备栏（Loadout Slot） | 固定数量的槽位，用于挂载装备实例；槽位规则由产品设计（如 6 大件 + 鞋）。 |
| 唯一被动（Unique Passive） | 同类效果在规则上互斥，通常按「被动组 id」或「配置 flag」实现互斥与替换。 |
| 装备 Buff 绑定（Equipment Buff Binding） | 一条配置：**在装备存在于有效栏位时**对持有者施加的 Buff 描述（`buffId`、等级、可选持续时间覆盖、自定义参数）；卸下时**成对移除**。 |
| 经济（Economy） | 金币获得、成长曲线、团队差等；**收入侧与击杀赏金**见《**MOBA局内经济系统设计文档**》；本文仅保留 **扣费/退款** 与 `ICurrencyWallet` 假设。 |

---

## 2. 范围与约束

### 2.1 范围内（In Scope）

- 局内**商店目录**与**购买/（可选）出售**流程的规则与数据模型；
- **装备栏/背包（若启用）** 的占用、替换、叠加上限；
- 装备/道具对角色的影响以 **多 Buff 挂载** 为主（属性、被动机制、可触发逻辑尽量落在 **Buff 子类** 内）；**主动道具**可另绑技能 id 或短期 Buff（见 §6）；
- 与 **Buff / BuffApplyService（或等价施加入口）** 及 **`BuffData.json`** 的 **buffId** 对齐；与 **技能系统** 共用「表驱动 id → 注册类型 → `BuffManager`」思路；
- **JSON 配置**：局内装备定义、商店条目与 **装备 Buff 绑定列表** 的可加载、可版本化、可校验（见 §6.5）；
- **多人对局**下的权威顺序与状态一致性**设计要点**（不展开具体网络协议）。

### 2.2 范围外（Out of Scope）

- 兵线/野怪/工资等**金币来源**与**经济曲线**（收入侧设计见《**MOBA局内经济系统设计文档**》）；
- 地图机制（多商店、野店、信使运送）的完整玩法设计；
- 完整商业化、反作弊、风控；
- UI 视觉规范与动效细则（仅保留「必要交互与信息架构」级要求）。

### 2.3 假设与依赖

- 存在**可查询的当前货币数量**与**原子扣费**能力（可由调试指令或占位服务提供）；
- 角色战斗属性可通过 **ECS 组件**（如 `EntityDataComponent`）或等价层读取/修改；
- **默认路径**：装备效果 = **一条或多条 Buff 施加**；非经论证不接受在商店模块内直接改写 `EntityDataComponent` 作为常驻装备主通道（特殊一次性 Impact 等另文约定）。

### 2.4 设计原则

1. **单一真理源**：装备对战斗的影响应可追溯到「**`ItemConfig` + `equippedBuffs[]` + `BuffData`**」，与技能管线引用同一 **buffId** 空间。
2. **可逆性**：卸下、出售、替换后，由该装备实例 **挂载的 Buff** 必须全部移除或等价失效（除非产品明确例外）；实现上需 **绑定 `bindingId` ↔ 运行时 `BuffBase` 引用** 或 **可重复的 Remove 语义**（见 §6.4）。
3. **规则可配置**：价格、**Buff 绑定列表**、叠层、唯一性、购买前置均以 **JSON（或生成自 JSON 的 SO）** 驱动，便于快速迭代。
4. **失败可解释**：任何购买/使用失败必须返回**稳定错误码或原因枚举**；Buff 施加失败应记录 **buffId、bindingId、装备 instanceId**。
5. **与技能对齐**：装备侧 **不实现第二套「效果执行器树」**；可选 **IBuffApplicationModifier** 仅作装备专属数值修正，仍产出合法 Buff 请求（与《基于Buff与ECS的技能系统设计文档》一致）。

---

## 3. 系统上下文

```text
┌─────────────────────────────────────────────────────────────┐
│           配置层（StreamingAssets JSON，与技能表同类工具链）      │
│  EquipmentData.json / ShopCatalog.json · schemaVersion       │
│  ItemConfig.equippedBuffs[] → buffId 对齐 BuffData.json       │
└────────────────────────────┬────────────────────────────────┘
                             │ 加载 / 校验 / 热更（可选）
                             ▼
┌─────────────────────────────────────────────────────────────┐
│                      表现层（UI / 输入）                      │
└────────────────────────────┬────────────────────────────────┘
                             │ 购买/出售/使用主动
                             ▼
┌─────────────────────────────────────────────────────────────┐
│              商店与库存模块（Shop & Inventory）              │
│  · 目录查询  · 校验  · 扣费协调  · 栏位变更                    │
└────────────────────────────┬────────────────────────────────┘
                             │ Equipped / Unequipped
                             ▼
┌─────────────────────────────────────────────────────────────┐
│        装备 Buff 应用器（EquipmentBuffApplier，逻辑模块名）      │
│  · OnEquipped：按 equippedBuffs[] 逐项调用与技能同源的施加服务   │
│  · OnUnequipped：按 bindingId / 实例引用 成对 RemoveBuff      │
│  · （可选）IBuffApplicationModifier：仅修正等级/时长，不绕过 Buff │
└────────────────────────────┬────────────────────────────────┘
                             │
         ┌───────────────────┴───────────────────┐
         ▼                                       ▼
┌──────────────────┐                    ┌──────────────────┐
│ BuffApplyService │ ← 与技能系统共用意图 │ 技能/输入（主动装）  │
│ BuffManager      │                    │ 可选触发施法管线    │
└────────┬─────────┘                    └──────────────────┘
         ▼
┌──────────────────┐     属性变化由 Buff 内聚或 Impact 另文约定
│ BuffBase / 池化   │ ←── buffId 注册表（BuffTypeRegistry）
└──────────────────┘

        ┌──────────────────────────────────────┐
        │ 经济子系统（本文档不设计，仅调用接口）  │
        │ TrySpend(currency, amount) → bool      │
        └──────────────────────────────────────┘
```

---

## 4. 功能需求

### 4.1 商店（Shop）

| ID | 需求描述 | 优先级 | 备注 |
|----|----------|--------|------|
| FR-SHOP-01 | 系统应能根据**配置**生成局内可购商品列表（含分类、价格、展示用元数据）。 | P0 | 可先单商店、单目录。 |
| FR-SHOP-02 | 玩家发起购买时，系统应执行**完整校验链**（见 5.2），全部通过后才执行扣费与发货。 | P0 | 原子性：禁止「扣费成功但未入栏」。 |
| FR-SHOP-03 | 购买失败时，系统应返回**明确原因**（枚举/错误码），不得静默失败。 | P0 | 供 UI 与日志使用。 |
| FR-SHOP-04 | 系统应支持**简单购买前置**（如等级下限、阵营限制），由配置驱动。 | P1 | MVP 可全部为「无限制」。 |
| FR-SHOP-05 | 系统应支持**出售或回购**（全价/折价策略可配置）。 | P2 | 首版可关闭或仅允许部分商品。 |
| FR-SHOP-06 | 系统应支持**合成/升级路径**（小件→大件）。 | P2 | MVP 可「仅直购成品」。 |

### 4.2 库存与栏位（Inventory / Loadout）

| ID | 需求描述 | 优先级 | 备注 |
|----|----------|--------|------|
| FR-INV-01 | 系统应为每名角色维护**装备栏槽位**及当前占用情况。 | P0 | 槽位数产品定。 |
| FR-INV-02 | 新装备应能进入**首个可用槽**或触发**替换规则**（配置：拒绝购买 / 弹出替换）。 | P0 | MVP 可用「满则拒绝」。 |
| FR-INV-03 | 系统应支持**堆叠规则**（若存在可堆叠消耗品）：最大层数、合并逻辑。 | P1 | 无消耗品可整节延后。 |
| FR-INV-04 | 系统应对**唯一装备**（Unique）与**唯一被动组**进行占用与互斥管理。 | P1 | 见 §6.5。 |
| FR-INV-05 | 装备实例应具备**稳定实例 id**（便于联机、存档、日志）。 | P1 | P0 可用「槽位+配置id」临时方案。 |

### 4.3 角色影响（装备 = 多 Buff 挂载）

| ID | 需求描述 | 优先级 | 备注 |
|----|----------|--------|------|
| FR-EFF-01 | 每件装备配置应包含 **`equippedBuffs` 列表**（≥0 条）；**穿戴时**对持有者（`target = owner`）逐项完成 Buff 施加，**卸下时**逐项移除或等价失效。 | P0 | 与技能共用 `buffId` / `BuffData` / `BuffTypeRegistry`。 |
| FR-EFF-02 | 单条绑定应支持：**`buffId`、`buffLevel`、可选 `durationOverride`、可选 `customArgs`**（与 `BuffBase.Init` 约定一致）；可选 **per-item-tier 等级缩放**（配置字段，语义对齐技能 `levelScaling`）。 | P0 | 具体字段见 §6.2。 |
| FR-EFF-03 | **Provider 语义**：默认 `provider = 装备持有者`；若需区分「来源为装备」以便驱散/日志，应在 `customArgs` 或扩展 `BuffRuntimeData` 约定中携带 **`itemInstanceId` / `itemConfigId`**（实现阶段定稿）。 | P1 | 避免与技能 Buff 在 `RemoveBuff` 时混淆。 |
| FR-EFF-04 | **主动道具**：可配置 **`activeSkillId`**（走技能门面）或 **单次 Buff 管线**；冷却与沉默规则与技能/道具文档对齐。 | P2 | 底层仍推荐效果落 Buff。 |
| FR-EFF-05 | 装备**生效/失效**须发布领域事件（`OnEquipped` / `OnUnequipped`），且 **Buff 应用器**在事件后完成施加/移除；供 VFX/音效/UI 监听。 | P1 | |
| FR-EFF-06 | 死亡、复活等流程下装备是否保留、Buff 是否暂停，由**产品规则表**驱动；若「死亡保留装备但暂停 Buff」，需在 Buff 或装备层有统一开关。 | P1 | |
| FR-EFF-07 | **配置校验**：加载 JSON 时校验 **`buffId` 在 `BuffData` 中存在**、**`buffId` 已在 `BuffTypeRegistry` 注册**（与技能表一致），失败写入日志并拒绝该条或整表（策略可配置）。 | P1 | 与 `JsonReadResult`、策划 CI 可结合。 |

### 4.4 同步与一致性（多人）

| ID | 需求描述 | 优先级 | 备注 |
|----|----------|--------|------|
| FR-SYNC-01 | 购买、出售、使用主动等改变**权威状态**的操作，应在一个**明确权威端**顺序执行（主机/服务器）。 | P1 | 客户端预测可后置。 |
| FR-SYNC-02 | 断线重连或 late join 应能恢复**装备栏与关键冷却**（简化版可仅支持完整重连）。 | P2 | 需序列化契约。 |

---

## 5. 用例与业务流程

### 5.1 购买（主成功场景）

1. 玩家从 UI 选择商品 `itemConfigId`；
2. 商店服务拉取配置，组装 **PurchaseRequest**（英雄引用、目标槽位可选）；
3. **校验**：价格、栏位、唯一性、前置条件、叠层上限；
4. 调用 **ICurrencyService.TrySpend**（占位接口）；
5. 成功则 **Inventory.ApplyPurchaseResult**（生成实例、写入槽位）；
6. 发布 **EquipmentChanged** 领域事件；
7. **EquipmentBuffApplier**：对新增栏位执行 **`ApplyEquippedBuffs(owner, itemInstance)`**（遍历 `equippedBuffs` → `BuffApplyService`）；对移除栏位执行 **`RemoveEquippedBuffs`**（按 §6.4 追踪关系移除）。

### 5.2 校验链（建议顺序）

1. 配置存在且当前局内可购；
2. 购买前置（等级/阵营等）；
3. 货币充足；
4. 栏位与堆叠规则；
5. 唯一被动/唯一装备互斥；
6. （若启用合成）配方材料是否满足。

任一失败则短路返回原因，**不扣费**。

### 5.3 卸下 / 出售 / 替换

- **卸下**：槽位置空；**EquipmentBuffApplier** 对该 `EquipmentInstance` 已挂载的全部 Buff 执行移除；再发事件（规则可配置：是否允许战斗中途卸下）。
- **出售**：在校验可售后退款（或部分退款），再执行与卸下等价的 **Buff 回滚**。
- **替换**：新装备占用目标槽，旧装备进入背包或销毁；**必须先完成旧装备的 Buff 移除，再施加新装备 Buff**，禁止双份叠加。

---

## 6. 领域模型与数据契约

### 6.1 核心实体（逻辑模型）

- **ShopCatalogEntry**：`entryId`、`itemConfigId`、`display`、`category`、`basePrice`、`tags`（可来自独立 JSON 或与物品表合并）。
- **ItemConfig**（装备/道具）：见 **§6.2**，为本文档推荐的**主配置形态**。
- **EquipmentInstance**：`instanceId`、`itemConfigId`、`ownerHeroRef`、`slotIndex`、`stackCount`、`charges`（可选）；**运行时**可挂 **「bindingId → BuffBase 引用」** 映射（或由 `BuffManager` 查询实现移除）。
- **HeroLoadout**：槽位数组 +（可选）背包列表。

### 6.2 ItemConfig：以「多 Buff 绑定」为中心（与技能步骤类比）

装备在底层视为 **携带有序 Buff 绑定列表** 的物品；**不将「扁平属性数组」作为主配置通道**（若需纯数值，应通过 **仅改属性的 Buff** 或已有 Buff 子类实现，以保持单一真理源）。

**ItemConfig 逻辑字段（与 JSON 字段名可一一映射）**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `itemConfigId` | int | 主键 |
| `displayName` | string | 展示 |
| `slotType` | enum/string | 大件/鞋/消耗等 |
| `maxStack` | int | 可堆叠时上限 |
| `uniqueItem` | bool | 同名配置是否唯一持有 |
| `uniqueGroupId` | string | 唯一被动组（装备规则层互斥，可与 Buff 层协调） |
| `basePrice` | int | 商店售价（经济接口用） |
| `purchasePrerequisites` | object | 等级/标签等，可简化 |
| **`equippedBuffs`** | **array** | **核心**：穿戴时要施加的 Buff 描述列表（顺序建议与施加顺序一致） |
| `activeSkillId` | int? | 主动装：可选，走技能系统 |
| `metadata` | object | 图标路径、描述等 |

**`equippedBuffs[]` 单条结构（对齐技能 `BuffApplicationStep` 的「施加语义」子集，无施法目标解析）**：

| 字段 | 类型 | 说明 |
|------|------|------|
| `bindingId` | string | **稳定键**，用于卸下时定位移除；建议唯一于本 `ItemConfig` 内 |
| `buffId` | int | 与 **`BuffData.json`** / **`BuffTypeRegistry`** 一致 |
| `buffLevel` | uint | 基础 Buff 等级 |
| `levelScalingPerItemTier` | uint | 可选；若装备有强化等级，参与解析（实现与技能 `levelScaling` 类似） |
| `durationOverride` | float? | 可选；缺省则走 `BuffConfig.maxDuration` |
| `customArgs` | array | 可选；透传 `BuffBase.HandleCustomArgs` |

**施加约定**：

- `target`：**始终为装备持有者**（`EntityBase owner`）。
- `provider`：**默认 owner**；日志/驱散若需区分「装备来源」，见 **FR-EFF-03**。

### 6.2.1 穿戴施加流程（实现契约）

1. **校验**：`buffId` 在 `BuffData` 中存在（若已加载）、在 `BuffTypeRegistry` 已注册（与技能表一致）；失败策略见 **FR-EFF-07**。
2. **解析等级**：`effectiveBuffLevel = buffLevel + f(itemTier, levelScalingPerItemTier)`（`itemTier` 无则按 0）。
3. **逐项施加**：按 `equippedBuffs` **数组顺序**调用 **`BuffApplyService.TryApply`**（或团队封装的唯一 Buff 入口），`target = provider = owner`（除非项目对 `provider` 另有约定）。
4. **引用追踪**：每次成功施加后，将 **`bindingId` → `BuffBase` 引用**（或等价句柄）写入 **装备实例运行时状态**，供 §6.4 **卸下时 Remove**；禁止仅依赖「再查一遍同名 Type」作为默认方案。
5. **严格模式（可选配置）**：若 `strictEquipmentBuffApply = true`，任一条 `TryApply` 失败则对本件已施加的 Buff **逐项回滚**并取消本次穿戴；记录 `EquipBuffError_ApplyFailed` / `EquipBuffError_StrictRollback`。非严格模式可跳过失败条并打日志（与 FR-EFF-07 策略一致）。
6. **禁止**在装备/商店模块内绕过 `BuffApplyService`（或团队规定的唯一 Buff 入口）私自 `new BuffBase` 并挂到实体。

### 6.3 JSON 配置与文件组织（快速调试 / 更新）

**目标**：与 **技能表 `SkillData.json`**、**`BuffData.json`** 同类工作流——策划改 JSON → 重进局或（可选）热重载 → 立即验证，减少代码改表成本。

**推荐文件**（可二合一或拆分）：

1. **`EquipmentData.json`**（或 `Items.json`）：`schemaVersion` + `items[]`，每项为完整 **ItemConfig**（含 `equippedBuffs`）。
2. **`ShopCatalog.json`**（可选）：仅商店展示与 `itemConfigId` 引用；或合并进 `EquipmentData.json` 的 `shopEntries[]`。

**加载建议**（与项目 Basement.Json 对齐）：

- 路径：`StreamingAssets`；
- 序列化：`JsonSerializerProfile.GameContent`；
- 解析：`DeserializeWithResult` / `JsonReadResult`，失败打结构化日志；
- **schemaVersion**：预留迁移（`JsonSchemaConstants.schemaVersion` 或本文件根字段），与技能表策略一致。

**根对象示例**（节选，非严格实现代码）：

```json
{
  "schemaVersion": 1,
  "items": [
    {
      "itemConfigId": 2001,
      "displayName": "示例长剑",
      "slotType": "Major",
      "uniqueItem": false,
      "uniqueGroupId": "",
      "basePrice": 350,
      "equippedBuffs": [
        {
          "bindingId": "main",
          "buffId": 5001,
          "buffLevel": 1,
          "levelScalingPerItemTier": 0,
          "durationOverride": null,
          "customArgs": []
        },
        {
          "bindingId": "passive_onhit",
          "buffId": 5002,
          "buffLevel": 1,
          "customArgs": []
        }
      ],
      "activeSkillId": null
    }
  ],
  "shopEntries": [
    { "entryId": 1, "itemConfigId": 2001, "category": "Attack", "sortOrder": 10 }
  ],
  "craftRecipes": [
    {
      "recipeId": 9001,
      "resultItemConfigId": 2002,
      "goldCost": 200,
      "materials": [ { "itemConfigId": 2001, "count": 2 } ],
      "enabled": true
    }
  ]
}
```

详见 **§6.9**，商店侧如何绑定「条目 ↔ 可选配方」见 **§6.10**。

**调试与协作**：

- 版本管理：JSON 与策划表同库，支持 diff 与回滚；
- 可选 **编辑器窗口**：读取同路径 JSON 校验 buffId 存在性与注册表；
- CI：加载全表 + 全 buffId 引用检查 + Registry 覆盖检查。

### 6.4 卸下时 Buff 移除策略（必须实现其一）

| 策略 | 做法 | 适用 |
|------|------|------|
| A. 引用追踪 | 穿戴时在 `EquipmentInstance` 或侧表存 `bindingId → BuffBase`（或句柄），卸下时对每个 `RemoveBuff(owner, buff)` | **推荐**，与现有 `BuffManager` API 对齐 |
| B. 类型 + 来源联合 | 若同类 Buff 仅装备提供，可按 `Type + provider == owner` 查找后移除 | 易误删技能同名 Buff，**慎用** |
| C. 装备专用 Buff 子类 | 每种装备 Buff 独立 `BuffBase` 子类，移除时 `FindBuff<T>` | 类型爆炸，不推荐作默认方案 |

**禁止**：卸下后仍残留装备 Buff，或重复穿戴导致叠层泄漏。

### 6.5 唯一性与叠加（装备规则层）

- **唯一装备（Unique Item）**：同一 `itemConfigId` 或 `uniqueItem=true` 时，已持有则拒绝再次购买或触发替换策略。
- **唯一被动组（Unique Passive Group）**：`uniqueGroupId` 相同则互斥；与 **Buff 层唯一性** 并存时，以**产品裁定**哪一层优先（推荐：装备层先拦截购买，Buff 层处理同 Buff 合并规则）。

### 6.6 错误与事件（建议）

**商店错误码（示例）**

- `ShopError_None`
- `ShopError_ItemNotFound`
- `ShopError_NotPurchasable`
- `ShopError_InsufficientCurrency`
- `ShopError_InventoryFull`
- `ShopError_UniqueConflict`
- `ShopError_PrerequisiteNotMet`
- `ShopError_SpendFailed`
- `ShopError_SellNotAllowed`、`ShopError_RecipeDisabled`
- `ShopError_CraftInsufficientMaterials`、`ShopError_CraftConsumeFailed`、`ShopError_CraftRecipeMismatch`

**装备 Buff 错误（示例）**

- `EquipBuffError_BuffDataMissing`（`BuffData` 无此 `buffId`）
- `EquipBuffError_BuffNotRegistered`（`BuffTypeRegistry` 未注册）
- `EquipBuffError_ApplyFailed`（`TryApply` 返回 false）
- `EquipBuffError_RemoveFailed`（卸下时 `RemoveBuff` 失败）
- `EquipBuffError_StrictRollback`（严格模式下穿戴回滚）

**领域事件（示例）**

- `EquipmentEquipped(hero, instance, slot)`
- `EquipmentUnequipped(hero, instance, slot)`
- `EquipmentBuffsApplied(hero, instance, bindingIds[])`
- `EquipmentBuffsRemoved(hero, instance, bindingIds[])`
- `ShopPurchaseSucceeded(hero, itemConfigId, instance)`（含直购与合成成功后「发货」链路，见 §6.10）
- `ShopPurchaseFailed(hero, itemConfigId, reason)`
- `EquipmentSold(hero, itemConfigId, goldEarned, …)`
- `EquipmentCraftSucceeded(hero, recipeId, resultInstance, …)`

### 6.7 出售（Sell）

物品表可选字段：`sellable`（`false` 时禁止售出）、`sellRefundRatio`（省略则默认 **0.5**）；返还金币 \(\texttt{Floor(basePrice × 比例)}\)。

实现：`Gameplay.Shop` 下 **`EquipmentSellService`**：可售校验 → **`PurchaseService.TryUnequipSlot`** → `Currency.TryRefund`。

### 6.8 回购（Buyback）

**暂无独立回购服务与设计落地**（FR-SHOP-05 预留）。售出后即卸下并销毁实例，不保留「本局回购队列」。若产品上需要，再在配置与状态中单独约定。

### 6.9 合成（Craft）与 JSON：`craftRecipes[]`

局内 **`EquipmentData.json`** 根级可选 **`craftRecipes`**，由 **`EquipmentDataLoader`** 载入 **`CraftRecipeCatalog`**。

| 字段 | 说明 |
|------|------|
| `recipeId` | 配方 id，供 **`CraftingService.TryCraft`** 选择 |
| `resultItemConfigId` | 成品须在 `items[]` 中存在 |
| `goldCost` | **手续费（金币）**，与成品直购 **`basePrice`** 独立 |
| `materials[]` | `itemConfigId` + `count`，从英雄当前 **装备栏实例**扣除 |
| `enabled` | `false` 时配方禁用 |

管线：**扣手续费 → 扣材料 → `PurchaseService.TryGrantPurchasedItem` 入账并施加 Buff**。成功时 **`EquipmentCraftSucceeded`**；发放阶段仍会触发 **`ShopPurchaseSucceeded`**（与直购同源），UI **可任选其一监听或两处合并展示**。

材料已扣、`TryGrantPurchasedItem` 仍失败时为**非正常路径**，经济上应退手续费；**材料回档需在事务层补完**（实现侧已打错误日志占位）。

### 6.10 商店条目与「合成购买」（统一收口）

商店展示行 **`shopEntries[]`** 的 `itemConfigId` 若与某配方 **`resultItemConfigId`** 相同，且 **`enabled`**，则运行时 **`ResolvedShopEntry.CraftRecipeIds`** 会挂载所有匹配配方 id；与是否同时 **直购**（`PurchaseService.TryPurchase` 扣 `basePrice`）互不排斥（产品可只对大件开放其一）。

Gameplay 收口 API：

| 类型 / 方法 | 说明 |
|-------------|------|
| `ShopAcquireMode` | **`DirectGoldPurchase`**：扣 `basePrice` 直购；**`CraftRecipe`**：走合成 |
| `ShopAcquisitionService.TryAcquireFromShopEntry(hero, heroLevel, shopEntryId, mode, recipeId, equipOptions)` | `mode=CraftRecipe` 时 **`recipeId` 必须属于该行 `CraftRecipeIds`**，且与 **`CraftRecipeCatalog`** 校验 `resultItemConfigId ==` 条目 `itemConfigId`，否则 **`ShopError_CraftRecipeMismatch`** |

单机 UI 推荐使用 **`TryAcquireFromShopEntry`**，避免手写 `itemConfigId` 与配方结果不一致的安全问题。

---

## 7. 模块划分与接口（逻辑接口）

> 以下为**逻辑边界**，实现时可合并为 fewer 类，但职责不得混淆。

| 模块 | 职责 | 对外接口（示例） |
|------|------|------------------|
| ShopCatalog (`ShopCatalog` + `ResolvedShopEntry`) | 商店目录与展示数据；挂载 **`CraftRecipeIds`**（`resultItemConfigId ==` 本条 `itemConfigId` 的启用配方） | `GetFiltered`, `TryGetEntry` |
| **`ShopAcquisitionService`** | **商店统一购买**：按 `ShopAcquireMode` 转 **`TryPurchase`** 或 **`CraftingService.TryCraft`**（§6.10） | `TryAcquireFromShopEntry` |
| PurchaseService | 直购：**校验 + `TrySpend(basePrice)` + `TryGrantPurchasedItem`**；合成与子系统复用 **`TryGrantPurchasedItem`** | `TryPurchase`、`TryGrantPurchasedItem`、`TryUnequipSlot` |
| **CraftRecipeCatalog / CraftingService** | `craftRecipes` 载入、`TryCraft` | `RebuildFrom`、`TryCraft(recipeId)` |
| InventoryService | 槽位、堆叠、实例生命周期 | `TryAddItem`, `TryRemoveItem`, `TrySwapSlot` |
| CurrencyGateway | 与经济系统隔离的扣费门面 | `TrySpend`, `TryRefund`（占位） |
| **EquipmentBuffApplier** | **穿戴**：按 `equippedBuffs[]` 调 `BuffApplyService`；**卸下**：按 §6.4 追踪移除；可选调用 **`IBuffApplicationModifier`** | `ApplyEquippedBuffs(owner, instance)`, `RemoveEquippedBuffs(instance)` |
| ItemConfigLoader | **JSON** 加载、`schemaVersion`、**buffId 引用校验** | `LoadFromStreamingAssets()`, `Reload()`（可选） |
| EquipmentRuleEngine | 唯一性、互斥、叠层 | `ValidateLoadoutChange` |

**依赖方向**：`PurchaseService` → `CurrencyGateway`、`InventoryService`、`EquipmentRuleEngine`；`InventoryService` → 领域事件 → **`EquipmentBuffApplier`**；**`EquipmentBuffApplier` → `BuffApplyService` / `BuffManager`（禁止反向依赖）**；`ItemConfigLoader`（实现名可为 **`EquipmentDataLoader`**）→ Basement.Json；主动装可调用 **技能门面**（`SkillExecutionFacade`，单向）。属性变化**默认由 Buff 内聚**，不强制 `EquipmentBuffApplier` 直接写 `EntityDataComponent`。

---

## 8. 非功能需求

| 类别 | 要求 |
|------|------|
| 性能 | 单次购买与**整件装备的 Buff 批量施加**应在单帧或可接受延迟内完成；避免每帧全量重载 JSON。 |
| 可维护性 | **装备效果以 JSON 为主**；与 `SkillData.json` / `BuffData.json` 同仓管理；错误码与事件稳定。 |
| 可测试性 | **配置校验单测**：buffId 存在性、`BuffTypeRegistry` 覆盖、重复 `bindingId`；穿戴/卸下 Buff 对称性集成测试。 |
| 可观测性 | 日志必含：`itemConfigId`、`instanceId`、`bindingId`、`buffId`、失败原因枚举。 |
| 配置工作流 | 支持 **StreamingAssets** 下改表即验；可选编辑器 **Reload**、CI **全表加载**。 |
| 安全（联机） | 客户端仅发**意图**；权威端校验 **itemConfigId**、价格与 **配置版本**（如 `schemaVersion` + 条目哈希或内容版本号），防止篡改 `equippedBuffs` 刷 Buff；运行时以权威快照为准。 |

---

## 9. 测试策略（概要）

- **单元测试**：校验链（钱不够、栏位满、唯一冲突、堆叠满）；**JSON 加载**（`schemaVersion`、缺字段、`buffId` 不存在）。
- **集成测试**：购买 → **Buff 挂载**（条数与 `bindingId` 一致）→ 卸下 → **Buff 全部移除**；与 **同名技能 Buff** 共存时是否误删。
- **回归**：与技能、沉默、复活流程的交叉用例（按产品规则表选取）；**改 `EquipmentData.json` 仅增删 `equippedBuffs` 条**的冒烟用例。

---

## 10. 实施路线

| 阶段 | 目标 | 交付物 |
|------|------|--------|
| MVP | 可购 + 扣费占位 + 装备栏 + **`EquipmentData.json` + `equippedBuffs[]`** + **`EquipmentBuffApplier`** | JSON Loader、目录、穿脱时 **BuffApplyService** 全链路、`bindingId` 追踪移除 |
| P1 | 唯一性/堆叠、配置校验（BuffData/Registry）、严格模式回滚、`Equipped`/`BuffsApplied` 事件 | `EquipmentRuleEngine`、CI 校验脚本 |
| P2 | 主动装（`activeSkillId`）、合成树、出售/回购 | 与 `SkillExecutionFacade` 对接、配方 JSON |
| P3 | 完整联机快照与预测（若需要） | 序列化与权威同步方案 |

---

## 11. 风险与缓解

| 风险 | 影响 | 缓解 |
|------|------|------|
| 装备绕过 Buff 直接改属性 | 与技能/Buff 双真理源 | **强制** `EquipmentBuffApplier` → `BuffApplyService`；代码评审门禁 |
| `buffId` 与技能/Buff 表不同步 | 穿戴失败或静默跳过 | FR-EFF-07 校验 + CI；`EquipBuffError_*` 可观测 |
| 卸下时误删技能 Buff | 玩法事故 | §6.4 **引用追踪**策略 A；禁止默认采用策略 B |
| 唯一被动（装备层 vs Buff 层） | 规则打架 | §6.5 产品裁定 + 文档化优先级 |
| JSON 热更与联机状态 | 不一致 | 权威端版本号；客户端仅拉表不改运行时 |
| 经济后接 | 扣费接口不兼容 | `CurrencyGateway` 保持稳定 DTO |

---

## 12. 文档维护

- **局内金币收入、击杀赏金与 `ICurrencyWallet` 多分钱包演进**见《**MOBA局内经济系统设计文档**》；扣费语义仍以本文 **`PurchaseService`** 为准；
- 与《角色属性系统设计文档》《基于Buff与ECS的技能系统设计文档》冲突时，以**属性与 Buff 真理源**文档为准修订装备效果章节；
- **装备 `equippedBuffs` 与技能 `BuffApplicationStep` 共用 `buffId` 与施加语义时**，两文档应交叉引用，避免 `BuffTypeRegistry` 登记策略分裂。

---

## 13. 参考资料（项目内）

- 《**MOBA局内经济系统设计文档**》：局内收入来源、ECS 赏金绑定、击杀结算与 **`ICurrencyWallet`** 收口；
- 角色属性与 ECS：`EntityDataComponent` 及相关枚举；
- 效果落地：**`BuffManager` / `BuffBase`**；Impact 系统（若采用）另文约定；
- 配置管线：**Basement Json**（`JsonSerializerProfile.GameContent`、`DeserializeFromFilePath`、`JsonReadResult`）、`StreamingAssets`；
- 实现参考（若已落地）：`Gameplay.Skill`（技能 JSON 与 Buff 注册模式可与装备 Loader 对齐）。
