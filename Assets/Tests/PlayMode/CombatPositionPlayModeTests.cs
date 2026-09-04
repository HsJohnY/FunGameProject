using System.Collections;
using System.Linq;
using FunGame.Combat;
using FunGame.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace FunGame.Tests.PlayMode
{
    public sealed class CombatPositionPlayModeTests
    {
        [UnityTest]
        public IEnumerator EnemiesResetInTheirOwnCompartmentAndRemainExposed()
        {
            yield return SceneManager.LoadSceneAsync("SinglePlayer_ThreeChapterDemo", LoadSceneMode.Additive);
            Scene scene = SceneManager.GetSceneByName("SinglePlayer_ThreeChapterDemo");
            Object.FindFirstObjectByType<GameMenuController>()?.EnterGameplayForAutomation();
            var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<InterferenceEnemy>(true)).ToArray();
            foreach (InterferenceEnemy enemy in all) enemy.enabled = false;
            Object.FindFirstObjectByType<FunGame.Demo.SinglePlayerDemoController>().enabled = false;
            Object.FindFirstObjectByType<FunGame.Demo.DemoChapterPresentation>().enabled = false;
            foreach (InterferenceEnemy enemy in all)
            {
                for (Transform parent = enemy.transform.parent; parent != null; parent = parent.parent) parent.gameObject.SetActive(true);
                Vector3 expected = enemy.transform.position;
                enemy.ResetEnemy();
                Assert.That(Vector3.Distance(enemy.transform.position, expected), Is.LessThan(0.01f), enemy.TargetId + " reset crossed compartments");
                Assert.That(Mathf.Abs(enemy.transform.position.z - enemy.DefenseTarget.transform.position.z), Is.LessThan(12f), enemy.TargetId + " left its room");
                enemy.transform.position = enemy.AttackPosition;
                Physics.SyncTransforms();
                float radius = enemy.GetComponent<CapsuleCollider>().radius * Mathf.Max(enemy.transform.lossyScale.x, enemy.transform.lossyScale.z);
                foreach (Collider obstacle in Physics.OverlapSphere(enemy.AttackPosition, radius + 1.2f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (obstacle.GetComponentInParent<InterferenceEnemy>() != null || obstacle.bounds.max.y < 0.2f) continue;
                    Assert.Fail(enemy.TargetId + " too close to " + obstacle.name);
                }
                bool hittable = new[] { Vector3.back, Vector3.left, Vector3.right, Vector3.forward }.Any(direction =>
                    Physics.Raycast(enemy.transform.position + direction * 2f, -direction, out RaycastHit hit, 2.5f, ~0, QueryTriggerInteraction.Ignore) &&
                    hit.collider.GetComponentInParent<InterferenceEnemy>() == enemy);
                Assert.That(hittable, Is.True, enemy.TargetId + " has no clear attack ray");
                enemy.SetEncounterActive(false);
            }
            foreach (InterferenceEnemy enemy in all.GroupBy(e => e.Behavior).Select(g => g.First()).Concat(new[] { all.Last() }).Distinct())
            {
                enemy.DefenseTarget.Configure(10000);
                enemy.ResetEnemy();
                enemy.enabled = true;
                yield return new WaitForSeconds(6f);
                enemy.enabled = false;
                Assert.That(enemy.IsAtCombatPosition(enemy.transform.position), Is.True, enemy.TargetId + " failed to reach open attack position");
                enemy.SetEncounterActive(false);
            }
            yield return SceneManager.UnloadSceneAsync(scene);
        }
    }
}
