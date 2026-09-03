using System.Linq;
using FunGame.Incident;
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

                Assert.That(incident, Is.Not.Null);
                Assert.That(incident.Phase, Is.EqualTo(CoolingIncidentPhase.AssessSymptoms));
                Assert.That(layout, Is.Not.Null);
                Assert.That(layout.LayoutCount, Is.EqualTo(3));
                Assert.That(metrics, Is.Not.Null);
                Assert.That(diagnostics.Length, Is.EqualTo(2));
                Assert.That(diagnostics.Select(item => item.Kind), Does.Contain(CoolingDiagnosticInteractable.DiagnosticKind.PressureGauge));
                Assert.That(diagnostics.Select(item => item.Kind), Does.Contain(CoolingDiagnosticInteractable.DiagnosticKind.PumpHousing));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static T FindInRoots<T>(GameObject[] roots) where T : Component
        {
            return roots.Select(root => root.GetComponentInChildren<T>(true)).FirstOrDefault(component => component != null);
        }
    }
}
