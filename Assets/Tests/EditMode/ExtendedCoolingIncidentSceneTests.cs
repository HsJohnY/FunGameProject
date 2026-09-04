using System.Linq;
using FunGame.Incident;
using FunGame.Interaction;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FunGame.Tests.EditMode
{
    public sealed class ExtendedCoolingIncidentSceneTests
    {
        private const string ScenePath = "Assets/Game/Scenes/M1_CoolingBay.unity";

        [Test]
        public void 冷却舱场景包含扩展诊断布局与指标组件()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                CoolingIncidentController incident = FindInRoots<CoolingIncidentController>(roots);
                CoolingIncidentLayoutController layout = FindInRoots<CoolingIncidentLayoutController>(roots);
                CoolingIncidentMetricsTracker metrics = FindInRoots<CoolingIncidentMetricsTracker>(roots);
                CoolingDiagnosticInteractable[] diagnostics = roots
                    .SelectMany(root => root.GetComponentsInChildren<CoolingDiagnosticInteractable>(true))
                    .ToArray();
                Transform pumpModel = roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .FirstOrDefault(item => item.name == "Modular Cooling Pump");
                Transform fastener = roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .FirstOrDefault(item => item.name == "Mechanical Fastener Demo");

                Assert.That(incident, Is.Not.Null);
                Assert.That(incident.Phase, Is.EqualTo(CoolingIncidentPhase.AssessSymptoms));
                Assert.That(layout, Is.Not.Null);
                Assert.That(layout.LayoutCount, Is.EqualTo(3));
                Assert.That(metrics, Is.Not.Null);
                Assert.That(diagnostics.Length, Is.EqualTo(2));
                Assert.That(diagnostics.Select(item => item.Kind), Does.Contain(CoolingDiagnosticInteractable.DiagnosticKind.PressureGauge));
                Assert.That(diagnostics.Select(item => item.Kind), Does.Contain(CoolingDiagnosticInteractable.DiagnosticKind.PumpHousing));
                Assert.That(pumpModel, Is.Not.Null);
                Assert.That(pumpModel.GetComponent<CapsuleCollider>(), Is.Not.Null,
                    "冷却泵应使用贴合圆筒的碰撞体，不能用覆盖检查面板的大方盒。");
                Assert.That(pumpModel.GetComponent<ContextInteractionProxy>()?.HasTarget, Is.True,
                    "命中泵外壳也应转发到泵检查交互。");
                CoolingDiagnosticInteractable pumpInspection = diagnostics.First(item =>
                    item.Kind == CoolingDiagnosticInteractable.DiagnosticKind.PumpHousing);
                Assert.That(pumpModel.GetComponent<Collider>().bounds.Intersects(
                    pumpInspection.GetComponent<Collider>().bounds), Is.False,
                    "泵体碰撞不能覆盖检查面板的左半边。");
                Assert.That(pumpModel.GetComponent<Collider>().bounds.Intersects(
                    fastener.GetComponent<Collider>().bounds), Is.False,
                    "泵体碰撞不能侵入机械接头造成中段卡点。");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void 关键交互物的碰撞范围覆盖玩家看到的模型()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                AssertModelCovered(roots, "Diagnostic Pressure Gauge", "Pressure Gauge Model");
                AssertModelCovered(roots, "Cooling Pump Inspection Panel", "Pump Inspection Panel Model");
                AssertModelCovered(roots, "Cooling Control Circuit Interlock", "Circuit Interlock Model");
                AssertModelCovered(roots, "Sealant Leak Demo", "Leaking Pipe Model");
                AssertModelCovered(roots, "Mechanical Fastener Demo", "Mechanical Joint Model");
                AssertModelCovered(roots, "Replacement Pipe", "Replacement Pipe Model");
                AssertRendererCovered(roots, "Impact Wrench Rack", "Rack Wrench Motor");
                AssertRendererCovered(roots, "Circuit Bridger Rack", "Rack Bridger Silhouette");
                AssertRendererCovered(roots, "Sealant Gun Rack", "Rack Sealant Nozzle");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void AssertModelCovered(GameObject[] roots, string targetName, string modelName)
        {
            Transform target = FindTransform(roots, targetName);
            Transform model = FindTransform(roots, modelName);
            Assert.That(target, Is.Not.Null, targetName);
            Assert.That(model, Is.Not.Null, modelName);
            Collider collider = target.GetComponent<Collider>();
            Assert.That(collider, Is.Not.Null, targetName + " 缺少碰撞体");
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                AssertBoundsContains(collider.bounds, renderer.bounds, targetName + " / " + renderer.name);
            }
        }

        private static void AssertRendererCovered(GameObject[] roots, string targetName, string rendererName)
        {
            Transform target = FindTransform(roots, targetName);
            Renderer renderer = FindTransform(roots, rendererName)?.GetComponent<Renderer>();
            Assert.That(target, Is.Not.Null, targetName);
            Assert.That(renderer, Is.Not.Null, rendererName);
            AssertBoundsContains(target.GetComponent<Collider>().bounds, renderer.bounds, targetName + " / " + rendererName);
        }

        private static void AssertBoundsContains(Bounds colliderBounds, Bounds rendererBounds, string message)
        {
            const float tolerance = 0.015f;
            Bounds expanded = colliderBounds;
            expanded.Expand(tolerance * 2f);
            Assert.That(expanded.Contains(rendererBounds.min) && expanded.Contains(rendererBounds.max), Is.True,
                message + " 的可见范围超出交互碰撞体");
        }

        private static Transform FindTransform(GameObject[] roots, string name)
        {
            return roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(item => item.name == name);
        }

        private static T FindInRoots<T>(GameObject[] roots) where T : Component
        {
            return roots.Select(root => root.GetComponentInChildren<T>(true)).FirstOrDefault(component => component != null);
        }
    }
}
