# Perspective B — Lifecycle and async ordering (Runtime + Editor .cs)

Scope read line by line: all 72 Runtime + Editor .cs files (10,394 lines). Tests consulted only for caller checks (grep).
Domain Reload OFF is stated by the task/AGENTS.md (`ProjectSettings/EditorSettings.asset` is not in the copy, so the setting itself could not be read; treated as given).
Format: `P-level | conclusion | file:line | trigger path / evidence`.

---

## 0. Enumeration (review objects)

**Unity lifecycle hosts (MonoBehaviour)**: CoreGuardianPrototype (Start 95, Update 138, OnDestroy 621) · PrototypeGridMovement (Awake 29, Update 47) · CoreGuardianHud (Update 501, LateUpdate 465, OnDestroy 588) · CoreGuardianHoverHint (OnDisable Redesign.cs:401) · CoreGuardianAugmentationView (Update 66, OnDisable 67) · CoreGuardianHudReaction (Awake/Start/Update/OnDisable HudSkin.cs:62-96) · CoreGuardianHudHandleReaction (LateUpdate/OnDisable) · CoreGuardianPerfOverlay (Awake 81, Update 91, OnApplicationQuit 370, OnDestroy 371, DDOL 30) · CoreGuardianCombatAudio (Awake 50, OnEnable 84, OnDisable 86, OnDestroy 87, Update 89) · CoreGuardianMusicAudio (Awake, Update, OnDisable 53) · CoreGuardianCombatArt (Update 100, OnEnable 113, OnDisable 114, OnDestroy 115) · CoreGuardianEffectsView (LateUpdate 97, OnDestroy 28) · CoreGuardianCombatAnimation (OnDestroy 174) · CoreGuardianUnitAnimation [order 100] (OnEnable/OnDisable/OnDestroy/LateUpdate 121-125) · CoreGuardianPhotoShadow [200] (LateUpdate 75, OnEnable 79, OnDisable 78) · CoreGuardianObjectEmphasis [450] (OnEnable 112, LateUpdate 160, OnDisable 233, OnDestroy 239) · CoreGuardianSelectionOutline [500] (LateUpdate 59, OnDisable 411, OnDestroy 412) · CoreGuardianBodyMaskLifetime (OnDisable BodyMaskCache.cs:244) · CoreGuardianUpgradeFeedback [600] · CoreGuardianRageOutline [600] · SegmentedEnemyHealthBar (Awake 16, OnDestroy 17) · ArtDepthSorting [ExecuteAlways] (LateUpdate 25) · CoreGuardianHierarchyWatch [ExecuteAlways] · CoreGuardianRouteMeshBake [ExecuteAlways] (OnEnable) · legacy: Health, EnemyController, BeanProjectile, LightningTower, TowerTargeting.
**Coroutines**: AugmentationView Flip 32 / Sparks 44 / Ray 52 (StopAllCoroutines in OnDisable 67) · LightningTower.ShowBeam LightningTower.cs:16. **Invoke/InvokeRepeating/async**: none found.
**C# events (subscribe → unsubscribe)**:
- Session.DamageApplied: CombatAudio Subscribe 83/Unsubscribe 85 · Hud.cs:115 / 590 · CombatArt.cs:30 / 117 · CombatAnimation.cs:42 / 177 · EffectsView.cs:26 / 31
- Session.ProjectileLaunched: CombatAudio 83/85 · CombatAnimation 41/176 · AttackLaunched, DefeatReached, CombatStarted, RoundCompleted, VictoryReached: CombatAudio 83/85
- Session.Cue: CombatAudio 83/85 · EffectsView 26/31 · TowerPulsed: EffectsView 26/31 · CombatAnimation 43/178 · AreaBlastUsed: EffectsView 26/31
- Prototype.Changed: Hud.cs:115 / 590 · PrototypeGridMovement.Stepped: Prototype.cs:108 / **none**
- RenderPipelineManager.begin/endCameraRendering: ObjectEmphasis 114-115 / 235-236 · static ctor (never removed): MaskOutputBatch 67-73, MaskPage 73-78 (incl. beginFrameRendering), BodyMaskCache 111-117
- SceneManager.sceneLoaded / Application.quitting: SpriteBatchCompat 22-23 (-=/+=), ObjectEmphasis 81 (-=/+=), MaskPage 75, MaskOutputBatch 70
**RuntimeInitializeOnLoadMethod**: SubsystemRegistration → ObjectEmphasis.ResetTwins 80, SpriteAtlasSources.Reset 17, SegmentedEnemyHealthBar.ResetCache 49, BodyMaskCache.NewSession 119 · AfterSceneLoad → PerfOverlay.Install 25, SpriteBatchCompat.Initialize 19.
**Pools / reuse**: EnemyState.Clear ModelPooling.cs:19 · ProjectileState.Clear 29 · ModelPool Rent/Return/Flush 145-161 · Prototype enemy body pool 468-504 / 542-574 · projectile view pool 575-602 · Hud damage-number pools 406-464 · CombatArt effect pool 81-99 · EffectsView renderer/anim pools 48-55,105,146 · CombatAnimation flash pool 111-124,157-165 · UnitAnimation.ResetEnemyForReuse 113 · HitFlash.ResetForReuse 99 · OriginalWalk.ResetForReuse 128 · FrameWalk.ResetForReuse 58 · Session StepWork/ShotWork 150-171.
**Timers in Update**: Prototype.notificationUntil 148/418 · Hud.nextRefresh 501 · damage numbers (Time.time) 470 · CombatArt effects (Time.time) 106 · CombatAudio cooldowns (unscaled) 95-188 · Session Preparation/Augmentation/cooldowns (scaled dt) 426-427,705,753.
**Click entry points**: HUD Button() wrapper Hud.cs:179 (all labelled buttons incl. Start, Reload, TestSpeed, RoleDiagnostic, purchases, lab, install, cheats, sound previews) · augmentation card AugmentationView.Click 31 → Hud.cs:160-161 · reroll Hud.cs:134 · damage fold 257 · SmallHandle Inspector.cs:58 · AreaBlast Redesign.cs:346 · AugmentMore Redesign.cs:281 · world click Prototype.cs:181-192 · keyboard Esc/Space/1/2 154-162.

