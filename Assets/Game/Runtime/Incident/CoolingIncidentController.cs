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
        [SerializeField, Min(0f)] private float temperatureRisePerSecond = 0.75f;
        private float _temperature;
        private CoolingIncidentRunState _runState = CoolingIncidentRunState.Active;

        public event Action StateChanged;
        public event Action RunStateChanged;
        public CoolingIncidentPhase Phase => Rules.Phase;
        public float SealProgress => Rules.SealProgress;
        public string CurrentInstruction => Rules.CurrentInstruction;
        public CoolingIncidentRunState RunState => _runState;
        public float Temperature => _temperature;
        public float FailureTemperature => failureTemperature;
        public int ResetCount { get; private set; }
        private CoolingIncidentRules Rules => _rules ?? (_rules = new CoolingIncidentRules());

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

            _temperature += temperatureRisePerSecond * Time.deltaTime;
            if (_temperature < failureTemperature)
            {
                return;
            }

            _runState = CoolingIncidentRunState.Failed;
            Debug.Log($"[Incident] result=failed phase={Phase} temperature={_temperature:F1}", this);
            RunStateChanged?.Invoke();
        }

        public void ConfigureTemperature(float starting, float failure, float risePerSecond)
        {
            startingTemperature = starting;
            failureTemperature = failure;
            temperatureRisePerSecond = risePerSecond;
            _temperature = starting;
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
            _runState = CoolingIncidentRunState.Active;
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
        public bool TryLoosen() => Execute("loosen", Rules.TryLoosen);
        public bool TryInstallPipe() => Execute("install-pipe", Rules.TryInstallPipe);
        public bool TryTighten() => Execute("tighten", Rules.TryTighten);
        public bool TryResetPump() => Execute("reset-pump", Rules.TryResetPump);

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
                Debug.Log($"[Incident] action={action} from={before} to={Rules.Phase}", this);
                if (Rules.Phase == CoolingIncidentPhase.Stabilized)
                {
                    _runState = CoolingIncidentRunState.Succeeded;
                    Debug.Log($"[Incident] result=succeeded temperature={_temperature:F1}", this);
                    RunStateChanged?.Invoke();
                }
            }

            return accepted;
        }
    }
}
