using System;
using System.Collections.Generic;
using FunGame.Tools;
using UnityEngine;
using UnityEngine.Serialization;
using FunGame.Content;

namespace FunGame.Combat
{
    /// <summary>
    /// 有限状态干扰体：直线接近或绕至设备侧面附着，蓄力后只攻击设备。
    /// </summary>
    [RequireComponent(typeof(CapsuleCollider), typeof(MeshRenderer), typeof(Rigidbody))]
    public sealed class InterferenceEnemy : MonoBehaviour, IToolTarget
    {
        [SerializeField] private EnemyDefinition definition;
        public EnemyDefinition Definition => definition;
        public void ConfigureDefinition(EnemyDefinition value) => definition = value;

        private const float CollisionSkin = 0.02f;

        [SerializeField] private string targetId = "interference-creature";
        [SerializeField] private string targetName = "线路干扰体";
        [SerializeField] private DefendableSystemTarget defenseTarget;
        [SerializeField] private CombatEncounterController encounter;
        [SerializeField, HideInInspector, FormerlySerializedAs("behavior")] private InterferenceEnemyBehavior legacyBehavior = InterferenceEnemyBehavior.Direct;
        private InterferenceEnemyBehavior behavior { get => definition != null ? definition.Behavior : legacyBehavior; set => legacyBehavior = value; }
        [SerializeField, HideInInspector, FormerlySerializedAs("maxHealth")] private int legacyMaxHealth = 3;
        private int maxHealth { get => definition != null ? definition.MaxHealth : legacyMaxHealth; set => legacyMaxHealth = value; }
        [SerializeField, HideInInspector, FormerlySerializedAs("moveSpeed")] private float legacyMoveSpeed = 1.35f;
        private float moveSpeed { get => definition != null ? definition.MoveSpeed : legacyMoveSpeed; set => legacyMoveSpeed = value; }
        [SerializeField, HideInInspector, FormerlySerializedAs("attackRange")] private float legacyAttackRange = 1.3f;
        private float attackRange { get => definition != null ? definition.AttackRange : legacyAttackRange; set => legacyAttackRange = value; }
        [SerializeField, HideInInspector, FormerlySerializedAs("attackIntervalSeconds")] private float legacyAttackIntervalSeconds = 1.25f;
        private float attackIntervalSeconds { get => definition != null ? definition.AttackIntervalSeconds : legacyAttackIntervalSeconds; set => legacyAttackIntervalSeconds = value; }
        [SerializeField, HideInInspector, FormerlySerializedAs("attackWindupSeconds")] private float legacyAttackWindupSeconds = 0.45f;
        private float attackWindupSeconds { get => definition != null ? definition.AttackWindupSeconds : legacyAttackWindupSeconds; set => legacyAttackWindupSeconds = value; }
        [SerializeField, HideInInspector, FormerlySerializedAs("interferenceDamage")] private int legacyInterferenceDamage = 10;
        private int interferenceDamage { get => definition != null ? definition.InterferenceDamage : legacyInterferenceDamage; set => legacyInterferenceDamage = value; }
        [SerializeField, HideInInspector, FormerlySerializedAs("wrenchDamage")] private int legacyWrenchDamage = 2;
        private int wrenchDamage { get => definition != null ? definition.WrenchDamage : legacyWrenchDamage; set => legacyWrenchDamage = value; }
        [SerializeField, HideInInspector, FormerlySerializedAs("knockbackDistance")] private float legacyKnockbackDistance = 1.25f;
        private float knockbackDistance { get => definition != null ? definition.KnockbackDistance : legacyKnockbackDistance; set => legacyKnockbackDistance = value; }
        [SerializeField, HideInInspector, FormerlySerializedAs("sealantSpeedMultiplier")] private float legacySealantSpeedMultiplier = 0.35f;
        private float sealantSpeedMultiplier { get => definition != null ? definition.SealantSpeedMultiplier : legacySealantSpeedMultiplier; set => legacySealantSpeedMultiplier = value; }
        [SerializeField, HideInInspector, FormerlySerializedAs("sealantSlowSeconds")] private float legacySealantSlowSeconds = 2.25f;
        private float sealantSlowSeconds { get => definition != null ? definition.SealantSlowSeconds : legacySealantSlowSeconds; set => legacySealantSlowSeconds = value; }
        [SerializeField, HideInInspector, FormerlySerializedAs("sealantPulseIntervalSeconds")] private float legacySealantPulseIntervalSeconds = 0.15f;
        private float sealantPulseIntervalSeconds { get => definition != null ? definition.SealantPulseIntervalSeconds : legacySealantPulseIntervalSeconds; set => legacySealantPulseIntervalSeconds = value; }
        [SerializeField, HideInInspector, FormerlySerializedAs("sealantPushDistance")] private float legacySealantPushDistance = 0.18f;
        private float sealantPushDistance { get => definition != null ? definition.SealantPushDistance : legacySealantPushDistance; set => legacySealantPushDistance = value; }
        [SerializeField, HideInInspector, FormerlySerializedAs("sealantDamage")] private int legacySealantDamage = 1;
        private int sealantDamage { get => definition != null ? definition.SealantDamage : legacySealantDamage; set => legacySealantDamage = value; }
        [SerializeField, HideInInspector, FormerlySerializedAs("sealantSplashRadius")] private float legacySealantSplashRadius = 1.65f;
        private float sealantSplashRadius { get => definition != null ? definition.SealantSplashRadius : legacySealantSplashRadius; set => legacySealantSplashRadius = value; }
        [SerializeField, HideInInspector, FormerlySerializedAs("bridgerStunSeconds")] private float legacyBridgerStunSeconds = 1.4f;
        private float bridgerStunSeconds { get => definition != null ? definition.BridgerStunSeconds : legacyBridgerStunSeconds; set => legacyBridgerStunSeconds = value; }
        [SerializeField, HideInInspector, FormerlySerializedAs("bridgerOverloadDamage")] private int legacyBridgerOverloadDamage = 1;
        private int bridgerOverloadDamage { get => definition != null ? definition.BridgerOverloadDamage : legacyBridgerOverloadDamage; set => legacyBridgerOverloadDamage = value; }
        [SerializeField, HideInInspector, FormerlySerializedAs("requiresCircuitDisruption")] private bool legacyRequiresCircuitDisruption = false;
        private bool requiresCircuitDisruption { get => definition != null ? definition.RequiresCircuitDisruption : legacyRequiresCircuitDisruption; set => legacyRequiresCircuitDisruption = value; }
        [SerializeField, HideInInspector, FormerlySerializedAs("defeatedVisualSeconds")] private float legacyDefeatedVisualSeconds = 0.75f;
        private float defeatedVisualSeconds { get => definition != null ? definition.DefeatedVisualSeconds : legacyDefeatedVisualSeconds; set => legacyDefeatedVisualSeconds = value; }

