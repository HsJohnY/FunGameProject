using System;
using FunGame.Tools;
using UnityEngine;

namespace FunGame.Combat
{
    /// <summary>
    /// 有限状态干扰体：直线接近或绕至设备侧面附着，蓄力后只攻击设备。
    /// </summary>
    [RequireComponent(typeof(CapsuleCollider), typeof(MeshRenderer), typeof(Rigidbody))]
    public sealed class InterferenceEnemy : MonoBehaviour, IToolTarget
    {
        private const float CollisionSkin = 0.02f;

        [SerializeField] private string targetId = "interference-creature";
        [SerializeField] private string targetName = "线路干扰体";
        [SerializeField] private DefendableSystemTarget defenseTarget;
        [SerializeField] private CombatEncounterController encounter;
        [SerializeField] private InterferenceEnemyBehavior behavior;
        [SerializeField, Min(1)] private int maxHealth = 3;
        [SerializeField, Min(0.1f)] private float moveSpeed = 1.35f;
        [SerializeField, Min(0.1f)] private float attackRange = 1.3f;
        [SerializeField, Min(0.05f)] private float attackIntervalSeconds = 1.25f;
        [SerializeField, Min(0f)] private float attackWindupSeconds = 0.45f;
        [SerializeField, Min(1)] private int interferenceDamage = 10;
        [SerializeField, Min(1)] private int wrenchDamage = 1;
        [SerializeField, Min(0f)] private float knockbackDistance = 1.25f;
        [SerializeField, Min(0f)] private float defeatedVisualSeconds = 0.75f;

        private InterferenceEnemyRules _rules;
        private Collider _collider;
        private Rigidbody _rigidbody;
        private Renderer _renderer;
        private MaterialPropertyBlock _propertyBlock;
        [SerializeField, HideInInspector] private Vector3 spawnPosition;
        [SerializeField, HideInInspector] private Vector3 baseScale;
        [SerializeField, HideInInspector] private bool hasSpawnPose;
        private float _hitFlashRemaining;
        private float _defeatedVisualRemaining;
        private bool _encounterActive = true;
        private bool _reachedFlankWaypoint;
        private float _flankSide = 1f;
        private Collider _avoidanceCollider;
        private Vector3 _avoidanceDirection;

        public event Action<InterferenceEnemy> HitReceived;
        public event Action<InterferenceEnemy> TelegraphStarted;
        public event Action<InterferenceEnemy> AttackCommitted;
        public event Action<InterferenceEnemy> Defeated;
        public int Health => _rules?.Health ?? maxHealth;
        public int MaxHealth => _rules?.MaxHealth ?? maxHealth;
        public bool IsDefeated => _rules != null && _rules.IsDefeated;
        public bool IsTelegraphing => _rules != null && _rules.IsTelegraphing;
        public bool IsEncounterActive => _encounterActive;
        public InterferenceEnemyBehavior Behavior => behavior;
        public DefendableSystemTarget DefenseTarget => defenseTarget;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            EnsurePhysicsBody();
            _renderer = GetComponent<Renderer>();
            _propertyBlock = new MaterialPropertyBlock();
            CaptureSpawnPoseIfNeeded();
            _flankSide = transform.position.x < (defenseTarget != null ? defenseTarget.transform.position.x : 0f) ? -1f : 1f;
            CreateRules();
            RefreshVisual();
        }

        private void Update()
        {
            UpdateFeedback(Time.unscaledDeltaTime);
            if (!_encounterActive || IsDefeated || defenseTarget == null || defenseTarget.IsOffline)
            {
                return;
            }

            Vector3 destination = GetDestination();
            Vector3 toDestination = destination - transform.position;
            toDestination.y = 0f;
            float distance = toDestination.magnitude;
            bool targetInRange = distance <= attackRange;

            if (!targetInRange && distance > 0.001f)
            {
                Vector3 direction = toDestination / distance;
                MoveWithCollision(direction * (moveSpeed * Time.deltaTime), true);
                transform.forward = direction;
            }

            InterferenceEnemyAction action = _rules.Advance(Time.deltaTime, targetInRange);
            if (action == InterferenceEnemyAction.TelegraphStarted)
            {
                TelegraphStarted?.Invoke(this);
            }
            else if (action == InterferenceEnemyAction.AttackCommitted)
            {
                AttackCommitted?.Invoke(this);
                bool targetOffline = defenseTarget.ApplyInterference(interferenceDamage);
                if (targetOffline)
                {
                    encounter?.NotifySystemOffline();
                }
            }
        }

