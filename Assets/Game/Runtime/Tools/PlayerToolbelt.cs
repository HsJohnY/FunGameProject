using UnityEngine;

namespace FunGame.Tools
{
    /// <summary>
    /// 管理玩家唯一主工具位和对应的第一人称占位模型。
    /// </summary>
    public sealed class PlayerToolbelt : MonoBehaviour
    {
        [SerializeField] private GameObject impactWrenchVisual;
        [SerializeField] private GameObject sealantGunVisual;
        [SerializeField] private GameObject circuitBridgerVisual;

        public ToolKind EquippedTool { get; private set; }

        /// <summary>
        /// 由灰盒场景生成器绑定两种互斥的第一人称工具占位模型。
        /// </summary>
        public void ConfigureVisuals(GameObject wrenchVisual, GameObject sealantVisual)
        {
            ConfigureVisuals(wrenchVisual, sealantVisual, null);
        }

        /// <summary>
        /// 绑定三种 MVP 主工具的互斥第一人称占位模型。
        /// </summary>
        public void ConfigureVisuals(
            GameObject wrenchVisual,
            GameObject sealantVisual,
            GameObject bridgerVisual)
        {
            impactWrenchVisual = wrenchVisual;
            sealantGunVisual = sealantVisual;
            circuitBridgerVisual = bridgerVisual;
            RefreshVisuals();
        }

        /// <summary>
        /// 替换主工具位内容；灰盒工具架提供无限次换取，不创建背包库存。
        /// </summary>
        public bool Equip(ToolKind tool)
        {
            if (tool == ToolKind.None || tool == EquippedTool)
            {
                return false;
            }

            EquippedTool = tool;
            RefreshVisuals();
            Debug.Log($"[Tool] equipped={tool} name={tool.GetDisplayName()}", this);
            return true;
        }

        /// <summary>
        /// 清空主工具位。空手时重复调用不会产生副作用。
        /// </summary>
        public bool Unequip()
        {
            if (EquippedTool == ToolKind.None)
            {
                return false;
            }

            ToolKind returnedTool = EquippedTool;
            EquippedTool = ToolKind.None;
            RefreshVisuals();
            Debug.Log($"[Tool] returned={returnedTool} equipped=None", this);
            return true;
        }

        private void RefreshVisuals()
        {
            if (impactWrenchVisual != null)
            {
                impactWrenchVisual.SetActive(EquippedTool == ToolKind.ImpactWrench);
            }

            if (sealantGunVisual != null)
            {
                sealantGunVisual.SetActive(EquippedTool == ToolKind.SealantGun);
            }

            if (circuitBridgerVisual != null)
            {
                circuitBridgerVisual.SetActive(EquippedTool == ToolKind.CircuitBridger);
            }
        }
    }
}
