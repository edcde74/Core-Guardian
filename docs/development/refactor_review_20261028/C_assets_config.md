# Perspective C — asset and configuration evidence

Reviewer: perspective C (read-only). Source: `/home/claude/review/src`. Every line cited below was opened and read.
Line numbers are per file. Scene = `Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity`.

## What the copy lacks (sets the limits of verification)
The copy only has `.cs.meta` (126) and `.shader.meta` (12). It has **no** `.prefab/.png/.csv/.mat/.asset .meta`. `Assets/Settings/**` is missing (UniversalRP.asset, Renderer2D.asset, URP global settings). `ProjectSettings/EditorSettings.asset` is missing. These files are also missing: `catalog.json`, `MaskAtlas/Manifest.asset`, the `.spriteatlasv2` files, and `docs/development/outline_sampling_20261023/`. Anything that depends on them is marked **UNVERIFIED (blocked: not in copy)**.

---

## 0. Enumeration (complete set, names + file:line only)

**E1. Resources/Shader lookups in code (every literal or prefix)**
- Prototype.cs:101 `Animation/Boss/BossAnimated`, :311-312 `Hud/Redesign/H1_turret_lab(_highlight)`, `H2_engineer_bench(_highlight)`, :517 `Units/`+enemy_/tower_+kind, :533 `Animation/Boss/`+Key+`Animated`, :272 Shader.Find `URP/2D/Sprite-Lit-Default`, :612 Shader.Find `URP/2D/Sprite-Unlit-Default`
- CombatData.cs:134 `CoreGuardian/`+player|augments|effectTags|skills
- AudioCatalog.cs:40 `Audio/SelectedB/catalog`, :27 cue.resourcePath (from JSON); CombatAudio.cs:10-23 (14 `*ResourcePath` consts)
- CombatArt.cs:10/28 LoadAll `Art/CombatB` + Names[9] :11; :45 `Art/Towers/Fireball`; :46 lib `Fx/RailBolt`
- CombatAnimation.cs:40 LoadAll CombatB `SingleBolt`; :45 `Art/Status/FreezeRamp`
- EffectsView.cs:38-44 `Art/`+key(+i) then lib `Fx/`+key; keys :72-88,116-139
- UnitAnimation.cs:69-70 `OriginalWalk/Flyer/Hover_0..5`, `VerticalWalk/Flyer/{Up,Down}_0..3`; :74 lib role/`{Left,Right,Up,Down}_0..3`
- OriginalWalk.cs:98 `VerticalWalk/`+role+`{Up_,Down_}0..3`; :108 `OriginalWalk/`+role+`/{Body,LeftUpper,LeftLower,RightUpper,RightLower}`
- OriginalIdle.cs:17 `OriginalIdle/Player/{Feet,Upper}`; OriginalMotion.cs:46-47 `OriginalMotion/`+role+`/{Fixed,Moving}`, :114 `{Fire_,Pulse_}0..15`
- PlayerMotion.cs:30 `PlayerFacing/D0..7/Dn_{Body,Arm,LeftLeg,RightLeg}`
- PhotoShadow.cs:38 `PhotoShadows/registration`, :43 `PhotoShadows/`+source
- Shaders: HitFlash.cs:25, CoreFeedback.cs:23 `BodyHitFlash`; TowerGlow.cs:25; TowerGrounding.cs:38 `TowerBaseShade`; CoreAmbient.cs:24; UpgradeFeedback.cs:36 `UpgradeScan`; SelectionOutline.cs:218 `PhotoShadowContact`, :396-397 `SelectionMask`,`SelectionOutline`; MaskProducerBatch.cs:66,72 `SelectionMask`,`BatchedSelectionMask`; MaskOutputBatch.cs:122,144 `BatchedMaskOutput`; SharedOutline.cs:313-314 `SharedOutlineMask`,`SharedOutlineOutput`
- SelectionOutline.cs:385 `MaskAtlas/Manifest`; SpriteAtlasSources.cs:22 `SpriteAtlases/Sources`; SpriteLibrary.cs:9 `SpriteLibrary`
- HUD: Hud.cs:97-101 fonts (Galmuri11, -Bold, PixelifySansBoldDamage, …Outline), :243 LoadAll `Hud/DamageRoles`, :246 `Hud/`+name; HudSkin.cs:11 `Hud/`+name; AugmentationView.cs:26; PerfOverlay.cs:192
- Editor: ContentAssets.cs:11-23,55-58; SpriteAtlasPipeline.cs:15-21,88; RouteMeshBuilder.cs:12-14

