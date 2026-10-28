# Perspective A: data flow and contracts

Scope: every Runtime and Editor .cs file, the 12 CoreGuardian shaders and the 2 test fixture shaders, the 9 CSVs in `Assets/Data/Balance/Resources/CoreGuardian`, and the combat scene's CSV references. Tests were read only where a name was in question.
Source was read-only. All line numbers are file-local: `ModelPooling.cs` is cited by its own line numbers, not the numbers from a concatenated cat.

Blocked (not verifiable from this copy):
- No `*.csv.meta` exists, so the scene TextAsset GUIDs (`CoreGuardian_CombatPrototype_20261008.unity:18493-18497`) cannot be mapped to a specific CSV file.
- `Assets/Audio/**` is absent: `catalog.json` is listed in `ASSET_LIST.txt:1209` but is not on disk here. The cue-id-to-clip mapping is therefore unverified.

---

## 0. Enumeration

**CSV tables, with their loader:**
- `enemies.csv`: CombatData.cs:274-303
- `towers.csv`: :305-320
- `upgrades.csv`: :322-329
- `waves.csv`: :331-339
- `stage.csv`: :260-262 and ApplyStage :158-176
- `player.csv`: :265-270
- `effectTags.csv`: :347-355
- `skills.csv`: :356-364
- `augments.csv`: :365-378
- Legacy path: :137-157 (tests only; selected when `IsNumericTable` at :122 is false)

**Lookups by key:**
- `Data.Setting`: Session.cs:307,326-327,611,614-615,626-628; Session.Debug.cs:56-57; CoreGuardianPrototype.cs:305-307
- `Data.Tag`: Session.Effects.cs:33-38; Hud.Redesign.cs:150-151
- `EnemyForWave`: CombatData.cs:85; Session.cs:402; Session.Effects.cs:311,331; Session.Debug.cs:202,207

**Session public events:**
- `DamageApplied` (Session.cs:14)
- `ProjectileLaunched` (:16)
- `AttackLaunched` (:18)
- `DefeatReached`, `CombatStarted`, `RoundCompleted`, `VictoryReached` (:19-22)
- `Cue`, `TowerPulsed`, `AreaBlastUsed` (Session.Effects.cs:14-16)

**Subscribers:**
- `DamageApplied`: CombatAudio.cs:265 (first, via Prototype.cs:111), Hud.cs:115, CombatArt.cs:30, CombatAnimation.cs:42, EffectsView.cs:148
- `ProjectileLaunched`: CombatAudio.cs:265, CombatAnimation.cs:41
- `AttackLaunched`, `DefeatReached`, `CombatStarted`, `RoundCompleted`, `VictoryReached`: CombatAudio.cs:265 only
- `Cue`: CombatAudio.cs:265, EffectsView.cs:148
- `TowerPulsed`: EffectsView.cs:148, CombatAnimation.cs:43
- `AreaBlastUsed`: EffectsView.cs:148
- Non-Session events: `Prototype.Changed` → Hud.cs:115; `PrototypeGridMovement.Stepped` → Prototype.cs:108

**Session public API, with its runtime callers:**
- Tick: Prototype:143
- StartCombat: Prototype:385, Session:427, Debug:36
- TryInstall: Prototype:289
- DestroyTower: Prototype:333
- UpgradePlayer, UpgradeTower, UpgradeTowerKind: Prototype:339,346,359
- UseAreaBlast, UseTeleportBlast: Prototype:370,380
- ChooseAugmentation: Hud:161, Session:426
- RerollAugmentation: Hud:134
- NotifyPlayerStep: Prototype:108
- SetBoard, SetFlyingRoute, SetStructures: Prototype:105,106,310
- BeginTowerViews, EndTowerViews: Prototype:443,456
- TowerTargetForView: CombatAnimation:77
- PlayerTarget: CombatAnimation:92
- FindTower: CombatAnimation:51,133
- TryTowerSourceId: CombatAnimation:130
- TowerFromSource: CombatAudio:281,297
- HasTowerSupport: Prototype:451
- HitAffinity, HitCritical: Hud:383-384
- FireCellEnumerator: EffectsView:252
- Debug* methods: Hud.Cheats:17-38
- Tests only: TowerTarget(int), TryRead, TrySetHealth, TryUpdate, DebugProjectile and similar, EnemyPool, ProjectilePool, TagValue, NumericIds

