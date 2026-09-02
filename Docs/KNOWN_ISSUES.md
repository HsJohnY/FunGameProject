# 已知问题

## KI-001：Unity 6 非英文编辑器界面的 TextCore 断言

- 状态：上游编辑器问题；已确认规避方式
- 发现版本：Unity `6000.0.38f1`
- 迁移复查：Unity `6000.0.82f1` 的批处理导入、自动测试和 Windows 构建中未再次出现；仍需编辑器人工操作确认。
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
