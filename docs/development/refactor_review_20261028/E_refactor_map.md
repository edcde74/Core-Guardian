# Perspective E: refactoring map and hot-path allocation/complexity inventory

Scope: every `.cs` under `Assets/Scripts/Runtime/**` (68 files) and `Assets/Scripts/Editor/**` (5 files), plus the 12 shaders in `Assets/Art/Rendering/Shaders/Resources/CoreGuardian/` and the 2 test fixture shaders. That is 83 runtime/editor/shader files, all read line by line. Tests were read only where they constrain a refactor (reflection, name lookups).
All paths below are relative to `Assets/Scripts/` unless they start with `Art/`. Line numbers are from the source copy at `/home/claude/review/src`.
Finding format: `P-level | conclusion | file:line | trigger path / evidence`.

---

## 0. Enumerated review objects

**Per-frame entry points (combat)** (file:line):
- `Bootstrap/CoreGuardianPrototype.cs:138` Update. It calls `Session.Tick` (`Domain/Rules/Session.cs:417`) and then `SynchronizeViews` (`:419`).
- Each `SynchronizeViews` call reaches `CombatAnimation.Enemy/Tower/Player/ProjectilePosition` (`Characters/CoreGuardianCombatAnimation.cs:53,73,88,167`).
- Update/LateUpdate methods: `Bootstrap/PrototypeGridMovement.cs:47`, `UI/CoreGuardianHud.cs:501,465`, `UI/CoreGuardianHudSkin.cs:72` (CoreGuardianHudReaction, one per button), `UI/CoreGuardianHudHandleReaction.cs:29`, `UI/CoreGuardianAugmentationView.cs:66`, `UI/CoreGuardianPerfOverlay.cs:91`, `Combat/CoreGuardianCombatArt.cs:100`, `Combat/CoreGuardianEffectsView.cs:97`, `Audio/CoreGuardianCombatAudio.cs:89`, `Audio/CoreGuardianMusicAudio.cs:35`.
- One component per unit (so the count scales with units): `Characters/CoreGuardianUnitAnimation.cs:125`, `Rendering/CoreGuardianPhotoShadow.cs:75`, `World/ArtDepthSorting.cs:24` (on 21 prefabs).
- Rendering LateUpdates: `Rendering/CoreGuardianSelectionOutline.cs:59`, `Rendering/CoreGuardianRageOutline.cs:38`, `Rendering/CoreGuardianUpgradeFeedback.cs:41`, `Rendering/CoreGuardianObjectEmphasis.cs:160`.
- Render-pipeline callbacks: `Rendering/CoreGuardianObjectEmphasis.cs:114`, `Rendering/CoreGuardianMaskOutputBatch.cs:67-68`, `Rendering/CoreGuardianMaskPage.cs:74`.
- Legacy Updates (not mounted in the scene or any prefab; GUID search returned 0 hits): `Domain/Towers/TowerTargeting.cs:12`, `Domain/Towers/BeanProjectile.cs:18`, `Domain/Enemies/EnemyController.cs:43`.

**Per-event entry points (high frequency)**:
- `Session.DamageApplied` (fired at `Session.Effects.cs:221`). Subscribers: `Hud.cs:115`, `CombatArt.cs:30`, `EffectsView.cs:26`, `CombatAnimation.cs:42`, `CombatAudio.cs:83`.
- `ProjectileLaunched` (fired at `Session.cs:794`). Subscribers: `CombatAnimation.cs:41`, `CombatAudio.cs:83`.
- `AttackLaunched` (`Session.cs:715,803`), `Cue` (`Session.Effects.cs:25`), `TowerPulsed` (`Session.cs:712`), `game.Changed` (`Prototype.cs:418`, subscribed by `Hud.cs:115`).

**Big files covered by the split plan**: `Session.cs` (857 lines), `Session.Effects.cs` (485), `CombatData.cs` (452), `CoreGuardianPrototype.cs` (638), `CoreGuardianHud*.cs` (598+402+209+92+48+48), `CoreGuardianSelectionOutline.cs` (503), `CoreGuardianSharedOutline.cs` (345), `CoreGuardianPerfOverlay.cs` (378), `CoreGuardianOriginalWalk.cs` (306).

**Shaders**: BatchedMaskOutput, BatchedSelectionMask, BodyHitFlash, CoreAmbient, PhotoShadowContact, SelectionMask, SelectionOutline, SharedOutlineMask, SharedOutlineOutput, TowerBaseShade, TowerGlow, UpgradeScan; fixtures SelectionOutlineCandidate and SelectionOutlineReference.

---

## 1. Per-file table

Column key:
- **L**: line count.
- **>120**: number of lines longer than 120 characters.
- **Magic**: count of numeric literals other than 0, 1, 2 and const declarations (rough), followed by notable examples.
- **Nest>3**: places with more than 3 nesting levels. Braceless inline nesting is counted.

The project rules are "function ≤50 lines, ≤4 params, ≤3 nesting, ≤120 chars". REFACTORING_RULES.md:24-26 and :37 set these limits.

### Domain

| Path | L | Responsibility (one sentence) | >1 responsibility? | Functions >50 lines | >4 params | Nest>3 | >120 | Magic | Naming problems |
|---|---|---|---|---|---|---|---|---|---|
| Runtime/Domain/Rules/Session.cs | 857 | Runs one combat session. | Yes: board/path, tower install/merge/upgrade, wave spawning, step loop, buff auras, targeting, shooting, projectile flight, round completion, augment choice, view-side target cache | StepTick 424-502 (79), Shoot 723-810 (88); ctor :258 is one ~700-char line | TryCollect:131(5), Pulse:696(5), Shoot:723(**12**) | Shoot try/while/for/for/if (5); TryCollect :142-144 (5); StepTick :455-464 (4) | 120 | 103: .05f substep :420; .00001f ×6 (:434,472,516,584,706,768); 512 :204; 256 :248; 6 (="Tower:".Length) :200-203; 1048576 :268; 3 multi targets :166,763-765; seed 31/7 :258; augment ids 12/13/16/23/30/34/41/45/47 etc.; setting fallbacks 50 :307, 3.2/1.8/.5 :326-327, 60/30/25 :628, 30 :615, .2 :433 | one-letter locals t/u/s/e/p; `Sides` PascalCase vs `rangeEntryOrder` camelCase for private statics; `Step()` vs `StepTick()`; no unit suffix (`PreparationRemaining`, `Range`) |
| Runtime/Domain/Rules/Session.Effects.cs | 485 | Status effects and the damage pipeline. | Yes: cue queue, tag cache, flying route, slow/burn/stun, damage/kill, chain, spawns, enemy abilities, fire cells (aug 22), area/teleport blast, crit | – (TickAbilities 41, OnKilled 38) | AddBurn:95(5), DealDamage:211(5) | TickAbilities :373-385 (4) | 41 | 116: tag fallbacks :33-38; 20 :75; .001f :83-84; .5f :144,365; .3f :253,383; 3 :267; `<<20 ^0xFFFFF` :315,328; 256 :19 | e/h/o locals; magic source strings "Player","PlayerSkill","Burn","Shatter" |
| Runtime/Domain/Rules/SessionModels.cs | 136 | Model-pool plumbing for Session (rent/retire/guards/Dispose). | Mostly single (debug hooks mixed in) | – | RentProjectile:69(5), DebugProjectile:105(5) | – | 24 | 3: pool caps 4/32 :15, 256 :16 | – |
| Runtime/Domain/Rules/Session.Debug.cs | 100 | Developer cheats. | Single | – | – | – | 11 | 7: 4 passes :22, 1e9f :25, 112 :68, .35f :84 | `DebugEnemyName` duplicates the HUD's Korean name tables |
| Runtime/Domain/Rules/ModelPooling.cs | 164 | Slot/handle/snapshot value types plus ModelList/ModelPool. | Yes: handle types, snapshots, HitPayload, list, pool | – | ModelPoolCounters ctor(5) | – | 25 | 7 | `Read`/`Write` private properties; :28 fully qualifies `System.Collections.Generic.List` although the using is present |
| Runtime/Domain/Rules/CombatData.cs | 452 | Combat data tables and their CSV loading. | Yes: 3 enums, 7 spec DTOs, CombatStats, CSV parser (legacy + numeric), validation, CombatRules, TargetOrder, TowerState | LoadTables 256-343 (88) | Load:131(5), Load:135(9), LoadLegacy(5), LoadTables(9), WaveEnemy(5) | SplitCsv :236 | **115** | 71: ID ranges :261-370 (42000, 10000, 11000/11101/11201, 12000/12101, 10001, 21000, 41000000, 31000, 32000, 22001-22050); 50/18/20/12 :375; RagePercent 50 :26; RageBoost 30 :27; AugmentationEvery 10 :83,172 | one-letter methods `F`,`I`,`S`,`B`; generic `Kind<T>` shadows field name `Kind`; locals d/et/tt/ut/wt/b |
| Runtime/Domain/Rules/Augmentations.cs | 111 | Card face, deck draw and the stacked effects accumulator. | 3 types (acceptable) | – | Draw:29(**9**) | Draw :38-46 (4) | 7 | 37: 51/3 arrays :66-67; 60/30/25 defaults duplicated with Session.cs:628; 3 cards :109 | numbered counters `Rounds12/18/32/34`; `RoundsSince12` is declared and never read (:75) |
| Runtime/Domain/Rules/SegmentedEnemyHealthBar.cs | 65 | Generates segmented HP sprites. | Single, but it is a MonoBehaviour inside Domain/Rules (wrong layer and folder) | – | – | – | 3 | 29: 12/8 :20, 100 key :33, 4/5 px :35, colours :38-39, 64 ppu :46 | – |
| Runtime/Domain/Common/Health.cs, Enemies/{EnemyController,EnemyDefinition,WaypointPath}.cs, Towers/{BeanProjectile,LightningTower,TowerCombatStats,TowerTargeting}.cs | 38/72/26/17/44/17/21/18 | Legacy prototype components. Not mounted: GUIDs appear in 0 scenes/prefabs; referenced only by tests. | Single each | – | EnemyDefinition.Configure(5) | – | 0-3 | small: .001f, .05f, 1.2f/1.25f, .12f, 50 | global namespace and Allman/spaced style, unlike the rest of the code |

