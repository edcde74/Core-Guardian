using UnityEngine;

public sealed class BeanProjectile : MonoBehaviour
{
    private Health target;
    private int damage;
    private float speed;
    private bool launched;

    public void Launch(Health targetHealth, int projectileDamage, float projectileSpeed)
    {
        target = targetHealth;
        damage = projectileDamage;
        speed = projectileSpeed;
        launched = true;
    }

    private void Update()
    {
        if (!launched)
        {
            return;
        }

        if (target == null || target.IsDead)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            target.transform.position,
            speed * Time.deltaTime);

        if (Vector2.Distance(transform.position, target.transform.position) > 0.05f)
        {
            return;
        }

        target.TakeDamage(damage);
        Destroy(gameObject);
    }
}
