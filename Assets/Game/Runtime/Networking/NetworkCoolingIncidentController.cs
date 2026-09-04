using FunGame.Incident;
using FunGame.Tools;
using Unity.Netcode;
using UnityEngine;

namespace FunGame.Networking
{
    /// <summary>
    /// M3-4 的服务器权威事故状态。客户端只能读取同步结果，所有阶段转换均在服务器验证。
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkCoolingIncidentController : NetworkBehaviour
    {
        [SerializeField] private float startingTemperature = 65f;
        [SerializeField] private float failureTemperature = 100f;
        [SerializeField] private float temperatureRisePerSecond = 0.07f;
        [SerializeField] private bool diagnosticChecksEnabled;

        private readonly NetworkVariable<CoolingIncidentPhase> phase = new NetworkVariable<CoolingIncidentPhase>();
        private readonly NetworkVariable<float> sealProgress = new NetworkVariable<float>();
        private readonly NetworkVariable<float> temperature = new NetworkVariable<float>();
        private readonly NetworkVariable<CoolingIncidentRunState> runState = new NetworkVariable<CoolingIncidentRunState>();
        private readonly NetworkVariable<bool> pressureInspected = new NetworkVariable<bool>();
        private readonly NetworkVariable<bool> pumpInspected = new NetworkVariable<bool>();
        private readonly NetworkVariable<int> circuitBridgeProgress = new NetworkVariable<int>();

        private CoolingIncidentRules _rules;

        public CoolingIncidentPhase Phase => phase.Value;
        public float SealProgress => sealProgress.Value;
        public float Temperature => temperature.Value;
        public float FailureTemperature => failureTemperature;
        public CoolingIncidentRunState RunState => runState.Value;
        public bool HasInspectedPressure => pressureInspected.Value;
        public bool HasInspectedPump => pumpInspected.Value;
        public int CircuitBridgeProgress => circuitBridgeProgress.Value;
        public bool DiagnosticChecksEnabled => diagnosticChecksEnabled;
        public string CurrentInstruction => GetInstruction(Phase);

        public void ConfigureExtendedIncident(bool enabled)
        {
            diagnosticChecksEnabled = enabled;
            _rules = new CoolingIncidentRules(enabled);
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                ResetIncidentServer();
            }
        }

        private void Update()
        {
            if (!IsServer || runState.Value != CoolingIncidentRunState.Active)
            {
                return;
            }

            temperature.Value += temperatureRisePerSecond * Time.deltaTime;
            if (temperature.Value >= failureTemperature)
            {
                runState.Value = CoolingIncidentRunState.Failed;
            }
        }

        public bool IsActionAvailable(NetworkIncidentAction action, ToolKind equippedTool, bool hasReplacementPipe = false)
        {
            if (runState.Value != CoolingIncidentRunState.Active)
            {
                return action == NetworkIncidentAction.OperatePump;
            }

            switch (action)
            {
                case NetworkIncidentAction.InspectPressure:
                    return phase.Value == CoolingIncidentPhase.AssessSymptoms
                           || phase.Value == CoolingIncidentPhase.VerifyPressure;
                case NetworkIncidentAction.InspectPump:
                    return phase.Value == CoolingIncidentPhase.AssessSymptoms;
                case NetworkIncidentAction.BridgeCircuit:
                    return phase.Value == CoolingIncidentPhase.RestoreControlPower
                           && equippedTool == ToolKind.CircuitBridger;
                case NetworkIncidentAction.SealLeak:
                    return phase.Value == CoolingIncidentPhase.ContainLeak && equippedTool == ToolKind.SealantGun;
                case NetworkIncidentAction.OperateFastener:
                    return (phase.Value == CoolingIncidentPhase.LoosenConnection
                        || phase.Value == CoolingIncidentPhase.TightenConnection)
                        && equippedTool == ToolKind.ImpactWrench;
                case NetworkIncidentAction.InstallPipe:
                    return phase.Value == CoolingIncidentPhase.InstallReplacementPipe && hasReplacementPipe;
                case NetworkIncidentAction.OperatePump:
                    return phase.Value == CoolingIncidentPhase.ResetPump;
                default:
                    return false;
            }
        }

