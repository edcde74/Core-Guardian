# 생물형 마수와 초기 타워 전투 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a playable first combat loop in which four biological mountain beasts follow a route and the player builds three functional tower types on allowed Tilemap cells.

**Architecture:** Keep route authoring and buildable-cell authoring separate: ordered `WaypointPath` transforms move enemies, while a non-rendered `BuildableCells` Tilemap is the sole authority for buildability. ScriptableObject definitions hold unit and tower data; focused MonoBehaviours own movement, targeting, attack, buff, and placement responsibilities.

**Tech Stack:** Unity 6.0.6f1, C#, URP 2D, Unity Tilemap, Input System, Unity Test Framework (NUnit EditMode and PlayMode).

**Spec:** `docs/superpowers/specs/2026-09-22-biological-beasts-and-towers-design.md`

## Global Constraints

- Preserve `Grid` with 1x1 cells and retain the existing visual terrain assets.
- Initial enemies must be entirely biological: no mana, mechanical, armor, modification, or cybernetic visual element.
- Only `BuildableCells` determines valid construction; `PathCells` must never become buildable.
- One cell contains at most one tower and a pointer above UI does not place one.
- The initial towers are Bean Pod, Lightning Spire, and Command Banner only.
- Command Banner affects attack towers only; its range and attack-speed bonuses are +20% and +25%, respectively, and its own type does not stack.
- Every new runtime behavior starts with a failing automated test.
- This project is not a Git repository; do not include commit commands.

## Review Focus

- A cell painted in both BuildableCells and PathCells must reject placement; `PathCells` wins even if authoring accidentally overlaps.
- A target destroyed during a bean projectile's flight must not create a null-reference exception or apply damage to a replacement target.
- Leaving a Command Banner range, disabling the banner, and re-entering must always restore and then reapply the exact base-derived values.
- An enemy reaching the final waypoint must emit its exit event once, then stop updating and be removed.
- Pointer input over UI must not reserve a cell, spawn a preview, or instantiate a tower.

---

## File Structure

```text
Assets/Game/
|- Scripts/
|  |- Common/          Health.cs
|  |- Enemies/         EnemyDefinition.cs, EnemyController.cs, WaypointPath.cs
|  |- Towers/          TowerCombatStats.cs, TowerTargeting.cs, BeanTower.cs,
|  |                   BeanProjectile.cs, LightningTower.cs, BannerTower.cs,
|  |                   TowerBuffReceiver.cs
|  `- Placement/       TowerDefinition.cs, GridPlacementValidator.cs,
|                      TowerPlacementController.cs, PlacementPreview.cs
|- Definitions/Enemies/ and Definitions/Towers/
|- Prefabs/Enemies/ and Prefabs/Towers/
`- Tests/
   |- EditMode/        PlacementAndBuffTests.cs
   `- PlayMode/        CombatAndMovementTests.cs
