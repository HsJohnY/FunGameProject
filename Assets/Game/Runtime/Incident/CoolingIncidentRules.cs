using System;

namespace FunGame.Incident
{
    /// <summary>
    /// 与 Unity 生命周期无关的固定事故规则，便于直接验证阶段转换和越序边界。
    /// </summary>
    public sealed class CoolingIncidentRules
    {
        public CoolingIncidentPhase Phase { get; private set; } = CoolingIncidentPhase.ContainLeak;
        public float SealProgress { get; private set; }

        public string CurrentInstruction
        {
            get
            {
                switch (Phase)
                {
                    case CoolingIncidentPhase.ContainLeak: return "使用密封喷枪持续密封泄漏点";
                    case CoolingIncidentPhase.LoosenConnection: return "使用冲击扳手松开损坏管件连接";
                    case CoolingIncidentPhase.InstallReplacementPipe: return "搬运替换管件并安装到接口";
                    case CoolingIncidentPhase.TightenConnection: return "使用冲击扳手紧固新管件";
                    case CoolingIncidentPhase.ResetPump: return "前往冷却控制台执行复位";
                    default: return "冷却系统已恢复稳定";
                }
            }
        }

        public bool AddSealProgress(float amount)
        {
            if (Phase != CoolingIncidentPhase.ContainLeak || amount <= 0f)
            {
                return false;
            }

            SealProgress = Math.Min(1f, SealProgress + amount);
            if (SealProgress >= 1f)
            {
                Phase = CoolingIncidentPhase.LoosenConnection;
            }

            return true;
        }

        public bool TryLoosen() => TryAdvance(CoolingIncidentPhase.LoosenConnection, CoolingIncidentPhase.InstallReplacementPipe);
        public bool TryInstallPipe() => TryAdvance(CoolingIncidentPhase.InstallReplacementPipe, CoolingIncidentPhase.TightenConnection);
        public bool TryTighten() => TryAdvance(CoolingIncidentPhase.TightenConnection, CoolingIncidentPhase.ResetPump);
        public bool TryResetPump() => TryAdvance(CoolingIncidentPhase.ResetPump, CoolingIncidentPhase.Stabilized);

        public void Reset()
        {
            Phase = CoolingIncidentPhase.ContainLeak;
            SealProgress = 0f;
        }

        private bool TryAdvance(CoolingIncidentPhase expected, CoolingIncidentPhase next)
        {
            if (Phase != expected)
            {
                return false;
            }

            Phase = next;
            return true;
        }
    }
}
