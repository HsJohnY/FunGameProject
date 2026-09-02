# ADR-0003：M3 使用 NGO 与 Unity Transport

状态：已接受（2026-09-02）

## 背景

M3 需要在 Unity `6000.0.38f1` 中验证 2 人监听主机、真实双进程、玩家移动、共享任务物、主机权威事故链和退出清理。项目现有玩法由 GameObject、MonoBehaviour 和普通 C# 规则组成，不使用 DOTS，也不需要大规模玩家或专用服务器。

## 决定

- 高层网络框架采用 `com.unity.netcode.gameobjects@2.7.0`；
- 传输层采用 NGO 默认集成的 Unity Transport；
- 本地编辑器辅助测试采用已发布版 `com.unity.multiplayer.playmode@1.3.3`；
- M3 先支持本机和局域网显式地址，不安装 Multiplayer Services、Relay、Lobby 或 Authentication；
- 普通好友的邀请码/Relay 流程在本地权威切片通过后单独验证，届时使用 Unity 6 的统一 Multiplayer Services 包，不采用已弃用的独立 Relay SDK 新接入；
- 事故状态、任务物归属和关键交互由监听主机判定；客户端所有权只用于本地玩家输入和允许的移动表现；
- 不启用 NGO 的分布式权威模式，不迁移到 Netcode for Entities。

## 理由

Unity 官方将 NGO 定位为少量玩家、较简单逻辑以及为现有 GameObject 单机项目增加联机能力的方案；这与当前项目结构一致。NGO、Unity Transport、Multiplayer Play Mode 和未来 Relay 位于同一官方工具链，减少 Unity 版本兼容和后续邀请码接入成本。

Mirror 与 FishNet 都能实现监听主机，也各有成熟能力；当前没有预测复杂物理、大规模并发或必须依赖其特有 API 的需求。为一个练手和好友联机项目同时承担第三方框架升级、Unity Transport/Relay 适配和更多维护边界，收益不足。

## M3 边界

M3 只建立：

1. 监听主机与客户端启动/停止；
2. 两个真实进程连接；
3. 两名玩家生成、所有权和移动同步；
4. 主工具、任务物和固定事故链的主机权威同步；
5. 客户端退出安全清理、房主退出结束会话；
6. 延迟、抖动和丢包条件下的开发测试。

M3 不实现公开匹配、互联网邀请码、主机迁移、远征中加入、断线重连、语音或商业反作弊。

## 验证与回退

- 包安装后必须通过现有 EditMode、PlayMode 和 M1 场景回归；
- M3-1 必须由两个真实进程完成连接与退出，不能只以同一编辑器内模拟代替；
- 如果 NGO 无法满足任务物所有权或主机权威交互，先记录最小复现和缺口，再重新比较 Mirror/FishNet；不得同时维护两套网络实现。

## 主要官方资料

- Unity 6 NGO 包版本与兼容性：https://docs.unity3d.com/6000.0/Manual/com.unity.netcode.gameobjects.html
- Unity 网络框架选择：https://docs.unity.com/multiplayer/netcode/netcode
- Unity 网络测试工具：https://docs.unity.com/en-us/multiplayer/netcode/networking-utilities
- NGO 与 Relay 集成：https://docs.unity.com/en-us/relay/relay-and-ngo