```

`Health` is the only damage receiver. `EnemyController` exposes route progress to `TowerTargeting`; attacks never select directly from the scene. `TowerCombatStats` owns base/effective values and `TowerBuffReceiver` applies one named source at a time. `GridPlacementValidator` stays independent from input and instantiation so EditMode tests can cover all cell rules without a scene.

### Task 1: Create health and biological enemy data

**Files:**
- Create: `Assets/Game/Scripts/Common/Health.cs`
- Create: `Assets/Game/Scripts/Enemies/EnemyDefinition.cs`
- Create: `Assets/Game/Scripts/Enemies/EnemyController.cs`
- Create: `Assets/Game/Tests/PlayMode/CombatAndMovementTests.cs`

**Interfaces:**
- Produces: `Health.TakeDamage(int damage)`, `Health.Current`, `Health.IsDead`, `EnemyController.Progress`, and `EnemyController.Initialize(EnemyDefinition definition, WaypointPath path)`.
- Consumes later: all attack scripts use `Health`; `TowerTargeting` uses `EnemyController.Progress`.

- [ ] **Step 1: Write the failing death test**

```csharp
[UnityTest]
public IEnumerator Health_DiesOnce_WhenDamageReachesMaximum()
{
    var subject = new GameObject().AddComponent<Health>();
    subject.Configure(20);
    var deathCount = 0;
    subject.Died += () => deathCount++;
    subject.TakeDamage(20);
    subject.TakeDamage(20);
    Assert.That(subject.IsDead, Is.True);
    Assert.That(deathCount, Is.EqualTo(1));
    yield return null;
}
```

- [ ] **Step 2: Run the PlayMode test and verify it fails because `Health` is absent**

Run in Unity Test Runner: `Window > General > Test Runner > PlayMode > CombatAndMovementTests > Run Selected`.

Expected: compilation failure naming missing type `Health`.

- [ ] **Step 3: Implement the minimum Health API and data definition**

```csharp
public sealed class Health : MonoBehaviour
{
    public event Action Died;
    public int Current => current;
    public bool IsDead { get; private set; }
    private int current;
    public void Configure(int maximum) { current = maximum; IsDead = false; }
    public void TakeDamage(int damage)
    {
        if (IsDead || damage <= 0) return;
        current = Mathf.Max(0, current - damage);
        if (current == 0) { IsDead = true; Died?.Invoke(); }
    }
}

private static Health CreateHealth(Vector2 position = default, int maximum = 100)
{
    var gameObject = new GameObject("Health Test Target");
    gameObject.transform.position = position;
    var health = gameObject.AddComponent<Health>();
    health.Configure(maximum);
    return health;
}

[CreateAssetMenu(menuName = "Fortress/Enemy Definition")]
public sealed class EnemyDefinition : ScriptableObject
{
    public int maxHealth; public float moveSpeed; public int reward;
    public Color color = Color.white; public Vector2 silhouetteScale = Vector2.one;
}
```

- [ ] **Step 4: Add `EnemyController` initialization**

```csharp
public float Progress { get; private set; }
public void Initialize(EnemyDefinition definition, WaypointPath path)
{
    this.definition = definition; this.path = path;
    health.Configure(definition.maxHealth);
    spriteRenderer.color = definition.color;
    transform.localScale = definition.silhouetteScale;
}
```

- [ ] **Step 5: Re-run the selected PlayMode test**

Expected: PASS with one death event.

### Task 2: Implement ordered waypoint movement and exit behavior

**Files:**
- Create: `Assets/Game/Scripts/Enemies/WaypointPath.cs`
- Modify: `Assets/Game/Scripts/Enemies/EnemyController.cs`
- Modify: `Assets/Game/Tests/PlayMode/CombatAndMovementTests.cs`

**Interfaces:**
- Consumes: `EnemyController.Initialize(EnemyDefinition, WaypointPath)`.
- Produces: `WaypointPath.Points`, `EnemyController.Exited`, and monotonic `Progress` in the range 0..1.

- [ ] **Step 1: Write the failing waypoint test**

```csharp
[UnityTest]
public IEnumerator Enemy_VisitsWaypointsInOrder_ThenExitsOnce()
{
    var path = CreatePath(Vector2.zero, Vector2.right, Vector2.right * 2f);
    var enemy = CreateEnemy(speed: 10f, path);
    var exits = 0; enemy.Exited += () => exits++;
    yield return new WaitForSeconds(0.5f);
    Assert.That(exits, Is.EqualTo(1));
}
```

- [ ] **Step 2: Run the test and verify it fails because movement and `Exited` do not exist**

Expected: compile failure mentioning `Exited`, then a behavioral assertion failure after the event is introduced.

- [ ] **Step 3: Implement the path API and minimal movement loop**

```csharp
public sealed class WaypointPath : MonoBehaviour
{
    [SerializeField] private Transform[] points;
    public IReadOnlyList<Transform> Points => points;
}

