namespace FunGame.Tools
{
    /// <summary>
    /// 由能接受工具主要功能的场景对象实现。
    /// </summary>
    public interface IToolTarget
    {
        ToolActionOption GetToolAction(PlayerToolbelt toolbelt);
        bool ApplyTool(PlayerToolbelt toolbelt);
    }
}
