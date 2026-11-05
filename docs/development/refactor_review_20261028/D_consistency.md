# Perspective D — cross-module consistency, project standards, dead code

Reviewer: perspective D (read-only). Scope read line by line: all `Assets/Scripts/Runtime/**` and `Assets/Scripts/Editor/**` .cs (72 files), the 12 `CoreGuardian/*.shader`; tests read where needed (reflection names, `Find(...)` names, event subscriptions, legacy-only users). Line numbers are per file.

Format per finding: `P-level | conclusion | file:line | trigger path / evidence`.

## 0. Carriers checked for every dead-code verdict (false-positive check 7)

| # | Carrier | How it was checked | Gaps |
|---|---|---|---|
| C1 | Scene and prefab `m_Script` GUIDs | Every Runtime/Editor `.cs.meta` GUID was grepped across the 3 `.unity`, 22 `.prefab`, `.asset` and `.mat` files in the copy | **67 serialized assets listed in ASSET_LIST.txt are missing from the copy.** They include `Assets/Settings/Editor/Welcome2DScript.asset`, `NewWelcomePage.asset`, `TutorialProjectSettings.asset`, `Art/Atlases/MaskData/Resources/CoreGuardian/MaskAtlas/Manifest.asset` plus 9 `Page_*.asset`, the URP/Renderer2D assets, the tile assets and the `.spriteatlasv2` files |
| C2 | `AddComponent<T>` / `GetComponent<T>` / `new T` | Grepped across Runtime and Editor | — |
| C3 | Reflection and string lookups | The tests use `GetType("X")` 32 types, `GetField/GetProperty/GetMethod("…")` 618 calls, `Find("…")` about 180 names. The member names were extracted and cross-checked | Ad-hoc Unity MCP `eval` scripts (AGENTS.md:20) could not be checked |
| C4 | Framework dispatch | `RuntimeInitializeOnLoadMethod` (6 sites), static constructors with `RenderPipelineManager` / `SceneManager` hooks, `AssetPostprocessor`, `IPreprocessBuildWithReport`, `[MenuItem]` | — |
| C5 | UnityEvent `m_MethodName` in scenes/prefabs, `SendMessage`, `Invoke("…")` | Grepped | Nothing found |
| C6 | `Resources.Load` paths and shader-name loads | Grepped | — |
| C7 | Tests | Grepped by name and by reflection string | — |

---

## 1. Enumeration (review objects)

**Twin sets (T1–T16)**

| ID | Twin set | Members |
|---|---|---|
| T1 | Outline consumers | SelectionOutline (`CoreGuardianSelectionOutline.cs`), ObjectEmphasis, SharedOutline, RageOutline, UpgradeFeedback |
| T2 | "Native gameplay camera" gates | SelectionOutline.cs:122-129, :235-238, :248-251; SharedOutline.cs:111-120; BodyMaskCache.cs:57-59 |
| T3 | Walk systems | OriginalWalk.cs, FrameWalk.cs, PlayerMotion.cs, OriginalIdle.cs; turret aim in OriginalMotion.cs:156-170 vs :174-200 |
| T4 | Mask pipeline | MaskPage, MaskAtlas, MaskProducerBatch, MaskOutputBatch, BodyMaskCache |
| T5 | Audio | CombatAudio, MusicAudio, Hud.Sound, SoundSettings |
| T6 | Targeting | Domain/Towers/TowerTargeting.cs; Session.Shoot (Session.cs:723); Session.TowerTarget (Session.cs:30); CombatRules.Priority / TowerPriorityOrder (CombatData.cs:392-394) |
| T7 | Legacy simulation vs Session | Health / EnemyController / WaypointPath / BeanProjectile / LightningTower / TowerCombatStats vs Session / EnemyState / ProjectileState |
| T8 | "Tower:" source parsing | Session.cs:195; Hud.cs:269-275, :291-299; CombatArt.cs:43-44 |
| T9 | Placement legality | Session.CanInstall (Session.cs:290); Prototype.CanInstall (Prototype.cs:238) + ChooseTower (:246-248); Hud.InstallTowerButton (Hud.cs:343/345); PrototypeGridMovement.CanOccupy (:120) |
| T10 | Display-name tables | Hud TowerName / TowerShortName / KindName / BossName / GradeName / StatName; Session.DebugEnemyName (Session.Debug.cs:69); AugmentationCard.GradeName (Augmentations.cs:15) |
| T11 | Hit-flash style recolor | CoreGuardianHitFlash vs CoreGuardianCoreFeedback |
| T12 | Neutral sprite property block | PlacementOverlay.cs:36-37, RouteMeshBake.cs:13-16, SpriteBatchCompat.cs:34-37 |
| T13 | Destroy-or-DestroyImmediate helper | 10 copies |
| T14 | Name-based part lookup | "2D Sprite" (8 runtime sites) and shadow-part classification (5 files); the "core" sprite lookup (3 sites) |
| T15 | Upgrade stat lists | Hud.UpgradeOrder (Hud.cs:306), Inspector.LabStats (Inspector.cs:24), CombatStats.IsStat (CombatData.cs:54), Hud.StatValue / StatName |
| T16 | Shader twins | Outline dilation kernel (SelectionOutline.shader:26-29, BatchedMaskOutput.shader:37-39, SharedOutlineOutput.shader:47-50); contact band (PhotoShadowContact.shader:24-28 vs BatchedMaskOutput.shader:30-36); mask producers (SelectionMask, BatchedSelectionMask, SharedOutlineMask) |

**Dead-code candidates (D1–D30).** Listed in §3.

**Global and static mutable state.** PerfOverlay.instance; ArtDepthSorting.nextLane; AudioCatalog.cues; SelectionOutline.contactShader; ProducerBatch material / sourceShader / checkedSource / commands; BodyMaskCache nextEpoch + Hits / Generations / Allocations / Releases; PhotoShadow.registration; SpriteLibrary instance / searched; SpriteBlocks.probe; MaskPage nextGeneration / nextLease; HitFlash.shared; SharedOutline ReuseGeometry / Batched / Enabled / rfloatSupported / LayoutRebuilds; OutputBatch.material; ObjectEmphasis.tintFrame; SpriteAtlasSources mappings / loaded / initialized; SegmentedEnemyHealthBar sprites / activeBars; Hud.damageSprites; HudSkin.sprites.

**Events.** Session: DamageApplied, ProjectileLaunched, AttackLaunched, DefeatReached, CombatStarted, RoundCompleted, VictoryReached, Cue, TowerPulsed, AreaBlastUsed. Prototype.Changed; PrototypeGridMovement.Stepped; Health.Died; EnemyController.Exited.

**Dependency edges.** Domain asmdef (`Fortress.Game`, Runtime/Domain only) vs Assembly-CSharp (Bootstrap + Presentation) vs Unity.2D.Welcome.

**Standards rules checked.** REFACTORING_RULES.md lines 7, 11, 18-23, 26-28, 31-39, 43.

---

## 2. Findings (sorted by severity)

There are no P0 or P1 findings from this perspective. Every item below is either a latent inconsistency, a standards violation or a refactoring hazard, and none has a currently reproducible severe consequence that I could prove with a current-value line.

### P2

