using UnityEngine;

namespace FunGame.Demo
{
    /// <summary>
    /// 用同一组低模装置的旋转、脉冲和辅助色表达章节升级，不为每个事件制作独立高成本资产。
    /// </summary>
    public sealed class DemoChapterPresentation : MonoBehaviour
    {
        [SerializeField] private SinglePlayerDemoController campaign;
        [SerializeField] private Transform[] movingParts;
        [SerializeField] private Renderer[] signalRenderers;
        [SerializeField] private Light[] workLights;
        private MaterialPropertyBlock _propertyBlock;
        private Vector3[] _baseScales;

        public void Configure(
            SinglePlayerDemoController configuredCampaign,
            Transform[] configuredMovingParts,
            Renderer[] configuredSignals,
            Light[] configuredWorkLights)
        {
            campaign = configuredCampaign;
            movingParts = configuredMovingParts;
            signalRenderers = configuredSignals;
            workLights = configuredWorkLights;
            CaptureBaseScales();
        }

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            CaptureBaseScales();
        }

        private void Update()
        {
            if (campaign == null)
            {
                return;
            }

            float speed = campaign.Chapter == SinglePlayerDemoChapter.StormCalibration ? 95f : 32f;
            if (movingParts != null)
            {
                foreach (Transform part in movingParts)
                {
                    part?.Rotate(Vector3.up, speed * Time.deltaTime, Space.Self);
                }
            }

            Color signalColor = GetChapterColor(campaign.Chapter);
            float pulse = 0.82f + Mathf.Sin(Time.unscaledTime * (campaign.IsCompleted ? 2f : 5f)) * 0.18f;
            if (signalRenderers != null)
            {
                foreach (Renderer signal in signalRenderers)
                {
                    if (signal == null) continue;
                    signal.GetPropertyBlock(_propertyBlock);
                    _propertyBlock.SetColor("_BaseColor", signalColor * pulse);
                    signal.SetPropertyBlock(_propertyBlock);
                }
            }

            if (workLights != null)
            {
                foreach (Light workLight in workLights)
                {
                    if (workLight == null) continue;
                    workLight.color = Color.Lerp(workLight.color, signalColor, Time.deltaTime * 1.8f);
                    workLight.intensity = Mathf.Lerp(workLight.intensity, campaign.IsCompleted ? 4f : 5.5f, Time.deltaTime * 2f);
                }
            }

            if (_baseScales != null && movingParts != null)
            {
                for (int index = 0; index < Mathf.Min(_baseScales.Length, movingParts.Length); index++)
                {
                    if (movingParts[index] != null)
                    {
                        movingParts[index].localScale = _baseScales[index] * (1f + pulse * 0.025f);
                    }
                }
            }
        }

        private void CaptureBaseScales()
        {
            if (movingParts == null)
            {
                return;
            }

            _baseScales = new Vector3[movingParts.Length];
            for (int index = 0; index < movingParts.Length; index++)
            {
                _baseScales[index] = movingParts[index] != null ? movingParts[index].localScale : Vector3.one;
            }
        }

        private static Color GetChapterColor(SinglePlayerDemoChapter chapter)
        {
            switch (chapter)
            {
                case SinglePlayerDemoChapter.CoolingEmergency: return new Color(1f, 0.45f, 0.08f);
                case SinglePlayerDemoChapter.RelaySurge: return new Color(0.75f, 0.18f, 0.95f);
                case SinglePlayerDemoChapter.StormCalibration: return new Color(0.15f, 0.72f, 1f);
                default: return new Color(0.2f, 1f, 0.62f);
            }
        }
    }
}
