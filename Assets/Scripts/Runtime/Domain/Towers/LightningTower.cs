using System.Collections;
using UnityEngine;

public sealed class LightningTower : MonoBehaviour
{
    [SerializeField] private TowerTargeting targeting;
    [SerializeField] private LineRenderer beam;
    [SerializeField] private int damage = 50;
    public void TryFire()
    {
        var target = targeting == null ? null : targeting.CurrentTarget;
        if (target == null || !target.TryGetComponent(out Health health) || health.IsDead) return;
        health.TakeDamage(damage);
        if (beam != null) StartCoroutine(ShowBeam(target.transform.position));
    }
    private IEnumerator ShowBeam(Vector3 end) { beam.enabled = true; beam.SetPosition(0, transform.position); beam.SetPosition(1, end); yield return new WaitForSeconds(.12f); beam.enabled = false; }
}
