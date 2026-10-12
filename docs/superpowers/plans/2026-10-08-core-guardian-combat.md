# Core Guardian combat prototype implementation plan

Goal: Implement requested steps 1–3 in a separate playable scene using current Notion rules and approved A artwork.
Architecture: CSV-backed deterministic combat/economy model in Fortress.Game; Unity adapter and functional uGUI in a separate runtime assembly; editor API scene builder.
Tech stack: Unity 6000.6.2f1, URP, Input System, uGUI, Unity Test Framework.
Spec: current Notion player/tower/enemy/damage/currency/growth/HUD/loop documents, plus user replies on 2026-10-08.

## Global constraints
- Preserve SampleScene and all art review scenes/prefabs. Never edit scene YAML.
- A road material, 0.6 artwork scale, foot/base anchors and attached shadows stay approved.
- Data values are provisional playtest values. CSV startup validation rejects missing references, duplicate IDs and invalid values.
- Confirmed: range exit clears target; install on player cell allowed, leave via WASD; start gold 0; instant kill gold; dead-target projectiles evaporate without damage.
- Do not create final art, HUD mockups, boss/augmentation systems or a Windows build.
- No git repository exists: use persistent local ledger and new files, no fabricated commits/worktree.
- Pending: preparation timeout without first tower; player upgrade price from step 4.

## Tasks and verification
1. Combat rules/model/data: write failing Unity tests, then implement rounding, priority/lock, three enemy/tower roles, homing projectiles, exact damage/gold/core accounting, placement and refunds. Run EditMode suite.
2. Unity adapter: follow tile centers, instantiate approved prefab visuals, synchronize occupied tilemap without blocking departure from occupied start. Add PlayMode tests for model progression and verify actual scene via editor helper.
3. Functional HUD: existing documented layout; installation/selection/destruction, player and tower upgrades, preparation/start, state and damage totals. Use temporary simple shapes for missing VFX; report asset requirements.
4. Create scene copy through Unity editor API; test actual combat, save/reopen verification, capture game view; run regression suites and fresh review.

## Review focus
- Rewards and core loss happen exactly once; no overkill counted in totals.
- Priority tie uses remaining path distance; range clearing and lock are distinct from initial first arrival.
- Multishot targets distinct, never splash; enemy speed buffs do not stack; tower damage support adds and never buffs player.
- UI click cannot also place/select world objects; no build/upgrade during combat, destruction stays available.
- All current balance numbers come from CSV, not embedded runtime constants.
