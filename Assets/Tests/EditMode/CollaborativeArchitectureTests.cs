using System;
using System.IO;
using System.Linq;
using FunGame.Combat;
using FunGame.Content;
using FunGame.Demo;
using FunGame.Editor;
using FunGame.Incident;
using FunGame.Networking;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FunGame.Tests.EditMode
{
    public sealed class CollaborativeArchitectureTests
    {
        [Test]
        public void CanonicalCompositionPreservesBindingsAndSharesDefinitions()
        {
            Scene scene = EditorSceneManager.OpenScene(SinglePlayerDemoBootstrap.ScenePath, OpenSceneMode.Additive);
            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                var context = roots.SelectMany(r => r.GetComponentsInChildren<ExpeditionContext>(true)).Single();
                Assert.That(context.Campaign, Is.Not.Null);
                Assert.That(context.CoolingCombat, Is.Not.Null);
                Assert.That(context.Enemies.Count, Is.GreaterThan(20));
                Assert.That(context.Enemies.Select(e => e.TargetId).Distinct().Count(), Is.EqualTo(context.Enemies.Count));
                foreach (InterferenceEnemy enemy in context.Enemies)
                {
                    Assert.That(enemy.Definition, Is.Not.Null, enemy.TargetId);
                    Assert.That(enemy.DefenseTarget, Is.Not.Null, enemy.TargetId);
                    Assert.That(enemy.DefenseTarget.gameObject.scene, Is.EqualTo(scene));
                    Assert.That(PrefabUtility.IsPartOfPrefabInstance(enemy), Is.True);
                }
                var incident = roots.SelectMany(r => r.GetComponentsInChildren<CoolingIncidentController>(true)).Single();
                var network = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Content/Networking/M4_CoolingIncident.prefab");
                Assert.That(incident.Definition, Is.SameAs(network.GetComponent<NetworkCoolingIncidentController>().Definition));
                foreach (MonoBehaviour component in roots.SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true)))
                {
                    Assert.That(component, Is.Not.Null, "Missing script after prefab extraction.");
                    var data = new SerializedObject(component);
                    SerializedProperty property = data.GetIterator();
                    while (property.Next(true))
                    {
                        if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                        Assert.That(property.objectReferenceValue != null || property.objectReferenceInstanceIDValue == 0, Is.True,
                            component.name + "." + property.propertyPath + " contains a broken reference.");
                        var target = property.objectReferenceValue as Component;
                        if (target != null && !EditorUtility.IsPersistent(target)) Assert.That(target.gameObject.scene, Is.EqualTo(scene));
                    }
                }
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void EnvironmentCompositionIsStaticAndRepeatable()
        {
            EnvironmentSceneDefinition[] definitions = ModularContentBuilder.Definitions();
            Assert.That(definitions.Length, Is.EqualTo(3));
            foreach (EnvironmentSceneDefinition definition in definitions)
            {
                Assert.That(definition.EnvironmentPrefab.GetComponentsInChildren<Transform>(true).Length, Is.GreaterThan(1));
                Assert.That(definition.EnvironmentPrefab.GetComponentsInChildren<MonoBehaviour>(true), Is.Empty);
                byte[] before = File.ReadAllBytes(definition.ScenePath);
                ModularContentBuilder.GenerateEnvironmentScene(definition);
                ModularContentBuilder.GenerateEnvironmentScene(definition);
                CollectionAssert.AreEqual(before, File.ReadAllBytes(definition.ScenePath), "Clean generation rewrote " + definition.ScenePath);
            }
        }

        [Test]
        public void LegacyGeneratorsCannotOverwriteAuthoredModules()
        {
            Assert.Throws<InvalidOperationException>(() => M4CoopIntegrationBootstrap.ConfigureCurrent());
            Assert.Throws<InvalidOperationException>(() => SinglePlayerDemoBootstrap.ConfigureCurrent());
        }

        [Test]
        public void ContentRegistryRejectsDuplicateIdsAndResolvesIndependentlyOfOrder()
        {
            var root = new GameObject("Context Test");
            try
            {
                var registry = new EnemyTemplateRegistry();
                var first = new GameObject("first"); first.transform.SetParent(root.transform);
                var second = new GameObject("second"); second.transform.SetParent(root.transform);
                var a = first.AddComponent<InterferenceEnemy>(); a.ConfigureIdentity("enemy-a", "A");
                var b = second.AddComponent<InterferenceEnemy>(); b.ConfigureIdentity("enemy-b", "B");
                registry.Register(new[] { b, a });
                Assert.That(registry.Resolve("enemy-a"), Is.SameAs(a));
                b.ConfigureIdentity("enemy-a", "B");
                Assert.Throws<InvalidOperationException>(() => registry.Register(new[] { b, a }));
                Assert.That(registry.Resolve("enemy-a"), Is.SameAs(a), "Rejected registration must preserve the previous map.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
