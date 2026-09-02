using FunGame.Tools;
using UnityEngine;

namespace FunGame.Combat
{
    /// <summary>
    /// 首个基础干扰敌人：沿直线接近设备、周期性干扰，并接受冲击扳手的离散击退。
    /// 障碍导航和复杂行为树明确留到关卡切片证明需要时再加入。
    /// </summary>
    [RequireComponent(typeof(CapsuleCollider), typeof(MeshRenderer), typeof(Rigidbody))]
    public sealed class InterferenceEnemy : MonoBehaviour, IToolTarget
    {
        private const float CollisionSkin = 0.02f;

        [SerializeField] private string targetId = "interference-creature";
        [SerializeField] private string targetName = "线路干扰体";
        [SerializeField] private DefendableSystemTarget defenseTarget;
        [SerializeField] private CombatEncounterController encounter;
        [SerializeField, Min(1)] private int maxHealth = 3;
        [SerializeField, Min(0.1f)] private float moveSpeed = 1.35f;
        [SerializeField, Min(0.1f)] private float attackRange = 1.3f;
        [SerializeField, Min(0.05f)] private float attackIntervalSeconds = 1.25f;
        [SerializeField, Min(1)] private int interferenceDamage = 10;
        [SerializeField, Min(1)] private int wrenchDamage = 1;
        [SerializeField, Min(0f)] private float knockbackDistance = 1.25f;

        private InterferenceEnemyRules _rules;
        private Collider _collider;
        private Rigidbody _rigidbody;
        private Renderer _renderer;
        private MaterialPropertyBlock _propertyBlock;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private bool _encounterActive = true;

        public int Health => _rules?.Health ?? maxHealth;
        public int MaxHealth => _rules?.MaxHealth ?? maxHealth;
        public bool IsDefeated => _rules != null && _rules.IsDefeated;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            EnsurePhysicsBody();
            _renderer = GetComponent<Renderer>();
            _propertyBlock = new MaterialPropertyBlock();
            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
            CreateRules();
            RefreshVisual();
        }

        private void Update()
        {
            if (!_encounterActive || IsDefeated || defenseTarget == null || defenseTarget.IsOffline)
            {
                return;
            }

            Vector3 toTarget = defenseTarget.transform.position - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            bool targetInRange = distance <= attackRange;

            if (!targetInRange && distance > 0.001f)
            {
                Vector3 direction = toTarget / distance;
                MoveWithCollision(direction * (moveSpeed * Time.deltaTime));
                transform.forward = direction;
            }

            if (_rules.Advance(Time.deltaTime, targetInRange))
            {
                bool targetOffline = defenseTarget.ApplyInterference(interferenceDamage);
                if (targetOffline)
                {
                    encounter?.NotifySystemOffline();
                }
            }
        }

        /// <summary>
        /// 由独立战斗灰盒生成器配置单一目标和最少必要数值。
        /// </summary>
        public void Configure(
            DefendableSystemTarget configuredTarget,
            CombatEncounterController configuredEncounter,
            int configuredMaxHealth = 3,
            float configuredMoveSpeed = 1.35f,
            float configuredAttackRange = 1.3f,
            float configuredAttackIntervalSeconds = 1.25f,
            int configuredInterferenceDamage = 10,
            int configuredWrenchDamage = 1,
            float configuredKnockbackDistance = 1.25f)
        {
            defenseTarget = configuredTarget;
            encounter = configuredEncounter;
            maxHealth = Mathf.Max(1, configuredMaxHealth);
            moveSpeed = Mathf.Max(0.1f, configuredMoveSpeed);
            attackRange = Mathf.Max(0.1f, configuredAttackRange);
            attackIntervalSeconds = Mathf.Max(0.05f, configuredAttackIntervalSeconds);
            interferenceDamage = Mathf.Max(1, configuredInterferenceDamage);
            wrenchDamage = Mathf.Max(1, configuredWrenchDamage);
            knockbackDistance = Mathf.Max(0f, configuredKnockbackDistance);
            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
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
                IsDefeated ? "干扰体已失去活动能力" : "防卫遭遇已结束");
        }

        public bool ApplyTool(PlayerToolbelt toolbelt)
        {
            ToolActionOption option = GetToolAction(toolbelt);
            if (!option.IsAvailable)
            {
                return false;
            }

            bool defeated = _rules.ReceiveHit(wrenchDamage);
            ApplyKnockback(toolbelt.transform.position);
            RefreshVisual();
            Debug.Log($"[Combat] target={targetId} action=wrench-hit health={Health}/{MaxHealth} defeated={defeated}", this);

            if (defeated)
            {
                SetEncounterActive(false);
                encounter?.NotifyEnemyDefeated();
            }

            return true;
        }

        public void ResetEnemy()
        {
            if (_rules == null)
            {
                CreateRules();
            }
            else
            {
                _rules.Reset();
            }

            EnsurePhysicsBody();
            // 结束态会关闭碰撞体；先保持关闭完成传送，再同步 Transform 与物理世界后重新激活。
            SetEncounterActive(false);
            _rigidbody.position = _spawnPosition;
            _rigidbody.rotation = _spawnRotation;
            transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);
            Physics.SyncTransforms();
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
                MoveWithCollision(away.normalized * knockbackDistance);
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

        /// <summary>
        /// 直接修改 Transform 会绕过物理阻挡；所有主动移动和击退都先扫掠实体碰撞体。
        /// </summary>
        private void MoveWithCollision(Vector3 displacement)
        {
            float requestedDistance = displacement.magnitude;
            if (requestedDistance <= 0.0001f)
            {
                return;
            }

            EnsurePhysicsBody();
            Vector3 direction = displacement / requestedDistance;
            float allowedDistance = requestedDistance;
            if (_rigidbody.SweepTest(
                    direction,
                    out RaycastHit hit,
                    requestedDistance + CollisionSkin,
                    QueryTriggerInteraction.Ignore))
            {
                allowedDistance = Mathf.Max(0f, hit.distance - CollisionSkin);
            }

            _rigidbody.position += direction * allowedDistance;
        }

        private void CreateRules()
        {
            _rules = new InterferenceEnemyRules(maxHealth, attackIntervalSeconds);
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
            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor("_BaseColor", IsDefeated
                ? new Color(0.08f, 0.08f, 0.08f)
                : Color.Lerp(new Color(1f, 0.2f, 0.05f), new Color(0.7f, 0.1f, 0.85f), ratio));
            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
