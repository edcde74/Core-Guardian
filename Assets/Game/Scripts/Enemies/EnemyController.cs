using System;
using UnityEngine;

[RequireComponent(typeof(Health))]
public sealed class EnemyController : MonoBehaviour
{
    public event Action<EnemyController> Exited;

    public float Progress { get; private set; }

    private EnemyDefinition definition;
    private WaypointPath path;
    private Health health;
    private int waypointIndex;
    private bool initialized;
    private bool hasExited;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    public void Initialize(EnemyDefinition enemyDefinition, WaypointPath waypointPath)
    {
        definition = enemyDefinition;
        path = waypointPath;
        waypointIndex = 0;
        Progress = 0f;
        hasExited = false;

        if (definition == null || path == null || path.Count == 0)
        {
            Debug.LogWarning("Enemy requires a definition and at least one waypoint.", this);
            initialized = false;
            return;
        }

        health.Configure(definition.MaxHealth);
        transform.localScale = definition.SilhouetteScale;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized || hasExited || health.IsDead)
        {
            return;
        }

        var target = path.GetPoint(waypointIndex);
        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            definition.MoveSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, target.position) > 0.001f)
        {
            return;
        }

        waypointIndex++;
        Progress = Mathf.Clamp01((float)waypointIndex / path.Count);
        if (waypointIndex < path.Count)
        {
            return;
        }

        hasExited = true;
        Exited?.Invoke(this);
        Destroy(gameObject);
    }
}
