using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FunGame.Combat;
using FunGame.Content;
using FunGame.Demo;
using FunGame.Incident;
using FunGame.Interaction;
using FunGame.Networking;
using FunGame.Player;
using FunGame.Tools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace FunGame.Editor
{
    /// <summary>
    /// 一次性将已验收地图迁移为人工维护的模块。迁移后拒绝再次覆盖源 Prefab；
    /// 日常编辑直接使用 Prefab Mode 和配置资产，不再从旧灰盒生成器派生。
    /// </summary>
    public static class CollaborationMigration
    {
        private const string Folder = ModularContentBuilder.ContentFolder;
        private sealed class Reference
        {
            public Component Owner;
            public string Property;
            public Object Target;
        }

        [MenuItem("FunGame/Modules/Migrate Canonical Map Once")]
        public static void Migrate()
        {
            if (Directory.Exists(Folder))
                throw new InvalidOperationException("Module sources already exist; migration will not overwrite them.");
            Scene scene = EditorSceneManager.OpenScene(SinglePlayerDemoBootstrap.ScenePath, OpenSceneMode.Single);
            GameObject[] roots = scene.GetRootGameObjects();
            var mode = roots.SelectMany(r => r.GetComponentsInChildren<SharedMapModeController>(true)).Single();
            Transform map = mode.MapRoot.transform;
            List<Reference> references = CaptureReferences(roots);
            var protectedObjects = new HashSet<Object>(references.Select(r => r.Target));
            EnsureFolder(Folder);
            EnsureFolder(Folder + "/Configuration");

            var wrench = CreateTool("ImpactWrench", ToolKind.ImpactWrench, 0.38f);
            var bridger = CreateTool("CircuitBridger", ToolKind.CircuitBridger, 0.8f);
            var incident = map.GetComponentsInChildren<CoolingIncidentController>(true).Single();
            var incidentDefinition = CreateDefinition<CoolingIncidentDefinition>("Incidents/Cooling", incident);
            incident.ConfigureDefinition(incidentDefinition);
            InterferenceEnemy[] enemies = map.GetComponentsInChildren<InterferenceEnemy>(true)
                .OrderBy(e => e.TargetId, StringComparer.Ordinal).ToArray();
            if (enemies.Select(e => e.TargetId).Distinct().Count() != enemies.Length)
                throw new InvalidDataException("Enemy IDs must be unique before migration.");
            foreach (InterferenceEnemy enemy in enemies)
                enemy.ConfigureDefinition(CreateDefinition<EnemyDefinition>("Enemies/" + Slug(enemy.TargetId), enemy));
            foreach (CombatEncounterController encounter in map.GetComponentsInChildren<CombatEncounterController>(true))
            {
                var definition = ScriptableObject.CreateInstance<EncounterDefinition>();
                var data = new SerializedObject(definition);
                data.FindProperty("briefing").stringValue = encounter.Briefing;
                SerializedProperty entries = data.FindProperty("deployments");
                entries.arraySize = encounter.Enemies.Count;
                for (int i = 0; i < entries.arraySize; i++)
                {
                    SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                    entry.FindPropertyRelative("enemyId").stringValue = encounter.Enemies[i].TargetId;
                    entry.FindPropertyRelative("delaySeconds").floatValue = encounter.Enemies[i].DeploymentDelay;
                }
                data.ApplyModifiedPropertiesWithoutUndo();
                SaveDefinition(definition, "Encounters/" + Slug(encounter.name));
                encounter.ConfigureDefinition(definition);
            }
            foreach (ToolController tool in map.GetComponentsInChildren<ToolController>(true)) tool.ConfigureDefinitions(wrench, bridger);
            ConfigureNetworkSources(incidentDefinition, wrench, bridger);

            // Extract only static branches that no gameplay component references. Interactive
            // art and animated chapter signals stay with their owning gameplay prefab.
            var environmentRoots = new GameObject[3];
            for (int i = 0; i < environmentRoots.Length; i++) environmentRoots[i] = new GameObject("Environment Chapter " + i);
            foreach (Transform child in map.Cast<Transform>().ToArray())
            {
                if (child.name == "Cooling Bay Graybox" || child.name == "Low Poly Cooling Bay Art Pass"
                    || child.name == "Single Player Three Chapter Demo") SplitEnvironment(child, 0, environmentRoots, protectedObjects);
                else if (IsStaticUnreferenced(child, protectedObjects)) child.SetParent(environmentRoots[0].transform, true);
            }
            var environmentDefinitions = new EnvironmentSceneDefinition[3];
            string[] chapterNames = { "CoolingBay", "PowerRelay", "StormCore" };
            EnsureFolder(Folder + "/Art/Environment");
            for (int i = 0; i < environmentRoots.Length; i++)
            {
                var definition = ScriptableObject.CreateInstance<EnvironmentSceneDefinition>();
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(environmentRoots[i], Folder + "/Art/Environment/" + chapterNames[i] + ".prefab");
                var data = new SerializedObject(definition);
                data.FindProperty("environmentPrefab").objectReferenceValue = prefab;
                data.FindProperty("scenePath").stringValue = ModularContentBuilder.EnvironmentFolder + "/" + chapterNames[i] + ".unity";
                data.FindProperty("chapter").intValue = i;
                data.ApplyModifiedPropertiesWithoutUndo();
                SaveDefinition(definition, "Scenes/" + chapterNames[i]);
                environmentDefinitions[i] = definition;
                Object.DestroyImmediate(environmentRoots[i]);
                ModularContentBuilder.GenerateEnvironmentScene(definition);
            }

            foreach (InterferenceEnemy enemy in enemies) ExtractVisualChildren(enemy.gameObject, "Enemies/" + Slug(enemy.TargetId));
            foreach (FirstPersonController player in map.GetComponentsInChildren<FirstPersonController>(true)) ExtractToolVisual(player.gameObject, "Player/SoloTools");

            // Deepest actors first; their parent modules keep nested prefab references.
            GameObject[] actors = map.GetComponentsInChildren<MonoBehaviour>(true)
                .Where(b => b is InterferenceEnemy || b is FirstPersonController || b is CoolingIncidentController
                    || b is CombatEncounterController || b is IContextInteractable)
                .Select(b => b.gameObject).Distinct().OrderByDescending(g => Depth(g.transform)).ToArray();
            foreach (GameObject actor in actors)
            {
                string category = actor.GetComponent<InterferenceEnemy>() != null ? "Enemies"
                    : actor.GetComponent<FirstPersonController>() != null ? "Player"
                    : actor.GetComponent<CoolingIncidentController>() != null ? "Incidents"
                    : actor.GetComponent<CombatEncounterController>() != null ? "Encounters" : "Entities";
                SaveAndConnect(actor, category + "/" + Slug(actor.GetComponent<InterferenceEnemy>() != null ? actor.GetComponent<InterferenceEnemy>().TargetId : actor.name));
            }
            foreach (Transform child in map.Cast<Transform>().ToArray())
            {
                if (PrefabUtility.IsAnyPrefabInstanceRoot(child.gameObject)) continue;
                string category = child.GetComponent<FunGame.UI.GameMenuController>() != null ? "UI" : "Composition";
                SaveAndConnect(child.gameObject, category + "/" + Slug(child.name));
            }
            foreach (GameObject root in roots)
                if (root != mode.gameObject && root != map.gameObject) SaveAndConnect(root, "Session/" + Slug(root.name));

            RestoreReferences(references);
            var context = mode.gameObject.AddComponent<ExpeditionContext>();
            context.ConfigureCampaign(map.GetComponentInChildren<SinglePlayerDemoController>(true), map.GetComponentInChildren<CoolingCombatIntegrationController>(true));
            mode.gameObject.AddComponent<ExpeditionEnvironmentLoader>().Configure(environmentDefinitions);
            ValidateSceneReferences(scene, references);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            EditorBuildSettings.scenes = ModularContentBuilder.BuildScenes.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Modules] Migrated {actors.Length} actor prefabs, {enemies.Length} enemy definitions, 3 additive environments; preserved {references.Count} scene references.");
        }

        private static T CreateDefinition<T>(string name, Component source) where T : ScriptableObject
        {
            var definition = ScriptableObject.CreateInstance<T>();
            var targetData = new SerializedObject(definition);
            var sourceData = new SerializedObject(source);
            SerializedProperty property = targetData.GetIterator();
            while (property.NextVisible(true))
            {
                if (property.name == "m_Script") continue;
                string legacyName = "legacy" + char.ToUpperInvariant(property.name[0]) + property.name.Substring(1);
                SerializedProperty original = sourceData.FindProperty(legacyName);
                if (original == null) continue;
                switch (property.propertyType)
                {
                    case SerializedPropertyType.Float: property.floatValue = original.floatValue; break;
                    case SerializedPropertyType.Integer: property.intValue = original.intValue; break;
                    case SerializedPropertyType.Boolean: property.boolValue = original.boolValue; break;
                    case SerializedPropertyType.Enum: property.enumValueIndex = original.enumValueIndex; break;
                }
            }
            targetData.ApplyModifiedPropertiesWithoutUndo();
            SaveDefinition(definition, name);
            return definition;
        }

        private static ToolDefinition CreateTool(string name, ToolKind kind, float cooldown)
        {
            var definition = ScriptableObject.CreateInstance<ToolDefinition>();
            var data = new SerializedObject(definition);
            data.FindProperty("kind").intValue = (int)kind;
            data.FindProperty("cooldownSeconds").floatValue = cooldown;
            data.ApplyModifiedPropertiesWithoutUndo();
            SaveDefinition(definition, "Tools/" + name);
            return definition;
        }

        private static void ConfigureNetworkSources(CoolingIncidentDefinition incident, ToolDefinition wrench, ToolDefinition bridger)
        {
            string[] paths = { "Assets/Game/Content/Networking/M3_NetworkPlayer.prefab", "Assets/Game/Content/Networking/M4_CoolingIncident.prefab" };
            foreach (string path in paths)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    root.GetComponent<ToolController>()?.ConfigureDefinitions(wrench, bridger);
                    root.GetComponent<NetworkCoolingIncidentController>()?.ConfigureDefinition(incident);
                    if (root.GetComponent<FirstPersonController>() != null) ExtractToolVisual(root, "Player/NetworkTools");
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
        }

        private static void ExtractToolVisual(GameObject player, string name)
        {
            Transform visual = player.GetComponentsInChildren<Transform>(true).SingleOrDefault(t => t.name == "Main Tool Visual Anchor");
            if (visual != null) SaveAndConnect(visual.gameObject, "Art/" + name);
        }

        private static void ExtractVisualChildren(GameObject actor, string name)
        {
            Transform[] children = actor.transform.Cast<Transform>().Where(t => t.GetComponentsInChildren<MonoBehaviour>(true).Length == 0).ToArray();
            if (children.Length == 0) return;
            var visual = new GameObject("Visual");
            visual.transform.SetParent(actor.transform, false);
            foreach (Transform child in children) child.SetParent(visual.transform, false);
            SaveAndConnect(visual, "Art/" + name);
        }

        private static void SplitEnvironment(Transform parent, int chapter, GameObject[] outputs, HashSet<Object> protectedObjects)
        {
            foreach (Transform child in parent.Cast<Transform>().ToArray())
            {
                if (child.name == "Chapter 2 - Power Relay Compartment") SplitEnvironment(child, 1, outputs, protectedObjects);
                else if (child.name == "Chapter 3 - Storm Core Chamber") SplitEnvironment(child, 2, outputs, protectedObjects);
                else if (IsStaticUnreferenced(child, protectedObjects)) child.SetParent(outputs[chapter].transform, true);
            }
        }

        private static bool IsStaticUnreferenced(Transform root, HashSet<Object> protectedObjects)
        {
            if (root.GetComponentsInChildren<MonoBehaviour>(true).Length != 0) return false;
            return root.GetComponentsInChildren<Transform>(true).All(t => !protectedObjects.Contains(t.gameObject)
                && t.GetComponents<Component>().All(c => !protectedObjects.Contains(c)));
        }

        private static List<Reference> CaptureReferences(GameObject[] roots)
        {
            var result = new List<Reference>();
            foreach (MonoBehaviour component in roots.SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true)))
            {
                if (component == null) throw new InvalidDataException("Missing script in source scene.");
                var data = new SerializedObject(component);
                SerializedProperty property = data.GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference || property.name.StartsWith("m_", StringComparison.Ordinal)) continue;
                    Object target = property.objectReferenceValue;
                    if (target != null && !EditorUtility.IsPersistent(target))
                        result.Add(new Reference { Owner = component, Property = property.propertyPath, Target = target });
                }
            }
            return result;
        }

        private static void RestoreReferences(List<Reference> references)
        {
            foreach (var group in references.GroupBy(r => r.Owner))
            {
                var data = new SerializedObject(group.Key);
                foreach (Reference reference in group)
                {
                    if (reference.Target == null) throw new InvalidDataException("Migration removed a referenced object: " + reference.Property);
                    SerializedProperty property = data.FindProperty(reference.Property);
                    if (property == null) throw new InvalidDataException("Missing serialized binding: " + reference.Property);
                    property.objectReferenceValue = reference.Target;
                }
                data.ApplyModifiedPropertiesWithoutUndo();
                if (PrefabUtility.IsPartOfPrefabInstance(group.Key)) PrefabUtility.RecordPrefabInstancePropertyModifications(group.Key);
            }
        }

        private static void ValidateSceneReferences(Scene scene, List<Reference> references)
        {
            foreach (Reference reference in references)
            {
                var data = new SerializedObject(reference.Owner);
                if (data.FindProperty(reference.Property).objectReferenceValue != reference.Target)
                    throw new InvalidDataException("Lost binding: " + reference.Owner.name + "." + reference.Property);
                var targetObject = reference.Target as GameObject ?? (reference.Target as Component)?.gameObject;
                if (targetObject != null && targetObject.scene != scene) throw new InvalidDataException("Cross-scene binding detected.");
            }
        }

        private static void SaveAndConnect(GameObject root, string relative)
        {
            string path = Folder + "/" + relative + ".prefab";
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            if (File.Exists(path)) throw new InvalidDataException("Duplicate prefab output: " + path);
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, path, InteractionMode.AutomatedAction);
        }

        private static void SaveDefinition(ScriptableObject asset, string name)
        {
            string path = Folder + "/Configuration/" + name + ".asset";
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            AssetDatabase.CreateAsset(asset, path);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
        private static int Depth(Transform t) => t.parent == null ? 0 : 1 + Depth(t.parent);
        private static string Slug(string value) => new string(value.Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '-').ToArray());
    }
}
