using UnityEditor;
using UnityEngine;

namespace FunGame.Editor.Collaboration
{
    /// <summary>Explicit, idempotent migration from the old network cylinder to the authored pipe visual.</summary>
    public static class RepairReplacementPipeVisual
    {
        private const string SoloPath = "Assets/Game/Content/Modules/Entities/Replacement-Pipe.prefab";
        private const string NetworkPath = "Assets/Game/Content/Networking/M4_ReplacementPipe.prefab";
        private const string VisualPath = "Assets/Game/Content/Modules/Art/Replacement-Pipe-Visual.prefab";

        [MenuItem("FunGame/Collaboration/Repair Replacement Pipe Visual")]
        public static void Run()
        {
            GameObject solo = PrefabUtility.LoadPrefabContents(SoloPath);
            GameObject network = PrefabUtility.LoadPrefabContents(NetworkPath);
            try
            {
                Transform source = solo.transform.Find("Replacement Pipe Model");
                GameObject visual = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPath);
                bool firstMigration = visual == null;
                if (visual == null)
                {
                    GameObject copy = Object.Instantiate(source.gameObject);
                    copy.name = "Replacement Pipe Visual";
                    copy.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    copy.transform.localScale = Vector3.one;
                    visual = PrefabUtility.SaveAsPrefabAsset(copy, VisualPath);
                    Object.DestroyImmediate(copy);
                }
                if (source.childCount != 1 || PrefabUtility.GetCorrespondingObjectFromSource(source.GetChild(0).gameObject) != visual)
                {
                    if (!firstMigration) throw new System.InvalidOperationException("The local pipe visual was edited after migration; merge it explicitly before running repair.");
                    while (source.childCount > 0) Object.DestroyImmediate(source.GetChild(0).gameObject);
                    PrefabUtility.InstantiatePrefab(visual, source);
                    PrefabUtility.SaveAsPrefabAsset(solo, SoloPath);
                }
                if (network.transform.Find("Pipe Visual Scale") == null)
                {
                    var container = new GameObject("Pipe Visual Scale").transform;
                    container.SetParent(network.transform, false);
                    Vector3 scale = network.transform.localScale;
                    container.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(visual, container);
                    instance.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                    network.GetComponent<MeshRenderer>().enabled = false;
                    PrefabUtility.SaveAsPrefabAsset(network, NetworkPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(solo);
                PrefabUtility.UnloadPrefabContents(network);
            }
        }
    }
}