private void Update()
{
    if (hasExited || path == null || path.Points.Count == 0) return;
    var target = path.Points[index].position;
    transform.position = Vector3.MoveTowards(transform.position, target, definition.moveSpeed * Time.deltaTime);
    if (Vector2.Distance(transform.position, target) > 0.001f) return;
    index++;
    Progress = Mathf.Clamp01((float)index / path.Points.Count);
    if (index == path.Points.Count) { hasExited = true; Exited?.Invoke(); Destroy(gameObject); }
}
```

- [ ] **Step 4: Add the empty-path warning guard**

```csharp
if (path == null || path.Points.Count == 0)
{
    Debug.LogWarning("Enemy has no waypoint path.", this);
    enabled = false;
    return;
}
```

- [ ] **Step 5: Re-run all PlayMode tests**

Expected: every PlayMode test passes; the exit count is exactly one.

### Task 3: Create tower stats, target selection, and non-stacking buff calculation

**Files:**
- Create: `Assets/Game/Scripts/Towers/TowerCombatStats.cs`
- Create: `Assets/Game/Scripts/Towers/TowerBuffReceiver.cs`
- Create: `Assets/Game/Scripts/Towers/TowerTargeting.cs`
- Create: `Assets/Game/Tests/EditMode/PlacementAndBuffTests.cs`

**Interfaces:**
- Produces: `TowerCombatStats.Range`, `TowerCombatStats.AttackInterval`, `TowerBuffReceiver.SetBannerSource(string, bool)`, and `TowerTargeting.CurrentTarget`.
- Consumes later: BeanTower and LightningTower use the effective stat properties and current target.

- [ ] **Step 1: Write the failing non-stacking and restoration tests**

```csharp
[Test]
public void BannerBuff_ChangesBaseStatsOnce_AndRestoresThem()
{
    var stats = new TowerCombatStats();
    stats.Configure(3f, 0.8f);
    stats.SetBannerActive(true);
    stats.SetBannerActive(true);
    Assert.That(stats.Range, Is.EqualTo(3.6f).Within(0.001f));
    Assert.That(stats.AttackInterval, Is.EqualTo(0.64f).Within(0.001f));
    stats.SetBannerActive(false);
    Assert.That(stats.Range, Is.EqualTo(3f));
    Assert.That(stats.AttackInterval, Is.EqualTo(0.8f));
}
```

- [ ] **Step 2: Run the EditMode test and verify it fails because stats are absent**

Run in Unity Test Runner: `EditMode > PlacementAndBuffTests > Run Selected`.

Expected: compilation failure naming missing `TowerCombatStats`.

- [ ] **Step 3: Implement base/effective stat calculation**

```csharp
public sealed class TowerCombatStats
{
    private float baseRange; private float baseAttackInterval; private bool bannerActive;
    public float Range => baseRange * (bannerActive ? 1.2f : 1f);
    public float AttackInterval => baseAttackInterval / (bannerActive ? 1.25f : 1f);
    public void Configure(float range, float interval) { baseRange = range; baseAttackInterval = interval; }
    public void SetBannerActive(bool active) => bannerActive = active;
}