**E2. Scene MonoBehaviours (guid→class)**: CoreGuardianPrototype (ee97…), CoreGuardianSelectionOutline (9269…), CoreGuardianCoreParticleSettings (c74e…), CoreGuardianUpgradeFeedback (54b1…), CoreGuardianObjectEmphasis (9421…), all on `CoreGuardian_Gameplay`. PrototypeGridMovement (09b4…) on `D_InsideCore_Prototype/PlayerStart_Prototype`. CoreGuardianRouteMeshBake (0484…) on `CoreGuardian_MapArt_B/CoreGuardian_Route01_VisualOnly`. Package scripts: UniversalAdditionalCameraData, Light2D, Volume. Prefab instances: Player.prefab, core.prefab.
**E3. Prefab MonoBehaviours**: ArtDepthSorting (field `artwork`) on all 21 prefabs. No other scripts.
**E4. ScriptableObject assets**: SpriteLibrary.asset (CoreGuardianSpriteLibrary), SpriteAtlases/Sources.asset (CoreGuardianSpriteAtlasSources), MaskAtlas/Manifest.asset (not in copy). ContactShapes/*.asset, BossFeetCenteredV2.asset and BossEyeContact.asset are Sprite assets, not scripts.
**E5. Shaders (12 + 2)**: BatchedMaskOutput, BatchedSelectionMask, BodyHitFlash, CoreAmbient, PhotoShadowContact, SelectionMask, SelectionOutline, SharedOutlineMask, SharedOutlineOutput, TowerBaseShade, TowerGlow, UpgradeScan; fixtures SelectionOutlineReference, SelectionOutlineCandidate.
**E6. Materials**: 12 Environment .mat (shader guid 13c02b14…), 11 MagicAura .mat (built-in fileID 46/200), 3 TMP .mat. **No .mat references any of our 12 shaders.** All of their materials are created at runtime.
**E7. ProjectSettings read**: ProjectSettings.asset, GraphicsSettings.asset, QualitySettings.asset, EditorBuildSettings.asset; Packages/manifest.json; asmdefs (4).

---

## 1. Findings (defects), sorted by severity

P0: none. P1: none.

**P2 | The sprite-atlas build hook runs on every player build. It can throw and abort the build, and it rewrites importer settings and assets during the build. | Editor/Rendering/CoreGuardianSpriteAtlasPipeline.cs:13,23-28,32-33,51,58,80,97 | Trigger path: any BuildPipeline build → `OnPreprocessBuild` (:23) → `Synchronize(false,…)` (:27).**
- It throws when `EditorSettings.spritePackerMode != SpriteAtlasV2` (:33).
- It throws "Duplicate sprite ownership" (:51) when any other atlas packs the same textures. The asset list contains a fifth, legacy `Assets/Art/Atlases/Sprites/CG_Enemies.spriteatlasv2` that is not in `Groups` (:18-22). It is scanned by :35/:44, and its contents are not in the copy.
- It throws "Source alpha changed; rebake" (:97) whenever a mask source texture's alpha changed.
- Side effects on every build: it forces `spriteMeshType=FullRect` with SaveAndReimport (:58), rewrites `Sources.asset` (:76), force-reimports every source texture when packing is off (:80), and rewrites MaskAtlas hashes (:98-104).
- Current-value evidence: EditorSettings.asset and the .spriteatlasv2 files are not in the copy, so whether the build is blocked today is UNVERIFIED.
- Consequence when a condition holds: the build fails, or a build silently changes tracked assets. This conflicts with the "모든 변경은 사용자 승인" rule in AGENTS.md:7.

**P3 | Static state survives Play sessions (Domain Reload OFF per AGENTS.md:20) without a reset, so successive Editor Play sessions are not identical. |**
- `ArtDepthSorting.nextLane` (World/ArtDepthSorting.cs:13,27) is never reset, and the component is `[ExecuteAlways]` (:6), so Edit-mode LateUpdates also advance it. The per-unit lane, and with it the same-row tie order of sort values (:21), differs between Play #1 and Play #2 in the Editor (a build always starts at 0).
- `CoreGuardianSpriteLibrary.searched` (Rendering/CoreGuardianSpriteLibrary.cs:14,19): one failed load sticks for the whole Editor session.
- `CoreGuardianSharedOutline.Enabled/Batched` (SharedOutline.cs:27,35) and the public mutable `ReuseGeometry` (:82) keep any value set by an MCP eval across Play sessions. No runtime caller of SetEnabled/SetBatched exists (grep in Runtime/Editor/Tests).
- By contrast, these are reset correctly at SubsystemRegistration: SegmentedEnemyHealthBar.cs:49, SpriteAtlasSources.cs:17-18, ObjectEmphasis.cs:80-81, BodyMaskCache.cs:119-120.
- Impact: before/after pixel or perf comparisons done in the Editor can differ for reasons unrelated to the change.

**P3 | The optimized mask-page path only runs when the camera pixel rect is exactly 1920×1080 and the ortho size is 8.5 or 4.25. The window is resizable and windowed, so any other client size silently falls back to the legacy per-body path. | Rendering/CoreGuardianSelectionOutline.cs:248-251 (QueuePage), :235-238 (RegisterOutput), :124 (localContact) | ProjectSettings.asset:44-45 1920×1080, :104 `resizableWindow: 1`, :116 `fullscreenMode: 3` (Windowed); scene camera `orthographic size: 8.5`, pos (13.5, 7.5, -10).**
- Today's scene satisfies the ortho and position guards. The resolution guard depends on the runtime window size.
- `localContact` (:122-129) is reachable only through `PresentContact`, and `PresentGroundContact*` has no runtime caller (see the next finding). So :124 is dead at runtime.
- This is a performance and measurement-validity issue only. Per the comments the visual result is identical.

**P3 | Two of our shaders cannot be reached at runtime but ship in `Resources`, which means they are always in the build and their variants are compiled. |**
- `PhotoShadowContact.shader`: its only loader is SelectionOutline.cs:218 inside `PresentContact`, which is reached only through `PresentGroundContact`/`PresentGroundContactWithBand` (:205-212). These have 0 callers in Runtime/Editor and 4 in Tests.
- `BatchedMaskOutput.shader`: it is loaded only by `MaskOutputBatch.Flush` when `entries` is non-empty (MaskOutputBatch.cs:108,122,144). Entries come only from `Register` (:76), whose only caller is `RegisterOutput`, and that returns at SelectionOutline.cs:231 `if(Application.isPlaying)return;`. So it runs in Edit mode and tests only. Line :240 `if(Application.isPlaying&&!prepared)` is dead behind :231. The contact branch of BatchedMaskOutput.shader:30-36 is also dead.
- Carriers checked: all .cs (Runtime, Editor, Tests), and no .mat references either shader. Do not delete without approval (REFACTORING_RULES.md:13), because tests depend on both.

**P3 | Resources content with no reference found among the carriers checked. It ships in the build anyway. |**
- Content: `Prefabs/Characters/Boss/Resources/CoreGuardian/Art/BossEye/BossEyePrefab.prefab`; `CoreGuardian/Animation/{Attack,Boss,Buff,Defense}/frames.json` and `{Armored,Charger,Healer,SplitChild,Splitter}/joints.json` (the only related code is the unused DTOs at UnitAnimation.cs:10-11); `Hud/Redesign/H3_augment_icons_sheet.png`; `Audio/SelectedB/Slice/stun.wav` and `teleport.wav` (no code emits `stun`/`teleport` cues; the Session cue list is in §3); SpriteLibrary keys `IronKnight/IronKnight_Body`, `IronKnight_ShieldPlanted`, `CrimsonLord/CrimsonLord_Body`, `CrimsonLord_Crown`, `Fx/Engineer/*`, `Fx/Multi/*`, `Fx/Single/*` (no lookup builds those keys).
- Carriers checked: .cs under Runtime/Editor/Tests, plus the text of every .prefab, .unity and .asset in the copy.
- Not checked (blocked): catalog.json, and GUID references from non-copied .meta/.mat files.
- BossFeetCenteredV2.asset and BossEyeContact.asset are Sprite assets that prefabs might reference by GUID, so they are excluded from this list.

**P3 | MagicAura materials use Built-in pipeline shaders and would render magenta under URP if anything uses them. | Assets/Art/Effects/MagicAura/Materials/Metal Plane.mat (`m_Shader: fileID 46`, Standard); Aura_1/*.mat and Aura_2/*.mat (`fileID 200`). | Usage UNVERIFIED: none are in Resources, and with no .mat.meta the scene's MeshRenderer material GUIDs (65ab…, 0038…, 3275…, 673a…) cannot be mapped.**

**P3 | Eight of our shaders are not SRP-Batcher compatible (material uniforms are outside `CBUFFER_START(UnityPerMaterial)`), and several C# systems are built on the assumption that the SRP Batcher is OFF. Re-enabling it would change behaviour, not just performance. |**
- Shaders with no UnityPerMaterial cbuffer: UpgradeScan.shader:15, PhotoShadowContact.shader:16-18, SelectionMask.shader:19-22, SelectionOutline.shader:16-18, BatchedSelectionMask.shader:20, SharedOutlineMask.shader:9-10, SharedOutlineOutput.shader:22-26, BatchedMaskOutput.shader:17.
- Shaders that are compatible: BodyHitFlash:32-37, TowerGlow:25-28, TowerBaseShade:22-24, CoreAmbient:23-28.
- C# relying on OFF: SpriteBlocks.cs:3-5 (`HasPropertyBlock()` is true for every sprite with the batcher off), SpriteBatchCompat.cs:5,44, ObjectEmphasis.cs:26-27.
- Current value UNVERIFIED (blocked): `m_UseSRPBatcher` lives in `Assets/Settings/Rendering/UniversalRP.asset`, which is not in the copy. GraphicsSettings.asset has no batcher field, and its `m_CustomRenderPipeline: {fileID: 0}` (:49) means URP comes from QualitySettings `customRenderPipeline 681886c5…` on all 6 levels (QualitySettings.asset:51 etc.).

**P3 | `BodyHitFlash` declares `_RendererColor` but never reads it. Its HLSL uses `unity_SpriteColor`. | BodyHitFlash.shader:7 vs :32-37,:44.** The property is harmless and its removal is optional, but any pixel change must be checked first (rule 5).

**P3 | `UpgradeScan._BandWidth` is never written from C#. The effective value is the Properties default `.06`. | UpgradeScan.shader:3,15,22,24; the only writes are UpgradeFeedback.cs:13,53-54 (`_BandY`,`_Opacity`).** Hazard: moving `_BandWidth` into a cbuffer or dropping the Properties default changes the visuals (the value would become 0).

**P3 | The outline pixel test does not cover the production `SelectionOutline.shader`. | Tests/EditMode/CoreGuardianSelectionOutlineShaderTests.cs:10-13,67.**
- The test renders the *Candidate* fixture against the *Reference* fixture.
- `diff Fixtures/SelectionOutlineReference.shader Resources/CoreGuardian/SelectionOutline.shader` differs only in line 1 (the shader name). So a refactor of the production shader can drift without any test failing.
- The test also reads `docs/development/outline_sampling_20261023/phase.txt` (:14-15) and appends CSV files into `docs/` (:70). That folder is not in the copy (UNVERIFIED on the PC).

**P3 | The same shader code is duplicated across files (candidates for a shared .hlsl; any merge needs pixel proof, REFACTORING_RULES.md:40).**
- (a) The contact-band fragment in PhotoShadowContact.shader:24-28 equals BatchedMaskOutput.shader:30-36 (same .55/.21 weights and colour .035/.03/.025).
- (b) The square-kernel outline appears in SelectionOutline.shader:24-29, BatchedMaskOutput.shader:37-39 and SharedOutlineOutput.shader:45-50 (the last uses Load plus an entity compare).
- (c) The mask fragment in SelectionMask.shader:36-39 equals BatchedSelectionMask.shader:42-45.
- (d) The UV-rect plus colour-threshold mask is in TowerGlow.shader:32-37 and CoreAmbient.shader:36-41.
- (e) The vertex boilerplate `TransformObjectToHClip`+uv is repeated in 7 shaders.

**P3 | `SharedOutlineMask` puts a target-4.5 StructuredBuffer pass and its own "no compute" fallback pass in one SubShader. On a device without 4.5 support the fallback may be unusable too. | SharedOutlineMask.shader:35-76 (pass0 `#pragma target 4.5` :46, pass1 target 3.5 :65).** The C# gates only the *batched* path to D3D11/12 (SharedOutline.cs:32). The Standalone build target defaults to D3D (`m_BuildTargetGraphicsAPIs: []`), so this is only latent for the Android preset in QualitySettings.

**P3 | Twin inconsistency: CoreFeedback restores a property block it captured even when the renderer had none. HitFlash restores `null` in that case. | Rendering/CoreGuardianCoreFeedback.cs:30,52 vs CoreGuardianHitFlash.cs:58,93 (HitFlash's own comment at :92 says an empty block changes sprite batching).** This affects the core body only (batching cost, no visual change).

**P3 | The new towers (Frost, Flame, Rail) take the *Support* base metrics in TowerGrounding but the *Single* shadow in PhotoShadow. Their prefabs were cloned from tower_single. | TowerGrounding.cs:18-19 (else → 40 / 23f), PhotoShadow.cs:37 (→ "Single"), Editor/Content/CoreGuardianContentAssets.cs:21.** This may be intended. It is listed only so the user can confirm, and must stay unchanged during the refactor.

**P3 | Shader.Find in a player build requires `Universal Render Pipeline/2D/Sprite-Lit-Default` to be included in the build. If it is excluded, the placement tint is silently disabled with only a warning. | Bootstrap/CoreGuardianPrototype.cs:264-276, :259.** Inclusion depends on built materials. All 244 scene SpriteRenderers use material 889af44c…, which cannot be mapped without .meta (UNVERIFIED). `Ring()` (:608-614, Shader.Find Sprite-Unlit-Default) has 0 callers, and the sprite `dot` (:113) is never used (dead).

**P3 | If the core sprite is ever renamed, ObjectEmphasis would scan every SpriteRenderer each frame. | Rendering/CoreGuardianObjectEmphasis.cs:165-167 (`FindObjectsByType<SpriteRenderer>()` while `core==null`).** The same name contract (`r.sprite.name=="core"`) is in CombatArt.cs:33 and CombatAnimation.cs:37. The scene has `core.prefab` with a `2D Sprite` child (sprite guid c973787b…); the sprite name itself is UNVERIFIED (no .png.meta).

**P3 | Code overrides a project setting at runtime: `Application.runInBackground=true` versus the project value `runInBackground: 0`. | Prototype.cs:97 vs ProjectSettings.asset:90.** It is behaviour-relevant (the game keeps running unfocused), so a refactor must keep it.

---

## 2. Refactoring hazards (names and contracts a refactor must NOT change)

**H1 (high). Scene values differ from code initializers. Renaming the field, or losing the YAML value, silently changes the map or visuals.**

| Field | Code default | Scene value (effective) |
|---|---|---|
| `CoreGuardianPrototype.boardSize` (Prototype.cs:17) | 45×25 | 39×22 (scene :18498) |
| `.coreCell` (:18) | (29,9) | (25,8) |
| `.routeCells` (:19) | (0,12)…(29,9) | (0,10),(10,10),(10,16),(30,16),(30,5),(15,5),(15,10),(25,10),(25,8) |
| `PrototypeGridMovement.spriteOffset` (PrototypeGridMovement.cs:11) | (0,0.11,0) | (0,0,0) (scene :8470) |
| `CoreGuardianSelectionOutline.color` (SelectionOutline.cs:24) | (.8,.67,.39,1) | (0.084,1,0,1) green (scene :18537) |
| `CoreGuardianCoreParticleSettings.sizeMax` (CoreParticleSettings.cs:11) | .045 (tooltip says "기존값 0.045") | 0.1 (scene :18553) |

**H2. Serialized field names present in YAML.** Orphans: none (every YAML field maps to a current field). Code fields missing from YAML: none. `{fileID:0}` in our MonoBehaviours: none. Dangling local fileIDs: 0 in the scene and in all 21 prefabs (script-checked).
- CoreGuardianPrototype: `ground, enemyPath, coreMap, occupied, player, enemyArt[3], towerArt[3], enemiesCsv, towersCsv, stageCsv, upgradesCsv, wavesCsv, boardSize, coreCell, routeCells`
- PrototypeGridMovement: `ground, enemyPath, core, occupiedTowerCells, spriteOffset, stepDuration, movementTileSize, movementTileOrigin`
- CoreGuardianSelectionOutline: `color, widthPixels, alphaThreshold` · CoreParticleSettings: `sizeMin, sizeMax, emissionPerSecond, maxParticles` · UpgradeFeedback: `duration` · ObjectEmphasis: `outlineColor, outlineWidthPixels, bodyBrightness, alphaThreshold`
- CoreGuardianRouteMeshBake: `sources` (244), `outputs` (2) · ArtDepthSorting: `artwork` (21 prefabs, all assigned)
- CoreGuardianSpriteLibrary: `names, sprites` (95/95, no nulls; keys verified below) · CoreGuardianSpriteAtlasSources: `entries{sprite,texture,rect}` (343) · CoreGuardianMaskAtlas: `entries{source,page,layer,dependencyHash}, inputHash`. These are also addressed **by string** through SerializedObject in SpriteAtlasPipeline.cs:89-103.
- JSON contracts read with JsonUtility: CoreGuardianAudioCue `id, category, resourcePath, gain, priority, important, protectCore` (AudioCatalog.cs:9-12); PhotoShadow `Entries.entries{role,x,y}` (PhotoShadow.cs:13-14).
- Script identity: the class names, file names and .cs.meta GUIDs listed in E2/E3/E4. YAML also stores `m_EditorClassIdentifier: Assembly-CSharp::<Class>`.

**H3. Enum order and enum names act as asset keys.**
- `EnemyKind` order is the `enemyArt[]` index (Prototype.cs:522; CombatData.cs:10 comment). `TowerKind` order is the `towerArt[]` index (:527).
- Lowercased names build `Units/enemy_<kind>` and `tower_<kind>` (:523,528).
- `EnemyKind/TowerKind.ToString()` gives the animation and shadow role, which builds the OriginalWalk, VerticalWalk, OriginalMotion and PhotoShadows paths (CombatAnimation.cs:57,76).
- `"Install_"+kind` drives HUD icons (HudSkin.cs:31-35; Hud.cs:188,341).
- CSV `key` strings are parsed into enums (CombatData.cs:281,308).
- `AugmentationGrade` order indexes the colours and the `aug_*` cue array (AugmentationView.cs:14,20,64). Its names build `CardBack`+Grade (:22) and `H4_frame_`+lowercase grade (Hud.Redesign.cs:250).
- `CoreGuardianAudioCategory` names are `Enum.Parse`d from catalog.json `category` (AudioCatalog.cs:23) and form PlayerPrefs keys (SoundSettings.cs:15).

**H4. GameObject and sprite *name* lookups (prefab art contract, verified present in all 21 prefabs).**
- `"2D Sprite"`: UnitAnimation.cs:51, PhotoShadow.cs:35, SelectionOutline.cs:94, ObjectEmphasis.cs:167,186, CombatArt.cs:33, CombatAnimation.cs:37, Prototype.cs:198, ContentAssets.cs:35.
- `"그림자"`: Prototype.cs:251, TowerGrounding.cs:22.
- `"바깥 그림자"`, `"접촉 그림자"`, `"바닥 밀착선"`: TowerGrounding.cs:25-28, PhotoShadow.cs:64, OriginalWalk.cs:91, PlayerMotion.cs:26.
- `"짧은 투사 그림자"` (created by TowerGrounding.cs:31, matched by PhotoShadow.cs:64); `"바닥 수정빛"` (core.prefab, PhotoShadow.cs:64).
- Sprite name `Contains("Shadow")`: OriginalWalk.cs:86, HitFlash.cs:29. Sprite name `"core"` (see above).
- Rig child names `"Fixed"/"Moving"/"Housing*"`: TowerGlow.cs:29 ↔ OriginalMotion.cs:51-64.
- Editor route path `CoreGuardian_MapArt_B/CoreGuardian_Route01_VisualOnly` (RouteMeshBuilder.cs:12): verified to exist in the scene.
- Runtime HUD names drive `Find`, icons and tests: `Label`, `Symbol_Core`, `Status_Core`, `PrototypeActions`, `Reload`, `TestSpeed`, `SoundControls`, `AugmentFold/HandleArt`, `HandleArt`, `Tower_<stat>`, `<stat>` (Hud.cs:233,288-289,399; Inspector.cs:80-86,179; Abilities.cs:41; Sound.cs:44).
- `HudSkin.Symbol(name)` (HudSkin.cs:25-53) picks the icon **from the button's GameObject name** and falls back silently to "Install", so renaming a button silently changes its icon. Button click sounds also key off names (Hud.cs:179 `StartsWith("Preview")`).

**H5. Resources paths.** Every path in E1 resolves in ASSET_LIST (see §3), so no file or folder under any `Resources/CoreGuardian/**` may be renamed. Shader names `CoreGuardian/*` come from Resources-relative paths and their `Shader "…"` names (both match for all 12).

**H6. Shader property names used from C# by string or PropertyToID.**
- `_HitFlashAmount, _HitFlashTint, _RampTex, _RampAmount, _Age, _Clock, _PulseScale, _UVRect, _Role, _BottomUV, _TextureHeight, _BandY, _Opacity, _Strength, _Particles, _MainTex, _MainTex_TexelSize, _MaskOriginPx, _Color, _Width, _Band, _MaskAtlas, _UseAtlas, _Threshold, _MaskVP, _UsePartMatrices, _Items, _PageSize, _CGSharedOutlineMask, _CGSharedOutlineLayout, _CGSharedOutlineVP, _CGSharedOutlineThreshold, _CGMaskEntity, _CGOutlineEntity, _CGMaskDepth, _CGMaskParts, _Single, _Region, _Parameters`
- URP-owned names: `unity_SpriteProps, unity_SpriteColor, _RendererColor` (SpriteBatchCompat.cs:35-37, PlacementOverlay.cs:36-37, RouteMeshBake.cs:14-16, SpriteBlocks.cs:16-19).

**H7. GPU struct, stride and vertex-stream contracts.**
- `PartItem` is 5×Vector4 = 80 B (SharedOutline.cs:30,263) and must match `struct Part` in SharedOutlineMask.shader:47.
- `Item` is 10×Vector4 and uses the **literal** stride 160 (MaskProducerBatch.cs:110, not derived from SizeOf). It must match BatchedSelectionMask.shader:16.
- UV channels: SelectionOutline.cs:273,373-375 UV1-5 ↔ SelectionMask.shader:24 TEXCOORD1-5; MaskOutputBatch.cs:193-200 uv0-7 ↔ BatchedMaskOutput.shader:19 (TEXCOORD3 is zero and unused); SharedOutline.cs:304 UV1 ↔ SharedOutlineMask.shader:49.

**H8. `DefaultExecutionOrder` values:** UnitAnimation 100 (:6), PhotoShadow 200 (:8), ObjectEmphasis 450 (:8), SelectionOutline 500 (:9), UpgradeFeedback and RageOutline 600.

**H9. Assembly and reflection layout.**
- The `Fortress.Game` asmdef covers only `Runtime/Domain/**` (Domain/Fortress.Game.asmdef). Everything else is Assembly-CSharp or Assembly-CSharp-Editor.
- Both test asmdefs reference only `Fortress.Game`, so tests reach presentation code by reflection: `GetType("CoreGuardianPrototype, Assembly-CSharp")` etc.
- 32 type names and about 230 member names are reflected (full list from grep). Examples: `SynchronizeViews`, `enemyViews`, `towerViews`, `Present`, `Attach`, `maskBinding`, `lastMaskDrawCalls`, `widthPixels`, `outlineWidthPixels`, the `*ResourcePath` consts, `Busy`, `IsVisible`, `TargetBounds`.
- Moving a file across the Domain folder changes its assembly, which breaks the `, Assembly-CSharp`-qualified strings. Renaming any private reflected member breaks tests at runtime, not at compile time.

**H10. PlayerPrefs keys:** `CoreGuardian.SoundVolume`, `CoreGuardian.ShotCandidate`, `CoreGuardian.Audio.<Category>` (SoundSettings.cs:8,15). Renaming them loses saved user settings.

---

## 3. Negative results (checked clean; evidence standard: file:line plus an ASSET_LIST match on the Resources-relative path)
- **Every Resources path in E1 exists**:
  - BossAnimated, IronKnightAnimated, CrimsonLordAnimated (boss keys `IronKnight`/`CrimsonLord` from enemies.csv rows 11102/11103).
  - Units for every EnemyKind/TowerKind outside the scene arrays: charger, splitter, healer, armored, flyer, splitchild, frost, flame, rail.
  - OriginalWalk 5 layers × 9 roles, plus Flyer Hover_0..5. VerticalWalk Up/Down_0..3 × 10 roles.
  - OriginalMotion Fixed/Moving × 7 roles, Fire_0..5 (Flame), Pulse_0..5 (Frost), Fire_0..6 (Rail).
  - PlayerFacing D0-7 × 4 parts. OriginalIdle Feet/Upper. PhotoShadows for every role reachable through the PhotoShadow.cs:37 mapping, plus registration.json.
  - EffectsView keys Status/Execute_0-6, FreezeShatter_0-4, Burn_1-5 (the loop at :40 skips 0), Chain_0-2, Stun_0-11, Slow, HealMark, FreezeFootIce, Augments/BurnExplosion, FireTile_0-3, StickyFloor, Enemies/HealPulse, ArmorDeflect_0-4, BossRageSpeedLines, Towers/MergeFlash_0-5, FrostPulse_0-2, StarRank2/3, Skills/AreaBlast_0-2, Teleport_0-7.
  - Library-backed keys exist in SpriteLibrary.asset: `Fx/IronKnightShield/ShieldStart_00-04`, `ShieldLoop_00-03`, `Fx/CrimsonLordSummon/CrownWave_00-05`, `SummonCircle_00-03`, `Fx/Frost/HitFrost_00-07`, `Fx/Rail/HitRail_00-05`, `Fx/Fire/HitFire_00-06`, `Fx/RailBolt`, `IronKnight` and `CrimsonLord` `{Left,Right,Up,Down}_0-3`.
  - HUD: all `Symbol()` outputs, Disabled/Hover/Button/Panel, CardBack{Silver,Gold,Platinum}, H5_{core_damage,defense,dps,hp,move_speed,resistance,weakness}, H4_frame_{silver,gold,platinum}, H6_scroll_{track,handle}, Augments/augment_01..50 (all 50 present), the 4 building sprites, the fonts and the TMP assets.
  - Audio: a .wav exists for every cue id code uses (single, player, multi_1-3, core, hit, death, boss_spawn, boss_accel, install, upgrade_ok, upgrade_fail, round_start, round_complete, victory, defeat, bgm, aug_bgm, install_fail, demolish, cancel_install, ui_click/open/close, aug_silver/gold/platinum, and the Slice cues for Session ids area_blast, burn_explode, chain, cold_pulse, crit, execute, flame_shot, freeze, heal_pulse, hit_cold, hit_flame, hit_rail, rail_shot, reroll, shatter, shield_deploy, star_merge, summon; `split` is ignored at CombatAudio.cs:124).
  - Sheets player/augments/effectTags/skills.
- **CSV keys**: every `Setting("…")` key used in code exists in stage.csv. All 16 `Tag/TagValue(tag,param)` pairs in Domain match effectTags.csv rows one-to-one.
- **Scene and prefab integrity**: 0 dangling local references (scene: 854 objects, 5820 refs). Every script GUID resolves. No `{fileID:0}` in our components. Scene array order matches the enum order (verified by prefab fileID anchors: enemy_attack/defense/buff, tower_single/multi/support).
- **Shader property blocks vs C#**: every property C# sets exists as a uniform in the target shader. Undeclared-in-Properties uniforms set through MPB (`_Threshold, _Opacity, _MaskVP, _Region, _Parameters`) are legal. `SelectionOutline` and `Reference` fixture are identical apart from the name.
- **Settings vs code assumptions**:
  - Linear colour space (ProjectSettings.asset:49) matches the code's Linear handling (PlacementOverlay.cs:47, RouteMeshBuilder.cs:104, and SharedOutlineOutput.shader:4-5, which relies on alpha passing through unchanged).
  - `activeInputHandler: 1` (Input System only, :724): no legacy `UnityEngine.Input` call anywhere. EventSystem uses `InputSystemUIInputModule` (Hud.cs:107).
  - `enableFrameTimingStats: 1` (:161) is required by PerfOverlay.cs:110.
  - Standalone scripting backend is Mono (only `Android: 1` is listed, :629-631). `apiCompatibilityLevel: 6` (.NET Standard 2.1) supports ConditionalWeakTable, StreamWriter and GC.GetAllocatedBytesForCurrentThread.
  - Current quality is 5 (Ultra) with vSync 1 (QualitySettings.asset:7,297).
  - The CanvasScaler reference is 1920×1080 with match 0.5 (Hud.cs:106). The far-right absolute-placed buttons (Reload/TestSpeed at x=1630, :210-211) are re-anchored left by Inspector.cs:81-82, so there is no overflow at 4:3 (recomputed: canvas width 1663 units).
  - `Unity.2D.Welcome` asmdef has no Editor-only platform, but its script uses only UnityEngine (Welcome2DScript.cs:1-12), so the player build is safe.
- **Materials vs shaders**: no .mat references our shaders, so no stale material properties can exist. Environment mats hold only standard sprite properties (`_MainTex, _AlphaTex, _Color, _RendererColor, PixelSnap, _EnableExternalAlpha, _ZWrite`).
- **Editor/player divergence in MaskAtlas**: TryGet validates the dependency hash in the Editor only (MaskAtlas.cs:36-40). The build hook re-checks alpha and refreshes hashes (SpriteAtlasPipeline.cs:86-104), which self-heals the divergence, so it is not reported as a defect.

## 4. Verification data notes
- The page-raster mask path and the related perf numbers are valid only at an exact 1920×1080 camera pixel rect with ortho 8.5 or 4.25 (SelectionOutline.cs:248-251). Measurements at any other window size exercise the legacy path.
- Editor before/after comparisons must start from a fresh domain, or from the same Play-session index, because `ArtDepthSorting.nextLane` and the SharedOutline toggles persist (Domain Reload OFF).
- The `CombatData.Setting(key,fallback)` fallbacks (e.g. Prototype.cs:305-307) and the `EffectsView.Set` empty-array cache (EffectsView.cs:46) mask missing data silently. With today's CSV and library every key is present, so no difference shows; only a renamed key would expose it.

## 5. UNVERIFIED (blocked: not in copy)
- SRP Batcher state, URP MSAA and renderer settings (`Assets/Settings/Rendering/*`).
- Enter Play Mode / Domain Reload and `spritePackerMode` (`ProjectSettings/EditorSettings.asset`). Domain Reload OFF is taken from AGENTS.md:20.
- The scene CSV TextAsset GUIDs (4abc…, 1d2d…, 863a…, a059…, e6ee…) pointing at the Resources CSVs (no .csv.meta).
- `catalog.json` cue ids and categories.
- `DamageRoles.png` sub-sprite names (`DamageHandle, DamageEngineer, DamageRange, DamageSingle`, used at Hud.cs:243-274).
- Sprite names `core` and `*Shadow*`.
- MaskAtlas Manifest entries.
- `.spriteatlasv2` packables.
- `docs/development/outline_sampling_20261023/`.

## 6. Shared base and framework layer (walked up)
- `CoreGuardianSelectionOutline` is the base consumer for HUD selection, RageOutline (via AddComponent, RageOutline.cs:32), UpgradeFeedback (:35) and the ObjectEmphasis per-body fallback (:189). It drives BodyMaskCache, MaskPage, MaskProducerBatch and MaskOutputBatch.
- Static constructors subscribe to `RenderPipelineManager` and editor events once per domain (MaskPage.cs:71-80, MaskOutputBatch.cs:65-75, BodyMaskCache.cs:109-118). With Domain Reload OFF these subscriptions persist, which is intended and not duplicated.
- The `CoreGuardianSharedMaskMaterial` registry (SelectionOutline.cs:426-450) and `CoreGuardianSpriteReferences`, which adds hidden `CoreGuardianHierarchyWatch` components to scene nodes at runtime (:494-497), are the foundations every outline consumer depends on.
- No further defects were found in this layer beyond those listed above.

Counts: P0 0 · P1 0 · P2 1 · P3 16 · Hazards H1-H10.
