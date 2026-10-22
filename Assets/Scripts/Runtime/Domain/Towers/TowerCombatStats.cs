public sealed class TowerCombatStats
{
    private float baseRange;
    private float baseAttackInterval;
    private bool bannerActive;

    public float Range => baseRange * (bannerActive ? 1.2f : 1f);
    public float AttackInterval => baseAttackInterval / (bannerActive ? 1.25f : 1f);

    public void Configure(float range, float attackInterval)
    {
        baseRange = range;
        baseAttackInterval = attackInterval;
        bannerActive = false;
    }

    public void SetBannerActive(bool active)
    {
        bannerActive = active;
    }
}