---

## 1. Findings

### P2

**B-01 | P2 | The "전체/+N" received-augment button (AugmentMore) is permanently non-interactable from the first augmentation screen onward, so the click-to-pin augment list never works in normal play (Tab still works).** | CoreGuardianHud.cs:530-531; CoreGuardianHud.Redesign.cs:278-281,315 | Trigger: enter any augmentation phase → `Refresh` sets `interactable=false` on every `Button` under the canvas (inactive included) that is not in `augmentationButtons` (Hud.cs:530-531). Every other Button restores itself on a later refresh: `Button()` helper Hud.cs:190, damage fold Hud.cs:255, SmallHandle Inspector.cs:55-57 (comment: "Augmentation disables ordinary buttons, including inactive handles. Restore on the next refresh"), blast Redesign.cs:354. AugmentMore is created by a bare `AddComponent<Button>()` (Redesign.cs:278) with no `refresh.Add`, and nothing else ever writes its `interactable`. It is only shown when `selected.Count>0` (Redesign.cs:315), which in normal play first happens right after that augmentation screen, so it is disabled before it ever appears (HudReaction then shows the "Disabled" skin, HudSkin.cs:75). Only path to see it enabled: cheat `Cheat_GrantCard` before any augmentation screen. No test references AugmentMore (grep of Tests: 0 hits). Twin-inconsistency (hard rule 11) with the three restoring twins listed above.

