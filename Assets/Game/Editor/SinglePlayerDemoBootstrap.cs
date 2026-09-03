using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FunGame.Combat;
using FunGame.Demo;
using FunGame.Diagnostics;
using FunGame.Incident;
using FunGame.Interaction;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FunGame.Editor
{
    /// <summary>
    /// 生成三章单人演示场景：扩展冷却事故、继电器事件和风暴核心三波校准防卫。
    /// </summary>
    public static class SinglePlayerDemoBootstrap
    {
        public const string ScenePath = "Assets/Game/Scenes/SinglePlayer_ThreeChapterDemo.unity";
        private const string MaterialFolder = "Assets/Game/Content/Graybox";

        [MenuItem("FunGame/Demo/生成三章单人演示场景")]
        public static void ConfigureCurrent()
        {
            M1GrayboxBootstrap.ConfigureCurrent();
            CombatRepairIntegrationBootstrap.ConfigureCurrent();

            Scene scene = EditorSceneManager.OpenScene(CombatRepairIntegrationBootstrap.ScenePath, OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(scene, ScenePath, false))
            {
                throw new IOException($"无法复制三章单人演示场景：{ScenePath}");
            }

            CoolingIncidentController incident = UnityEngine.Object.FindFirstObjectByType<CoolingIncidentController>();
            CoolingCombatIntegrationController chapterOneCombat = UnityEngine.Object.FindFirstObjectByType<CoolingCombatIntegrationController>();
            if (incident == null || chapterOneCombat == null)
            {
                throw new InvalidDataException("三章演示源场景缺少冷却事故或维修防卫组件。");
            }

            Material relayMaterial = CreateOrLoadMaterial(
                MaterialFolder + "/Demo_Relay.mat", new Color(0.48f, 0.16f, 0.78f));
            Material stormMaterial = CreateOrLoadMaterial(
                MaterialFolder + "/Demo_Storm.mat", new Color(0.12f, 0.62f, 0.9f));
            Material warningMaterial = CreateOrLoadMaterial(
                MaterialFolder + "/Demo_Warning.mat", new Color(0.95f, 0.42f, 0.08f));
            Material darkMaterial = CreateOrLoadMaterial(
                MaterialFolder + "/Demo_Dark.mat", new Color(0.08f, 0.12f, 0.18f));

            var root = new GameObject("Single Player Three Chapter Demo").transform;
            DemoChapterPresentation presentation = CreateChapterArt(
                root, relayMaterial, stormMaterial, warningMaterial, darkMaterial);

            DemoRelayTarget[] relays = CreateRelayTargets(root, relayMaterial, warningMaterial);
            CombatEncounterController relayDefense = CreateEncounter(
                root,
                "Chapter 2 Relay Surge",
                new Vector3(-4.4f, 1f, 5.8f),
                100,
                new[]
                {
                    EnemySpec.Strider("relay-strider", new Vector3(-2.8f, 1f, -6.5f)),
                    EnemySpec.Skitter("relay-skitter-left", new Vector3(3.8f, 1f, -5.6f)),
                    EnemySpec.Skitter("relay-skitter-right", new Vector3(-4.2f, 1f, -3.4f)),
                    EnemySpec.Pulser("relay-pulser", new Vector3(2.4f, 1f, 0.5f))
                },
                darkMaterial,
                warningMaterial);

            DefendableSystemTarget stormCoreTarget = CreateDefenseTarget(
                root,
                "Shared Storm Calibration Core",
                new Vector3(4.2f, 1f, 6.4f),
                130,
                stormMaterial);
            CombatEncounterController[] stormWaves =
            {
                CreateEncounter(
                    root,
                    "Chapter 3 Wave 1 - Skitter Rush",
                    new Vector3(4.2f, 1f, 6.4f),
                    90,
                    new[]
                    {
                        EnemySpec.Skitter("wave1-skitter-a", new Vector3(-5f, 1f, 5.5f)),
                        EnemySpec.Skitter("wave1-skitter-b", new Vector3(5f, 1f, 5.5f)),
                        EnemySpec.Strider("wave1-strider", new Vector3(0f, 1f, 7.8f))
                    },
                    darkMaterial,
                    warningMaterial,
                    stormCoreTarget),
                CreateEncounter(
                    root,
                    "Chapter 3 Wave 2 - Pulse Crossfire",
                    new Vector3(4.2f, 1f, 6.4f),
                    110,
                    new[]
                    {
                        EnemySpec.Pulser("wave2-pulser-a", new Vector3(-5.2f, 1f, 4.8f)),
                        EnemySpec.Pulser("wave2-pulser-b", new Vector3(5.2f, 1f, 4.8f)),
                        EnemySpec.Strider("wave2-strider", new Vector3(0f, 1f, 7.8f)),
                        EnemySpec.Skitter("wave2-skitter", new Vector3(4.6f, 1f, 0.8f))
                    },
                    darkMaterial,
                    warningMaterial,
                    stormCoreTarget),
                CreateEncounter(
                    root,
                    "Chapter 3 Wave 3 - Armored Convergence",
                    new Vector3(4.2f, 1f, 6.4f),
                    130,
                    new[]
                    {
                        EnemySpec.Bulwark("wave3-bulwark", new Vector3(0f, 1f, 8f)),
                        EnemySpec.Pulser("wave3-pulser-left", new Vector3(-5f, 1f, 4.8f)),
                        EnemySpec.Pulser("wave3-pulser-right", new Vector3(5f, 1f, 4.8f)),
                        EnemySpec.Skitter("wave3-skitter-left", new Vector3(-4.8f, 1f, -0.5f)),
                        EnemySpec.Skitter("wave3-skitter-right", new Vector3(4.8f, 1f, -0.5f))
                    },
                    darkMaterial,
                    warningMaterial,
                    stormCoreTarget)
            };

            GameObject consoleObject = CreateBlock(
                root,
                "Storm Calibration Console",
                new Vector3(0f, 1f, -8.55f),
                new Vector3(2.2f, 2f, 0.7f),
                stormMaterial,
                true);
            var campaignConsole = consoleObject.AddComponent<DemoCalibrationConsole>();

            var campaign = root.gameObject.AddComponent<SinglePlayerDemoController>();
            campaign.Configure(incident, relayDefense, relays, stormWaves, campaignConsole);
            root.gameObject.AddComponent<SinglePlayerDemoOverlay>().Configure(campaign);
            root.gameObject.AddComponent<DemoScreenshotCheckpoint>();
            presentation.Configure(
                campaign,
                presentation.GetComponentsInChildren<Transform>(true)
                    .Where(item => item.name.StartsWith("Storm Ceiling Node", StringComparison.Ordinal)).ToArray(),
                presentation.GetComponentsInChildren<Renderer>(true)
                    .Where(item => item.name.StartsWith("Storm Ceiling Node", StringComparison.Ordinal)).ToArray(),
                UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)
                    .Where(item => item.type == LightType.Point).ToArray());
            CreateEasterEgg325(root, campaign, warningMaterial, darkMaterial);

            ContextPromptOverlay prompt = UnityEngine.Object.FindFirstObjectByType<ContextPromptOverlay>();
            prompt?.ConfigureDemoCampaign(campaign);
            CoolingCombatStatusOverlay combatOverlay = UnityEngine.Object.FindFirstObjectByType<CoolingCombatStatusOverlay>();
            combatOverlay?.ConfigureDemoCampaign(campaign);
            ConfigureCheckpoint();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new IOException($"无法保存三章单人演示场景：{ScenePath}");
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Demo] 三章单人演示场景生成成功。");
        }

        [MenuItem("FunGame/Demo/构建三章单人 Windows 开发版本")]
        public static void BuildWindowsDevelopment()
        {
            ConfigureCurrent();
            Directory.CreateDirectory("Builds/SinglePlayerDemo-Windows");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/SinglePlayerDemo-Windows/FunGame-SinglePlayerDemo.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"三章单人演示构建失败：{report.summary.result}");
            }

            Debug.Log($"[Demo] Windows 开发构建成功：{report.summary.totalSize} bytes。");
        }

        private static DemoRelayTarget[] CreateRelayTargets(Transform parent, Material relay, Material warning)
        {
            Vector3[] positions =
            {
                new Vector3(-5.85f, 1.15f, -5.2f),
                new Vector3(5.85f, 1.15f, 0.8f),
                new Vector3(-5.85f, 1.15f, 6.5f)
            };
            var relays = new DemoRelayTarget[positions.Length];
            for (int index = 0; index < positions.Length; index++)
            {
                GameObject relayObject = CreateBlock(
                    parent,
                    $"Storm Relay {index + 1}",
                    positions[index],
                    new Vector3(0.35f, 1.45f, 1.1f),
                    relay,
                    true);
                relays[index] = relayObject.AddComponent<DemoRelayTarget>();
                relays[index].Configure($"storm-relay-{index + 1}", $"风暴继电器 {index + 1}");
                CreateLocalDecoration(relayObject.transform, "Relay Crown", PrimitiveType.Sphere,
                    new Vector3(0f, 0.85f, 0f), new Vector3(0.28f, 0.18f, 0.28f), warning);
            }

            return relays;
        }

        private static CombatEncounterController CreateEncounter(
            Transform parent,
            string name,
            Vector3 targetPosition,
            int targetIntegrity,
            IReadOnlyList<EnemySpec> specs,
            Material enemyMaterial,
            Material warningMaterial,
            DefendableSystemTarget sharedTarget = null)
        {
            var encounterObject = new GameObject(name);
            encounterObject.transform.SetParent(parent);
            var encounter = encounterObject.AddComponent<CombatEncounterController>();
            DefendableSystemTarget target = sharedTarget != null
                ? sharedTarget
                : CreateDefenseTarget(
                    encounterObject.transform,
                    name + " Target",
                    targetPosition,
                    targetIntegrity,
                    warningMaterial);

            var enemies = new List<InterferenceEnemy>(specs.Count);
            foreach (EnemySpec spec in specs)
            {
                enemies.Add(CreateEnemy(encounterObject.transform, target, encounter, spec, enemyMaterial, warningMaterial));
            }

            encounter.Configure(target, enemies, false);
            return encounter;
        }

        private static DefendableSystemTarget CreateDefenseTarget(
            Transform parent,
            string name,
            Vector3 position,
            int integrity,
            Material material)
        {
            GameObject targetObject = CreateBlock(
                parent,
                name,
                position,
                new Vector3(1.7f, 2f, 1.7f),
                material,
                true);
            var target = targetObject.AddComponent<DefendableSystemTarget>();
            target.Configure(integrity);
            targetObject.AddComponent<AudioSource>();
            targetObject.AddComponent<DeviceDamageFeedback>().Configure(target);
            return target;
        }

        private static InterferenceEnemy CreateEnemy(
            Transform parent,
            DefendableSystemTarget target,
            CombatEncounterController encounter,
            EnemySpec spec,
            Material material,
            Material accentMaterial)
        {
            GameObject enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyObject.name = spec.DisplayName;
            enemyObject.transform.SetParent(parent);
            enemyObject.transform.position = spec.Position;
            enemyObject.transform.localScale = spec.Scale;
            enemyObject.GetComponent<Renderer>().sharedMaterial = material;
            var enemy = enemyObject.AddComponent<InterferenceEnemy>();
            enemy.ConfigureIdentity(spec.Id, spec.DisplayName);
            enemy.Configure(
                target,
                encounter,
                configuredMaxHealth: spec.Health,
                configuredMoveSpeed: spec.Speed,
                configuredAttackRange: spec.AttackRange,
                configuredAttackIntervalSeconds: spec.AttackInterval,
                configuredInterferenceDamage: spec.Damage,
                configuredWrenchDamage: 1,
                configuredKnockbackDistance: spec.Knockback,
                configuredBehavior: spec.Behavior,
                configuredAttackWindupSeconds: spec.Windup);

            var line = enemyObject.AddComponent<LineRenderer>();
            line.startColor = spec.Behavior == InterferenceEnemyBehavior.RangedPulse
                ? new Color(0.15f, 0.75f, 1f)
                : new Color(1f, 0.7f, 0.08f);
            line.endColor = new Color(0.85f, 0.12f, 0.95f);
            enemyObject.AddComponent<InterferenceLinkFeedback>().Configure(enemy, target, accentMaterial);
            AddEnemySilhouette(enemyObject.transform, spec, accentMaterial);
            return enemy;
        }

        private static void AddEnemySilhouette(Transform enemy, EnemySpec spec, Material accent)
        {
            if (spec.Archetype == "skitter")
            {
                CreateLocalDecoration(enemy, "Left Fin", PrimitiveType.Cube,
                    new Vector3(-0.55f, 0f, 0f), new Vector3(0.55f, 0.08f, 0.25f), accent);
                CreateLocalDecoration(enemy, "Right Fin", PrimitiveType.Cube,
                    new Vector3(0.55f, 0f, 0f), new Vector3(0.55f, 0.08f, 0.25f), accent);
            }
            else if (spec.Archetype == "pulser")
            {
                CreateLocalDecoration(enemy, "Pulse Crown", PrimitiveType.Sphere,
                    new Vector3(0f, 0.72f, 0f), new Vector3(0.42f, 0.18f, 0.42f), accent);
            }
            else if (spec.Archetype == "bulwark")
            {
                CreateLocalDecoration(enemy, "Front Armor", PrimitiveType.Cube,
                    new Vector3(0f, 0f, -0.55f), new Vector3(0.85f, 0.65f, 0.18f), accent);
                CreateLocalDecoration(enemy, "Armor Crown", PrimitiveType.Cube,
                    new Vector3(0f, 0.8f, 0f), new Vector3(0.75f, 0.2f, 0.7f), accent);
            }
        }

        private static DemoChapterPresentation CreateChapterArt(
            Transform parent,
            Material relay,
            Material storm,
            Material warning,
            Material dark)
        {
            var art = new GameObject("Demo Chapter Art").transform;
            art.SetParent(parent);
            CreateDecoration(art, "Relay Conduit Left", PrimitiveType.Cylinder,
                new Vector3(-5.45f, 3.25f, 0.5f), new Vector3(0.12f, 4.3f, 0.12f), relay, Quaternion.Euler(90f, 0f, 0f));
            CreateDecoration(art, "Relay Conduit Right", PrimitiveType.Cylinder,
                new Vector3(5.45f, 3.25f, 0.5f), new Vector3(0.12f, 4.3f, 0.12f), relay, Quaternion.Euler(90f, 0f, 0f));
            for (int index = 0; index < 5; index++)
            {
                CreateDecoration(art, $"Storm Ceiling Node {index + 1}", PrimitiveType.Sphere,
                    new Vector3(-4.8f + index * 2.4f, 4.45f, -5.8f + index * 2.6f),
                    new Vector3(0.22f, 0.16f, 0.22f), index % 2 == 0 ? storm : warning);
            }

            CreateDecoration(art, "Storm Core Outer", PrimitiveType.Cylinder,
                new Vector3(4.2f, 1f, 6.4f), new Vector3(1.2f, 1.35f, 1.2f), dark, Quaternion.Euler(90f, 0f, 0f));
            CreateDecoration(art, "Storm Core Inner", PrimitiveType.Sphere,
                new Vector3(4.2f, 1f, 6.4f), new Vector3(0.55f, 0.55f, 0.55f), storm);
            return art.gameObject.AddComponent<DemoChapterPresentation>();
        }

        private static void CreateEasterEgg325(
            Transform parent,
            SinglePlayerDemoController campaign,
            Material plateMaterial,
            Material textMaterial)
        {
            GameObject plate = CreateBlock(
                parent,
                "Hidden Maintenance Plate 325",
                new Vector3(-6.15f, 1.25f, -7.25f),
                new Vector3(0.16f, 0.9f, 1.5f),
                plateMaterial,
                true);
            plate.AddComponent<DemoEasterEgg325Interactable>().Configure(campaign);

            var textObject = new GameObject("Engraved Number 325");
            textObject.transform.SetParent(plate.transform, false);
            textObject.transform.localPosition = new Vector3(0.56f, 0f, 0f);
            textObject.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            var text = textObject.AddComponent<TextMesh>();
            text.text = "325";
            text.fontSize = 64;
            text.characterSize = 0.12f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = textMaterial.color;
        }

        private static void ConfigureCheckpoint()
        {
            DevelopmentCheckpoint checkpoint = UnityEngine.Object.FindFirstObjectByType<DevelopmentCheckpoint>();
            if (checkpoint == null)
            {
                checkpoint = new GameObject("Single Player Demo Checkpoint").AddComponent<DevelopmentCheckpoint>();
            }

            checkpoint.name = "Single Player Demo Checkpoint";
            checkpoint.Configure("singleplayer-three-chapter-demo", "--singleplayer-demo-smoke");
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
            block.GetComponent<Renderer>().sharedMaterial = material;
            return block;
        }

        private static GameObject CreateDecoration(
            Transform parent,
            string name,
            PrimitiveType type,
            Vector3 position,
            Vector3 scale,
            Material material,
            Quaternion rotation = default)
        {
            GameObject decoration = GameObject.CreatePrimitive(type);
            decoration.name = name;
            decoration.transform.SetParent(parent);
            decoration.transform.SetPositionAndRotation(position, rotation == default ? Quaternion.identity : rotation);
            decoration.transform.localScale = scale;
            decoration.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(decoration.GetComponent<Collider>());
            return decoration;
        }

        private static GameObject CreateLocalDecoration(
            Transform parent,
            string name,
            PrimitiveType type,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            GameObject decoration = GameObject.CreatePrimitive(type);
            decoration.name = name;
            decoration.transform.SetParent(parent, false);
            decoration.transform.localPosition = localPosition;
            decoration.transform.localRotation = Quaternion.identity;
            decoration.transform.localScale = localScale;
            decoration.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(decoration.GetComponent<Collider>());
            return decoration;
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

        private readonly struct EnemySpec
        {
            private EnemySpec(
                string id,
                string displayName,
                string archetype,
                Vector3 position,
                Vector3 scale,
                InterferenceEnemyBehavior behavior,
                int health,
                float speed,
                float attackRange,
                float attackInterval,
                int damage,
                float knockback,
                float windup)
            {
                Id = id;
                DisplayName = displayName;
                Archetype = archetype;
                Position = position;
                Scale = scale;
                Behavior = behavior;
                Health = health;
                Speed = speed;
                AttackRange = attackRange;
                AttackInterval = attackInterval;
                Damage = damage;
                Knockback = knockback;
                Windup = windup;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public string Archetype { get; }
            public Vector3 Position { get; }
            public Vector3 Scale { get; }
            public InterferenceEnemyBehavior Behavior { get; }
            public int Health { get; }
            public float Speed { get; }
            public float AttackRange { get; }
            public float AttackInterval { get; }
            public int Damage { get; }
            public float Knockback { get; }
            public float Windup { get; }

            public static EnemySpec Strider(string id, Vector3 position) => new EnemySpec(
                id, "直行干扰体", "strider", position, new Vector3(0.75f, 0.75f, 0.75f),
                InterferenceEnemyBehavior.Direct, 3, 1.25f, 1.15f, 1.5f, 10, 1.1f, 0.55f);

            public static EnemySpec Skitter(string id, Vector3 position) => new EnemySpec(
                id, "侧袭附着体", "skitter", position, new Vector3(0.62f, 0.5f, 0.9f),
                InterferenceEnemyBehavior.FlankingAttach, 2, 1.65f, 1.1f, 1.3f, 8, 1.35f, 0.42f);

            public static EnemySpec Pulser(string id, Vector3 position) => new EnemySpec(
                id, "远距脉冲体", "pulser", position, new Vector3(0.7f, 0.82f, 0.7f),
                InterferenceEnemyBehavior.RangedPulse, 3, 0.95f, 3.1f, 1.8f, 9, 0.8f, 0.8f);

            public static EnemySpec Bulwark(string id, Vector3 position) => new EnemySpec(
                id, "重甲干扰体", "bulwark", position, new Vector3(1.05f, 1.05f, 1.05f),
                InterferenceEnemyBehavior.Direct, 6, 0.72f, 1.4f, 2.2f, 16, 0.45f, 1.05f);
        }
    }
}