**Shader property names set from C#:**
- SelectionOutline.cs:19-21,179,223,264-265,321-323
- MaskProducerBatch.cs:48-49,115-116,145-146
- MaskOutputBatch.cs:234,287,303-305
- SharedOutline.cs:20-24,184,195-196,200,212,269
- HitFlash.cs:163,232-233
- CoreFeedback.cs:261,297
- CoreAmbient.cs:7,38,55,68
- TowerGlow.cs:111,133,148
- TowerGrounding.cs:210-211
- UpgradeFeedback.cs:13,53-54
- SpriteBlocks.cs:21-27
- SpriteBatchCompat.cs:273-275
- PlacementOverlay.cs:339-340
- RouteMeshBake.cs:385-387
- ObjectEmphasis.cs:77,102

---

## 1. CSV column inventory (defined / written / read)

Effective value = the current CSV value unless stated otherwise.

**enemies.csv** (28 columns; all parsed at CombatData.cs:284-291)
- **Live in normal play:** coreDamage, buffRadius, buffSpeed, splitInto, healInterval, healRadius, healPercent, flying, rageAt, rageSpeed, shieldAt, shieldDefense, shieldSeconds, summonInto, summonInterval, summonRageInterval, weakTower, resistTower, and status (validation only).
- **speed:** live for SplitChild (base 1.3 × wave growth, Session.Effects.cs:319-322) and for Attack (used as the growth denominator).
- **health, gold, defense:** overwritten for every scheduled row by `WaveEnemy` (CombatData.cs:185, :338).
  - Base values are reached only through validation and debug spawns (Session.Debug.cs:88-97).
  - Example: Boss base 500/0/120 versus wave 10's 500/5/120. Base Boss defense 0 is never used in play.
- **healthMul, goldMul:** live only for SplitChild (0.4 and 0.5, Session.Effects.cs:320-321). For every other kind they compute base specs that the waves table then overwrites.
- **defenseRef:** dead in normal play.
  - The Splitter/Healer/Armored references are overwritten by waves.
  - SplitChild's own `11001*1` is ignored because the child takes its parent's defense (Session.Effects.cs:322).
  - It is read only by DebugRoundSpec (Session.Debug.cs:95-96).

**towers.csv** (20 columns)
- **Read:** id, key, cost, damage, rate, range, projectileSpeed, buffDamage, slowPercent, slowDuration, burnPerStack, burnDuration, pierceFalloff, targetPriority, targetTiebreak, and status (validation).
- **Dead (written at CombatData.cs:313-314, zero reads repo-wide):** tag, attackType, requires.
  - The behaviours these columns describe are hardcoded instead: Support requiring an attack tower (Session.cs:292), the Frost pulse by kind (Session.cs:485), and Rail lines by kind (Session.cs:691).
- **maxTargets:** read only by the HUD (Hud.Redesign.cs:116,185). Combat hardcodes 3 (Session.cs:166,763).

**upgrades.csv:** all columns are read for tower rows. For the Player rows 21001-21004, cost=100 and costIncrement=0 are dead: the player price comes from stage values (Session.cs:343 → CombatData.cs:195).
- Effective player price for steps 1, 2, 3: 40, 65, 85, from 50+(s-1)·15+5·(s-1)(s-2)/2−10·[s=1].
- Effective tower price: 60+30·step.

**waves.csv:** all columns are read. `interval` has no effect: all 1946 rows have count=1, so a spawn always happens at `start` (Session.cs:434), and interval is only checked to be greater than 0 (CombatData.cs:193).

**stage.csv**
- **Fallback-only or dead:**
  - `waveReward`=40: all 30 `waveRewardN` keys are present (CombatData.cs:80).
  - `augmentationEveryLate`=5: needs CompletedWave>30, which LastWave=30 never allows (Session.cs:608 returns before :611).
  - `augmentGradePlatinum`=10: used only in the sum-to-100 validation (CombatData.cs:377). Platinum is the remainder in `RollGrade` (Augmentations.cs:25).