public sealed class TowerBuffReceiver : MonoBehaviour
{
    public TowerCombatStats Stats { get; } = new();
    private readonly HashSet<string> bannerSources = new();
    public float Range => Stats.Range;
    public float AttackInterval => Stats.AttackInterval;
    public void Configure(float range, float interval) => Stats.Configure(range, interval);
    public void SetBannerSource(string sourceId, bool active)
    {
        if (active) bannerSources.Add(sourceId); else bannerSources.Remove(sourceId);
        Stats.SetBannerActive(bannerSources.Count > 0);
    }
}
```

- [ ] **Step 4: Implement highest-progress targeting**

```csharp
private void OnTriggerStay2D(Collider2D other)
{
    if (!other.TryGetComponent<EnemyController>(out var enemy) || enemy.GetComponent<Health>().IsDead) return;
    if (currentTarget == null || enemy.Progress > currentTarget.Progress) currentTarget = enemy;
}
private void OnTriggerExit2D(Collider2D other)
{
    if (other.TryGetComponent<EnemyController>(out var enemy) && enemy == currentTarget) currentTarget = null;
}
```

- [ ] **Step 5: Re-run all EditMode tests**

Expected: banner bonus is +20% range and 25% faster firing once only, then exact base values return.

### Task 4: Implement Bean Pod projectile combat

**Files:**
- Create: `Assets/Game/Scripts/Towers/BeanProjectile.cs`
- Create: `Assets/Game/Scripts/Towers/BeanTower.cs`
- Modify: `Assets/Game/Tests/PlayMode/CombatAndMovementTests.cs`

**Interfaces:**
- Consumes: `TowerTargeting.CurrentTarget`, `TowerCombatStats.AttackInterval`, `Health.TakeDamage(int)`.
- Produces: `BeanProjectile.Launch(Health target, int damage, float speed)`.

- [ ] **Step 1: Write the failing impact-once test**

```csharp
[UnityTest]
public IEnumerator BeanProjectile_HitsLiveTargetExactlyOnce()
{
    var health = CreateHealth(position: Vector2.right, maximum: 40);
    var projectile = CreateProjectile(Vector2.zero);
    projectile.Launch(health, 20, 10f);
    yield return new WaitForSeconds(0.25f);
    Assert.That(health.Current, Is.EqualTo(20));
}
```

- [ ] **Step 2: Run it and verify it fails because `Launch` is absent**

Expected: compilation failure naming `Launch`.

- [ ] **Step 3: Implement target-safe projectile travel**

```csharp
public void Launch(Health target, int damage, float speed)
{ this.target = target; this.damage = damage; this.speed = speed; }
private void Update()
{
    if (target == null || target.IsDead) { Destroy(gameObject); return; }
    transform.position = Vector3.MoveTowards(transform.position, target.transform.position, speed * Time.deltaTime);
    if (Vector2.Distance(transform.position, target.transform.position) > 0.05f) return;
    target.TakeDamage(damage); Destroy(gameObject);
}
```

- [ ] **Step 4: Implement BeanTower cooldown firing**

```csharp
if (cooldown > 0f) { cooldown -= Time.deltaTime; return; }
var target = targeting.CurrentTarget;
if (target == null) return;
Instantiate(projectilePrefab, firePoint.position, Quaternion.identity)
    .Launch(target.GetComponent<Health>(), damage, projectileSpeed);