1. **P2 | In Play mode and builds, `CoreGuardianMaskOutputBatch` does no output batching.** Its static `beginCameraRendering` hook is still the only per-camera driver of `CoreGuardianBodyMaskCache.Flush`, so it is load-bearing and must not be removed as dead.
   - `CoreGuardianSelectionOutline.cs:231`: `if(Application.isPlaying)return;` runs before `CoreGuardianMaskOutputBatch.Register` at :243.
   - The play-mode-only producer path is `QueuePage` (:248 requires `Application.isPlaying`), which leads to `MarkPending`.
   - Pending pages are flushed only by `CoreGuardianMaskOutputBatch.Begin` → `Flush` → `CoreGuardianBodyMaskCache.Flush(camera)` (MaskOutputBatch.cs:65-66, 88, 93), or by `MaskPage.ReleaseID` (MaskPage.cs:108).
   - The static constructor is triggered every frame by `CoreGuardianMaskOutputBatch.Invalidate(this)` at SelectionOutline.cs:82.
   - Hazard: a refactor that "removes the edit-mode-only batcher" silently stops play-mode mask production. The masks would never become valid, and the fallback in `RenderOriginal` happens only inside `Flush`.
   - Also, SelectionOutline.cs:240 `if(Application.isPlaying&&!prepared)return;` is unreachable, because :231 already returned in play mode.

2. **P2 | The test suite reaches all of Bootstrap and Presentation only through reflection strings.** Any rename, namespace move or assembly move compiles cleanly and fails at test runtime.
   - Fortress.Game.asmdef sits in `Runtime/Domain` only (`Assets/Scripts/Runtime/Domain/Fortress.Game.asmdef`). Bootstrap and Presentation therefore compile into Assembly-CSharp, which test asmdefs cannot reference (`Fortress.Game.EditModeTests.asmdef` references `["Fortress.Game"]` only).
   - Tests use `Type.GetType("CoreGuardianPrototype, Assembly-CSharp")` (27 sites, e.g. CoreGuardianCombatAudioPlayModeTests.cs:22) and `AppDomain…GetType("CoreGuardianSelectionOutline")`.
   - 618 `GetField/GetProperty/GetMethod("…")` calls cover about 180 member names. These include private fields: `mask`, `output`, `maskBinding`, `lastAtlasMisses`, `lastMaskDrawCalls`, `previousCore`, `enemyViews`, `towerViews`, `damageNumbers`, `isMoving`, `entries`, `pending`.
   - REFACTORING_RULES.md:11 protects only "MonoBehaviour·ScriptableObject 클래스 이름과 파일 이름, 직렬화 필드 이름 … 테스트가 찾는 오브젝트 이름". It does **not** list type names of plain classes looked up by reflection (`CoreGuardianMaskPage`, `CoreGuardianBodyMaskCache`, `CoreGuardianMaskOutputBatch`, `CoreGuardianPlacementOverlay`, `CoreGuardianCoreAmbient`, `CoreGuardianCoreFeedback`, `CoreGuardianOriginalWalk`, `CoreGuardianSpriteAtlasPipeline`, `CoreGuardianRouteMeshBuilder`, `CoreGuardianMaskAtlasBuilder`, `CoreGuardianAudioCatalog`, `CoreGuardianSoundSettings`, `CoreGuardianAudioCategory`, `CoreGuardian.CombatRules`, `CoreGuardian.SegmentedEnemyHealthBar`), nor private member names. Both are equally protected in practice.

3. **P2 | Removed "ground contact" feature: about 120 lines of runtime code plus one shader are reachable only from EditMode tests.**
   - Runtime paths:
     - `PresentGroundContact` (SelectionOutline.cs:205) has zero callers anywhere.
     - `PresentGroundContactWithBand` (:209) and `PresentContact` (:213-226), together with the `compactContact` / `localContact` crop (:119-136, :122-134), the `contactShader` static (:18, :218), and `RegisterOutput(contact=true)` (:225), have callers only in tests: CoreGuardianBodyMaskSharingTests.cs:59 and :154, CoreGuardianMaskOutputBatchTests.cs:81 and :146.
     - The contact branch of `MaskOutputBatch` is MaskOutputBatch.cs:104 (`parameters.contact`) and BatchedMaskOutput.shader:30-36.
     - `PhotoShadowContact.shader` is referenced only through `Resources.Load` at SelectionOutline.cs:218 (no `.mat`, prefab or ProjectSettings GUID reference).
   - Tests assert that the feature was removed: CoreGuardianPhotoShadowTests.cs:73 "Approved removal must skip contact-mask creation entirely".
   - Carriers checked: C1–C7. No runtime caller was found. Do not delete it: it is still covered by tests, and the copy lacks the URP settings assets (C1 gap).

### P3 — twin implementations (details in §4)

4. **P3 | Five different "eligible native gameplay camera" predicates gate the same mask-sharing contract.** They differ in which of 11 conditions they include.
   - Sites: SelectionOutline.cs:122-129 (localContact), :235-238 (RegisterOutput), :248-251 (QueuePage); SharedOutline.cs:111-120 (Supports); BodyMaskCache.cs:57-59 (shareable).
   - The literals `1920`, `1080`, `8.5f`, `4.25f` and `32` are each repeated 3 times in SelectionOutline.cs. See the §4 T2 table.

5. **P3 | The "front part" sort-order and bounds rule is duplicated, with a different output offset.**
   - SelectionOutline.cs:169-176 (+4) and SharedOutline.cs:135-149 / :209 (+3). Both keep the `>=` tie rule.
   - SharedOutline skips `activeInHierarchy` on purpose (comment at :137-138), relying on its only caller ObjectEmphasis.cs:196. This is a cross-module precondition that is not enforced.

6. **P3 | RageOutline and UpgradeFeedback are near-copies of each other, with three inconsistencies.**
   - Rage removes a flash on `!target.gameObject.activeInHierarchy` (CoreGuardianRageOutline.cs:44). Upgrade does not (CoreGuardianUpgradeFeedback.cs:48).
   - Only Upgrade has `ReleaseTarget` (UpgradeFeedback.cs:64), which Prototype.cs:482 calls when a pooled enemy body is returned. Rage has no equivalent and relies on the active check.
   - Both use `[DefaultExecutionOrder(600)]` (Rage :6, Upgrade :5), so their relative order is undefined.
   - In EditMode, ObjectEmphasis.cs:219 explicitly calls `union.ReleaseResources()` because "Non-ExecuteAlways EditMode consumers may never receive OnDestroy". Rage (:56) and Upgrade (:61) do not, so their per-instance `CoreGuardianSharedMaskMaterial` lease counts and the masks in `BodyMaskCache` depend on `OnDestroy`. This is EditMode / test-only.

7. **P3 | Tower aiming math is duplicated inside OriginalMotion.**
   - Single (CoreGuardianOriginalMotion.cs:156-169) and the Flame/Rail turret (:174-199) repeat the same kick envelope (:148 vs :178), the mirror rule, foreshortening `.72f` and kick `1.72f`.
   - The `role=="Player"` branches at :150-155 and :209 are unreachable: UnitAnimation.cs:53 routes "Player" to PlayerMotion before :54-56 can construct OriginalMotion. Tests look for a GameObject named "OriginalMotion" on the player, but that object is created by PlayerMotion.cs:33, not by this class.

8. **P3 | `CoreGuardianOriginalIdle` duplicates PlayerMotion's idle breathing, and the class has zero users.**
   - Same formula: `(1-cos(t·2π/2.8))·.5·.55/ppu` at OriginalIdle.cs:27-28 and PlayerMotion.cs:77-78.
   - Tests find a GameObject named "OriginalIdle" (CoreGuardianAnimationPlayModeTests.cs:247), but that object comes from PlayerMotion.cs:33.
   - Resources `CoreGuardian/OriginalIdle/Player/{Feet,Upper}.png` (ASSET_LIST lines 2126-2129) are loaded only by the dead class.

