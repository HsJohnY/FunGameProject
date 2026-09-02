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

## KI-002：Codex 启动环境缺少 `ALLUSERSPROFILE` 导致 Package Manager 无法初始化

- 状态：已定位并规避；不再阻塞 Unity 导入、测试或构建
- 发现版本：Unity `6000.0.82f1`
- 首次发现：2026-09-02，基础战斗切片开发

### 表现

编辑器在 `Application.AssetDatabase Initial Refresh Start` 后立即退出：

```text
[Package Manager] The "path" argument must be of type string. Received undefined
[Package Manager] Failed to update project manifest: The "path" argument must be of type string. Received undefined
```

### 根因与本项目证据

- 当前系统已不存在需求基线使用的 `6000.0.38f1`，只安装了 `D:\Unity\Hub\Editor\6000.0.82f1`；
- 项目 `Packages/manifest.json` 是有效 JSON，且没有本地路径依赖；
- 使用同一编辑器创建全新临时项目时出现完全相同错误，因此不是本仓库内容、新战斗代码或包锁造成；
- 直接运行 Unity Package Manager 并查看调用栈后，确认其读取 `process.env.ALLUSERSPROFILE` 并将缺失值传给 `path.join`；
- Codex 启动的进程环境缺少标准 Windows 变量 `ALLUSERSPROFILE`，而系统约定值为 `C:\ProgramData`；
- 仅为 Unity 子进程设置该变量后，包解析、项目导入、场景生成、EditMode、PlayMode 和 Windows 构建均成功。

### 影响边界

- 从 Unity Hub 或拥有完整标准环境的终端启动时通常不受影响；
- 从缺少该变量的自动化进程直接启动 Unity 时仍会复现；
- 不应通过修改包清单或重装编辑器来掩盖该环境缺失。

### 自动化规避

启动 Unity 或 `UnityPackageManager.exe` 前，只在当前 PowerShell/子进程中设置：

```powershell
$env:ALLUSERSPROFILE = 'C:\ProgramData'
```

不要为此修改仓库文件或覆盖机器全局环境。Unity 版本升级及回归结果记录在 ADR-0003。
