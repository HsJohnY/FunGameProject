using System.Collections.Generic;
using FunGame.Interaction;
using FunGame.Player;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FunGame.Networking
{
    /// <summary>
    /// MVP 文字聊天：服务器只验证长度并转发，不包含过滤、历史持久化或私聊。
    /// </summary>
    public sealed class NetworkChatController : NetworkBehaviour
    {
        private const int MaximumMessageLength = 80;
        private const int MaximumVisibleMessages = 6;
        private const float ClosedPreviewSeconds = 8f;
        private const string InputControlName = "M3 Network Chat Input";

        private readonly struct ChatEntry
        {
            public ChatEntry(string text, float receivedAt)
            {
                Text = text;
                ReceivedAt = receivedAt;
            }

            public string Text { get; }
            public float ReceivedAt { get; }
        }

        private readonly List<ChatEntry> _messages = new List<ChatEntry>();
        private string _draft = string.Empty;
        private bool _panelVisible;
        private bool _focusInputOnNextGui;

        private void Update()
        {
            if (!IsSpawned || Keyboard.current?.f3Key.wasPressedThisFrame != true)
            {
                return;
            }

            SetPanelVisible(!_panelVisible);
        }

        public override void OnNetworkDespawn()
        {
            SetPanelVisible(false);
            _messages.Clear();
        }

        private void OnGUI()
        {
            if (!IsSpawned)
            {
                return;
            }

            float height = _panelVisible ? 245f : GetClosedPreviewHeight();
            GUILayout.BeginArea(new Rect(20f, Screen.height - height - 20f, 460f, height), GUI.skin.box);
            if (!_panelVisible)
            {
                DrawClosedPreview();
                GUILayout.Label("F3：打开文字聊天");
                GUILayout.EndArea();
                return;
            }

            GUILayout.Label("文字聊天（F3 关闭）");
            // TextField 会把回车事件的 type 标记为 Used，因此必须保留不会被控件改写的 rawType。
            bool submitWithEnter = Event.current.rawType == EventType.KeyDown
                && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter);
            int firstMessage = Mathf.Max(0, _messages.Count - MaximumVisibleMessages);
            for (int index = firstMessage; index < _messages.Count; index++)
            {
                GUILayout.Label(_messages[index].Text);
            }

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUI.SetNextControlName(InputControlName);
            _draft = GUILayout.TextField(_draft, MaximumMessageLength, GUILayout.Height(28f));
            if (_focusInputOnNextGui)
            {
                GUI.FocusControl(InputControlName);
                _focusInputOnNextGui = false;
            }

            if (GUILayout.Button("发送", GUILayout.Width(70f), GUILayout.Height(28f)))
            {
                SubmitDraft();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            if (submitWithEnter && GUI.GetNameOfFocusedControl() == InputControlName)
            {
                SubmitDraft();
                _focusInputOnNextGui = true;
            }
        }

        public static string NormalizeMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return string.Empty;
            }

            string normalized = message.Replace('\r', ' ').Replace('\n', ' ').Trim();
            return normalized.Length <= MaximumMessageLength
                ? normalized
                : normalized.Substring(0, MaximumMessageLength);
        }

        private void SubmitDraft()
        {
            string message = NormalizeMessage(_draft);
            if (message.Length == 0)
            {
                return;
            }

            SubmitMessageRpc(message);
            _draft = string.Empty;
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void SubmitMessageRpc(string message, RpcParams rpcParams = default)
        {
            string normalized = NormalizeMessage(message);
            if (normalized.Length == 0)
            {
                return;
            }

            ReceiveMessageRpc(rpcParams.Receive.SenderClientId, normalized);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void ReceiveMessageRpc(ulong senderClientId, string message)
        {
            _messages.Add(new ChatEntry($"玩家 {senderClientId}：{message}", Time.unscaledTime));
            if (_messages.Count > 20)
            {
                _messages.RemoveAt(0);
            }
        }

        private void SetPanelVisible(bool visible)
        {
            _panelVisible = visible && IsSpawned;
            _focusInputOnNextGui = _panelVisible;
            GameObject playerObject = GetLocalPlayerObject();
            FirstPersonController player = playerObject != null
                ? playerObject.GetComponent<FirstPersonController>()
                : null;
            player?.SetGameplayInputEnabled(!_panelVisible);
            ContextInteractor interactor = playerObject != null
                ? playerObject.GetComponent<ContextInteractor>()
                : null;
            if (interactor != null)
            {
                interactor.enabled = !_panelVisible;
            }

            playerObject?.GetComponent<NetworkPlayerCarryController>()
                ?.SetGameplayInputEnabled(!_panelVisible);
            bool shouldLockForGameplay = !_panelVisible
                && player != null
                && NetworkManager != null
                && NetworkManager.IsListening;
            Cursor.lockState = shouldLockForGameplay ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !shouldLockForGameplay;
        }

        private float GetClosedPreviewHeight()
        {
            int visibleCount = 0;
            for (int index = _messages.Count - 1; index >= 0 && visibleCount < MaximumVisibleMessages; index--)
            {
                if (Time.unscaledTime - _messages[index].ReceivedAt <= ClosedPreviewSeconds)
                {
                    visibleCount++;
                }
            }

            return 52f + visibleCount * 22f;
        }

        private void DrawClosedPreview()
        {
            int firstMessage = _messages.Count;
            int visibleCount = 0;
            for (int index = _messages.Count - 1; index >= 0 && visibleCount < MaximumVisibleMessages; index--)
            {
                if (Time.unscaledTime - _messages[index].ReceivedAt > ClosedPreviewSeconds)
                {
                    break;
                }

                firstMessage = index;
                visibleCount++;
            }

            for (int index = firstMessage; index < _messages.Count; index++)
            {
                GUILayout.Label(_messages[index].Text);
            }
        }

        private static GameObject GetLocalPlayerObject()
        {
            NetworkManager manager = NetworkManager.Singleton;
            return manager != null && manager.LocalClient?.PlayerObject != null
                ? manager.LocalClient.PlayerObject.gameObject
                : null;
        }
    }
}