9. **P3 | Walk twins OriginalWalk and FrameWalk differ in mirror handling and reset.**
   - On vertical travel, OriginalWalk resets `mirror=false` (OriginalWalk.cs:122). FrameWalk keeps the previous mirror (FrameWalk.cs:33-34).
   - `ResetForReuse` in OriginalWalk restores color, flipY, enabled and rig scale (:128-134). FrameWalk restores only sprite, flipX and position (:58).
   - OriginalWalk writes the vertical branch unconditionally every frame (:152-154, VerticalLegPair :280-302). FrameWalk uses change-detected writes (:49-56).
   - The `+4` pixel health-bar margin and the `.000001f` / `.0001f` thresholds are duplicated (OriginalWalk :54, :116, :123; FrameWalk :27, :30, :34).

10. **P3 | Twin "Tower:" source parsing.** HUD reimplements the parse instead of using `Session.TryTowerSourceId`.
    - Hud.cs:272 and :294 use `source.StartsWith("Tower:")&&int.TryParse(source.Substring(6))` plus `Towers.FirstOrDefault`, which allocates per row per refresh.
    - Session.cs:195-207 is culture-guarded and cached. CombatArt.cs:44 compares `AttackSource` strings.
    - The two parsers disagree for non-ASCII digit spellings (Session's comment at :201). This cannot happen with current sources, which are always `"Tower:"+Id` (CombatData.cs:448).

11. **P3 | The placement-legality rule exists in four places with different cell rules and different messages.**
    - Model: Session.IsBuildable / CanInstall (Session.cs:277, :290-293) checks bounds, `pathCells`, core, `structureCells` and towers.
    - Scene: Prototype.CanInstall (Prototype.cs:238-242) checks tilemaps ground, enemyPath, coreMap and occupied. It drives both the green/red overlay (:261) and the first rejection in Place (:288).
    - Pre-checks are repeated in Prototype.ChooseTower (:246-248) and HUD (Hud.cs:343, :345). The "support needs an attack tower" rule therefore exists 3 times.
    - Messages differ: Session "지금은 설치 불가"/"설치 불가 칸"/"공격 포탑을 먼저 설치" vs Prototype "지금은 설치할 수 없습니다."/"이 칸에는 설치할 수 없습니다."/"공격 포탑을 먼저 설치하세요.".
    - Cells where the `enemyPath` tilemap and `routeCells` (Prototype.cs:19) disagree would show green but be rejected by Session, or the reverse. This was not verified against the tile data (perspective C).

12. **P3 | The HUD recomputes simulation numbers with its own formulas.**
    - Support buff: HUD uses LINQ `Where/Sum` plus `Augments.TowerBuffPercent`, which is **rounded to an integer** (Redesign.cs:113-114; Augmentations.cs:92). The simulation uses `supportCache` plus the unrounded `Augments.TowerDamagePercent` (Session.cs:483). Prototype.cs:451 decides whether to show the tower aura with the rounded value too.
    - Affinity multipliers are duplicated with literal fallbacks `1.3f` / `.7f` (Redesign.cs:150-151; Session.Effects.cs:38 and :170-176).
    - Current data: augment cards 1/19/36 have integer values (augments.csv 22001=8, 22019=20, 22036=1), so there is no difference today. See the verification notes.

13. **P3 | Display-name tables are duplicated across layers, and Domain carries Korean UI strings.**
    - Domain: Session.DebugEnemyName (Session.Debug.cs:69-78) has no SplitChild entry, so it falls back to the key "SplitChild". HUD KindName (Redesign.cs:189-197) shows "분열 작은 개체".
    - BossName exists twice: Hud.cs:276 and Session.Debug.cs:76.
    - GradeName exists twice: Augmentations.cs:15 and Redesign.cs:251.
    - TowerName (Hud.cs:290) vs TowerShortName (Inspector.cs:129).

14. **P3 | Stat lists are duplicated with different orders, and `burnPerStack` is not representable in the Lab.**
    - Hud.cs:306 `UpgradeOrder` and Inspector.cs:24 `LabStats` hold the same 8 stats in a different order.
    - CombatStats.IsStat (CombatData.cs:54) accepts 9 stats. Lab buttons are created only for `LabStats` (Inspector.cs:118-124). A `burnPerStack` upgrade that is a group leader would be silently hidden. Today it is a group member (data-dependent).

15. **P3 | HitFlash and CoreFeedback twins restore the property block differently.**
    - HitFlash restores "no block" as `null`, with the comment "An empty override still changes sprite batching" (CoreGuardianHitFlash.cs:92-93).
    - CoreFeedback always restores the saved, possibly empty, block (CoreGuardianCoreFeedback.cs:52 `body.SetPropertyBlock(saved)`), so after the first core hit the core keeps an empty override.
    - The shared shader `CoreGuardian/BodyHitFlash` is used with a static shared material (HitFlash:27) in one twin and a per-instance material (CoreFeedback:25) in the other.

16. **P3 | Core particle defaults are duplicated.** CoreAmbient.cs:46-48 (`.025/.045`, rate 4, max 12) repeats CoreGuardianCoreParticleSettings.cs:9-15. The scene component overrides them. A refactor that changes only one place changes behaviour whenever the settings component is absent.

17. **P3 | Name-string part classification is spread over 9 files and uses two different "shadow" rules.**
    - "2D Sprite" is looked up at CombatArt.cs:33, UnitAnimation.cs:51, CombatAnimation.cs:37, SelectionOutline.cs:94, PhotoShadow.cs:35, ObjectEmphasis.cs:167 and :186, Prototype.cs:198, and in the editor at ContentAssets.cs:35.
    - Shadow parts are detected by **sprite name** `Contains("Shadow")` (OriginalWalk.cs:86, HitFlash.cs:29) vs **GameObject name** "바깥 그림자"/"접촉 그림자"/"바닥 밀착선"/"그림자" (PhotoShadow.cs:64, TowerGrounding.cs:22-28, Prototype.cs:251).
    - "바닥 밀착선" is hidden in four different ways: `enabled=false` at OriginalWalk.cs:91 and PlayerMotion.cs:26, re-disabled every frame at PhotoShadow.cs:68, and `SetActive(false)` at TowerGrounding.cs:28.
    - The "core" sprite lookup is copied 3 times: CombatArt.cs:32-33, CombatAnimation.cs:36-37, ObjectEmphasis.cs:166-167. The last one runs `FindObjectsByType` every LateUpdate until the core is found.

18. **P3 | Twin helpers: Destroy-or-DestroyImmediate exists 10 times, `EnsureFolder` 3 times, and the neutral sprite property block 3 times.**
    - Destroy helpers: SelectionOutline.cs:422, SharedOutline.cs:344, UpgradeFeedback.cs:71, TowerGlow.cs:68-72, TowerGrounding.cs:69, CoreAmbient.cs:90-94, PlacementOverlay.cs:67, MaskPage.cs:114 (the only one without a null check), MaskOutputBatch.cs:223, plus inline copies (Rage :56, SegmentedEnemyHealthBar :60, ProducerBatch :152).
    - EnsureFolder: SpriteAtlasPipeline.cs:107, MaskAtlasBuilder.cs:88, RouteMeshBuilder.cs:152.
    - The `unity_SpriteProps/unity_SpriteColor/_RendererColor` block: PlacementOverlay.cs:36-37, RouteMeshBake.cs:13-16, SpriteBatchCompat.cs:34-37.

19. **P3 | Device-support and material ownership patterns are inconsistent across the mask pipeline (T4).**
    - The expression `supportsComputeShaders&&(D3D11||D3D12)` is duplicated at ProducerBatch.cs:46 and SharedOutline.cs:32.
    - Material ownership comes in 4 patterns: leased shared (SelectionOutline.cs:396-397), static (ProducerBatch :70-74, OutputBatch :120-123), per-instance (SharedOutline :313-314), per-scan (UpgradeFeedback :36).
    - Lifecycle hooks differ. BodyMaskCache has no `beforeAssemblyReload` or `quitting` (BodyMaskCache.cs:109-120). MaskPage has no `ExitingEditMode` (MaskPage.cs:71-80). OutputBatch hooks both camera begin and end (:65-75).

20. **P3 | Twin area-blast implementations.** `UseAreaBlast` (Session.Effects.cs:419-442) and `UseTeleportBlast` (:445-468) are line-for-line copies apart from the cooldown and the scale. A fix applied to one, such as the 43/25 execute rules or the cue order, can miss the other.

21. **P3 | Legacy parallel simulation (T7) duplicates Session concepts with different rules.**
    - TowerTargeting picks the enemy with max `Progress` (TowerTargeting.cs:16). Session uses `TargetOrder`.
    - BeanProjectile hits at 0.05 (BeanProjectile.cs:36). Session hits at .00001 (Session.cs:516).
    - LightningTower hard-codes `damage = 50` (LightningTower.cs:8). TowerCombatStats uses banner ×1.2 / ×1.25 (TowerCombatStats.cs:7-8). Neither has a CSV equivalent.
    - See D1–D7 for liveness.

22. **P3 | `CombatRules` target helpers ignore their `tower` argument and encode a rule the runtime no longer uses for towers.**
    - `CompareTargetPriority(a,b,bool tower)` ignores `tower` (CombatData.cs:392). `playerPriority` and `towerPriority` are therefore identical (:390).
    - `TowerPriorityOrder` (:394) orders towers by `TargetOrder.Common`. The runtime uses per-kind `TowerOrder` (Session.cs:812).
    - Only tests use these helpers (CoreGuardianContentTests.cs:120, CoreGuardianSessionTests.cs:222, :239-241). Tests therefore validate a tower order the game does not use.

### P3 — dead code (details and carriers in §3)

23. **P3 | Unreferenced runtime members.** No reference was found among carriers C1–C7 for:
    - Prototype.Ring (Prototype.cs:608), the `dot` sprite (:61, :113, :635: created and destroyed, never used), IsLabCell / IsBenchCell (:86-87)
    - Hud: ReleaseInspection (Inspector.cs:44), InfoVisible (Redesign.cs:47), the PurchaseTower → TowerCanUpgrade → Price → Relevant chain (Hud.cs:289, :309, :301, :308), `towerFolded` (write-only: Hud.cs:70, Inspector.cs:192), `towerAbilities` (never assigned, Abilities.cs:9)
    - RageOutline.IsFlashing (:23)
    - UnitAnimation: Loaded (:35), IsEnemyBody (:39), FrameInfo / AtlasInfo (:10-11)
    - OriginalWalk.Phase (:52)
    - ArtDepthSorting.SetArtwork (:17)
    - SharedOutline: SetBatched (:28), ReuseGeometry writers (:82, read only at :239), LayoutRebuilds / LastMaskDraws / LastEntityCount / LastPartCount (:310, :91, :106-107)
    - MaskPage.PageCount / RuntimeBytes (:62-63)
    - BodyMaskCache: Hits / Generations / Allocations / Releases / AllocatedCount / AllocatedBytes (:22, :25-26)
    - SpriteBatchCompat.LastColorTwins / LastMeshes (:16-17)
    - CombatAudio: SessionCuePlayedCount (:121), MultiVolleyPlayedCount (:45), ActiveVoices (:48), DroppedCount (:44) — write-only
    - Session.FireCellCount (Session.Effects.cs:395), DebugAddProjectile (SessionModels.cs:107)
    - HitPayload.ExtraLine (ModelPooling.cs:110), HitPayload.Skill (written at Session.Effects.cs:427 and :453, never read)
    - The `shots` loop that is always 1 (Session.cs:778-779)
    - The diagnostic counters may be read by ad-hoc MCP `eval` scripts (gap C3).

24. **P3 | Domain legacy folders are test-only.** None of `Domain/Common/Health.cs`, `Domain/Enemies/*` or `Domain/Towers/{BeanProjectile,LightningTower,TowerTargeting}` is referenced by any scene, prefab or runtime code. LightningTower and TowerTargeting are not referenced even by tests. See D1–D7. Deletion needs user approval (REFACTORING_RULES.md:16 "안 쓰는 코드·파일 삭제는 목록으로 보고하고 사용자 승인 후에만.").

25. **P3 | `PrototypeGridMovement` is live, not legacy.** It is referenced by GUID in all 3 scenes (C1), by the field `CoreGuardianPrototype.player` (Prototype.cs:14), and by tests (CoreGuardianRuntimePlayModeTests.cs:263). Do not treat it as legacy.

26. **P3 | Editor/ProjectWelcome is a 2D-template leftover, but its instance asset is an unchecked carrier.**
    - Welcome2DScript.cs:5-12 has no code reference.
    - ASSET_LIST lines 92-97 list `Assets/Settings/Editor/{Welcome2DScript,NewWelcomePage,TutorialProjectSettings}.asset`, which are not in the copy (C1 gap). These are presumably the instance and the welcome page that invoke `Open2DLandingPage`.
    - No tutorial package appears in Packages/manifest.json.
    - `Unity.2D.Welcome.asmdef` has no `includePlatforms: ["Editor"]`, so this "Editor" script compiles into player builds.

27. **P3 | The legacy CSV loader path is test-only in the shipped game.**
    - `CombatData.Load` takes `LoadLegacy` only when the enemies header lacks `key` (CombatData.cs:122-132).
    - The scene CSV (`Assets/Data/Balance/Resources/CoreGuardian/enemies.csv`, header `id,key,…`) always takes `LoadTables`.
    - `LoadLegacy` (:137-157) is exercised only by `Tests/EditMode/Fixtures/LegacyData` through CoreGuardianLegacyData.cs.
    - Even the legacy path loads the live `augments/effectTags/skills` through `Resources` (:155). This is a hidden global input in the Domain loader.

28. **P3 | HudSkin.Symbol has unreachable branches and a garbage literal.**
    - `name=="接기"` (Chinese characters) at CoreGuardianHudSkin.cs:45.
    - `name=="rate"||name=="Tower_rate"` at :47 can never match, because :41 already returned for those names.
    - `name=="PlayerSpecs"` at :50 can never match, because :38 already returned.
    - `RefreshAbilityPlate(...,bool support)` is called only with `false` (Inspector.cs:194), so every `support` branch in Abilities.cs:75-90 is dead.

29. **P3 | Some "looks dead" items are live and must stay.**
    - The invisible `status` text (color clear, Hud.cs:195, rebuilt every refresh at :509) is asserted by CoreGuardianRuntimePlayModeTests.cs:203 (`GameObject.Find("Status")`).
    - `SoundSettings.Candidate` (:37-41) is used by reflection in CoreGuardianCombatAudioPlayModeTests.cs:509.
    - `TowerCombatStats` is the assembly anchor in CoreGuardianRulesTests.cs:14 (`typeof(TowerCombatStats).Assembly`). Deleting it breaks that test at compile time.
    - `SpriteBatchCompat` and `PerfOverlay` are live through `RuntimeInitializeOnLoadMethod` (:19 / :25).
    - `MaskOutputBatch` is live (finding 1).

### P3 — events (bidirectional check)

30. **P3 | Cue "split" is published but has no consumer.** It is raised at Session.Effects.cs:254. CombatAudio drops it explicitly (CombatAudio.cs:124 `if(cue.Id=="split")return;`). EffectsView has no `case "split"` (EffectsView.cs:70-83).
    - Also not drawn by EffectsView: crit, flame_shot, rail_shot, cold_pulse, freeze, reroll and area_blast (the last is drawn through `AreaBlastUsed`). Their audio consumption depends on `catalog.json`, which is not in the copy and was not verified.

31. **P3 | The `HitCritical` / `HitAffinity` "ambient during callback" contract is implicit.**
    - The HUD reads `Session.HitCritical` and `HitAffinity` inside the DamageApplied callback (Hud.cs:383-384).
    - They are set around `DealDamage` only in `Strike` (Session.Effects.cs:184-185) and in `TickBurn` (:122). Execute damage (:199) and nested chain hits run after the finally block resets them.
    - A refactor that moves the `DamageApplied` invoke or the try/finally changes damage-number colours silently.

32. **P3 | Subscriber order on `Session.DamageApplied` is fixed by `Prototype.Start` call order.**
    - The order is CombatAudio (Prototype.cs:111 → CombatAudio.cs:83), Hud (:114 → Hud.cs:115), CombatArt (:115 → CombatArt.cs:30), CombatAnimation (:116 → CombatAnimation.cs:42), EffectsView (:117 → EffectsView.cs:26).
    - REFACTORING_RULES.md:10 requires "이벤트 순서 … 바꾸지 않는다". Moving these Configure calls is a behaviour change.
    - Unsubscribe pairs are complete for all 5 (negative result).

33. **P3 | Legacy events have test-only subscribers.** `Health.Died` (Health.cs:9) and `EnemyController.Exited` (EnemyController.cs:7) are subscribed only by HealthPlayModeTests and EnemyMovementPlayModeTests.

### P3 — global and static state

34. **P3 | Static state persists across Play sessions without a SubsystemRegistration reset.** AGENTS.md:20 says the environment runs with "Domain Reload OFF".
    - `ArtDepthSorting.nextLane` (ArtDepthSorting.cs:13, :27): lane assignment, and with it the depth order among same-row units, continues from the previous session and from `Restart()`.
    - `CoreGuardianSpriteLibrary.searched` (:14, :19): one failed lookup stays null for the rest of the editor domain.
    - `CoreGuardianAudioCatalog.cues` (:34) and `CoreGuardianPhotoShadow.registration` (:15): JSON edits are not picked up.
    - `CoreGuardianHitFlash.shared` (:12, :27): a HideAndDontSave material that is never released.
    - Compare BodyMaskCache.cs:119, ObjectEmphasis.cs:80, SpriteAtlasSources.cs:17 and SegmentedEnemyHealthBar.cs:49, which do reset.

35. **P3 | Globally writable statics violate REFACTORING_RULES.md:37 "전역에서 쓰기 가능한 static·public 필드 금지(읽기 전용 속성으로)."**
    - `public static bool ReuseGeometry=true;` (SharedOutline.cs:82)
    - `SetBatched` / `SetEnabled` public static mutators (:28, :36)
    - `internal static long Hits,Generations,Allocations,Releases;` (BodyMaskCache.cs:22)
    - Public mutable Domain state readable and writable by Presentation: `Session.Towers` is a `List<TowerState>` (Session.cs:11); `DamageTotals` is a `Dictionary` (:13); TowerState has public fields `Id, Spent, Kind, Position, Stats, Stars` (CombatData.cs:444-446); AugmentationEffects has public counters (Augmentations.cs:75); CombatData has about 60 public fields (CombatData.cs:67-94).
    - Negative result: I found no Presentation or Bootstrap write into these (grep of `.Stats.X =`, `Towers.Add/Remove`, `Augments.X =` in Runtime/Presentation and Bootstrap returned nothing).

### P3 — dependency direction and layering

36. **P3 | The rule's namespace premise does not match the code.** REFACTORING_RULES.md:23 says "네임스페이스는 지금처럼 `CoreGuardian`".
    - Only the 8 Domain/Rules files use `namespace CoreGuardian`.
    - 64 runtime and editor files, including the legacy `Domain/Common`, `Enemies` and `Towers` files, are in the global namespace.
    - The asmdef has `"rootNamespace": "Fortress.Game"` (Fortress.Game.asmdef). Tests use `Fortress.Game.Tests` or the global namespace. ProjectWelcome uses `Unity.U2D.Welcome`.
    - "Moving to CoreGuardian" would break every `GetType("CoreGuardianPrototype, Assembly-CSharp")` in finding 2.

37. **P3 | Domain → Presentation compile direction holds, but Domain hosts presentation concerns.**
    - Compile-time direction is enforced by the asmdef, so REFACTORING_RULES.md:33 "Domain(규칙) → Presentation(화면) 참조 금지" is satisfied.
    - The Domain assembly nevertheless contains:
      - a texture-generating MonoBehaviour `SegmentedEnemyHealthBar` (SegmentedEnemyHealthBar.cs:9-64)
      - `Resources.Load` asset access (CombatData.cs:134)
      - presentation-only flags HitAffinity / HitCritical / HealMarkSeconds (Session.Effects.cs:163-168, :347)
      - Korean UI strings in `LastMessage` (e.g. Session.cs:303, :308) and DebugEnemyName (Session.Debug.cs:74-76)
      - presentation cue ids (Session.Effects.cs:12-13).

38. **P3 | There are layering cycles inside Presentation (hard rule 3 of the skill: one-directional).**
    - Rendering → UI: SelectionOutline.LateUpdate reads `CoreGuardianHud.InspectionTarget` (SelectionOutline.cs:61-62).
    - Rendering → Characters: `pageRasterSupported` reads `CoreGuardianUnitAnimation.PresentationRole` (:90-93).
    - Rendering ↔ Characters: SpriteAtlasSources.Reset calls OriginalWalk.ClearCachedParts (SpriteAtlasSources.cs:18), and OriginalWalk calls SpriteAtlasSources.Cut (OriginalWalk.cs:251).
    - SelectionOutline ↔ BodyMaskCache ↔ MaskPage ↔ ProducerBatch, and MaskOutputBatch ↔ SelectionOutline (BodyMaskCache.cs:30 and :126; MaskPage.cs:108; MaskOutputBatch.cs:76).
    - Bootstrap ↔ Presentation: Prototype ↔ Hud, CombatArt, CombatAnimation, EffectsView and ObjectEmphasis (Prototype.cs:114-117 and :481; e.g. ObjectEmphasis.cs:162-164).

### P3 — project standards (REFACTORING_RULES.md, quoted)

39. **P3 | `?.` / `??` on UnityEngine.Object: 43 sites.** Rule line 36: "`UnityEngine.Object`에 `?.`·`??` 금지(파괴된 오브젝트 판정이 틀림) → `!= null` 비교."
    - Prototype.cs:150, 246, 247, 248, 288, 290, 293, 313 (2), 334, 339, 340, 350, 352, 362, 364, 390, 430, 481, 482, 523 (`??`), 528 (`??`)
    - Hud.cs:179, 313, 352; Inspector.cs:58, 79, 151, 171; Redesign.cs:32, 38, 44, 281
    - AugmentationView.cs:26 (`??` Font), 64
    - CombatArt.cs:45 (`??=` Sprite), 46 (`??=` + `?.Get`)
    - CombatData.cs:134 (`?.text` on TextAsset)
    - SelectionOutline.cs:94
    - TowerGrounding.cs:25, 26
    - SpriteLibrary.cs:35 (`??` Sprite)
    - Current values: each target component exists today. CombatAudio is added at Prototype.cs:110. ObjectEmphasis and UpgradeFeedback have GUID references in the scene (C1). So there is no current misbehaviour.

40. **P3 | Empty catch.** Rule line 35: "빈 `catch` 금지. 실패를 무시하면 이유를 주석으로." — CoreGuardianPerfOverlay.cs:319 `try{csv?.Dispose();}catch{}` has no comment. It is the only site found.

41. **P3 | Line length, spacing and braces are violated pervasively.**
    - Rule lines 19-21: "들여쓰기 4칸, 중괄호는 새 줄, 쉼표·연산자·키워드 뒤 띄어쓰기." / "한 줄 120자 이하, 한 줄에 문장 하나." / "`if`·`for`·`foreach` 본문은 중괄호."
    - 1222 lines over 120 characters in Runtime and Editor. The worst files are Session.cs 120/857, CombatData.cs 115/452, Hud.cs 99/598, Prototype.cs 65/638 and Hud.Redesign.cs 64/402.
    - Single-line multi-statement code is the house style. Example: Session.cs:258 is one 600+ character constructor line.
    - Rule line 22 (LF): only Tests/EditMode/CoreGuardianRulesTests.cs has CRLF in the copy. The zip may have normalised line endings, so this is unverified on the PC. There is no BOM anywhere.
    - Rule line 18 "`.editorconfig`가 정본": no `.editorconfig` exists in the copy.

42. **P3 | Function size and parameter count.** Rule line 31: "한 함수 50줄, 매개변수 4개, 중첩 3단을 넘으면 나눈다(기본값)."
    - Over 50 lines:
      - SelectionOutline.PresentMask (:79-184, 106 lines)
      - Session.Shoot (:723-810, 88)
      - CombatData.LoadTables (:256-343, 88)
      - Session.StepTick (:424-502, 79)
      - MaskAtlasBuilder.Build (:10-87, 78)
      - RouteMeshBuilder.CombineKeeping (:66-133, 68)
      - MaskOutputBatch.Flush (:90-155, 66)
      - SpriteAtlasPipeline.Synchronize (:30-85, 56)
      - Prototype.Update (:138-193, 56)
    - Over 4 parameters:
      - Session.Shoot has 12 parameters (:723)
      - MaskProducerBatch.QueueShape 10 (:64)
      - AugmentationDeck.Draw 9 (Augmentations.cs file line 29)
      - CombatData.Load and LoadTables 9 (:135, :256)
      - MaskPage.Queue 9 (:65)
      - Hud.Button 7 (Hud.cs:176), Hud.Box 6 (:165)

43. **P3 | Hot-path complexity comments are absent.** Rule line 38: "전투 중 매 프레임 경로의 반복·중첩 함수는 주석으로 복잡도와 N의 최대치를 적는다(예: `O(적 수), 최대 약 150`)." There is zero `O(` comment in Runtime. Examples: Session.StepTick aura loop is O(E·B) (Session.cs:455-466); MoveLine is O(E) per projectile (:562-567); Nearest is O(E·chain) with `List.Contains` (Session.Effects.cs:276-286).

44. **P3 | Public API summary comments.** Rule line 39: "공개 API에는 요약 주석." There are 3 `///` lines in all of Runtime and Editor, all in Health.cs:4-6, a test-only legacy type.

45. **P3 | Unit-less and non-predicate names.** Rule lines 27-28: "단위가 있는 값은 이름에 단위를 붙인다: `Seconds`, `Tiles`, `Px`, `Ratio`." / "bool은 `is/has/can` 또는 상태 형용사".
    - Examples: `Duration` (RageOutline.cs:11 = seconds; UpgradeFeedback.cs:11-12), `stepDuration` (PrototypeGridMovement.cs:12), `Cooldown` / `Range` / `Speed` / `Rate` in CombatStats (CombatData.cs:50), `LineHitRadius` (Session.cs:554, tiles), `cyclePixels` (FrameWalk.cs:11, fine), `HealMarkSeconds` (fine).
    - Bools: `replace`, `generic`, `single`, `compact`, `merge`, `shared`, `mirror`, `walking`.

46. **P3 | Planning numbers remain in code.** Rule line 34: "기획 수치는 CSV가 정본이므로 코드에 남은 기획 수치는 목록으로 보고." This is the list:
    - TagCache fallbacks (Session.Effects.cs:33-38: 60, 30, .5, 5, 1, 50, 2, .7, .8, 8, 0, 2, 100, .25, 3, 1.3, .7), with 1.3 / .7 repeated at Redesign.cs:150-151
    - Star multipliers 3.2 / 1.8 and range 1 / .5 (Session.cs:326-327)
    - Refund 50 (:307)
    - Augment UI: rerolls 1, seconds 30, cards 3, grades 60 / 30, weight 25 (Session.cs:614-615, 626-628; Session.Debug.cs:56-57; Augmentations.cs file lines 22, 29)
    - Player slow default 20% (Session.Effects.cs:75)
    - Heal halving .5 / .5 (:365)
    - Shatter ×.5 (:144)
    - Split / summon offset ±.3 (:253, :383)
    - Rage defaults 50 / 30 (CombatData.cs:26-27)
    - LineHitRadius .35 (Session.cs:554)
    - Tick step .05 (Session.cs:420)
    - Buildings (2,1), (35,1), size 2 (Prototype.cs:305-307)
    - Board 45×25, core (29,9), route (Prototype.cs:17-19)
    - LightningTower 50 (LightningTower.cs:8)
    - Banner ×1.2 / ×1.25 (TowerCombatStats.cs:7-8)

### P3 — shaders

47. **P3 | Shader twins duplicate formulas.** Rule line 42 allows a `.hlsl` merge "결과 동일 확인(픽셀 비교 테스트) 후에만".
    - The square dilation kernel exists 3 times: SelectionOutline.shader:26-29, BatchedMaskOutput.shader:37-39, SharedOutlineOutput.shader:47-50.
    - The contact band constants `.55` / `.21` / `half4(.035,.03,.025,…)` exist twice: PhotoShadowContact.shader:26-28 and BatchedMaskOutput.shader:33-35.
    - The clip rule `alpha*opacity-threshold` exists 3 times: SelectionMask.shader:39, BatchedSelectionMask.shader:45, SharedOutlineMask.shader:31.
    - These are renaming and merge hazards only. Property names follow `_PascalCase` (negative result).

---

## 3. Dead-code inventory (with carriers)

"No ref" means no reference was found among carriers C1–C7 (§0) unless a gap is noted.

| ID | Item | Runtime users | Test users | Verdict |
|---|---|---|---|---|
| D1 | Health (Domain/Common/Health.cs:7) | Only legacy Domain types (EnemyController.cs:4, :20; BeanProjectile.cs:5; LightningTower.cs:12; TowerTargeting.cs:16). No scene or prefab GUID (C1) | HealthPlayModeTests, EnemyMovementPlayModeTests, BeanProjectilePlayModeTests | Test-only |
| D2 | EnemyController / EnemyDefinition / WaypointPath | None; no GUID; `EnemyDefinition` has `[CreateAssetMenu]` but no `.asset` instance in the copy | EnemyMovementPlayModeTests | Test-only. Tile assets are missing from the copy, but they cannot be EnemyDefinition |
| D3 | BeanProjectile | None | BeanProjectilePlayModeTests | Test-only |
| D4 | LightningTower | None | None | No ref (C1–C7) |
| D5 | TowerTargeting | Only LightningTower | None | No ref outside D4 |
| D6 | TowerCombatStats | None | TowerCombatStatsTests, plus the anchor at RulesTests.cs:14 | Test-only. Deleting it breaks compile |
| D7 | CombatRules.Priority / TowerPriorityOrder / CompareTargetPriority (CombatData.cs:392-394) | None | ContentTests, SessionTests | Test-only |
| D8 | Session test hooks: DebugProjectile, DebugReplaceProjectiles, DebugReverseEnemies, DebugRemoveEnemy, DebugClearProjectiles, TrySetHealth, TryUpdate, TryRead, EnemyPool/ProjectilePool, TowerTarget(int), IsBuildable, DebugGrantAugmentation | None | Yes | Test-only API in Domain. Keep |
| D9 | Session.DebugAddProjectile (SessionModels.cs:107) | None | None | No ref |
| D10 | Session.FireCellCount (Session.Effects.cs:395) | None | None | No ref |
| D11 | HitPayload.ExtraLine (ModelPooling.cs:110) and HitPayload.Skill (written only) | — | — | No ref / write-only |
| D12 | CombatData.LoadLegacy path (:137-157) | Unreachable with the shipped CSV (finding 27) | Legacy fixtures | Test-only |
| D13 | OriginalMotion "Player" branches (:150-155, :209) | Unreachable (UnitAnimation.cs:53) | — | Dead branch |
| D14 | CoreGuardianOriginalIdle (whole class) | None | None (tests find the *GameObject* "OriginalIdle" made by PlayerMotion.cs:33) | No ref. The `Resources/OriginalIdle` art is used only by it |
| D15 | UnitAnimation.FrameInfo / AtlasInfo (:10-11), Loaded (:35), IsEnemyBody (:39) | None | None | No ref. The comment "diagnostic tools" names no tool (C3 gap: MCP eval) |
| D16 | OriginalWalk.Phase (:52) | None | None | No ref |
| D17 | Prototype.Ring (:608-614), `dot` sprite (:61, :113, :635), IsLabCell / IsBenchCell (:86-87) | None | None | No ref. `dot` allocates a texture at Start |
| D18 | Hud chain: PurchaseTower (Hud.cs:289) → TowerCanUpgrade (:309) → Price (:301) → Relevant (:308) | None | None | No ref. Superseded by the Lab |
| D19 | Hud.ReleaseInspection (Inspector.cs:44), InfoVisible (Redesign.cs:47), towerFolded (write-only), towerAbilities (never assigned), the `support` branch in RefreshAbilityPlate | None | None | No ref |
| D20 | Prototype.UpgradeTower(stat) (:343) | Only the dead D18 | GetMethod("UpgradeTower") | Test-only |
| D21 | SelectionOutline.PresentGroundContact (:205) | None | None | No ref |
| D22 | PresentGroundContactWithBand / PresentContact / contact crop / PhotoShadowContact.shader | None | BodyMaskSharing and MaskOutputBatch tests | Test-only (finding 3) |
| D23 | SelectionOutline.PresentAtOrder (:191) | None (ObjectEmphasis uses the `…WithSources` variant) | ObjectEmphasisTests.cs:30 | Test-only |
| D24 | MaskOutputBatch Register/Flush batching body, BatchedMaskOutput.shader | EditMode only (SelectionOutline.cs:231) | MaskOutputBatchTests | Play-mode inert, **but the hook is load-bearing** (finding 1) |
| D25 | MaskPage.Queue (mesh variant, :65) and ProducerBatch.Queue (:57) | None (runtime uses QueueShape) | MaskProducerTests | Test-only |
| D26 | Diagnostic counters (finding 23 list) | Write-only | Some via reflection (EntryCount, BindingCount, LastProducerDraws, LastBatchedOutputs) | Keep the ones tests read; the rest have no ref (C3 gap: MCP eval) |
| D27 | RageOutline.IsFlashing (:23), ArtDepthSorting.SetArtwork (:17), SharedOutline.SetBatched (:28) | None | None | No ref |
| D28 | HudSkin.Symbol unreachable branches (:45, :47, :50) | Unreachable | — | Dead branch |
| D29 | Welcome2DScript (Editor/ProjectWelcome) | None in code | None | **Not verifiable.** Instance `Assets/Settings/Editor/Welcome2DScript.asset` is missing from the copy (C1 gap) |
| D30 | SoundSettings.Candidate (:37-41) | None | CombatAudioPlayModeTests.cs:509 | Test-only. The comment says "Legacy" |

---

## 4. Twin comparisons (feature point by feature point)

### T1 — Outline consumers

| Feature | SelectionOutline (HUD) | ObjectEmphasis | SharedOutline | RageOutline | UpgradeFeedback |
|---|---|---|---|---|---|
| Owner and instance | Scene component (GUID in scene) | Scene component; one disabled SelectionOutline per body (:188-190) | Plain class owned by Emphasis (:178) | Added by CombatAnimation.cs:44; one disabled union per target | Scene component; one union per target |
| Execution order | 500 | 450 | — | 600 | 600 |
| Source filter | enabled, sprite, alpha ≥ threshold, activeInHierarchy (:99) | activeInHierarchy (:196) | enabled, sprite, alpha (no active check, :139) | via union | via union |
| Order offset | +4 (:176) | +3 via `PresentAtOrderWithSources(…,3,…)` (:206) | +3 (:209) | +4 (Present) | +4, then `++` in PresentOverlay (:71) |
| Width and color | Inspector Range 0..8 | Clamp 0..8 (:204) | Clamp 0..8 (:124) | const 2 | default 2 |
| Liveness check | `hud.InspectionTarget` | `activeInHierarchy` | — | null or inactive (:44) | null only (:48) |
| ReleaseTarget | — | Yes (:227) | — | **No** | Yes (:64) |
| EditMode ReleaseResources | — | Yes (:219) | Release() | **No** | **No** |
| Material ownership | Shared lease | via union | Own instances | via union | Own scan material |

### T2 — Native camera gate

Conditions in each gate, in this column order: ortho / stereo / dynRes / rotation=identity / viewport==1920×1080 / orthoSize∈{8.5,4.25} / DefaultProjection / |cam|≤32 / |artwork|≤32 / pixel-rect rounding / isPlaying.

| Gate | Conditions |
|---|---|
| SelectionOutline localContact (:122-129) | ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓, plus x=y=0, radius==2; – |
| RegisterOutput (:235-238) | ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✗ ✗; **requires !playing** (:231) |
| QueuePage (:248-251) | ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✓ ✗; **requires playing**, plus pageRasterSupported, plus producer Supported |
| SharedOutline.Supports (:111-120) | ✓ ✓ ✓ ✓ ✗ (any size, x=y=0) ✗ ✗ ✗ ✗ ✗; plus parent rotation, RFloat |
| BodyMaskCache shareable (:57-59) | ✓ ✓ ✓ ✗ ✗ ✗ ✗ ✗ ✗ ✓ (rect and viewport) |

### T3 — Walks

See finding 9. In addition, PlayerMotion advances phase using `lossyScale` and the constant 64 (PlayerMotion.cs:53), while the enemy walks use `ppu/cyclePixels`. These are different gait clocks by design.

### T4 — Mask pipeline

| Feature | MaskPage | ProducerBatch | OutputBatch | BodyMaskCache | MaskAtlas |
|---|---|---|---|---|---|
| Kind | static class | static class | static class | static class | ScriptableObject (Manifest.asset missing from the copy) |
| Active in Play | Yes | Yes | Hook only (finding 1) | Yes | Yes, when `supports2DArrayTextures` |
| Reset hooks | sceneUnloaded, beginFrameRendering, quitting, assembly reload, exit-play | via MaskPage | begin/end camera, sceneUnloaded, quitting, assembly reload, exit-play | sceneUnloaded, exit-edit / exit-play, SubsystemRegistration | OnEnable / OnValidate; editor postprocessor |
| Dispose null check | **no** (:114) | inline | yes | inline | — |

### T5 — Audio

| Feature | CombatAudio | MusicAudio | Hud.Sound |
|---|---|---|---|
| Session coupling | 8 event subscriptions with OnEnable/OnDisable symmetry (:83-86) | **polls** `session.Phase` every Update (:37) | `game.GetComponent<CoreGuardianCombatAudio>()` each click |
| Volume | per-frame `cue.Volume` → 2 PlayerPrefs reads per voice (:89; SoundSettings.cs:22, :28) | per frame, the same (:49) | slider writes PlayerPrefs |
| Ducking | Calls `music.Duck` (:215) | Owns it | — |
| Created by | Prototype.cs:110 AddComponent | CombatAudio.Awake (:52-53) | HUD Build (Inspector.cs:83) |

### T6 / T7 / T8 / T9 / T10 / T11 / T12 / T14 / T15 / T16

See findings 10-22 and 47.

---

## 5. Refactoring hazards (summary list for the refactor bundles)

1. Reflection surface. About 32 type names and about 180 member names, including private fields, are used by tests (finding 2). The type list is in §2 finding 2. Treat **all** of them like REFACTORING_RULES.md:11 names.
2. GameObject names created at runtime and found by tests (§0 C3). They include "OriginalMotion" and "OriginalIdle" (created by PlayerMotion, *not* by the same-named classes), "VerticalWalk", "LeftFoot", "RightFoot", "OriginalWalk", "PlayerWalk", "CombatHUD", "BottomControls", "TowerInstallPanel", "TopStatus", "Status" (the invisible text), "PlayerGrowthPanel", "PlayerSpecTab", "PlayerBuyTab", "LabKind_Single", "HitDamage", "Tower_Single_1", "TowerGlow", "SelectionInfo/Title" and "SoundVolume".
3. Prefab child names that runtime looks up: "2D Sprite", "그림자", "바깥 그림자", "접촉 그림자", "바닥 밀착선", "Fixed", "Moving", "Housing*", and the sprite names "core", "SingleBolt" (CombatAnimation.cs:40), and `*Shadow*`.
4. Order dependencies: the order of the Configure calls in Prototype.Start (finding 32); `DefaultExecutionOrder` 100 / 200 / 450 / 500 / 600 / 600 (finding 6); the ambient `HitCritical` contract (finding 31).
5. The `MaskOutputBatch` static constructor is the Play-mode flush driver (finding 1).
6. `TowerCombatStats` is the test assembly anchor (finding 29).
7. Namespace rule mismatch (finding 36).

## 6. Shared root-cause consolidation

- **RC-A — Presentation lives in Assembly-CSharp.** This causes findings 2 and 36 and the string-name hazards in §5 items 1 and 2. One fix covers all of them: give Presentation and Bootstrap their own asmdef and let the tests reference it. That turns the reflection strings into compile-checked references. This is a tooling change and must precede any rename or namespace bundle (REFACTORING_RULES.md:48 bundle ① 도구 설정).
- **RC-B — One shared "native gameplay camera" predicate.** It covers findings 4 and 3 (contact crop) and part of 19. It is safe only with the pixel-comparison tests, because the conditions differ on purpose.
- **RC-C — One owner for "selection-overlay union instances" (Rage and Upgrade).** It covers finding 6 and the RageOutline / UpgradeFeedback duplicates in finding 18.
- **RC-D — Make Session the only source of rule values the HUD shows.** It covers findings 11, 12, 13 and 14. Expose read-only query methods (for example, effective buff and affinity multiplier) instead of re-deriving them in the HUD. This is behaviour-sensitive: the rounding in finding 12.
- **RC-E — Move presentation concerns out of the Domain assembly.** It covers findings 37 and 13 (Domain name tables), and part of 35.
- Repair order: RC-A, then everything else. Without RC-A, every other bundle risks breaking tests at runtime silently.

## 7. Negative results (checked and clean)

- **Domain never references Presentation at compile time.** This is guaranteed by the asmdef. Fortress.Game.asmdef has `references: []`; there are no Presentation type names inside Runtime/Domain apart from comments.
- **Presentation and Bootstrap never write into Domain mutable fields or collections.** Checked by grep for assignments to Stats, Spent, Stars, Position and Augments counters, and for Towers/DamageTotals Add/Remove/Clear, under Runtime/Presentation and Runtime/Bootstrap.
- **All 5 runtime subscribers to Session events unsubscribe on destroy or disable,** and `Session.Dispose` nulls every event (SessionModels.cs:131).
- **No UnityEvent `m_MethodName`, `SendMessage` or string `Invoke` carriers** exist in scenes or prefabs.
- **Shader names and the `_PascalCase` property naming are consistent.** All 12 shaders are loaded by name through `Resources.Load` from code; no `.mat` file references them.
- **No BOM** in any .cs file.
- **The `PrototypeGridMovement`, `SpriteBatchCompat` and `PerfOverlay` liveness carriers are confirmed** (C1 and C4).

## 8. Verification data notes

- **Finding 12 (rounding):** the current augment values (22001=8, 22019=20, 22036=1) are integers, so `TowerBuffPercent==TowerDamagePercent` today. Only a non-integer card value would expose the HUD/aura mismatch.
- **Finding 11 (placement):** the outcome depends on whether the `enemyPath` tilemap tiles exactly match `routeCells` (Prototype.cs:19) plus the flying route. This was not checked against the tile data; that belongs to perspective C.
- **Finding 30 (cues):** audio consumption of the crit, flame_shot, rail_shot, cold_pulse, freeze and reroll cues depends on `Assets/Audio/Resources/CoreGuardian/Audio/SelectedB/catalog.json` (ASSET_LIST line 1209), which is not in the copy.
- **D29 and the Manifest-based MaskAtlas:** the carrier assets are listed in ASSET_LIST but are absent from the copy.

---
Disposition (self-check before handoff; the independent final review is not part of this perspective): cross-reconciliation merged/deleted 2 (contact-path entries merged into finding 3; RageOutline/UpgradeFeedback duplicates merged into finding 6); seven checks deleted 1 (an editor "fake-null GetComponent" crash claim, withdrawn as unverified authority) and downgraded 1 (finding 12 → P3, with a data note); opposing-case deleted or downgraded 0 (no P0/P1); severity corrected 1; file inventory added 3 (HudSkin, Abilities, HandleReaction re-read; findings 28 and 23).
