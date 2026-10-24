# Prepared Mask Inputs and Output Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Reduce mask preparation CPU while preserving pixel output, callbacks and queued ownership.

**Architecture:** B1 exposes immutable values from the cache Part already collected by Begin to its synchronous QueuePage caller. B2 independently caches normal-outline MPB payload construction while always applying the block to its renderer.

**Tech Stack:** Existing Unity C#, NUnit, ProfilerRecorder and bounded editor scripts.

**Spec:** [approved B1/B2 design](../../development/emphasis_mask_cpu_20261024/NEXT_PROPOSAL.md). User approved B1/B2 implementation, regressions, each candidate512 once and warm15/sample90 x3, reload/own Play/temp1920 selection/restore, 60–90min.

## Global Constraints

- No build, executable, commit or push. Preserve existing changes, scene/settings and held candidates.
- Original output/fallback, native batch OFF, no quality/count/lifetime change.
- No repeated failed comparisons or measurements without approval; stop on unexpected load/timeouts.
- Reuse original512 and valid baseline3; save each stage separately. Mask1ms goal; below0.5ms misses large-cost goal.

## Review Focus

- Next Begin must not mutate previously queued producer commands.
- Resource callbacks must run before input preparation.
- External renderer MPB writes must be restored by the next Present.
- Contact/overlay properties must clear on returning to normal outline.
- RT recreation, exact origin/color and clamped width changes must rebuild output payload.

### Task 1: B1 prepared Part consumption

Files: modify `Assets/Scripts/Runtime/Presentation/Rendering/CoreGuardianBodyMaskCache.cs` and `CoreGuardianSelectionOutline.cs`; add `Assets/Scripts/Tests/EditMode/CoreGuardianPreparedMaskInputsTests.cs` and a PlayMode ownership test. Evidence: `docs/development/emphasis_mask_cpu_20261024/b1/`.

Interfaces: Part gains internal readonly `Sprite Sprite`, `float Alpha`, `Matrix4x4 DrawMatrix`. DrawMatrix retains original localToWorld * flip scale arithmetic. QueuePage consumes `maskBinding.scratch` only synchronously after Begin; producer copies remain unchanged.

- [x] Write PreparedPartValuesStayStableAcrossSourceChanges test: reflected properties equal original sprite/alpha/matrix; mutating source does not mutate old Part, next Present refreshes values.
- [x] Compile and run new test; expect missing property assertion (RED).
- [x] Implement accessors and replace only QueuePage renderer getter reads. Start from original instrumented baseline, preserve crop/fallback/sort.
- [x] Run new test plus sharing/producer/outline regressions; actual Play queued test changes same owner's scratch after queue then Flush and verifies old opaque center survives. Preserve resource callback and next-Begin behavior.
- [x] Candidate512 once; require all output/mask/geometry/rect hashes equal original and isolation/nonblank valid.
- [x] Candidate warm15/sample90 x3 sequentially, inspect each completion/restoration before next. Compare target CPU, frame/FPS/GC and memory. Save source/diff; restore if rejected.

### Task 2: B2 output payload

Files: modify SelectionOutline; add `CoreGuardianOutlinePayloadTests.cs`. Evidence: `.../b2/`. Use original if B1 rejected; record exact cumulative base otherwise.

Interfaces: private `bool PrepareOutlineProperties(int radius)` returns true iff owned MPB rebuilt. Tracks exact texture reference/physical width/height/origin/color/effective radius. `SetOutputMask`, InvalidateSources and ReleasePresentation invalidate normal payload. Every normal Present still SetPropertyBlock.

- [x] Write RED tests for preparation reuse and effective input changes, external MPB restoration, contact-to-normal removal of Band and resource loss/release refresh.
- [x] Compile/run; require expected missing method RED before implementation.
- [x] Implement minimal payload metadata and preparation guard, preserving geometry, material, sorting and non-normal writes.
- [x] Run targeted regressions and new tests (GREEN).
- [x] Candidate512 once and candidate warm15/sample90 x3 under same fixed conditions; stop on failure, report own stage and original deltas separately.
- [x] One final fresh reviewer, verify resulting files/restoration and update evidence/handover with actual results and limitations. No commits.

Self-review: all approved requirements mapped above; no new snapshot array, slot-validation change or camera/structure expansion. Execution remains inline per existing approval; no additional approval handoff.

Execution result: both stages completed independently and rejected (<0.5ms), restored original. B1 callback order verified by source review; new callback-mutation scenario not executed. Runtime ownership checks buffer overwrite/opaque presence, not complete pool-cycle pixel equality. Ruling: preserve evidence outside Assets; no whole-suite/extra reruns/commits. See B1_B2_RESULTS.md for exact coverage and limitations.
