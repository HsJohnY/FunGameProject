# Unity 6000.0.82f1 迁移评审

状态：自动门禁通过，等待人工复测

## 迁移范围

- 编辑器：`6000.0.38f1` → `6000.0.82f1`；
- URP：`17.0.3` → `17.0.4`；
- Input System：`1.13.0` → `1.19.0`；
- Multiplayer Play Mode：`1.3.3` → `1.6.3`；
- Unity Transport：`2.6.0` → `2.7.4`（NGO 的间接依赖）；
- Netcode for GameObjects：保持 `2.7.0`。

以上包版本由新编辑器首次解析项目时更新，没有额外引入新系统。

## 自动验证

- 首次导入：通过，无脚本编译或包解析错误；
- EditMode：25/25 通过；
- PlayMode：16/16 通过；
- Windows Development Build：通过；
- 构建启动：播放器使用 `6000.0.82f1 (2fb0dae735e1)`，隐藏运行 6 秒未提前退出；
- 场景和 Prefab：没有自动批量重写。

第一次 Windows Build 的 Burst AOT 编译器发生一次 `AccessViolationException`；所有相关进程退出后，在全新输出目录原样重试成功，暂未能稳定复现，因此不修改 Burst 配置或降级包。若后续再次出现，应重新评估为稳定迁移缺陷。

## 人工复测

### M1 灰盒

- [ ] 打开 `Assets/Game/Scenes/M1_CoolingBay.unity`，画面、材质和 UI 无明显异常；
- [ ] 第一人称移动、观察、交互、工具、搬运和抛物正常；
- [ ] 完成一次事故链，并确认成功、失败与重置正常；
- [ ] Console 没有进入 `Assets/Game` 调用栈的红色错误；
- [ ] 旧 TextCore `DontSaveInEditor` 断言没有再次出现。

### M3 网络

- [ ] 打开 `Assets/Game/Scenes/M3_NetworkSlice.unity`；
- [ ] Multiplayer Play Mode 能正常启动第二实例；
- [ ] 主机和客户端通过 `127.0.0.1:7777` 连接；
- [ ] 客户端断开、重新连接和主机停止正常；
- [ ] 不可达地址失败后恢复输入；
- [ ] 两个主机占用相同端口时，第二个实例显示普通占用提示且 Console 无绑定红色错误。

## 回滚点

迁移前标签：`m3-1-network-session-v1.0.0`。旧编辑器 `6000.0.38f1` 在人工迁移验收前保留，不应卸载。
