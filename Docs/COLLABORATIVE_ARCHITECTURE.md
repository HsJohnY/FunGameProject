# 模块化协作架构

开始编辑前必须完整阅读仓库根目录 `AGENTS.md`。本文件解释资源的权威来源、模块边界和运行时装配方式。

## 权威来源与编辑位置

| 改动 | 权威来源 | 约束 |
| --- | --- | --- |
| 静态舱室外观、环境碰撞 | `Assets/Game/Content/Modules/Art/Environment/*.prefab` | 每个舱室独立编辑，不加入业务脚本或 NetworkObject |
| 第一人称工具外观 | `Modules/Art/Player/*.prefab` | 维护挂点和模型尺寸，不修改玩家输入及网络组件 |
| 敌人附属模型 | `Modules/Art/Enemies/*.prefab` | 可独立调整模型、材质；根实体上的碰撞和受击 Renderer 由玩法模块维护 |
| 单人玩家、敌人、交互实体、事故、遭遇 | `Modules/Player`、`Enemies`、`Entities`、`Incidents`、`Encounters` | 使用 Prefab Mode；外部连接由组合场景保留 |
| UI 与会话入口 | `Modules/UI`、`Modules/Session` | UI 不拥有玩法进度；网络会话对象由会话模块管理 |
| 敌人数值、事故温度、工具冷却、波次部署 | `Modules/Configuration` 对应子目录 | ScriptableObject 运行时只读；每项内容单独资产 |
| 网络玩家与服务器生成对象 | `Assets/Game/Content/Networking` | 保留现有资源 GUID；不能通过旧生成器重新创建 |
| 跨模块连接、主菜单启动 | `Assets/Game/Scenes/SinglePlayer_ThreeChapterDemo.unity` | 仅组合 Prefab、连接引用及配置加载器；由集成任务修改 |
| Additive Scene 清单 | `Modules/Configuration/Scenes/*.asset` | 新场景登记属于集成边界，路径必须纳入构建列表 |
| 环境场景产物 | `Assets/Game/Scenes/Environment/*.unity` | 只引用源环境 Prefab；禁止手工修改，使用显式生成入口 |

上述路径中的 `Modules` 均指 `Assets/Game/Content/Modules`。目录用于分配文件所有权，不意味着每个目录必须固定由一个人负责；任务开始时说明本次编辑范围。

## 场景与生命周期

主组合场景保留原来的单人/合作入口，实例化的是独立玩法 Prefab。`ExpeditionEnvironmentLoader` 在激活玩法与网络会话前加载冷却舱、配电舱和风暴核心三个 Additive 环境场景。`SharedMapModeController.IsReady` 表示依赖已就绪。

这三个 Additive Scene 只包含静态环境和碰撞，不含 NetworkObject。每个客户端先装入相同环境，再开放房间入口；晚加入和重连仍由现有 NGO 动态生成机制同步玩家、事故、战斗与章节。当前没有把网络业务对象迁移到 Additive Scene，也没有引入按距离动态卸载。未来需要这样做时，必须单独接入 NGO NetworkSceneManager 并验证场景同步事件。

章节控制器明确通知环境加载器更新可见性。单人未解锁舱室的装饰保持隐藏，合作模式沿用既有可见性策略；门禁和会动的交互模型保留在拥有它们的玩法 Prefab。卸载主组合场景时，加载器释放自己加载的环境；退出游戏时不再安排异步卸载。

跨 Prefab 引用保留为组合场景或父组合 Prefab 的显式引用覆盖，不使用反射在运行时注入私有字段，不在 Additive Scene 之间序列化对象引用。外观 Prefab 的修改不需要重新保存这些连接。

`ExpeditionContext` 通过战役和冷却战斗的显式遭遇引用登记敌人。向已有遭遇添加敌人时维护该遭遇的列表和必要的共享目标连接，不需要维护主场景中的另一份敌人总表。网络敌人传输稳定 `TargetId`，接收方通过登记表解析，不再用场景扫描排序的数组下标决定模板。新增敌人必须提供唯一且稳定的 ID；改名显示文本不能改变身份。

## 配置与运行状态

`EnemyDefinition` 保存敌人行为参数及其对工具的响应，单人实体与联网代理读取同一份参数。`CoolingIncidentDefinition` 同时被单人事故和联网事故引用。`ToolDefinition` 保存独立工具冷却，单人与联网玩家的 ToolController 引用相同资产。`EncounterDefinition` 用敌人 ID 保存部署间隔和简报。

血量、温度、任务阶段、冷却截止时间等仍在独立运行时实例中，联网状态只由主机写入。保留的隐藏序列化字段用于兼容未迁移的历史验证场景；已迁移模块以 Definition 为权威来源，不能通过更改隐藏字段调参。

## 生成与构建

`CollaborationMigration.Migrate` 是一次性迁移工具：读取已验收地图、提取模块、保存所有原有组件引用并验证没有丢失。模块源目录存在后再次运行会拒绝覆盖。旧灰盒整图生成器也会拒绝重建，防止覆写美术和策划之后的编辑。

日常流程：

1. 阅读 `AGENTS.md`，检查 Git 状态与任务文件范围。
2. 编辑对应 Prefab 或配置，避免修改场景实例的公共属性覆盖。
3. 若新增环境场景或需要验证组合产物，运行 `Tools/Generate-ModuleScenes.ps1`。已有正确场景不重复保存，因此重复执行不产生序列化差异。
4. 使用 `Tools/Build-M4.ps1` 构建双模式 exe。构建只消费保存的源资源，不调用旧场景生成器、不保存源资产。
5. 运行相关测试，检查 `git diff`，确认没有其他模块的重序列化或意外属性覆盖。

旧 M0/M1 等历史检查点保留用于回归，不作为新内容的编辑入口。新增验证场景应消费独立模块，不复制并继续维护另一套正式地图。

## Git 初始化与合并

每位开发者运行一次 `Tools/Initialize-Collaboration.ps1`，安装仓库本地的 UnityYAMLMerge 驱动。`.gitattributes` 只指定驱动名称，本机 Git 配置不会随 clone 自动安装。保持 Force Text 和 Visible Meta Files；移动资产同时移动 `.meta`。

共享项目设置、包版本、输入定义和网络注册列表应由一个集成任务统一更新。二进制资源引入 LFS 前先建立仓库过滤配置；需要锁定的文件由团队明确选择，不给所有文本资产加锁。

合并时先解决配置、Prefab 和代码的语义差异，再验证组合场景连接。自动合并成功不代表业务正确；不得通过手工改 fileID 或忽略引用错误完成交付。

## 可重复验证入口

```powershell
.\Tools\Run-UnityTests.ps1 -Mode All
.\Tools\Test-ModuleBuild.ps1
.\Tools\Test-M4Build.ps1
.\Tools\Test-M4NetworkBuild.ps1
```

`Test-ModuleBuild` 对 Assets、Packages、ProjectSettings 建立文件字节哈希，连续验证两次场景生成及一次 Windows 构建没有写入源文件。`Test-M4Build` 使用真实图形播放器验证主机、工具模型、单人入口、设置及模式切换后的 Additive Scene 清理，并保存截图。`Test-M4NetworkBuild` 启动两个独立播放器，验证已有敌人和事故状态的晚加入同步、稳定内容 ID 及断线重连。

本地日志和截图写入被 Git 忽略的 Logs。两项播放器检查应顺序运行，避免端口和同机渲染资源干扰。这里的自动验证不代替多人实际体验验收。
