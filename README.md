# Fun Game Project

这是我们的小型趣味游戏独立工作空间。目前处于 **阶段 0：项目发现与立项**，尚未确定玩法和技术方向。

## 从哪里开始

1. 查看 [Docs/PROJECT_STATUS.md](Docs/PROJECT_STATUS.md)，了解当前阶段、正在做什么和下一步。
2. 在 [Docs/Requirements/GAME_BRIEF.md](Docs/Requirements/GAME_BRIEF.md) 中形成一句话玩法与范围。
3. 需求通过评审后，再进入原型开发；不会在需求未明确时搭建过度复杂的业务代码。

## 目录约定

- `Assets/Game/`：游戏源代码与资源；第三方资源不得混入其中。
- `Assets/Tests/`：EditMode 与 PlayMode 自动化测试。
- `Docs/`：需求、架构、计划、决策、测试与状态。
- `Packages/`、`ProjectSettings/`：Unity 工程配置。
- `Tools/`：备份、验证和维护脚本。

## 环境基线

- Unity：`6000.0.38f1`（与本机已有项目一致）
- 版本管理：Git
- 默认分支：`main`
- 大型二进制资源：正式引入前评估 Git LFS；当前不提前启用

## 工作流

每个可交付变化使用独立分支：`feature/<主题>`、`fix/<问题>`、`docs/<主题>`。提交前更新测试或验收记录；里程碑使用标签，例如 `prototype-v0.1.0`。只有可恢复的提交才视为完成。

