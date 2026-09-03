using System;
using System.Collections.Generic;
using UnityEngine;

namespace FunGame.Incident
{
    /// <summary>
    /// 将纯事故规则适配到场景，并向目标与临时 UI 发布阶段变化。
    /// </summary>
    public sealed class CoolingIncidentController : MonoBehaviour
    {
        private CoolingIncidentRules _rules;
        private readonly List<IIncidentResettable> _resettableObjects = new List<IIncidentResettable>();
        [SerializeField, Min(0f)] private float startingTemperature = 65f;
        [SerializeField, Min(1f)] private float failureTemperature = 100f;
        [SerializeField, Min(0f)] private float temperatureRisePerSecond = 0.07f;
        [SerializeField] private bool diagnosticChecksEnabled;
        private float _temperature;
        private float _elapsedSeconds;
        private float _phaseElapsedSeconds;
        private float _lastRunDurationSeconds;
        private CoolingIncidentRunState _runState = CoolingIncidentRunState.Active;

        public event Action StateChanged;
        public event Action RunStateChanged;
        public CoolingIncidentPhase Phase => Rules.Phase;
        public float SealProgress => Rules.SealProgress;
        public string CurrentInstruction => Rules.CurrentInstruction;
        public string CurrentGuidance => Rules.GetGuidance(_phaseElapsedSeconds);
        public bool HasInspectedPressure => Rules.HasInspectedPressure;
        public bool HasInspectedPump => Rules.HasInspectedPump;
        public int CircuitBridgeProgress => Rules.CircuitBridgeProgress;
        public CoolingIncidentRunState RunState => _runState;
        public float Temperature => _temperature;
        public float FailureTemperature => failureTemperature;
        public float ElapsedSeconds => _elapsedSeconds;
        public float PhaseElapsedSeconds => _phaseElapsedSeconds;
        public float LastRunDurationSeconds => _lastRunDurationSeconds;
        public int ResetCount { get; private set; }
        public int RejectedActionCount { get; private set; }
        public int TemperatureSpikeCount { get; private set; }
        private CoolingIncidentRules Rules => _rules ?? (_rules = new CoolingIncidentRules(diagnosticChecksEnabled));

        private void Awake()
        {
            _temperature = startingTemperature;
        }

        private void Update()
        {
            if (_runState != CoolingIncidentRunState.Active)
            {
                return;
            }

            _elapsedSeconds += Time.deltaTime;
            _phaseElapsedSeconds += Time.deltaTime;
            _temperature += temperatureRisePerSecond * Time.deltaTime;
            if (_temperature < failureTemperature)
            {
                return;
            }

            FailIncident("ambient-temperature");
        }

        public void ConfigureTemperature(float starting, float failure, float risePerSecond)
        {
            startingTemperature = starting;
            failureTemperature = failure;
            temperatureRisePerSecond = risePerSecond;
            _temperature = starting;
        }

        public void ConfigureExtendedIncident(bool enabled)
        {
            diagnosticChecksEnabled = enabled;
            _rules = new CoolingIncidentRules(enabled);
            _phaseElapsedSeconds = 0f;
        }

        public void RegisterResettable(IIncidentResettable resettable)
        {
            if (resettable != null && !_resettableObjects.Contains(resettable))
            {
                _resettableObjects.Add(resettable);
            }
        }

        public bool ResetIncident()
        {
            Rules.Reset();
            _temperature = startingTemperature;
            _elapsedSeconds = 0f;
            _phaseElapsedSeconds = 0f;
            _runState = CoolingIncidentRunState.Active;
            RejectedActionCount = 0;
            TemperatureSpikeCount = 0;
            foreach (IIncidentResettable resettable in _resettableObjects)
            {
                resettable.ResetIncidentState();
            }

            ResetCount++;
            Debug.Log($"[Incident] result=reset count={ResetCount}", this);
            StateChanged?.Invoke();
            RunStateChanged?.Invoke();
            return true;
        }

        public bool AddSealProgress(float amount) => Execute("seal", () => Rules.AddSealProgress(amount));
        public bool TryAdvanceCircuitBridge() => Execute("bridge-circuit", Rules.TryAdvanceCircuitBridge);
        public bool TryInspectPressure() => Execute("inspect-pressure", Rules.TryInspectPressure);
        public bool TryInspectPump() => Execute("inspect-pump", Rules.TryInspectPump);
        public bool TryLoosen() => Execute("loosen", Rules.TryLoosen);
        public bool TryInstallPipe() => Execute("install-pipe", Rules.TryInstallPipe);
        public bool TryTighten() => Execute("tighten", Rules.TryTighten);
        public bool TryResetPump() => Execute("reset-pump", Rules.TryResetPump);

        /// <summary>
        /// 外部系统干扰统一折算为温度冲击；不引入独立玩家生命或第二套失败规则。
        /// </summary>
        public bool ApplyTemperatureSpike(float amount)
        {
            if (_runState != CoolingIncidentRunState.Active || amount <= 0f)
            {
                return false;
            }

            _temperature += amount;
            TemperatureSpikeCount++;
            StateChanged?.Invoke();
            if (_temperature < failureTemperature)
            {
                return true;
            }

            FailIncident("temperature-spike");
            return true;
        }

        /// <summary>
        /// 记录玩家主动尝试但被规则拒绝的动作，供本轮结算与试玩观察使用。
        /// </summary>
        public void RecordRejectedAction(string source, string reason)
        {
            if (_runState != CoolingIncidentRunState.Active)
            {
                return;
            }

            RejectedActionCount++;
            Debug.Log($"[Incident] action=rejected source={source} reason={reason} count={RejectedActionCount}", this);
        }

        private bool Execute(string action, Func<bool> transition)
        {
            if (_runState != CoolingIncidentRunState.Active)
            {
                return false;
            }

            CoolingIncidentPhase before = Rules.Phase;
            bool accepted = transition();
            if (accepted)
            {
                StateChanged?.Invoke();
            }

            if (before != Rules.Phase)
            {
                _phaseElapsedSeconds = 0f;
                Debug.Log($"[Incident] action={action} from={before} to={Rules.Phase}", this);
                if (Rules.Phase == CoolingIncidentPhase.Stabilized)
                {
                    _runState = CoolingIncidentRunState.Succeeded;
                    _lastRunDurationSeconds = _elapsedSeconds;
                    Debug.Log($"[Incident] result=succeeded temperature={_temperature:F1} duration={FormatDuration(_lastRunDurationSeconds)} rejected={RejectedActionCount} spikes={TemperatureSpikeCount}", this);
                    RunStateChanged?.Invoke();
                }
            }

            return accepted;
        }

        public static string FormatDuration(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        private void FailIncident(string reason)
        {
            if (_runState != CoolingIncidentRunState.Active)
            {
                return;
            }

            _runState = CoolingIncidentRunState.Failed;
            _lastRunDurationSeconds = _elapsedSeconds;
            Debug.Log($"[Incident] result=failed reason={reason} phase={Phase} temperature={_temperature:F1} duration={FormatDuration(_lastRunDurationSeconds)} rejected={RejectedActionCount} spikes={TemperatureSpikeCount}", this);
            RunStateChanged?.Invoke();
        }
    }
}
