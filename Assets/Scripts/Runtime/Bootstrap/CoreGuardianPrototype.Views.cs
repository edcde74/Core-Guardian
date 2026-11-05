using System;
using System.Collections.Generic;
using System.Linq;
using CoreGuardian;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

// CoreGuardianPrototype: session → scene view sync and pooled enemy/projectile views.
public sealed partial class CoreGuardianPrototype
{
    sealed class EnemyBodyView
    {
        public EnemyKind Kind;
        public GameObject Source,
            Root;
        public CoreGuardianUnitAnimation Animation;
        public CoreGuardianPhotoShadow Shadow;
        public SegmentedEnemyHealthBar Health;
        public Vector3 InitialScale;
        public Quaternion InitialRotation;

        // 10-30 겹친 적 그리기(#8): 지금 위로 올린 높이(칸 단위). 전투 위치는 그대로.
        public float Lift;
        public bool Valid => Root != null && Animation != null && Health != null && Shadow != null;
    }

    public Transform HealthView(int id) => healthViews.TryGetValue(id, out var view) ? view : null;

    public Transform TowerView(int id) =>
        towerViews.TryGetValue(id, out var view) && view != null ? view.transform : null;

    public Transform EnemyView(int id) =>
        enemyViews.TryGetValue(id, out var view) && view != null ? view.transform : null;

    public void GetCombatBodyTargets(List<Transform> targets)
    {
        if (player != null && player.gameObject.activeInHierarchy)
        {
            targets.Add(player.transform);
        }

        foreach (var view in towerViews.Values)
        {
            if (view != null && view.activeInHierarchy)
            {
                targets.Add(view.transform);
            }
        }

        foreach (var view in enemyViews.Values)
        {
            if (view != null && view.activeInHierarchy)
            {
                targets.Add(view.transform);
            }
        }
    }

    // 10-30 사용자 결정(#8): 거의 같은 자리에서 겹쳐 걷는 적은 뒤쪽을 살짝(약 3픽셀) 위로 올려 두 줄로 걷는 것처럼 그린다.
    // 그림 위치만 바뀌고 전투 위치·판정은 그대로다. 보스·비행체는 빼고, 이미 올라간 적 뒤에 붙은 적은 다시 아래 줄로.
    const float OverlapDistance = .35f,
        OverlapLift = .09f;
    readonly List<EnemyHandle> overlapOrder = new List<EnemyHandle>();
    readonly HashSet<int> liftedEnemyIds = new HashSet<int>();
    static readonly Comparison<EnemyHandle> AheadFirst = (a, b) => b.PathTravelled.CompareTo(a.PathTravelled);

    void MarkOverlaps()
    {
        overlapOrder.Clear();
        liftedEnemyIds.Clear();
        foreach (var e in livingViewEnemies)
        {
            if (e.Kind != EnemyKind.Flyer && e.Kind != EnemyKind.Boss)
            {
                overlapOrder.Add(e);
            }
        }
        overlapOrder.Sort(AheadFirst);
        for (int i = 1; i < overlapOrder.Count; i++)
        {
            var e = overlapOrder[i];
            for (int j = i - 1; j >= 0; j--)
            {
                var front = overlapOrder[j];
                if (front.PathTravelled - e.PathTravelled > OverlapDistance)
                    break;
                if (liftedEnemyIds.Contains(front.Id))
                    continue;
                if ((front.Position - e.Position).sqrMagnitude < OverlapDistance * OverlapDistance)
                {
                    liftedEnemyIds.Add(e.Id);
                    break;
                }
            }
        }
    }

