# 主要工具与设备建模交付

日期：2026-09-06。Unity：6000.0.82f1，URP。主题：低模科幻巨构维修。

## 交付内容

- 三种手持工具：冲击扳手、密封喷枪、线路桥接器。单人和合作模式共用网格及材质。
- 七类设备模型：冷却泵、控制台、风暴继电器、替换管件、压力表、工具停靠架、泵检修面板。
- 模型包含倒角、紧固件、散热结构、仪表、线圈和功能色标。深蓝灰工业外壳配黄色警示、青色密封/诊断、紫色电路标识。
- 10 个独立视觉 Prefab、OBJ 网格、共享色板材质以及可重复运行的 Blender 建模源脚本。
- 26 个既有 Prefab 完成视觉接入：2 个玩家工具、21 个交互设备、3 个环境美术 Prefab。

## 权威来源与边界

`Tools/Art/build_equipment.py` 是本套网格与色板的权威来源，输出限定到 `Assets/Game/Content/Modules/Art/Equipment`。不调用旧整图生成器。

`MaintenanceEquipmentArt.Import` 是显式 Unity 导入入口。首次创建视觉 Prefab 并接入既有资源；已接入的节点不重复创建、无变化不保存。修改网格后正常 Unity 导入即可刷新引用；手工编辑的视觉 Prefab 不由重复导入覆盖。

保留原有资源 GUID、组件、碰撞体、挂点、工具切换锚点及稳定内容 ID。原有装饰渲染组件保留但关闭，以保护外部引用。继电器原有状态 Renderer 继续驱动进度颜色，其 Mesh 换为独立状态指示条。控制台阶段灯与冷却泵状态核心保留。

未修改运行时玩法代码、网络 Prefab 注册、Packages、ProjectSettings、主组合场景或环境场景产物。环境美术改动仅关闭已被设备新模型替代的旧装饰。

## 网格预算

| 模型 | 三角面 |
| --- | ---: |
| ImpactWrench | 1,368 |
| SealantGun | 1,396 |
| CircuitBridger | 1,480 |
| CoolingPump | 3,136 |
| ControlConsole | 912 |
| StormRelay | 1,168 |
| ReplacementPipe | 1,112 |
| PressureGauge | 1,168 |
| ToolDock | 484 |
| InspectionPanel | 564 |

每个基础模型一个 Renderer、一个子网格、一个共享材质。工具架由停靠架和展示工具两个模型组成；动态状态灯独立保留。色板为 64×8，无大尺寸贴图，模型关闭 Read/Write。优化针对渲染组件和材质数量；未以帧率基准测试宣称性能提升。

## 实际验证

- EditMode：97/97 通过。
- PlayMode：44/44 通过。
- 模型脚本重复生成：所有设备资源字节哈希一致。
- Unity 设备导入重复运行：Assets、Packages、ProjectSettings 全部字节哈希一致。
- 源 Prefab 检查：模型存在、预算合规，无缺失脚本或破损引用；视觉 Prefab 无 Collider 或业务组件。
- `Test-ModuleBuild.ps1`：两次环境场景生成及 Windows 构建后 875 个源文件不变。
- `Test-M4Build.ps1`：真实图形播放器通过主机、三种工具、单人、设置、模式重载及 Additive Scene 清理检查；已查看工具及维修站截图。
- `Test-M4NetworkBuild.ps1`：两个本机播放器通过晚加入、稳定敌人 ID、事故状态同步及断线重连。

这些是自动化验证和截图检查，不代表完整三章人工通关、跨机器联机验收或多档显卡性能测试。

## 打开与维护

解压源项目包后，在 Unity Hub 添加项目文件夹，用 6000.0.82f1 打开。运行 `Assets/Game/Scenes/SinglePlayer_ThreeChapterDemo.unity`。

运行版需保留 exe 与同目录的 Data、UnityPlayer.dll 等文件，一起解压后启动 `FunGame-M4-Coop.exe`。

网格再生成与预览操作见 `Tools/Art/README.md`。全部内容为本次程序化建模，不引入第三方模型或纹理。
