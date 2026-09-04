using System.Collections.Generic;
using FunGame.Combat;
using FunGame.Networking;
using FunGame.Incident;
using FunGame.Interaction;
using FunGame.Tools;
using FunGame.UI;
using UnityEngine;

namespace FunGame.Demo
{
    /// <summary>
    /// 把演示状态转换为始终可见的操作句，并为当前目标提供屏幕内/屏幕边缘标记。
    /// </summary>
    public sealed class DemoObjectiveGuidancePresenter : MonoBehaviour
    {
        [SerializeField] private SinglePlayerDemoController campaign;
        [SerializeField] private CoolingIncidentController incident;
        [SerializeField] private ContextInteractor interactor;
        [SerializeField] private CoolingCombatIntegrationController coolingCombat;
        [SerializeField] private NetworkObjectiveGuidance networkSource;
        private readonly Dictionary<DemoGuidanceTargetKind, Transform> _fixedTargets =
            new Dictionary<DemoGuidanceTargetKind, Transform>();
        private DemoRelayTarget[] _relays;
        private ToolRackInteractable[] _toolRacks;
        private Camera _viewCamera;
        private Texture2D _panelTexture;
        private GUIStyle _panelStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _instructionStyle;
        private GUIStyle _progressStyle;
        private GUIStyle _controlsStyle;
        private GUIStyle _primaryMarkerStyle;
        private GUIStyle _secondaryMarkerStyle;
        private float _primaryBehindSide = 1f;
        private float _secondaryBehindSide = -1f;
        private string _lastProgressSignature;
        private float _lastProgressAt;

        public DemoGuidanceInstruction CurrentInstruction { get; private set; }
        public Transform CurrentTarget { get; private set; }
        public Transform SecondaryTarget { get; private set; }

        public void Configure(
            SinglePlayerDemoController configuredCampaign,
            CoolingIncidentController configuredIncident,
            ContextInteractor configuredInteractor,
            CoolingCombatIntegrationController configuredCoolingCombat)
        {
            campaign = configuredCampaign;
            incident = configuredIncident;
            interactor = configuredInteractor;
            coolingCombat = configuredCoolingCombat;
        }

        public void ConfigureNetwork(NetworkObjectiveGuidance source, ContextInteractor actor)
        {
            networkSource = source;
            interactor = actor;
        }

        private void Start()
        {
            if (interactor == null)
            {
                interactor = FindFirstObjectByType<ContextInteractor>();
            }

            _viewCamera = interactor != null ? interactor.GetComponentInChildren<Camera>(true) : Camera.main;
            CacheTargets();
            RefreshGuidance();
        }

        private void Update()
        {
            RefreshGuidance();
        }

        private void OnDestroy()
        {
            if (_panelTexture != null)
            {
                Destroy(_panelTexture);
            }
        }

