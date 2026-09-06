using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FunGame.Editor
{
    /// <summary>Explicit visual-only kit import. Never called by ordinary builds.</summary>
    public static class MaintenanceEquipmentArt
    {
        public const string Kit = "Assets/Game/Content/Modules/Art/Equipment/";
        const string Modules = "Assets/Game/Content/Modules/";
        const string InstalledName = "Maintenance Equipment Visual";
        static readonly string[] Models = { "ImpactWrench", "SealantGun", "CircuitBridger", "CoolingPump",
            "ControlConsole", "StormRelay", "ReplacementPipe", "PressureGauge", "ToolDock", "InspectionPanel" };

        [MenuItem("Fun Game/Art/Import Maintenance Equipment")]
        public static void Import()
        {
            AssetDatabase.Refresh();
            string materialPath = Kit + "MaintenancePalette.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                var textureImporter = (TextureImporter)AssetImporter.GetAtPath(Kit + "MaintenancePalette.tga");
                textureImporter.filterMode = FilterMode.Point;
                textureImporter.mipmapEnabled = false;
                textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
                textureImporter.SaveAndReimport();
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.name = "Maintenance Palette";
                material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Kit + "MaintenancePalette.tga"));
                material.SetColor("_BaseColor", Color.white);
                material.SetFloat("_Metallic", .25f);
                material.SetFloat("_Smoothness", .32f);
                material.enableInstancing = true;
                AssetDatabase.CreateAsset(material, materialPath);
            }
            foreach (string model in Models)
            {
                string prefabPath = Kit + model + ".prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) continue;
                var importer = (ModelImporter)AssetImporter.GetAtPath(Kit + model + ".obj");
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.importNormals = ModelImporterNormals.Import;
                importer.importTangents = ModelImporterTangents.None;
                importer.isReadable = false;
                importer.meshCompression = ModelImporterMeshCompression.Off;
                importer.addCollider = false;
                importer.SaveAndReimport();
                Mesh mesh = AssetDatabase.LoadAllAssetsAtPath(Kit + model + ".obj").OfType<Mesh>().Single();
                var root = new GameObject(model);
                root.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = root.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                UnityEngine.Object.DestroyImmediate(root);
            }
            foreach (string player in new[] { "SoloTools", "NetworkTools" })
                Edit("Art/Player/" + player, root =>
                {
                    string[] anchors = { "Impact Wrench Visual", "Sealant Gun Visual", "Circuit Bridger Visual" };
                    bool changed = false;
                    for (int i = 0; i < 3; i++)
                    {
                        Transform anchor = root.GetComponentsInChildren<Transform>(true).Single(t => t.name == anchors[i]);
                        changed |= Attach(anchor, Models[i], Vector3.zero, Vector3.one, Quaternion.identity, r => true);
                    }
                    return changed;
                });

            Entity("Modular-Cooling-Pump", "CoolingPump", new Vector3(0,0,5.8f), Vector3.one,
                r => r.name != "Pump Status Core");
            Entity("Interactive-Control-Console", "ControlConsole", Vector3.zero, Vector3.one);
            foreach (string name in new[] { "Power-Compartment-Recovery-Console", "Storm-Core-Calibration-Console" })
                Entity(name, "ControlConsole", Vector3.zero, Vector3.one,
                    r => !r.name.StartsWith("Calibration Stage Light", StringComparison.Ordinal) && r.name != "Calibration Console Screen");
            for (int i = 1; i <= 5; i++)
                Entity("Storm-Relay-" + i, "StormRelay", Vector3.zero, Vector3.one);
            EntityUnscaled("Replacement-Pipe", "ReplacementPipe");
            EntityUnscaled("Diagnostic-Pressure-Gauge", "PressureGauge");
            EntityUnscaled("Cooling-Pump-Inspection-Panel", "InspectionPanel");

            // Dock width follows the existing wall-normal X axis. Existing colliders and IDs stay intact.
            string[] racks = { "Impact-Wrench-Rack", "Sealant-Gun-Rack", "Circuit-Bridger-Rack",
                "Relay-Wrench-Station", "Relay-Sealant-Station", "Relay-Bridger-Station",
                "Storm-Wrench-Station", "Storm-Sealant-Station", "Storm-Bridger-Station" };
            foreach (string name in racks)
                Edit("Entities/" + name, root =>
                {
                    if (root.transform.Find(InstalledName) != null) return false;
                    // Rotate normalized dock so its shallow dimension follows the wall-facing X axis.
                    Attach(root.transform, "ToolDock", Vector3.zero, Vector3.one, Quaternion.Euler(0,90,0), r => true);
                    Transform dock = root.transform.Find(InstalledName);
                    string model = name.Contains("Wrench") ? "ImpactWrench" : name.Contains("Sealant") ? "SealantGun" : "CircuitBridger";
                    var tool = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Kit + model + ".prefab"), root.scene);
                    tool.name = "Docked Tool Display";
                    tool.transform.SetParent(dock, false);
                    tool.transform.localPosition = new Vector3(0,.05f,-.24f);
                    tool.transform.localRotation = Quaternion.Euler(0,180,0);
                    tool.transform.localScale = new Vector3(.75f,.65f,.65f);
                    return true;
                });

            // These decorations are the old visual duplicates of the interactive equipment above.
            Edit("Art/Environment/CoolingBay", root => Disable(root,
                r => r.name.StartsWith("Console ", StringComparison.Ordinal) || r.name.StartsWith("Rack ", StringComparison.Ordinal)));
            Edit("Art/Environment/PowerRelay", root => Disable(root, r => r.name.Contains(" Station ")));
            Edit("Art/Environment/StormCore", root => Disable(root, r => r.name.Contains(" Station ")));
            for (int i = 1; i <= 5; i++)
                Edit("Entities/Storm-Relay-" + i, root =>
                {
                    float side = root.transform.localPosition.x < 0 ? -1 : 1;
                    Transform visual = root.transform.Find(InstalledName);
                    Quaternion rotation = Quaternion.Euler(0, side * 90, 0);
                    bool changed = Quaternion.Angle(visual.localRotation, rotation) > .01f;
                    if (changed) visual.localRotation = rotation;
                    string meshPath = Kit + (side < 0 ? "RelayStatusLeft.asset" : "RelayStatusRight.asset");
                    Mesh indicator = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    if (indicator == null)
                    {
                        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        indicator = UnityEngine.Object.Instantiate(cube.GetComponent<MeshFilter>().sharedMesh);
                        UnityEngine.Object.DestroyImmediate(cube);
                        indicator.name = "Relay state indicator";
                        Vector3[] vertices = indicator.vertices;
                        for (int n = 0; n < vertices.Length; n++)
                            vertices[n] = Vector3.Scale(vertices[n], new Vector3(.03f,.055f,.32f)) + new Vector3(-side*.475f,.38f,0);
                        indicator.vertices = vertices;
                        indicator.RecalculateBounds();
                        AssetDatabase.CreateAsset(indicator, meshPath);
                    }
                    var filter = root.GetComponent<MeshFilter>();
                    var renderer = root.GetComponent<MeshRenderer>();
                    if (filter.sharedMesh != indicator || !renderer.enabled)
                    {
                        filter.sharedMesh = indicator;
                        renderer.enabled = true;
                        changed = true;
                    }
                    return changed;
                });
            AssetDatabase.SaveAssets();
            Validate();
            Debug.Log("[MaintenanceEquipment] Import and reference validation PASS");
        }

        static void Entity(string name, string model, Vector3 position, Vector3 scale, Func<Renderer,bool> hide = null)
        {
            Edit("Entities/" + name, root => Attach(root.transform, model, position, scale, Quaternion.identity, hide ?? (r => true)));
        }

        static void EntityUnscaled(string name, string model)
        {
            Edit("Entities/" + name, root =>
            {
                Vector3 s = root.transform.localScale;
                return Attach(root.transform, model, Vector3.zero, new Vector3(1/s.x,1/s.y,1/s.z), Quaternion.identity, r => true);
            });
        }

        static bool Attach(Transform parent, string model, Vector3 position, Vector3 scale, Quaternion rotation, Func<Renderer,bool> hide)
        {
            if (parent.Find(InstalledName) != null) return false;
            foreach (Renderer renderer in parent.GetComponentsInChildren<Renderer>(true))
                if (hide(renderer)) renderer.enabled = false;
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Kit + model + ".prefab"), parent.gameObject.scene);
            visual.name = InstalledName;
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = position;
            visual.transform.localRotation = rotation;
            visual.transform.localScale = scale;
            return true;
        }

        static bool Disable(GameObject root, Func<Renderer,bool> predicate)
        {
            bool changed = false;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                if (renderer.enabled && predicate(renderer)) { renderer.enabled = false; changed = true; }
            return changed;
        }

        static void Edit(string relativePath, Func<GameObject,bool> edit)
        {
            string path = Modules + relativePath + ".prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try { if (edit(root)) PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        public static void Validate()
        {
            foreach (string model in Models)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Kit + model + ".prefab");
                if (prefab == null || prefab.GetComponentsInChildren<Renderer>().Length != 1 ||
                    prefab.GetComponentsInChildren<Collider>().Length != 0 || prefab.GetComponentsInChildren<MonoBehaviour>().Length != 0)
                    throw new InvalidOperationException("Invalid visual-only kit prefab: " + model);
                Mesh mesh = prefab.GetComponent<MeshFilter>().sharedMesh;
                if (mesh == null || mesh.vertexCount == 0 || mesh.subMeshCount != 1 || mesh.GetIndexCount(0) / 3 > 3500)
                    throw new InvalidOperationException("Missing or over-budget mesh: " + model);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { Modules + "Entities", Modules + "Art" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (Component component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component == null) throw new InvalidOperationException("Missing script: " + path);
                    var serialized = new SerializedObject(component);
                    var property = serialized.GetIterator();
                    while (property.Next(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference &&
                            property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0)
                            throw new InvalidOperationException("Broken reference: " + path + " " + property.propertyPath);
                }
            }
        }
    }
}