### Bootstrap

| Path | L | Responsibility | >1 responsibility? | Functions >50 lines | >4 params | Nest>3 | >120 | Magic | Naming |
|---|---|---|---|---|---|---|---|---|---|
| Runtime/Bootstrap/CoreGuardianPrototype.cs | 638 | Scene composition root for the combat prototype. | Yes: bootstrap, per-frame input, placement/ghost/overlay, buildings, commands, test speed/pause, view sync with enemy/projectile view pools, prefab resolution through Resources, sprite generation | Update 138-193 (56); SynchronizeViews 419-467 (49) | BuildingView(5), CreateSprite(5) | ReturnRemovedEnemyBodies :473-497 (5) | 65 | 52: 2 s :148, 3 s :418, .6/.4/1.2 :221, ghost colours :177, OverlayBase+1800/1900/1500 :253,556,596, .17/.2/1.02/.88/.25 :437-463, .2/.275 :600, Ring 48/.025/.4 :611-613, 16/7.5/56 :617-619, TestSpeed 3 :413, building defaults 2/1/35 :305-307 | public lowercase fields (serialized, so names must stay); method `Diagnostic()` is a noun; locals xy/g/l/r/sr; dead `Ring()` :608 and unused `dot` sprite :61/113 |
| Runtime/Bootstrap/PrototypeGridMovement.cs | 132 | Grid stepping and teleport for the player. | Single | – | – | – | 3 | 1 | clean (Allman) |

### Presentation/Audio

| Path | L | Responsibility | >1? | >50 | >4p | Nest | >120 | Magic | Naming |
|---|---|---|---|---|---|---|---|---|---|
| Audio/CoreGuardianAudioCatalog.cs | 56 | JSON cue catalog plus cue DTO. | 2 types | – | – | – | 1 | 1 | public lowercase serialized JSON fields (id, category, resourcePath, gain, priority, important, protectCore). These are a contract, so keep them. |
| Audio/CoreGuardianCombatAudio.cs | 218 | Bounded SFX voice pool driven by Session events. | Yes: event routing, rate limiting, voice stealing, music child creation, PlayerPrefs.Save | – | – | – | 12 | 11: .035/.08/.12/.2/.1/.3/.8 | 14 `*ResourcePath` consts :10-23 are unused at runtime but **reflected by tests**; fully qualified `System.Collections.Generic` |
| Audio/CoreGuardianHud.Sound.cs | 48 | HUD sound panel (a partial of CoreGuardianHud). | Single | – | CompactSoundButton(6) | – | 9 | 79 (layout) | file sits in Audio/ while its partial siblings are in UI/ |
| Audio/CoreGuardianMusicAudio.cs | 54 | Two-voice music crossfade. | Single | – | – | – | 3 | 3 | – |
| Audio/CoreGuardianSoundSettings.cs | 42 | PlayerPrefs volume settings. | Single | – | – | – | 1 | 2 (.65 ×3) | – |

### Presentation/Characters

| Path | L | Responsibility | >1? | >50 | >4p | Nest | >120 | Magic | Naming |
|---|---|---|---|---|---|---|---|---|---|
| Characters/CoreGuardianCombatAnimation.cs | 182 | Bridges Session events and view state to unit animations. | Yes: enemy/tower/player animation drive, launch offsets, muzzle flash pool, rage trigger | – | – | flashes loop :111-121 | 13 | 11: .25f aim height ×4 (also Prototype:463); .07 ×2 | `Flash` public lowercase fields |
| Characters/CoreGuardianFrameWalk.cs | 59 | Whole-frame walk cycles. | Single | – | ctor(7) | – | 9 | 5 | – |
| Characters/CoreGuardianOriginalIdle.cs | 30 | Player breathing layer. | Single | – | – | – | 1 | 3 (2.8, .55) | – |
| Characters/CoreGuardianOriginalMotion.cs | 220 | Tower and player cutout attack/aim rigs. | Yes: rig build, Single barrel, turret cut, recoil, muzzle point | – (Present 46, ctor 43) | CutPart(5) | – | 19 | 82 (approved pixel coordinates :15,28-29,56-58,148,152-172,209-212) | `h`; `Turret` is a bool property named like a noun; role strings compared repeatedly |
| Characters/CoreGuardianOriginalWalk.cs | 306 | Enemy side-walk IK and vertical frames. | 2 classes (VerticalLegPair nested) with static sprite caches | ctor 55-105 (51) | Bone(**8**) | ctor/VLP ctor (4) | 31 | **240** (joint tables :15-19, :73-80, :234-262) | Leg public lowercase fields; `lu/ll/ru/rl`; local named `transform` :175; broken indentation :72-81 |
| Characters/CoreGuardianPlayerMotion.cs | 105 | 8-direction player cutouts, gait and arm aim. | Single | – | Present(6) | – | 8 | 48 | Pose public lowercase fields |
| Characters/CoreGuardianUnitAnimation.cs | 137 | Per-unit animation facade. | Yes: role dispatch, facing, fire, hit flash, glow, grounding | – | – | – | 11 | 44 (100/.25/-90/3 defaults :24-26, duplicated at :118-119) | local functions `V`,`B` |

### Presentation/Combat

| Path | L | Responsibility | >1? | >50 | >4p | Nest | >120 | Magic | Naming |
|---|---|---|---|---|---|---|---|---|---|
| Combat/CoreGuardianCombatArt.cs | 122 | Combat sprite catalog plus pooled hit/death/core effects. | Yes: catalog, projectile sprite choice, effect pool, core feedback/ambient host | – | Show(5) | – | 14 | 19 | – |
| Combat/CoreGuardianEffectsView.cs | 151 | Cue animations plus persistent status markers. | Yes: anim pool, cue mapping, per-enemy markers, tower stars, fire tiles, sticky floor | – (LateUpdate 49) | Play(6) | :116-122 inline 4 | 25 | 66 | `W()`,`Set()`,`Anim`; one-letter a/r/f/s |

### Presentation/Rendering

| Path | L | Responsibility | >1? | >50 | >4p | Nest | >120 | Magic | Naming |
|---|---|---|---|---|---|---|---|---|---|
| Rendering/CoreGuardianBodyMaskCache.cs | 245 | Shared body-mask cache. | 2 types; `CoreGuardianBodyMaskLifetime` MonoBehaviour shares the file and its name differs from the file name | – | Request ctor(6) | – | 31 | 0 | internal lowercase fields mixed with PascalCase `Sample` fields; writable static counters :22 |
| Rendering/CoreGuardianCoreAmbient.cs | 95 | Core glow quad plus particles. | Single | – (ctor 39) | – | – | 13 | 32 (particle params :45-53) | – |
| Rendering/CoreGuardianCoreFeedback.cs | 60 | Core hit flash plus HUD tint. | Single | – | – | – | 4 | 7 | – |
| Rendering/CoreGuardianCoreParticleSettings.cs | 53 | Inspector particle settings. | Single | – | – | – | 2 | 9 | serialized lowercase fields (keep) |
| Rendering/CoreGuardianHierarchyWatch.cs | 16 | Hierarchy version stamp. | Single | – | – | – | 0 | 0 | – |
| Rendering/CoreGuardianHitFlash.cs | 100 | Body hit flash and ramp recolor. | Single | – | – | – | 5 | 0 | – |
| Rendering/CoreGuardianMaskAtlas.cs | 49 | Mask page lookup asset (ScriptableObject). | Single | – | – | – | 2 | 0 | serialized Entry fields (keep) |
| Rendering/CoreGuardianMaskOutputBatch.cs | 224 | Batches selection-mask output quads per camera. | Yes: register, group, overlap proof, proxy mesh, restore, prune | Flush 90-155 (66) | Register(6) | Flush :115-138 (4) | 34 | 8 | public struct `OutputParameters` with lowercase public fields |
| Rendering/CoreGuardianMaskPage.cs | 115 | GPU staging-page allocator. | Single | – | Queue(9), QueueShape(9), Slot ctor(6), TryAcquire(5) | – | 12 | 0 | – |
| Rendering/CoreGuardianMaskProducerBatch.cs | 153 | Batched GPU mask producer. | Single | – | Queue(9), QueueShape(**10**) | – | 29 | 5 (160 stride, uv channel 5) | ReleaseAll :152 is one 563-char line; `g` reused as foreach and for variable |
| Rendering/CoreGuardianObjectEmphasis.cs | 241 | Always-on outline plus body brightness for all bodies. | Yes: target discovery, twin-material cache, tint pass, outline dispatch | – | – | PresentTargets (4) | 17 | 13 | Tint/Body public lowercase fields; empty `EndCameraRendering` handler |
| Rendering/CoreGuardianPhotoShadow.cs | 80 | Per-unit floor shadow. | Single | – | – | – | 5 | 8 | – |
| Rendering/CoreGuardianRageOutline.cs | 61 | Boss rage outline flash. | Single | – | – | – | 3 | 4 | – |
| Rendering/CoreGuardianSelectionOutline.cs | 503 | Union-mask outline presentation. | Yes: **3 types** in the file (SelectionOutline, SharedMaskMaterial, SpriteReferences); source collection, screen crop, cache lease, page queue, legacy draw, output quad, batch registration, resources | PresentMask 79-184 (**106**), DrawMaskParts 325-381 (57) | PresentMask(6), PresentAtOrderWithSources(5) | PresentMask, SpriteReferences.Read :496 | **51** | 57 (1920/1080, 8.5/4.25, 32 repeated ×3) | MaskGeometry/MaskBatch public lowercase fields; one-letter a/b/c/d/w/h/u/v |
| Rendering/CoreGuardianSharedOutline.cs | 345 | Shared screen-mask outline for emphasis bodies. | Yes: collect, sort, batched/fallback record, layout cache, output quads, resources | End 167-217 (51) | Begin(5) | – | 33 | 10 | Part/Entry/PartItem public lowercase fields; `public static bool ReuseGeometry` is a mutable public static |
| Rendering/CoreGuardianSpriteAtlasSources.cs | 57 | Sprite to source-texture mapping. | Single | – | – | – | 7 | 0 | – |
| Rendering/CoreGuardianSpriteBatchCompat.cs | 65 | One-pass scene colour and constant compatibility fix. | Single | – | – | – | 5 | 0 | – |
| Rendering/CoreGuardianSpriteBlocks.cs | 27 | Custom property-block probe (used only by the Editor RouteMeshBuilder). | Single | – | – | – | 1 | 0 | – |
| Rendering/CoreGuardianSpriteLibrary.cs | 44 | Name to sprite lookup asset (ScriptableObject). | Single | – | – | – | 3 | 1 (64) | – |
| Rendering/CoreGuardianTowerGlow.cs | 73 | Tower glow overlay meshes. | Single | – | – | – | 8 | 15 | – |
| Rendering/CoreGuardianTowerGrounding.cs | 70 | Tower shadow placement plus base shade. | Single | – | SetShadow(5) | – | 10 | 25 | local `transform` is a Vector4 :47 |
| Rendering/CoreGuardianUpgradeFeedback.cs | 72 | Upgrade scan overlay. | Single | – | – | – | 4 | 10 | – |

