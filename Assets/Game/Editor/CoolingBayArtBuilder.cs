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
            Material glowMaterial)
        {
            var root = new GameObject("Low Poly Cooling Bay Art Pass").transform;
            BuildBulkheadRibs(root, structureMaterial, warningMaterial);
            BuildCoolingPump(root, machineryMaterial, warningMaterial, trimMaterial, glowMaterial);
            BuildPipeNetwork(root, machineryMaterial, warningMaterial, trimMaterial, glowMaterial);
            BuildConsole(root, structureMaterial, warningMaterial, glowMaterial);
            BuildToolAndPipeRacks(root, structureMaterial, machineryMaterial, warningMaterial);
            BuildMaintenanceSuitReference(root, structureMaterial, warningMaterial, trimMaterial, glowMaterial);
        }

        public static void EnhanceFirstPersonTools(Material machineryMaterial, Material warningMaterial, Material trimMaterial)
        {
            GameObject wrenchPlaceholder = GameObject.Find("Impact Wrench Visual");
            if (wrenchPlaceholder != null)
            {
                SetRendererVisible(wrenchPlaceholder, false);
                Transform root = wrenchPlaceholder.transform;
                root.localScale = Vector3.one;
                CreateLocalShape(root, "Wrench Grip", PrimitiveType.Cylinder,
                    new Vector3(0f, -0.01f, 0.03f), new Vector3(0.065f, 0.27f, 0.065f),
                    trimMaterial, Quaternion.Euler(90f, 0f, 0f));
                CreateLocalShape(root, "Wrench Motor", PrimitiveType.Cylinder,
                    new Vector3(0f, 0f, 0.33f), new Vector3(0.14f, 0.13f, 0.14f),
                    warningMaterial, Quaternion.Euler(0f, 0f, 90f));
                CreateLocalShape(root, "Wrench Socket", PrimitiveType.Cylinder,
                    new Vector3(0.15f, 0f, 0.33f), new Vector3(0.06f, 0.08f, 0.06f),
                    machineryMaterial, Quaternion.Euler(0f, 0f, 90f));
            }

            GameObject sealantPlaceholder = GameObject.Find("Sealant Gun Visual");
            if (sealantPlaceholder != null)
            {
                SetRendererVisible(sealantPlaceholder, false);
                Transform root = sealantPlaceholder.transform;
                root.localScale = Vector3.one;
                CreateLocalShape(root, "Sealant Cartridge", PrimitiveType.Cylinder,
                    new Vector3(0f, -0.03f, 0.1f), new Vector3(0.105f, 0.24f, 0.105f),
                    machineryMaterial, Quaternion.Euler(90f, 0f, 0f));
                CreateLocalShape(root, "Sealant Nozzle", PrimitiveType.Cylinder,
                    new Vector3(0f, -0.03f, 0.41f), new Vector3(0.035f, 0.1f, 0.035f),
                    warningMaterial, Quaternion.Euler(90f, 0f, 0f));
                CreateLocalShape(root, "Sealant Grip", PrimitiveType.Cube,
                    new Vector3(0f, -0.17f, 0.07f), new Vector3(0.11f, 0.25f, 0.12f),
                    trimMaterial, Quaternion.Euler(-12f, 0f, 0f));
            }

            GameObject bridgerPlaceholder = GameObject.Find("Circuit Bridger Visual");
            if (bridgerPlaceholder != null)
            {
                SetRendererVisible(bridgerPlaceholder, false);
                Transform root = bridgerPlaceholder.transform;
                root.localScale = Vector3.one;
                CreateLocalShape(root, "Bridger Grip", PrimitiveType.Cube,
                    new Vector3(0f, -0.12f, 0.08f), new Vector3(0.11f, 0.24f, 0.13f),
                    trimMaterial, Quaternion.Euler(-10f, 0f, 0f));
                CreateLocalShape(root, "Bridger Coil", PrimitiveType.Cylinder,
                    new Vector3(0f, 0f, 0.24f), new Vector3(0.12f, 0.18f, 0.12f),
                    machineryMaterial, Quaternion.Euler(90f, 0f, 0f));
                CreateLocalShape(root, "Bridger Fork Left", PrimitiveType.Cube,
                    new Vector3(-0.07f, 0f, 0.48f), new Vector3(0.035f, 0.045f, 0.22f),
                    warningMaterial, Quaternion.identity);
                CreateLocalShape(root, "Bridger Fork Right", PrimitiveType.Cube,
                    new Vector3(0.07f, 0f, 0.48f), new Vector3(0.035f, 0.045f, 0.22f),
                    warningMaterial, Quaternion.identity);
            }
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
            var pump = new GameObject("Modular Cooling Pump").transform;
            pump.SetParent(root);
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

            CreateShape(root, "Pressure Gauge Housing", PrimitiveType.Cylinder,
                new Vector3(-5.55f, 2.25f, 0.3f), new Vector3(0.38f, 0.1f, 0.38f), trim,
                Quaternion.Euler(0f, 0f, 90f));
            CreateShape(root, "Pressure Gauge Face", PrimitiveType.Cylinder,
                new Vector3(-5.43f, 2.25f, 0.3f), new Vector3(0.29f, 0.04f, 0.29f), glow,
                Quaternion.Euler(0f, 0f, 90f));
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

        private static void BuildToolAndPipeRacks(Transform root, Material structure, Material machinery, Material warning)
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
                new Vector3(5.12f, 1.82f, -2.5f), new Vector3(0.22f, 0.3f, 4.9f), warning);
            CreateShape(root, "Rack Wrench Silhouette", PrimitiveType.Cylinder,
                new Vector3(4.91f, 1.05f, -4f), new Vector3(0.08f, 0.42f, 0.08f), warning,
                Quaternion.Euler(90f, 0f, 0f));
            CreateShape(root, "Rack Sealant Silhouette", PrimitiveType.Cylinder,
                new Vector3(4.91f, 1.05f, -2.5f), new Vector3(0.13f, 0.34f, 0.13f), machinery,
                Quaternion.Euler(90f, 0f, 0f));
            CreateShape(root, "Rack Bridger Silhouette", PrimitiveType.Cube,
                new Vector3(4.91f, 1.05f, -1f), new Vector3(0.12f, 0.16f, 0.58f), warning,
                Quaternion.Euler(90f, 0f, 0f));
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