        public void Configure(
            DefendableSystemTarget configuredTarget,
            CombatEncounterController configuredEncounter,
            int configuredMaxHealth = 3,
            float configuredMoveSpeed = 1.35f,
            float configuredAttackRange = 1.3f,
            float configuredAttackIntervalSeconds = 1.25f,
            int configuredInterferenceDamage = 10,
            int configuredWrenchDamage = 1,
            float configuredKnockbackDistance = 1.25f,
            InterferenceEnemyBehavior configuredBehavior = InterferenceEnemyBehavior.Direct,
            float configuredAttackWindupSeconds = 0.45f)
        {
            defenseTarget = configuredTarget;
            encounter = configuredEncounter;
            behavior = configuredBehavior;
            maxHealth = Mathf.Max(1, configuredMaxHealth);
            moveSpeed = Mathf.Max(0.1f, configuredMoveSpeed);
            attackRange = Mathf.Max(0.1f, configuredAttackRange);
            attackIntervalSeconds = Mathf.Max(0.05f, configuredAttackIntervalSeconds);
            attackWindupSeconds = Mathf.Max(0f, configuredAttackWindupSeconds);
            interferenceDamage = Mathf.Max(1, configuredInterferenceDamage);
            wrenchDamage = Mathf.Max(1, configuredWrenchDamage);
            knockbackDistance = Mathf.Max(0f, configuredKnockbackDistance);
            spawnPosition = transform.position;
            baseScale = transform.localScale;
            hasSpawnPose = true;
            _flankSide = transform.position.x < configuredTarget.transform.position.x ? -1f : 1f;
            _reachedFlankWaypoint = false;
            EnsurePhysicsBody();
            CreateRules();
            SetEncounterActive(true);
            RefreshVisual();
        }

        public ToolActionOption GetToolAction(PlayerToolbelt toolbelt)
        {
            bool canDefend = _encounterActive && !IsDefeated && defenseTarget != null && !defenseTarget.IsOffline;
            return new ToolActionOption(
                targetId,
                targetName,
                "击退",
                ToolKind.ImpactWrench,
                toolbelt.EquippedTool,
                canDefend,
                IsDefeated ? "干扰体已失去活动能力" : "防卫遭遇尚未开始或已经结束");
        }

        public bool ApplyTool(PlayerToolbelt toolbelt)
        {
            ToolActionOption option = GetToolAction(toolbelt);
            if (!option.IsAvailable)
            {
                return false;
            }

            bool defeated = _rules.ReceiveHit(wrenchDamage);
            _hitFlashRemaining = 0.12f;
            if (defeated)
            {
                _defeatedVisualRemaining = defeatedVisualSeconds;
            }

            ApplyKnockback(toolbelt.transform.position);
            RefreshVisual();
            HitReceived?.Invoke(this);
            Debug.Log($"[Combat] target={targetId} action=wrench-hit health={Health}/{MaxHealth} defeated={defeated}", this);

            if (defeated)
            {
                SetEncounterActive(false);
                Defeated?.Invoke(this);
                encounter?.NotifyEnemyDefeated(this);
            }

            return true;
        }

        public void ResetEnemy()
        {
            CaptureSpawnPoseIfNeeded();
            if (_rules == null)
            {
                CreateRules();
            }
            else
            {
                _rules.Reset();
            }

            EnsurePhysicsBody();
            _rigidbody.position = spawnPosition;
            transform.localScale = baseScale;
            _reachedFlankWaypoint = false;
            ClearAvoidance();
            _hitFlashRemaining = 0f;
            _defeatedVisualRemaining = 0f;
            SetEncounterActive(true);
            RefreshVisual();
        }

