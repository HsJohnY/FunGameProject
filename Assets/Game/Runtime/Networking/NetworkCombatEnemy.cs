using FunGame.Tools;
using FunGame.Combat;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace FunGame.Networking
{
    public enum NetworkEnemyKind { Swarm, Flanker, ShieldElite, Ranged, Direct }
    /// <summary>轻量主机权威干扰体；位置、生命和攻击结果由服务器统一决定。</summary>
    [RequireComponent(typeof(NetworkObject), typeof(Collider), typeof(Renderer))]
    public sealed class NetworkCombatEnemy : NetworkBehaviour, IToolTarget
    {
        private readonly NetworkVariable<int> health = new NetworkVariable<int>();
        private readonly NetworkVariable<int> maxHealth = new NetworkVariable<int>();
        private readonly NetworkVariable<bool> shielded = new NetworkVariable<bool>();
        private readonly NetworkVariable<NetworkEnemyKind> kind = new NetworkVariable<NetworkEnemyKind>();
        private readonly NetworkVariable<double> slowedUntil = new NetworkVariable<double>();
        private readonly NetworkVariable<double> stunnedUntil = new NetworkVariable<double>();
        private readonly NetworkVariable<int> templateIndex = new NetworkVariable<int>(-1);
        private readonly NetworkVariable<bool> telegraphing = new NetworkVariable<bool>();
        private InterferenceEnemy _template;
        private InterferenceEnemyRules _attackRules;
        private bool _reachedWaypoint;
        private LineRenderer _link;
        private float _nextSealantPulse;
        private Vector3 _target;
        private float _speed;
        private float _nextAttack;
        private NetworkCampaignController _campaign;

        public int Health => health.Value;
        public bool IsShielded => shielded.Value && !IsStunned;
        public NetworkEnemyKind Kind => kind.Value;
        public bool IsSlowed => IsSpawned && NetworkManager.ServerTime.Time < slowedUntil.Value;
        public bool IsStunned => IsSpawned && NetworkManager.ServerTime.Time < stunnedUntil.Value;
        public InterferenceEnemy Template => _template;
        public bool IsTelegraphing => telegraphing.Value;

        public static InterferenceEnemy[] SceneTemplates() => FindObjectsByType<InterferenceEnemy>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .OrderBy(e => e.TargetId, System.StringComparer.Ordinal).ToArray();

        public void InitializeFromMapServer(NetworkCampaignController campaign, InterferenceEnemy source)
        {
            NetworkEnemyKind enemyKind = source.RequiresCircuitDisruption ? NetworkEnemyKind.ShieldElite
                : source.Behavior == InterferenceEnemyBehavior.FlankingAttach ? NetworkEnemyKind.Flanker
                : source.Behavior == InterferenceEnemyBehavior.RangedPulse ? NetworkEnemyKind.Ranged
                : source.MaxHealth <= 1 ? NetworkEnemyKind.Swarm : NetworkEnemyKind.Direct;
            InitializeServer(campaign, source.DefenseTarget.transform.position, source.MaxHealth,
                source.MoveSpeed, source.RequiresCircuitDisruption, enemyKind);
            templateIndex.Value = System.Array.IndexOf(SceneTemplates(), source);
        }

        public void InitializeServer(NetworkCampaignController campaign, Vector3 target, int configuredHealth,
            float speed, bool hasShield, NetworkEnemyKind enemyKind = NetworkEnemyKind.Swarm)
        {
            _campaign = campaign;
            _target = target;
            _speed = speed;
            maxHealth.Value = configuredHealth;
            health.Value = configuredHealth;
            shielded.Value = hasShield;
            kind.Value = hasShield ? NetworkEnemyKind.ShieldElite : enemyKind;
            RefreshVisual();
        }

        public override void OnNetworkSpawn()
        {
            health.OnValueChanged += Refresh;
            shielded.OnValueChanged += RefreshShield;
            kind.OnValueChanged += RefreshKind;
            templateIndex.OnValueChanged += RefreshTemplate;
            RefreshTemplate(-1, templateIndex.Value);
            Refresh(0, health.Value);
        }

        public override void OnNetworkDespawn()
        {
            health.OnValueChanged -= Refresh;
            shielded.OnValueChanged -= RefreshShield;
            kind.OnValueChanged -= RefreshKind;
            templateIndex.OnValueChanged -= RefreshTemplate;
        }

        private void Update()
        {
            if (!IsServer || health.Value <= 0) return;
            if (_template != null)
            {
                UpdateAuthoredCombat();
                return;
            }
            if (IsStunned) return;
            Vector3 destination = _target;
            if (kind.Value == NetworkEnemyKind.Flanker && transform.position.z > _target.z + 2f)
                destination += new Vector3(transform.position.x < 0f ? -3f : 3f, 0f, 0f);
            Vector3 delta = destination - transform.position;
            delta.y = 0f;
            if (delta.magnitude > (kind.Value == NetworkEnemyKind.Ranged ? 4.5f : 1.4f))
            {
                MoveWithCollision(delta.normalized * (_speed * (IsSlowed ? 0.35f : 1f) * Time.deltaTime));
                transform.forward = delta.normalized;
                return;
            }
            if (Time.time >= _nextAttack)
            {
                _nextAttack = Time.time + 1.25f;
                _campaign?.ApplyCoreDamageServer(kind.Value == NetworkEnemyKind.ShieldElite ? 14 : 9);
            }
        }

        private void UpdateAuthoredCombat()
        {
            if (IsStunned)
            {
                _attackRules.Advance(0f, false);
                telegraphing.Value = false;
                return;
            }
            Vector3 destination = _template.GetApproachDestination(transform.position, ref _reachedWaypoint);
            Vector3 delta = destination - transform.position;
            delta.y = 0f;
            bool ready = _reachedWaypoint && _template.IsAtCombatPosition(transform.position);
            if (!ready && delta.sqrMagnitude > 0.0001f)
            {
                MoveWithCollision(delta.normalized * Mathf.Min(delta.magnitude, _speed * (IsSlowed ? 0.35f : 1f) * Time.deltaTime));
                transform.forward = delta.normalized;
            }
            InterferenceEnemyAction action = _attackRules.Advance(Time.deltaTime, ready);
            telegraphing.Value = _attackRules.IsTelegraphing;
            if (action == InterferenceEnemyAction.AttackCommitted)
                _campaign?.ApplyCoreDamageServer(_template.InterferenceDamage);
        }

        private void RefreshTemplate(int previous, int current)
        {
            if (current < 0) return;
            InterferenceEnemy[] templates = SceneTemplates();
            if (current >= templates.Length) return;
            _template = templates[current];
            _attackRules = new InterferenceEnemyRules(_template.MaxHealth, _template.AttackInterval, _template.AttackWindup);
            _target = _template.DefenseTarget.transform.position;
            foreach (Transform child in transform) child.gameObject.SetActive(false);
            GetComponent<Renderer>().sharedMaterials = _template.GetComponent<Renderer>().sharedMaterials;
            foreach (Transform child in _template.transform)
            {
                GameObject part = Instantiate(child.gameObject, transform, false);
                part.SetActive(true);
                foreach (Renderer renderer in part.GetComponentsInChildren<Renderer>(true)) renderer.enabled = true;
                foreach (Collider collider in part.GetComponentsInChildren<Collider>(true)) collider.enabled = true;
            }
            _link = GetComponent<LineRenderer>();
            if (_link == null) _link = gameObject.AddComponent<LineRenderer>();
            _link.sharedMaterial = _template.GetComponent<LineRenderer>().sharedMaterial;
            _link.positionCount = 3;
            _link.useWorldSpace = true;
            _link.startWidth = 0.045f;
            _link.endWidth = 0.02f;
            _link.enabled = false;
            RefreshVisual();
        }

        private void LateUpdate()
        {
            if (_template == null || _link == null) return;
            RefreshVisual();
            _link.enabled = health.Value > 0 && telegraphing.Value;
            if (_link.enabled)
            {
                Vector3 start = transform.position + Vector3.up * 0.35f;
                Vector3 end = _target + Vector3.up * 0.4f;
                _link.SetPosition(0, start);
                _link.SetPosition(1, Vector3.Lerp(start, end, 0.5f) + Vector3.up * 0.13f);
                _link.SetPosition(2, end);
            }
        }

        public ToolActionOption GetToolAction(PlayerToolbelt toolbelt)
        {
            ToolKind tool = toolbelt != null ? toolbelt.EquippedTool : ToolKind.None;
            bool supported = tool == ToolKind.ImpactWrench || tool == ToolKind.SealantGun || tool == ToolKind.CircuitBridger;
            return new ToolActionOption("m4-network-enemy", _template != null ? _template.DisplayName : IsShielded ? "护盾精英" : "线路干扰体",
                IsShielded ? "破盾 / 攻击" : "攻击", tool, tool, supported && health.Value > 0,
                supported ? "目标已被清除" : "需要装备任意核心工具");
        }

        public bool ApplyTool(PlayerToolbelt toolbelt)
        {
            NetworkPlayerCampaignAgent agent = toolbelt != null ? toolbelt.GetComponent<NetworkPlayerCampaignAgent>() : null;
            return agent != null && agent.RequestEnemyHit(NetworkObject);
        }

        public void ApplyToolServer(ToolKind tool, Vector3 source)
        {
            if (!IsServer || health.Value <= 0 || !NetworkPlayerToolbelt.IsSupportedTool(tool)) return;
            if (tool == ToolKind.CircuitBridger)
            {
                stunnedUntil.Value = NetworkManager.ServerTime.Time + 1.4;
                _nextAttack = Time.time + 1.4f;
            }
            if (tool == ToolKind.SealantGun)
            {
                ApplySealantPulse(source);
                foreach (NetworkCombatEnemy nearby in FindObjectsByType<NetworkCombatEnemy>(FindObjectsSortMode.None))
                    if (nearby != this && nearby.IsSpawned && Vector3.Distance(transform.position, nearby.transform.position) <= 1.65f)
                        nearby.ApplySealantPulse(source);
                return;
            }
            if (IsShielded)
            {
                return;
            }
            int damage = tool == ToolKind.ImpactWrench ? (_template != null ? _template.WrenchDamage : 2) : 1;
            health.Value = Mathf.Max(0, health.Value - damage);
            if (tool == ToolKind.ImpactWrench)
            {
                Vector3 away = transform.position - source;
                away.y = 0f;
                if (away.sqrMagnitude > 0.01f) MoveWithCollision(away.normalized * (_template != null ? _template.KnockbackDistance : 0.8f));
            }
            if (health.Value == 0)
            {
                _campaign?.NotifyEnemyDefeatedServer();
                NetworkObject.Despawn(true);
            }
        }

        private void Refresh(int previous, int current)
        {
            RefreshVisual();
        }

        private void RefreshShield(bool previous, bool current)
        {
            RefreshVisual();
        }

        private void RefreshKind(NetworkEnemyKind previous, NetworkEnemyKind current) => RefreshVisual();

        private void ApplySealantPulse(Vector3 source)
        {
            if (!IsServer || health.Value <= 0 || Time.time < _nextSealantPulse) return;
            _nextSealantPulse = Time.time + 0.15f;
            slowedUntil.Value = NetworkManager.ServerTime.Time + 2.25;
            Vector3 away = transform.position - source;
            away.y = 0f;
            MoveWithCollision(away.normalized * 0.18f);
            if (IsShielded) return;
            health.Value = Mathf.Max(0, health.Value - 1);
            if (health.Value == 0)
            {
                _campaign?.NotifyEnemyDefeatedServer();
                NetworkObject.Despawn(true);
            }
        }

        private void MoveWithCollision(Vector3 displacement)
        {
            float distance = displacement.magnitude;
            if (distance < 0.0001f) return;
            float allowed = distance;
            float radius = GetComponent<CapsuleCollider>().radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
            foreach (RaycastHit hit in Physics.SphereCastAll(transform.position, radius, displacement.normalized,
                         distance, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.IsChildOf(transform) || hit.collider.GetComponentInParent<NetworkCombatEnemy>() != null) continue;
                allowed = Mathf.Min(allowed, Mathf.Max(0f, hit.distance - 0.03f));
            }
            transform.position += displacement.normalized * allowed;
        }

        private void RefreshVisual()
        {
            if (_template != null)
            {
                float healthRatio = (float)health.Value / Mathf.Max(1, maxHealth.Value);
                float pulse = telegraphing.Value ? 1f + Mathf.Sin(Time.unscaledTime * 30f) * 0.12f : 1f;
                transform.localScale = Vector3.Scale(_template.AuthoredScale,
                    new Vector3(pulse, 1f, pulse) * Mathf.Lerp(0.72f, 1f, healthRatio));
                Color tint = kind.Value == NetworkEnemyKind.Flanker ? new Color(0.95f, 0.18f, 0.6f)
                    : kind.Value == NetworkEnemyKind.Ranged ? new Color(0.15f, 0.7f, 1f) : new Color(0.7f, 0.1f, 0.85f);
                if (IsShielded) tint = new Color(0.25f, 0.45f, 1f);
                if (IsSlowed) tint = Color.Lerp(tint, new Color(0.4f, 0.8f, 1f), 0.65f);
                if (IsStunned) tint = new Color(0.15f, 1f, 0.95f);
                if (telegraphing.Value) tint = Color.Lerp(tint, new Color(1f, 0.75f, 0.05f), 0.75f);
                var state = new MaterialPropertyBlock();
                state.SetColor("_BaseColor", tint);
                GetComponent<Renderer>().SetPropertyBlock(state);
                return;
            }
            transform.localScale = kind.Value == NetworkEnemyKind.ShieldElite ? new Vector3(1.3f, 1.1f, 1.3f)
                : kind.Value == NetworkEnemyKind.Flanker ? new Vector3(0.95f, 0.45f, 1.1f)
                : kind.Value == NetworkEnemyKind.Ranged ? new Vector3(0.7f, 0.9f, 0.7f) : new Vector3(0.45f, 0.4f, 0.55f);
            var block = new MaterialPropertyBlock();
            Color color = shielded.Value ? new Color(0.2f, 0.45f, 1f)
                : kind.Value == NetworkEnemyKind.Flanker ? new Color(0.95f, 0.18f, 0.6f)
                : kind.Value == NetworkEnemyKind.Ranged ? Color.cyan : new Color(0.7f, 0.1f, 0.85f);
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            foreach (Renderer part in GetComponentsInChildren<Renderer>(true))
            {
                part.enabled = health.Value > 0 && (part.gameObject == gameObject ||
                    (part.name == "Shield Armor" ? shielded.Value : kind.Value == NetworkEnemyKind.Flanker));
                part.SetPropertyBlock(block);
            }
        }
    }
}
