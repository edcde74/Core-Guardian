# Mask atlas implementation plan

> **For agentic workers:** Use superpowers:executing-plans inline, task by task. Keep one Unity operator.

**Goal:** Reduce actual mask submissions by putting distinct part textures on dedicated atlas pages without changing body sprites.

**Architecture:** Editor-baked mask-only pages and immutable source/page mappings. The mask owner groups geometry by page and exact opacity, preserving GPU part transforms and original fallback.

**Tech Stack:** Unity6000.6.2f1, URP17.6, C#, official Unity CLI1.0.0-beta.13.

**Spec:** [approved design](../../development/mask_atlas_design_20261021.md).

## Global Constraints

- Current checkout and its connected Editor are required for matched captures; preserve pre-existing settings/untracked files. Recovery HEAD `f0b0229`; no empty checkpoint.
- No build/executable/push/package/global packing changes. Stop only owned checks/Play.
- Preserve original textures/importers, art/animation/RT consumers/balance/sound. Extra atlas memory must be reported.
- User's “계속 진행해” authorizes this design's implementation and targeted verification, superseding design-only handoff scope.

## Review Focus

Different texture/opacity/page and unknown source must not disappear or merge incorrectly; cropped Sprite UVs must preserve alpha; mesh shrink/disable/destroy must not retain geometry; pages must not reallocate per frame; real input must reduce submissions with zero pixel changes.

### Task 1: Atlas data and renderer contract

Files: create `Assets/Scripts/CoreGuardianMaskAtlas.cs`, `Assets/Editor/CoreGuardian/MaskAtlasBuilder.cs`, `Assets/Game/Tests/EditMode/CoreGuardianMaskAtlasTests.cs`; modify `CoreGuardianSelectionOutline.cs`, `SelectionMask.shader`.

Interfaces: `MaskAtlasBuilder.Build(Texture2D[] sources,string folder,int maxSize)` returns generated manifest. Manifest exposes immutable `Entry` source/page/rect mapping through `TryGet(Texture2D,out Entry)`. Owner uses page UVs only for registered sprites; unknown textures render original geometry. Private `lastMaskDrawCalls`/`lastAtlasMisses` diagnose actual submissions.

- [ ] Write reflection-based EditMode test: manifest/builder must exist, two distinct textures on one page submit1, alpha split2, unknown source fallback2, cropped UV/alpha identity, original Sprite unchanged; cleanup test-owned assets.
- [ ] Run filter `CoreGuardianMaskAtlasTests`, observe intended missing-type failure before product code.
- [ ] Implement data, deterministic Editor generation (no rotation/scale,4px extruded gutter, Point/Clamp/no mips/RGBA32, <=2048), grouped mesh and GPU matrices. Assert imported alpha equality before retaining generated files.
- [ ] Run new contract and existing outline class; record actual counts and failures, no stale status reuse.
- [ ] Local checkpoint only scoped source/test/generated assets and records after green, with unverified runtime status explicit.

### Task 2: Real source generation and comparison

Files: diagnostic scripts/results under `docs/development/mask_atlas_20261021/`; generated pages/manifest in `Assets/Resources/CoreGuardian/MaskAtlas/`.

- [ ] Save initial state/settings, collect before100frames×3 via existing controlled probe on unchanged renderer before implementation (or reuse current matching baseline only with condition/hash explanation).
- [ ] Build first actual attack texture set, verify part submission decrease and alpha/pixel equality. Expand to eight-role source sets only after this gate.
- [ ] Reuse existing8×432 effect comparison against archived original. Capture after100frames×3 with matched settings; report engine markers, Draw/SetPass, FPS min/max/avg, allocations, page/mesh memory and limitations.
- [ ] Check repeated owned reset600frames×3 and cleanup. Restore own state/settings, leave existing user diff, verify scene dirty=false and product scope.
- [ ] Review complete diff, document failures/limits, update handoff and local completion commit. No push.

## Execution ledger

- 2026-10-21: Current CLI latest confirmed. Editor port7800, ready/stopped, no other test; read-only eval initially5s timeout, current schema20s query succeeded, combat scene clean, profiler off. Initial settings now differ from previous session: capture fresh values, never restore old hashes.
- Ruling: reuse current checkout rather than isolate it because the existing Editor and real workload belong here; only scoped files are changed and HEAD is recovery point.
- Ruling: reject 2D UV-remapped pages after actual boundary-pixel failures. Use Texture2DArray pages grouped by exact original width/height, Point/Clamp/no mips, original UV unchanged; RGBA32 per-layer alpha. This replaces the initial gutter/rect layout and avoids hardware-specific UV quantization. Attack actual5→1 and432 effect cases0 differences; expanded82 sources/9 arrays,3456 cases0 differences/misses observed before review fixes.
- Review: independent requesting-code-review agent found source reimport staleness, manifest revision cache staleness, partial overwrite publication, and transition coverage gaps. Both staleness defects reproduced by2 failing tests; mask-pixel assertions added for crop/flip/fallback/shrink. Publication will use content-addressed immutable pages, manifest-last commit. No user quality tradeoff accepted.