        private void CacheTargets()
        {
            _fixedTargets.Clear();
            foreach (CoolingDiagnosticInteractable diagnostic in
                     FindObjectsByType<CoolingDiagnosticInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                _fixedTargets[diagnostic.Kind == CoolingDiagnosticInteractable.DiagnosticKind.PressureGauge
                    ? DemoGuidanceTargetKind.PressureGauge
                    : DemoGuidanceTargetKind.PumpInspection] = diagnostic.transform;
            }

            CacheFirst<CircuitBridgeTarget>(DemoGuidanceTargetKind.CircuitInterlock);
            CacheFirst<SealantTarget>(DemoGuidanceTargetKind.Leak);
            CacheFirst<MechanicalFastenerTarget>(DemoGuidanceTargetKind.Fastener);
            CacheFirst<ToggleConsoleInteractable>(DemoGuidanceTargetKind.CoolingConsole);
            CacheFirst<DemoEasterEgg325Interactable>(DemoGuidanceTargetKind.SecretPlate);

            foreach (CarryableInteractable carryable in
                     FindObjectsByType<CarryableInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (carryable.TargetId == "replacement-pipe")
                {
                    _fixedTargets[DemoGuidanceTargetKind.ReplacementPipe] = carryable.transform;
                    break;
                }
            }

            _toolRacks = FindObjectsByType<ToolRackInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            _relays = FindObjectsByType<DemoRelayTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        private void CacheFirst<T>(DemoGuidanceTargetKind kind) where T : Component
        {
            T target = FindFirstObjectByType<T>(FindObjectsInactive.Include);
            if (target != null)
            {
                _fixedTargets[kind] = target.transform;
            }
        }

        private void RefreshGuidance()
        {
            if (networkSource != null)
            {
                networkSource.Refresh();
                CurrentInstruction = networkSource.Instruction;
                CurrentTarget = networkSource.PrimaryTarget;
                SecondaryTarget = networkSource.SecondaryTarget;
                return;
            }
            if (campaign == null || incident == null)
            {
                return;
            }

            ToolKind equipped = interactor != null && interactor.Toolbelt != null
                ? interactor.Toolbelt.EquippedTool
                : ToolKind.None;
            switch (campaign.Chapter)
            {
                case SinglePlayerDemoChapter.CoolingEmergency:
                    CurrentInstruction = DemoGuidanceRules.ResolveCooling(
                        incident.RunState,
                        incident.Phase,
                        incident.HasInspectedPressure,
                        incident.HasInspectedPump,
                        equipped,
                        interactor != null && interactor.IsHoldingItem,
                        coolingCombat != null && coolingCombat.IsInterferenceActive);
                    break;
                case SinglePlayerDemoChapter.RelaySurge:
                    CurrentInstruction = DemoGuidanceRules.ResolveRelay(
                        campaign.IsCurrentChapterFailed,
                        Mathf.Max(0, campaign.RequiredRelayCount - campaign.StabilizedRelayCount),
                        campaign.RelayDefenseEncounter != null ? campaign.RelayDefenseEncounter.RemainingEnemyCount : 0,
                        equipped);
                    break;
                default:
                    CurrentInstruction = DemoGuidanceRules.ResolveStorm(
                        campaign.IsCurrentChapterFailed,
                        campaign.IsAwaitingCalibration,
                        campaign.CurrentStormEncounter != null ? campaign.CurrentStormEncounter.RemainingEnemyCount : 0,
                        equipped,
                        campaign.IsCompleted);
                    break;
            }

            CurrentTarget = ResolveTarget(CurrentInstruction.PrimaryTarget);
            SecondaryTarget = ResolveTarget(CurrentInstruction.SecondaryTarget);
            string progressSignature = CurrentInstruction.PrimaryTarget + ":" + GetProgressStatus(false);
            if (_lastProgressSignature != progressSignature)
            {
                _lastProgressSignature = progressSignature;
                _lastProgressAt = Time.unscaledTime;
            }
        }

        private Transform ResolveTarget(DemoGuidanceTargetKind kind)
        {
            if (kind == DemoGuidanceTargetKind.CampaignConsole)
            {
                return campaign != null && campaign.CurrentCampaignConsole != null
                    ? campaign.CurrentCampaignConsole.transform
                    : null;
            }

            if (kind == DemoGuidanceTargetKind.Relay)
            {
                return FindNearestRelay();
            }

            if (kind == DemoGuidanceTargetKind.Enemy)
            {
                return FindNearestEnemy();
            }

            if (kind == DemoGuidanceTargetKind.ImpactWrenchRack)
            {
                return FindNearestToolRack(ToolKind.ImpactWrench);
            }

            if (kind == DemoGuidanceTargetKind.SealantRack)
            {
                return FindNearestToolRack(ToolKind.SealantGun);
            }

            if (kind == DemoGuidanceTargetKind.CircuitBridgerRack)
            {
                return FindNearestToolRack(ToolKind.CircuitBridger);
            }

            return _fixedTargets.TryGetValue(kind, out Transform target) ? target : null;
        }

        private Transform FindNearestRelay()
        {
            Transform nearest = null;
            float nearestDistance = float.MaxValue;
            if (_relays == null)
            {
                return null;
            }

            foreach (DemoRelayTarget relay in _relays)
            {
                if (relay == null || relay.IsStabilized || !relay.gameObject.activeInHierarchy)
                {
                    continue;
                }

                float distance = DistanceToPlayer(relay.transform);
                if (distance < nearestDistance)
                {
                    nearest = relay.transform;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private Transform FindNearestToolRack(ToolKind tool)
        {
            Transform nearest = null;
            float nearestDistance = float.MaxValue;
            if (_toolRacks == null)
            {
                return null;
            }

            foreach (ToolRackInteractable rack in _toolRacks)
            {
                if (rack == null || rack.OfferedTool != tool || !rack.gameObject.activeInHierarchy)
                {
                    continue;
                }

                float distance = DistanceToPlayer(rack.transform);
                if (distance < nearestDistance)
                {
                    nearest = rack.transform;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private Transform FindNearestEnemy()
        {
            CombatEncounterController encounter = campaign.Chapter == SinglePlayerDemoChapter.CoolingEmergency
                ? coolingCombat != null ? coolingCombat.Encounter : null
                : campaign.Chapter == SinglePlayerDemoChapter.RelaySurge
                    ? campaign.RelayDefenseEncounter
                    : campaign.CurrentStormEncounter;
            Transform nearest = null;
            float nearestDistance = float.MaxValue;
            if (encounter == null)
            {
                return null;
            }

            foreach (InterferenceEnemy enemy in encounter.Enemies)
            {
                if (enemy == null || enemy.IsDefeated || !enemy.IsDeployed || !enemy.gameObject.activeInHierarchy)
                {
                    continue;
                }

                float distance = DistanceToPlayer(enemy.transform);
                if (distance < nearestDistance)
                {
                    nearest = enemy.transform;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private float DistanceToPlayer(Transform target)
        {
            return interactor != null ? Vector3.Distance(interactor.transform.position, target.position) : 0f;
        }

        private void OnGUI()
        {
            if ((networkSource != null ? !networkSource.IsReady : campaign == null) || GameMenuController.IsAnyMenuOpen)
            {
                return;
            }

            EnsureStyles();
            float panelWidth = Mathf.Min(610f, networkSource != null ? Screen.width * 0.58f : Screen.width - 32f);
            var panelRect = new Rect(16f, Screen.height - 160f, panelWidth, 144f);
            GUI.Box(panelRect, GUIContent.none, _panelStyle);
            GUI.Label(new Rect(panelRect.x + 18f, panelRect.y + 12f, panelWidth - 36f, 25f), "任务导航", _headerStyle);
            GUI.Label(new Rect(panelRect.x + 18f, panelRect.y + 39f, panelWidth - 36f, 48f),
                CurrentInstruction.ActionText, _instructionStyle);
            GUI.Label(new Rect(panelRect.x + 18f, panelRect.y + 87f, panelWidth - 36f, 22f),
                GetProgressStatus(true), _progressStyle);
            GUI.Label(new Rect(panelRect.x + 18f, panelRect.y + 114f, panelWidth - 36f, 22f),
                "WASD 移动  ·  鼠标观察  ·  E 交互  ·  左键使用工具  ·  Q 放下  ·  Esc 暂停",
                _controlsStyle);

            DrawTargetMarker(
                CurrentTarget,
                GetTargetLabel(CurrentInstruction.PrimaryTarget),
                _primaryMarkerStyle,
                ref _primaryBehindSide);
            DrawTargetMarker(
                SecondaryTarget,
                GetTargetLabel(CurrentInstruction.SecondaryTarget),
                _secondaryMarkerStyle,
                ref _secondaryBehindSide);
        }

        private void DrawTargetMarker(Transform target, string label, GUIStyle style, ref float behindSide)
        {
            if (target == null || _viewCamera == null)
            {
                return;
            }

            Vector3 screen = _viewCamera.WorldToScreenPoint(target.position + Vector3.up * 0.65f);
            Vector3 localTarget = _viewCamera.transform.InverseTransformPoint(target.position + Vector3.up * 0.65f);
            var safeRect = new Rect(92f, 44f, Screen.width - 184f, Screen.height - 234f);
            DemoMarkerPlacement placement = DemoMarkerLayout.Calculate(
                localTarget,
                new Vector2(screen.x, Screen.height - screen.y),
                safeRect,
                behindSide);
            behindSide = placement.BehindSide;
            string glyph = placement.IsEdge ? GetEdgeGlyph(placement.Position, safeRect) : "◆";
            GUI.Label(new Rect(placement.Position.x - 90f, placement.Position.y - 19f, 180f, 38f),
                $"{glyph} {label}  {DistanceToPlayer(target):0}m", style);
        }

        private static string GetEdgeGlyph(Vector2 position, Rect safeRect)
        {
            const float tolerance = 1.5f;
            if (Mathf.Abs(position.x - safeRect.xMin) <= tolerance) return "◀";
            if (Mathf.Abs(position.x - safeRect.xMax) <= tolerance) return "▶";
            if (Mathf.Abs(position.y - safeRect.yMin) <= tolerance) return "▲";
            return "▼";
        }

        private string GetTargetLabel(DemoGuidanceTargetKind kind)
        {
            switch (kind)
            {
                case DemoGuidanceTargetKind.PressureGauge: return "压力表";
                case DemoGuidanceTargetKind.PumpInspection: return "泵检查面板";
                case DemoGuidanceTargetKind.CircuitBridgerRack: return "线路桥接器";
                case DemoGuidanceTargetKind.CircuitInterlock: return "联锁箱";
                case DemoGuidanceTargetKind.SealantRack: return "密封枪";
                case DemoGuidanceTargetKind.Leak: return "泄漏管段";
                case DemoGuidanceTargetKind.ImpactWrenchRack: return "冲击扳手";
                case DemoGuidanceTargetKind.Enemy: return "干扰体";
                case DemoGuidanceTargetKind.Fastener: return "机械接头";
                case DemoGuidanceTargetKind.ReplacementPipe: return "替换管";
                case DemoGuidanceTargetKind.CoolingConsole: return "冷却控制台";
                case DemoGuidanceTargetKind.Relay: return "继电器";
                case DemoGuidanceTargetKind.CampaignConsole:
                    return campaign != null && campaign.CurrentCampaignConsole != null &&
                           campaign.CurrentCampaignConsole.Role == DemoCalibrationConsoleRole.RelayRecovery
                        ? "配电恢复终端"
                        : "核心校准终端";
                case DemoGuidanceTargetKind.SecretPlate: return "旧维修铭牌";
                default: return string.Empty;
            }
        }

        private string GetProgressStatus(bool includeRecoveryHint)
        {
            if (networkSource != null) return networkSource.ProgressText;
            string status;
            switch (CurrentInstruction.PrimaryTarget)
            {
                case DemoGuidanceTargetKind.CircuitInterlock:
                    status = $"当前进度：联锁桥接 {incident.CircuitBridgeProgress}/3";
                    break;
                case DemoGuidanceTargetKind.Leak:
                    status = $"当前进度：泄漏密封 {Mathf.RoundToInt(incident.SealProgress * 100f)}%";
                    break;
                case DemoGuidanceTargetKind.Relay:
                    DemoRelayTarget relay = CurrentTarget != null ? CurrentTarget.GetComponent<DemoRelayTarget>() : null;
                    status = relay != null ? $"当前继电器：相位稳定 {relay.CompletedSteps}/3" : "正在定位下一座继电器";
                    break;
                case DemoGuidanceTargetKind.Enemy:
                    status = "紫色标记会锁定最近且尚未清除的干扰体";
                    break;
                default:
                    status = string.IsNullOrEmpty(CurrentInstruction.SecondaryText)
                        ? "橙色标记始终指向当前可推进主线的对象"
                        : CurrentInstruction.SecondaryText;
                    break;
            }

            if (includeRecoveryHint && Time.unscaledTime - _lastProgressAt > 40f)
            {
                return status + "  ·  卡住时确认准星提示：设备按 E，工具目标点左键";
            }

            return status;
        }

        private void EnsureStyles()
        {
            if (_panelStyle != null)
            {
                return;
            }

            _panelTexture = new Texture2D(1, 1);
            _panelTexture.SetPixel(0, 0, new Color(0.025f, 0.045f, 0.065f, 0.93f));
            _panelTexture.Apply();
            _panelStyle = new GUIStyle(GUI.skin.box) { normal = { background = _panelTexture } };
            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.63f, 0.16f) }
            };
            _instructionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                wordWrap = true,
                normal = { textColor = Color.white }
            };
            _progressStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                normal = { textColor = new Color(1f, 0.72f, 0.32f) }
            };
            _controlsStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                normal = { textColor = new Color(0.62f, 0.82f, 0.88f) }
            };
            _primaryMarkerStyle = CreateMarkerStyle(new Color(1f, 0.57f, 0.12f));
            _secondaryMarkerStyle = CreateMarkerStyle(new Color(0.96f, 0.25f, 0.86f));
        }

        private static GUIStyle CreateMarkerStyle(Color color)
        {
            return new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = color }
            };
        }
    }
}
