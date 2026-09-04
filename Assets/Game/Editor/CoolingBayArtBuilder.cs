using FunGame.Incident;
using FunGame.Interaction;
using UnityEngine;

namespace FunGame.Editor
{
    /// <summary>
    /// 以可编辑基础几何搭建冷却舱低模套件；装饰模型不承载玩法碰撞或状态。
    /// </summary>
    public static class CoolingBayArtBuilder
    {
        public static void BuildEnvironment(
            Material structureMaterial,
            Material machineryMaterial,
            Material warningMaterial,
            Material trimMaterial,
            Material glowMaterial,
            Material circuitMaterial)
        {
            var root = new GameObject("Low Poly Cooling Bay Art Pass").transform;
            BuildBulkheadRibs(root, structureMaterial, warningMaterial);
            BuildCoolingPump(root, machineryMaterial, warningMaterial, trimMaterial, glowMaterial);
            BuildPipeNetwork(root, machineryMaterial, warningMaterial, trimMaterial, glowMaterial);
            BuildConsole(root, structureMaterial, warningMaterial, glowMaterial);
            BuildToolAndPipeRacks(root, structureMaterial, machineryMaterial, warningMaterial, circuitMaterial);
            BuildInteractiveGameplayProps(
                root, structureMaterial, machineryMaterial, warningMaterial, trimMaterial, glowMaterial, circuitMaterial);
            BuildMaintenanceSuitReference(root, structureMaterial, warningMaterial, trimMaterial, glowMaterial);
        }