    void SynchronizeViews()
    {
        livingViewEnemies.Clear();
        activeEnemyViewIds.Clear();
        foreach (var enemy in Session.Enemies)
        {
            if (enemy.Alive)
            {
                livingViewEnemies.Add(enemy);
                activeEnemyViewIds.Add(enemy.Id);
            }
        }

        ReturnRemovedEnemyBodies();
        SyncRemoved(auraViews, activeEnemyViewIds);
        MarkOverlaps();
        foreach (var e in livingViewEnemies)
        {
            if (!enemyBodies.TryGetValue(e.Id, out var body))
            {
                body = RentEnemyBody(e);
                enemyBodies.Add(e.Id, body);
                enemyViews.Add(e.Id, body.Root);
                healthViews.Add(e.Id, body.Health.transform);
                if (e.Kind == EnemyKind.Boss)
                {
                    GetComponent<CoreGuardianCombatAudio>().OrNull()?.NotifyBossSpawn(e.Id);
                }
            }
            var view = body.Root;
            float lift = liftedEnemyIds.Contains(e.Id) ? OverlapLift : 0;
            body.Lift = Mathf.MoveTowards(body.Lift, lift, Time.deltaTime * .6f);
            view.transform.position = World(e.Position + Vector2.up * body.Lift);
            combatAnimation.Enemy(e, view);
            body.Health.transform.position = body.Animation.HealthBarWorld;
            body.Health.SetHealth(e.Health, e.Spec.Health);
            if (e.Buffed && !auraViews.ContainsKey(e.Id))
            {
                auraViews[e.Id] = combatArt.Marker("EnemySpeedBuff", World(e.Position), .17f);
            }

            if (auraViews.TryGetValue(e.Id, out var aura))
            {
                aura.SetActive(e.Buffed);
                aura.transform.position = World(e.Position) + Vector3.up * 1.02f;
            }
        }
        activeTowerViewIds.Clear();
        foreach (var tower in Session.Towers)
        {
            activeTowerViewIds.Add(tower.Id);
        }

        SyncRemoved(towerViews, activeTowerViewIds);
        SyncRemoved(towerAuraViews, activeTowerViewIds);
        Session.BeginTowerViews();
        try
        {
            foreach (var t in Session.Towers)
            {
                if (!towerViews.TryGetValue(t.Id, out var view))
                {
                    view = Instantiate(TowerArtFor(t.Kind));
                    view.name = "Tower_" + t.Kind + "_" + t.Id;
                    towerViews.Add(t.Id, view);
                }
                view.transform.position = World(t.Position);
                combatAnimation.Tower(t, view);
                bool supported =
                    t.Kind != TowerKind.Support
                    && (Session.Augments.TowerBuffPercent > 0 || Session.HasTowerSupport(t));
                if (supported && !towerAuraViews.ContainsKey(t.Id))
                {
                    towerAuraViews[t.Id] = combatArt.Marker("TowerDamageBuff", World(t.Position), .2f);
                }

                if (towerAuraViews.TryGetValue(t.Id, out var aura))
                {
                    aura.SetActive(supported);
                    aura.transform.position = World(t.Position) + Vector3.up * .88f;
                }
            }
        }
        finally
        {
            Session.EndTowerViews();
        }
        activeProjectileViewIds.Clear();
        foreach (var projectile in Session.Projectiles)
        {
            activeProjectileViewIds.Add(projectile.Id);
        }

        SyncRemoved(projectileViews, activeProjectileViewIds, true);
        foreach (var p in Session.Projectiles)
        {
            if (!projectileViews.TryGetValue(p.Id, out var view))
            {
                view = RentProjectileView(p);
                projectileViews.Add(p.Id, view);
            }
            view.transform.position = combatAnimation.ProjectilePosition(p);
            var direction = World(p.TargetPosition) + Vector3.up * .25f - view.transform.position;
            if (direction.sqrMagnitude > .000001f)
            {
                view.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            }
        }
        combatAnimation.Player();
    }

    void ReturnRemovedEnemyBodies()
    {
        removedViewIds.Clear();
        foreach (var entry in enemyViews)
        {
            if (
                !activeEnemyViewIds.Contains(entry.Key)
                || entry.Value == null
                || !enemyBodies.TryGetValue(entry.Key, out var body)
                || !body.Valid
            )
            {
                removedViewIds.Add(entry.Key);
            }
        }

        foreach (int id in removedViewIds)
        {
            combatAnimation.ReleaseEnemy(id);
            enemyBodies.TryGetValue(id, out var body);
            var root = enemyViews[id];
            if (root != null)
            {
                // These hosts live outside the enemy hierarchy. Detach them before same-frame reuse.
                GetComponent<CoreGuardianObjectEmphasis>().OrNull()?.ReleaseTarget(root.transform);
                upgradeFeedback.OrNull()?.ReleaseTarget(root.transform);
                GetComponent<CoreGuardianRageOutline>().OrNull()?.ReleaseTarget(root.transform);
                root.SetActive(false); // Restores hit materials and releases the contact-mask binding.
                if (body != null && body.Valid)
                {
                    body.Animation.ResetEnemyForReuse();
                    if (!enemyBodyPools.TryGetValue(body.Kind, out var pool))
                    {
                        pool = new Stack<EnemyBodyView>();
                        enemyBodyPools.Add(body.Kind, pool);
                    }
                    // Same size as the model pool of that kind (32, or more where a round has more; F18).
                    int capacity = Session.EnemyPoolLimit(body.Kind);
                    if (pool.Count < capacity)
                    {
                        if (enemyPoolRoot == null)
                        {
                            var host = new GameObject("CoreGuardianEnemyBodyPool");
                            host.transform.SetParent(transform, false);
                            host.SetActive(false);
                            enemyPoolRoot = host.transform;
                        }
                        root.transform.SetParent(enemyPoolRoot, false);
                        pool.Push(body);
                    }
                    else
                    {
                        Destroy(root);
                    }
                }
                else
                {
                    Destroy(root);
                }
            }
            enemyViews.Remove(id);
            healthViews.Remove(id);
            enemyBodies.Remove(id);
        }
        removedViewIds.Clear();
    }

