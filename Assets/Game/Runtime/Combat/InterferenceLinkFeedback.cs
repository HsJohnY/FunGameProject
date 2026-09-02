using UnityEngine;

namespace FunGame.Combat
{
    /// <summary>
    /// 蓄力期间在干扰体和设备之间绘制跳动连线，明确伤害来源。
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class InterferenceLinkFeedback : MonoBehaviour
    {
        [SerializeField] private InterferenceEnemy enemy;
        [SerializeField] private DefendableSystemTarget target;
        private LineRenderer _line;

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.positionCount = 3;
            _line.useWorldSpace = true;
            _line.startWidth = 0.045f;
            _line.endWidth = 0.02f;
            _line.enabled = false;
        }

        private void LateUpdate()
        {
            if (_line == null || enemy == null || target == null)
            {
                return;
            }

            bool visible = enemy.IsEncounterActive && enemy.IsTelegraphing;
            _line.enabled = visible;
            if (!visible)
            {
                return;
            }

            Vector3 start = enemy.transform.position + Vector3.up * 0.35f;
            Vector3 end = target.transform.position + Vector3.up * 0.4f;
            Vector3 midpoint = Vector3.Lerp(start, end, 0.5f);
            midpoint += (enemy.transform.right * Mathf.Sin(Time.unscaledTime * 45f) + Vector3.up) * 0.13f;
            _line.SetPosition(0, start);
            _line.SetPosition(1, midpoint);
            _line.SetPosition(2, end);
        }

        public void Configure(InterferenceEnemy configuredEnemy, DefendableSystemTarget configuredTarget, Material lineMaterial)
        {
            enemy = configuredEnemy;
            target = configuredTarget;
            if (_line == null)
            {
                _line = GetComponent<LineRenderer>();
            }

            if (lineMaterial != null)
            {
                _line.sharedMaterial = lineMaterial;
            }
        }
    }
}
