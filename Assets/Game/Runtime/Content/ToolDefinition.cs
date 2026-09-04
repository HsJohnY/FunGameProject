using FunGame.Tools;
using UnityEngine;

namespace FunGame.Content
{
    [CreateAssetMenu(menuName = "FunGame/Content/Tool Definition")]
    public sealed class ToolDefinition : ScriptableObject
    {
        [SerializeField] private ToolKind kind;
        [SerializeField, Min(0.05f)] private float cooldownSeconds = 0.38f;
        public ToolKind Kind => kind;
        public float CooldownSeconds => cooldownSeconds;
    }
}