    EnemyBodyView RentEnemyBody(EnemyHandle enemy)
    {
        var source = enemy.Kind == EnemyKind.Boss ? BossArtFor(enemy.Spec) : EnemyArtFor(enemy.Kind);
        EnemyBodyView body = null;
        if (enemyBodyPools.TryGetValue(enemy.Kind, out var pool))
        {
            while (pool.Count > 0 && body == null)
            {
                var candidate = pool.Pop();
                if (candidate.Valid && candidate.Source == source)
                {
                    body = candidate;
                }
                else if (candidate.Root != null)
                {
                    Destroy(candidate.Root);
                }
            }
        }

        if (body == null)
        {
            var root = Instantiate(source);
            root.name = "Enemy_" + enemy.Kind + "_" + enemy.Id;
            root.transform.position = World(enemy.Position);
            if (enemy.Kind == EnemyKind.Flyer)
            {
                foreach (var sorting in root.GetComponentsInChildren<ArtDepthSorting>(true))
                {
                    sorting.Airborne = true;
                }
            }
            var bar = CreateSprite(
                "Health",
                square,
                Vector3.zero,
                new Color(.35f, 1f, .45f),
                ArtDepthSorting.OverlayBase + 1900
            );
            bar.transform.SetParent(root.transform, false);
            var health = bar.AddComponent<SegmentedEnemyHealthBar>();
            health.Configure(enemy.Kind == EnemyKind.Boss);
            var animation = CoreGuardianUnitAnimation.Attach(root, EnemyArtRole(enemy));
            body = new EnemyBodyView
            {
                Kind = enemy.Kind,
                Source = source,
                Root = root,
                Animation = animation,
                Shadow = root.GetComponent<CoreGuardianPhotoShadow>(),
                Health = health,
                InitialScale = root.transform.localScale,
                InitialRotation = root.transform.rotation,
            };
        }
        else
        {
            body.Root.transform.SetParent(null, false);
            body.Root.transform.localScale = body.InitialScale;
            body.Root.transform.rotation = body.InitialRotation;
            body.Root.transform.position = World(enemy.Position);
            body.Animation.ResetEnemyForReuse();
            body.Shadow.InvalidateSources();
            body.Health.Configure(enemy.Kind == EnemyKind.Boss);
        }
        body.Lift = 0;
        body.Root.name = "Enemy_" + enemy.Kind + "_" + enemy.Id;
        body.Health.SetHealth(enemy.Health, enemy.Spec.Health);
        body.Health.transform.position = body.Animation.HealthBarWorld;
        body.Root.SetActive(true);
        return body;
    }

    void SyncRemoved(Dictionary<int, GameObject> views, HashSet<int> active, bool poolProjectiles = false)
    {
        removedViewIds.Clear();
        foreach (var id in views.Keys)
        {
            if (!active.Contains(id))
            {
                removedViewIds.Add(id);
            }
        }

        foreach (var id in removedViewIds)
        {
            var view = views[id];
            if (view != null)
            {
                var renderer = poolProjectiles ? view.GetComponent<SpriteRenderer>() : null;
                if (renderer != null && projectileViewPool.Count < MaxRetainedProjectileViews)
                {
                    view.SetActive(false);
                    projectileViewPool.Push(renderer);
                }
                else
                {
                    Destroy(view);
                }
            }
            views.Remove(id);
        }
        removedViewIds.Clear();
    }

    GameObject RentProjectileView(ProjectileHandle projectile)
    {
        SpriteRenderer renderer = null;
        while (projectileViewPool.Count > 0 && renderer == null)
        {
            renderer = projectileViewPool.Pop();
        }

        if (renderer == null)
        {
            renderer = CreateSprite(
                    "Projectile_" + projectile.Id,
                    combatArt.Projectile(projectile),
                    World(projectile.Position),
                    Color.white,
                    ArtDepthSorting.OverlayBase + 1500
                )
                .GetComponent<SpriteRenderer>();
        }
        else
        {
            // A reused shot keeps its first name; renaming every shot only made garbage (F8).
            renderer.sprite = combatArt.Projectile(projectile);
        }
        renderer.color = Color.white;
        renderer.flipX = renderer.flipY = false;
        renderer.sortingOrder = ArtDepthSorting.OverlayBase + 1500;
        renderer.transform.position = World(projectile.Position);
        renderer.transform.rotation = Quaternion.identity;
        CoreGuardianCombatArt.SetWidth(renderer, projectile.Source == "Player" ? .2f : .275f);
        renderer.transform.SetAsLastSibling();
        renderer.gameObject.SetActive(true);
        return renderer.gameObject;
    }
}
