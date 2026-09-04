using System.Linq;
using FunGame.Demo;
using FunGame.Incident;
using FunGame.Tools;
using UnityEngine;

namespace FunGame.Networking
{
    /// <summary>把同步状态适配到原有任务导航；不推进任何本地任务状态。</summary>
    public sealed class NetworkObjectiveGuidance : MonoBehaviour
    {
        private NetworkCampaignController _campaign;
        private NetworkCoolingIncidentController _incident;
        private PlayerToolbelt _belt;
        private NetworkPlayerCarryController _carry;
        private float _nextRefresh;
        public bool IsReady => _campaign != null && _campaign.IsSpawned;
        public DemoGuidanceInstruction Instruction { get; private set; }
        public Transform PrimaryTarget { get; private set; }
        public Transform SecondaryTarget { get; private set; }
        public string ProgressText { get; private set; }

        public void Refresh()
        {
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + 0.1f;
            if (_campaign == null) _campaign = FindFirstObjectByType<NetworkCampaignController>();
            if (_incident == null) _incident = FindFirstObjectByType<NetworkCoolingIncidentController>();
            if (_belt == null) _belt = GetComponent<PlayerToolbelt>();
            if (_carry == null) _carry = GetComponent<NetworkPlayerCarryController>();
            if (!IsReady || _incident == null || _belt == null) return;

            ToolKind tool = _belt.EquippedTool;
            if (_campaign.Chapter == NetworkCampaignChapter.CoolingRepair)
                Instruction = DemoGuidanceRules.ResolveCooling(_incident.RunState, _incident.Phase,
                    _incident.HasInspectedPressure, _incident.HasInspectedPump, tool,
                    _carry != null && _carry.HasHeldItem, _campaign.EnemiesRemaining > 0);
            else if (_campaign.Chapter == NetworkCampaignChapter.RelaySurge)
                Instruction = DemoGuidanceRules.ResolveRelay(_campaign.IsCurrentChapterFailed,
                    Enumerable.Range(0, 5).Count(_campaign.CanOperateRelay), _campaign.EnemiesRemaining, tool);
            else
                Instruction = DemoGuidanceRules.ResolveStorm(_campaign.IsCurrentChapterFailed, _campaign.CanConfirmStormWave,
                    _campaign.EnemiesRemaining, tool, _campaign.Chapter == NetworkCampaignChapter.Completed);

            if (_campaign.EnemiesRemaining > 0)
            {
                bool shield = FindObjectsByType<NetworkCombatEnemy>(FindObjectsSortMode.None).Any(e => e.IsShielded);
                if (Instruction.PrimaryTarget == DemoGuidanceTargetKind.Enemy ||
                    Instruction.PrimaryTarget == DemoGuidanceTargetKind.ImpactWrenchRack)
                    Instruction = new DemoGuidanceInstruction(
                        tool == ToolKind.None ? DemoGuidanceTargetKind.ImpactWrenchRack : DemoGuidanceTargetKind.Enemy,
                        shield ? "蓝色精英护盾需桥接器破除并瘫痪；喷枪范围清理虫群，扳手负责单体重击。"
                               : "清除干扰体：扳手重击单体，喷枪范围清群，桥接器瘫痪敌人。",
                        shield && tool != ToolKind.CircuitBridger ? DemoGuidanceTargetKind.CircuitBridgerRack : DemoGuidanceTargetKind.None);
            }
            PrimaryTarget = Resolve(Instruction.PrimaryTarget);
            SecondaryTarget = Resolve(Instruction.SecondaryTarget);
            ProgressText = _campaign.Chapter == NetworkCampaignChapter.CoolingRepair
                ? $"冷却 {_incident.Temperature:0.0}°C · 桥接 {_incident.CircuitBridgeProgress}/3 · 密封 {_incident.SealProgress:P0}"
                : _campaign.CurrentObjective;
        }

        private Transform Resolve(DemoGuidanceTargetKind kind)
        {
            if (kind == DemoGuidanceTargetKind.None) return null;
            if (kind == DemoGuidanceTargetKind.Enemy)
                return Nearest(FindObjectsByType<NetworkCombatEnemy>(FindObjectsSortMode.None)
                    .Where(e => e.Health > 0).Select(e => e.transform));
            if (kind == DemoGuidanceTargetKind.Relay)
                return Nearest(FindObjectsByType<NetworkCampaignStation>(FindObjectsSortMode.None)
                    .Where(s => !s.IsCalibrationConsole && _campaign.CanOperateRelay(s.StationIndex)).Select(s => s.transform));
            if (kind == DemoGuidanceTargetKind.CampaignConsole)
                return Nearest(FindObjectsByType<NetworkCampaignStation>(FindObjectsSortMode.None)
                    .Where(s => s.IsCalibrationConsole && s.StationIndex == (_campaign.Chapter == NetworkCampaignChapter.RelaySurge ? 1 : 0)).Select(s => s.transform));
            if (kind == DemoGuidanceTargetKind.SecretPlate)
                return FindFirstObjectByType<DemoEasterEgg325Interactable>()?.transform;
            if (kind == DemoGuidanceTargetKind.ReplacementPipe)
            {
                if (_carry != null && _carry.HasHeldItem) return Station(NetworkIncidentAction.InstallPipe);
                return FindFirstObjectByType<NetworkCarryableItem>()?.transform;
            }
            ToolKind desired = kind == DemoGuidanceTargetKind.ImpactWrenchRack ? ToolKind.ImpactWrench
                : kind == DemoGuidanceTargetKind.SealantRack ? ToolKind.SealantGun
                : kind == DemoGuidanceTargetKind.CircuitBridgerRack ? ToolKind.CircuitBridger : ToolKind.None;
            if (desired != ToolKind.None)
                return Nearest(FindObjectsByType<NetworkToolRackInteractable>(FindObjectsSortMode.None)
                    .Where(r => r.OfferedTool == desired).Select(r => r.transform));
            return kind switch
            {
                DemoGuidanceTargetKind.PressureGauge => Station(NetworkIncidentAction.InspectPressure),
                DemoGuidanceTargetKind.PumpInspection => Station(NetworkIncidentAction.InspectPump),
                DemoGuidanceTargetKind.CircuitInterlock => Station(NetworkIncidentAction.BridgeCircuit),
                DemoGuidanceTargetKind.Leak => Station(NetworkIncidentAction.SealLeak),
                DemoGuidanceTargetKind.Fastener => Station(NetworkIncidentAction.OperateFastener),
                DemoGuidanceTargetKind.CoolingConsole => Station(NetworkIncidentAction.OperatePump),
                _ => null
            };
        }

        private Transform Station(NetworkIncidentAction action) => Nearest(
            FindObjectsByType<NetworkIncidentStation>(FindObjectsSortMode.None)
                .Where(s => s.Action == action).Select(s => s.transform));

        private Transform Nearest(System.Collections.Generic.IEnumerable<Transform> candidates) =>
            candidates.OrderBy(t => (t.position - transform.position).sqrMagnitude).FirstOrDefault();
    }
}