- **Live:** every other key.
- **Hardcoded fallbacks equal the current values:** starDamage2/3 (1.8/3.2), starRange2/3 (.5/1), refundPercent 50, augmentCards 3, augmentRerolls 1, augmentSeconds 30, Silver/Gold grade chances 60/30, subcategory weight 25, building positions 2,1 / 35,1 / 2.

**player.csv:** all columns read.

**skills.csv**
- **Read:** damageMul 4, radius 2, cooldown 12, resetOnRoundStart 1.
- **Dead:** key, input, phase, android (written at CombatData.cs:362, never read).
  - Space is hardcoded at Prototype.cs:156, and the Combat-phase gate at Session.Effects.cs:421.
  - The skill is chosen by position (`Skills[0]`, Session.Effects.cs:416).

**effectTags.csv:** all 16 (tag, param) pairs requested by TagCache exist, so no fallback is currently used.
- **Dead:** the `unit` column (CombatData.cs:353).
- **Value never read:**
  - Row 31102 (Slow effectScale, value 1): non-boss scale is hardcoded to 1 (Session.Effects.cs:72).
  - Row 31701 (Stun durationScale, value 1): hardcoded to 1 (:131).
- **bossValue "immune" never read:** row 31501 (Execute). TagCache reads only the non-boss value (:36), and Strike hardcodes `!IsBoss` (:196).

**augments.csv**
- **Read:** id (as Number=id−22000), name, grade, subcategory (draw weights), value1-3, repeatable, text.
- **Dead:** category and personality (CombatData.cs:372-373).
- **effect:** written to `AugmentSpec.Effect` (:373) but read nowhere. Every effect is bound by card number (Session.cs:656-664, Augmentations.cs:82-109, Session.Effects.cs throughout).
- **icon:** never parsed. The HUD rebuilds the name from Number (Hud.Redesign.cs:249). It matches today only by naming convention.

---

## 2. Findings

Format per finding: `P-level | conclusion | file:line | trigger path / evidence`.

### P2

- **P2 | Losing core energy to augments 48 (GoldenDeal) or 50 (GlassCannon) is presented as core damage: hit effect, core-damage sound, and a "코어 에너지 -N" notice | CoreGuardianPrototype.cs:150; Session.cs:662-663**
  - Trigger path: an augmentation screen offers card 50, the core is at 20/20 (stage `coreEnergy` = 20), and the player picks it (Hud.cs:161, or the timeout auto-pick at Session.cs:426).
  - Mechanism: `CoreMaximum` becomes ceil(20·50/100)=10, and `CoreEnergy=Min(20,10)` = 10. On the next Update, `CoreEnergy<previousCore` (Prototype.cs:150) calls `combatArt.CoreDamaged(10)`, `NotifyCoreDamage(10)` and `Notify("코어 에너지 -10")`. Card 48 gives the same result with −5.
  - Root cause: the presentation infers "core damaged" from a change in an authoritative value. It is not driven by an event.
  - `coreDamagedThisRound` is correctly left unchanged (domain side), so only presentation is wrong.

- **P2 | The floating gold number on a kill shows the base `Spec.Gold`, not the gold actually awarded, whenever kill-gold augments 10, 11 or 18 are active | CoreGuardianCombatArt.cs:69 vs Session.Effects.cs:243 (also Hud.Redesign.cs:137)**
  - Trigger path: the event handler (`OnDamage` at CombatArt.cs:62, receiving the DamageApplied snapshot) runs before `OnKilled` (Session.Effects.cs:221-222). It reads `enemy.Spec.Gold`.
  - The award is `floor(Gold·(1+KillGoldPercent/100)+.5)`.
  - Example: with 18 (GoldLoan, −20%) active, a 3-gold kill awards floor(2.4+.5)=2 but displays "+3". With 10 (+10%), a 6-gold kill awards 7 but displays "+6".
  - The status-bar message (Prototype.cs:148) uses the real gold difference, so the two displays disagree.