        public static void EnhanceFirstPersonTools(
            Material machineryMaterial,
            Material warningMaterial,
            Material trimMaterial,
            Material circuitMaterial)
        {
            GameObject wrenchPlaceholder = FindSceneObject("Impact Wrench Visual");
            if (wrenchPlaceholder != null)
            {
                Transform root = PrepareToolModel(wrenchPlaceholder, "Impact Wrench Model");
                CreateLocalShape(root, "Wrench Main Housing", PrimitiveType.Cube,
                    new Vector3(0f, 0f, 0.22f), new Vector3(0.2f, 0.16f, 0.32f),
                    warningMaterial, Quaternion.identity);
                CreateLocalShape(root, "Wrench Motor Drum", PrimitiveType.Cylinder,
                    new Vector3(0f, 0f, 0.25f), new Vector3(0.16f, 0.19f, 0.16f),
                    machineryMaterial, Quaternion.Euler(0f, 0f, 90f));
                CreateLocalShape(root, "Wrench Rear Cap", PrimitiveType.Cylinder,
                    new Vector3(0f, 0f, 0.02f), new Vector3(0.12f, 0.04f, 0.12f),
                    trimMaterial, Quaternion.Euler(90f, 0f, 0f));
                CreateLocalShape(root, "Wrench Nose Collar", PrimitiveType.Cylinder,
                    new Vector3(0f, 0f, 0.44f), new Vector3(0.105f, 0.07f, 0.105f),
                    warningMaterial, Quaternion.Euler(90f, 0f, 0f));
                CreateLocalShape(root, "Wrench Socket Shaft", PrimitiveType.Cylinder,
                    new Vector3(0f, 0f, 0.57f), new Vector3(0.055f, 0.07f, 0.055f),
                    machineryMaterial, Quaternion.Euler(90f, 0f, 0f));
                CreateLocalShape(root, "Wrench Socket Anvil", PrimitiveType.Cube,
                    new Vector3(0f, 0f, 0.67f), new Vector3(0.085f, 0.085f, 0.09f),
                    trimMaterial, Quaternion.identity);
                CreateLocalShape(root, "Wrench Pistol Grip", PrimitiveType.Cube,
                    new Vector3(0f, -0.22f, 0.14f), new Vector3(0.13f, 0.34f, 0.14f),
                    trimMaterial, Quaternion.Euler(-12f, 0f, 0f));
                CreateLocalShape(root, "Wrench Trigger", PrimitiveType.Cube,
                    new Vector3(0f, -0.08f, 0.25f), new Vector3(0.065f, 0.08f, 0.05f),
                    machineryMaterial, Quaternion.Euler(-12f, 0f, 0f));
                CreateLocalShape(root, "Wrench Battery", PrimitiveType.Cube,
                    new Vector3(0f, -0.41f, 0.09f), new Vector3(0.2f, 0.1f, 0.22f),
                    machineryMaterial, Quaternion.identity);
            }

            GameObject sealantPlaceholder = FindSceneObject("Sealant Gun Visual");
            if (sealantPlaceholder != null)
            {
                Transform root = PrepareToolModel(sealantPlaceholder, "Sealant Sprayer Model");
                CreateLocalShape(root, "Sealant Cartridge", PrimitiveType.Cylinder,
                    new Vector3(0f, 0.01f, 0.2f), new Vector3(0.135f, 0.28f, 0.135f),
                    machineryMaterial, Quaternion.Euler(90f, 0f, 0f));
                CreateLocalShape(root, "Sealant Rear Band", PrimitiveType.Cylinder,
                    new Vector3(0f, 0.01f, -0.08f), new Vector3(0.15f, 0.025f, 0.15f),
                    warningMaterial, Quaternion.Euler(90f, 0f, 0f));
                CreateLocalShape(root, "Sealant Front Band", PrimitiveType.Cylinder,
                    new Vector3(0f, 0.01f, 0.46f), new Vector3(0.15f, 0.025f, 0.15f),
                    warningMaterial, Quaternion.Euler(90f, 0f, 0f));
                CreateLocalShape(root, "Sealant Nozzle Collar", PrimitiveType.Cylinder,
                    new Vector3(0f, 0.01f, 0.53f), new Vector3(0.095f, 0.055f, 0.095f),
                    trimMaterial, Quaternion.Euler(90f, 0f, 0f));
                CreateLocalShape(root, "Sealant Spray Nozzle", PrimitiveType.Cylinder,
                    new Vector3(0f, 0.01f, 0.67f), new Vector3(0.045f, 0.1f, 0.045f),
                    warningMaterial, Quaternion.Euler(90f, 0f, 0f));
                CreateLocalShape(root, "Sealant Spray Shroud", PrimitiveType.Cylinder,
                    new Vector3(0f, 0.01f, 0.79f), new Vector3(0.075f, 0.035f, 0.075f),
                    machineryMaterial, Quaternion.Euler(90f, 0f, 0f));
                CreateLocalShape(root, "Sealant Pistol Grip", PrimitiveType.Cube,
                    new Vector3(0f, -0.22f, 0.08f), new Vector3(0.13f, 0.34f, 0.14f),
                    trimMaterial, Quaternion.Euler(-12f, 0f, 0f));
                CreateLocalShape(root, "Sealant Trigger", PrimitiveType.Cube,
                    new Vector3(0f, -0.08f, 0.2f), new Vector3(0.065f, 0.08f, 0.05f),
                    warningMaterial, Quaternion.Euler(-12f, 0f, 0f));
                CreateLocalShape(root, "Sealant Pressure Gauge", PrimitiveType.Sphere,
                    new Vector3(0.13f, 0.12f, 0.17f), new Vector3(0.1f, 0.1f, 0.045f),
                    warningMaterial, Quaternion.identity);
                CreateLocalShape(root, "Sealant Pump Rail", PrimitiveType.Cube,
                    new Vector3(-0.14f, -0.11f, 0.28f), new Vector3(0.045f, 0.055f, 0.42f),
                    trimMaterial, Quaternion.identity);
            }

            GameObject bridgerPlaceholder = FindSceneObject("Circuit Bridger Visual");
            if (bridgerPlaceholder != null)
            {
                Transform root = PrepareToolModel(bridgerPlaceholder, "Circuit Bridger Model");
                CreateLocalShape(root, "Bridger Main Housing", PrimitiveType.Cube,
                    new Vector3(0f, 0f, 0.2f), new Vector3(0.25f, 0.16f, 0.34f),
                    trimMaterial, Quaternion.identity);
                CreateLocalShape(root, "Bridger Pistol Grip", PrimitiveType.Cube,
                    new Vector3(0f, -0.22f, 0.08f), new Vector3(0.13f, 0.34f, 0.14f),
                    trimMaterial, Quaternion.Euler(-10f, 0f, 0f));
                CreateLocalShape(root, "Bridger Battery", PrimitiveType.Cube,
                    new Vector3(0f, -0.41f, 0.06f), new Vector3(0.21f, 0.1f, 0.2f),
                    machineryMaterial, Quaternion.identity);
                CreateLocalShape(root, "Bridger Coil Core", PrimitiveType.Cylinder,
                    new Vector3(0f, 0f, 0.4f), new Vector3(0.115f, 0.12f, 0.115f),
                    machineryMaterial, Quaternion.Euler(90f, 0f, 0f));
                CreateLocalShape(root, "Bridger Coil Rear Ring", PrimitiveType.Cylinder,
                    new Vector3(0f, 0f, 0.31f), new Vector3(0.14f, 0.025f, 0.14f),
                    warningMaterial, Quaternion.Euler(90f, 0f, 0f));
                CreateLocalShape(root, "Bridger Coil Front Ring", PrimitiveType.Cylinder,
                    new Vector3(0f, 0f, 0.49f), new Vector3(0.14f, 0.025f, 0.14f),
                    warningMaterial, Quaternion.Euler(90f, 0f, 0f));
                CreateLocalShape(root, "Bridger Fork Brace", PrimitiveType.Cube,
                    new Vector3(0f, 0f, 0.54f), new Vector3(0.3f, 0.08f, 0.08f),
                    machineryMaterial, Quaternion.identity);
                CreateLocalShape(root, "Bridger Probe Left", PrimitiveType.Cube,
                    new Vector3(-0.11f, 0f, 0.7f), new Vector3(0.045f, 0.045f, 0.32f),
                    circuitMaterial, Quaternion.identity);
                CreateLocalShape(root, "Bridger Probe Right", PrimitiveType.Cube,
                    new Vector3(0.11f, 0f, 0.7f), new Vector3(0.045f, 0.045f, 0.32f),
                    circuitMaterial, Quaternion.identity);
                CreateLocalShape(root, "Bridger Probe Tip Left", PrimitiveType.Sphere,
                    new Vector3(-0.11f, 0f, 0.88f), new Vector3(0.075f, 0.075f, 0.075f),
                    machineryMaterial, Quaternion.identity);
                CreateLocalShape(root, "Bridger Probe Tip Right", PrimitiveType.Sphere,
                    new Vector3(0.11f, 0f, 0.88f), new Vector3(0.075f, 0.075f, 0.075f),
                    machineryMaterial, Quaternion.identity);
                CreateLocalShape(root, "Bridger Status Display", PrimitiveType.Cube,
                    new Vector3(0f, 0.1f, 0.15f), new Vector3(0.13f, 0.025f, 0.11f),
                    circuitMaterial, Quaternion.Euler(-12f, 0f, 0f));
            }
        }

