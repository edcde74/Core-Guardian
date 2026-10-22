param([string]$Side='b_final',[string]$BaselineSide='baseline')
$ErrorActionPreference='Stop'
function Normalize($outcome){
    $offset=[int](($outcome.enemies.Id|Measure-Object -Minimum).Minimum)
    foreach($enemy in $outcome.enemies){$enemy.Id=[int]$enemy.Id-$offset}
    $outcome.events=@($outcome.events|ForEach-Object{
        $parts=$_ -split '\|';if($parts[0] -eq 'P'){$parts[2]=[string]([int]$parts[2]-$offset)}elseif($parts[0] -eq 'D'){$parts[1]=[string]([int]$parts[1]-$offset)};$parts -join '|'
    })
    foreach($projectile in $outcome.projectiles){$projectile.target=[int]$projectile.target-$offset}
    return @{offset=$offset;value=$outcome}
}
$rows=@(foreach($run in 0..2){
    $before=Get-Content "$PSScriptRoot/$BaselineSide/run_${run}_outcome.json" -Raw|ConvertFrom-Json
    $after=Get-Content "$PSScriptRoot/$Side/run_${run}_outcome.json" -Raw|ConvertFrom-Json
    $before=Normalize $before;$after=Normalize $after;$checks=[ordered]@{}
    foreach($key in @('events','CoreEnergy','Gold','Crystals','phase','enemies','projectiles','damage')){
        $checks[$key]=(($before.value.$key|ConvertTo-Json -Depth 20 -Compress) -ceq ($after.value.$key|ConvertTo-Json -Depth 20 -Compress))
    }
    [ordered]@{run=$run;beforeEnemyIdOffset=$before.offset;afterEnemyIdOffset=$after.offset;idOffsetDifference=$before.offset-$after.offset;beforeEvents=$before.value.events.Count;afterEvents=$after.value.events.Count;checks=$checks;equal=(@($checks.Values|Where-Object {-not $_}).Count -eq 0)}
})
[ordered]@{kind='Enemy IDs normalized by a constant per-run offset only; original outcomes and raw comparisons retained. Different normal-play time before Diagnostic caused initial ID offsets. No positions, health, order or event payloads normalized.';side=$Side;rows=$rows}|ConvertTo-Json -Depth 15|Set-Content "$PSScriptRoot/equivalence_$Side.json"
$rows|Select-Object run,idOffsetDifference,beforeEvents,afterEvents,equal
