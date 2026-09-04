using System;
using System.Collections.Generic;
using System.Linq;
using FunGame.Combat;
using UnityEngine;

namespace FunGame.Demo
{
    /// <summary>通过战役的显式遭遇引用登记内容，避免在主场景维护另一份敌人总表。</summary>
    public sealed class ExpeditionContext : MonoBehaviour
    {
        [SerializeField] private SinglePlayerDemoController campaign;
        [SerializeField] private CoolingCombatIntegrationController coolingCombat;
        private readonly EnemyTemplateRegistry _registry = new EnemyTemplateRegistry();
        public static ExpeditionContext Current { get; private set; }
        public SinglePlayerDemoController Campaign => campaign;
        public CoolingCombatIntegrationController CoolingCombat => coolingCombat;
        public IReadOnlyList<InterferenceEnemy> Enemies => ConfiguredEncounters()
            .SelectMany(encounter => encounter.Enemies).Distinct().ToArray();
        public void ConfigureCampaign(SinglePlayerDemoController value, CoolingCombatIntegrationController cooling)
        { campaign = value; coolingCombat = cooling; }
        public void SetEnvironmentVisibility(bool relay, bool storm) => GetComponent<ExpeditionEnvironmentLoader>()?.SetVisibility(relay, storm);

        private IEnumerable<CombatEncounterController> ConfiguredEncounters()
        {
            if (campaign == null || coolingCombat == null) throw new InvalidOperationException("Expedition campaign bindings are missing.");
            yield return coolingCombat.Encounter;
            yield return campaign.RelayDefenseEncounter;
            foreach (CombatEncounterController encounter in campaign.StormEncounters) yield return encounter;
        }
        public void Register()
        {
            if (Current != null && Current != this) throw new InvalidOperationException("More than one expedition context is active.");
            _registry.Register(Enemies);
            Current = this;
        }
        public InterferenceEnemy ResolveEnemy(string id) => _registry.Resolve(id);
        private void OnDestroy() { if (Current == this) Current = null; }
    }

    /// <summary>登记与查找使用稳定 ID。验证失败时保持原有完整登记表。</summary>
    public sealed class EnemyTemplateRegistry
    {
        private Dictionary<string, InterferenceEnemy> _byId = new Dictionary<string, InterferenceEnemy>(StringComparer.Ordinal);
        public void Register(IEnumerable<InterferenceEnemy> enemies)
        {
            var next = new Dictionary<string, InterferenceEnemy>(StringComparer.Ordinal);
            foreach (InterferenceEnemy enemy in enemies)
                if (enemy == null || string.IsNullOrWhiteSpace(enemy.TargetId) || !next.TryAdd(enemy.TargetId, enemy))
                    throw new InvalidOperationException("Missing or duplicate expedition enemy ID.");
            _byId = next;
        }
        public InterferenceEnemy Resolve(string id)
        {
            if (!_byId.TryGetValue(id, out InterferenceEnemy enemy)) throw new InvalidOperationException("Unknown expedition enemy ID: " + id);
            return enemy;
        }
    }
}