### Presentation/UI and World

| Path | L | Responsibility | >1? | >50 | >4p | Nest | >120 | Magic | Naming |
|---|---|---|---|---|---|---|---|---|---|
| UI/CoreGuardianHud.cs | 598 | Combat HUD core. | Yes: UI factory, top status, bottom controls, augmentation cards, install panel, damage totals, floating-number pools and animation, placement focus, refresh scheduler, text formatters | – (Build 48, Refresh 41) | Box(6), Text(5), Button(**7**), UpgradeLabel(5), FloatingNumber(5) | – | **99** | **287** (layout) | HUD types fully qualified as `UnityEngine.UI.Text`, while Redesign uses `using UnityEngine.UI` (inconsistent across partials) |
| UI/CoreGuardianHud.Redesign.cs | 402 | Selection-info panel, augment strip and blast button. | Yes, plus a second MonoBehaviour `CoreGuardianHoverHint` whose name differs from the file | RefreshSelectionInfo 74-149 (76) | MakeAugmentSlot(5) | – | 64 | 176 | method named `H5()` |
| UI/CoreGuardianHud.Inspector.cs | 209 | Bench/lab panels and developer menu host. | Yes | – | – | – | 34 | 101 | dead `ReleaseInspection` :44 |
| UI/CoreGuardianHud.Abilities.cs | 92 | Ability plate. | Single | – | – | – | 10 | 53 | dead field `towerAbilities` :9; `abilityKeys` camelCase vs `LabStats`/`UpgradeOrder` PascalCase |
| UI/CoreGuardianHud.Cheats.cs | 48 | Developer cheat panel. | Single | – | – | – | 20 | 24 | one-letter consts `W`,`H` |
| UI/CoreGuardianAugmentationView.cs | 71 | Augment card flip/reveal. | Single | – | Label(6) | – | 20 | 61 | – |
| UI/CoreGuardianHudHandleReaction.cs | 37 | Fold-grip hover decoration. | Single | – | – | – | 6 | 15 | – |
| UI/CoreGuardianHudSkin.cs | 97 | HUD sprite skin, factories and symbol map. | 2 types: `CoreGuardianHudReaction` MonoBehaviour shares the file and its name differs from the file name | – | – | – | 8 | 16 | stray CJK literal `"接기"` next to `"접기"` :45 |
| UI/CoreGuardianPerfOverlay.cs | 378 | Shipping performance overlay plus CSV recorder. | Yes: install, cmdline flags, sampling, overlay UI, CSV, summary, uncapped mode | – (Sample 48) | Window.Add(5), ReadGame(5) | – | 28 | 37 | `Inv` abbreviation |
| World/ArtDepthSorting.cs | 30 | Ground-contact sorting order. | Single | – | – | – | 2 | 0 | spaced style (differs) |
| World/CoreGuardianPlacementOverlay.cs | 68 | Install tint mesh. | Single | – | – | – | 7 | 12 | – |
| World/CoreGuardianRouteMeshBake.cs | 19 | Serialized route bake state. | Single | – | – | – | 0 | 0 | public serialized arrays (keep) |

### Editor

| Path | L | Responsibility | >1? | >50 | >4p | Nest | >120 | Magic | Naming |
|---|---|---|---|---|---|---|---|---|---|
| Editor/Content/CoreGuardianContentAssets.cs | 64 | Menu: build content prefabs and sprite library. | Single | – | – | – | 4 | 0 | – |
| Editor/ProjectWelcome/Welcome2DScript.cs | 13 | Unity template. | Single | – | – | – | 0 | 0 | – |
| Editor/Rendering/CoreGuardianSpriteAtlasPipeline.cs | 124 | Prebuild atlas sync plus manifest. | 2 types (plus an AssetPostprocessor) | Synchronize 30-85 (56) | – | – | 30 | 3 | – |
| Editor/Rendering/MaskAtlasBuilder.cs | 116 | Mask array bake. | 2 types; file name ≠ class `CoreGuardianMaskAtlasBuilder` | Build 10-87 (78) | – | Build :44-67 (**6**) | 13 | 6 | – |
| Editor/Rendering/RouteMeshBuilder.cs | 157 | Road sprite to mesh combine. | Single; file name ≠ class `CoreGuardianRouteMeshBuilder` | CombineKeeping 66-133 (68) | – | (5) | 23 | 2 | – |

Cross-file duplicates:
- `EnsureFolder` exists 3 times: SpriteAtlasPipeline:107, MaskAtlasBuilder:88, RouteMeshBuilder:152.
- The "neutral sprite constants" MaterialPropertyBlock setup exists 3 times: SpriteBatchCompat:34-37, PlacementOverlay:36-37, RouteMeshBake:13-16.
- The "native QA camera" predicate exists 3 times: SelectionOutline:122-129, :235-238, :248-251.
- The in-range collection block exists 3 times: Session.cs:38-39, :703-704, :729-730.
- Area blast and teleport blast bodies are duplicated: Session.Effects.cs:419-442 and :445-468.
- `SplitChildSpec` and `SummonSpec` share a cache prologue: Session.Effects.cs:314, :327.
- Effects.cs:400 creates a new direction array that duplicates `Session.Sides` (Session.cs:310), in the same order.
- The recoil "kick" formula appears twice: OriginalMotion:147-148 and :177-178.
- Session.cs builds `CombatStats` from `TowerSpec` twice: :296 and :350.
- MaskOutputBatch creates its material twice (:120-124, :142-146) and disposes proxies twice (:219, :222).
- Hud.Redesign:150-151 repeats the 1.3f/.7f affinity fallbacks from Session.Effects.cs:38.

---

## 2. Split plan for big files

General rules that make these splits behaviour-preserving:
- Use `partial` and keep every type in its namespace (global or `CoreGuardian`).
- Each MonoBehaviour must stay in the file whose name matches the class, with the same `.meta` GUID. Extra partial files get new GUIDs, which is safe because only the main file is a MonoScript.
- Any member reached by test reflection, a string name, or a serialized field keeps its name. It moves only into another **partial file of the same class**, never into a new class.

### 2.1 `Domain/Rules/Session.cs` and `Session.Effects.cs` (non-MonoBehaviour, already partial)

| New partial file | Members (current lines) |
|---|---|
| Session.cs (keep) | public fields/events/properties :9-23, :64-76, ctor :258 (split into multiple statements, same order), `Tick` :417, `StepTick` :424 |
| Session.Board.cs | path state :79-81, `SetBoard` :259, `IsBuildable` :277, structures :279-281, `PositionEnemy` :403 |
| Session.Spatial.cs | `RangeEntry/RangeStamp` :85-86, `rangeEntryOrder` :90, `EnemyRangeIndex` :91-149, `StepWork/ShotWork` + pools :150-171, `BeginTowerViews/EndTowerViews/TowerTargetForView/TowerTarget` :24-63 |
| Session.Towers.cs | `FindTower/TowerFromSource/TryTowerSourceId` :172-207, `SupportCache` :208-254, install/merge/stars/clamp/destroy :282-337, upgrades/prices/`TowerKindStats` :255-257, :338-381, `TowerRatePercent` :667 |
| Session.Attack.cs | `LaunchPayload` :674, `Pulse` :696, `Shoot` :723, orders :811-812, `MostPiercedTarget/PiercedCount/CompareInitialTarget` :815-841, `ApplyDamage` :842 |
| Session.Projectiles.cs | `MoveProjectiles` :503, `FlushProjectileRemovals` :534, `ProjectileHit` :550, `MoveLine` + `lineHits` :554-591 |
| Session.Rounds.cs | `StartCombat` :382, `StartDiagnosticEncounter` :392, `DebugSpawn` :402, `CompleteRound` :592, reroll/choose/draw/ApplyImmediate :621-665 |
| Effects to Session.Cues.cs / Session.Tags.cs / Session.FlyingRoute.cs / Session.Status.cs (slow, burn, stun, shatter) / Session.Damage.cs (Strike, DealDamage, CheckShield, OnKilled, Nearest, HitWithChain) / Session.Spawns.cs (PendingSpawn, specs, FlushSpawns, TickAbilities) / Session.Skills.cs (fire cells, blasts) / Session.Crit.cs | by section headers already present in Session.Effects.cs (:41-483) |

