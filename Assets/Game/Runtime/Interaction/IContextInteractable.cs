namespace FunGame.Interaction
{
    /// <summary>
    /// 由可被准星选中的场景对象实现，负责描述并执行自己的上下文动作。
    /// </summary>
    public interface IContextInteractable
    {
        InteractionOption GetInteractionOption(ContextInteractor actor);
        bool ExecuteInteraction(ContextInteractor actor);
    }
}