        private static Transform PrepareToolModel(GameObject placeholder, string modelName)
        {
            SetRendererVisible(placeholder, false);
            Transform placeholderTransform = placeholder.transform;
            placeholderTransform.localScale = Vector3.one;
            for (int index = placeholderTransform.childCount - 1; index >= 0; index--)
            {
                UnityEngine.Object.DestroyImmediate(placeholderTransform.GetChild(index).gameObject);
            }

            var model = new GameObject(modelName).transform;
            model.SetParent(placeholderTransform, false);
            return model;
        }

        private static GameObject FindSceneObject(string objectName)
        {
            Transform[] candidates = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (Transform candidate in candidates)
            {
                if (candidate.name == objectName && candidate.gameObject.scene.IsValid())
                {
                    return candidate.gameObject;
                }
            }

            return null;
        }

        private static void BuildBulkheadRibs(Transform root, Material structure, Material warning)
        {
            for (int index = 0; index < 5; index++)
            {
                float z = -8f + (index * 4f);
                CreateShape(root, $"Bulkhead {index + 1} Left", PrimitiveType.Cube,
                    new Vector3(-6.45f, 2.5f, z), new Vector3(0.22f, 4.5f, 0.32f), structure);
                CreateShape(root, $"Bulkhead {index + 1} Right", PrimitiveType.Cube,
                    new Vector3(6.45f, 2.5f, z), new Vector3(0.22f, 4.5f, 0.32f), structure);
                CreateShape(root, $"Bulkhead {index + 1} Beam", PrimitiveType.Cube,
                    new Vector3(0f, 4.45f, z), new Vector3(12.7f, 0.24f, 0.32f), structure);
                CreateShape(root, $"Bulkhead {index + 1} Marker", PrimitiveType.Cube,
                    new Vector3(0f, 4.18f, z - 0.02f), new Vector3(1.2f, 0.12f, 0.36f), warning);
            }
        }