Possible in-method extractions (each needs the same order of side effects and RNG calls):
- Shoot → `CollectInRange` (also usable by Pulse and TowerTarget), `ResolveTarget` (:735-751), `SelectMultiTargets` (:754-767), `FireVolley` (:768-807).
- StepTick → `SpawnWave` (:432-434), `TickStatuses` (:438-448), `ApplyBuffAuras` (:450-467), `MoveEnemies` (:468-473), `FireTowers` (:479-488), `IsRoundComplete` (:493-498).
- One shared `Blast(position, damage, payload)` for the two blast methods. `UseAreaBlast` must keep setting the cooldown **before** the damage loop (:425).

Hazards:
- **Test reflection on Session.** Tests read the private field `"path"` (PlayMode/CoreGuardianAnimationPlayModeTests.cs:47,99; HitFlashPlayModeTests.cs:30). Tests also call `SetValue` on the private-set auto-properties `CoreEnergy`, `Wave`, `Gold`, `Crystals` (CombatAudioPlayModeTests.cs:45,72,148,160; SelectedAudioPlayModeTests.cs:75-145). These must stay auto-properties with setters under the same names. Tests also use `GetEvent("DamageApplied")` (EditMode AugmentationTests.cs:71,91; SessionTests.cs:38).
- **Callback re-entrancy points.** After every `onProjectile`, `onAttack` and `DamageApplied` invoke there is a `rangeIndex.Invalidate()`/`supportCache.Dirty=true` plus `if(disposed)return;` check (Session.cs:715-716, 793-805, 478, 487, 510, 580). Extracted helpers must return a "disposed" signal and stop at the same points.
- **RNG order.** `combatRandom.NextDouble()` runs inside `RollCrit` (Effects.cs:479) once per volley/pulse/blast target, in loop order. `augmentationRandom` runs in Draw. Any reordering of loops changes outcomes.
- **Field initializers across partials.** The C# specification leaves the order of field initializers across partial declarations unspecified. No current Session initializer depends on another, but static readonly fields that depend on each other must stay together, for example `rangeEntryOrder` (:90).
- `internal` members (`PiercedCount`, `SlowFraction`, `RequireEnemyWrite`) are not visible to the test assemblies (no InternalsVisibleTo; the only asmdef is `Runtime/Domain/Fortress.Game.asmdef`). Renaming them is test-safe, but they are used across files.

### 2.2 `Domain/Rules/CombatData.cs`

Split into separate files in namespace `CoreGuardian`:
- `CombatEnums.cs`: lines :11-13. Hazard: the comment at :10 says "Appended values only". Enum ordinals index pools (SessionModels.cs:15) and the serialized `enemyArt/towerArt` arrays (Prototype.cs:522-528).
- `CombatSpecs.cs`: :14-47.
- `CombatStats.cs`: :48-64. Stat string keys are a CSV contract.
- `CombatData.cs`: :65-106 and :195. Make it `partial`.
- `CombatData.Loading.cs`: :131-194 and :256-379. `LoadTables` can split into LoadStage/LoadPlayer/LoadEnemies/LoadTowers/LoadAffinities/LoadUpgrades/LoadWaves, keeping the call order because later stages read earlier dictionaries.
- `CombatData.Csv.cs`: `F/I/Rows/Kind<T>/IsNumericTable/Table/SplitCsv/TowerMask/Unique/Reference`, :107-130 and :198-255.
- `CombatRules.cs`: :381-395.
- `TargetOrder.cs`: :399-441. Hazard: `KindCount` (:402) must be initialized before `Common` (:405), so keep both in one file in that order.
- `TowerState.cs`: :442-451.

Hazards:
- Tests do `GetType("CoreGuardian.CombatRules")`, so keep the namespace and type name.
- Exception message strings may be asserted by tests; keep them verbatim.
- `CombatData.Sheet` uses the Resources path `"CoreGuardian/"+name` (:134).

### 2.3 `Bootstrap/CoreGuardianPrototype.cs` (MonoBehaviour)

`CoreGuardianPrototype.cs` keeps the class declaration, all serialized and public fields (:13-27), `Start` (:95), and `Update` (:138) as an orchestrator. New partials:
- `.Input.cs`: keyboard block :154-162, pointer block :163-192, `PlayerHit` :195, `HandleWorldClick` :211, `OverUI` :231.
- `.Placement.cs`: `CanInstall` :238, `ChooseTower` :243, `PlacementOverlaySource` :265, `CancelInstall` :277, `Place` :286, `RebuildOccupied` :296.
- `.Buildings.cs`: :82-87 and :303-328.
- `.Commands.cs`: :329-418.
- `.Views.cs`: `EnemyBodyView` :42-55, `SynchronizeViews` :419, `ReturnRemovedEnemyBodies` :468, `RentEnemyBody` :542, `SyncRemoved` :575, `RentProjectileView` :592.
- `.Art.cs`: :506-541 and :603-620.

Hazards:
- **Reflection by tests** on fields `enemyViews`, `towerViews`, `previousCore`, `player`, `ground`, `enemyArt`, `towerArt` and on methods `SynchronizeViews`, `PlayerTile`, `World`, `CanInstall`, `Place`, `ChooseTower`, `CancelInstall`, `Diagnostic`, `Restart`, `CycleTestSpeed`, `Notify`, `DestroySelected`, `UpgradePlayer`, `UpgradeTower`, `StartCombat`, `TowerView`, `PlaceAtPlayerForTest`, `ToggleBench/ToggleLab` (through the HUD). Tests also do `GetType("CoreGuardianPrototype")` (×11).
- Serialized public field names must stay: `ground, enemyPath, coreMap, occupied, player, enemyArt, towerArt, enemiesCsv..wavesCsv`, plus `boardSize, coreCell, routeCells`.
- `Update` has early returns: at :154 (Esc consumed means no mouse handling) and at :163, :187, :191. An extracted `HandleInput()` must keep that control flow. The order Tick → SyncTestSpeed → phase/feedback → `SynchronizeViews` → input must stay.
- **Object names read by tests and code**: `"Tower_"+Kind+"_"+Id` (tests look up `"Tower_Single_1"`), `"TowerPlacementPreview"`, `"그림자"`, `"2D Sprite"`, `"CoreGuardianEnemyBodyPool"`, `"Building_TurretLab"`, `"Building_EngineerBench"`.
- Resources paths: :101, :311-312, :517, :533.

### 2.4 `CoreGuardianHud*.cs` (MonoBehaviour, 6 partial files)

`CoreGuardianHud.cs` keeps fields, `Configure`, `Update`, `LateUpdate`, `Refresh` and `OnDestroy`. New or moved partials:
- `Hud.Factory.cs`: `Box/Text/Button/Pin/SetHud*` (:90-93, :165-191, :240).
- `Hud.FloatingNumbers.cs`: :72-89, :380-500. LateUpdate can stay in the main file and call `AnimateNumbers()`.
- `Hud.DamageTotals.cs`: :27-56, :218-230, :241-275, :291-299, :546-587.
- `Hud.Install.cs`: :310-379.
- `Hud.AugmentSelection.cs`: :118-164.
- `Hud.Formatters.cs`: `StatName/StatValue/StatDisplay/UpgradeLabel/TowerName/BossName` (:276-290). Redesign's `KindName/EnemyName/EnemyFamily/AbilityText/StatusText/TowerTagText/OrderText` (:152-232) can join it.
- Redesign.cs splits into `Hud.SelectionInfo.cs` (:14-149), `Hud.AugmentStrip.cs` (:235-337), `Hud.BlastButton.cs` (:339-370) and `Hud.DamageScrollbar.cs` (:373-385).
- `CoreGuardianHoverHint` (Redesign:395) and `CoreGuardianHudReaction` (HudSkin.cs:56) each get their own file. Both are AddComponent-only today; no scene or prefab can reference a MonoBehaviour whose class name does not match its file.
- Move `Audio/CoreGuardianHud.Sound.cs` into `UI/` together with its `.meta`.

Hazards:
- **Static initializer order**: `damageTotalCompare=damageTotalComparer.Compare` (:41-43) must stay in the same partial file, after `damageTotalComparer`.
- **`refresh` list order equals build order.** The order of `Build()` calls (:192-239), `BuildDeveloperMenu` (Inspector:77) and so on fixes the order in which refresh lambdas execute (Hud.cs:523), including `SetActive` side effects. Do not reorder the Build* calls.
- Reflection by tests: fields `damageNumbers`, `bossStatus`, `Started` (field of DamageNumber); methods `ShowDamageNumber`, `ClearDamageNumbers`, `CancelInstallation`, `Refresh`, `ToggleLab/ToggleBench`; property `InspectionTarget`. Tests also do `GetType("CoreGuardianHud")`.
- **Behaviour depends on object names.** HudSkin.Symbol(name) (:25-53) and Button (:179-188) choose behaviour by `StartsWith("Install_")`, `"Preview"`, `EndsWith("SpecTab")` and similar. Code finds `"Status"`, `"PrototypeActions"`, `"Reload"`, `"TestSpeed"`, `"SoundControls"`, `"AugmentFold/HandleArt"`, `"Symbol_Core"`, `"Status_Core"`, `"Tower_"+stat`. Tests find `"CombatHUD"`, `"TopStatus"`, `"BottomControls"`, `"TowerInstallPanel"`, `"PlayerGrowthPanel"`, `"PlayerSpecTab"`, `"PlayerBuyTab"`, `"LabKind_Single"`, `"CombatHUD/SelectionInfo/Title"`, `"AugmentationSelection"`, `"Status_Wave"`, `"HitDamage"`. **Renaming any UI GameObject changes behaviour or breaks tests.**

