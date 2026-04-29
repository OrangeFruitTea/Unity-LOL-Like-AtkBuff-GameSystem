# 网络同步服务设计说明书（Mirror）

| 属性 | 说明 |
|------|------|
| 文档类型 | 软件设计说明（轻量化、可对齐毕业论文「系统设计与实现」章节） |
| 版本 | 2.0 |
| 运行时依赖 | Unity 项目已集成 **Mirror**（`Assets/ThirdParty/Mirror`）；传输层以实现为准（如内置 Telepathy/KCP） |
| 对齐资产 | Mirror 自带的 **脚本模板**：`ThirdParty/Mirror/ScriptTemplates`（本章 §3 逐项映射） |
| 外文参考 | Mirror 文档：[Network Manager](https://mirror-networking.gitbook.io/docs/components/network-manager)、[NetworkBehaviour](https://mirror-networking.gitbook.io/docs/guides/networkbehaviour) |

---

## 1. 摘要

本说明书定义游戏客户端在多人场景下的**网络同步边界**与**与 Mirror API 的对齐关系**。设计目标面向**本科毕业设计**常见规模：**局域网（LAN）对战可玩、服务端权威清晰、易于演示与答辩**，同时为将来接入远程服务器保留**最小扩展点**。

**不在本文档范畴**：底层传输协议选型细节、ECS 与非 Mirror 逻辑的接口实现代码、商业化反作弊与安全运营体系。

---

## 2. 目标与非目标

### 2.1 设计目标（必须）

| ID | 目标 | 验收要点 |
|----|------|----------|
| G-ARCH | **架构可追溯**：任一网络行为能说清「谁权威、在哪一侧执行、如何用 Mirror API 落地」 | 答辩时可对照本章表格说明 |
| G-AUTH | **服务端权威**：位置/血量/胜负等Gameplay状态以服务端为准 | 不出现「客户端单方改关键状态且无校验」的长期方案 |
| G-MIRROR | **与 Mirror 原生模型一致**：`NetworkManager`、`NetworkBehaviour`、生成物（Spawn）、消息模式 | 新代码优先从 ScriptTemplates 派生而非重复造轮子 |
| G-LAN | **局域网可达**：同网段宿主/客机可连、断线有可解释行为（至少日志+UI反馈） | 演示视频可录制 |
| G-DEMO | **可维护的适度抽象**：会话、房间、关卡切换有清晰状态机叙述 | 代码量与复杂度适合单人毕业设计 |

### 2.2 非目标（明确排除或降级）

| 类型 | 说明 |
|------|------|
| 大规模分布式 | 不设计分片大区、不写复杂网关 |
| 完整延迟补偿射击 | **标注为未来工作**；本篇仅预留「插值预测」原则性描述 |
| 数据压缩专有协议 | 以 Mirror 内置序列化体积与按需同步为主；专有压缩非必需 |
| 全球低延迟 QoS | LAN 与外网占位接口即可 |

---

## 3. 与 Mirror 脚本模板对齐

项目内已通过 Unity 菜单 **Create → Mirror → …** 生成脚本时使用以下模板路径（与实际文件名一致）：

| Script Template（`ThirdParty/Mirror/ScriptTemplates`） | 用途 | 本设计引用方式 |
|----------------------------------------------------------|------|----------------|
| `50-Mirror__Network Manager-NewNetworkManager.cs.txt` | 自定义 **NetworkManager**、生命周期钩子 | §6.2 会话与场景 |
| `51-Mirror__Network Manager With Actions-NewNetworkManagerWithActions.cs.txt` | 在钩子中转发 **UnityEvent/Action**，利于 UI/系统解耦 | 可选：**演示层**与 **业务层**解耦时使用 |
| `52-Mirror__Network Behaviour-NewNetworkBehaviour.cs.txt` | **NetworkBehaviour**：`OnStartServer` / `OnStartClient` 等 | §5 同步构件 |
| `53-Mirror__Network Behaviour With Actions-NewNetworkBehaviourWithActions.cs.txt` | 生命周期 **Action**，便于拼装子系统监听 | §7 推荐用于「薄适配层」 |
| `52-Mirror__Network Authenticator-NewNetworkAuthenticator.cs.txt` | **NetworkAuthenticator**：入网前握手 | LAN 可走空实现或使用模板扩展令牌；外网占位 |
| `54-Mirror__Network Room Manager-NewNetworkRoomManager.cs.txt` | **NetworkRoomManager**：房间槽位与 Ready→切局 | §6.4 组队/房间内流程（可选用） |
| `55-Mirror__Network Room Player-NewNetworkRoomPlayer.cs.txt` | **NetworkRoomPlayer** 预制体配套 | 与 Room Manager 成对使用 |
| `56-Mirror__Network Discovery-NewNetworkDiscovery.cs.txt` | **局域网发现** UDP 广播 | §8 LAN 演示 |
| `57-Mirror__Network Transform-NewNetworkTransform.cs.txt` | 位姿同步（或等价组件） | 移动体同步优先考虑官方方案 |
| `54-Mirror__Custom Interest Management-CustomInterestManagement.cs.txt` | **兴趣管理 AoI** 自定义 | §5.5 可选优化（非 MVP 必选） |

> **约定**：上文「文件名」均以仓库内 Mirror 附带模板为准；实现类名由项目自定，说明书只约束**职责分层**与**钩子的语义**。

---

## 4. 总体架构

### 4.1 分层视图

逻辑上仍可区分「业务Gameplay」与「网络适配」，但**不得在业务层手写 socket**；适配层只做 **Mirror 语义封装**。

```text
┌─────────────────────────────────────────────────────────┐
│  Gameplay（角色、技能、局内规则）                          │
│  — 仅以「权威状态或服务端入口」改写世界                     │
└───────────────────────────┬───────────────────────────────┘
                            │ 调用服务端入口 / 读同步状态
┌───────────────────────────▼───────────────────────────────┐
│  Network Adaptation（薄层）                               │
│  · SyncVar / Hooks · [Command] / ClientRpc TargetRpc      │
│  · Serialize / Spawn 约定 · Messages（必要时）               │
└───────────────────────────┬───────────────────────────────┘
                            │ Mirror API
┌───────────────────────────▼───────────────────────────────┐
│  Mirror                                                   │
│  NetworkServer / NetworkClient · Spawn · Identity ·       │
│  Transport                                                │
└───────────────────────────────────────────────────────────┘
```

### 4.2 权威的单一来源

| 类别 | 推荐做法 |
|------|----------|
| 可推导状态（生命、是否在技能中、局内阶段） | **服务端计算 + SyncVar/同步结构** 下发 |
| 玩家意图（移动、施法、购买） | 客户端 **Command**/**消息**上报，服务端校验后执行 |
| 纯表演（音效、弹道特效） | 客户端本地或经由 **Rpc/ClientRpc** 触发 |

---

## 5. 核心同步构件（不写实现代码）

### 5.1 NetworkIdentity 与 NetworkBehaviour

每个需网络存在的场景对象挂载 **NetworkIdentity**；其上 **NetworkBehaviour** 子脚本承担同步逻辑。

- **生命周期语义**（与模板注释一致）：`OnStartServer` / `OnStartClient` / `OnStartLocalPlayer` 等；
- **`OnStartLocalPlayer`**：仅本地玩家初始化输入、相机监听等；
- **`hasAuthority`**：与对象归属结合使用（见 Mirror 文档），避免误判「谁在改状态」。

### 5.2 状态同步：SyncVar 与钩子

原则：**少而稳**——仅同步答辩与玩法必需字段；大变体用 `[SyncVar(hook)]` 或拆分为多个 SyncVar。

| 准则 | 说明 |
|------|------|
| 粒度 | 优先按「逻辑属性」拆 SyncVar，避免单巨型结构体频繁整包变化 |
| Hook | 表现层（UI/特效）放在 hook 或客户端侧只读分支，避免在 hook 内再改权威数据 |
| 初始化 | 依赖 `OnStartClient` 时 SyncVar 已就绪的语义（见模板说明） |

### 5.3 玩家意图：Command / RPC

| 方向 | 机制 | 适用 |
|------|------|------|
| 客户端→服务端 | `[Command]`（或对应 API） | 移动、施法请求、交互 |
| 服务端→客户端 | `ClientRpc` / `TargetRpc` | 结算广播、仅本人提示 |

**校验清单（答辩可讲）**：频率限制、参数范围、当前游戏阶段是否允许、服务端是否拥有目标实体。

### 5.4 生成与销毁（Spawn）

- 玩家对象：由 **NetworkManager** 的 `playerPrefab` 与 `OnServerAddPlayer` 等管线配置（见 Network Manager 模板）；
- 动态实体：仅在服务端 `NetworkServer.Spawn`，客户端禁止「本地 Instantiate 当网络实体」长期存在。

### 5.5 兴趣管理（可选）

若同场景实体数量大，再考虑 **Custom Interest Management** 模板；毕业设计 LAN 规模通常可延后。

---

## 6. 会话、场景与房间

### 6.1 NetworkManager 角色

负责 **启动/停止** Host、Server、Client，及 **场景在线切换**（`ServerChangeScene` 等）。实现时建议直接继承模板 `NetworkManager` 或 `NetworkManager With Actions`，在子类中**仅覆写需要的虚方法**，避免复制整份空实现造成维护负担。

### 6.2 关键回调（逻辑职责，非代码）

| 场景 | 典型钩子（名称以 Mirror 为准） | 设计说明 |
|------|----------------------------------|----------|
| 主机/服务启动 | `OnStartHost` / `OnStartServer` | 初始化局内单例、注册自定义消息（若需要） |
| 客户端启动 | `OnStartClient` | UI 状态、输入策略 |
| 连接/断开 | `OnServerConnect` / `OnServerDisconnect`、`OnClientConnect` / `OnClientDisconnect` | 日志、重连提示、清理临时数据 |
| 场景切换 | `ServerChangeScene` / `OnServerSceneChanged` / `OnClientSceneChanged` | **同一套关卡流程在服务端驱动** |

### 6.3 场景同步策略

- **Online/Offline 场景**：可在 NetworkManager 上配置 `offlineScene` / `onlineScene`（以项目配置为准）；
- **切局时**：注意 Mirror 对 **Client Ready** 的默认行为；答辩需说清「何时再次 Ready、何时生成玩家」。

### 6.4 NetworkRoomManager（可选）

若毕业设计需要 **大厅 → 全员 Ready → 开局** 的完整演示链，优先采用 **NetworkRoomManager + NetworkRoomPlayer** 模板，避免自建房间状态机与 Mirror 场景流冲突。

| 能力 | 说明 |
|------|------|
| 槽位与人数上限 | 模板内建房间玩家槽位 |
| Ready 聚合 | `OnRoomServerPlayersReady` 前可插入倒计时等 |
| 房间→局内 | `OnRoomServerCreateGamePlayer` 等定制生成逻辑 |

若项目极简（仅「开始游戏即进局」），可暂不用 Room，改由单一 NetworkManager 承担。

### 6.5 认证（Authenticator）

- **LAN 演示**：可使用模板默认的「空认证」或极简握手；
- **外网预留**：在 `NetworkAuthenticator` 子类中扩展 **AuthRequestMessage/AuthResponseMessage** 负载（字段由论文需求定义），本说明书不固定报文格式。

---

## 7. 「网络同步服务」逻辑模块划分

> 以下名称表示**逻辑职责**，与旧版文档中「XX管理器」类名不必一一对应；实现时多数能力应落在 **NetworkBehaviour + NetworkManager 子类** 中，避免再维护一套与 Mirror 平行的「第二网络层」。

| 逻辑模块 | 职责 | Mirror 落点 |
|----------|------|-------------|
| 连接与会话 | Host/Client 启停、断线处理 | `NetworkManager` 子类、`OnClientDisconnect` 等 |
| 房间与匹配（可选） | 房间槽、Ready、切局 | `NetworkRoomManager` / `NetworkRoomPlayer` |
| 实体状态同步 | 位置、血量、局内枚举 | **带 SyncVar / Transform 组件**的 `NetworkBehaviour` |
| 事件与通知 | 技能结算、播报 | `ClientRpc`、`TargetRpc`、或 `NetworkMessage`（按需） |
| 延迟与体验（轻量） | 插值、输入缓冲简述 | **客户端预测/插值**限制在局部脚本，不上升到全局第二条网络栈 |

**删除旧文档中独立「延迟处理器」「冲突解决器」作为必备类的假设**——毕业设计应将**冲突消除在服务端裁决**，客户端仅呈现；复杂回滚可作为**展望**单列一节即可。

---

## 8. 局域网（LAN）与发现

### 8.1 目标

同一路由器网段内，**主机创建房间**，**客机发现并加入**（或通过 IP 直连）。

### 8.2 对齐实现

优先使用 **`NetworkDiscovery` 脚本模板**，将「广播端口、键值、会话信息」写入设计参数表（论文中可单列配置节）。

### 8.3 限制说明

- 发现服务与 **防火墙**、**多网卡** 相关的问题，在论文「测试环境」中说明测试机设置；
- **NAT 打洞、公网直连** 不作为本篇必达项。

---

## 9. 外网与传输层（预留）

| 项目 | 说明 |
|------|------|
| 远程主机 | 固定 IP 或域名 + 端口的手动输入即可作为**最小外网 demo** |
| 传输抽象 | Mirror 已抽象 Transport；换 KCP/Telepathy 属**配置/部署**变更，本设计不展开 |
| 数据安全 | 毕设阶段可为**信任模型**；若论文需要，可描述 TLS/专用协议为**未来工作** |

---

## 10. 性能与可扩展性（轻量化指标）

| 类别 | 建议 |
|------|------|
| 同步频率 | Transform/状态按组件默认与项目需要调节；避免每帧全量广播大结构 |
| 事件合并 | 同一逻辑帧多事件可合并为单 Rpc 或单消息（按需） |
| 可观测性 | 关键路径 `Debug.Log` 可开关；答辩演示用统一日志前缀 |

---

## 11. 测试与验收（适合毕设答辩）

| 类型 | 用例示例 |
|------|----------|
| 功能 | LAN 两台机 Host+Client，进同局；断线一端，另一端行为符合预期（不崩溃） |
| 同步 | 服务端改血量，客机 UI/表现一致 |
| 安全基线 | 客户端修改本地缓存数值不能持久影响服务端裁决（任选一种玩法现场试） |

---

## 12. 风险与缓解

| 风险 | 缓解 |
|------|------|
| 业务层与 Mirror 双轨并行 | **禁止**长期维护两套网络栈；新业务只走 Mirror 钩子 |
| 场景流与自建房间冲突 | 若用 Room，则房间状态跟 **NetworkRoomManager** |
| 过度设计预测回滚 | 毕设先做权威+插值，预测留论文「展望」 |
| ScriptTemplates 与引擎版本漂移 | Mirror 升级后复查模板文件名与钩子签名 |

---

## 13. 实施路线（建议）

| 阶段 | 范围 | 交付 |
|------|------|------|
| MVP | `NetworkManager` + 玩家 `NetworkBehaviour` + SyncVar + 基础 Command | LAN 可玩最小局 |
| P1 | `NetworkDiscovery` 或固定 IP 入房；断线提示 | 演示流程完整 |
| P2 | 可选 `NetworkRoomManager`；Authenticator 扩展草稿 | 大厅 Ready 与「像产品」一点的体验 |

---

## 14. 参考

- Mirror 官方文档与 API（见各 ScriptTemplates 文件头注释中的链接）。
- 项目内脚本模板目录：`Assets/ThirdParty/Mirror/ScriptTemplates`。
- 项目内其它设计文档（若存在）：局内规则、技能与 Buff 权威说明需与本篇 **「服务端权威」** 一致。

---

## 附录 A：与旧版 v1.x 文档的关系

旧版（v1.x）以**大段示例代码**堆叠「连接管理器、房间管理器、状态同步器」等，易与 Mirror 自带职责重复，且 API 版本（如 `NetworkConnection` 与 `NetworkConnectionToClient`）可能随 Mirror 升级而变化。

**v2.0** 将上述内容抽象为：

- **职责表**（本章 §7）；
- **与 ScriptTemplates 的映射**（§3）；
- **Mirror 原生生命周期**（§5、§6）。

实现代码应在 Unity 工程内由 ScriptTemplates 生成后再行填充，**本说明书不替代 API 文档**。
