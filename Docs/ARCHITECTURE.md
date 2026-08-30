# 架构基线

当前只确立原则，不在玩法未知时预建复杂框架。

## 原则

- 玩法逻辑尽量使用普通 C# 类型，MonoBehaviour 负责 Unity 生命周期和场景适配。
- 配置与运行时状态分离；静态配置可使用 ScriptableObject，存档模型保持可序列化且可迁移。
- 模块通过明确接口或事件协作，避免全局可变单例成为默认方案。
- 第三方资源放入独立目录，通过适配层接入，便于升级或替换。
- 先为核心循环建立可测试边界，再扩展内容生产工具。

## 预定目录（按需要创建）

```text
Assets/
  Game/
    Runtime/
    Editor/
    Content/
    Scenes/
  Tests/
    EditMode/
    PlayMode/
  ThirdParty/
```

## 尚待玩法决定

- 2D 或 3D，以及 Built-in / URP 渲染管线
- 场景组织和加载策略
- 输入设备与重绑定需求
- 存档、关卡、AI、音频等模块是否必要

