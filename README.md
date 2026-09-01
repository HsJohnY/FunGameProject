# Fun Game Project

这是“小型多人合作巨构维修远征”练手项目的独立 Unity 工作空间。目前需求基线 `requirements-v1.0.0` 已完成，进入 **阶段 1：技术准备与单机核心灰盒**。

## 从哪里开始

1. 查看 [Docs/PROJECT_STATUS.md](Docs/PROJECT_STATUS.md)，了解当前阶段、正在做什么和下一步。
2. 查看 [Docs/MVP_DEVELOPMENT_PLAN.md](Docs/MVP_DEVELOPMENT_PLAN.md)，了解当前小阶段、运行门禁和人工审查方式。
3. 查看 [Docs/Requirements/GRAYBOX_SCOPE.md](Docs/Requirements/GRAYBOX_SCOPE.md)，确认首个 8–12 分钟灰盒的冻结范围。

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
