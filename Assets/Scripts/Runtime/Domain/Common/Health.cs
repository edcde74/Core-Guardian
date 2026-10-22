using System;
using UnityEngine;

/// <summary>
/// Owns a unit's current health and emits one death event when it reaches zero.
/// </summary>
public sealed class Health : MonoBehaviour
{
    public event Action Died;

    public int Current => current;
    public bool IsDead { get; private set; }

    private int current;

    public void Configure(int maximum)
    {
        current = Mathf.Max(0, maximum);
        IsDead = current == 0;
    }

    public void TakeDamage(int damage)
    {
        if (IsDead || damage <= 0)
        {
            return;
        }

        current = Mathf.Max(0, current - damage);
        if (current != 0)
        {
            return;
        }

        IsDead = true;
        Died?.Invoke();
    }
}
