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

Interfaces: `CoreGuardianMaskAtlasBuilder.Build(Texture2D[] sources,string folder,int maxSize)` returns generated manifest. Final immutable `Entry` contains source/array page/layer/dependency hash through `TryGet(Texture2D,out Entry)`. Original UVs remain unchanged; unknown sources render original geometry. Private `lastMaskDrawCalls`/`lastAtlasMisses` diagnose actual submissions.

- [x] Write reflection-based EditMode contracts and actual mask pixel assertions; clean test-owned assets/native resources.
- [x] Observe intended missing-type failure before product code; subsequent revision/source/publication defects also RED→GREEN.
- [x] Implement immutable deterministic array pages, exact source dimensions/UV, Point/Clamp/no mips/RGBA32 <=2048, grouped mesh/GPU matrices; compare every alpha layer before publish. No rect/gutter remap retained.
- [x] New2/2 and existing3/3 completed; failures preserved, no stale results reused.
- [x] Checkpoint `5531f70` scoped implementation and generated assets; performance then still pending.

### Task 2: Real source generation and comparison

Files: diagnostic scripts/results under `docs/development/mask_atlas_20261021/`; generated pages/manifest in `Assets/Resources/CoreGuardian/MaskAtlas/`.

- [x] Fresh before100frames×3 and initial hashes saved on unchanged renderer.
- [x] Actual attack5→1 and432 cases0 differences; expand only after gate.
- [x] Final3456 effect comparisons0 differences; after100frames×3, engine/Frame Debugger/raw evidence and all FPS/memory limits recorded. Calls improve; FPS not established, p95 worsens.
- [x] Reset600frames×3 completed; live masks3 each, arrays9 constant. Unknown native RT27 retained, own orphan batch meshes24 cleaned; finalbatch0/loadedarrays0. Scene clean/settings match.
- [x] Independent review and re-review performed, failures/limits in result record, handoff updated; completion commit records evidence and test cleanup. No push.

## Execution ledger

- 2026-10-21: Current CLI latest confirmed. Editor port7800, ready/stopped, no other test; read-only eval initially5s timeout, current schema20s query succeeded, combat scene clean, profiler off. Initial settings now differ from previous session: capture fresh values, never restore old hashes.
- Ruling: reuse current checkout rather than isolate it because the existing Editor and real workload belong here; only scoped files are changed and HEAD is recovery point.
- Ruling: reject 2D UV-remapped pages after actual boundary-pixel failures. Use Texture2DArray pages grouped by exact original width/height, Point/Clamp/no mips, original UV unchanged; RGBA32 per-layer alpha. This replaces the initial gutter/rect layout and avoids hardware-specific UV quantization. Attack actual5→1 and432 effect cases0 differences; expanded82 sources/9 arrays,3456 cases0 differences/misses observed before review fixes.
- Review: independent requesting-code-review agent found source reimport staleness, manifest revision cache staleness, partial overwrite publication, and transition coverage gaps. Both staleness defects reproduced by2 failing tests; mask-pixel assertions added for crop/flip/fallback/shrink. Publication will use content-addressed immutable pages, manifest-last commit. No user quality tradeoff accepted.
- Outcome: final92 sources/9 arrays/4410920 engine bytes; Stats3208→2178 and1608→578 on3 runs. Mean FPS27.314→27.420 overlaps variation; p95 and minimum FPS regression explicitly unresolved. Next narrow PhotoShadow first-capture hitch before claiming responsiveness/FPS success; shared body masks remain a separate follow-up. [Evidence and limitations](../../development/mask_atlas_20261021/RESULTS.md).
