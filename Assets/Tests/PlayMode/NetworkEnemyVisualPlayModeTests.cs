using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using FunGame.Networking;
using FunGame.Tools;
using FunGame.UI;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace FunGame.Tests.PlayMode
{
    public sealed class NetworkEnemyVisualPlayModeTests
    {
        private Scene _scene;
        private NetworkManager _manager;
        private NetworkCombatEnemy[] _enemies;

        [UnitySetUp]
        public IEnumerator StartEncounter()
        {
            FunGame.Demo.SharedMapModeController.NextMode = FunGame.Demo.ExpeditionMode.Cooperative;
            yield return SceneManager.LoadSceneAsync(GameMenuController.CooperativeScene, LoadSceneMode.Additive);
            _scene = SceneManager.GetSceneByName(GameMenuController.CooperativeScene);
            yield return ModularSceneTestUtility.WaitUntilReady(_scene);
            Object.FindFirstObjectByType<GameMenuController>().EnterGameplayForAutomation();
            var session = Object.FindFirstObjectByType<NetworkSessionController>();
            _manager = Object.FindFirstObjectByType<NetworkManager>();
            Assert.That(session.TrySetEndpointInput("127.0.0.1", "17846"), Is.True);
            Assert.That(session.StartHost(), Is.True, session.StatusText);
            yield return null;

            var incident = Object.FindFirstObjectByType<NetworkCoolingIncidentController>();
            Assert.That(incident.TryExecuteServer(NetworkIncidentAction.InspectPressure, ToolKind.None), Is.True);
            Assert.That(incident.TryExecuteServer(NetworkIncidentAction.InspectPump, ToolKind.None), Is.True);
            for (int i = 0; i < 3; i++)
                Assert.That(incident.TryExecuteServer(NetworkIncidentAction.BridgeCircuit, ToolKind.CircuitBridger), Is.True);
            for (int i = 0; i < 4; i++)
                Assert.That(incident.TryExecuteServer(NetworkIncidentAction.SealLeak, ToolKind.SealantGun), Is.True);
            yield return null;
            _enemies = Object.FindObjectsByType<NetworkCombatEnemy>(FindObjectsSortMode.None);
            Assert.That(_enemies.Length, Is.EqualTo(7));
            Assert.That(_enemies.All(enemy => enemy.Template != null), Is.True);
            // Freeze movement and drive only presentation, so physics/NGO/test-runner allocations
            // cannot contaminate the measurement of this per-frame hot path.
            foreach (NetworkCombatEnemy enemy in _enemies) enemy.enabled = false;
        }

        [UnityTearDown]
        public IEnumerator StopEncounter()
        {
            if (_manager != null) _manager.Shutdown();
            yield return null;
            if (_manager != null)
            {
                float deadline = Time.realtimeSinceStartup + 5f;
                while (_manager.ShutdownInProgress && Time.realtimeSinceStartup < deadline) yield return null;
                Object.Destroy(_manager.gameObject);
                yield return null;
            }
            if (_scene.IsValid() && _scene.isLoaded) yield return SceneManager.UnloadSceneAsync(_scene);
        }

        [Test]
        public void AuthoredEnemyPresentation_DoesNotAllocateAfterWarmup()
        {
            Action[] refresh = _enemies.Select(BindLateUpdate).ToArray();
            foreach (NetworkCombatEnemy enemy in _enemies)
            {
                SetState(enemy, "deploymentAt", _manager.ServerTime.Time - 1d);
                SetState(enemy, "telegraphing", true);
            }
            for (int warmup = 0; warmup < 10; warmup++)
                foreach (Action update in refresh) update();

            const int frames = 300;
            // Use Unity's GC.Alloc marker, as the bundled Test Framework does:
            // the Mono runtime can report zero for GC.GetAllocatedBytesForCurrentThread.
            var recorder = Recorder.Get("GC.Alloc");
            Assert.That(recorder.isValid, Is.True);
            recorder.enabled = false;
            recorder.FilterToCurrentThread();
            recorder.enabled = true;
            try
            {
                for (int frame = 0; frame < frames; frame++)
                    foreach (Action update in refresh) update();
            }
            finally
            {
                recorder.enabled = false;
                recorder.CollectFromAllThreads();
            }
            int allocated = recorder.sampleBlockCount;

            TestContext.WriteLine($"Enemy presentation: {_enemies.Length} enemies x {frames} frames = {allocated} GC allocations.");
            Assert.That(allocated, Is.Zero, "Steady-state enemy presentation must not allocate each frame.");
        }

        [Test]
        public void AuthoredEnemyPresentation_TracksDeploymentStatusAndDefeat()
        {
            NetworkCombatEnemy enemy = _enemies.Single(e => e.IsShielded);
            Action refresh = BindLateUpdate(enemy);
            MeshRenderer[] meshes = enemy.GetComponentsInChildren<MeshRenderer>();
            Collider[] colliders = enemy.GetComponentsInChildren<Collider>();
            var link = enemy.GetComponent<LineRenderer>();
            Assert.That(meshes.Length, Is.GreaterThan(0));

            SetState(enemy, "deploymentAt", _manager.ServerTime.Time + 10d);
            refresh();
            Assert.That(meshes.All(mesh => !mesh.enabled), Is.True);
            Assert.That(colliders.All(collider => !collider.enabled), Is.True);
            Assert.That(link.enabled, Is.False);

            SetState(enemy, "deploymentAt", _manager.ServerTime.Time - 1d);
            refresh();
            Assert.That(meshes.All(mesh => mesh.enabled), Is.True);
            Assert.That(colliders.All(collider => collider.enabled), Is.True);
            AssertTint(enemy, new Color(0.25f, 0.45f, 1f));

            SetState(enemy, "stunnedUntil", _manager.ServerTime.Time + 10d);
            refresh();
            AssertTint(enemy, new Color(0.15f, 1f, 0.95f));
            SetState(enemy, "stunnedUntil", 0d);
            SetState(enemy, "slowedUntil", _manager.ServerTime.Time + 10d);
            refresh();
            AssertTint(enemy, Color.Lerp(new Color(0.25f, 0.45f, 1f), new Color(0.4f, 0.8f, 1f), 0.65f));
            SetState(enemy, "slowedUntil", 0d);
            SetState(enemy, "telegraphing", true);
            refresh();
            Assert.That(link.enabled, Is.True);
            Assert.That(link.positionCount, Is.EqualTo(3));
            AssertTint(enemy, Color.Lerp(new Color(0.25f, 0.45f, 1f), new Color(1f, 0.75f, 0.05f), 0.75f));

            SetState(enemy, "health", 0);
            refresh();
            Assert.That(meshes.All(mesh => !mesh.enabled), Is.True);
            Assert.That(colliders.All(collider => !collider.enabled), Is.True);
            Assert.That(link.enabled, Is.False);
        }

        [Test]
        public void ReplacingTemplate_RefreshesDescendantVisibilityAndCollision()
        {
            NetworkCombatEnemy enemy = _enemies[0];
            var source = _enemies.First(e => e.Template != enemy.Template).Template;
            // Runtime-only fixture geometry exercises a model with nested meshes/colliders.
            // It is cloned through the production template binding path, never saved to assets.
            var model = new GameObject("Visual cache test model");
            model.transform.SetParent(source.transform, false);
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = "Nested visual cache test part";
            part.transform.SetParent(model.transform, false);
            enemy.InitializeFromMapServer(Object.FindFirstObjectByType<NetworkCampaignController>(), source);
            Action refresh = BindLateUpdate(enemy);
            var clonedModel = enemy.transform.Find(model.name + "(Clone)");
            Assert.That(clonedModel, Is.Not.Null);
            var mesh = clonedModel.GetComponentInChildren<MeshRenderer>();
            var collider = clonedModel.GetComponentInChildren<Collider>();

            SetState(enemy, "deploymentAt", _manager.ServerTime.Time + 10d);
            refresh();
            Assert.That(mesh.enabled, Is.False);
            Assert.That(collider.enabled, Is.False);
            SetState(enemy, "deploymentAt", _manager.ServerTime.Time - 1d);
            refresh();
            Assert.That(mesh.enabled, Is.True);
            Assert.That(collider.enabled, Is.True);

            // Preserve active-hierarchy semantics when a cached visual is hidden externally.
            clonedModel.gameObject.SetActive(false);
            mesh.enabled = false;
            collider.enabled = false;
            refresh();
            Assert.That(mesh.enabled, Is.False);
            Assert.That(collider.enabled, Is.False);
            clonedModel.gameObject.SetActive(true);
            refresh();
            Assert.That(mesh.enabled, Is.True);
            Assert.That(collider.enabled, Is.True);
        }

        private static Action BindLateUpdate(NetworkCombatEnemy enemy) => (Action)Delegate.CreateDelegate(
            typeof(Action), enemy, typeof(NetworkCombatEnemy).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic));

        private static void SetState<T>(NetworkCombatEnemy enemy, string field, T value)
        {
            var state = (NetworkVariable<T>)typeof(NetworkCombatEnemy)
                .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(enemy);
            state.Value = value;
        }

        private static void AssertTint(NetworkCombatEnemy enemy, Color expected)
        {
            var properties = new MaterialPropertyBlock();
            enemy.GetComponent<Renderer>().GetPropertyBlock(properties);
            Color actual = properties.GetColor("_BaseColor");
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.0001f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.0001f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.0001f));
        }
    }
}