        private static void BuildCoolingPump(
            Transform root,
            Material machinery,
            Material warning,
            Material trim,
            Material glow)
        {
            SetRendererVisible(GameObject.Find("Cooling Pump Placeholder"), false);
            Collider placeholderCollider = GameObject.Find("Cooling Pump Placeholder")?.GetComponent<Collider>();
            if (placeholderCollider != null)
            {
                placeholderCollider.enabled = false;
            }

            var pump = new GameObject("Modular Cooling Pump").transform;
            pump.SetParent(root);
            var pumpCollider = pump.gameObject.AddComponent<CapsuleCollider>();
            pumpCollider.center = new Vector3(0f, 1.05f, 5.8f);
            pumpCollider.direction = 2;
            pumpCollider.radius = 0.92f;
            pumpCollider.height = 3.55f;
            CoolingDiagnosticInteractable inspectionTarget =
                GameObject.Find("Cooling Pump Inspection Panel")?.GetComponent<CoolingDiagnosticInteractable>();
            pump.gameObject.AddComponent<ContextInteractionProxy>().Configure(inspectionTarget);
            CreateShape(pump, "Pump Drum", PrimitiveType.Cylinder,
                new Vector3(0f, 1.05f, 5.8f), new Vector3(0.9f, 1.72f, 0.9f), machinery,
                Quaternion.Euler(90f, 0f, 0f));
            CreateShape(pump, "Pump Front Ring", PrimitiveType.Cylinder,
                new Vector3(0f, 1.05f, 4.02f), new Vector3(1.02f, 0.12f, 1.02f), warning,
                Quaternion.Euler(90f, 0f, 0f));
            CreateShape(pump, "Pump Rear Ring", PrimitiveType.Cylinder,
                new Vector3(0f, 1.05f, 7.58f), new Vector3(1.02f, 0.12f, 1.02f), warning,
                Quaternion.Euler(90f, 0f, 0f));
            CreateShape(pump, "Pump Hub", PrimitiveType.Cylinder,
                new Vector3(0f, 1.05f, 3.82f), new Vector3(0.38f, 0.16f, 0.38f), trim,
                Quaternion.Euler(90f, 0f, 0f));
            CreateShape(pump, "Pump Status Core", PrimitiveType.Sphere,
                new Vector3(0f, 1.05f, 3.62f), new Vector3(0.2f, 0.2f, 0.12f), glow);
            CreateShape(pump, "Pump Foot Left", PrimitiveType.Cube,
                new Vector3(-1.05f, 0.32f, 5.8f), new Vector3(0.45f, 0.65f, 2.6f), trim);
            CreateShape(pump, "Pump Foot Right", PrimitiveType.Cube,
                new Vector3(1.05f, 0.32f, 5.8f), new Vector3(0.45f, 0.65f, 2.6f), trim);
        }

