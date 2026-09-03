using FunGame.Interaction;
using UnityEngine;

namespace FunGame.Incident
{
    /// <summary>
    /// 在少量人工验证过的位置间轮换维修工位，提供路线变化而不引入随机事故导演。
    /// </summary>
    public sealed class CoolingIncidentLayoutController : MonoBehaviour
    {
        [SerializeField] private CoolingIncidentController incident;
        [SerializeField] private Transform leakTarget;
        [SerializeField] private Transform repairAssembly;
        [SerializeField] private Transform replacementRecoveryPoint;
        [SerializeField] private CarryableInteractable replacementPipe;
        [SerializeField] private ContextInteractor playerInteractor;
        [SerializeField] private Vector3[] leakPositions;
        [SerializeField] private Vector3[] repairPositions;
        [SerializeField] private Vector3[] recoveryPositions;
        private bool _subscribed;

        public int CurrentLayoutIndex { get; private set; }
        public int LayoutCount => ValidLayoutCount;

        public void Configure(
            CoolingIncidentController incidentController,
            Transform configuredLeakTarget,
            Transform configuredRepairAssembly,
            Transform configuredRecoveryPoint,
            CarryableInteractable configuredReplacementPipe,
            Vector3[] configuredLeakPositions,
            Vector3[] configuredRepairPositions,
            Vector3[] configuredRecoveryPositions)
        {
            Unsubscribe();
            incident = incidentController;
            leakTarget = configuredLeakTarget;
            repairAssembly = configuredRepairAssembly;
            replacementRecoveryPoint = configuredRecoveryPoint;
            replacementPipe = configuredReplacementPipe;
            leakPositions = configuredLeakPositions;
            repairPositions = configuredRepairPositions;
            recoveryPositions = configuredRecoveryPositions;
            Subscribe();
            ApplyLayout(0, false);
        }

        public void ConfigurePlayer(ContextInteractor configuredPlayerInteractor)
        {
            playerInteractor = configuredPlayerInteractor;
        }

        private void Awake()
        {
            ApplyLayout(0, false);
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void HandleRunStateChanged()
        {
            if (incident == null || incident.RunState != CoolingIncidentRunState.Active || ValidLayoutCount == 0)
            {
                return;
            }

            ApplyLayout(incident.ResetCount % ValidLayoutCount, true);
        }

        public bool ApplyLayout(int index, bool recoverTaskItem)
        {
            int count = ValidLayoutCount;
            if (count == 0 || index < 0 || index >= count)
            {
                return false;
            }

            CurrentLayoutIndex = index;
            leakTarget.position = leakPositions[index];
            repairAssembly.position = repairPositions[index];
            replacementRecoveryPoint.position = recoveryPositions[index];

            if (recoverTaskItem && replacementPipe != null)
            {
                playerInteractor?.ReleaseHeldItemForRecovery(replacementPipe);
                replacementPipe.RecoverTo(replacementRecoveryPoint.position);
            }

            Physics.SyncTransforms();
            Debug.Log($"[Incident] layout={CurrentLayoutIndex + 1}/{count}", this);
            return true;
        }

        private int ValidLayoutCount
        {
            get
            {
                if (leakTarget == null || repairAssembly == null || replacementRecoveryPoint == null ||
                    leakPositions == null || repairPositions == null || recoveryPositions == null)
                {
                    return 0;
                }

                return Mathf.Min(leakPositions.Length, repairPositions.Length, recoveryPositions.Length);
            }
        }

        private void Subscribe()
        {
            if (_subscribed || incident == null)
            {
                return;
            }

            incident.RunStateChanged += HandleRunStateChanged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || incident == null)
            {
                return;
            }

            incident.RunStateChanged -= HandleRunStateChanged;
            _subscribed = false;
        }
    }
}
