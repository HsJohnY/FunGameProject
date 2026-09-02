using FunGame.Interaction;
using UnityEngine;

namespace FunGame.Combat
{
    /// <summary>
    /// 在一次防卫成功或失败后，用统一上下文键重新开始独立战斗灰盒。
    /// </summary>
    public sealed class CombatResetConsoleInteractable : MonoBehaviour, IContextInteractable
    {
        [SerializeField] private CombatEncounterController encounter;

        public void Configure(CombatEncounterController configuredEncounter)
        {
            encounter = configuredEncounter;
        }

        public InteractionOption GetInteractionOption(ContextInteractor actor)
        {
            bool canReset = encounter != null && encounter.State != CombatEncounterState.Active;
            return new InteractionOption(
                "combat-training-console",
                "防卫训练控制台",
                "重新开始防卫",
                InteractionPriority.Device,
                canReset,
                "防卫仍在进行");
        }

        public bool ExecuteInteraction(ContextInteractor actor)
        {
            return encounter != null && encounter.ResetEncounter();
        }
    }
}
