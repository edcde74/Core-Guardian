using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public sealed class TowerTargeting : MonoBehaviour
{
    private readonly HashSet<EnemyController> candidates = new();
    public EnemyController CurrentTarget { get; private set; }
    public void SetRange(float range) => GetComponent<CircleCollider2D>().radius = range;
    private void OnTriggerEnter2D(Collider2D other) { if (other.TryGetComponent(out EnemyController enemy)) candidates.Add(enemy); }
    private void OnTriggerExit2D(Collider2D other) { if (other.TryGetComponent(out EnemyController enemy)) candidates.Remove(enemy); }
    private void Update()
    {
        CurrentTarget = null;
        foreach (var enemy in candidates)
            if (enemy != null && !enemy.GetComponent<Health>().IsDead && (CurrentTarget == null || enemy.Progress > CurrentTarget.Progress)) CurrentTarget = enemy;
    }
}
