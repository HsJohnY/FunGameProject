using FunGame.Player;
using Unity.Netcode;
using UnityEngine;

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

        private void Awake()
        {
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
