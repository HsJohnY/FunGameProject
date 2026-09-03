using System.IO;
using FunGame.Audio;
using FunGame.Combat;
using FunGame.Diagnostics;
using FunGame.Incident;
using FunGame.Tools;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FunGame.Editor
{
    /// <summary>
    /// 从稳定的 M1 冷却舱复制生成维修+防卫候选场景，避免直接争用原维修场景资产。
    /// </summary>
    public static class CombatRepairIntegrationBootstrap
    {
        public const string ScenePath = "Assets/Game/Scenes/Combat_CoolingBayIntegration.unity";
        private const string SourceScenePath = "Assets/Game/Scenes/M1_CoolingBay.unity";
        private const string MaterialFolder = "Assets/Game/Content/Graybox";

        [MenuItem("FunGame/Combat/生成维修防卫集成场景")]
        public static void ConfigureCurrent()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceScenePath) == null)
            {
                throw new FileNotFoundException("缺少 M1 冷却舱源场景。", SourceScenePath);
            }

            EnsureFolder(MaterialFolder);
            Material systemMaterial = CreateOrLoadMaterial(
                MaterialFolder + "/Combat_System.mat", new Color(0.1f, 0.8f, 0.55f));
            Material warningMaterial = CreateOrLoadMaterial(
                MaterialFolder + "/Combat_Warning.mat", new Color(0.9f, 0.42f, 0.08f));
            Material enemyMaterial = CreateOrLoadMaterial(
                MaterialFolder + "/Combat_Enemy.mat", new Color(0.7f, 0.1f, 0.85f));

            Scene scene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(scene, ScenePath, false))
            {
                throw new IOException($"无法复制维修防卫集成场景：{ScenePath}");
            }

            CoolingIncidentController incident = Object.FindFirstObjectByType<CoolingIncidentController>();
            if (incident == null)
            {
                throw new InvalidDataException("M1 冷却舱缺少 CoolingIncidentController。");
            }

            ConfigureCheckpoint();
            var root = new GameObject("Cooling Repair Defense Integration").transform;
            var encounter = new GameObject("Repair Interference Encounter").AddComponent<CombatEncounterController>();
            encounter.transform.SetParent(root);

            GameObject targetObject = CreateBlock(
                root,
                "Auxiliary Cooling Control Unit",
                new Vector3(4.4f, 1f, 5.8f),
                new Vector3(1.4f, 2f, 1.4f),
                systemMaterial,
                true);
            var defenseTarget = targetObject.AddComponent<DefendableSystemTarget>();
            defenseTarget.Configure(60);
            targetObject.AddComponent<AudioSource>();
            targetObject.AddComponent<DeviceDamageFeedback>().Configure(defenseTarget);
            CreateIntegrityIndicator(targetObject.transform, defenseTarget, warningMaterial);

            InterferenceEnemy directEnemy = CreateEnemy(
                root,
                "Direct Interference Creature",
                new Vector3(-2.8f, 1f, -3.8f),
                enemyMaterial,
                warningMaterial,
                defenseTarget,
                encounter,
                InterferenceEnemyBehavior.Direct);
            InterferenceEnemy flankingEnemy = CreateEnemy(
                root,
                "Flanking Attachment Creature",
                new Vector3(3.7f, 1f, -5.8f),
                enemyMaterial,
                warningMaterial,
                defenseTarget,
                encounter,
                InterferenceEnemyBehavior.FlankingAttach);

            encounter.Configure(defenseTarget, new[] { directEnemy, flankingEnemy }, false);
            var integration = root.gameObject.AddComponent<CoolingCombatIntegrationController>();
            integration.Configure(incident, encounter, defenseTarget, 2.5f);
            CoolingBayBgmController bgm = Object.FindFirstObjectByType<CoolingBayBgmController>();
            bgm?.Configure(incident, integration);

            CreateRouteMarkers(root, warningMaterial);
            ConfigurePlayerFeedback(integration);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new IOException($"无法保存维修防卫集成场景：{ScenePath}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Combat] 维修防卫集成候选场景生成成功。");
        }

        [MenuItem("FunGame/Combat/构建维修防卫 Windows 开发版本")]
        public static void BuildWindowsDevelopment()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                ConfigureCurrent();
            }

            Directory.CreateDirectory("Builds/CombatRepair-Windows");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/CombatRepair-Windows/FunGame-CombatRepair.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"维修防卫构建失败：{report.summary.result}");
            }

            Debug.Log($"[Combat] 维修防卫 Windows 构建成功：{report.summary.totalSize} bytes。");
        }

        private static void ConfigureCheckpoint()
        {
            DevelopmentCheckpoint checkpoint = Object.FindFirstObjectByType<DevelopmentCheckpoint>();
            if (checkpoint == null)
            {
                checkpoint = new GameObject("Combat Repair Development Checkpoint").AddComponent<DevelopmentCheckpoint>();
            }

            checkpoint.name = "Combat Repair Development Checkpoint";
            checkpoint.Configure("combat-repair-integration-candidate", "--combat-repair-smoke");
        }

        private static InterferenceEnemy CreateEnemy(
            Transform parent,
            string name,
            Vector3 position,
            Material material,
            Material linkMaterial,
            DefendableSystemTarget target,
            CombatEncounterController encounter,
            InterferenceEnemyBehavior behavior)
        {
            GameObject enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyObject.name = name;
            enemyObject.transform.SetParent(parent);
            enemyObject.transform.position = position;
            enemyObject.transform.localScale = behavior == InterferenceEnemyBehavior.Direct
                ? new Vector3(0.75f, 0.75f, 0.75f)
                : new Vector3(0.62f, 0.5f, 0.9f);
            enemyObject.GetComponent<Renderer>().sharedMaterial = material;
            var enemy = enemyObject.AddComponent<InterferenceEnemy>();
            enemy.Configure(
                target,
                encounter,
                configuredMaxHealth: behavior == InterferenceEnemyBehavior.Direct ? 3 : 2,
                configuredMoveSpeed: behavior == InterferenceEnemyBehavior.Direct ? 1.25f : 1.55f,
                configuredAttackRange: 1.15f,
                configuredAttackIntervalSeconds: 1.5f,
                configuredInterferenceDamage: 10,
                configuredWrenchDamage: 2,
                configuredKnockbackDistance: 1.1f,
                configuredBehavior: behavior,
                configuredAttackWindupSeconds: 0.55f);
            var line = enemyObject.AddComponent<LineRenderer>();
            line.startColor = new Color(1f, 0.75f, 0.08f);
            line.endColor = new Color(0.8f, 0.15f, 1f);
            enemyObject.AddComponent<InterferenceLinkFeedback>().Configure(enemy, target, linkMaterial);
            return enemy;
        }

        private static void CreateIntegrityIndicator(Transform target, DefendableSystemTarget defenseTarget, Material material)
        {
            var indicatorRoot = new GameObject("World Integrity Indicator");
            indicatorRoot.transform.SetParent(target, false);
            indicatorRoot.transform.localPosition = new Vector3(0f, 0.72f, -0.72f);
            var segments = new Renderer[3];
            for (int index = 0; index < segments.Length; index++)
            {
                GameObject segment = CreateBlock(
                    indicatorRoot.transform,
                    $"Integrity Segment {index + 1}",
                    Vector3.zero,
                    new Vector3(0.24f, 0.16f, 0.08f),
                    material,
                    false);
                segment.transform.localPosition = new Vector3((index - 1) * 0.32f, 0f, 0f);
                Object.DestroyImmediate(segment.GetComponent<Collider>());
                segments[index] = segment.GetComponent<Renderer>();
            }

            indicatorRoot.AddComponent<DefendableSystemIndicator>().Configure(defenseTarget, segments);
        }

        private static void CreateRouteMarkers(Transform parent, Material warningMaterial)
        {
            var tutorial = new GameObject("Silent Tutorial Route - Rack Enemy Device").transform;
            tutorial.SetParent(parent);
            Vector3[] points =
            {
                new Vector3(5.1f, 0.035f, -4f),
                new Vector3(4.2f, 0.035f, -2.0f),
                new Vector3(3.4f, 0.035f, -0.5f),
                new Vector3(3.5f, 0.035f, 1.2f),
                new Vector3(3.9f, 0.035f, 3.0f),
                new Vector3(4.3f, 0.035f, 4.7f)
            };
            for (int index = 0; index < points.Length; index++)
            {
                GameObject marker = CreateBlock(
                    tutorial,
                    $"Route Chevron {index + 1}",
                    points[index],
                    new Vector3(0.18f, 0.04f, 0.55f),
                    warningMaterial,
                    false);
                marker.transform.rotation = Quaternion.Euler(0f, -18f, 0f);
                Object.DestroyImmediate(marker.GetComponent<Collider>());
            }
        }

        private static void ConfigurePlayerFeedback(CoolingCombatIntegrationController integration)
        {
            GameObject player = GameObject.Find("Local First Person Player");
            if (player == null)
            {
                throw new InvalidDataException("M1 冷却舱缺少本地第一人称玩家。");
            }

            Camera viewCamera = player.GetComponentInChildren<Camera>(true);
            Transform wrenchVisual = player.transform.Find("First Person Camera/Main Tool Visual Anchor/Impact Wrench Visual");
            Transform sealantVisual = player.transform.Find("First Person Camera/Main Tool Visual Anchor/Sealant Gun Visual");
            Transform bridgerVisual = player.transform.Find("First Person Camera/Main Tool Visual Anchor/Circuit Bridger Visual");
            var cameraFeedback = player.AddComponent<CombatCameraFeedback>();
            cameraFeedback.Configure(viewCamera != null ? viewCamera.transform : null);
            var hitStop = player.AddComponent<LocalHitStopFeedback>();
            player.AddComponent<AudioSource>();
            var presenter = player.AddComponent<WrenchFeedbackPresenter>();
            presenter.Configure(wrenchVisual, sealantVisual, bridgerVisual, cameraFeedback, hitStop);
            player.AddComponent<CoolingCombatStatusOverlay>().Configure(integration);
        }

        private static GameObject CreateBlock(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            Material material,
            bool worldPosition)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, !worldPosition);
            if (worldPosition)
            {
                block.transform.position = position;
            }
            else
            {
                block.transform.localPosition = position;
            }

            block.transform.localScale = scale;
            block.GetComponent<MeshRenderer>().sharedMaterial = material;
            return block;
        }

        private static Material CreateOrLoadMaterial(string path, Color color)
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                return existing;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidDataException("无法找到 URP Lit Shader。");
            }

            var material = new Material(shader) { color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void EnsureFolder(string assetPath)
        {
            string current = "Assets";
            string[] parts = assetPath.Split('/');
            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }
    }
}
