using System.Collections.Generic;
using FunGame.Combat;
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
        private readonly Dictionary<DemoGuidanceTargetKind, Transform> _fixedTargets =
            new Dictionary<DemoGuidanceTargetKind, Transform>();
        private DemoRelayTarget[] _relays;
        private Camera _viewCamera;
        private Texture2D _panelTexture;
        private GUIStyle _panelStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _instructionStyle;
        private GUIStyle _controlsStyle;
        private GUIStyle _primaryMarkerStyle;
        private GUIStyle _secondaryMarkerStyle;

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
            CacheFirst<DemoCalibrationConsole>(DemoGuidanceTargetKind.CampaignConsole);
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

            foreach (ToolRackInteractable rack in
                     FindObjectsByType<ToolRackInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                DemoGuidanceTargetKind kind = DemoGuidanceTargetKind.None;
                switch (rack.OfferedTool)
                {
                    case ToolKind.ImpactWrench: kind = DemoGuidanceTargetKind.ImpactWrenchRack; break;
                    case ToolKind.SealantGun: kind = DemoGuidanceTargetKind.SealantRack; break;
                    case ToolKind.CircuitBridger: kind = DemoGuidanceTargetKind.CircuitBridgerRack; break;
                }

                if (kind != DemoGuidanceTargetKind.None)
                {
                    _fixedTargets[kind] = rack.transform;
                }
            }

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
        }

        private Transform ResolveTarget(DemoGuidanceTargetKind kind)
        {
            if (kind == DemoGuidanceTargetKind.Relay)
            {
                return FindNearestRelay();
            }

            if (kind == DemoGuidanceTargetKind.Enemy)
            {
                return FindNearestEnemy();
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
                if (enemy == null || enemy.IsDefeated || !enemy.gameObject.activeInHierarchy)
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
            if (campaign == null || GameMenuController.IsAnyMenuOpen)
            {
                return;
            }

            EnsureStyles();
            float panelWidth = Mathf.Min(610f, Screen.width - 32f);
            var panelRect = new Rect(16f, Screen.height - 142f, panelWidth, 126f);
            GUI.Box(panelRect, GUIContent.none, _panelStyle);
            GUI.Label(new Rect(panelRect.x + 18f, panelRect.y + 12f, panelWidth - 36f, 25f), "任务导航", _headerStyle);
            GUI.Label(new Rect(panelRect.x + 18f, panelRect.y + 39f, panelWidth - 36f, 52f),
                CurrentInstruction.ActionText, _instructionStyle);
            GUI.Label(new Rect(panelRect.x + 18f, panelRect.y + 96f, panelWidth - 36f, 22f),
                "WASD 移动  ·  鼠标观察  ·  E 交互  ·  左键使用工具  ·  Q 放下  ·  Esc 暂停",
                _controlsStyle);

            DrawTargetMarker(CurrentTarget, GetTargetLabel(CurrentInstruction.PrimaryTarget), _primaryMarkerStyle);
            DrawTargetMarker(SecondaryTarget, GetTargetLabel(CurrentInstruction.SecondaryTarget), _secondaryMarkerStyle);
        }

        private void DrawTargetMarker(Transform target, string label, GUIStyle style)
        {
            if (target == null || _viewCamera == null)
            {
                return;
            }

            Vector3 screen = _viewCamera.WorldToScreenPoint(target.position + Vector3.up * 0.65f);
            if (screen.z < 0f)
            {
                screen.x = Screen.width - screen.x;
                screen.y = Screen.height - screen.y;
            }

            float x = Mathf.Clamp(screen.x, 92f, Screen.width - 92f);
            float y = Mathf.Clamp(Screen.height - screen.y, 44f, Screen.height - 172f);
            GUI.Label(new Rect(x - 90f, y - 19f, 180f, 38f),
                $"◆ {label}  {DistanceToPlayer(target):0}m", style);
        }

        private static string GetTargetLabel(DemoGuidanceTargetKind kind)
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
                case DemoGuidanceTargetKind.CampaignConsole: return "风暴控制台";
                case DemoGuidanceTargetKind.SecretPlate: return "旧维修铭牌";
                default: return string.Empty;
            }
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