        public bool TryExecuteServer(NetworkIncidentAction action, ToolKind equippedTool, bool hasReplacementPipe = false)
        {
            if (!IsServer)
            {
                return false;
            }

            if (runState.Value != CoolingIncidentRunState.Active)
            {
                if (action != NetworkIncidentAction.OperatePump)
                {
                    return false;
                }

                ResetIncidentServer();
                return true;
            }

            if (!IsActionAvailable(action, equippedTool, hasReplacementPipe))
            {
                return false;
            }

            bool accepted;
            switch (action)
            {
                case NetworkIncidentAction.InspectPressure:
                    accepted = Rules.TryInspectPressure();
                    break;
                case NetworkIncidentAction.InspectPump:
                    accepted = Rules.TryInspectPump();
                    break;
                case NetworkIncidentAction.BridgeCircuit:
                    accepted = Rules.TryAdvanceCircuitBridge();
                    break;
                case NetworkIncidentAction.SealLeak:
                    accepted = Rules.AddSealProgress(0.25f);
                    break;
                case NetworkIncidentAction.OperateFastener:
                    accepted = phase.Value == CoolingIncidentPhase.LoosenConnection
                        ? Rules.TryLoosen()
                        : Rules.TryTighten();
                    break;
                case NetworkIncidentAction.InstallPipe:
                    accepted = Rules.TryInstallPipe();
                    break;
                case NetworkIncidentAction.OperatePump:
                    accepted = Rules.TryResetPump();
                    break;
                default:
                    return false;
            }

            if (!accepted)
            {
                return false;
            }

            SynchronizeRules();
            if (phase.Value == CoolingIncidentPhase.Stabilized)
            {
                runState.Value = CoolingIncidentRunState.Succeeded;
            }

            return true;
        }

        public static ToolKind GetRequiredTool(NetworkIncidentAction action, CoolingIncidentPhase currentPhase)
        {
            if (action == NetworkIncidentAction.SealLeak)
            {
                return ToolKind.SealantGun;
            }

            if (action == NetworkIncidentAction.BridgeCircuit)
            {
                return ToolKind.CircuitBridger;
            }

            if (action == NetworkIncidentAction.OperateFastener
                && (currentPhase == CoolingIncidentPhase.LoosenConnection
                    || currentPhase == CoolingIncidentPhase.TightenConnection))
            {
                return ToolKind.ImpactWrench;
            }

            return ToolKind.None;
        }

        public static bool RequiresReplacementPipe(NetworkIncidentAction action)
        {
            return action == NetworkIncidentAction.InstallPipe;
        }

        private CoolingIncidentRules Rules => _rules ??= new CoolingIncidentRules(diagnosticChecksEnabled);

        public void BeginNextBranchServer()
        {
            if (IsServer && runState.Value == CoolingIncidentRunState.Succeeded) ResetIncidentServer();
        }

        public void ApplyTemperatureSpikeServer(float amount)
        {
            if (!IsServer || runState.Value != CoolingIncidentRunState.Active) return;
            temperature.Value += Mathf.Max(0f, amount);
            if (temperature.Value >= failureTemperature) runState.Value = CoolingIncidentRunState.Failed;
        }

        private void ResetIncidentServer()
        {
            if (IsSpawned)
            {
                foreach (NetworkCarryableItem item in FindObjectsByType<NetworkCarryableItem>(FindObjectsSortMode.None))
                {
                    item.ResetToSpawnServer();
                }
            }

            Rules.Reset();
            phase.Value = Rules.Phase;
            sealProgress.Value = Rules.SealProgress;
            temperature.Value = startingTemperature;
            runState.Value = CoolingIncidentRunState.Active;
            pressureInspected.Value = Rules.HasInspectedPressure;
            pumpInspected.Value = Rules.HasInspectedPump;
            circuitBridgeProgress.Value = Rules.CircuitBridgeProgress;
        }

        private void SynchronizeRules()
        {
            phase.Value = Rules.Phase;
            sealProgress.Value = Rules.SealProgress;
            pressureInspected.Value = Rules.HasInspectedPressure;
            pumpInspected.Value = Rules.HasInspectedPump;
            circuitBridgeProgress.Value = Rules.CircuitBridgeProgress;
        }

        private static string GetInstruction(CoolingIncidentPhase currentPhase)
        {
            return currentPhase switch
            {
                CoolingIncidentPhase.AssessSymptoms => "分别检查压力表和冷却泵外壳",
                CoolingIncidentPhase.RestoreControlPower => "使用线路桥接器恢复控制联锁",
                CoolingIncidentPhase.ContainLeak => "使用密封喷枪密封泄漏点",
                CoolingIncidentPhase.LoosenConnection => "使用冲击扳手松开管件连接",
                CoolingIncidentPhase.InstallReplacementPipe => "搬运共享替换管件并安装到接口",
                CoolingIncidentPhase.TightenConnection => "使用冲击扳手紧固新管件",
                CoolingIncidentPhase.VerifyPressure => "读取压力表确认压力恢复",
                CoolingIncidentPhase.ResetPump => "前往控制台执行泵复位",
                _ => "冷却系统已恢复稳定"
            };
        }
    }
}
