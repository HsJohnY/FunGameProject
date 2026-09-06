using UnityEditor;
using UnityEngine;

namespace FunGame.Editor.Collaboration
{
    /// <summary>Explicit, idempotent migration from the old network cylinder to the official pipe visual.</summary>
    public static class RepairReplacementPipeVisual
    {
        private const string NetworkPath = "Assets/Game/Content/Networking/M4_ReplacementPipe.prefab";
        private const string VisualPath = "Assets/Game/Content/Modules/Art/Equipment/ReplacementPipe.prefab";

        [MenuItem("FunGame/Collaboration/Repair Replacement Pipe Visual")]
        public static void Run()
        {
            GameObject network = PrefabUtility.LoadPrefabContents(NetworkPath);
            try
            {
                GameObject visual = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPath);
                if (visual == null)
                    throw new System.InvalidOperationException($"Missing official pipe visual at {VisualPath}.");

                bool changed = false;
                Transform container = network.transform.Find("Pipe Visual Scale");
                if (container == null)
                {
                    container = new GameObject("Pipe Visual Scale").transform;
                    container.SetParent(network.transform, false);
                    changed = true;
                }

                Vector3 rootScale = network.transform.localScale;
                Vector3 requiredScale = new(1f / rootScale.x, 1f / rootScale.y, 1f / rootScale.z);
                if (container.localPosition.sqrMagnitude > 0.000001f ||
                    Quaternion.Angle(container.localRotation, Quaternion.identity) > 0.001f ||
                    (container.localScale - requiredScale).sqrMagnitude > 0.000001f)
                {
                    container.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                    container.localScale = requiredScale;
                    changed = true;
                }

                GameObject instance = container.childCount == 1 ? container.GetChild(0).gameObject : null;
                if (instance == null || PrefabUtility.GetCorrespondingObjectFromSource(instance) != visual)
                {
                    while (container.childCount > 0)
                        Object.DestroyImmediate(container.GetChild(0).gameObject);
                    instance = (GameObject)PrefabUtility.InstantiatePrefab(visual, container);
                    changed = true;
                }

                Quaternion requiredRotation = Quaternion.Euler(-90f, 0f, 0f);
                if (instance.transform.localPosition.sqrMagnitude > 0.000001f ||
                    Quaternion.Angle(instance.transform.localRotation, requiredRotation) > 0.001f ||
                    (instance.transform.localScale - Vector3.one).sqrMagnitude > 0.000001f)
                {
                    instance.transform.localPosition = Vector3.zero;
                    instance.transform.localRotation = requiredRotation;
                    instance.transform.localScale = Vector3.one;
                    changed = true;
                }

                MeshRenderer legacyRenderer = network.GetComponent<MeshRenderer>();
                if (legacyRenderer != null && legacyRenderer.enabled)
                {
                    legacyRenderer.enabled = false;
                    changed = true;
                }

                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(network, NetworkPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(network);
            }
        }
    }
}