### 2.5 `Rendering/CoreGuardianSelectionOutline.cs`

- Move `CoreGuardianSharedMaskMaterial` (:426-450) and `CoreGuardianSpriteReferences` (:454-503) into their own files. Both are internal, not reflected, and also used by ObjectEmphasis, PhotoShadow and BodyMaskCache.
- The MonoBehaviour gets partials:
  - `.Mask.cs`: PresentMask split into `CollectSources` (:87-100), `ProjectBounds` (:102-118), `ContactCrop` (:121-136), `ProduceMask` (:137-159) and `BuildOutputQuad` (:160-183).
  - `.Geometry.cs`: :267-381.
  - `.Output.cs`: :185-266, with a single `IsNativeQaCamera(camera)` replacing the 3 copies.
  - `.Resources.cs`: :382-422.

Hazards:
- Tests reflect fields `mask`, `atlas`, `maskBinding`, `lastMaskDrawCalls`, `lastAtlasMisses`, `output`, `widthPixels`, `color`, `alphaThreshold` and methods `Present`, `PresentAtOrder`, `PresentAtOrderWithSources`, `PresentGroundContactWithBand`, `PresentOverlay`, `ReleasePresentation`, `InvalidateSources`. Tests also do `GetType("CoreGuardianSelectionOutline")`. `PresentAtOrder` and `PresentGroundContact*` have **no runtime callers**; they are test-only surface, so keep them.
- The local function `Point` (:168) captures `camera` and `depth` as a struct closure (no allocation). An extracted method must take them as parameters, not as a lambda.
- Emphasis, Rage and Upgrade code writes the serialized fields `widthPixels`, `color` and `alphaThreshold` by name.

### 2.6 `Rendering/CoreGuardianSharedOutline.cs` (internal class)

Partials:
- `.cs`: state, `Supports`, `Begin`, `Add`, `End` orchestration.
- `.Mask.cs`: `RecordFallback`, `RecordBatched`, `RebuildLayout`, `Geometry`, `Eligible` (:219-310).
- `.Output.cs`: the output loop at :198-216.
- `.Resources.cs`: :311-345.

Hazards:
- `partCompare=partOrder.Compare` (:62-64) has a static initializer dependency, so keep it in one file in that order.
- The struct `PartItem` (:30) must stay byte-identical to the shader's `struct Part` (SharedOutlineMask.shader:47). The same applies to `Item` in MaskProducerBatch.cs:21-24 and BatchedSelectionMask.shader:16.
- The global shader property names `_CG*` are a contract with the shaders.

### 2.7 `UI/CoreGuardianPerfOverlay.cs`

Partials: `.Sampling.cs` (Window, Sample, ReadGame, recorders), `.Overlay.cs` (BuildUi, RefreshText), `.Recording.cs` (RoundStat, Start/Stop/WriteRow/BuildSummary).

Hazards: `[RuntimeInitializeOnLoadMethod]` `Install` and the `instance` singleton; the CSV header and column order (:279, :301-307) are read by external docs (`docs/development/perf_overlay_20261024/README.md` per AGENTS.md); the ProfilerRecorder counter names.

### 2.8 `Characters/CoreGuardianOriginalWalk.cs`

- Move the nested `VerticalLegPair` into `CoreGuardianOriginalWalk.VerticalLegs.cs` as `sealed partial class CoreGuardianOriginalWalk { sealed class VerticalLegPair … }`, so the nested type name stays the same.
- Merge the per-role bone tables (:15-19 and :73-80) into one `RoleRig` data table with exactly the same float literals.

Hazards: tests reflect the static field `boots` and the method `FootOffset`, and do `GetType("CoreGuardianOriginalWalk")`. `ClearCachedParts` is called from SpriteAtlasSources.cs:18. Child object names are looked up by tests: `"OriginalWalk"`, `"VerticalWalk"`, `"LeftFoot"`, `"RightFoot"`, `"LeftLower"`, `"Body"`, `"바닥 밀착선"`.

### 2.9 Other medium files

- `MaskOutputBatch.Flush` (66 lines) splits into `CollectGroups`, `MergeGroups` and `ApplySingles`.
- `ObjectEmphasis`: the twin-material static cache (:53-86) can move into a `.Twins.cs` partial.
- `MaskAtlasBuilder.Build` (78 lines) splits into `BakeGroup` and `ValidatePage`.
- `RouteMeshBuilder.CombineKeeping` (68 lines) splits into `ValidateSources` and `BuildGroupMesh`.

Static classes `MaskOutputBatch`, `MaskPage` and `BodyMaskCache` have explicit static constructors that subscribe to engine events. Splitting them into partials is safe because there is still one static constructor. Moving their statics into new classes is **not** safe: a class without an explicit static constructor is `beforefieldinit`, so its initialization timing would change.

---

## 3. Hot-path allocation and complexity inventory

N bounds from current data (`Data/Balance/Resources/CoreGuardian/waves.csv`, computed by script):
- Up to 120 enemies spawn in a round (wave 29), not counting splits and summons.
- The largest single kind per round is 35 (enemy 11002 in wave 29).
- Towers T ≤ ~32 (from the brief).
- Ordinary combat runs one 0.05 s sub-step per frame at 3× speed (Session.cs:420).

### 3.1 Per-frame allocations during combat

1. P2 | `OverUI` allocates a new `PointerEventData` (a reference type) on every call, and it is called every frame | Bootstrap/CoreGuardianPrototype.cs:235 | Update:167 calls `OverUI(mouse)` every frame when Mouse and Camera exist. There are extra calls at :171 (while installing), :184 (press) and :189 (release). That is 1-3 allocations per frame.
2. P2 | `PresentTargets(IEnumerable<Transform>)` iterates a `List<Transform>` through the interface, so the List enumerator is boxed every frame | Rendering/CoreGuardianObjectEmphasis.cs:171,180 | LateUpdate:164-169 passes `targets` (a List) every frame; one allocation per frame.
3. P2 | The blast cooldown label allocates `Mathf.CeilToInt(cd).ToString()` every frame while the blast is cooling down | UI/CoreGuardianHud.Redesign.cs:364 | Hud.Update:501 → `UpdateRedesignFrame`:368 → `UpdateBlastCooldown` runs each frame. `SetHudText` compares only after the string has been built.
4. P3 | `FindObjectsByType<SpriteRenderer>()` allocates an array every frame for as long as the core has not been found | Rendering/CoreGuardianObjectEmphasis.cs:165-167 | Conditional: it stops once `core` is set. The loop never ends only if no `"2D Sprite"` with sprite `"core"` exists in the scene.
5. P3 | While recording, the perf recorder allocates every frame and once per second: list growth and per-second `phase.ToString()`, `ToString(fmt)` calls, `sb.ToString()` and a synchronous `csv.Flush()`. The visible overlay allocates about 15 strings every 0.5 s. This pollutes its own "GC KB/frame" readout | UI/CoreGuardianPerfOverlay.cs:138, 199-222, 294-310 | F3/F4 only; otherwise one keyboard poll per frame (:93-100).

### 3.2 Per-refresh allocations (HUD runs at 5 Hz from Hud.cs:501, plus on every `game.Changed`/Notify)

6. P2 | Every refresh rebuilds about 66 button labels, whether or not their panels are visible | UI/CoreGuardianHud.cs:190 | Each `Button()` adds a lambda that calls `label()` (:190). Examples:
   - 8 lab buttons (Inspector.cs:121), each calling `TowerKindStats` (Session.cs:348-353: a new `CombatStats` + Dictionary + `kind.ToString()` per upgrade row), `labKind.ToString()`, `TowerKindUpgradePrice` (`kind.ToString()`, Session.cs:346) and `UpgradeLabel` (a ~15-part concat with `ToString("0.##")`, Hud.cs:282-286).
   - 4 bench buttons (Inspector.cs:104).
   - 28 developer cheat labels (Cheats.cs:17-39). `DebugAugmentName` uses LINQ with a capturing closure (Session.Debug.cs:59).
   - Install labels call `Towers.Any` (Hud.cs:343,345).
7. P2 | Top status, centre and boss texts are re-concatenated every refresh, and `status` is invisible | UI/CoreGuardianHud.cs:509, 512, 513 | `status.color=Color.clear` (:195), yet the 12-part string is still built. `LivingEnemyCount` is O(E) (:544).
8. P2 | Damage-total rows allocate on every changed refresh, which in combat is every refresh | UI/CoreGuardianHud.cs:272-273, 294-296, 562, 565 | `DamageRole`/`SourceName` use culture-sensitive `StartsWith` + `Substring` + `FirstOrDefault` with a capturing closure, plus concat. `row.Root.name` is a getter allocation. `pair.Value.ToString()`. That is up to ~33 rows × 5 allocations per refresh.
9. P2 | The selection-info panel builds 8 rows of concat strings plus LINQ closures and temporary List/HashSet/Join every refresh while it is open | UI/CoreGuardianHud.Redesign.cs:92-140, 113, 119-120, 123, 140, 152-232 | Called from RefreshInspectors (Inspector.cs:206) every refresh.
10. P2 | The augment strip builds sprite-key strings per slot per refresh | UI/CoreGuardianHud.Redesign.cs:249-250, 268-270, 313 | `grade.ToString().ToLowerInvariant()` + concat, `Number.ToString("00")` + concat and `"×"+count`, for 6 slots, once any augment is owned (RefreshAugmentStrip from Inspector.cs:207).
11. P3 | RefreshInspectors concatenates 3 titles, builds `"Install_"+labKind` (an enum ToString), counts with LINQ and then runs `HudSkin.Symbol` (~45 string compares, culture-sensitive StartsWith/EndsWith, `Substring`) on every refresh | UI/CoreGuardianHud.Inspector.cs:195-200; UI/CoreGuardianHudSkin.cs:25-53 | every refresh.
12. P3 | The volume label is concatenated every refresh even while the developer menu is hidden | Audio/CoreGuardianHud.Sound.cs:36 | refresh lambda.
13. P3 | During placement, each refresh reads `root.name` 4 times per canvas child (a getter allocation) | UI/CoreGuardianHud.cs:369-371 | Preparation phase with installing active.
14. P3 | The augmentation-card signature runs `string.Join` + `Select` every refresh | UI/CoreGuardianHud.cs:149 | augmentation phase only.
15. P2 | Every core-damage frame triggers an extra full HUD refresh | Bootstrap/CoreGuardianPrototype.cs:150 → :418 `Changed?.Invoke()` → Hud.cs:115 | That refresh repeats all of items 6-11, plus a Notify concat and `GetComponent<CoreGuardianCombatAudio>()` (:150).