**B-02 | P2 | The kill gold popup shows the base `Spec.Gold`, not the gold actually awarded, whenever kill-gold augments (10 현상금, 11 저금통, 18 빚내기) are active.** | CoreGuardianCombatArt.cs:69; Session.Effects.cs:243 | Subscriber-by-subscriber audit of the DamageApplied broadcast (hard rule 10): award = `FloorToInt(Spec.Gold*(1+KillGoldPercent/100)+.5)` (Session.Effects.cs:243), popup = `enemy.Spec.Gold` from the snapshot (CombatArt.cs:69, snapshot field ModelPooling.cs:100). The Prototype's own text feedback uses the real delta (Prototype.cs:148), so the two displays disagree. Summons are correctly suppressed (SummonSpec Gold=0, Session.Effects.cs:331 → ShowGoldNumber early-out Hud.cs:391). Overlaps perspective A (data flow); behaviour-preserving refactor keeps it, list for user choice.

### P3

**B-03 | P3 | `ArtDepthSorting.nextLane` is a static counter never reset; with Domain Reload OFF and `[ExecuteAlways]`, the lane (and so the same-row draw order of overlapping units) differs between Play sessions and between Editor and build.** | ArtDepthSorting.cs:13,27 (ExecuteAlways 6) | Each instance takes `lane=(nextLane++)&15` on its first LateUpdate (edit-mode LateUpdates also increment). ArtDepthSorting is on 21 prefabs (GUID 62c7bc6f… found in 21 .prefab files). Order() uses the lane to break same-row ties (ArtDepthSorting.cs:18-21). Consequence: purely visual, but it makes "visuals 100% identical" before/after comparisons non-reproducible unless Unity is restarted / domain reloaded first. Refactor hazard: verification data note.

**B-04 | P3 | Several static caches have no Play-enter reset, so with Domain Reload OFF they keep data from the previous Play (content edited between Plays is not picked up; a first miss is latched).** | CoreGuardianAudioCatalog.cs:34-35,38 (`cues`, `byClip`); CoreGuardianPhotoShadow.cs:15,38 (`registration` JSON); CoreGuardianSpriteLibrary.cs:13-14,19 (`instance`+`searched`: one null result is latched until domain reload); CoreGuardianHud.cs:56,243-246 (`damageSprites` stores null for missing names permanently, unlike its twin `CoreGuardianHudSkin.Sprite` which retries nulls, HudSkin.cs:10-11); CoreGuardianHitFlash.cs:12,23-28 (`shared` HideAndDontSave material, never destroyed; recreated only if Unity-null) | Contrast: ObjectEmphasis/SpriteAtlasSources/SegmentedEnemyHealthBar/BodyMaskCache all reset at SubsystemRegistration (see §3). Nothing breaks in the current data; it bites only when catalog.json / registration.json / SpriteLibrary.asset / HUD sprites change while the Editor stays open.

**B-05 | P3 | `CoreGuardianSharedOutline` keeps global mutable switches (`Batched`, `Enabled`, `public static ReuseGeometry`) with no reset; currently zero writers, so behaviour is fixed.** | CoreGuardianSharedOutline.cs:27-28,35-36,82 | grep for `SetBatched(`/`SetEnabled(`/`ReuseGeometry=` across Assets/Scripts (incl. Tests): only the definitions. Hazard: any future test/tool toggle survives into the next Play with Domain Reload OFF (hard rule 7: public mutable static field).

**B-06 | P3 | `Session.Dispose` nulls 7 events but not `Cue`, `TowerPulsed`, `AreaBlastUsed`.** | SessionModels.cs:131; Session.Effects.cs:14-16 | All three subscribers (EffectsView 26/31, CombatAnimation 43/178, CombatAudio 83/85) unsubscribe themselves, so there is no leak or late callback today; inconsistent teardown contract only.

**B-07 | P3 | `player.Stepped` is subscribed with a lambda and never unsubscribed; the handler calls `Session.NotifyPlayerStep`, which has no `disposed` guard.** | CoreGuardianPrototype.cs:108; PrototypeGridMovement.cs:27,89; Session.Effects.cs:396-398 | Both objects live and die with the same scene and Stepped fires only from the player's own Update, so it cannot fire after Prototype.OnDestroy in the current design. Hazard if the player is ever made persistent or the prototype is destroyed alone.

