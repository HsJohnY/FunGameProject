using System;
using System.Collections;
using System.Collections.Generic;
using FunGame.Content;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FunGame.Demo
{
    /// <summary>
    /// 所有客户端在可进入会话之前加载相同的静态环境。此处不承载 NetworkObject，
    /// 游戏对象仍由会话主机生成；无需依赖客户端之间的场景加载顺序。
    /// </summary>
    public sealed class ExpeditionEnvironmentLoader : MonoBehaviour
    {
        [SerializeField] private EnvironmentSceneDefinition[] modules = Array.Empty<EnvironmentSceneDefinition>();
        private readonly List<Scene> _ownedScenes = new List<Scene>();
        private readonly Dictionary<int, List<GameObject>> _chapterRoots = new Dictionary<int, List<GameObject>>();
        private bool _quitting;
        public bool IsReady { get; private set; }
        public IReadOnlyList<EnvironmentSceneDefinition> Modules => modules;
        public void Configure(EnvironmentSceneDefinition[] value) => modules = value;

        public IEnumerator Load()
        {
            foreach (EnvironmentSceneDefinition module in modules)
            {
                if (module == null || !Application.CanStreamedLevelBeLoaded(module.ScenePath))
                    throw new InvalidOperationException("Missing environment scene in build settings.");
                // Tests can unload the gameplay scene additively. Finish its pending cleanup
                // before acquiring another copy of the same environment.
                Scene scene = SceneManager.GetSceneByPath(module.ScenePath);
                while (scene.IsValid() && !scene.isLoaded)
                {
                    yield return null;
                    scene = SceneManager.GetSceneByPath(module.ScenePath);
                }
                if (!scene.IsValid())
                {
                    yield return SceneManager.LoadSceneAsync(module.ScenePath, LoadSceneMode.Additive);
                    scene = SceneManager.GetSceneByPath(module.ScenePath);
                    _ownedScenes.Add(scene);
                }
                if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Environment failed to load: " + module.ScenePath);
                if (!_chapterRoots.TryGetValue(module.Chapter, out List<GameObject> roots))
                    _chapterRoots.Add(module.Chapter, roots = new List<GameObject>());
                roots.AddRange(scene.GetRootGameObjects());
            }
            IsReady = true;
        }

        public void SetVisibility(bool relayVisible, bool stormVisible)
        {
            foreach (var entry in _chapterRoots)
                foreach (GameObject root in entry.Value)
                    if (root != null) root.SetActive(entry.Key == 0 || (entry.Key == 1 ? relayVisible : stormVisible));
        }

        private void OnApplicationQuit() => _quitting = true;
        private void OnDestroy()
        {
            if (_quitting || !Application.isPlaying) return;
            foreach (Scene scene in _ownedScenes)
                if (scene.IsValid() && scene.isLoaded) SceneManager.UnloadSceneAsync(scene);
        }
    }
}
