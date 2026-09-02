# 已知问题

## KI-001：Unity 6 非英文编辑器界面的 TextCore 断言

- 状态：上游编辑器问题；已确认规避方式
- 发现版本：Unity `6000.0.38f1`
- 首次发现：2026-09-01，M1-1 人工评审

### 表现

Unity Console 偶发出现：

```text
Assertion failed on expression: '!(o->TestHideFlag(Object::kDontSaveInEditor) && (options & kAllowDontSaveObjectsToBePersistent) == 0)'
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)
```

### 本项目证据

`Editor.log` 调用栈为 `TextEditorResourceManager.AddTextureToAsset` → `FontAsset.SetupNewAtlasTexture` → `IMGUITextHandle` → `MeshRendererEditor.OnInspectorGUI`。调用栈不经过 `Assets/Game` 的运行时代码，发生在 Inspector 为缺失字符扩充临时字体图集时。

### 影响

- 只影响 Unity 编辑器界面；当前未观察到玩法、场景数据、自动测试或玩家程序异常；
- 同一编辑器会话中通常只出现一次，但仍可能污染 Console 的“零错误”检查；
- 不能因此忽略其他调用栈不同的 Assertion。

### 临时规避

需要干净 Console 时，可将 Unity Editor Language 临时切换为 English 并重启编辑器。项目代码注释和玩家文本仍可使用中文。

### 后续处理

不为此问题删除中文注释，也不单独升级到实验版 Unity。未来评估稳定 Unity 升级时复测；若调用栈进入项目代码或在玩家构建中出现，则重新按阻塞缺陷处理。

## KI-002：Unity 6000.0.82f1 Package Manager 无法初始化项目

- 状态：本机编辑器安装或环境问题；阻塞 Unity 导入与自动测试
- 发现版本：Unity `6000.0.82f1`
- 首次发现：2026-09-02，基础战斗切片开发

### 表现

编辑器在 `Application.AssetDatabase Initial Refresh Start` 后立即退出：

```text
[Package Manager] The "path" argument must be of type string. Received undefined
[Package Manager] Failed to update project manifest: The "path" argument must be of type string. Received undefined
```

### 本项目证据

- 当前系统已不存在需求基线使用的 `6000.0.38f1`，只安装了 `D:\Unity\Hub\Editor\6000.0.82f1`；
- 项目 `Packages/manifest.json` 是有效 JSON，且没有本地路径依赖；
- 临时更新到该编辑器清单要求的包版本后错误不变，已恢复原版本声明；
- 使用同一编辑器创建全新临时项目时出现完全相同错误，因此不是本仓库内容、新战斗代码或包锁造成；
- Unity 尚未进入项目脚本编译阶段。

### 影响

- 无法生成或保存 `Combat_DefenseSandbox` 场景资产；
- 无法运行 Unity EditMode、PlayMode 或开发构建验证；
- 纯 C# 规则和新增运行时代码已通过独立编译冒烟，但不能替代 Unity 验证。

### 恢复建议

优先在 Unity Hub 中修复或重新安装项目原版本 `6000.0.38f1`；若决定统一升级到 `6000.0.82f1`，应先修复该编辑器安装并在独立提交中完成包版本、项目版本和全量回归升级，不与战斗功能提交混合。