**B-08 | P3 | Returning a boss body to the pool detaches ObjectEmphasis and UpgradeFeedback but not the RageOutline; if a same-kind boss reuses that body in the same frame, the 0.5 s red rage flash carries over onto the new boss.** | CoreGuardianPrototype.cs:480-483 (comment: "These hosts live outside the enemy hierarchy. Detach them before same-frame reuse"); CoreGuardianRageOutline.cs:28,44 | RageOutline drops a flash only in LateUpdate when `!target.activeInHierarchy` (44); ReturnRemovedEnemyBodies → RentEnemyBody runs within one SynchronizeViews before LateUpdate and reactivates the root (Prototype.cs:573). `ragedBosses` is released (CombatAnimation.cs:71), the outline host is not. Needs a boss of the same source to die and another to spawn in one frame (boss pool cap 4, Prototype.cs:41): narrow.

**B-09 | P3 | Pool-return cleanup order makes the emphasis "twin copy" restore ineffective: `ObjectEmphasis.ReleaseTarget` (which runs `RestoreTwinCopies`) runs before `root.SetActive(false)`, whose `UnitAnimation.OnDisable → HitFlash.Clear` then writes back `originals[]` that were captured while the twin was assigned.** | CoreGuardianPrototype.cs:481-483; CoreGuardianObjectEmphasis.cs:153-158,212-215; CoreGuardianUnitAnimation.cs:122; CoreGuardianHitFlash.cs:58 (capture), Release (restore) | Trigger: an enemy that dies within 0.09 s of a non-lethal hit flash, or while frozen (ramp engaged). The pooled body keeps the CG-emphasis twin material. No visible difference today: emphasis re-applies the same twin on reuse, and `SourceOf()` maps it back (ObjectEmphasis.cs:39-40). Classic "Clear before restore" case (hard rule 9).

**B-10 | P3 | Execute (처형) damage is reported after the per-hit side-channel flags have been reset, so its damage number never shows crit/weak/resist styling.** | Session.Effects.cs:184-185 (flags set, reset in finally), 196-199 (second DealDamage outside that scope); CoreGuardianHud.cs:383-385 (reads `HitCritical`/`HitAffinity` inside the callback) | `HitCritical`/`HitAffinity` are valid only during the first `DealDamage`. Whether the execute number should inherit them is a design call; listed as an ordering artefact of the global-flag channel.

**B-11 | P3 | Presentation cues are silently capped at 256 per flush; under very dense combat the later cues of the frame (including execute/shatter/summon/star_merge VFX and SFX) are dropped first-come.** | Session.Effects.cs:19,20-27 | Signal false negative (hard rule 10). Frost pulses raise one `hit_cold` per enemy in range (Session.cs:713), so 32 towers × clustered enemies can exceed 256 at ×3 speed. Bounded by design; no failure feedback. Not measured; P3.

**B-12 | P3 | Diagnostic reset (`StartDiagnosticEncounter` + `Prototype.Diagnostic`) leaves per-run crit state from the previous run: `playerAttacks`/`playerCritBonus` are not reset, so a 26-augment crit bonus or a 44 forced-crit cadence carries into the first diagnostic shots.** | Session.cs:396-398; Session.Effects.cs:471,476,477 | `Augments` is replaced (Session.cs:398), but `RollCrit` adds the stale `bonus` before it is cleared (Session.Effects.cs:476). Area-blast cooldown is safe because skills.csv `resetOnRoundStart=1` (Data/Balance/Resources/CoreGuardian/skills.csv row 32001) → reset at StartCombat (Session.cs:388). A dev/QA-mode inaccuracy only.

**B-13 | P3 | `CoreGuardianPrototype.Start` failure leaves a half-initialized but "Ready" game: `Ready => Session!=null` stays true once the Session is built, while `enabled=false` stops ticking; HUD/Emphasis/Effects keep running against it.** | CoreGuardianPrototype.cs:76,103,125 | Example: a missing CombatB sprite throws in CombatArt.Configure (CombatArt.cs:29) after the HUD (Prototype.cs:114) and CombatAudio (111) are already subscribed. The error is logged and shown in Feedback, but the state continues (hard rule 5: logging and continuing). Needs a missing asset → latent.