        private static void BuildPipeNetwork(
            Transform root,
            Material machinery,
            Material warning,
            Material trim,
            Material glow)
        {
            CreateShape(root, "Main Pipe Left", PrimitiveType.Cylinder,
                new Vector3(-5.9f, 2.25f, 3.1f), new Vector3(0.28f, 3.15f, 0.28f), machinery,
                Quaternion.Euler(90f, 0f, 0f));
            CreateShape(root, "Main Pipe Right", PrimitiveType.Cylinder,
                new Vector3(5.9f, 3.65f, 2.5f), new Vector3(0.22f, 3.7f, 0.22f), trim,
                Quaternion.Euler(90f, 0f, 0f));
            for (int index = 0; index < 4; index++)
            {
                float z = -1.3f + (index * 2.4f);
                CreateShape(root, $"Pipe Collar Left {index + 1}", PrimitiveType.Cylinder,
                    new Vector3(-5.9f, 2.25f, z), new Vector3(0.36f, 0.09f, 0.36f), warning,
                    Quaternion.Euler(90f, 0f, 0f));
            }

        }

        private static void BuildConsole(Transform root, Material structure, Material warning, Material glow)
        {
            SetRendererVisible(GameObject.Find("Interactive Control Console"), false);
            CreateShape(root, "Console Cabinet", PrimitiveType.Cube,
                new Vector3(-4.8f, 0.85f, 2.2f), new Vector3(1.55f, 1.65f, 1.55f), structure);
            CreateShape(root, "Console Sloped Panel", PrimitiveType.Cube,
                new Vector3(-4.8f, 1.72f, 1.88f), new Vector3(1.35f, 0.16f, 0.95f), warning,
                Quaternion.Euler(18f, 0f, 0f));
            for (int index = 0; index < 3; index++)
            {
                CreateShape(root, $"Console Indicator {index + 1}", PrimitiveType.Sphere,
                    new Vector3(-5.18f + (index * 0.38f), 1.86f, 1.68f), new Vector3(0.1f, 0.06f, 0.1f), glow);
            }
        }

        private static void BuildToolAndPipeRacks(
            Transform root,
            Material structure,
            Material machinery,
            Material warning,
            Material circuit)
        {
            SetRendererVisible(GameObject.Find("Pipe Rack Placeholder"), false);
            CreateShape(root, "Pipe Rack Spine", PrimitiveType.Cube,
                new Vector3(-5.5f, 1.15f, -4.5f), new Vector3(0.28f, 2.25f, 3.7f), structure);
            for (int index = 0; index < 3; index++)
            {
                float z = -5.6f + (index * 1.1f);
                CreateShape(root, $"Spare Pipe {index + 1}", PrimitiveType.Cylinder,
                    new Vector3(-5.12f, 1.1f, z), new Vector3(0.2f, 0.75f, 0.2f), machinery,
                    Quaternion.Euler(90f, 0f, 0f));
                CreateShape(root, $"Spare Pipe Collar {index + 1}", PrimitiveType.Cylinder,
                    new Vector3(-5.12f, 1.1f, z - 0.78f), new Vector3(0.27f, 0.08f, 0.27f), warning,
                    Quaternion.Euler(90f, 0f, 0f));
            }

            CreateShape(root, "Tool Rack Header", PrimitiveType.Cube,
                new Vector3(5.12f, 1.82f, -2.5f), new Vector3(0.22f, 0.3f, 3.45f), warning);
            CreateShape(root, "Rack Wrench Silhouette", PrimitiveType.Cylinder,
                new Vector3(4.91f, 1.05f, -3.65f), new Vector3(0.08f, 0.34f, 0.08f), warning,
                Quaternion.Euler(90f, 0f, 0f));
            CreateShape(root, "Rack Wrench Motor", PrimitiveType.Cylinder,
                new Vector3(4.88f, 1.27f, -3.65f), new Vector3(0.17f, 0.13f, 0.17f), machinery,
                Quaternion.Euler(0f, 0f, 90f));
            CreateShape(root, "Rack Wrench Socket", PrimitiveType.Cylinder,
                new Vector3(4.7f, 1.27f, -3.65f), new Vector3(0.07f, 0.08f, 0.07f), warning,
                Quaternion.Euler(0f, 0f, 90f));
            CreateShape(root, "Rack Bridger Silhouette", PrimitiveType.Cube,
                new Vector3(4.91f, 1.12f, -2.5f), new Vector3(0.12f, 0.28f, 0.34f), circuit);
            CreateShape(root, "Rack Bridger Probe A", PrimitiveType.Cylinder,
                new Vector3(4.85f, 1.12f, -2.28f), new Vector3(0.035f, 0.16f, 0.035f), warning,
                Quaternion.Euler(90f, 0f, 0f));
            CreateShape(root, "Rack Bridger Probe B", PrimitiveType.Cylinder,
                new Vector3(4.85f, 1.12f, -2.72f), new Vector3(0.035f, 0.16f, 0.035f), warning,
                Quaternion.Euler(90f, 0f, 0f));
            CreateShape(root, "Rack Sealant Silhouette", PrimitiveType.Cylinder,
                new Vector3(4.91f, 1.05f, -1.35f), new Vector3(0.13f, 0.3f, 0.13f), machinery,
                Quaternion.Euler(90f, 0f, 0f));
            CreateShape(root, "Rack Sealant Nozzle", PrimitiveType.Cylinder,
                new Vector3(4.91f, 1.05f, -0.98f), new Vector3(0.045f, 0.12f, 0.045f), warning,
                Quaternion.Euler(90f, 0f, 0f));
            CreateShape(root, "Rack Sealant Grip", PrimitiveType.Cube,
                new Vector3(4.91f, 0.82f, -1.43f), new Vector3(0.14f, 0.34f, 0.16f), structure,
                Quaternion.Euler(-12f, 0f, 0f));
        }