- **P2 | "Future" cards 11 and 36 count progress from the start of the run, not from when the card was picked. Cards 9, 31 and 32 count only from the pick | Augmentations.cs:91,101; Session.cs:604; Session.Effects.cs:246**
  - Mechanism: `CleanRounds++` (Session.cs:604) and `Kills++` (Session.Effects.cs:246) run unconditionally, whether or not the player holds card 36 or 11.
  - Example: pick card 36 at round 27 after 26 clean rounds, and it immediately gives +26% tower damage. Card text: "코어 피해 없이 끝난 라운드마다 모든 포탑 피해 +1%".
  - By contrast, `Rounds32` advances only while card 32 is held (Session.cs:600), and card 9 adds per kill after the pick (:248).
  - This affects balance; it needs a design decision, and it is reported, not fixed.

- **P2 | Frost slowPercent and Rail pierceFalloff upgrades can be bought past their clamp. Each extra purchase spends crystals for no effect, and the lab label shows the unclamped value | Session.cs:333-337,346,363-377; CoreGuardianHud.Inspector.cs:121,130; Session.cs:348-352**
  - Current values: Frost slow 30 with +3 per step (upgrades.csv 21405), capped at `Slow.maxPercent` 60 (effectTags 31101; ClampTowerStats and ApplyFrostSlow at Session.Effects.cs:88). Rail falloff 0.8 with +0.02 per step (21605), capped at 1.
  - Trigger path: the 11th upgrade of either stat. Its price is 60+30·10 = 360 crystals.
  - Neither `LabCanUpgrade` (Inspector.cs:130) nor `UpgradeTowerKind` (Session.cs:366) checks the cap.
  - `TowerKindStats` (used for the label) never clamps, so the label shows "63 → 66".

### P3

**Data contracts and dead data**

- **P3 | The augments `effect` column is the semantic key in the data, but the code binds every effect to the card's numeric ID | CombatData.cs:373; Session.cs:656; Augmentations.cs:82-109**
  - Refactor and data hazard: reordering or renumbering rows silently re-binds effects.
  - `icon` has the same coupling (Hud.Redesign.cs:249).

- **P3 | Several CSV columns are dead, or are shadowed by hardcoded behaviour, so editing them changes nothing (or only the HUD) | see section 1**
  - Columns: towers tag/attackType/requires/maxTargets (combat hardcodes 3 targets at Session.cs:166,763); skills key/input/phase/android; augments category/personality; effectTags unit, plus 31102 value, 31701 value and 31501 bossValue "immune"; upgrades cost/costIncrement for the Player rows; waves interval; stage waveReward, augmentationEveryLate, augmentGradePlatinum.
  - The current data matches the hardcoded behaviour (maxTargets=3, Support requires an attack tower, and so on). All of this is masked.

- **P3 | Most of the base enemy stats in enemies.csv (health/gold/defense/defenseRef, and healthMul/goldMul for non-children) do nothing in normal play; only debug spawns read them | CombatData.cs:185,338; Session.Debug.cs:88-97**
  - The waves table carries baked per-wave stats.
  - Edits to these base stats affect only debug spawns, so a data editor can be misled.

- **P3 | Two config lookups fail silently | CombatData.cs:91,96-99,161**
  - `Setting(key, fallback)` and `Tag(..., fallback)` return hardcoded defaults on a missing key.
  - ApplyStage drops any value that does not parse as a float (`if(TryParse)` with no else, :161).
  - Every fallback equals the current CSV value, so a renamed or deleted row would go unnoticed.
  - `TagValue` (:102), which throws, has no runtime caller.

- **P3 | When CSV loading throws, the game disables itself with only a console log, and nothing on screen says why | CoreGuardianPrototype.cs:98-125**
  - Load happens at :103, before the HUD exists (:114). The catch at :125 sets `Feedback` and `enabled=false`, but no HUD exists to show the message.

- **P3 | Split children always take the parent's defense and ignore their own defenseRef. The debug spawn code follows defenseRef, so the two disagree | Session.Effects.cs:322 vs Session.Debug.cs:95-96, CombatData.cs:302**
  - Masked: in every wave, Splitter defense equals Attack defense (both 0, 0.35, …, 8.75; checked across waves 7-30).

- **P3 | Several lookups return the first match, which is safe only because the current data is uniform | CombatData.cs:85; Session.Effects.cs:311,315,331**
  - `EnemyForWave` returns the first wave row of a kind.
  - `SplitChildSpec` and `SummonSpec` cache per (wave, parent Id), so the first parent's stats win.
  - Masked: every (wave, enemy) pair has identical stats across its rows (0 of 1946 rows differ).