**B-14 | P3 | `?.` / `??` on UnityEngine.Object (banned by hard rule 9). Every target exists today, so none currently misbehaves.** All occurrences:
- `GetComponent<CoreGuardianCombatAudio>()?.` — CoreGuardianPrototype.cs:150,246,247,248,288,290,293,334,339,340,350,352,362,364,430; CoreGuardianHud.cs:179,313,352; CoreGuardianHud.Redesign.cs:32,38,44,281; CoreGuardianHud.Inspector.cs:58,79,151,171
- `GetComponent<CoreGuardianObjectEmphasis>()?.ReleaseTarget` CoreGuardianPrototype.cs:481 · `upgradeFeedback?.ReleaseTarget` 482 · `effects?.Clear()` 390 · `TowerArtFor(..)?.GetComponentInChildren<SpriteRenderer>(true)?.sharedMaterial` 313
- `GetComponentInParent<CoreGuardianCombatAudio>()?.PlayCue` CoreGuardianAugmentationView.cs:64 · `Resources.Load<Font>(..)??font` CoreGuardianAugmentationView.cs:26
- `fireball??=Resources.Load<Sprite>` CoreGuardianCombatArt.cs:45 · `railBolt??=CoreGuardianSpriteLibrary.Instance?.Get(..)` 46
- `UnitPrefab(..)??enemyArt[..]` CoreGuardianPrototype.cs:523 · `UnitPrefab(..)??towerArt[..]` 528
- `FirstOrDefault(r=>r.name=="2D Sprite")?.transform` CoreGuardianSelectionOutline.cs:94
- `root.Find(..)?.GetComponent<SpriteRenderer>()` CoreGuardianTowerGrounding.cs:25,26
- `Get(prefix+i)??Get(..)` (Sprite) CoreGuardianSpriteLibrary.cs:35
- `Resources.Load<TextAsset>(..)?.text` CombatData.cs:134
- Editor: `(AssetImporter.GetAtPath(p) as TextureImporter)?.textureType` CoreGuardianSpriteAtlasPipeline.cs:40
Evidence that targets exist: CombatAudio is GetComponent-or-AddComponent'd on the same GO before any use (Prototype.cs:109-110); ObjectEmphasis, UpgradeFeedback, SelectionOutline, CoreParticleSettings and Prototype all sit on scene GameObject fileID 1617141371 (scene lines 18469-18580). Risk is conditional: in the Editor a missing `GetComponent<T>()` yields a Unity "fake null" (Unity's custom `==` behaviour), so `?.` would not short-circuit and would throw MissingComponentException. This would only surface if a refactor moves these components. Excluded as non-Unity (`?.` is fine there): Session/handle structs, events (`?.Invoke`), plain classes (placementOverlay, coreFeedback/coreAmbient, towerGlow/Grounding, hitFlash, leases, GraphicsBuffer/CommandBuffer, MaterialPropertyBlock, StreamWriter).

**B-15 | P3 | Small pool/reset gaps with no visible effect:** `EffectsView.Clear` drops Anim objects instead of returning them to `animPool` (CoreGuardianEffectsView.cs:148 vs 105), which costs a re-allocation after Diagnostic · `EnemyState.Clear` does not reset `PoolKind` (ModelPooling.cs:15,19); harmless because a slot always returns to its own per-kind pool and `PoolKind` is rewritten on rent (SessionModels.cs:63) · `PerfOverlay.StopRecording` has an empty `catch{}` on Dispose (CoreGuardianPerfOverlay.cs:319, hard rule 5).

**B-16 | P3 | Legacy MonoBehaviours have no scene or prefab mounts; `LightningTower.ShowBeam` would leave the beam enabled if its GameObject were disabled mid-coroutine.** | LightningTower.cs:14,16; Health.cs; EnemyController.cs; BeanProjectile.cs; TowerTargeting.cs; WaypointPath.cs | GUID search over all .unity/.prefab: 0 refs each. Code references only from Tests/PlayMode (EnemyMovement/BeanProjectile/Health tests). Carriers checked: .unity, .prefab, Runtime/Editor code. Not checked: .asset files, reflection. So "no reference found" here, not "dead".

