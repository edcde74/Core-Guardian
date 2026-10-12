# SDD ledger — plan: docs/superpowers/plans/2026-09-22-biological-beasts-and-towers.md

Setup: Ruling: no Git repository exists, so an isolated Git worktree cannot be created; implementation will remain in the user-authorized project workspace — cost if wrong: changes cannot be reverted with Git history.
Setup: Ruling: `unity status --format json` found an unreadable Editor descriptor and no controllable instance; do not edit scene or prefab YAML, and defer Task 8 Editor work until the user confirms Editor state — cost if wrong: an in-memory scene could be overwritten or the wrong scene changed.
Pre-flight: Task 1 Health produces `Health.TakeDamage`, `Current`, and `IsDead`; Tasks 3-5 consume those exact members — compatible.
Pre-flight: Task 2 `EnemyController.Progress` is consumed by Task 3 targeting — compatible.
Pre-flight: Task 3 `TowerBuffReceiver.SetBannerSource` is consumed by Task 6 BannerTower — compatible.
Pre-flight: Task 7 placement validation and Task 8 scene layers share `BuildableCells` and `PathCells` names — compatible.
Task 1: Ruling: add the `Fortress.Game` runtime assembly definition and reference it from the PlayMode test assembly — Unity test assemblies do not automatically see code in the predefined Assembly-CSharp assembly — cost if wrong: runtime code must move with this assembly definition.
Task 1: complete (no Git repository; tests: Unity CLI PlayMode → 1/1 passed).
Task 2: complete (no Git repository; tests: Unity CLI PlayMode → 2/2 passed).
