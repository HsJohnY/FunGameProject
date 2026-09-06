using FunGame.Player;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using System.Text;

namespace FunGame.Networking
{
    /// <summary>
    /// 把 NGO 的玩家所有权转换为本地第一人称控制权。
    /// 只有拥有者读取输入和启用摄像机，远端实例仅显示同步后的角色外观。
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(FirstPersonController))]
    public sealed class NetworkPlayerController : NetworkBehaviour
    {
        [SerializeField] private FirstPersonController firstPersonController;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private AudioListener audioListener;
        [SerializeField] private Renderer[] remoteBodyRenderers;
        [SerializeField] private Behaviour[] ownerOnlyBehaviours;

        public bool HasLocalControl => firstPersonController != null && firstPersonController.enabled;
        public bool IsViewActive => viewCamera != null && viewCamera.enabled;
        public const int MaximumNicknameLength = 16;
        private const string NicknamePreference = "Coop.Nickname";
        private readonly NetworkVariable<FixedString64Bytes> nickname = new NetworkVariable<FixedString64Bytes>();
        private GUIStyle _nameStyle;
        private CharacterController _body;
        private static string _localNickname;
        public string DisplayName => nickname.Value.IsEmpty ? $"玩家 {OwnerClientId}" : nickname.Value.ToString();
        public static string LocalNickname
        {
            get => _localNickname ??= PlayerPrefs.GetString(NicknamePreference, "维修员");
            set
            {
                _localNickname = NormalizeNickname(value);
                PlayerPrefs.SetString(NicknamePreference, _localNickname);
                PlayerPrefs.Save();
            }
        }

        public static string NormalizeNickname(string value)
        {
            var result = new StringBuilder();
            foreach (char character in (value ?? string.Empty).Trim())
            {
                if (char.IsControl(character) || char.IsSurrogate(character) || character == '<' || character == '>') continue;
                if (result.Length == MaximumNicknameLength) break;
                result.Append(character);
            }
            return result.ToString().Trim();
        }

        private void Awake()
        {
            _body = GetComponent<CharacterController>();
            if (firstPersonController == null)
            {
                firstPersonController = GetComponent<FirstPersonController>();
            }

            if (viewCamera == null)
            {
                viewCamera = GetComponentInChildren<Camera>(true);
            }

            if (audioListener == null && viewCamera != null)
            {
                audioListener = viewCamera.GetComponent<AudioListener>();
            }

            ApplyOwnershipPresentation(false);
        }

        public override void OnNetworkSpawn()
        {
            ApplyPlayerColor(OwnerClientId);
            ApplyOwnershipPresentation(IsOwner);
            if (IsOwner) SetNicknameRpc(LocalNickname);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void SetNicknameRpc(string value)
        {
            string normalized = NormalizeNickname(value);
            nickname.Value = new FixedString64Bytes(normalized.Length == 0 ? $"玩家 {OwnerClientId}" : normalized);
        }

        private void OnGUI()
        {
            if (!IsSpawned || IsOwner || FunGame.UI.GameMenuController.IsAnyMenuOpen) return;
            var local = NetworkManager.LocalClient?.PlayerObject;
            Camera camera = local != null ? local.GetComponent<NetworkPlayerController>()?.viewCamera : null;
            if (camera == null || !camera.enabled) return;
            Vector3 head = transform.TransformPoint(_body != null
                ? _body.center + Vector3.up * (_body.height * 0.5f + 0.2f)
                : Vector3.up * 1.2f);
            Vector3 point = camera.WorldToScreenPoint(head);
            float distance = Vector3.Distance(camera.transform.position, head);
            if (point.z <= 0f || distance > 35f) return;
            if (Physics.Linecast(camera.transform.position, head, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore)
                && !hit.transform.IsChildOf(transform) && !hit.transform.IsChildOf(local.transform)) return;
            if (_nameStyle == null) _nameStyle = new GUIStyle(GUI.skin.box)
            { fontSize = 16, alignment = TextAnchor.MiddleCenter, richText = false, padding = new RectOffset(10, 10, 4, 4) };
            _nameStyle.normal.textColor = GetPlayerColor(OwnerClientId);
            var content = new GUIContent(DisplayName);
            Vector2 size = _nameStyle.CalcSize(content);
            GUI.Label(new Rect(point.x - size.x / 2f, Screen.height - point.y - size.y, size.x, size.y), content, _nameStyle);
        }

        public override void OnNetworkDespawn()
        {
            ApplyOwnershipPresentation(false);
        }

        private void Update()
        {
            if (!IsSpawned || !IsOwner || transform.position.y >= NetworkPlayerSpawnLayout.FallResetHeight)
            {
                return;
            }

            // 验证场不应因一次跌落中断整轮双端测试；复位仍由拥有者写入并同步给其他客户端。
            bool controllerWasEnabled = firstPersonController != null && firstPersonController.enabled;
            if (firstPersonController != null)
            {
                firstPersonController.enabled = false;
            }

            transform.SetPositionAndRotation(
                NetworkPlayerSpawnLayout.GetSpawnPosition(OwnerClientId),
                Quaternion.identity);

            if (firstPersonController != null)
            {
                firstPersonController.enabled = controllerWasEnabled;
            }
        }

        /// <summary>
        /// 独立封装所有权表现，方便自动测试验证输入、镜头和身体显隐边界。
        /// </summary>
        public void ApplyOwnershipPresentation(bool ownsPlayer)
        {
            if (firstPersonController != null)
            {
                firstPersonController.enabled = ownsPlayer;
                firstPersonController.SetGameplayInputEnabled(ownsPlayer);
            }

            if (viewCamera != null)
            {
                viewCamera.enabled = ownsPlayer;
            }

            if (audioListener != null)
            {
                audioListener.enabled = ownsPlayer;
            }

            if (remoteBodyRenderers == null)
            {
                remoteBodyRenderers = new Renderer[0];
            }

            foreach (Renderer bodyRenderer in remoteBodyRenderers)
            {
                if (bodyRenderer != null)
                {
                    // 第一人称拥有者隐藏自己的临时胶囊体，其他玩家仍可看到它。
                    bodyRenderer.enabled = !ownsPlayer;
                }
            }

            if (ownerOnlyBehaviours == null)
            {
                return;
            }

            foreach (Behaviour ownerOnlyBehaviour in ownerOnlyBehaviours)
            {
                if (ownerOnlyBehaviour != null)
                {
                    ownerOnlyBehaviour.enabled = ownsPlayer;
                }
            }
        }

        public static Color GetPlayerColor(ulong clientId)
        {
            Color[] colors =
            {
                new Color(0.2f, 0.65f, 1f),
                new Color(1f, 0.42f, 0.2f),
                new Color(0.35f, 0.9f, 0.4f),
                new Color(0.85f, 0.35f, 1f)
            };
            return colors[clientId % (ulong)colors.Length];
        }

        private void ApplyPlayerColor(ulong clientId)
        {
            if (remoteBodyRenderers == null)
            {
                return;
            }

            var propertyBlock = new MaterialPropertyBlock();
            propertyBlock.SetColor("_BaseColor", GetPlayerColor(clientId));
            propertyBlock.SetColor("_Color", GetPlayerColor(clientId));
            foreach (Renderer bodyRenderer in remoteBodyRenderers)
            {
                bodyRenderer?.SetPropertyBlock(propertyBlock);
            }
        }
    }
}