---

## 2. Click entry points: in-flight / double-trigger

| Entry | Guard | Verdict |
|---|---|---|
| All `Button()` HUD buttons (Hud.cs:179) | `if(!b.interactable)return;` + synchronous Session validation | Safe: every action is synchronous; no request is in flight. Repeat clicks repeat purchases by design, and Session re-checks currency each time (Session.cs:354,366, 292). |
| Start (Hud.cs:201) | `Phase==Preparation` enabled + Session.cs:384 | Safe |
| Card select (AugmentationView.cs:31) | `Busy` during flip; `allowed()`=phase; Session.cs:639 phase check | Safe; reroll/destroy stop coroutines via OnDisable 67 |
| Reroll (Hud.cs:134) | rerollsLeft (Session.cs:633) + `choiceSignature=null` forces rebuild | Safe |
| Timeout auto-choose (Session.cs:426) vs a click in the 0.2 s refresh gap | click sees `allowed()` false | Safe |
| AreaBlast (Redesign.cs:346, Space Prototype.cs:156) | cooldown in Session.Effects.cs:421 | Safe |
| Destroy (Inspector `infoDestroy`, Redesign.cs:63) | SelectedTower → -1 after use (Prototype.cs:335) | Safe |
| World place/select (Prototype.cs:181-192) | press/release gesture, `pointerBlocked` on phase change (152,184) | Safe |
| Restart (Prototype.cs:417) | none, but LoadScene is a single frame-end op | Acceptable |
| AugmentMore (Redesign.cs:281) | see B-01 | Dead after first modal |

## 3. The three reset moments: what actually resets

**(1) Restart run (`Restart` → `SceneManager.LoadScene`, Prototype.cs:417)**: the whole scene is rebuilt, including a new Session (103), HUD, views and pools. timeScale is restored (417) and again in OnDestroy (626). Cursor is restored (627-628). Session.Dispose (625) clears models and 7 events (SessionModels.cs:129-131). Statics that survive: see §4. BodyMaskCache/MaskPage/MaskOutputBatch prune on `sceneUnloaded`. SegmentedEnemyHealthBar sprites are cleared when the last bar dies (17). PerfOverlay survives (DDOL) and re-finds the game (155).

**(2) Round transitions (Session)**
- Prep→Combat `StartCombat` Session.cs:385-390: waveTime, spawned, DamageTotals, BossKilled, coreDamaged, player target/acquired/cooldown, fireCells/fireTick, areaBlastCooldown (data flag = 1), tower Target/AcquiredOnce/Cooldown. Not reset (run-wide by design): crit counters, Augments counters, towerUpgradeSteps.
- Combat→Prep/Aug/Victory `CompleteRound` 594-619: projectiles, fireCells. DamageTotals is kept for the preparation display (cleared at next StartCombat). Defeat 474: projectiles plus dead enemies only; living enemies stay frozen in the list.
- Aug→Prep `ChooseAugmentation` 643: choices, timer, rerolls.
- HUD side: install panel closes on any phase change (Hud.cs:508); big panels close (Inspector.cs:184-186), and info is released on Augmentation; modal gating (Hud.cs:527-537) + per-button restore (B-01 gap).
- Prototype side: install cancelled and pointer blocked on phase change (146,152). Detection is poll-based (phase captured at 141 before Tick), so changes made by HUD clicks are announced by the click handlers themselves (StartCombat 385, card 161, cheats `Done()`).
- Diagnostic: Session.cs:396-398 + Prototype.cs:386-395 (damage numbers, upgrade scans, combat effects, effects view, install, occupancy). Gaps are listed in B-12.

**(3) Scene unload / Play exit (Domain Reload OFF)**: per-object teardown runs (all subscription pairs above are matched). Static state: see §4.