        private InterferenceEnemyRules _rules;
        private Collider _collider;
        private Rigidbody _rigidbody;
        private Renderer _renderer;
        private MeshRenderer[] _modelRenderers;
        private MaterialPropertyBlock _propertyBlock;
        [SerializeField, HideInInspector] private Vector3 spawnPosition;
        [SerializeField, HideInInspector] private Vector3 baseScale;
        [SerializeField, HideInInspector] private bool hasSpawnPose;
        [SerializeField] private bool hasCombatPosition;
        [SerializeField] private Vector3 attackLocalPoint;
        [SerializeField] private Vector3 approachLocalPoint;
        [SerializeField, Min(0f)] private float deploymentDelay;
        private float _deploymentEndsAt;
        private bool _deploymentShown;
        private float _hitFlashRemaining;
        private float _defeatedVisualRemaining;
        private float _slowedUntil;
        private float _stunnedUntil;
        private float _nextSealantPulseTime;
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
        public bool IsSlowed => Time.time < _slowedUntil;
        public bool IsStunned => Time.time < _stunnedUntil;
        public bool IsDisruptionShieldActive => requiresCircuitDisruption && !IsStunned && !IsDefeated;
        public bool IsEncounterActive => _encounterActive;
        public InterferenceEnemyBehavior Behavior => behavior;
        public DefendableSystemTarget DefenseTarget => defenseTarget;
        public string TargetId => targetId;
        public string DisplayName => targetName;
        public float MoveSpeed => moveSpeed;
        public float AttackInterval => attackIntervalSeconds;
        public float AttackWindup => attackWindupSeconds;
        public int InterferenceDamage => interferenceDamage;
        public int WrenchDamage => wrenchDamage;
        public float SealantSpeedMultiplier => sealantSpeedMultiplier;
        public float SealantSlowSeconds => sealantSlowSeconds;
        public float SealantPulseInterval => sealantPulseIntervalSeconds;
        public float SealantPushDistance => sealantPushDistance;
        public int SealantDamage => sealantDamage;
        public float SealantSplashRadius => sealantSplashRadius;
        public float BridgerStunSeconds => bridgerStunSeconds;
        public int BridgerDamage => bridgerOverloadDamage;
        public float KnockbackDistance => knockbackDistance;
        public bool RequiresCircuitDisruption => requiresCircuitDisruption;
        public Vector3 AuthoredScale => baseScale;
        public bool HasCombatPosition => hasCombatPosition;
        private float? _deploymentDelayOverride;
        public float DeploymentDelay => _deploymentDelayOverride ?? (encounter != null && encounter.Definition != null
            ? encounter.Definition.GetDelay(targetId, deploymentDelay) : deploymentDelay);
        public bool IsDeployed => _encounterActive && Time.time >= _deploymentEndsAt;
        public float DeploymentRemaining => Mathf.Max(0f, _deploymentEndsAt - Time.time);
        public void ConfigureDeployment(float delay)
        {
            if (Application.isPlaying) _deploymentDelayOverride = Mathf.Max(0f, delay);
            else deploymentDelay = Mathf.Max(0f, delay);
        }
        public Vector3 AttackPosition => hasCombatPosition ? defenseTarget.transform.TransformPoint(attackLocalPoint) : defenseTarget.transform.position;
        public Vector3 ApproachPosition => hasCombatPosition ? defenseTarget.transform.TransformPoint(approachLocalPoint) : AttackPosition;

