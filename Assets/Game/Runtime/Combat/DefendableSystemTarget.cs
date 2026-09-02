using UnityEngine;

namespace FunGame.Combat
{
    /// <summary>
    /// 将纯设备完整度规则适配到场景对象，并以本体颜色表达受干扰程度。
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class DefendableSystemTarget : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maxIntegrity = 60;
        [SerializeField] private Color healthyColor = new Color(0.1f, 0.8f, 0.55f);
        private DefendableSystemRules _rules;
        private Renderer _renderer;
        private MaterialPropertyBlock _propertyBlock;

        public int Integrity => _rules?.Integrity ?? maxIntegrity;
        public int MaxIntegrity => _rules?.MaxIntegrity ?? maxIntegrity;
        public bool IsOffline => _rules != null && _rules.IsOffline;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _propertyBlock = new MaterialPropertyBlock();
            _rules = new DefendableSystemRules(maxIntegrity);
            RefreshVisual();
        }

        /// <summary>
        /// 配置首个切片的设备耐受值；仅用于场景生成器和自动测试。
        /// </summary>
        public void Configure(int configuredMaxIntegrity)
        {
            maxIntegrity = Mathf.Max(1, configuredMaxIntegrity);
            _rules = new DefendableSystemRules(maxIntegrity);
            RefreshVisual();
        }

        public bool ApplyInterference(int damage)
        {
            EnsureRules();
            bool wentOffline = _rules.ApplyInterference(damage);
            RefreshVisual();
            Debug.Log($"[Combat] target=defense-system action=interference integrity={Integrity}/{MaxIntegrity} offline={wentOffline}", this);
            return wentOffline;
        }

        public void ResetSystem()
        {
            EnsureRules();
            _rules.Reset();
            RefreshVisual();
        }

        private void EnsureRules()
        {
            if (_rules == null)
            {
                _rules = new DefendableSystemRules(maxIntegrity);
            }
        }

        private void RefreshVisual()
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<Renderer>();
            }

            if (_renderer == null)
            {
                return;
            }

            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }

            float ratio = MaxIntegrity <= 0 ? 0f : (float)Integrity / MaxIntegrity;
            Color currentColor = ratio > 0.5f
                ? Color.Lerp(new Color(1f, 0.7f, 0.1f), healthyColor, (ratio - 0.5f) * 2f)
                : Color.Lerp(new Color(0.65f, 0.05f, 0.05f), new Color(1f, 0.7f, 0.1f), ratio * 2f);
            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor("_BaseColor", currentColor);
            _renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