        private static void BuildInteractiveGameplayProps(
            Transform root,
            Material structure,
            Material machinery,
            Material warning,
            Material trim,
            Material glow,
            Material circuit)
        {
            GameObject pressureGauge = GameObject.Find("Diagnostic Pressure Gauge");
            if (pressureGauge != null)
            {
                SetRendererVisible(pressureGauge, false);
                Transform model = CreateUnscaledModelRoot(pressureGauge.transform, "Pressure Gauge Model");
                CreateLocalShape(model, "Pressure Gauge Housing", PrimitiveType.Cylinder,
                    Vector3.zero, new Vector3(0.5f, 0.14f, 0.5f), trim, Quaternion.Euler(0f, 0f, 90f));
                CreateLocalShape(model, "Pressure Gauge Face", PrimitiveType.Cylinder,
                    new Vector3(0.16f, 0f, 0f), new Vector3(0.4f, 0.04f, 0.4f), glow, Quaternion.Euler(0f, 0f, 90f));
                CreateLocalShape(model, "Pressure Gauge Needle Model", PrimitiveType.Cube,
                    new Vector3(0.205f, 0.1f, 0f), new Vector3(0.04f, 0.3f, 0.035f), warning,
                    Quaternion.Euler(0f, 0f, -32f));
                CreateLocalShape(model, "Pressure Gauge Hub", PrimitiveType.Sphere,
                    new Vector3(0.22f, 0f, 0f), new Vector3(0.09f, 0.09f, 0.09f), warning, Quaternion.identity);
            }

            GameObject pumpPanel = GameObject.Find("Cooling Pump Inspection Panel");
            if (pumpPanel != null)
            {
                SetRendererVisible(pumpPanel, false);
                Transform model = CreateUnscaledModelRoot(pumpPanel.transform, "Pump Inspection Panel Model");
                CreateLocalShape(model, "Pump Inspection Housing", PrimitiveType.Cube,
                    Vector3.zero, new Vector3(1.05f, 0.75f, 0.2f), structure, Quaternion.identity);
                CreateLocalShape(model, "Pump Inspection Screen", PrimitiveType.Cube,
                    new Vector3(0f, 0.08f, -0.13f), new Vector3(0.68f, 0.35f, 0.07f), glow, Quaternion.identity);
                for (int index = 0; index < 3; index++)
                {
                    CreateLocalShape(model, $"Pump Inspection Indicator {index + 1}", PrimitiveType.Sphere,
                        new Vector3(-0.28f + index * 0.28f, -0.24f, -0.2f), new Vector3(0.08f, 0.08f, 0.05f),
                        index == 2 ? warning : glow, Quaternion.identity);
                }
            }

            GameObject interlock = GameObject.Find("Cooling Control Circuit Interlock");
            if (interlock != null)
            {
                SetRendererVisible(interlock, false);
                Transform model = CreateUnscaledModelRoot(interlock.transform, "Circuit Interlock Model");
                CreateLocalShape(model, "Circuit Interlock Cabinet", PrimitiveType.Cube,
                    Vector3.zero, new Vector3(0.34f, 1.5f, 1.45f), structure, Quaternion.identity);
                for (int index = 0; index < 3; index++)
                {
                    CreateLocalShape(model, $"Circuit Phase Node {index + 1}", PrimitiveType.Sphere,
                        new Vector3(0.22f, 0.42f - index * 0.42f, 0f), new Vector3(0.16f, 0.16f, 0.16f),
                        index == 1 ? warning : circuit, Quaternion.identity);
                }
                CreateLocalShape(model, "Circuit Bus", PrimitiveType.Cube,
                    new Vector3(0.2f, 0f, 0f), new Vector3(0.06f, 1.05f, 0.08f), glow, Quaternion.identity);
            }

            GameObject leak = GameObject.Find("Sealant Leak Demo");
            if (leak != null)
            {
                SetRendererVisible(leak, false);
                Transform model = CreateUnscaledModelRoot(leak.transform, "Leaking Pipe Model");
                CreateLocalShape(model, "Leaking Pipe Segment", PrimitiveType.Cylinder,
                    Vector3.zero, new Vector3(0.34f, 0.7f, 0.34f), machinery, Quaternion.Euler(90f, 0f, 0f));
                CreateLocalShape(model, "Leak Collar", PrimitiveType.Cylinder,
                    Vector3.zero, new Vector3(0.46f, 0.15f, 0.46f), trim, Quaternion.Euler(90f, 0f, 0f));
                CreateLocalShape(model, "Leak Glow", PrimitiveType.Sphere,
                    new Vector3(0.32f, 0f, 0f), new Vector3(0.19f, 0.12f, 0.12f), glow, Quaternion.identity);
            }

            GameObject fastener = GameObject.Find("Mechanical Fastener Demo");
            if (fastener != null)
            {
                SetRendererVisible(fastener, false);
                Transform model = CreateUnscaledModelRoot(fastener.transform, "Mechanical Joint Model");
                CreateLocalShape(model, "Mechanical Joint Flange", PrimitiveType.Cylinder,
                    Vector3.zero, new Vector3(0.58f, 0.2f, 0.58f), machinery, Quaternion.Euler(90f, 0f, 0f));
                CreateLocalShape(model, "Mechanical Joint Bolt", PrimitiveType.Cylinder,
                    new Vector3(0f, 0f, -0.25f), new Vector3(0.22f, 0.13f, 0.22f), warning,
                    Quaternion.Euler(90f, 0f, 0f));
                for (int index = 0; index < 4; index++)
                {
                    float angle = index * Mathf.PI * 0.5f;
                    CreateLocalShape(model, $"Flange Bolt {index + 1}", PrimitiveType.Sphere,
                        new Vector3(Mathf.Cos(angle) * 0.38f, Mathf.Sin(angle) * 0.38f, -0.22f),
                        new Vector3(0.09f, 0.09f, 0.07f), warning, Quaternion.identity);
                }
            }

            GameObject replacementPipe = GameObject.Find("Replacement Pipe");
            if (replacementPipe != null)
            {
                SetRendererVisible(replacementPipe, false);
                Transform model = CreateUnscaledModelRoot(replacementPipe.transform, "Replacement Pipe Model");
                CreateLocalShape(model, "Replacement Pipe Body", PrimitiveType.Cylinder,
                    Vector3.zero, new Vector3(0.28f, 0.72f, 0.28f), machinery, Quaternion.Euler(90f, 0f, 0f));
                CreateLocalShape(model, "Replacement Pipe Collar A", PrimitiveType.Cylinder,
                    new Vector3(0f, 0f, -0.76f), new Vector3(0.4f, 0.1f, 0.4f), warning,
                    Quaternion.Euler(90f, 0f, 0f));
                CreateLocalShape(model, "Replacement Pipe Collar B", PrimitiveType.Cylinder,
                    new Vector3(0f, 0f, 0.76f), new Vector3(0.4f, 0.1f, 0.4f), warning,
                    Quaternion.Euler(90f, 0f, 0f));
            }
        }

