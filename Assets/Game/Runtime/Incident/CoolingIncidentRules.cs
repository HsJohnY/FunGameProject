using System;

namespace FunGame.Incident
{
    /// <summary>
    /// 与 Unity 生命周期无关的固定事故规则，便于直接验证阶段转换和越序边界。
    /// </summary>
    public sealed class CoolingIncidentRules
    {
        private readonly bool _diagnosticChecksEnabled;

        public CoolingIncidentRules(bool diagnosticChecksEnabled = false)
        {
            _diagnosticChecksEnabled = diagnosticChecksEnabled;
            Phase = diagnosticChecksEnabled
                ? CoolingIncidentPhase.AssessSymptoms
                : CoolingIncidentPhase.ContainLeak;
        }

        public CoolingIncidentPhase Phase { get; private set; }
        public float SealProgress { get; private set; }
        public bool HasInspectedPressure { get; private set; }
        public bool HasInspectedPump { get; private set; }
        public int CircuitBridgeProgress { get; private set; }
        public const int RequiredCircuitBridgeSteps = 3;

        public string CurrentInstruction
        {
            get
            {
                switch (Phase)
                {
                    case CoolingIncidentPhase.AssessSymptoms:
                        if (!HasInspectedPressure && !HasInspectedPump) return "检查压力表与冷却泵，确认事故来源";
                        if (!HasInspectedPressure) return "读取压力表，补全诊断信息";
                        return "检查冷却泵外壳，补全诊断信息";
                    case CoolingIncidentPhase.RestoreControlPower: return "使用线路桥接器恢复冷却控制联锁";
                    case CoolingIncidentPhase.ContainLeak: return "使用密封喷枪持续密封泄漏点";
                    case CoolingIncidentPhase.LoosenConnection: return "使用冲击扳手松开损坏管件连接";
                    case CoolingIncidentPhase.InstallReplacementPipe: return "搬运替换管件并安装到接口";
                    case CoolingIncidentPhase.TightenConnection: return "使用冲击扳手紧固新管件";
                    case CoolingIncidentPhase.VerifyPressure: return "读取压力表，确认维修后的压力回升";
                    case CoolingIncidentPhase.ResetPump: return "前往冷却控制台执行复位";
                    default: return "冷却系统已恢复稳定";
                }
            }
        }

        /// <summary>
        /// 根据玩家在当前阶段停留的时间逐步增加提示精度，避免开场直接公布完整解法。
        /// </summary>
        public string GetGuidance(float phaseElapsedSeconds)
        {
            float elapsed = Math.Max(0f, phaseElapsedSeconds);
            switch (Phase)
            {
                case CoolingIncidentPhase.AssessSymptoms:
                    if (elapsed < 12f) return "冷却舱警报：压力持续下降 · 先观察设备异常";
                    if (elapsed < 30f) return "压力读数与泵体状态能共同说明故障来源";
                    return CurrentInstruction;
                case CoolingIncidentPhase.ContainLeak:
                    if (elapsed < 12f) return "已确认系统失压 · 沿管线寻找泄漏声与喷雾";
                    if (elapsed < 30f) return "泄漏点位于舱体侧面管段，并持续喷出蓝色介质";
                    return CurrentInstruction;
                case CoolingIncidentPhase.RestoreControlPower:
                    if (elapsed < 15f) return "诊断确认控制联锁断路 · 寻找紫色线路节点";
                    if (elapsed < 35f) return "线路桥接器需要依次检测端点、连接旁路并验证回路";
                    return CurrentInstruction;
                case CoolingIncidentPhase.LoosenConnection:
                    return elapsed < 18f ? "更换管件前需要先解除机械连接" : CurrentInstruction;
                case CoolingIncidentPhase.InstallReplacementPipe:
                    return elapsed < 18f ? "从零件架搬运橙色替换管件到拆开的接口" : CurrentInstruction;
                case CoolingIncidentPhase.TightenConnection:
                    return elapsed < 18f ? "新管件已经就位，但机械连接仍未锁定" : CurrentInstruction;
                case CoolingIncidentPhase.VerifyPressure:
                    return elapsed < 18f ? "先验证压力是否恢复，再尝试启动冷却泵" : CurrentInstruction;
                default:
                    return CurrentInstruction;
            }
        }

        public bool TryInspectPressure()
        {
            if (Phase == CoolingIncidentPhase.VerifyPressure)
            {
                Phase = CoolingIncidentPhase.ResetPump;
                return true;
            }

            if (Phase != CoolingIncidentPhase.AssessSymptoms || HasInspectedPressure)
            {
                return false;
            }

            HasInspectedPressure = true;
            TryCompleteAssessment();
            return true;
        }

        public bool TryInspectPump()
        {
            if (Phase != CoolingIncidentPhase.AssessSymptoms || HasInspectedPump)
            {
                return false;
            }

            HasInspectedPump = true;
            TryCompleteAssessment();
            return true;
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

        public bool TryAdvanceCircuitBridge()
        {
            if (Phase != CoolingIncidentPhase.RestoreControlPower || CircuitBridgeProgress >= RequiredCircuitBridgeSteps)
            {
                return false;
            }

            CircuitBridgeProgress++;
            if (CircuitBridgeProgress >= RequiredCircuitBridgeSteps)
            {
                Phase = CoolingIncidentPhase.ContainLeak;
            }

            return true;
        }

        public bool TryLoosen() => TryAdvance(CoolingIncidentPhase.LoosenConnection, CoolingIncidentPhase.InstallReplacementPipe);
        public bool TryInstallPipe() => TryAdvance(CoolingIncidentPhase.InstallReplacementPipe, CoolingIncidentPhase.TightenConnection);
        public bool TryTighten() => TryAdvance(
            CoolingIncidentPhase.TightenConnection,
            _diagnosticChecksEnabled ? CoolingIncidentPhase.VerifyPressure : CoolingIncidentPhase.ResetPump);
        public bool TryResetPump() => TryAdvance(CoolingIncidentPhase.ResetPump, CoolingIncidentPhase.Stabilized);

        public void Reset()
        {
            Phase = _diagnosticChecksEnabled
                ? CoolingIncidentPhase.AssessSymptoms
                : CoolingIncidentPhase.ContainLeak;
            SealProgress = 0f;
            HasInspectedPressure = false;
            HasInspectedPump = false;
            CircuitBridgeProgress = 0;
        }

        private void TryCompleteAssessment()
        {
            if (HasInspectedPressure && HasInspectedPump)
            {
                Phase = _diagnosticChecksEnabled
                    ? CoolingIncidentPhase.RestoreControlPower
                    : CoolingIncidentPhase.ContainLeak;
            }
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
