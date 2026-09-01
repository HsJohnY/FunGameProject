# Tests

自动化测试按 `EditMode` 和 `PlayMode` 分开：

- `EditMode/`：普通 C# 规则、状态转换和无需运行场景的快速测试；
- `PlayMode/`：Unity 生命周期、场景、物理和输入适配测试。

两组测试使用独立程序集，不进入玩家构建。统一入口是 `Tools/Run-UnityTests.ps1`。
