using UnityEngine;

namespace FunGame.Combat
{
    /// <summary>
    /// 只对本地相机叠加轻微位移震动，不改变角色朝向或网络状态。
    /// </summary>
    public sealed class CombatCameraFeedback : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField, Min(0.01f)] private float duration = 0.12f;
        [SerializeField, Min(0f)] private float amplitude = 0.025f;
        private Vector3 _restLocalPosition;
        private float _remaining;

        private void Awake()
        {
            if (cameraTransform == null)
            {
                Camera camera = GetComponentInChildren<Camera>(true);
                cameraTransform = camera != null ? camera.transform : null;
            }

            if (cameraTransform != null)
            {
                _restLocalPosition = cameraTransform.localPosition;
            }
        }

        private void LateUpdate()
        {
            if (cameraTransform == null)
            {
                return;
            }

            if (_remaining <= 0f)
            {
                cameraTransform.localPosition = _restLocalPosition;
                return;
            }

            _remaining = Mathf.Max(0f, _remaining - Time.unscaledDeltaTime);
            float strength = duration <= 0f ? 0f : _remaining / duration;
            cameraTransform.localPosition = _restLocalPosition + (Vector3)Random.insideUnitCircle * (amplitude * strength);
        }

        private void OnDisable()
        {
            if (cameraTransform != null)
            {
                cameraTransform.localPosition = _restLocalPosition;
            }
        }

        public void Configure(Transform configuredCamera)
        {
            cameraTransform = configuredCamera;
            if (cameraTransform != null)
            {
                _restLocalPosition = cameraTransform.localPosition;
            }
        }

        public void Play()
        {
            _remaining = duration;
        }
    }
}