        private static void BuildMaintenanceSuitReference(
            Transform root,
            Material suit,
            Material warning,
            Material trim,
            Material visor)
        {
            var model = new GameObject("Remote Maintenance Suit Scale Reference").transform;
            model.SetParent(root);
            Vector3 origin = new Vector3(-4.65f, 0f, 7.9f);
            CreateShape(model, "Suit Torso", PrimitiveType.Capsule,
                origin + new Vector3(0f, 1.05f, 0f), new Vector3(0.43f, 0.48f, 0.34f), suit);
            CreateShape(model, "Oversized Helmet", PrimitiveType.Sphere,
                origin + new Vector3(0f, 1.72f, 0f), new Vector3(0.6f, 0.5f, 0.52f), warning);
            CreateShape(model, "Helmet Visor", PrimitiveType.Sphere,
                origin + new Vector3(0f, 1.72f, -0.39f), new Vector3(0.42f, 0.26f, 0.12f), visor);
            CreateShape(model, "Tool Backpack", PrimitiveType.Cube,
                origin + new Vector3(0f, 1.1f, 0.38f), new Vector3(0.58f, 0.72f, 0.26f), trim);
            CreateShape(model, "Left Glove", PrimitiveType.Sphere,
                origin + new Vector3(-0.48f, 1.02f, 0f), new Vector3(0.24f, 0.2f, 0.24f), warning);
            CreateShape(model, "Right Glove", PrimitiveType.Sphere,
                origin + new Vector3(0.48f, 1.02f, 0f), new Vector3(0.24f, 0.2f, 0.24f), warning);
            CreateShape(model, "Left Boot", PrimitiveType.Cube,
                origin + new Vector3(-0.22f, 0.22f, -0.05f), new Vector3(0.34f, 0.42f, 0.48f), trim);
            CreateShape(model, "Right Boot", PrimitiveType.Cube,
                origin + new Vector3(0.22f, 0.22f, -0.05f), new Vector3(0.34f, 0.42f, 0.48f), trim);
        }