        public void ConfigureCombatPosition(Vector3 spawn, Vector3 attack, Vector3 approach)
        {
            transform.position = spawn;
            spawnPosition = spawn;
            attackLocalPoint = defenseTarget.transform.InverseTransformPoint(attack);
            approachLocalPoint = defenseTarget.transform.InverseTransformPoint(approach);
            hasCombatPosition = true;
        }

        public Vector3 GetApproachDestination(Vector3 position, ref bool reachedWaypoint)
        {
            Vector3 toWaypoint = ApproachPosition - position;
            toWaypoint.y = 0f;
            if (toWaypoint.sqrMagnitude <= 0.09f) reachedWaypoint = true;
            return reachedWaypoint ? AttackPosition : ApproachPosition;
        }

        public bool IsAtCombatPosition(Vector3 position)
        {
            Vector3 delta = AttackPosition - position;
            delta.y = 0f;
            return delta.sqrMagnitude <= 0.0625f;
        }

        public void ConfigureIdentity(string id, string displayName)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                targetId = id;
            }

            if (!string.IsNullOrWhiteSpace(displayName))
            {
                targetName = displayName;
            }
        }

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            EnsurePhysicsBody();
            _renderer = GetComponent<Renderer>();
            _propertyBlock = new MaterialPropertyBlock();
            // 场景生成时父舱室可能在 Configure 后平移，以加载完成的世界坐标为准。
            hasSpawnPose = false;
            CaptureSpawnPoseIfNeeded();
            _flankSide = transform.position.x < (defenseTarget != null ? defenseTarget.transform.position.x : 0f) ? -1f : 1f;
            CreateRules();
            RefreshVisual();
        }

        private void Update()
        {
            if (_encounterActive && !_deploymentShown && IsDeployed) SetEncounterActive(true);
            UpdateFeedback(Time.unscaledDeltaTime);
            if (!IsDeployed || IsDefeated || defenseTarget == null || defenseTarget.IsOffline)
            {
                return;
            }

            if (IsStunned)
            {
                // 瘫痪会中断当前攻击蓄力，恢复后必须重新给出预警。
                _rules.Advance(0f, false);
                return;
            }

            Vector3 destination = GetDestination();
            Vector3 toDestination = destination - transform.position;
            toDestination.y = 0f;
            float distance = toDestination.magnitude;
            bool targetInRange = hasCombatPosition
                ? _reachedFlankWaypoint && IsAtCombatPosition(transform.position)
                : distance <= attackRange && (behavior != InterferenceEnemyBehavior.FlankingAttach || _reachedFlankWaypoint);

            if (!targetInRange && distance > 0.001f)
            {
                Vector3 direction = toDestination / distance;
                float speedMultiplier = IsSlowed ? sealantSpeedMultiplier : 1f;
                MoveWithCollision(direction * Mathf.Min(distance, moveSpeed * speedMultiplier * Time.deltaTime), true);
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
            int configuredWrenchDamage = 2,
            float configuredKnockbackDistance = 1.25f,
            InterferenceEnemyBehavior configuredBehavior = InterferenceEnemyBehavior.Direct,
            float configuredAttackWindupSeconds = 0.45f,
            float configuredSealantSpeedMultiplier = 0.35f,
            float configuredSealantSlowSeconds = 2.25f,
            float configuredSealantPushDistance = 0.18f,
            float configuredBridgerStunSeconds = 1.4f,
            int configuredBridgerOverloadDamage = 1,
            int configuredSealantDamage = 1,
            float configuredSealantSplashRadius = 1.65f,
            bool configuredRequiresCircuitDisruption = false)
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
            sealantSpeedMultiplier = Mathf.Clamp(configuredSealantSpeedMultiplier, 0.1f, 1f);
            sealantSlowSeconds = Mathf.Max(0.1f, configuredSealantSlowSeconds);
            sealantPushDistance = Mathf.Max(0f, configuredSealantPushDistance);
            sealantDamage = Mathf.Max(1, configuredSealantDamage);
            sealantSplashRadius = Mathf.Max(0.1f, configuredSealantSplashRadius);
            bridgerStunSeconds = Mathf.Max(0.1f, configuredBridgerStunSeconds);
            bridgerOverloadDamage = Mathf.Max(0, configuredBridgerOverloadDamage);
            requiresCircuitDisruption = configuredRequiresCircuitDisruption;
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
            bool canDefend = IsDeployed && !IsDefeated && defenseTarget != null && !defenseTarget.IsOffline;
            ToolKind equippedTool = toolbelt.EquippedTool;
            if (canDefend && IsDisruptionShieldActive && equippedTool != ToolKind.CircuitBridger)
            {
                return new ToolActionOption(
                    targetId,
                    targetName,
                    "短路护盾",
                    ToolKind.CircuitBridger,
                    equippedTool,
                    true);
            }

            ToolKind requiredTool = equippedTool == ToolKind.None ? ToolKind.ImpactWrench : equippedTool;
            string actionLabel;
            switch (equippedTool)
            {
                case ToolKind.SealantGun:
                    actionLabel = "范围喷覆";
                    break;
                case ToolKind.CircuitBridger:
                    actionLabel = IsDisruptionShieldActive ? "短路护盾" : "电击瘫痪";
                    break;
                default:
                    actionLabel = "重击";
                    break;
            }

            return new ToolActionOption(
                targetId,
                targetName,
                actionLabel,
                requiredTool,
                equippedTool,
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

            ToolKind tool = toolbelt.EquippedTool;
            switch (tool)
            {
                case ToolKind.SealantGun:
                    return ApplySealantArea(toolbelt.transform.position);
                case ToolKind.CircuitBridger:
                    bool brokeShield = IsDisruptionShieldActive;
                    _stunnedUntil = Mathf.Max(_stunnedUntil, Time.time + bridgerStunSeconds);
                    _rules.Advance(0f, false);
                    return ApplyCombatResult(
                        "bridger-stun",
                        _rules.ReceiveHit(bridgerOverloadDamage),
                        brokeShield ? " shield=disabled" : string.Empty);
                case ToolKind.ImpactWrench:
                    ApplyKnockback(toolbelt.transform.position, knockbackDistance);
                    return ApplyCombatResult("wrench-heavy-hit", _rules.ReceiveHit(wrenchDamage));
                default:
                    return false;
            }
        }

        private bool ApplySealantArea(Vector3 sourcePosition)
        {
            Vector3 center = transform.position;
            if (!TryApplySealantEffect(sourcePosition, "sealant-area-primary"))
            {
                return false;
            }

            Collider[] overlaps = Physics.OverlapSphere(
                center,
                sealantSplashRadius,
                ~0,
                QueryTriggerInteraction.Ignore);
            var affected = new HashSet<InterferenceEnemy> { this };
            foreach (Collider overlap in overlaps)
            {
                InterferenceEnemy nearby = overlap.GetComponentInParent<InterferenceEnemy>();
                if (nearby == null || nearby.encounter != encounter || !affected.Add(nearby))
                {
                    continue;
                }

                nearby.TryApplySealantEffect(sourcePosition, "sealant-area-splash");
            }

            return true;
        }

        private bool TryApplySealantEffect(Vector3 sourcePosition, string action)
        {
            if (!IsDeployed || IsDefeated || Time.time < _nextSealantPulseTime)
            {
                return false;
            }

            _nextSealantPulseTime = Time.time + sealantPulseIntervalSeconds;
            _slowedUntil = Mathf.Max(_slowedUntil, Time.time + sealantSlowSeconds);
            ApplyKnockback(sourcePosition, sealantPushDistance);
            int damage = IsDisruptionShieldActive ? 0 : sealantDamage;
            return ApplyCombatResult(action, _rules.ReceiveHit(damage));
        }

        private bool ApplyCombatResult(string action, bool defeated, string extraLog = "")
        {

            _hitFlashRemaining = 0.12f;
            if (defeated)
            {
                _defeatedVisualRemaining = defeatedVisualSeconds;
            }

            RefreshVisual();
            HitReceived?.Invoke(this);
            Debug.Log($"[Combat] target={targetId} action={action} health={Health}/{MaxHealth} slowed={IsSlowed} stunned={IsStunned} defeated={defeated}{extraLog}", this);

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
            _slowedUntil = 0f;
            _stunnedUntil = 0f;
            _nextSealantPulseTime = 0f;
            _deploymentEndsAt = Time.time + DeploymentDelay;
            SetEncounterActive(true);
            RefreshVisual();
        }

        public void SetEncounterActive(bool active)
        {
            _encounterActive = active;
            bool visible = active && IsDeployed;
            _deploymentShown = visible;
            if (_collider == null)
            {
                _collider = GetComponent<Collider>();
            }

            Collider[] hitColliders = GetComponentsInChildren<Collider>(true);
            foreach (Collider hitCollider in hitColliders)
            {
                if (hitCollider != null)
                {
                    hitCollider.enabled = visible;
                }
            }

            _modelRenderers = GetComponentsInChildren<MeshRenderer>(true);
            SetModelVisibility(visible || (IsDefeated && _defeatedVisualRemaining > 0f));
        }

        private Vector3 GetDestination()
        {
            if (hasCombatPosition)
                return GetApproachDestination(transform.position, ref _reachedFlankWaypoint);

            if (behavior == InterferenceEnemyBehavior.Direct || behavior == InterferenceEnemyBehavior.RangedPulse)
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

        private void ApplyKnockback(Vector3 sourcePosition, float distance)
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
                MoveWithCollision(away.normalized * distance, false);
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
                SetModelVisibility(_defeatedVisualRemaining > 0f);

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

        private void SetModelVisibility(bool visible)
        {
            if (_modelRenderers == null)
            {
                _modelRenderers = GetComponentsInChildren<MeshRenderer>(true);
            }

            foreach (MeshRenderer modelRenderer in _modelRenderers)
            {
                if (modelRenderer != null)
                {
                    modelRenderer.enabled = visible;
                }
            }
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
            Color healthyColor = behavior == InterferenceEnemyBehavior.FlankingAttach
                ? new Color(0.95f, 0.18f, 0.6f)
                : behavior == InterferenceEnemyBehavior.RangedPulse
                    ? new Color(0.15f, 0.7f, 1f)
                    : new Color(0.7f, 0.1f, 0.85f);
            Color stateColor = IsDefeated
                ? new Color(0.08f, 0.08f, 0.08f)
                : Color.Lerp(new Color(1f, 0.2f, 0.05f), healthyColor, ratio);
            if (IsTelegraphing)
            {
                stateColor = Color.Lerp(stateColor, new Color(1f, 0.75f, 0.05f), 0.75f);
            }

            if (IsDisruptionShieldActive)
            {
                stateColor = Color.Lerp(stateColor, new Color(0.25f, 0.45f, 1f), 0.82f);
            }

            if (IsSlowed)
            {
                stateColor = Color.Lerp(stateColor, new Color(0.4f, 0.8f, 1f), 0.65f);
            }

            if (IsStunned)
            {
                stateColor = Color.Lerp(stateColor, new Color(0.15f, 1f, 0.95f), 0.85f);
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
