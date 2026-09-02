using FunGame.Incident;
using UnityEngine;

namespace FunGame.Combat
{
    /// <summary>
    /// 将一次短防卫插曲接入冷却维修：密封完成后激活，受击转化为温度压力，事故重置时一并复位。
    /// </summary>
    public sealed class CoolingCombatIntegrationController : MonoBehaviour, IIncidentResettable
    {
        [SerializeField] private CoolingIncidentController incident;
        [SerializeField] private CombatEncounterController encounter;
        [SerializeField] private DefendableSystemTarget defenseTarget;
        [SerializeField] private CoolingIncidentPhase triggerPhase = CoolingIncidentPhase.LoosenConnection;
        [SerializeField, Min(0.1f)] private float temperatureSpikePerHit = 2.5f;
        private bool _subscribed;

        public bool HasTriggered { get; private set; }
        public bool IsInterferenceActive => encounter != null && encounter.State == CombatEncounterState.Active;
        public CombatEncounterController Encounter => encounter;

        private void Awake()
        {
            Subscribe();
            incident?.RegisterResettable(this);
            if (incident != null && incident.Phase < triggerPhase)
            {
                encounter?.PrepareDormant();
            }
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Configure(
            CoolingIncidentController configuredIncident,
            CombatEncounterController configuredEncounter,
            DefendableSystemTarget configuredTarget,
            float configuredTemperatureSpikePerHit = 2.5f)
        {
            Unsubscribe();
            incident = configuredIncident;
            encounter = configuredEncounter;
            defenseTarget = configuredTarget;
            temperatureSpikePerHit = Mathf.Max(0.1f, configuredTemperatureSpikePerHit);
            Subscribe();
            incident?.RegisterResettable(this);
            encounter?.PrepareDormant();
        }

        public void ResetIncidentState()
        {
            HasTriggered = false;
            encounter?.PrepareDormant();
        }

        private void Subscribe()
        {
            if (_subscribed || incident == null || defenseTarget == null)
            {
                return;
            }

            incident.StateChanged += HandleIncidentStateChanged;
            incident.RunStateChanged += HandleRunStateChanged;
            defenseTarget.Damaged += HandleDeviceDamaged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            if (incident != null)
            {
                incident.StateChanged -= HandleIncidentStateChanged;
                incident.RunStateChanged -= HandleRunStateChanged;
            }

            if (defenseTarget != null)
            {
                defenseTarget.Damaged -= HandleDeviceDamaged;
            }

            _subscribed = false;
        }

        private void HandleIncidentStateChanged()
        {
            if (HasTriggered || incident == null || encounter == null || incident.RunState != CoolingIncidentRunState.Active)
            {
                return;
            }

            if (incident.Phase < triggerPhase)
            {
                return;
            }

            HasTriggered = true;
            encounter.BeginEncounter();
            Debug.Log($"[Combat] integration=repair action=trigger phase={incident.Phase}", this);
        }

        private void HandleRunStateChanged()
        {
            if (incident != null && incident.RunState != CoolingIncidentRunState.Active && encounter != null)
            {
                foreach (InterferenceEnemy enemy in encounter.Enemies)
                {
                    enemy?.SetEncounterActive(false);
                }
            }
        }

        private void HandleDeviceDamaged(int damage)
        {
            if (incident == null || defenseTarget == null || encounter == null || encounter.State != CombatEncounterState.Active)
            {
                return;
            }

            float spike = defenseTarget.IsOffline ? incident.FailureTemperature : temperatureSpikePerHit;
            incident.ApplyTemperatureSpike(spike);
        }
    }
}