        public void SetEncounterActive(bool active)
        {
            _encounterActive = active;
            if (_collider == null)
            {
                _collider = GetComponent<Collider>();
            }

            if (_collider != null)
            {
                _collider.enabled = active;
            }

            if (_renderer != null)
            {
                _renderer.enabled = active || IsDefeated;
            }
        }

        private Vector3 GetDestination()
        {
            if (behavior == InterferenceEnemyBehavior.Direct)
            {
                return defenseTarget.transform.position;
            }

            float halfWidth = 1f;
            if (defenseTarget.TryGetComponent(out BoxCollider box))
            {
                halfWidth = box.bounds.extents.x;
            }

            Vector3 side = defenseTarget.transform.right * (_flankSide * (halfWidth + 0.3f));
            Vector3 away = spawnPosition - defenseTarget.transform.position;
            away.y = 0f;
            away = away.sqrMagnitude > 0.001f ? away.normalized : -defenseTarget.transform.forward;
            Vector3 attachPoint = defenseTarget.transform.position + side;
            Vector3 flankWaypoint = attachPoint + away * 1.8f;
            if (!_reachedFlankWaypoint && Vector3.Distance(transform.position, flankWaypoint) <= attackRange)
            {
                _reachedFlankWaypoint = true;
            }

            return _reachedFlankWaypoint ? attachPoint : flankWaypoint;
        }

        private void ApplyKnockback(Vector3 sourcePosition)
        {
            Vector3 away = transform.position - sourcePosition;
            away.y = 0f;
            if (away.sqrMagnitude < 0.001f && defenseTarget != null)
            {
                away = transform.position - defenseTarget.transform.position;
                away.y = 0f;
            }

            if (away.sqrMagnitude > 0.001f)
            {
                MoveWithCollision(away.normalized * knockbackDistance, false);
            }
        }

        private void EnsurePhysicsBody()
        {
            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
            }