### 3.3 Per-event allocations (combat frequency)

16. P2 | Every damage event allocates a damage-number string and assigns TMP `.text=` instead of using `SetText(int)` | UI/CoreGuardianHud.cs:385, 413 | DamageApplied (Session.Effects.cs:221) fires per hit, chain bounce, burn tick and splash. Kills add `"+"+amount` plus a `GetPreferredValues` layout pass (:392-394). Core hits add `"-"+amount` (:402).
17. P2 | Each played cue effect allocates `"Fx_"+key` | Combat/CoreGuardianEffectsView.cs:62 | Triggered by the Cue events hit_cold, hit_flame, hit_rail and chain (:79-81), by armored hits (:86), and by frost pulses and blasts (:87-88).
18. P2 | Each projectile view rent allocates `"Projectile_"+Id`, even when the view comes from the pool | Bootstrap/CoreGuardianPrototype.cs:596-597 | once per projectile, in SynchronizeViews:461.
19. P2 | Each enemy body rent allocates `"Enemy_"+Kind+"_"+Id` (enum ToString + concat), even on pool reuse. New towers allocate `"Tower_"+Kind+"_"+Id` | Bootstrap/CoreGuardianPrototype.cs:555, 570, 448 | per spawn. The tower name format is test-referenced (`"Tower_Single_1"`), so the format must be kept.
20. P2 | Emphasis hosts are not pooled. Each new body allocates a `GetComponentsInChildren` array, `"ObjectEmphasis_B2_"+target.name`, a new GameObject, `AddComponent<CoreGuardianSelectionOutline>` and a `"SharedOutline"` child GameObject. Each removal calls Destroy plus another `GetComponentsInChildren` | Rendering/CoreGuardianObjectEmphasis.cs:186-190, 221, 156; Rendering/CoreGuardianSharedOutline.cs:158 | per enemy spawn and death, through `ReleaseTarget` (Prototype.cs:481). The enemy body itself is pooled.
21. P2 | `LastMessage` is concatenated on every kill and every core exit, although it is display-only | Domain/Rules/Session.Effects.cs:244; Domain/Rules/Session.cs:472 | per kill (`"+"+gold+" 골드"`) and per enemy exit (`"코어 -"+…`), possibly many per frame.
22. P2 | The gold-gain banner is concatenated on every frame in which gold increased | Bootstrap/CoreGuardianPrototype.cs:148 | effectively every kill frame.
23. P3 | Multi-tower volley SFX allocates a key with `"multi_"+n`. When all voices are busy, an important cue runs a LINQ voice-steal (`Range/Where/OrderBy/DefaultIfEmpty` with closures) | Audio/CoreGuardianCombatAudio.cs:117, 206-208 | per multi volley; voice steal is rare.
24. P3 | `FlushSpawns` copies the list with `pendingSpawns.ToArray()`. `NotifyPlayerStep` allocates `new[]{4 directions}` per step | Domain/Rules/Session.Effects.cs:337, 400 | per split/summon flush; per player tile step with augment 22.
25. P3 | Mask geometry caches allocate on misses: LINQ `Select.ToArray` ×2, `sprite.vertices/uv/triangles` copies, `"Selection mask "+sprite.name` and `Enumerable.Repeat(...).ToList()` | Rendering/CoreGuardianSelectionOutline.cs:301, 271-273; Rendering/CoreGuardianSharedOutline.cs:230 | Per new sprite, so bounded by the sprite set. In the default configuration (SharedOutline.Enabled=true, :35) only the HUD inspection, rage and upgrade SelectionOutline instances run, and their caches clear on epoch change (:144).
26. P3 | Rage and upgrade overlays use `List.Find/Exists` with capturing lambdas and create a new GameObject + SelectionOutline (+ Material) per trigger | Rendering/CoreGuardianRageOutline.cs:23, 28, 31-32; Rendering/CoreGuardianUpgradeFeedback.cs:26, 31, 34-36 | Rage: per boss rage. Upgrade: per upgrade, for each tower of that kind (Prototype.cs:349, 361).
27. P3 | The enemy model pool retains at most 32 slots per kind, so a new `EnemyState` is allocated when more than 32 of one kind are alive | Domain/Rules/SessionModels.cs:15; Domain/Rules/ModelPooling.cs:148, 158 | **Verification data note:** with current data this only triggers in waves 28-29 (33/35 of enemy 11002), and only if they are alive at the same time. The body view pool has the same cap (Prototype.cs:41).
28. P3 | `PlayerHit` allocates 2 `GetComponentsInChildren` arrays, uses LINQ `FirstOrDefault` and reads `r.name` per renderer | Bootstrap/CoreGuardianPrototype.cs:198-201 | per left press/release (:185, :188).
29. P3 | Health-bar sprites are destroyed when the active bar count reaches 0 and regenerated later | Domain/Rules/SegmentedEnemyHealthBar.cs:17, 36-47 | Bars live with the pooled bodies (cap 32/kind), so this is rare.
30. P3 | `GetComponent<CoreGuardianCombatAudio>()?.` runs per click or event. `?.` on a UnityEngine.Object goes against REFACTORING_RULES.md:40 | Bootstrap/CoreGuardianPrototype.cs:150,246-248,288-293,334-364,430,481; UI/CoreGuardianHud.cs:179,313,352; UI/CoreGuardianHud.Redesign.cs:32,38,44,281; UI/CoreGuardianHud.Inspector.cs:58,79,151,171; UI/CoreGuardianAugmentationView.cs:64. Further `?.`/`??` on UnityEngine.Object: Prototype.cs:313, CombatArt.cs:45-46, TowerGrounding.cs:25-26, SpriteLibrary.cs:35, AugmentationView.cs:26 | per event, not per frame.
31. P3 | The augmentation card flip allocates arrays, a `WaitForSecondsRealtime` per batch and one GameObject per spark (up to 104) | UI/CoreGuardianAugmentationView.cs:34, 46, 49, 56, 64 | augmentation screen only.

### 3.4 Gated or unreachable paths (gate-swallowing checks)

32. P3 | The heavy per-frame code in MaskOutputBatch is **edit-mode only**. `RegisterOutput` returns early in Play mode, so no entries are ever registered in Play | Rendering/CoreGuardianSelectionOutline.cs:231 gates Rendering/CoreGuardianMaskOutputBatch.cs:101, 111, 113, 156-169 | Affected code: `Resources.FindObjectsOfTypeAll<Renderer>()` (a new array of all renderers), `FindObjectsByType<SortingGroup>()`, `Safe()` O(entries × all renderers × 8 `WorldToScreenPoint`) and per-entry `GetComponentInParent`. In Play, `Flush` only runs `Restore`, `BodyMaskCache.Flush` and `Prune`, then returns at :94. Also, SelectionOutline.cs:240 (`if(Application.isPlaying&&!prepared)`) is dead because of :231.
33. P3 | The muzzle-flash allocation path in CombatAnimation is unreachable with current roles | Characters/CoreGuardianCombatAnimation.cs:156-165 | A launch source is always the player or a tower (roles Player/Single/Multi/Support/Frost/Flame/Rail). `UsesOriginalArtwork` (Characters/CoreGuardianUnitAnimation.cs:36) is true for every one of them, so the method returns at :156. The `"Muzzle_"+Id` concat, flash GameObjects and pool never run, `flashes` stays empty, and the loop at :111-124 does nothing.
34. P3 | `MaskPage.Queue` and the `MaskProducerBatch.Queue` overload copy mesh arrays and allocate a `Shape` per call, but have no runtime callers (test-only) | Rendering/CoreGuardianMaskPage.cs:65-66; Rendering/CoreGuardianMaskProducerBatch.cs:57-63 | The runtime uses `QueueShape` with cached shapes (SelectionOutline.cs:256).

### 3.5 Hot-path CPU and complexity (no GC; the project rule requires a stated N bound)