cooldown = stats.AttackInterval;
```

- [ ] **Step 5: Re-run all PlayMode tests**

Expected: live target loses damage once; destroyed target produces no exception and no second damage.

### Task 5: Implement Lightning Spire hitscan combat

**Files:**
- Create: `Assets/Game/Scripts/Towers/LightningTower.cs`
- Modify: `Assets/Game/Tests/PlayMode/CombatAndMovementTests.cs`

**Interfaces:**
- Consumes: `TowerTargeting.CurrentTarget`, `TowerCombatStats.AttackInterval`, `Health.TakeDamage(int)`.
- Produces: `LightningTower.TryFire()`; optional `LineRenderer` only visualizes the already-applied hit.

- [ ] **Step 1: Write the failing instantaneous-damage test**

```csharp
[Test]
public void LightningSpire_AppliesDamageImmediatelyOnce()
{
    var health = CreateHealth(maximum: 100);
    var tower = CreateLightningTower(target: health);
    tower.TryFire();
    Assert.That(health.Current, Is.EqualTo(50));
}
```

- [ ] **Step 2: Run it and verify it fails because `TryFire` is absent**

Expected: compilation failure naming `TryFire`.

- [ ] **Step 3: Implement hitscan damage before visual effect**

```csharp
public void TryFire()
{
    var target = targeting.CurrentTarget;
    if (target == null) return;
    var health = target.GetComponent<Health>();
    if (health == null || health.IsDead) return;
    health.TakeDamage(damage);
    StartCoroutine(ShowBeam(target.transform.position));
}
private IEnumerator ShowBeam(Vector3 end)
{
    beam.enabled = true; beam.SetPosition(0, firePoint.position); beam.SetPosition(1, end);
    yield return new WaitForSeconds(0.12f); beam.enabled = false;
}
```

- [ ] **Step 4: Add the cooldown call from Update and test out-of-range behavior**

```csharp
[UnityTest]
public IEnumerator LightningSpire_DoesNotHitTargetOutsideTriggerRange()
{
    var health = CreateHealth(position: Vector2.right * 10f, maximum: 100);
    yield return null;
    Assert.That(health.Current, Is.EqualTo(100));
}
```

- [ ] **Step 5: Run all PlayMode tests**

Expected: target damage is immediate and exactly once per cooldown; out-of-range target remains unhurt.

### Task 6: Implement Command Banner trigger lifecycle

**Files:**
- Create: `Assets/Game/Scripts/Towers/BannerTower.cs`
- Modify: `Assets/Game/Scripts/Towers/TowerBuffReceiver.cs`
- Modify: `Assets/Game/Tests/EditMode/PlacementAndBuffTests.cs`

**Interfaces:**
- Consumes: attack tower `TowerCombatStats`.
- Produces: `TowerBuffReceiver.SetBannerSource(string sourceId, bool active)` and `BannerTower` cleanup in `OnDisable`.

- [ ] **Step 1: Write the failing disable-cleanup test**

```csharp
[Test]
public void DisablingBannerSource_RemovesItsExactBonus()
{
    var receiver = new GameObject("Tower").AddComponent<TowerBuffReceiver>();
    receiver.Configure(3f, 0.8f);
    var banner = new GameObject("Banner").AddComponent<BannerTower>();
    banner.Register(receiver);
    banner.Unregister(receiver);
    Assert.That(receiver.Range, Is.EqualTo(3f));
    Assert.That(receiver.AttackInterval, Is.EqualTo(0.8f));
}
```

- [ ] **Step 2: Run the test and verify it fails because `BannerTower` is absent**

Expected: compilation failure naming missing `BannerTower`.

- [ ] **Step 3: Implement banner registration using the Task 3 source set**

```csharp
public void Register(TowerBuffReceiver receiver)
{
    if (affected.Add(receiver)) receiver.SetBannerSource(sourceId, true);
}
public void Unregister(TowerBuffReceiver receiver)
{
    if (affected.Remove(receiver)) receiver.SetBannerSource(sourceId, false);
}
```

- [ ] **Step 4: Implement trigger filtering and disable cleanup**

```csharp
private void OnTriggerEnter2D(Collider2D other)
{
    if (!other.TryGetComponent<TowerBuffReceiver>(out var receiver) ||
        !other.TryGetComponent<TowerTargeting>(out _)) return;
    Register(receiver);
}
private void OnDisable()
{
    foreach (var receiver in affected) if (receiver != null) receiver.SetBannerSource(sourceId, false);
    affected.Clear();
}
```

- [ ] **Step 5: Re-run all EditMode tests**

Expected: overlapping banners remain one bonus and disable/exit restores base values.

### Task 7: Implement Tilemap placement validation and input controller

**Files:**
- Create: `Assets/Game/Scripts/Placement/GridPlacementValidator.cs`
- Create: `Assets/Game/Scripts/Placement/TowerDefinition.cs`
- Create: `Assets/Game/Scripts/Placement/TowerPlacementController.cs`
- Create: `Assets/Game/Scripts/Placement/PlacementPreview.cs`
- Modify: `Assets/Game/Tests/EditMode/PlacementAndBuffTests.cs`

**Interfaces:**
- Consumes: a Buildable Tilemap, a Path Tilemap, selected `TowerDefinition`, and `EventSystem.current.IsPointerOverGameObject()`.
- Produces: `GridPlacementValidator.CanPlace(Vector3Int cell)`, `TowerPlacementController.Select(TowerDefinition)`, and one instantiated prefab per valid cell.

- [ ] **Step 1: Write the failing placement-matrix test**

```csharp
[TestCase(true, false, false, true)]
[TestCase(false, false, false, false)]
[TestCase(true, true, false, false)]
[TestCase(true, false, true, false)]
public void CellValidity_RequiresBuildableAndRejectsPathOrOccupied(
    bool buildable, bool path, bool occupied, bool expected)
{
    var validator = new GridPlacementValidator(CreateTilemap(buildable), CreateTilemap(path));
    if (occupied) validator.Reserve(Vector3Int.zero);
    Assert.That(validator.CanPlace(Vector3Int.zero), Is.EqualTo(expected));
}
```

- [ ] **Step 2: Run the EditMode test and verify it fails because validator is absent**

Expected: compilation failure naming `GridPlacementValidator`.

- [ ] **Step 3: Implement the precedence rules and reservation**

```csharp
public bool CanPlace(Vector3Int cell) =>
    buildable.HasTile(cell) && !path.HasTile(cell) && !occupied.Contains(cell);
