using FunGame.Tools;
using Unity.Netcode;
using UnityEngine;

namespace FunGame.Networking
{
    public enum NetworkEnemyKind { Swarm, Flanker, ShieldElite, Ranged }
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
        private float _nextSealantPulse;
        private Vector3 _target;
        private float _speed;
        private float _nextAttack;
        private NetworkCampaignController _campaign;

        public int Health => health.Value;
        public bool IsShielded => shielded.Value;
        public NetworkEnemyKind Kind => kind.Value;
        public bool IsSlowed => IsSpawned && NetworkManager.ServerTime.Time < slowedUntil.Value;
        public bool IsStunned => IsSpawned && NetworkManager.ServerTime.Time < stunnedUntil.Value;

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
            Refresh(0, health.Value);
        }

        public override void OnNetworkDespawn()
        {
            health.OnValueChanged -= Refresh;
            shielded.OnValueChanged -= RefreshShield;
            kind.OnValueChanged -= RefreshKind;
        }

        private void Update()
        {
            if (!IsServer || health.Value <= 0 || IsStunned) return;
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

        public ToolActionOption GetToolAction(PlayerToolbelt toolbelt)
        {
            ToolKind tool = toolbelt != null ? toolbelt.EquippedTool : ToolKind.None;
            bool supported = tool == ToolKind.ImpactWrench || tool == ToolKind.SealantGun || tool == ToolKind.CircuitBridger;
            return new ToolActionOption("m4-network-enemy", shielded.Value ? "护盾精英" : kind.Value == NetworkEnemyKind.Swarm ? "虫群干扰体" : "线路干扰体",
                shielded.Value ? "破盾 / 攻击" : "攻击", tool, tool, supported && health.Value > 0,
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
                stunnedUntil.Value = NetworkManager.ServerTime.Time + 1.6;
                _nextAttack = Time.time + 1.6f;
            }
            if (tool == ToolKind.SealantGun)
            {
                ApplySealantPulse(source);
                foreach (NetworkCombatEnemy nearby in FindObjectsByType<NetworkCombatEnemy>(FindObjectsSortMode.None))
                    if (nearby != this && nearby.IsSpawned && Vector3.Distance(transform.position, nearby.transform.position) <= 1.65f)
                        nearby.ApplySealantPulse(source);
                return;
            }
            if (shielded.Value)
            {
                if (tool != ToolKind.CircuitBridger) return;
                shielded.Value = false;
                return;
            }
            int damage = tool == ToolKind.ImpactWrench ? 2 : 1;
            health.Value = Mathf.Max(0, health.Value - damage);
            if (tool == ToolKind.ImpactWrench)
            {
                Vector3 away = transform.position - source;
                away.y = 0f;
                if (away.sqrMagnitude > 0.01f) MoveWithCollision(away.normalized * 0.8f);
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
            if (shielded.Value) return;
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
            foreach (RaycastHit hit in Physics.SphereCastAll(transform.position, 0.22f, displacement.normalized,
                         distance, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.IsChildOf(transform) || hit.collider.GetComponentInParent<NetworkCombatEnemy>() != null) continue;
                allowed = Mathf.Min(allowed, Mathf.Max(0f, hit.distance - 0.03f));
            }
            transform.position += displacement.normalized * allowed;
        }

        private void RefreshVisual()
        {
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