35. P3 | Buff auras cost O(E × B) per sub-step, with no complexity comment | Domain/Rules/Session.cs:455-466 | E ≤ ~150, B = living Buff-kind enemies ≤ E.
36. P3 | `FindTower` checks every tower stamp, O(T), on each call. It runs once per tower per frame from `TowerTargetForView`, so the total is O(T²) per frame | Domain/Rules/Session.cs:176-190, 57; Characters/CoreGuardianCombatAnimation.cs:77 | T ≤ 32, so ≤ 1024 compares per frame. `HasTowerSupport` per tower per frame (Prototype.cs:451 → Session.cs:254 `Refresh` O(T)) adds another O(T²).
37. P3 | Line projectiles test every enemy each sub-step, O(L × E), with `Hits.Contains` O(h) and an insertion sort O(h²) | Domain/Rules/Session.cs:562-569 | L = live rail/pierce shots.
38. P3 | `MostPiercedTarget` costs O(A × E) per rail acquisition | Domain/Rules/Session.cs:815-836 | Runs on every retarget, i.e. whenever the rail tower's target dies or leaves range. A ≤ E ≤ 150 gives ≤ 22,500 point-line tests.
39. P3 | With augment 16, `TowerRatePercent` scans 4 sides × all towers for each tower, O(T²) per sub-step | Domain/Rules/Session.cs:670 | T ≤ 32.
40. P3 | `TargetOrder.Compare` switches on the `string Tiebreak` in every comparison. Strings loaded from the CSV are not interned, so each case is a content comparison | Domain/Rules/CombatData.cs:430-436 | Called from every target loop in Shoot and TowerTarget (Session.cs:41, 747, 762, 823).
41. P3 | `StartsWith("Tower:")` (culture-sensitive overload) runs on every projectile and every damage row | Domain/Rules/Session.cs:197; Characters/CoreGuardianCombatAnimation.cs:141; UI/CoreGuardianHud.cs:272,294; UI/CoreGuardianHudSkin.cs:27,38-39,45-51; Audio/CoreGuardianCombatAudio.cs:127 | Per launch: CombatAudio.OnLaunch:99 → TowerFromSource and CombatAnimation.Launched:130,141. CombatArt.Projectile:44 does an O(T) string `==` per projectile view.
42. P3 | Each unit carries three engine-to-managed LateUpdates: `UnitAnimation.LateUpdate` (which does nothing for enemies), `PhotoShadow.LateUpdate`, and `ArtDepthSorting.LateUpdate` (on 21 prefabs). That is about 3 × bodies calls per frame, a candidate for the manager pattern (baseline #12) | Characters/CoreGuardianUnitAnimation.cs:125; Rendering/CoreGuardianPhotoShadow.cs:75; World/ArtDepthSorting.cs:24 | ~185 bodies give ~550 calls per frame. TowerGrounding.Present also runs twice per tower per frame (UnitAnimation.cs:125 and :134).
43. P3 | The shared outline rebuilds its whole batch layout (all part vertices plus a mesh upload) whenever the sprite/flip sequence changes. During combat, animation frame swaps and spawns make this almost every frame | Rendering/CoreGuardianSharedOutline.cs:239-249, 275-309 | `parts.Sort` is O(P log P) with P ≈ bodies × parts (~1000+). The comparison delegate is cached (:64), so there is no GC.
44. P3 | EffectsView checks up to 7 markers per living enemy every frame. Each check does a `Set(string)` dictionary lookup (string hash) and reads `e.SlowPercent` (a `SlowFraction` computation) | Combat/CoreGuardianEffectsView.cs:111-123 | O(E × 7) per frame.
45. P3 | Audio volume reads go to `PlayerPrefs.GetFloat` about 12 times per frame (4 voices × 2 + 2 music × 2) | Audio/CoreGuardianCombatAudio.cs:89 → Audio/CoreGuardianAudioCatalog.cs:28 → Audio/CoreGuardianSoundSettings.cs:22,28; Audio/CoreGuardianMusicAudio.cs:49 | per frame.
46. P3 | Role strings are compared per unit per frame | Characters/CoreGuardianUnitAnimation.cs:84-85, 93; Characters/CoreGuardianOriginalMotion.cs:42, 140-171, 209-213 | per unit per frame. `UsesOriginalArtwork` (7 compares) also runs per projectile (CombatAnimation.cs:156).
47. P3 | Some presentation code writes renderer properties every frame even when nothing changed, unlike the dirty-checked OriginalWalk.cs:160-163 | Characters/CoreGuardianOriginalWalk.cs:280-302 (VerticalLegPair), Characters/CoreGuardianPlayerMotion.cs:70-76, Characters/CoreGuardianOriginalIdle.cs:23-28, Rendering/CoreGuardianPhotoShadow.cs:69-72, Rendering/CoreGuardianTowerGlow.cs:51-53 | per unit per frame (CPU and draw-data dirtying).
48. P3 | Every active `CoreGuardianHudReaction` writes sprite, type, colour and position every frame (the setters equality-check) and invokes the `Selected()` delegate | UI/CoreGuardianHudSkin.cs:72-89 | per active button per frame.
49. P3 | HUD floating numbers call `SetActive(screen.z>0)` and `WorldToScreenPoint`, write colours and re-project every frame for each live number | UI/CoreGuardianHud.cs:465-480 | O(live numbers) per frame, with a canvas rebuild per number per frame (the fade itself makes this inherent).

Negative results (checked and clean, no per-frame GC):
- `ModelList` foreach uses a struct enumerator (ModelPooling.cs:128).
- `EnemyHandle` and `ProjectileHandle` implement `IEquatable`, so the Dictionary/HashSet keys at Session.cs:156,159 and SessionModels.cs:9 do not box.
- `TowerState.AttackSource` is cached (CombatData.cs:448).
- The range-index sort uses a cached `Comparison` (Session.cs:90).
- `RemoveAll(e=>!e.Alive)` (Session.cs:718) captures nothing, so its delegate is cached.
- `SharedOutline.parts.Sort` uses a cached delegate.
- `SpriteReferences.Read` uses the `GetComponentsInChildren(List)` overload.
- `TagCache` is read once (Effects.cs:28).
- `FireCellEnumerator` returns a struct enumerator (Effects.cs:394).
- `AudioCue.Category` is cached (AudioCatalog.cs:18-26).
- `SoundSettings` keys are prebuilt (:10).
- Enum-keyed dictionaries (`Data.Towers` and similar): Unity 6 Mono/IL2CPP `EqualityComparer<TEnum>.Default` does not box.
- Local functions `Point` (SelectionOutline.cs:168) and `Lift` (OriginalWalk.cs:299) are never converted to delegates, so their closures are structs with no heap allocation.
- Session.Tick creates no strings except at items 21 and 22.

---

## 4. Shader cleanup candidates (pixel-identical only)

Verification harness already in the repo: `Tests/EditMode/CoreGuardianSelectionOutlineShaderTests.cs:12-13,67-68`. It renders a fixture reference against a candidate and compares pixels. `Fixtures/SelectionOutlineReference.shader` has HLSL byte-identical to `Art/.../SelectionOutline.shader` (only the shader name differs). Reuse this pattern for every shader change.

50. P3 | Six shaders contain an identical "position → clip, pass uv" vertex function. It can move into a shared `CoreGuardianCommon.hlsl` (same code, so identical output) | Art/Rendering/Shaders/Resources/CoreGuardian/PhotoShadowContact.shader:20-21, SelectionOutline.shader:20-21, TowerBaseShade.shader:27, TowerGlow.shader:31, CoreAmbient.shader:31-35 (+color), UpgradeScan.shader:18 (+worldY) | REFACTORING_RULES.md:52 requires a pixel-compare test first.
51. P3 | The square "max neighbour" outline kernel exists 3 times with different fetch methods (SAMPLE vs LOAD). It could become a macro taking the fetch expression | SelectionOutline.shader:24-29; SharedOutlineOutput.shader:45-50; BatchedMaskOutput.shader:37-39 | Keep `[loop]` and the order of the clip calls.
52. P3 | The contact-band constants `.55`, `.21` and `half4(.035,.03,.025,…)` are duplicated | PhotoShadowContact.shader:26-28; BatchedMaskOutput.shader:33-35 | Use shared `#define`s. Do not merge the code: one uses SAMPLE, the other LOAD.
53. P3 | The "inside UV rect" step expression and the 4/8-tap halo pattern are duplicated | CoreAmbient.shader:38; TowerGlow.shader:34 | A shared helper `InsideRect(uv,rect)` with the same expression gives identical output.
54. P3 | Some local names shadow HLSL intrinsics: `float4 clip` and `float distance` | SharedOutlineMask.shader:14 (`clip` shadows `clip()`); UpgradeScan.shader:22 (`distance` shadows `distance()`) | Rename to `clipPos` and `bandDistance`; identical output.
55. P3 | `_RendererColor` is declared but never read in the HLSL | BodyHitFlash.shader:7 | **Hazard, keep it.** It is listed in the C# probe (Rendering/CoreGuardianSpriteBlocks.cs:13) and set by SpriteBatchCompat.cs:37, PlacementOverlay.cs:37 and RouteMeshBake.cs:16. Report only.
56. P3 | The varying named `alpha` actually carries (opacity, threshold, layer) | BatchedSelectionMask.shader:21,37 | Rename for clarity; identical output.
57. P3 | Only `.x` of `nointerpolation float4 atlasRect` is read | SelectionMask.shader:23,37 | Rename it (for example to atlasLayer). Keep the type and semantics to stay pixel-identical.
58. P3 | The test `_Role>.5&&_Role<1.5` is duplicated, and the role values 0/1/2 are magic numbers | TowerGlow.shader:45,51 | Use a `#define` or local bool. **Do not** replace the literal `3.14159` (:46) with `PI`, because PI is more precise and would change pixels.
59. P3 | Input-struct naming is inconsistent: `In`/`Out` here, `Attributes`/`Varyings` elsewhere; `Attributes i` reuses the name `i` that other shaders use for Varyings | UpgradeScan.shader:16-18; TowerBaseShade.shader:27; TowerGlow.shader:31 | rename only.
60. P3 | Blend/Cull/ZWrite state is declared at SubShader level in some shaders and at Pass level in others | SubShader level: BodyHitFlash:17-19, CoreAmbient:13-15, TowerBaseShade:12-14, TowerGlow:15-17. Pass level: the rest | Style only. Moving state between levels is legal, but treat it as a pass-state change and verify with the harness.
61. P3 | Global or MPB-driven uniforms are not in `UnityPerMaterial`: `_Region`/`_Parameters` (BatchedMaskOutput:17), `_Band`/`_MaskOriginPx` (PhotoShadowContact:16-18), `_Threshold`/`_Opacity`/`_MaskVP` (SelectionMask:19-22), `_BandY`/`_BandWidth`/`_Opacity` (UpgradeScan:15) | listed files | **Do not "fix".** The SRP Batcher is off, and moving uniforms into the CBUFFER changes batching compatibility. Report only.
62. Hazard list (no change allowed). These are contracts with the C# code:
    - Shader names `CoreGuardian/*`, loaded by `Resources.Load<Shader>`.
    - Every property name set from C#: `_HitFlashAmount`, `_HitFlashTint`, `_RampTex`, `_RampAmount`, `_Strength`, `_UVRect`, `_Particles`, `_Band`, `_MaskOriginPx`, `_MainTex_TexelSize`, `_Color`, `_Width`, `_MaskAtlas`, `_UseAtlas`, `_Opacity`, `_Threshold`, `_MaskVP`, `_UsePartMatrices`, `_Items`, `_PageSize`, `_Single`, `_Region`, `_Parameters`, all `_CG*`, `_BottomUV`, `_TextureHeight`, `_Role`, `_Age`, `_Clock`, `_PulseScale`, `_BandY`.
    - SharedOutlineMask pass indices 0/1 (C# `DrawMesh(...,i,0,...)` at SharedOutline.cs:270 and `DrawRenderer(...,0,1)` at :225).
    - Struct layouts `Item` (BatchedSelectionMask.shader:16 = MaskProducerBatch.cs:21-24, 160 B) and `Part` (SharedOutlineMask.shader:47 = SharedOutline.cs:30).
    - `precise` and the 256-subpixel snap at BatchedSelectionMask.shader:26,32.

---

## 5. Mechanical style facts

- **Encoding and line endings**: all 83 runtime, editor and shader files are UTF-8 **without BOM** (no `EF BB BF` header was found with `od`). All use **LF**: `grep -c $'\r$'` counted 0 CR line endings in every file, and `file` reported no CRLF. Every file checked ends with a newline. This matches REFACTORING_RULES.md:22.
- **`.editorconfig` is missing**: REFACTORING_RULES.md:18 calls `.editorconfig` canonical, but none exists in the copy (`ls -la /home/claude/review/src` shows only AGENTS.md, ASSET_LIST.txt, Assets, Packages, ProjectSettings, docs). Either it was not exported or it does not exist; it must be created in step ① (tool setup).
- **Indentation**: 4 spaces everywhere; no line starts with a tab in any file. Exception: Welcome2DScript.asmdef uses tabs.
- **Brace and spacing style, two dialects**:
  - (a) **Compressed**: the majority, including all of Session*, CombatData, Prototype, Hud*, Rendering, Characters, Combat, Audio and Editor. No spaces around operators or after commas, many statements per line, braceless nested control on one line, and one-line methods (for example the Session constructor at Session.cs:258 and MaskProducerBatch.ReleaseAll at :152). Braces go on new lines for types and methods, but inline `{...}` blocks are common.
  - (b) **Allman with spaces**: Health.cs, EnemyController.cs, EnemyDefinition.cs, WaypointPath.cs, BeanProjectile.cs, TowerTargeting.cs (partly one-line), TowerCombatStats.cs, PrototypeGridMovement.cs, ArtDepthSorting.cs, Welcome2DScript.cs.
  - Rules :19-21 (spaces, 120 chars, one statement per line, braces on bodies) are violated by dialect (a) almost everywhere.
- **Lines over 120 characters: 1,222 in .cs files plus 25 in the 12 shaders (1,247 total).** Largest: CombatData 115, Session 120, Hud 99, Prototype 65, Hud.Redesign 64, SelectionOutline 51, Session.Effects 41 (see the table). The longest single lines are MaskProducerBatch.cs:152 (563 characters) and Session.cs:258 (~700).
- **Namespaces**:
  - `CoreGuardian` is used only by Domain/Rules (Session*, CombatData, Augmentations, ModelPooling, SegmentedEnemyHealthBar).
  - The **global namespace** holds every Presentation, Bootstrap and Editor type and the legacy Domain types.
  - `Unity.U2D.Welcome` holds the template.
  - Rule :23 says "keep `CoreGuardian`", so moving types into a namespace is a rename of their full names. It would break `GetType("CoreGuardianPrototype")` and the other 21 global-name lookups in tests. Do not change namespaces.
- **Assemblies**:
  - Only `Runtime/Domain/Fortress.Game.asmdef` exists. Bootstrap and Presentation compile into Assembly-CSharp, and Editor into Assembly-CSharp-Editor.
  - The test asmdefs reference only `Fortress.Game`. That is why tests use reflection for everything in Presentation: 23 `GetType` names, 208 distinct `GetField/GetMethod/GetProperty` member names and 55 `BindingFlags.NonPublic` sites.
  - Domain cannot reference Presentation (the asmdef enforces the one-way dependency). However, Domain/Rules contains the MonoBehaviour `SegmentedEnemyHealthBar`, and Domain/Common/Enemies/Towers contain legacy MonoBehaviours.
- **Unused usings**: a heuristic scan (namespace→type pattern match, script at `/tmp/claude-0/-home-claude/33b186f7-118c-5db9-8d3f-f655106e9b85/scratchpad/usings.py`) found **no unused `using`** in Runtime or Editor; `UnityEngine.U2D` in SpriteAtlasPipeline is used for `SpriteAtlas`. Redundant or inconsistent qualification:
  - ModelPooling.cs:28 fully qualifies `System.Collections.Generic.List` although the using is present.
  - CombatAudio.cs:25-26,120, OriginalMotion.cs:8,112, OriginalWalk.cs:13,208-209, Inspector.cs:22 and BodyMaskCache.cs:21 fully qualify instead of adding a using.
  - The HUD partials disagree: Hud.cs/Inspector.cs/Abilities.cs write `UnityEngine.UI.Text`, while Redesign.cs and Abilities.cs `using UnityEngine.UI`.
- **Files with several types** (a split candidate; MonoBehaviour name ≠ file name means AddComponent-only):
  - SelectionOutline.cs (3 types), BodyMaskCache.cs (+`CoreGuardianBodyMaskLifetime` MonoBehaviour), HudSkin.cs (+`CoreGuardianHudReaction` MonoBehaviour), Hud.Redesign.cs (+`CoreGuardianHoverHint` MonoBehaviour), AudioCatalog.cs (2), Augmentations.cs (3), CombatData.cs (15), ModelPooling.cs (10), SpriteAtlasPipeline.cs (2), MaskAtlasBuilder.cs (2).
  - File name ≠ class name: MaskAtlasBuilder.cs → `CoreGuardianMaskAtlasBuilder`, RouteMeshBuilder.cs → `CoreGuardianRouteMeshBuilder`. Both are static Editor classes, so Unity is fine, but tests look up both by type name.
- **Naming convention facts**:
  - Private statics vary: camelCase (`rangeEntryOrder`, `abilityKeys`, `damageSprites`) vs PascalCase (`Sides`, `UpgradeOrder`, `LabStats`, `Names`, `PlayerAngles`).
  - Public fields of nested and plain data types are lowercase in about 15 types: Tint, Body, Leg, Pose, Flash, Part, Entry, MaskGeometry, MaskBatch, OutputParameters, Draw, Item, AudioCue, Entries, Layer.
  - One-letter methods: `F`, `I`, `S`, `B`, `W`, `V`, `H5`.
  - Unit suffixes required by rule :29 are mostly missing (`PreparationRemaining`, `Range`, `Cooldown`, `duration` are all seconds or tiles). `widthPixels`, `outlineWidthPixels`, `maskOriginPx` and `HealMarkSeconds` comply.
  - A writable public static `CoreGuardianSharedOutline.ReuseGeometry` (:82) and internal writable static counters (BodyMaskCache.cs:22) go against rule :41.
- **Empty catch**: `try{csv?.Dispose();}catch{}` (PerfOverlay.cs:319) breaks rule :39. The other bare `catch{...throw;}` blocks rethrow and are fine: SessionModels.cs:67,78; SelectionOutline.cs:158; RouteMeshBuilder.cs:37,125; MaskAtlasBuilder.cs:80.
- **Dead or test-only code** (for perspective D):
  - `Prototype.Ring` :608 and the `dot` sprite :61/113.
  - `Hud.ReleaseInspection` (Inspector.cs:44) and `towerAbilities` (Abilities.cs:9).
  - `AugmentationEffects.RoundsSince12` (Augmentations.cs:75).
  - `ObjectEmphasis.EndCameraRendering` (empty) :123.
  - `CombatRules.Priority/TowerPriorityOrder` (CombatData.cs:393-394) have no runtime callers.
  - `UnitAnimation.FrameInfo/AtlasInfo` (:10-11) have no references anywhere in Scripts.
  - The `MaskPage.Queue` overload, `SelectionOutline.PresentAtOrder` and `PresentGroundContact*` have no runtime callers (test-only).
  - The muzzle-flash path (item 33).
  - The CombatAudio `*ResourcePath` consts are **not** dead: tests reflect them.

---

## Counts

P0: 0 · P1: 0 · P2: 16 (items 1-3, 6-10, 15-22) · P3: 45 (items 4-5, 11-14, 23-61; item 62 is a hazard list, not a finding).

Disposition note: this perspective did not run a final review step. Gate checks applied:
- Item 32: MaskOutputBatch allocations downgraded to P3 as edit-mode only (gate at SelectionOutline.cs:231).
- Item 33: muzzle-flash allocations downgraded to P3 as unreachable (UnitAnimation.cs:36).
- Item 27: pool-churn evidence checked against waves.csv.