public bool Reserve(Vector3Int cell)
{
    if (!CanPlace(cell)) return false;
    occupied.Add(cell); return true;
}
```

- [ ] **Step 4: Implement input-to-cell conversion and UI guard**

```csharp
var pointer = Pointer.current;
if (selected == null || pointer == null || EventSystem.current.IsPointerOverGameObject(pointer.deviceId)) return;
var screenPosition = pointer.position.ReadValue();
var world = Camera.main.ScreenToWorldPoint(screenPosition);
var cell = buildable.WorldToCell(world);
preview.Show(buildable.GetCellCenterWorld(cell), validator.CanPlace(cell));
if (pointer.press.wasPressedThisFrame && validator.Reserve(cell))
    Instantiate(selected.prefab, buildable.GetCellCenterWorld(cell), Quaternion.identity, towerLayer);
```

- [ ] **Step 5: Re-run all EditMode tests**

Expected: overlap with PathCells rejects placement; an already reserved cell rejects the second placement.

### Task 8: Create assets, prefabs, scene layers, and integration test

**Files:**
- Create: `Assets/Game/Definitions/Enemies/MountainHound.asset`, `RockBoar.asset`, `CanyonRhino.asset`, `RockLizard.asset`
- Create: `Assets/Game/Definitions/Towers/BeanPodTower.asset`, `LightningSpire.asset`, `CommandBanner.asset`
- Create: `Assets/Game/Prefabs/Enemies/MountainHound.prefab`, `RockBoar.prefab`, `CanyonRhino.prefab`, `RockLizard.prefab`
- Create: `Assets/Game/Prefabs/Towers/BeanPodTower.prefab`, `LightningSpire.prefab`, `CommandBanner.prefab`
- Modify: `Assets/Scenes/SampleScene.unity`
- Modify: `Assets/Game/Tests/PlayMode/CombatAndMovementTests.cs`

**Interfaces:**
- Consumes: all runtime components from Tasks 1-7.
- Produces: a runnable SampleScene with `GroundVisual`, `PathCells`, `BuildableCells`, `TowerLayer`, and `WaypointPath`.

- [ ] **Step 1: Write the failing scene integration test**

```csharp
[UnityTest]
public IEnumerator BuiltTower_DamagesEnemyFollowingScenePath()
{
    var scene = SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
    yield return new WaitUntil(() => scene.isDone);
    Assert.That(Object.FindFirstObjectByType<TowerPlacementController>(), Is.Not.Null);
    Assert.That(Object.FindFirstObjectByType<WaypointPath>(), Is.Not.Null);
}
```

- [ ] **Step 2: Run it and verify it fails because the scene lacks the controller and path**

Expected: assertion failure for the missing component.

- [ ] **Step 3: Create the scene hierarchy in a running Unity Editor**

Before changing scene assets, run `unity status --format json`. If an Editor is connected, create and save the following in its active `SampleScene`; do not edit YAML directly.

```text
Grid
|- GroundVisual (rename existing Tilemap)
|- PathCells (copy/paint the visible enemy path)
`- BuildableCells (no renderer; paint plaza-only build cells)
TowerLayer
WaypointPath (ordered child transforms at route turns and exits)
GameSystems (TowerPlacementController and selected default tower definition)
```

