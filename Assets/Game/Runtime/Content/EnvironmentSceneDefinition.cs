using UnityEngine;

namespace FunGame.Content
{
    [CreateAssetMenu(menuName = "FunGame/Content/Environment Scene")]
    public sealed class EnvironmentSceneDefinition : ScriptableObject
    {
        [SerializeField] private string scenePath;
        [SerializeField] private GameObject environmentPrefab;
        [SerializeField, Range(0, 2)] private int chapter;
        public string ScenePath => scenePath;
        public GameObject EnvironmentPrefab => environmentPrefab;
        public int Chapter => chapter;
    }
}
