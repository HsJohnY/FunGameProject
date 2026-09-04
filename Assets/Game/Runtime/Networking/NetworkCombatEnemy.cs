using FunGame.Tools;
using Unity.Netcode;
using UnityEngine;

namespace FunGame.Networking
{
    /// <summary>轻量主机权威干扰体；位置、生命和攻击结果由服务器统一决定。</summary>
    [RequireComponent(typeof(NetworkObject), typeof(Collider), typeof(Renderer))]
    public sealed class NetworkCombatEnemy : NetworkBehaviour, IToolTarget
    {
        private readonly NetworkVariable<int> health = new NetworkVariable<int>();
        private readonly NetworkVariable<int> maxHealth = new NetworkVariable<int>();
        private readonly NetworkVariable<bool> shielded = new NetworkVariable<bool>();
        private Vector3 _target;
        private float _speed;
        private float _nextAttack;
        private NetworkCampaignController _campaign;

        public int Health => health.Value;
        public bool IsShielded => shielded.Value;

        public void InitializeServer(NetworkCampaignController campaign, Vector3 target, int configuredHealth,
            float speed, bool hasShield)
        {
            _campaign = campaign;
            _target = target;
            _speed = speed;
            maxHealth.Value = configuredHealth;
            health.Value = configuredHealth;
            shielded.Value = hasShield;
        }

        public override void OnNetworkSpawn()
        {
            health.OnValueChanged += Refresh;
            shielded.OnValueChanged += RefreshShield;
            Refresh(0, health.Value);
        }

        private void Update()
        {
            if (!IsServer || health.Value <= 0) return;
            Vector3 delta = _target - transform.position;
            delta.y = 0f;
            if (delta.magnitude > 1.4f)
            {
                transform.position += delta.normalized * (_speed * Time.deltaTime);
                transform.forward = delta.normalized;
                return;
            }
            if (Time.time >= _nextAttack)
            {
                _nextAttack = Time.time + 1.25f;
                _campaign?.ApplyCoreDamageServer(shielded.Value ? 14 : 9);
            }
        }

        public ToolActionOption GetToolAction(PlayerToolbelt toolbelt)
        {
            ToolKind tool = toolbelt != null ? toolbelt.EquippedTool : ToolKind.None;
            bool supported = tool == ToolKind.ImpactWrench || tool == ToolKind.SealantGun || tool == ToolKind.CircuitBridger;
            return new ToolActionOption("m4-network-enemy", shielded.Value ? "重甲干扰体" : "线路干扰体",
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
            if (!IsServer || health.Value <= 0) return;
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
                if (away.sqrMagnitude > 0.01f) transform.position += away.normalized * 0.8f;
            }
            if (health.Value == 0)
            {
                _campaign?.NotifyEnemyDefeatedServer();
                NetworkObject.Despawn(true);
            }
        }

        private void Refresh(int previous, int current)
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = current <= 0 ? Color.black : Color.Lerp(Color.red, Color.magenta,
                maxHealth.Value <= 0 ? 1f : (float)current / maxHealth.Value);
        }

        private void RefreshShield(bool previous, bool current)
        {
            if (current && TryGetComponent(out Renderer renderer)) renderer.material.color = new Color(0.2f, 0.45f, 1f);
        }
    }
}