## 4. Static mutable state vs Domain Reload OFF

| Static | File:line | Reset on Play enter / exit? |
|---|---|---|
| ObjectEmphasis twinCache/twinInfo/twinSynced/tintFrame | ObjectEmphasis.cs:55-58 | Yes: SubsystemRegistration 80-81 + quitting |
| SpriteAtlasSources mappings/authored/loaded/initialized (+OriginalWalk VerticalLegPair caches) | SpriteAtlasSources.cs:12-15,17-18; OriginalWalk.cs:6 | Yes: SubsystemRegistration |
| SegmentedEnemyHealthBar sprites/activeBars | SegmentedEnemyHealthBar.cs:11-12,49 | Yes |
| BodyMaskCache entries/active/pending/pendingCopies | BodyMaskCache.cs:16-21,113-120 | Yes (Exiting*Mode + SubsystemRegistration + sceneUnloaded); counters `nextEpoch`, `Hits…` monotonic (diagnostic only) |
| MaskPage views/nextGeneration/nextLease | MaskPage.cs:59-61,75-78 | Yes (ExitingPlayMode/quitting ReleaseAll); counters monotonic |
| MaskProducerBatch queued/groups/material/commands | MaskProducerBatch.cs:35-54 | Yes via MaskPage.ReleaseAll |
| MaskOutputBatch entries/views/material | MaskOutputBatch.cs:46-64 | Yes (ExitingPlayMode/quitting Clear) |
| SpriteBatchCompat twins + counters | SpriteBatchCompat.cs:14-17,19-29 | Yes (quitting Release; counters at AfterSceneLoad) |
| SharedMaskMaterial entries | SelectionOutline.cs:428 | Ref-counted; released by consumers' OnDestroy 412-419 |
| PerfOverlay.instance | PerfOverlay.cs:23,376 | Yes (nulled in OnDestroy; DDOL destroyed on exit) |
| **ArtDepthSorting.nextLane** | ArtDepthSorting.cs:13 | **No** → B-03 |
| **AudioCatalog cues/byClip** | AudioCatalog.cs:34-35 | **No** → B-04 |
| **PhotoShadow.registration** | PhotoShadow.cs:15 | **No** → B-04 |
| **SpriteLibrary instance/searched** (+ asset-side `lookup`) | SpriteLibrary.cs:12-14 | **No** → B-04 |
| **Hud.damageSprites** | Hud.cs:56 | **No** → B-04 |
| HudSkin.sprites | HudSkin.cs:10 | No, but self-heals nulls (11) |
| **HitFlash.shared** | HitFlash.cs:12 | **No** (one leaked material per domain; self-heals) → B-04 |
| SelectionOutline.contactShader, MaskProducerBatch.sourceShader, SpriteBlocks.probe, SharedOutline.rfloatSupported | various | No; immutable asset/device caches; OK |
| **SharedOutline Batched/Enabled/ReuseGeometry/LayoutRebuilds** | SharedOutline.cs:27,35,82,310 | **No** → B-05 |
| SoundSettings | SoundSettings.cs | PlayerPrefs-backed by design |

## 5. Refactoring hazards (lifecycle / ordering)

