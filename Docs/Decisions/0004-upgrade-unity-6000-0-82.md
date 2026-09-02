# ADR-0004：升级到 Unity 6000.0.82f1

- 状态：已接受
- 日期：2026-09-02
- 取代：ADR-0002 中的具体 Unity 与包版本；其渲染、输入和测试分层决策继续有效

## 背景

项目原基线为 Unity `6000.0.38f1`，但当前协作环境仅安装 Unity `6000.0.82f1`。为避免团队在不同补丁版本间反复迁移场景、材质和项目设置，项目推动者决定统一使用已安装版本。

初次命令行导入时，Package Manager 因 Codex 子进程缺少标准 Windows 环境变量 `ALLUSERSPROFILE` 而退出。该问题已定位为启动环境缺失，不是项目清单、包锁或编辑器安装损坏；只为 Unity 子进程设置 `C:\ProgramData` 后可稳定解析依赖。

## 决策

1. 项目编辑器固定为 Unity `6000.0.82f1`（revision `2fb0dae735e1`）。
2. 依赖固定为 URP `17.0.4`、Input System `1.19.0`、Test Framework `1.6.0` 和 UGUI `2.0.0`。
3. 接受 Unity/URP 对材质版本、URP Global Settings 与 Shader Graph Settings 的确定性迁移，并将这些迁移与新版本清单一同纳入版本控制。
4. 自动化启动 Unity 时显式定位编辑器；若父进程缺少 `ALLUSERSPROFILE`，只在 Unity 子进程环境中设置为 `C:\ProgramData`，不修改机器全局环境。
5. 升级必须通过全量 EditMode、PlayMode 和至少一个 Windows Development Build 玩家程序冒烟门禁。

## 验证

- 项目导入与 `Combat_DefenseSandbox` 场景生成成功；
- EditMode 23/23 通过；
- PlayMode 19/19 通过；
- 战斗 Windows Development Build 成功，真实播放器进入检查点并以退出码 0 正常退出。

## 后果

- 所有协作者应使用 Unity `6000.0.82f1`，避免较旧编辑器反向改写场景、材质和项目设置。
- URP 资产和既有 M1 材质会包含一次版本迁移，这是工具链升级的一部分，不是战斗功能对既有场景的内容修改。
- `Builds/`、`Temp/` 和测试结果保持为本地验证产物，不进入 Git。
- 若后续再次更换 Unity 或渲染包版本，必须新增 ADR 并重新执行同等级回归。