- **P3 | The heal ability and the summon ability share `AbilityTimer` | Session.Effects.cs:356,375**
  - An enemy with both would advance the timer twice per tick.
  - Masked: no current row has both.

- **P3 | Defense resolution depends on row order: if a defenseRef target also lacks explicit defense, its unresolved −1 would be multiplied | CombatData.cs:302**
  - Masked: the current refs (11001, 11002) have explicit defense.

**Write-only fields**

- **P3 | Several fields are written but never read, or never written at all | various**
  - `HitPayload.ExtraLine` (ModelPooling.cs:110): never written or read.
  - `HitPayload.Skill`: written at Session.Effects.cs:427,453, never read.
  - `AugmentationEffects.RoundsSince12`: never used.
  - `PlayerKills` and `BossKills` (Augmentations.cs:75; Session.Effects.cs:247-248): write-only.
  - `TowerState.Stats.UpgradeSteps` (Session.cs:300,351,377): write-only. Only the Player's steps are read (:342); `towerUpgradeSteps` is authoritative.
  - `CombatData.NumericIds`: read by tests only.

- **P3 | `CombatStats.Clone()` does not copy `UpgradeSteps` | CombatData.cs:53**
  - No impact today: only `Data.Player` (empty steps) is cloned (Session.cs:258,397).
  - Refactor hazard if a clone of a live player or tower stat block is ever introduced.

**Augment card semantics** (design confirmation needed)

- **P3 | Repeatable cards 3, 7, 13 and 15 add nothing when picked a second time: their values are averaged (`Value/Count`), not summed | Session.cs:681,683,307; Session.Effects.cs:76**
  - This only holds once a duplicate can be offered. In normal play the deck never re-offers a picked card (Augmentations.cs:43, pass 0), and at most 9 augmentation screens occur (rounds 3-27). Silver always has at least 18−9−4 = 5 unseen cards, so a duplicate is reachable only through `DebugGrantAugmentation`.

- **P3 | Card 29 (CritNoPierceFalloff) applies only to Rail payloads, not to the player's pierce from card 5. The card text ("치명타가 나면 관통 감쇠 없음") is not rail-specific | Session.cs:691 vs :682; :579**
  - Needs a design decision.

- **P3 | Card 27 gives an execute threshold to every damage source on slowed enemies, including towers (threshold 0 becomes 5), not just "+5%p" on an existing execute | Session.Effects.cs:195**
  - Needs a design decision.

**Presentation flags and labels**

- **P3 | Damage numbers for execute kills never show crit or weak/resist styling | Session.Effects.cs:185 vs :199**
  - The execute's `DealDamage` runs after the `finally` that resets `hitAffinity` and `hitCritical`.
  - The reset discipline is otherwise correct: every write goes through try/finally (:184-185, :122).

- **P3 | Burn damage can be credited to the wrong tower, and two damage sources show raw English labels in the Korean damage panel | Session.Effects.cs:101,261,267,411; CoreGuardianHud.cs:298**
  - `AddBurn(source=null)` (cards 22, 28, 40) keeps a previous `BurnSource`, so later burn ticks are credited to an old (possibly demolished) tower.
  - Fresh unsourced burns and Shatter appear in the damage panel as the raw keys "Burn" and "Shatter", with the Single-tower icon (Hud.cs:274).

- **P3 | The HUD recomputes several domain numbers its own way, and its results differ from what combat uses | Hud.Redesign.cs:113-116,135; Augmentations.cs:92; Prototype.cs:451; ModelPooling.cs:52; Session.cs:483-484**
  - The DPS display omits the rate percent (cards 16, 30), the damage-increase percent (21, 37, 38, 50), affinity and crit.
  - `TowerBuffPercent` rounds the value that combat uses unrounded. The buff marker uses this rounded value (masked: all card values are integers today).
  - Enemy move speed excludes the Buff aura (applied separately at Session.cs:471).
  - Lab labels show unscaled deltas, while ★2/★3 towers receive delta × starDamage (Session.cs:377).

