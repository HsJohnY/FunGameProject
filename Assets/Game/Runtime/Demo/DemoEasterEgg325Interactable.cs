using FunGame.Interaction;
using UnityEngine;

namespace FunGame.Demo
{
    /// <summary>
    /// 藏在旧维修区域的 325 编号铭牌；只记录发现，不改变主线数值或门禁。
    /// </summary>
    public sealed class DemoEasterEgg325Interactable : MonoBehaviour, IContextInteractable
    {
        [SerializeField] private SinglePlayerDemoController campaign;
        [SerializeField] private Renderer plateRenderer;
        private bool _discovered;
        private MaterialPropertyBlock _propertyBlock;

        public void Configure(SinglePlayerDemoController configuredCampaign)
        {
            campaign = configuredCampaign;
        }

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            if (plateRenderer == null)
            {
                plateRenderer = GetComponent<Renderer>();
            }
        }

        public InteractionOption GetInteractionOption(ContextInteractor actor)
        {
            return new InteractionOption(
                "archive-plate-325",
                _discovered ? "维修队 325 号旧铭牌" : "积灰的旧维修铭牌",
                _discovered ? "查看记录" : "擦拭铭牌",
                InteractionPriority.Device,
                !_discovered,
                "铭牌背面刻着：325 · 风暴里总有人留下一盏灯");
        }

        public bool ExecuteInteraction(ContextInteractor actor)
        {
            if (_discovered)
            {
                return false;
            }

            _discovered = true;
            if (plateRenderer != null)
            {
                plateRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor("_BaseColor", new Color(1f, 0.72f, 0.18f));
                plateRenderer.SetPropertyBlock(_propertyBlock);
            }

            campaign?.DiscoverEasterEgg325();
            Debug.Log("[Demo] secret=325 discovered=true", this);
            return true;
        }
    }
}
