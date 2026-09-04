using System;
using System.Collections.Generic;
using System.Linq;
using FunGame.Combat;
using UnityEngine;

namespace FunGame.Editor
{
    /// <summary>在同一张地图上为两种模式生成可见、可接近的交战位置。</summary>
    public static class CombatLayoutAuthoring
    {
        public const float EquipmentClearance = 1.25f;

        public static void Configure(CombatEncounterController encounter, Vector3 roomCenter)
        {
            Physics.SyncTransforms();
            Collider[] obstacles = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(c => !c.isTrigger && c.GetComponentInParent<InterferenceEnemy>() == null &&
                            c.GetComponentInParent<FunGame.Player.FirstPersonController>() == null)
                .ToArray();
            var occupied = new List<(Vector3 point, float radius)>();
            foreach (InterferenceEnemy enemy in encounter.Enemies.OrderByDescending(e => e.AuthoredScale.x))
            {
                float radius = Mathf.Max(enemy.AuthoredScale.x, enemy.AuthoredScale.z) * 0.65f;
                Vector3 target = encounter.DefenseTarget.transform.position;
                Vector3 preferred = target + new Vector3(target.x > roomCenter.x ? -2.5f : 2.5f, 0f, -3.5f);
                var candidates = new List<Vector3>();
                for (float x = -3.2f; x <= 3.21f; x += 0.8f)
                for (float z = -4f; z <= 4.01f; z += 0.8f)
                    candidates.Add(roomCenter + new Vector3(x, enemy.transform.position.y - roomCenter.y, z));
                Vector3 attack = candidates.OrderBy(p => (p - preferred).sqrMagnitude).FirstOrDefault(p =>
                    IsClear(p, radius + EquipmentClearance, obstacles) &&
                    occupied.All(o => HorizontalDistance(p, o.point) >= radius + o.radius + 0.35f));
                if (attack == Vector3.zero)
                    throw new InvalidOperationException($"No visible combat position for {enemy.TargetId}");
                Vector3 spawn = attack + Vector3.back * 2.4f;
                if (spawn.z < roomCenter.z - 5f || !IsClear(spawn, radius + EquipmentClearance, obstacles) ||
                    occupied.Any(o => HorizontalDistance(spawn, o.point) < radius + o.radius + 0.35f) ||
                    !IsClear(Vector3.Lerp(spawn, attack, 0.5f), radius + EquipmentClearance, obstacles))
                    spawn = attack;
                occupied.Add((attack, radius));
                if (spawn != attack) occupied.Add((spawn, radius));
                Vector3 approach = attack;
                if (enemy.Behavior == InterferenceEnemyBehavior.FlankingAttach && spawn != attack)
                {
                    Vector3 flank = Vector3.Lerp(spawn, attack, 0.5f) + Vector3.right * (attack.x < roomCenter.x ? -0.8f : 0.8f);
                    if (IsClear(flank, radius + EquipmentClearance, obstacles)) approach = flank;
                }
                enemy.ConfigureCombatPosition(spawn, attack, approach);
            }
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = b.y;
            return Vector3.Distance(a, b);
        }

        private static bool IsClear(Vector3 point, float radius, IEnumerable<Collider> obstacles)
        {
            foreach (Collider obstacle in obstacles)
            {
                // bounds of disabled colliders are empty; map obstacles remain enabled in the source scene.
                Bounds bounds = obstacle.bounds;
                if (bounds.size == Vector3.zero || bounds.max.y < 0.2f || bounds.min.y > 2.5f) continue;
                Vector3 nearest = bounds.ClosestPoint(point);
                if (HorizontalDistance(point, nearest) < radius) return false;
            }
            return true;
        }
    }
}