- **P3 | `Session.Dispose` nulls 7 events but not `Cue`, `TowerPulsed` or `AreaBlastUsed` | SessionModels.cs:131**
  - Harmless today (the session is dead), but this is inconsistent cleanup.

- **P3 | Cues are silently dropped once 256 are pending | Session.Effects.cs:19**
  - Also, cues raised outside Tick are delivered on the next Tick (one frame late). Example: star_merge from TryInstall (Session.cs:322).

**Mutable shared state**

- **P3 | Authoritative state is publicly mutable | Session.cs:11,13; CombatData.cs:67-78,444; Augmentations.cs:75-76**
  - `Towers` (List), `DamageTotals`, every CombatData table, `TowerState` and `CombatStats` fields, and the augment counters.
  - No presentation code writes them today: a grep for setters found none, and presentation only calls Session commands.
  - The developer cheat panel (Hud.Cheats.cs:17-38) is always built and mutates state through `Debug*` methods. This is fine for a single-player game, but it is noted.

**Shader contracts and refactor hazards**

- **P3 | `_Width` is declared as `Int` in Properties and as `int` in HLSL, and is set with float-backed `SetInt` | SelectionOutline.shader:3,18; SharedOutlineOutput.shader:8,25; SelectionOutline.cs:179; SharedOutline.cs:200**
  - It works today. Hazard: "modernizing" to `SetInteger` without switching the property to `Integer` breaks the width.

- **P3 | Global-only uniforms must stay out of Properties blocks | SharedOutlineMask.shader:9-10; SharedOutlineOutput.shader:22-23**
  - Uniforms: `_CGSharedOutlineVP`, `_CGSharedOutlineThreshold`, `_CGMaskEntity`, `_CGMaskDepth`, `_CGSharedOutlineMask`, `_CGSharedOutlineLayout`.
  - They are set only as globals (SharedOutline.cs:184,195-196,223-224). Adding one to a material Properties block would shadow the global.

- **P3 | `_MainTex_TexelSize` is set by hand on the property block | SelectionOutline.cs:264**
  - SelectionOutline.shader:28 and PhotoShadowContact.shader:24 depend on it (`.w` = mask height).
  - The batched twin uses `sourceSize` (MaskOutputBatch.cs:304,347) for the same value. Removing either breaks contact-band parity.

- **P3 | The UpgradeScan band half-width (.06) is defined twice | UpgradeScan.shader:3; UpgradeFeedback.cs:53**
  - It is the shader default for `_BandWidth`, which C# never sets, and C# separately hardcodes the sweep margin as ±.06.

- **P3 | The `BatchedMaskOutput` path runs only in Edit Mode | SelectionOutline.cs:231,240**
  - `RegisterOutput` returns when `Application.isPlaying`, so this path is reachable only in Edit Mode tests. The play-mode guard at :240 is unreachable.
  - `BodyHitFlash` declares `_RendererColor` (BodyHitFlash.shader:7) but its HLSL never uses it.

**Assets and loading**

- **P3 | No code emits the `stun` or `teleport` cues, although Slice clips exist for them (`ASSET_LIST.txt` Slice/stun.wav, Slice/teleport.wav) | Session.Effects.cs:12-13 cue list**
  - Freeze raises "freeze" (:206). Teleport plays visuals only (Prototype.cs:378).
  - Unverified because `catalog.json` is absent.

- **P3 | Data arrives through two channels: 5 tables as scene-serialized TextAssets and 4 through Resources | CoreGuardianPrototype.cs:16,103; CombatData.cs:134**
  - The legacy path pulls live Resources sheets into test fixtures (CombatData.cs:155).
  - The legacy parser splits naively on commas, while the table parser handles quotes (`SplitCsv`) (:114 vs :230).
  - Scene GUID-to-file mapping is unverifiable (no .csv.meta).

---

## 3. Negative results (checked and clean)

**Data tables**
- Every column the loader requires exists in its CSV: compared the required lists at CombatData.cs:260,267,274,305,322,331,349,358,367 against each CSV header.
- Every optional column the loader reads (healthMul, …, group, input, android, and so on) exists.
- Every ID reference resolves:
  - targetPriority IDs 11001-11201 (all present)
  - weakTower/resistTower IDs 12001-12005
  - upgrade targets 10001, 12001-12005, 12101
  - splitInto 11201 is a SplitChild
  - summonInto 11004 and defenseRef 11001/11002 exist
  - no wave schedules a SplitChild
