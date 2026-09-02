using UnityEngine;

namespace FunGame.Combat
{
    /// <summary>
    /// 以三段世界空间指示灯表达设备完整度，避免再增加传统血条。
    /// </summary>
    public sealed class DefendableSystemIndicator : MonoBehaviour
    {
        [SerializeField] private DefendableSystemTarget target;
        [SerializeField] private Renderer[] segments;
        private MaterialPropertyBlock _propertyBlock;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            if (target != null)
            {
                target.IntegrityChanged += HandleIntegrityChanged;
                Refresh(target.Integrity, target.MaxIntegrity);
            }
        }

        private void OnDisable()
        {
            if (target != null)
            {
                target.IntegrityChanged -= HandleIntegrityChanged;
            }
        }

        public void Configure(DefendableSystemTarget configuredTarget, Renderer[] configuredSegments)
        {
            target = configuredTarget;
            segments = configuredSegments;
            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }

            if (target != null)
            {
                Refresh(target.Integrity, target.MaxIntegrity);
            }
        }

        private void HandleIntegrityChanged(int integrity, int maximum)
        {
            Refresh(integrity, maximum);
        }

        private void Refresh(int integrity, int maximum)
        {
            if (segments == null || segments.Length == 0)
            {
                return;
            }

            float ratio = maximum <= 0 ? 0f : (float)integrity / maximum;
            int activeCount = Mathf.CeilToInt(ratio * segments.Length);
            Color activeColor = ratio > 0.66f
                ? new Color(0.1f, 1f, 0.45f)
                : ratio > 0.33f ? new Color(1f, 0.7f, 0.05f) : new Color(1f, 0.08f, 0.04f);
            for (int index = 0; index < segments.Length; index++)
            {
                Renderer segment = segments[index];
                if (segment == null)
                {
                    continue;
                }

                segment.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor("_BaseColor", index < activeCount ? activeColor : new Color(0.04f, 0.04f, 0.04f));
                segment.SetPropertyBlock(_propertyBlock);
            }
        }
    }
}
