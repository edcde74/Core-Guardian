$ErrorActionPreference='Stop'
$root='docs/development/view_sync_20261023'
$p='Assets/Scripts/Runtime/Bootstrap/CoreGuardianPrototype.cs'
$a='Assets/Scripts/Runtime/Presentation/Characters/CoreGuardianCombatAnimation.cs'
Copy-Item -LiteralPath $p -Destination "$root/CoreGuardianPrototype_before.cs"
Copy-Item -LiteralPath $a -Destination "$root/CoreGuardianCombatAnimation_before.cs"
git -c safe.directory=C:/Users/edcde/OneDrive/Desktop/CD diff --binary -- . ':!docs/development/font_tools_vendor' | Out-File "$root/starting.diff" -Encoding utf8
git -c safe.directory=C:/Users/edcde/OneDrive/Desktop/CD status --porcelain -- . ':!docs/development/font_tools_vendor' | Out-File "$root/starting_status.txt" -Encoding utf8
$protected=@('Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity','ProjectSettings/PackageManagerSettings.asset','ProjectSettings/TimeManager.asset','ProjectSettings/EditorSettings.asset','ProjectSettings/ProjectSettings.asset','Assets/Scripts/Runtime/Domain/Rules/Session.cs','Assets/Shaders/CoreGuardianSelectionOutline.shader')
$protected | Where-Object {Test-Path -LiteralPath $_} | ForEach-Object {Get-FileHash -LiteralPath $_} | Select-Object Path,Hash | ConvertTo-Json | Set-Content "$root/protected_before.json" -Encoding utf8
$s=[IO.File]::ReadAllText((Join-Path $pwd $p))
$s=$s.Replace('using UnityEngine;','using UnityEngine;'+"`r`nusing Unity.Profiling;")
$s=$s.Replace('    void SynchronizeViews()',"    static readonly ProfilerMarker SyncMarker=new ProfilerMarker(`"CG.Views.All`");`r`n    static readonly ProfilerMarker EnemyMarker=new ProfilerMarker(`"CG.Views.Enemies`");`r`n    static readonly ProfilerMarker TowerMarker=new ProfilerMarker(`"CG.Views.Towers`");`r`n    static readonly ProfilerMarker ProjectileMarker=new ProfilerMarker(`"CG.Views.Projectiles`");`r`n    void SynchronizeViews()")
$s=$s.Replace("    void SynchronizeViews()`r`n    {","    void SynchronizeViews()`r`n    {`r`n        using(SyncMarker.Auto())`r`n        {`r`n        using(EnemyMarker.Auto())`r`n        {")
$s=$s.Replace('        activeTowerViewIds.Clear();foreach',"        }`r`n        using(TowerMarker.Auto())`r`n        {`r`n        activeTowerViewIds.Clear();foreach")
$s=$s.Replace('        activeProjectileViewIds.Clear();foreach',"        }`r`n        using(ProjectileMarker.Auto())`r`n        {`r`n        activeProjectileViewIds.Clear();foreach")
$s=$s.Replace("        combatAnimation.Player();`r`n    }","        }`r`n        combatAnimation.Player();`r`n        }`r`n    }")
[IO.File]::WriteAllText((Join-Path $pwd $p),$s,[Text.UTF8Encoding]::new($false))
@'
# SDD ledger — plan: docs/superpowers/plans/2026-10-23-view-synchronization.md
Approval: Task1+2; 3 x (warm15/sample90) each condition, separate diagnostic15 each. 20-40 min. No Task3/build/exe/commit/push.
BASE: 1de8ced4f60dd16d9ec50ff5a9cd76f24c8d88b9; starting changes preserved in starting.diff and status.
Ruling: work in the explicitly requested checkout against its running Editor; no new worktree, no checkpoint commit per user.
Pre-flight Task1 -> Task2: markers and reference data precede candidate; retain identical fixed markers and benchmark code.
Task1: in progress. Initial Editor stopped, clean 7-root combat scene, compiling/updating false, recording/deep/profileEditor false. No running tests; old completed result is not current validation.
Counters: collect separately from timing; do not leave per-call diagnostic increments in performance source.
'@ | Set-Content "$root/progress.md" -Encoding utf8
'Backups, protected hashes, starting diff and markers ready.'
