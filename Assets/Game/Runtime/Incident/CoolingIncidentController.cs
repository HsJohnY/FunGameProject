using System;
using UnityEngine;

namespace FunGame.Incident
{
    /// <summary>
    /// 将纯事故规则适配到场景，并向目标与临时 UI 发布阶段变化。
    /// </summary>
    public sealed class CoolingIncidentController : MonoBehaviour
    {
        private CoolingIncidentRules _rules;

        public event Action StateChanged;
        public CoolingIncidentPhase Phase => Rules.Phase;
        public float SealProgress => Rules.SealProgress;
        public string CurrentInstruction => Rules.CurrentInstruction;
        private CoolingIncidentRules Rules => _rules ?? (_rules = new CoolingIncidentRules());

        public bool AddSealProgress(float amount) => Execute("seal", () => Rules.AddSealProgress(amount));
        public bool TryLoosen() => Execute("loosen", Rules.TryLoosen);
        public bool TryInstallPipe() => Execute("install-pipe", Rules.TryInstallPipe);
        public bool TryTighten() => Execute("tighten", Rules.TryTighten);
        public bool TryResetPump() => Execute("reset-pump", Rules.TryResetPump);

        private bool Execute(string action, Func<bool> transition)
        {
            CoolingIncidentPhase before = Rules.Phase;
            bool accepted = transition();
            if (accepted)
            {
                StateChanged?.Invoke();
            }

            if (before != Rules.Phase)
            {
                Debug.Log($"[Incident] action={action} from={before} to={Rules.Phase}", this);
            }

            return accepted;
        }
    }
}
