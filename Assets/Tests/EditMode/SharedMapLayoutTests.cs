using System.Linq;
using FunGame.Combat;
using FunGame.Player;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FunGame.Tests.EditMode
{
    public sealed class SharedMapLayoutTests
    {
        [Test]
        public void BothModesHaveIdenticalMapGeometryAndAuthoredEncounters()
        {
            Scene solo = EditorSceneManager.OpenScene("Assets/Game/Scenes/SinglePlayer_ThreeChapterDemo.unity", OpenSceneMode.Additive);
            Scene coop = EditorSceneManager.OpenScene("Assets/Game/Scenes/M4_CoopThreeChapterDemo.unity", OpenSceneMode.Additive);
            try
            {
                MeshFilter[] source = solo.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MeshFilter>(true))
                    .Where(m => m.GetComponentInParent<FirstPersonController>() == null && m.GetComponentInParent<InterferenceEnemy>() == null).ToArray();
                var destination = coop.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MeshFilter>(true))
                    .Where(m => m.GetComponentInParent<InterferenceEnemy>() == null)
                    .ToDictionary(m => Path(m.transform));
                Assert.That(source.Length, Is.GreaterThan(200));
                foreach (MeshFilter mesh in source)
                {
                    Assert.That(destination.TryGetValue(Path(mesh.transform), out MeshFilter matching), Is.True, Path(mesh.transform));
                    Assert.That(matching.sharedMesh, Is.EqualTo(mesh.sharedMesh));
                    Assert.That(Vector3.Distance(mesh.transform.position, matching.transform.position), Is.LessThan(0.001f), Path(mesh.transform));
                    Assert.That(mesh.transform.lossyScale, Is.EqualTo(matching.transform.lossyScale));
                    Assert.That(Quaternion.Angle(mesh.transform.rotation, matching.transform.rotation), Is.LessThan(0.01f));
                }
                var copies = coop.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<InterferenceEnemy>(true)).ToDictionary(e => e.TargetId);
                foreach (InterferenceEnemy enemy in solo.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<InterferenceEnemy>(true)))
                {
                    InterferenceEnemy copy = copies[enemy.TargetId];
                    Assert.That(enemy.HasCombatPosition, Is.True, enemy.TargetId);
                    Assert.That(copy.AttackPosition, Is.EqualTo(enemy.AttackPosition));
                    Assert.That(copy.ApproachPosition, Is.EqualTo(enemy.ApproachPosition));
                    Assert.That(copy.transform.position, Is.EqualTo(enemy.transform.position));
                    Assert.That(copy.MaxHealth, Is.EqualTo(enemy.MaxHealth));
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(coop, true);
                EditorSceneManager.CloseScene(solo, true);
            }
        }

        private static string Path(Transform item) => item.parent == null ? item.name : Path(item.parent) + "/" + item.name + "[" + item.GetSiblingIndex() + "]";
    }
}
