using System;
using UnityEngine;

namespace FunGame.Content
{
    [CreateAssetMenu(menuName = "FunGame/Content/Encounter Definition")]
    public sealed class EncounterDefinition : ScriptableObject
    {
        [Serializable]
        public struct Deployment
        {
            public string enemyId;
            [Min(0f)] public float delaySeconds;
        }
        [SerializeField, TextArea] private string briefing;
        [SerializeField] private Deployment[] deployments = Array.Empty<Deployment>();
        public string Briefing => briefing;
        public float GetDelay(string enemyId, float fallback)
        {
            foreach (Deployment deployment in deployments)
                if (deployment.enemyId == enemyId) return deployment.delaySeconds;
            return fallback;
        }
    }
}
