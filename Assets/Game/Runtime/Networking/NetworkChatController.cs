using System.Collections.Generic;
using FunGame.Interaction;
using FunGame.Player;
using FunGame.Tools;
using FunGame.UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace FunGame.Networking
{
    [DefaultExecutionOrder(-200)]
    public sealed class NetworkChatController : NetworkBehaviour
    {
        private const int MaximumMessageLength = 80;
        private const float ClosedPreviewSeconds = 8f;
        private const string InputControlName = "Network Chat Input";
        private readonly struct ChatEntry
        {
            public ChatEntry(string text, float receivedAt, bool local)
            { Text = text; ReceivedAt = receivedAt; IsLocal = local; }
            public string Text { get; }
            public float ReceivedAt { get; }
            public bool IsLocal { get; }
        }

        private readonly List<ChatEntry> _messages = new List<ChatEntry>();
        private string _draft = string.Empty;
        private bool _panelVisible;
        private bool _focusInputOnNextGui;
        private bool _scrollToEnd;
        private Vector2 _scroll;
        private GUIStyle _messageStyle, _titleStyle, _inputStyle, _buttonStyle;
        private Texture2D _background;
        private Keyboard _keyboard;
        private bool _composing;
        private int _imeCommitFrame = -1;
        private static NetworkChatController _active;
        private static int _closedFrame = -1;
        public static bool IsChatOpen => _active != null && _active._panelVisible;
        public static bool ConsumedCloseKey => _closedFrame == Time.frameCount;
        public int MessageCount => _messages.Count;
        public int ClosedPreviewCount => CountPreviews(Time.unscaledTime);
        public string LatestMessage => _messages.Count == 0 ? string.Empty : _messages[_messages.Count - 1].Text;

        private void Update()
        {
            if (!IsSpawned) return;
            if (_keyboard != Keyboard.current)
            {
                if (_keyboard != null) _keyboard.onIMECompositionChange -= OnCompositionChanged;
                _keyboard = Keyboard.current;
                if (_keyboard != null) _keyboard.onIMECompositionChange += OnCompositionChanged;
            }
            if (GameMenuController.IsAnyMenuOpen)
            {
                if (_panelVisible) SetPanelVisible(false);
                return;
            }
            if (_keyboard?.f3Key.wasPressedThisFrame == true) SetPanelVisible(!_panelVisible);
            else if (_panelVisible && !_composing && _imeCommitFrame != Time.frameCount
                && _keyboard?.escapeKey.wasPressedThisFrame == true) SetPanelVisible(false);
        }

        private void OnCompositionChanged(IMECompositionString composition)
        {
            if (_composing && composition.Count == 0) _imeCommitFrame = Time.frameCount;
            _composing = composition.Count > 0;
        }

        public override void OnNetworkDespawn()
        {
            SetPanelVisible(false);
            _messages.Clear();
            _draft = string.Empty;
        }

        private void OnDisable()
        {
            if (_panelVisible) SetPanelVisible(false);
            if (_keyboard != null) _keyboard.onIMECompositionChange -= OnCompositionChanged;
            _keyboard = null;
            _composing = false;
            if (_active == this) _active = null;
        }

        public override void OnDestroy()
        {
            if (_background != null) Destroy(_background);
            base.OnDestroy();
        }

        private void EnsureStyles()
        {
            if (_messageStyle != null) return;
            _background = new Texture2D(1, 1);
            _background.SetPixel(0, 0, new Color(0.025f, 0.04f, 0.065f, 0.94f));
            _background.Apply();
            _messageStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true, richText = false,
                padding = new RectOffset(0, 0, 4, 4), normal = { textColor = new Color(0.86f, 0.93f, 0.98f) } };
            _titleStyle = new GUIStyle(_messageStyle) { fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.3f, 0.85f, 0.95f) } };
            _inputStyle = new GUIStyle(GUI.skin.textField) { fontSize = 17, padding = new RectOffset(10, 10, 7, 7) };
            _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 16 };
        }

        private void OnGUI()
        {
            if (!IsSpawned || GameMenuController.IsAnyMenuOpen) return;
            EnsureStyles();
            int previousDepth = GUI.depth;
            GUI.depth = -50;
            Matrix4x4 previous = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            float screenHeight = Screen.height / scale;
            if (!_panelVisible)
            {
                DrawClosedPreview(screenHeight);
                GUI.matrix = previous;
                GUI.depth = previousDepth;
                return;
            }
            float height = 340f;
            Rect panel = new Rect(16f, screenHeight - 170f - height, 510f, height);
            GUI.DrawTexture(panel, _background);
            GUILayout.BeginArea(new Rect(panel.x + 12f, panel.y + 8f, panel.width - 24f, panel.height - 16f));
            if (_panelVisible)
            {
                GUILayout.Label("队伍聊天   ·   F3 / Esc 收起", _titleStyle);
                bool submit = Event.current.rawType == EventType.KeyDown
                    && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                    && !_composing && _imeCommitFrame != Time.frameCount;
                bool followBottom = _scrollToEnd && Event.current.type == EventType.Repaint;
                if (followBottom) _scroll.y = float.MaxValue;
                _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
                foreach (ChatEntry entry in _messages) GUILayout.Label(entry.Text, _messageStyle);
                GUILayout.EndScrollView();
                if (followBottom) _scrollToEnd = false;
                GUILayout.Label("Enter 发送  ·  滚动查看本次会话全部记录", _messageStyle);
                GUILayout.BeginHorizontal();
                GUI.SetNextControlName(InputControlName);
                _draft = GUILayout.TextField(_draft, MaximumMessageLength, _inputStyle, GUILayout.Height(38f));
                if (_focusInputOnNextGui && Event.current.type == EventType.Repaint)
                { GUI.FocusControl(InputControlName); _focusInputOnNextGui = false; }
                bool send = GUILayout.Button("发送", _buttonStyle, GUILayout.Width(66f), GUILayout.Height(38f));
                GUILayout.EndHorizontal();
                if (send || (submit && GUI.GetNameOfFocusedControl() == InputControlName))
                {
                    if (SendMessage(_draft)) _draft = string.Empty;
                    _focusInputOnNextGui = true;
                }
            }
            GUILayout.EndArea();
            GUI.matrix = previous;
            GUI.depth = previousDepth;
        }

        private void DrawClosedPreview(float screenHeight)
        {
            int remaining = ClosedPreviewCount;
            if (remaining == 0) return;
            int start = _messages.Count;
            float height = 0f;
            const float width = 486f;
            while (start > 0 && remaining > 0)
            {
                start--;
                if (!ShowPreview(_messages[start], Time.unscaledTime)) continue;
                remaining--;
                height += _messageStyle.CalcHeight(new GUIContent(_messages[start].Text), width);
            }
            float y = screenHeight - 170f - height;
            Color previousColor = GUI.color;
            for (int i = start; i < _messages.Count; i++)
            {
                ChatEntry entry = _messages[i];
                if (!ShowPreview(entry, Time.unscaledTime)) continue;
                float lineHeight = _messageStyle.CalcHeight(new GUIContent(entry.Text), width);
                float opacity = Mathf.Clamp01((ClosedPreviewSeconds - (Time.unscaledTime - entry.ReceivedAt)) / 2f);
                GUI.color = new Color(previousColor.r, previousColor.g, previousColor.b, previousColor.a * opacity * 0.7f);
                GUI.DrawTexture(new Rect(16f, y, width + 24f, lineHeight), _background);
                GUI.color = new Color(previousColor.r, previousColor.g, previousColor.b, previousColor.a * opacity);
                GUI.Label(new Rect(28f, y, width, lineHeight), entry.Text, _messageStyle);
                y += lineHeight;
            }
            GUI.color = previousColor;
        }

        public static string NormalizeMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return string.Empty;
            string normalized = message.Replace('\r', ' ').Replace('\n', ' ').Trim();
            return normalized.Length <= MaximumMessageLength ? normalized : normalized.Substring(0, MaximumMessageLength);
        }

        public bool SendMessage(string message)
        {
            string normalized = NormalizeMessage(message);
            if (!IsSpawned || normalized.Length == 0) return false;
            SubmitMessageRpc(normalized);
            _scrollToEnd = true;
            return true;
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void SubmitMessageRpc(string message, RpcParams rpcParams = default)
        {
            string normalized = NormalizeMessage(message);
            if (normalized.Length == 0) return;
            ulong sender = rpcParams.Receive.SenderClientId;
            if (!NetworkManager.ConnectedClients.TryGetValue(sender, out var client) || client.PlayerObject == null) return;
            string name = client.PlayerObject.GetComponent<NetworkPlayerController>().DisplayName;
            ReceiveMessageRpc(sender, name, normalized);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void ReceiveMessageRpc(ulong sender, string name, string message)
        {
            _messages.Add(new ChatEntry($"{name}：{message}", Time.unscaledTime, sender == NetworkManager.LocalClientId));
            _scrollToEnd = true;
        }

        public void SetPanelVisible(bool visible)
        {
            bool wasVisible = _panelVisible;
            _panelVisible = visible && IsSpawned && !GameMenuController.IsAnyMenuOpen;
            if (_panelVisible) _active = this;
            else if (_active == this) _active = null;
            if (wasVisible && !_panelVisible) _closedFrame = Time.frameCount;
            _focusInputOnNextGui = _panelVisible;
            _scrollToEnd = _panelVisible;
            GameObject local = NetworkManager != null ? NetworkManager.LocalClient?.PlayerObject?.gameObject : null;
            bool enabledInput = !_panelVisible && !GameMenuController.IsAnyMenuOpen
                && NetworkManager != null && NetworkManager.IsListening;
            local?.GetComponent<FirstPersonController>()?.SetGameplayInputEnabled(enabledInput);
            local?.GetComponent<NetworkPlayerCarryController>()?.SetGameplayInputEnabled(enabledInput);
            if (local != null)
            {
                var interactor = local.GetComponent<ContextInteractor>();
                if (interactor != null) interactor.enabled = enabledInput;
                var tool = local.GetComponent<ToolController>();
                if (tool != null) tool.enabled = enabledInput;
            }
            bool locked = enabledInput && local != null;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private static bool ShowPreview(ChatEntry entry, float now) => !entry.IsLocal && now - entry.ReceivedAt < ClosedPreviewSeconds;
        private int CountPreviews(float now)
        {
            int count = 0;
            for (int i = _messages.Count - 1; i >= 0 && count < 3; i--)
            {
                if (now - _messages[i].ReceivedAt >= ClosedPreviewSeconds) break;
                if (ShowPreview(_messages[i], now)) count++;
            }
            return count;
        }
    }
}
