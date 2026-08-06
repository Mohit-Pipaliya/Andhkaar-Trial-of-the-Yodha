using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks live enemies without expensive FindObjectsByType scans.
/// </summary>
public static class EnemyRegistry
{
    private static readonly List<Transform> activeEnemies = new List<Transform>(16);

    public static void Register(Transform enemy)
    {
        if (enemy == null) return;
        if (!activeEnemies.Contains(enemy))
            activeEnemies.Add(enemy);
    }

    public static void Unregister(Transform enemy)
    {
        if (enemy == null) return;
        activeEnemies.Remove(enemy);
    }

    public static bool IsAnyAliveNear(Vector3 position, float radius, Transform exclude = null)
    {
        float radiusSqr = radius * radius;

        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            Transform enemy = activeEnemies[i];
            if (enemy == null)
            {
                activeEnemies.RemoveAt(i);
                continue;
            }

            if (exclude != null && enemy == exclude) continue;
            if ((enemy.position - position).sqrMagnitude <= radiusSqr)
                return true;
        }

        return false;
    }
}