- [ ] **Step 4: Author simple biological enemy silhouettes and tower prefabs**

Create each enemy from child `SpriteRenderer` primitives or placeholder simple shapes, then apply these serialized definition values: Hound `(35, 2.7, 5)`, Boar `(85, 1.6, 10)`, Rhino `(280, 0.75, 25)`, Lizard `(20, 1.35, 3)`. Create Bean Pod `(range 3.0, damage 20, interval 0.75, projectile speed 7.0)`, Lightning Spire `(4.2, 50, 1.35)`, and Command Banner `(2.5, range multiplier 1.2, rate multiplier 1.25)` prefabs. Give attack towers a trigger `CircleCollider2D` matching their base range.

- [ ] **Step 5: Re-run PlayMode integration and full test suites**

Run `EditMode > Run All`, then `PlayMode > Run All` in Test Runner.

Expected: all tests pass and SampleScene contains both a placement controller and a waypoint path.

### Task 9: Perform manual mobile-facing verification

**Files:**
- Verify: `Assets/Scenes/SampleScene.unity`
- Verify: `Assets/Game/Definitions/`, `Assets/Game/Prefabs/`, `Assets/Game/Tests/`

**Interfaces:**
- Consumes: completed scene and full automated suite.
- Produces: an evidence-backed handoff listing completed features and any unresolved editor condition.

- [ ] **Step 1: Enter Play Mode in SampleScene**

Select each tower definition in turn, hover a plaza cell, a path cell, an empty cell, and an occupied plaza cell. Verify only the unoccupied buildable plaza uses the valid preview and accepts a click.

- [ ] **Step 2: Verify combat readability**

Spawn one Hound, Boar, Rhino, and group of Lizards onto the route. Confirm the Bean Pod visibly travels to a target, Lightning Spire strikes instantly, and Command Banner changes the nearby attack towers' range and cadence while never attacking itself.

- [ ] **Step 3: Verify enemy art boundary**

Inspect all four enemy prefabs at normal mobile-game zoom. Confirm that their differing size, body outline, and earth-tone color distinguish roles, and no enemy contains mana glow, metal, armor, or machinery.

- [ ] **Step 4: Record verification output**

Report the Test Runner EditMode and PlayMode totals, the Scene hierarchy names, and any failure rather than describing unverified behavior as complete.

## Self-review

- Spec coverage: Tasks 1-2 implement enemy data and pathing; Tasks 3-6 implement targeting, both attacks, and banners; Task 7 implements tilemap building; Task 8 creates data, prefabs, and scene wiring; Task 9 covers visual acceptance.
- Placeholder scan: the implementation paths, names, values, runtime APIs, and test cases are specified; there are no deferred requirements.
- Type consistency: combat attacks consume `Health`; targeting returns `EnemyController`; each target exposes its `Health` component; banner sources call the single `TowerBuffReceiver.SetBannerSource` API.
- Review Focus coverage: path/buildable overlap and reservation are Task 7 tests; projectile target loss is Task 4; banner lifecycle is Task 6; final waypoint exit is Task 2; UI input guard belongs to Task 7 and the Task 9 manual pass.