        private static GameObject CreateShape(
            Transform parent,
            string name,
            PrimitiveType primitiveType,
            Vector3 position,
            Vector3 scale,
            Material material,
            Quaternion rotation = default)
        {
            GameObject shape = GameObject.CreatePrimitive(primitiveType);
            shape.name = name;
            shape.transform.SetParent(parent);
            shape.transform.SetPositionAndRotation(position, rotation == default ? Quaternion.identity : rotation);
            shape.transform.localScale = scale;
            shape.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(shape.GetComponent<Collider>());
            return shape;
        }

        private static GameObject CreateLocalShape(
            Transform parent,
            string name,
            PrimitiveType primitiveType,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            Quaternion localRotation)
        {
            GameObject shape = GameObject.CreatePrimitive(primitiveType);
            shape.name = name;
            shape.transform.SetParent(parent, false);
            shape.transform.localPosition = localPosition;
            shape.transform.localRotation = localRotation;
            shape.transform.localScale = localScale;
            shape.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(shape.GetComponent<Collider>());
            return shape;
        }

        private static Transform CreateUnscaledModelRoot(Transform parent, string name)
        {
            var model = new GameObject(name).transform;
            model.SetParent(parent, false);
            Vector3 scale = parent.localScale;
            model.localScale = new Vector3(
                Mathf.Approximately(scale.x, 0f) ? 1f : 1f / scale.x,
                Mathf.Approximately(scale.y, 0f) ? 1f : 1f / scale.y,
                Mathf.Approximately(scale.z, 0f) ? 1f : 1f / scale.z);
            return model;
        }

        private static void SetRendererVisible(GameObject target, bool visible)
        {
            Renderer renderer = target != null ? target.GetComponent<Renderer>() : null;
            if (renderer != null)
            {
                renderer.enabled = visible;
            }
        }
    }
}