- **H-1 Event subscriber order = `Configure` order** in Prototype.Start (111 CombatAudio → 114 HUD → 115 CombatArt → 116 CombatAnimation → 117 EffectsView). DamageApplied invocation order decides sibling order in `worldFeedback`: the hit number is created by the HUD before the gold number is created by CombatArt (Hud.cs:416 `SetAsLastSibling`), so the gold number draws on top. Reordering changes overlap visuals.
- **H-2 `DefaultExecutionOrder` chain**: ArtDepthSorting (0) sets sortingOrder → UnitAnimation (100) LateUpdate grounding reads it (UnitAnimation.cs:6,125) → PhotoShadow (200) → ObjectEmphasis (450) → SelectionOutline (500) → UpgradeFeedback/RageOutline (600, they read the union after emphasis). Changing or removing these attributes, or merging these components, changes the frame result.
- **H-3 Re-entrancy contract inside Tick**: after every external callback Session re-checks `disposed` (Session.cs:478,487,716,796,805; 580) and wraps callbacks with `rangeIndex.Invalidate(); supportCache.Dirty=true` (715,793-794,802-803). DamageApplied removals are flushed before the observer runs (517-525). A refactor that extracts these loops must keep them.
- **H-4 Poll-based phase-change detection** captures `phase` before Tick (Prototype.cs:141) and compares at 146/152/184; the HUD compares in Refresh (Hud.cs:508, Inspector.cs:184). Moving the capture, or making the HUD event-driven, changes when install cancel, pointer block and notifications happen.
- **H-5 The only Domain-Reload-OFF protections are the four `SubsystemRegistration` methods and three static-ctor `playModeStateChanged` hooks** (§4). Renaming, moving or de-staticizing them silently reintroduces cross-Play leakage.
- **H-6 Modal gating is structural** (`GetComponentsInChildren<Button>(true)` at Hud.cs:530). Every new Button needs a refresh restore, or it ends up like B-01.
- **H-7 Pool-return sequence** Prototype.cs:475-501: ReleaseEnemy → ReleaseTarget×2 → SetActive(false) (triggers OnDisable restores) → ResetEnemyForReuse → reparent. The order is load-bearing (see B-08, B-09).

## 6. Negative results (checked clean)

- Every Session event subscription has a matching unsubscribe on destroy; CombatAudio also on disable, with re-subscribe on enable (83-86). Unsubscribing after Session.Dispose is harmless (`Session` property is not nulled). EffectsView/CombatArt/CombatAnimation/HUD guard `game!=null&&game.Session!=null`.
- RenderPipelineManager instance subscriptions are paired (ObjectEmphasis OnEnable/OnDisable). Static-ctor subscriptions run once per domain, so there is no double subscription across Plays.
- Coroutines: AugmentationView stops all coroutines and clears Busy/scale/sparks in OnDisable (67). Card destruction on reroll goes through SetActive(false)→OnDisable (Hud.cs:151).
- Pool field completeness: ProjectileState.Clear covers all fields (ModelPooling.cs:25-29). EnemyState.Clear covers all except PoolKind (B-15, harmless). HitFlash.ResetForReuse resets ramp/active/engaged/age/cooldown (HitFlash.cs:97-99). OriginalWalk/FrameWalk reuse resets phase/facing/flip/position. Projectile view rent resets sprite/name/color/flip/order/pos/rot/width/sibling (Prototype.cs:596-601). Damage numbers reset world/time/lane/text/color/icon/scale/rotation/sibling (Hud.cs:412-416), and core/gold-specific alignment/pivot/size live in separate per-kind pools (419). CombatArt effect reuse (84-91), EffectsView Rent (52-53) and muzzle flash reuse (CombatAnimation.cs:158-165) are complete.
- Model handles: generation-checked (ModelPooling.cs:36-38). Pending slots are cleared only in `Flush` at operation depth 0 (SessionModels.cs:19-35). Projectile target snapshot is preserved before clear (28).
- Timers: UI timers use unscaled time; combat presentation uses scaled time, consistent with debug pause (Time.timeScale 0) freezing combat visuals. timeScale is restored on Restart and OnDestroy.
- SegmentedEnemyHealthBar Awake/OnDestroy counting is balanced, including pooled bars (bars live under the pool root and are not destroyed).
- Static `CoreGuardianSpriteBatchCompat`/`ObjectEmphasis` use `-=` then `+=` (idempotent) for Application.quitting.

## 7. Verification data notes

- B-01 only surfaces after the first augmentation screen. Tests never touch AugmentMore.
- B-02 is invisible unless augments 10/11/18 are held.
- B-03: the same-row draw order seen in screenshots depends on how many ArtDepthSorting instances have ticked since the last domain reload. Restart Unity before any before/after visual comparison.

Counts: P0 0 · P1 0 · P2 2 · P3 14 (B-03…B-16) · hazards 7.
