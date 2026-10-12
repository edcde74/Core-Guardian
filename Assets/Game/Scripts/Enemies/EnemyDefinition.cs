using UnityEngine;

[CreateAssetMenu(menuName = "Fortress/Enemy Definition", fileName = "EnemyDefinition")]
public sealed class EnemyDefinition : ScriptableObject
{
    [SerializeField] private int maxHealth = 20;
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private int reward = 1;
    [SerializeField] private Color color = Color.white;
    [SerializeField] private Vector2 silhouetteScale = Vector2.one;

    public int MaxHealth => maxHealth;
    public float MoveSpeed => moveSpeed;
    public int Reward => reward;
    public Color Color => color;
    public Vector2 SilhouetteScale => silhouetteScale;

    public void Configure(int health, float speed, int enemyReward, Color enemyColor, Vector2 scale)
    {
        maxHealth = Mathf.Max(1, health);
        moveSpeed = Mathf.Max(0f, speed);
        reward = Mathf.Max(0, enemyReward);
        color = enemyColor;
        silhouetteScale = scale;
    }
}
