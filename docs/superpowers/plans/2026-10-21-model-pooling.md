# Model Pooling Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans for sequential inline execution; one independent final review. User approved the linked design and proceeding in this chat.

**Goal:** Reuse enemy/projectile data while generation handles prevent old references targeting new lives.
**Architecture:** Private pooled data, value handles checked against owner/generation, immutable event snapshots, retirement quarantine through outermost Tick/event. Read-only public lists and explicit diagnostic mutation APIs replace public mutable lists.
**Tech Stack:** Unity 6000.6 C#; existing CoreGuardian assemblies.
**Spec:** [approved design](../specs/2026-10-21-model-pooling-design.md)

## Constraints

Recovery base `ed81777`; preserve user dirty scene/settings/untracked files. No Play/test/profiling/build/executable/push. Source tests are added/migrated but not executed. Compile/static review only; no speedup claims. Follow existing pooling/quality rules.

## Tasks

- [x] Add model storage/pools, owner+generation handles and event snapshot types; stale/default/foreign handles fail safely. Expose explicit TryRead/TryUpdate/diagnostic APIs, read-only lists and counters.
- [x] Migrate Session snapshots/targeting/index/creation/removal/reset/exception/disposal to handles, preserving combat arithmetic and order. Quarantine retired data until outermost operation ends and clear model references before reuse.
- [x] Migrate Prototype, Animation, Art, HUD, Audio to handles/read snapshots; event payloads remain stable across later ticks and reentry.
- [x] Migrate existing test sources' direct list mutation/identity assumptions; add owner/generation/reuse, immutable events, cap/clear, reentry/exception tests. Do not run them.
- [x] Compile, repair compiler errors sequentially, independent final review, verify hashes/settings/scene state, record measured/unmeasured boundary, update handover and make own local completion commit.

## Ledger

- Ruling: no extra request for implementation permission; user approved design and said proceed. The requested test/performance deferral remains in force.
- Ruling: handles can provide convenient read properties, but model storage never escapes; any mutation path validates owner/generation. Snapshots used by events are values, never active handles masquerading as historical data.

- Ruling: slot identity uses an internal managed slot reference rather than a numeric slot table; value handles still validate owner and generation and never expose model storage.
- Ruling: retain living frozen enemies on defeat until reset/disposal to preserve the existing defeat screen; retire exited/dead models immediately at that boundary.