- Augments: 50 cards (18/20/12); grade chances sum to 100.
- All 50 card numbers have code bindings.
- Every `Data.Setting` key used in code exists in stage.csv.

**Damage pipeline order** (Session.Effects.cs:178-209)
1. Launch damage × (1 + buff%) × crit (× chain or pierce falloff)
2. × (1 + increase%) × affinity
3. Defense rounding: `floor(max(0, d·(1−def/100)) + .5)`
4. Health change
5. Shield check
6. DamageApplied
7. OnKilled
8. On-hit burn and slow (only if still alive)
9. Execute
10. Freeze

Burn ticks apply increase × Flame affinity × defense·(1−50%).

Affinity bit encoding is consistent between the loader (CombatData.cs:247) and combat (Session.Effects.cs:172; payload `Tower = kind+1` at Session.cs:677).

**Event payloads**
- The DamageApplied snapshot is taken after the health change and before kill side effects (gold, splits). This matches all 5 subscribers:
  - death detection via Health≤0: CombatArt:67, CombatAudio:356
  - rage-transition detection: CombatAudio:361
  - hit reaction skipped on death: CombatAnimation:84
- AttackLaunched fires once per volley, after all projectiles exist (Session.cs:799-804, :715).
- ProjectileSnapshot is taken after Line conversion (Session.cs:784-794).

**Handles and views**
- Handles check generation (ModelPooling.cs:36-38).
- View dictionaries are keyed by monotonic enemy and projectile IDs, which are never reused (SessionModels.cs:63,74).
- Every presentation read of a stored handle is guarded by `.Alive`: CombatAnimation:78,93; Hud.Redesign:43,83; Inspector:39.

**Ownership of state**
- No presentation file writes Session state directly.

**Shaders**
- Every property name C# sets exists in its target shader, and every non-default shader property is set by its owner. Pairs checked:
  - SelectionMask ↔ SelectionOutline.cs:321-323 and MaskProducerBatch:146
  - BatchedSelectionMask ↔ MaskProducerBatch:115-116
  - BatchedMaskOutput ↔ MaskOutputBatch:303-305
  - SelectionOutline/PhotoShadowContact ↔ SelectionOutline.cs:179,223,263-265
  - SharedOutlineMask/Output ↔ SharedOutline.cs
  - BodyHitFlash ↔ HitFlash/CoreFeedback
  - CoreAmbient, TowerGlow, TowerBaseShade, UpgradeScan ↔ their owners
- GPU struct strides match:
  - Item: 10×float4 = 160 bytes (MaskProducerBatch.cs:110 vs BatchedSelectionMask.shader:16)
  - PartItem: 80 bytes (SharedOutline.cs:30 vs SharedOutlineMask.shader:47)
  - UV channel layouts match their shader input semantics.
- All 12 `Resources.Load<Shader>("CoreGuardian/X")` paths exist under Shaders/Resources/CoreGuardian.

---

## 4. Shared root-cause consolidation

1. **Presentation infers domain facts by comparing values instead of receiving events.** Covers the augment-as-core-damage finding and the gold-number finding. Fix: emit an explicit domain event or payload carrying core-damage and awarded-gold amounts.
2. **Data columns duplicate hardcoded behaviour.** Covers dead columns, maxTargets, effect-ID binding, and the effectTags values that are never read. Fix: either read the columns or delete them; the order does not matter.
3. **Two HUD re-implementations of domain math.** Covers DPS, MovementSpeed, gold and lab labels. Fix: expose read-only derived values from the domain.

## 5. Verification-data notes (currently masked)

- Every fallback equals its CSV value. Uniform per-wave stats mask the first-row caches. Splitter defense equals Attack defense.
- No enemy has both heal and summon. maxTargets=3 equals the hardcoded 3. All augment values are integers, so the rounding in `TowerBuffPercent` is masked.

Disposition line: n/a (perspective pass; the final review is not yet applied). Counts: P0 0 / P1 0 / P2 4 / P3 27.
