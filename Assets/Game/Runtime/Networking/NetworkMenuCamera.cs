using Unity.Netcode;
using UnityEngine;

namespace FunGame.Networking
{
    /// <summary>会话开始前为模式菜单保留场景画面，生成本地玩家后让出摄像机和监听器。</summary>
    [RequireComponent(typeof(Camera), typeof(AudioListener))]
    public sealed class NetworkMenuCamera : MonoBehaviour
    {
        private Camera _camera;
        private AudioListener _listener;
        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _listener = GetComponent<AudioListener>();
        }
        private void LateUpdate()
        {
            NetworkManager manager = NetworkManager.Singleton;
            bool hasPlayer = manager != null && manager.IsClient && manager.LocalClient?.PlayerObject != null;
            _camera.enabled = !hasPlayer;
            _listener.enabled = !hasPlayer;
        }
    }
}
