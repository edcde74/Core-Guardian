# Ledger — plan: docs/superpowers/plans/2026-10-08-core-guardian-combat.md

Task 1: initial rules pass; rounding RED 3/3 missing rules → GREEN 16/16. Boundary aura-order regression under verification.
Pre-flight: model emits positions/events; adapter consumes them and owns Unity objects. Runtime assembly can reference Fortress.Game but cannot reference Assembly-CSharp; movement access stays through reflection or a public callback bridge.
Ruling: use a new gameplay scene and runtime assembly; legacy Fortress primitives remain for existing tests and are not wired into current gameplay.
Confirmed user replies: gold 0, instant kill gold, dead-target projectiles vanish, range exit reacquires, player-cell tower allowed with manual WASD departure.
Confirmed later replies: preparation timeout starts without tower; player prices 100/140/190/250/320/400, next increase adds 10.
Task 2: runtime bridge RED missing component → GREEN actual-scene PlayMode 1/1. Installed on player cell, departed .6 world units, return blocked.
Task 3: functional Canvas HUD connected; camera/road/art scale unchanged. Prototype projectile dots and buff rings remain temporary assets.
Task 1: complete, final EditMode 20/20 including 1 legacy test. Aura boundary order and exact attack interval RED→GREEN; enum validation and mandatory wave1 hardening RED→GREEN.
Final review: fresh reviewer found numeric enum acceptance, missing wave1 validation and upgrade target case mismatch. All fixed in one pass with regression coverage.
Task 2–3: final PlayMode 5/5 including legacy 3, after HUD corrections. Actual wave1 simulation: 15 kills, gold45, completion crystals20, core20, total actual damage375. Player-cell install/departure/reentry blocker verified in actual scene.
Task 4: complete. Scene saved/reopened Verify passed, A material references and 3+3 prefab references retained, player start15,10. HUD-composited screenshot combat_roles_20261008.png inspected. Original Depth SHA256 unchanged: 556E9BFF36A387C7F714CF5416C9403AAA296E298E4EA22F1CDF8E4637C76EF6.
Limitations: final animations/VFX/HUD art not produced, official round progression limited to wave1. Existing Windows build unchanged. New scene registered for editor restart; old build scene retained.
Ruling: default Assembly-CSharp adapter directly references existing movement and Fortress.Game, avoiding a new assembly dependency cycle.
Ruling: provide canonical wave 1 and separate diagnostic mixed encounter; later wave ordering/boss/augmentation stay outside requested steps 1–3.
