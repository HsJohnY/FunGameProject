using System.Collections.Generic;
using FunGame.Interaction;
using FunGame.Player;
using FunGame.Settings;
using FunGame.Tools;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FunGame.UI
{
    /// <summary>
    /// 提供启动主菜单、游戏内暂停菜单和可持久化必要设置。
    /// </summary>
    public sealed partial class GameMenuController : MonoBehaviour
    {
        private const string OpenAsMainKey = "FunGame.Menu.OpenAsMain";
        public const string SinglePlayerScene = "SinglePlayer_ThreeChapterDemo";
        public const string CooperativeScene = SinglePlayerScene;
        private static readonly Color Cyan = new Color(0.2f, 0.92f, 0.88f);
        private static readonly Color Amber = new Color(1f, 0.56f, 0.18f);
        private static readonly Color MutedText = new Color(0.52f, 0.68f, 0.74f);
        private static readonly Color PanelColor = new Color(0.018f, 0.052f, 0.075f, 0.98f);

        private enum MenuPage
        {
            Main,
            Pause,
            Lobby,
            Settings
        }

        [SerializeField] private FirstPersonController player;
        [SerializeField] private bool showMainMenuOnStart = true;
        [SerializeField] private bool networkSessionFlow;
        private ContextInteractor _interactor;
        private ToolController _toolController;
        private GameSettingsValues _pendingSettings;
        private MenuPage _page;
        private MenuPage _settingsReturnPage;
        private bool _menuOpen;
        private bool _changingScene;
        private Vector2 _settingsScroll;
        private Vector2Int[] _resolutionOptions;
        private GUIStyle _titleStyle;
        private GUIStyle _subtitleStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _panelStyle;
        private GUIStyle _contentStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _secondaryButtonStyle;
        private GUIStyle _choiceStyle;
        private GUIStyle _toggleStyle;
        private GUIStyle _statusStyle;
        private GUIStyle _eyebrowStyle;
        private GUIStyle _sliderStyle;
        private GUIStyle _sliderThumbStyle;
        private Texture2D _panelTexture;
        private Texture2D _contentTexture;
        private Texture2D _buttonTexture;
        private Texture2D _buttonHoverTexture;
        private Texture2D _buttonActiveTexture;
        private Texture2D _choiceTexture;
        private Texture2D _choiceOnTexture;
        private Texture2D _sliderTexture;
        private Texture2D _sliderThumbTexture;

        public static bool IsAnyMenuOpen { get; private set; }
        public bool IsMenuOpen => _menuOpen;
        public bool UsesNetworkSessionFlow => networkSessionFlow;
        public bool CanStartSinglePlayer => networkSessionFlow && Application.CanStreamedLevelBeLoaded(SinglePlayerScene);

        public void StartSinglePlayerMode()
        {
            if (!CanStartSinglePlayer) return;
            FunGame.Demo.SharedMapModeController.NextMode = FunGame.Demo.ExpeditionMode.Solo;
            StartCoroutine(ChangeScene(SinglePlayerScene, false));
        }

        public void ReturnToModeSelection() => ReloadAs(MenuPage.Main);

        public void Configure(FirstPersonController configuredPlayer)
        {
            BindPlayer(configuredPlayer);
        }

        /// <summary>
        /// 联机场景的玩家在启动会话后才生成，因此菜单需要延迟绑定本地拥有者。
        /// </summary>
        public void ConfigureForNetworkSession()
        {
            networkSessionFlow = true;
            BindPlayer(null);
        }

        public void ConfigureForSinglePlayer(FirstPersonController localPlayer)
        {
            networkSessionFlow = false;
            BindPlayer(localPlayer);
        }

        /// <summary>
        /// 仅供开发构建的截图检查点进入玩法画面；普通玩家仍通过主菜单按钮开始。
        /// </summary>
        public void EnterGameplayForAutomation()
        {
            CloseMenu();
        }

        /// <summary>
        /// 仅供开发构建的截图检查点直接打开设置页；普通玩家仍从主菜单进入。
        /// </summary>
        public void OpenSettingsForAutomation()
        {
            _settingsReturnPage = MenuPage.Main;
            _pendingSettings = GameSettingsStore.Current;
            OpenMenu(MenuPage.Settings);
        }

        private void Awake()
        {
            if (player == null)
            {
                player = Object.FindFirstObjectByType<FirstPersonController>();
            }

            if (player != null)
            {
                _interactor = player.GetComponent<ContextInteractor>();
                _toolController = player.GetComponent<ToolController>();
            }

            _pendingSettings = GameSettingsStore.Load();
            GameSettingsStore.Apply(_pendingSettings, player, false);
            _resolutionOptions = BuildResolutionOptions();
            bool openAsMain = showMainMenuOnStart;
            if (PlayerPrefs.HasKey(OpenAsMainKey))
            {
                openAsMain = PlayerPrefs.GetInt(OpenAsMainKey, 1) != 0;
                PlayerPrefs.DeleteKey(OpenAsMainKey);
            }

            if (openAsMain)
            {
                OpenMenu(MenuPage.Main);
            }
            else
            {
                CloseMenu();
            }
        }

        private void Update()
        {
            if (player == null)
            {
                TryBindSpawnedPlayer();
            }

            UpdateNetworkLobby();

            if (_menuOpen && !networkSessionFlow && Time.timeScale != 0f)
            {
                Time.timeScale = 0f;
            }
        }

        private void OnDestroy()
        {
            if (IsAnyMenuOpen)
            {
                Time.timeScale = 1f;
                IsAnyMenuOpen = false;
            }

            DestroyTexture(_panelTexture);
            DestroyTexture(_contentTexture);
            DestroyTexture(_buttonTexture);
            DestroyTexture(_buttonHoverTexture);
            DestroyTexture(_buttonActiveTexture);
            DestroyTexture(_choiceTexture);
            DestroyTexture(_choiceOnTexture);
            DestroyTexture(_sliderTexture);
            DestroyTexture(_sliderThumbTexture);
        }

        private void OnGUI()
        {
            HandleMenuKeyboard();
            if (!_menuOpen)
            {
                return;
            }

            EnsureStyles();
            DrawBackdrop();

            if (_page == MenuPage.Lobby)
            {
                DrawNetworkLobby();
                return;
            }

            float panelWidth = Mathf.Min(_page == MenuPage.Settings ? 920f : 1080f, Screen.width - 40f);
            float panelHeight = Mathf.Min(_page == MenuPage.Settings ? 820f : 650f, Screen.height - 40f);
            Rect panel = new Rect(
                (Screen.width - panelWidth) * 0.5f,
                (Screen.height - panelHeight) * 0.5f,
                panelWidth,
                panelHeight);
            DrawPanelFrame(panel);
            if (_page == MenuPage.Settings)
            {
                DrawSettingsPage(panel);
            }
            else
            {
                DrawMenuPage(panel);
            }
        }

        private void DrawMenuPage(Rect panel)
        {
            const float padding = 42f;
            Rect inner = new Rect(panel.x + padding, panel.y + padding, panel.width - padding * 2f, panel.height - padding * 2f);
            float split = inner.width * 0.55f;
            Rect left = new Rect(inner.x, inner.y, split - 26f, inner.height);
            Rect right = new Rect(inner.x + split + 16f, inner.y + 54f, inner.width - split - 16f, inner.height - 80f);

            GUI.Label(new Rect(left.x, left.y, left.width, 24f), "NOMAD ENGINEERING DIVISION  //  TERMINAL 07", _eyebrowStyle);
            GUI.Label(new Rect(left.x, left.y + 42f, left.width, 108f), "风暴荒原\n维修队", _titleStyle);
            GUI.Label(
                new Rect(left.x, left.y + 158f, left.width, 30f),
                _page == MenuPage.Main ? "移动巨构冷却舱 · 作业控制台" : "任务暂停 · 本地输入已隔离",
                _subtitleStyle);

            DrawRect(new Rect(left.x, left.y + 205f, left.width - 12f, 1f), new Color(Cyan.r, Cyan.g, Cyan.b, 0.35f));
            float statusY = left.y + 232f;
            DrawStatusRow(left.x, ref statusY, left.width - 12f, "CORE", "巨构主系统", "ONLINE", Cyan);
            DrawStatusRow(left.x, ref statusY, left.width - 12f, "BAY-04", "冷却舱任务链", "READY", Amber);
            DrawStatusRow(left.x, ref statusY, left.width - 12f, "LINK", "维修队本地终端", "STABLE", Cyan);

            float pulse = 0.55f + Mathf.Sin(Time.unscaledTime * 2.4f) * 0.25f;
            DrawRect(new Rect(left.x, left.y + left.height - 54f, 6f, 32f), new Color(Amber.r, Amber.g, Amber.b, pulse));
            GUI.Label(
                new Rect(left.x + 18f, left.y + left.height - 58f, left.width, 40f),
                _page == MenuPage.Main ? "STORM WINDOW // MAINTENANCE ROUTE AVAILABLE" : "PAUSE LOCK // SIMULATION HOLDING",
                _eyebrowStyle);

            GUI.Label(
                new Rect(right.x, inner.y, right.width, 32f),
                _page == MenuPage.Main ? "任务终端" : "暂停控制",
                _sectionStyle);
            DrawRect(new Rect(right.x, inner.y + 38f, right.width, 2f), Amber);

            float buttonY = right.y;
            if (_page == MenuPage.Main)
            {
                if (CanStartSinglePlayer && DrawTechButton(right, ref buttonY, "单人模式", "SOLO · THREE CHAPTER EXPEDITION", Amber))
                {
                    StartSinglePlayerMode();
                }
                string startTitle = networkSessionFlow ? "联机协作" : "开始维修任务";
                string startSubtitle = networkSessionFlow ? "HOST OR JOIN SESSION" : "INITIALIZE EXPEDITION";
                if (DrawTechButton(right, ref buttonY, startTitle, startSubtitle, Amber))
                {
                    if (networkSessionFlow) OpenNetworkLobby();
                    else CloseMenu();
                }
            }
            else if (DrawTechButton(right, ref buttonY, "继续任务", "RESUME OPERATION", Cyan))
            {
                CloseMenu();
            }

            if (DrawTechButton(right, ref buttonY, "系统设置", "AUDIO · DISPLAY · CONTROL", Cyan))
            {
                _settingsReturnPage = _page;
                _pendingSettings = GameSettingsStore.Current;
                _page = MenuPage.Settings;
            }

            if (_page == MenuPage.Pause)
            {
                if (DrawTechButton(right, ref buttonY, networkSessionFlow ? "联机房间" : "重新开始任务",
                    networkSessionFlow ? "ROOM · CONNECTION" : "RELOAD CHECKPOINT", Amber))
                {
                    if (networkSessionFlow) OpenNetworkLobby();
                    else ReloadAs(MenuPage.Pause);
                }

                if (DrawTechButton(right, ref buttonY, "返回主菜单", "DISCONNECT FROM BAY", Cyan))
                {
                    ReloadAs(MenuPage.Main);
                }
            }

            if (DrawTechButton(right, ref buttonY, "退出游戏", "TERMINATE CLIENT", new Color(1f, 0.35f, 0.22f)))
            {
                Application.Quit();
            }

            GUI.Label(
                new Rect(right.x, panel.yMax - 62f, right.width, 24f),
                "[ ESC ]  OPEN / CLOSE TERMINAL",
                _eyebrowStyle);
        }

        private void DrawSettingsPage(Rect panel)
        {
            Rect header = new Rect(panel.x + 42f, panel.y + 28f, panel.width - 84f, 66f);
            GUI.Label(new Rect(header.x, header.y, header.width, 22f), "SYSTEM CONFIGURATION  //  LOCAL PROFILE", _eyebrowStyle);
            GUI.Label(new Rect(header.x, header.y + 18f, header.width, 48f), "终端设置", _sectionStyle);
            DrawRect(new Rect(header.x, header.yMax + 8f, header.width, 2f), Amber);

            Rect content = new Rect(panel.x + 42f, panel.y + 118f, panel.width - 84f, panel.height - 158f);
            GUI.Box(content, GUIContent.none, _contentStyle);
            const float horizontalPadding = 22f;
            const float topPadding = 12f;
            const float bottomPadding = 18f;
            const float footerHeight = 44f;
            const float footerGap = 14f;
            Rect footer = new Rect(
                content.x + horizontalPadding,
                content.yMax - bottomPadding - footerHeight,
                content.width - horizontalPadding * 2f,
                footerHeight);
            Rect scrollArea = new Rect(
                content.x + horizontalPadding,
                content.y + topPadding,
                content.width - horizontalPadding * 2f,
                footer.y - footerGap - content.y - topPadding);

            GUILayout.BeginArea(scrollArea);
            _settingsScroll = GUILayout.BeginScrollView(_settingsScroll);

            DrawSection("AUDIO BUS // 音频");
            _pendingSettings.MasterVolume = DrawSlider("主音量", _pendingSettings.MasterVolume, 0f, 1f, true);
            _pendingSettings.MusicVolume = DrawSlider("音乐音量", _pendingSettings.MusicVolume, 0f, 1f, true);
            _pendingSettings.SoundEffectsVolume = DrawSlider("音效音量", _pendingSettings.SoundEffectsVolume, 0f, 1f, true);

            DrawSection("OPTICS & INPUT // 控制与视角");
            _pendingSettings.MouseSensitivity = DrawSlider(
                "鼠标灵敏度", _pendingSettings.MouseSensitivity, 0.02f, 0.3f, false);
            _pendingSettings.FieldOfView = DrawSlider("视野范围", _pendingSettings.FieldOfView, 65f, 110f, false);
            _pendingSettings.InvertYAxis = GUILayout.Toggle(
                _pendingSettings.InvertYAxis, "反转鼠标 Y 轴  //  INVERT VERTICAL AXIS", _toggleStyle, GUILayout.Height(36f));

            DrawSection("DISPLAY ARRAY // 画面");
            GUILayout.Label("分辨率", _valueStyle);
            string[] resolutionLabels = new string[_resolutionOptions.Length];
            int selectedResolution = 0;
            for (int index = 0; index < _resolutionOptions.Length; index++)
            {
                Vector2Int option = _resolutionOptions[index];
                resolutionLabels[index] = $"{option.x} × {option.y}";
                if (option.x == _pendingSettings.ResolutionWidth && option.y == _pendingSettings.ResolutionHeight)
                {
                    selectedResolution = index;
                }
            }

            selectedResolution = GUILayout.SelectionGrid(selectedResolution, resolutionLabels, 3, _choiceStyle);
            _pendingSettings.ResolutionWidth = _resolutionOptions[selectedResolution].x;
            _pendingSettings.ResolutionHeight = _resolutionOptions[selectedResolution].y;
            _pendingSettings.Fullscreen = GUILayout.Toggle(
                _pendingSettings.Fullscreen, "全屏窗口  //  FULLSCREEN WINDOW", _toggleStyle, GUILayout.Height(36f));

            GUILayout.Label("画质预设", _valueStyle);
            _pendingSettings.QualityLevel = GUILayout.SelectionGrid(
                Mathf.Clamp(_pendingSettings.QualityLevel, 0, QualitySettings.names.Length - 1),
                QualitySettings.names,
                3,
                _choiceStyle);
            _pendingSettings.VerticalSync = GUILayout.Toggle(
                _pendingSettings.VerticalSync, "垂直同步  //  V-SYNC", _toggleStyle, GUILayout.Height(36f));

            GUILayout.Label("帧率上限（关闭垂直同步后生效）", _valueStyle);
            int[] frameRates = { 30, 60, 120, -1 };
            string[] frameRateLabels = { "30", "60", "120", "不限" };
            int frameRateIndex = System.Array.IndexOf(frameRates, _pendingSettings.FrameRateLimit);
            frameRateIndex = GUILayout.SelectionGrid(Mathf.Max(0, frameRateIndex), frameRateLabels, 4, _choiceStyle);
            _pendingSettings.FrameRateLimit = frameRates[frameRateIndex];

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            GUILayout.BeginArea(footer);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("恢复默认  //  RESET", _secondaryButtonStyle, GUILayout.Height(44f)))
            {
                _pendingSettings = GameSettingsStore.RestoreDefaults();
            }

            if (GUILayout.Button("应用并保存  //  APPLY", _buttonStyle, GUILayout.Height(44f)))
            {
                GameSettingsStore.Apply(_pendingSettings, player, true);
                _pendingSettings = GameSettingsStore.Current;
            }

            if (GUILayout.Button("返回  //  BACK", _secondaryButtonStyle, GUILayout.Height(44f)))
            {
                _page = _settingsReturnPage;
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private float DrawSlider(string label, float value, float minimum, float maximum, bool percentage)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _valueStyle, GUILayout.Width(150f));
            value = GUILayout.HorizontalSlider(
                value, minimum, maximum, _sliderStyle, _sliderThumbStyle, GUILayout.Height(24f));
            string text = percentage ? $"{value:P0}" : value.ToString("0.00");
            GUILayout.Label(text, _valueStyle, GUILayout.Width(72f));
            GUILayout.EndHorizontal();
            return value;
        }

        private void DrawSection(string title)
        {
            GUILayout.Space(18f);
            GUILayout.Label(title, _sectionStyle);
            GUILayout.Box(GUIContent.none, _sliderStyle, GUILayout.ExpandWidth(true), GUILayout.Height(2f));
            GUILayout.Space(10f);
        }

        private void DrawBackdrop()
        {
            DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.006f, 0.016f, 0.025f, 0.96f));
            const float gridSize = 64f;
            Color gridColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.045f);
            for (float x = 0f; x < Screen.width; x += gridSize)
            {
                DrawRect(new Rect(x, 0f, 1f, Screen.height), gridColor);
            }

            for (float y = 0f; y < Screen.height; y += gridSize)
            {
                DrawRect(new Rect(0f, y, Screen.width, 1f), gridColor);
            }

            float scanY = Mathf.Repeat(Time.unscaledTime * 92f, Screen.height + 140f) - 70f;
            DrawRect(new Rect(0f, scanY, Screen.width, 2f), new Color(Cyan.r, Cyan.g, Cyan.b, 0.16f));
            DrawRect(new Rect(0f, scanY - 18f, Screen.width, 38f), new Color(Cyan.r, Cyan.g, Cyan.b, 0.018f));

            float pulse = 0.09f + Mathf.Sin(Time.unscaledTime * 1.7f) * 0.035f;
            DrawRect(new Rect(0f, 0f, 9f, Screen.height), new Color(Amber.r, Amber.g, Amber.b, pulse));
            DrawRect(new Rect(Screen.width - 5f, 0f, 5f, Screen.height), new Color(Cyan.r, Cyan.g, Cyan.b, pulse));
        }

        private void DrawPanelFrame(Rect panel)
        {
            GUI.Box(panel, GUIContent.none, _panelStyle);
            DrawRect(new Rect(panel.x, panel.y, panel.width, 2f), new Color(Cyan.r, Cyan.g, Cyan.b, 0.75f));
            DrawRect(new Rect(panel.x, panel.yMax - 2f, panel.width, 2f), new Color(Amber.r, Amber.g, Amber.b, 0.45f));
            DrawRect(new Rect(panel.x, panel.y, 2f, panel.height), new Color(Cyan.r, Cyan.g, Cyan.b, 0.4f));
            DrawRect(new Rect(panel.xMax - 2f, panel.y, 2f, panel.height), new Color(Cyan.r, Cyan.g, Cyan.b, 0.4f));

            const float corner = 30f;
            DrawRect(new Rect(panel.x - 5f, panel.y - 5f, corner, 4f), Amber);
            DrawRect(new Rect(panel.x - 5f, panel.y - 5f, 4f, corner), Amber);
            DrawRect(new Rect(panel.xMax - corner + 5f, panel.y - 5f, corner, 4f), Cyan);
            DrawRect(new Rect(panel.xMax + 1f, panel.y - 5f, 4f, corner), Cyan);
            DrawRect(new Rect(panel.x - 5f, panel.yMax + 1f, corner, 4f), Cyan);
            DrawRect(new Rect(panel.x - 5f, panel.yMax - corner + 5f, 4f, corner), Cyan);
            DrawRect(new Rect(panel.xMax - corner + 5f, panel.yMax + 1f, corner, 4f), Amber);
            DrawRect(new Rect(panel.xMax + 1f, panel.yMax - corner + 5f, 4f, corner), Amber);
        }

        private void DrawStatusRow(
            float x,
            ref float y,
            float width,
            string code,
            string label,
            string status,
            Color statusColor)
        {
            Rect row = new Rect(x, y, width, 46f);
            DrawRect(row, new Color(0.035f, 0.095f, 0.12f, 0.72f));
            DrawRect(new Rect(row.x, row.y, 3f, row.height), statusColor);
            GUI.Label(new Rect(row.x + 14f, row.y + 4f, 76f, 38f), code, _eyebrowStyle);
            GUI.Label(new Rect(row.x + 92f, row.y + 4f, row.width - 190f, 38f), label, _statusStyle);
            _eyebrowStyle.normal.textColor = statusColor;
            GUI.Label(new Rect(row.xMax - 90f, row.y + 4f, 76f, 38f), status, _eyebrowStyle);
            _eyebrowStyle.normal.textColor = MutedText;
            y += 56f;
        }

        private bool DrawTechButton(Rect column, ref float y, string title, string subtitle, Color accent)
        {
            Rect button = new Rect(column.x, y, column.width, 58f);
            bool hovered = button.Contains(Event.current.mousePosition);
            DrawRect(
                new Rect(button.x, button.y, hovered ? 7f : 4f, button.height),
                new Color(accent.r, accent.g, accent.b, hovered ? 1f : 0.7f));
            bool clicked = GUI.Button(button, $"{title}\n<size=10>{subtitle}</size>", _buttonStyle);
            DrawRect(new Rect(button.xMax - 22f, button.y + 8f, 14f, 2f), accent);
            DrawRect(new Rect(button.xMax - 10f, button.y + 8f, 2f, 14f), accent);
            y += 68f;
            return clicked;
        }

        private static void DrawRect(Rect rectangle, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rectangle, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void OpenMenu(MenuPage page)
        {
            _page = page;
            _menuOpen = true;
            IsAnyMenuOpen = true;
            Time.timeScale = networkSessionFlow ? 1f : 0f;
            SetGameplayEnabled(false);
        }

        private void CloseMenu()
        {
            _menuOpen = false;
            IsAnyMenuOpen = false;
            Time.timeScale = 1f;
            SetGameplayEnabled(true);
        }

        private void SetGameplayEnabled(bool enabled)
        {
            enabled = enabled && !FunGame.Networking.NetworkChatController.IsChatOpen;
            if (player != null)
            {
                player.SetGameplayInputEnabled(enabled);
                var carry = player.GetComponent<FunGame.Networking.NetworkPlayerCarryController>();
                if (carry != null) carry.SetGameplayInputEnabled(enabled);
            }
            if (!enabled || player == null)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            if (_interactor != null)
            {
                _interactor.enabled = enabled;
            }

            if (_toolController != null)
            {
                _toolController.enabled = enabled;
            }
        }

        private void TryBindSpawnedPlayer()
        {
            foreach (FirstPersonController candidate in
                     Object.FindObjectsByType<FirstPersonController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.isActiveAndEnabled)
                {
                    BindPlayer(candidate);
                    return;
                }
            }
        }

        private void BindPlayer(FirstPersonController configuredPlayer)
        {
            player = configuredPlayer;
            _interactor = player != null ? player.GetComponent<ContextInteractor>() : null;
            _toolController = player != null ? player.GetComponent<ToolController>() : null;
            if (player == null)
            {
                return;
            }

            GameSettingsStore.Apply(GameSettingsStore.Current, player, false);
            SetGameplayEnabled(!_menuOpen);
        }

        private void ReloadAs(MenuPage page)
        {
            FunGame.Demo.SharedMapModeController.NextMode = page == MenuPage.Main || networkSessionFlow
                ? FunGame.Demo.ExpeditionMode.Cooperative : FunGame.Demo.ExpeditionMode.Solo;
            string scene = page == MenuPage.Main && Application.CanStreamedLevelBeLoaded(CooperativeScene)
                ? CooperativeScene : SceneManager.GetActiveScene().name;
            StartCoroutine(ChangeScene(scene, page == MenuPage.Main));
        }

        private System.Collections.IEnumerator ChangeScene(string scene, bool openMenu)
        {
            if (_changingScene) yield break;
            _changingScene = true;
            Time.timeScale = 1f;
            var manager = Unity.Netcode.NetworkManager.Singleton;
            if (manager != null)
            {
                manager.Shutdown();
                yield return null;
                float deadline = Time.realtimeSinceStartup + 5f;
                while (manager != null && manager.IsListening && Time.realtimeSinceStartup < deadline)
                    yield return null;
                if (manager != null) Destroy(manager.gameObject);
                yield return null;
            }
            PlayerPrefs.SetInt(OpenAsMainKey, openMenu ? 1 : 0);
            SceneManager.LoadScene(scene);
        }

        private Vector2Int[] BuildResolutionOptions()
        {
            var options = new List<Vector2Int>
            {
                new Vector2Int(1280, 720),
                new Vector2Int(1600, 900),
                new Vector2Int(1920, 1080)
            };
            Vector2Int current = new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height);
            if (!options.Contains(current))
            {
                options.Add(current);
            }

            Vector2Int saved = new Vector2Int(_pendingSettings.ResolutionWidth, _pendingSettings.ResolutionHeight);
            if (!options.Contains(saved))
            {
                options.Add(saved);
            }

            return options.ToArray();
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null)
            {
                return;
            }

            _panelTexture = CreateSolidTexture("Menu Panel", PanelColor);
            _contentTexture = CreateSolidTexture("Menu Content", new Color(0.025f, 0.075f, 0.1f, 0.92f));
            _buttonTexture = CreateSolidTexture("Menu Button", new Color(0.035f, 0.11f, 0.145f, 0.98f));
            _buttonHoverTexture = CreateSolidTexture("Menu Button Hover", new Color(0.045f, 0.2f, 0.225f, 1f));
            _buttonActiveTexture = CreateSolidTexture("Menu Button Active", new Color(0.62f, 0.31f, 0.09f, 1f));
            _choiceTexture = CreateSolidTexture("Menu Choice", new Color(0.035f, 0.095f, 0.12f, 1f));
            _choiceOnTexture = CreateSolidTexture("Menu Choice On", new Color(0.06f, 0.32f, 0.34f, 1f));
            _sliderTexture = CreateSolidTexture("Menu Slider", new Color(0.08f, 0.28f, 0.3f, 1f));
            _sliderThumbTexture = CreateSolidTexture("Menu Slider Thumb", Amber);

            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _panelTexture },
                padding = new RectOffset(0, 0, 0, 0)
            };
            _contentStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _contentTexture },
                padding = new RectOffset(22, 22, 12, 18)
            };
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 43,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.9f, 0.98f, 1f) }
            };
            _subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 16,
                normal = { textColor = MutedText }
            };
            _sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Cyan }
            };
            _valueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                normal = { textColor = new Color(0.85f, 0.94f, 0.96f) },
                margin = new RectOffset(5, 5, 6, 6)
            };
            _statusStyle = new GUIStyle(_valueStyle)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 14
            };
            _eyebrowStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = MutedText }
            };
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                richText = true,
                padding = new RectOffset(24, 34, 5, 5),
                normal = { background = _buttonTexture, textColor = new Color(0.9f, 0.98f, 1f) },
                hover = { background = _buttonHoverTexture, textColor = Color.white },
                active = { background = _buttonActiveTexture, textColor = Color.white }
            };
            _secondaryButtonStyle = new GUIStyle(_buttonStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                padding = new RectOffset(8, 8, 5, 5)
            };
            _choiceStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                fixedHeight = 34f,
                margin = new RectOffset(3, 3, 3, 3),
                normal = { background = _choiceTexture, textColor = MutedText },
                hover = { background = _buttonHoverTexture, textColor = Color.white },
                active = { background = _buttonActiveTexture, textColor = Color.white },
                onNormal = { background = _choiceOnTexture, textColor = Cyan },
                onHover = { background = _buttonHoverTexture, textColor = Color.white },
                onActive = { background = _buttonActiveTexture, textColor = Color.white }
            };
            _toggleStyle = new GUIStyle(_choiceStyle)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(14, 10, 4, 4),
                margin = new RectOffset(3, 3, 7, 7)
            };
            _sliderStyle = new GUIStyle(GUI.skin.horizontalSlider)
            {
                normal = { background = _sliderTexture },
                fixedHeight = 6f,
                margin = new RectOffset(4, 4, 9, 9)
            };
            _sliderThumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb)
            {
                normal = { background = _sliderThumbTexture },
                hover = { background = _sliderThumbTexture },
                active = { background = _sliderThumbTexture },
                fixedWidth = 14f,
                fixedHeight = 18f
            };
        }

        private static Texture2D CreateSolidTexture(string name, Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                name = name,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixel(0, 0, color);
            texture.Apply(false, true);
            return texture;
        }

        private static void DestroyTexture(Texture2D texture)
        {
            if (texture != null)
            {
                Object.Destroy(texture);
            }
        }
    }
}
