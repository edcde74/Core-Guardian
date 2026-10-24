$ErrorActionPreference='Stop'
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$source=Join-Path $projectRoot 'docs/development/mask_cpu_preparation_20261024'
$names=@('Main Thread','PlayerLoop','CG.Emphasis.Present','CG.Emphasis.Tint','CG.Mask.Present','CG.Mask.SourceFilter','CG.Mask.BoundsCrop','CG.Mask.CacheLookup','CG.Mask.Output','CG.Mask.QuadUpload','CG.Mask.PageProducer','CG.SpriteReferences.Read','CG.PhotoShadow.Present','WaitForJobGroupID','WaitForLastPresentation','Semaphore.WaitForSignal','GarbageCollector.CollectIncremental')
$runs=@(1..3|ForEach-Object {
    $rep=$_;$data=Get-Content (Join-Path $source ('before_'+$rep+'.json')) -Raw|ConvertFrom-Json;$frames=@($data.frames)
    $times=@($frames.ms);$sum=($times|Measure-Object -Sum).Sum;$metrics=[ordered]@{}
    foreach($name in $names){$values=@($frames|ForEach-Object {$counter=$_.counters.$name;if($counter.valid -and $null -ne $counter.value){[double]$counter.value/1e6}});$metrics[$name]=[ordered]@{valid=$values.Count;meanMs=($values|Measure-Object -Average).Average}}
    $alloc=@($frames|ForEach-Object {[double]$_.counters.'GC Allocated In Frame'.value}|Sort-Object)
    [ordered]@{rep=$rep;samples=$frames.Count;nonunitGaps=@($frames|Where-Object {$_.gap -ne 1}).Count;frameMeanMs=$sum/$frames.Count;fpsMean=$frames.Count*1000/$sum;fpsMin=1000/($times|Measure-Object -Maximum).Maximum;fpsMax=1000/($times|Measure-Object -Minimum).Minimum;drawMean=($frames.draw|Measure-Object -Average).Average;setpassMean=($frames.setpass|Measure-Object -Average).Average;gcCollections=($frames.collectionsSincePrevious|Measure-Object -Sum).Sum;gcMeanBytes=($alloc|Measure-Object -Average).Average;gcMedianBytes=($alloc[44]+$alloc[45])/2;metrics=$metrics}
})
$runs|ConvertTo-Json -Depth 10|Set-Content (Join-Path $PSScriptRoot 'cost_evidence.json')
$inventory=Get-Content (Join-Path $projectRoot 'docs/development/damage_number_prewarm_20261023/script_inventory.json') -Raw|ConvertFrom-Json
$checks=@($inventory|Where-Object {$_.scope -eq 'runtime'}|ForEach-Object {$actual=(Get-FileHash -LiteralPath (Join-Path $projectRoot $_.path)).Hash.ToLower();[ordered]@{path=$_.path;previous=$_.sha256;current=$actual;equal=($actual -eq $_.sha256)}})
[ordered]@{previousRuntime=$checks.Count;currentRuntime=@(Get-ChildItem (Join-Path $projectRoot 'Assets/Scripts/Runtime') -Recurse -Filter '*.cs').Count;unequal=@($checks|Where-Object {-not $_.equal}).Count;files=$checks}|ConvertTo-Json -Depth 8|Set-Content (Join-Path $PSScriptRoot 'runtime_source_check.json')
$runs|ForEach-Object {[pscustomobject]@{rep=$_.rep;frameMs=$_.frameMeanMs;fps=$_.fpsMean;playerLoop=$_.metrics['PlayerLoop'].meanMs;emphasis=$_.metrics['CG.Emphasis.Present'].meanMs;tint=$_.metrics['CG.Emphasis.Tint'].meanMs;mask=$_.metrics['CG.Mask.Present'].meanMs;shadow=$_.metrics['CG.PhotoShadow.Present'].meanMs;draw=$_.drawMean;setpass=$_.setpassMean}}|ConvertTo-Json