            if (_rigidbody == null)
            {
                _rigidbody = gameObject.AddComponent<Rigidbody>();
            }

            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = true;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _rigidbody.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        }

        private void MoveWithCollision(Vector3 displacement, bool navigateAroundObstacle)
        {
            float requestedDistance = displacement.magnitude;
            if (requestedDistance <= 0.0001f)
            {
                return;
            }

            EnsurePhysicsBody();
            Vector3 direction = displacement / requestedDistance;
            if (!_rigidbody.SweepTest(direction, out RaycastHit hit, requestedDistance + CollisionSkin, QueryTriggerInteraction.Ignore))
            {
                _rigidbody.position += displacement;
                if (navigateAroundObstacle)
                {
                    ClearAvoidance();
                }

                return;
            }

            float allowedDistance = Mathf.Max(0f, hit.distance - CollisionSkin);
            _rigidbody.position += direction * allowedDistance;
            if (!navigateAroundObstacle)
            {
                return;
            }

            float remainingDistance = requestedDistance - allowedDistance;
            if (remainingDistance <= 0.0001f)
            {
                return;
            }

            Vector3 avoidanceDirection = GetAvoidanceDirection(direction, hit);
            if (avoidanceDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            float avoidanceDistance = remainingDistance;
            if (_rigidbody.SweepTest(
                    avoidanceDirection,
                    out RaycastHit avoidanceHit,
                    remainingDistance + CollisionSkin,
                    QueryTriggerInteraction.Ignore))
            {
                avoidanceDistance = Mathf.Max(0f, avoidanceHit.distance - CollisionSkin);
            }

            _rigidbody.position += avoidanceDirection * avoidanceDistance;
        }

        private Vector3 GetAvoidanceDirection(Vector3 requestedDirection, RaycastHit hit)
        {
            Vector3 obstacleNormal = hit.normal;
            obstacleNormal.y = 0f;
            if (obstacleNormal.sqrMagnitude <= 0.0001f)
            {
                return Vector3.zero;
            }

            obstacleNormal.Normalize();
            if (_avoidanceCollider == hit.collider && _avoidanceDirection.sqrMagnitude > 0.0001f)
            {
                Vector3 retainedDirection = Vector3.ProjectOnPlane(_avoidanceDirection, obstacleNormal);
                retainedDirection.y = 0f;
                if (retainedDirection.sqrMagnitude > 0.0001f)
                {
                    _avoidanceDirection = retainedDirection.normalized;
                    return _avoidanceDirection;
                }
            }

            Vector3 directionAlongObstacle = Vector3.ProjectOnPlane(requestedDirection, obstacleNormal);
            directionAlongObstacle.y = 0f;
            if (directionAlongObstacle.sqrMagnitude <= 0.0025f)
            {
                directionAlongObstacle = Vector3.Cross(Vector3.up, obstacleNormal).normalized;
                Vector3 toTarget = defenseTarget != null
                    ? defenseTarget.transform.position - transform.position
                    : requestedDirection;
                toTarget.y = 0f;
                float targetAlignment = Vector3.Dot(directionAlongObstacle, toTarget);
                if (targetAlignment < -0.01f || (Mathf.Abs(targetAlignment) <= 0.01f && _flankSide < 0f))
                {
                    directionAlongObstacle = -directionAlongObstacle;
                }
            }

            _avoidanceCollider = hit.collider;
            _avoidanceDirection = directionAlongObstacle.normalized;
            return _avoidanceDirection;
        }

        private void ClearAvoidance()
        {
            _avoidanceCollider = null;
            _avoidanceDirection = Vector3.zero;
        }

        private void CreateRules()
        {
            _rules = new InterferenceEnemyRules(maxHealth, attackIntervalSeconds, attackWindupSeconds);
        }

        private void UpdateFeedback(float unscaledDeltaTime)
        {
            _hitFlashRemaining = Mathf.Max(0f, _hitFlashRemaining - unscaledDeltaTime);
            CaptureSpawnPoseIfNeeded();

            if (IsDefeated)
            {
                _defeatedVisualRemaining = Mathf.Max(0f, _defeatedVisualRemaining - unscaledDeltaTime);
                float vanishProgress = defeatedVisualSeconds <= 0.001f
                    ? 1f
                    : 1f - (_defeatedVisualRemaining / defeatedVisualSeconds);
                float horizontalScale = Mathf.Lerp(0.72f, 0.08f, vanishProgress);
                float verticalScale = Mathf.Lerp(0.6f, 0.03f, vanishProgress);
                transform.localScale = Vector3.Scale(
                    baseScale,
                    new Vector3(horizontalScale, verticalScale, horizontalScale));
                RefreshVisual();
                if (_renderer != null)
                {
                    _renderer.enabled = _defeatedVisualRemaining > 0f;
                }

                return;
            }

            float healthRatio = MaxHealth <= 0 ? 0f : (float)Health / MaxHealth;
            float weakenedScale = Mathf.Lerp(0.72f, 1f, healthRatio);
            float pulse = IsTelegraphing ? 1f + Mathf.Sin(Time.unscaledTime * 30f) * 0.12f : 1f;
            float hitSquash = _hitFlashRemaining > 0f ? 0.82f : 1f;
            transform.localScale = Vector3.Scale(baseScale, new Vector3(pulse, hitSquash, pulse) * weakenedScale);
            RefreshVisual();
        }

        private void CaptureSpawnPoseIfNeeded()
        {
            if (hasSpawnPose)
            {
                return;
            }

            spawnPosition = transform.position;
            baseScale = transform.localScale;
            hasSpawnPose = true;
        }

        private void RefreshVisual()
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<Renderer>();
            }

            if (_renderer == null)
            {
                return;
            }

            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }

            float ratio = MaxHealth <= 0 ? 0f : (float)Health / MaxHealth;
            Color stateColor = IsDefeated
                ? new Color(0.08f, 0.08f, 0.08f)
                : Color.Lerp(new Color(1f, 0.2f, 0.05f), new Color(0.7f, 0.1f, 0.85f), ratio);
            if (IsTelegraphing)
            {
                stateColor = Color.Lerp(stateColor, new Color(1f, 0.75f, 0.05f), 0.75f);
            }

            if (_hitFlashRemaining > 0f)
            {
                stateColor = Color.white;
            }

            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor("_BaseColor", stateColor);
            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
