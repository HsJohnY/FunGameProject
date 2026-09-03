# Fun Game Project

这是“小型多人合作巨构维修远征”练手项目的独立 Unity 工作空间。目前需求基线、单机核心灰盒、首次玩法评审和 M3-1 网络会话已完成，正在评审一个约半小时的三章单人 Demo：玩家依次恢复冷却、配电和风暴核心三个连续舱室，已恢复的舰船能力会持续在线。游戏内包含动态任务句、稳定的屏幕边缘目标标记、关键交互物低模轮廓和按章节开启的舱门，目标是让首次玩家不读攻略也能完整通关。

## 从哪里开始

1. 查看 [Docs/PROJECT_STATUS.md](Docs/PROJECT_STATUS.md)，了解当前阶段、正在做什么和下一步。
2. 查看 [Docs/MVP_DEVELOPMENT_PLAN.md](Docs/MVP_DEVELOPMENT_PLAN.md)，了解当前小阶段、运行门禁和人工审查方式。
3. 查看 [Docs/Requirements/GRAYBOX_SCOPE.md](Docs/Requirements/GRAYBOX_SCOPE.md)，确认首个 8–12 分钟灰盒的冻结范围。
4. 查看 [Docs/Reviews/SINGLEPLAYER_THREE_CHAPTER_DEMO.md](Docs/Reviews/SINGLEPLAYER_THREE_CHAPTER_DEMO.md)，运行并评审当前三章单人候选。

## 目录约定

- `Assets/Game/`：游戏源代码与资源；第三方资源不得混入其中。
- `Assets/Tests/`：EditMode 与 PlayMode 自动化测试。
- `Docs/`：需求、架构、计划、决策、测试与状态。
- `Packages/`、`ProjectSettings/`：Unity 工程配置。
- `Tools/`：备份、验证和维护脚本。

## 环境基线

- Unity：`6000.0.82f1`（Unity 6.0 LTS）
- 渲染：URP `17.0.3`，Linear 色彩空间
- 输入：Input System `1.13.0`
- 版本管理：Git
- 默认分支：`main`
- 大型二进制资源：正式引入前评估 Git LFS；当前不提前启用

## 本地验证入口

在 PowerShell 中从仓库根目录运行：

```powershell
.\Tools\Initialize-M0.ps1
.\Tools\Run-UnityTests.ps1 -Mode All
.\Tools\Build-M0.ps1
.\Tools\Test-M0Build.ps1
.\Tools\Build-SinglePlayerDemo.ps1
.\Tools\Test-SinglePlayerDemoBuild.ps1
.\Tools\Run-SinglePlayerDemoPlaytest.ps1
```

脚本默认使用本机已确认的 Unity 路径，也接受 `-UnityEditorPath` 覆盖。`Run-SinglePlayerDemoPlaytest.ps1` 会打开真实玩家窗口，并在退出后根据结算日志判定是否完整通关且处于 25–35 分钟目标区间。生成包、日志和测试结果均被 Git 忽略。

## 工作流

每个可交付变化使用独立分支：`feature/<主题>`、`fix/<问题>`、`docs/<主题>`。提交前更新测试或验收记录；里程碑使用标签，例如 `prototype-v0.1.0`。只有可恢复的提交才视为完成。
