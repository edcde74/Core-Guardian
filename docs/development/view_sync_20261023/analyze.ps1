$ErrorActionPreference='Stop'
$root=Join-Path $PSScriptRoot '.'
function Stats([double[]]$Values){
 $v=@($Values|Sort-Object);$n=$v.Count
 if($n -eq 0){return $null}
 $sum=($v|Measure-Object -Sum).Sum
 $median=if($n%2){$v[[int][Math]::Floor($n/2)]}else{($v[$n/2-1]+$v[$n/2])/2}
 [ordered]@{n=$n;mean=$sum/$n;median=$median;min=$v[0];max=$v[-1];p95=$v[[Math]::Ceiling(.95*$n)-1];p99=$(if($n -ge 100){$v[[Math]::Ceiling(.99*$n)-1]}else{$null})}
}
function Describe($Frames){
 $f=@($Frames);$ms=[double[]]@($f|ForEach-Object {$_.ms})
 $names=@('CG.Views.All','CG.Views.Enemies','CG.Views.Towers','CG.Views.Projectiles','GC Allocated In Frame','GC.Collect','GarbageCollector.CollectIncremental','Main Thread','PlayerLoop','CG.Emphasis.Present','CG.Mask.Present')
 $counterStats=[ordered]@{}
 foreach($name in $names){
  $values=@($f|ForEach-Object {$c=$_.counters.$name;if($null -ne $c -and $c.valid){[double]$c.value}})
  $divisor=if($name -eq 'GC Allocated In Frame'){1}else{1e6}
  $counterStats[$name]=Stats ([double[]]@($values|ForEach-Object {$_/$divisor}))
 }
 $sum=($ms|Measure-Object -Sum).Sum
 [ordered]@{
  samples=$f.Count;nonUnitGaps=@($f|Where-Object {$_.gap -ne 1}).Count;invalidMs=@($f|Where-Object {$_.ms -le 0 -or [double]::IsNaN($_.ms) -or [double]::IsInfinity($_.ms)}).Count
  frameMs=Stats $ms;fps=[ordered]@{min=1000/($ms|Measure-Object -Maximum).Maximum;max=1000/($ms|Measure-Object -Minimum).Minimum;average=$f.Count*1000/$sum}
  counters=$counterStats;draw=Stats ([double[]]@($f.draw));setPass=Stats ([double[]]@($f.setpass));projectiles=Stats ([double[]]@($f.projectiles))
  observedCollectionDeltasExcludingFirst=($f|Select-Object -Skip 1|Measure-Object -Property collectionsSincePrevious -Sum).Sum
  firstCollectionDeltaIncludesWarmup=$f[0].collectionsSincePrevious
 }
}
$runs=[ordered]@{};$all=[ordered]@{}
foreach($condition in @('before','after')){
 $all[$condition]=@()
 foreach($rep in 1..3){
  $label="${condition}_$rep";$path=Join-Path $root "$label.json"
  $raw=Get-Content -Raw -LiteralPath $path|ConvertFrom-Json
  $completion=Get-Content -Raw -LiteralPath (Join-Path $root "${label}_benchmark_completion.json")|ConvertFrom-Json
  if($null -ne $completion.error){throw "$label failure: $($completion.error)"}
  $runs[$label]=Describe $raw.frames;$all[$condition]+=@($raw.frames)
 }
}
$groups=[ordered]@{before=Describe $all.before;after=Describe $all.after}
foreach($condition in @('before','after')){
 $groups[$condition].observedCollectionDeltasExcludingFirst=(1..3|ForEach-Object {$runs["${condition}_$_"].observedCollectionDeltasExcludingFirst}|Measure-Object -Sum).Sum
 $groups[$condition].firstCollectionDeltaIncludesWarmup=@(1..3|ForEach-Object {$runs["${condition}_$_"].firstCollectionDeltaIncludesWarmup})
}
$comparison=[ordered]@{}
foreach($name in @('CG.Views.All','CG.Views.Enemies','CG.Views.Towers','CG.Views.Projectiles','GC Allocated In Frame')){
 $b=$groups.before.counters[$name].mean;$a=$groups.after.counters[$name].mean
 $comparison[$name]=[ordered]@{before=$b;after=$a;saved=$b-$a;savedPercent=100*($b-$a)/$b}
}
$b=$groups.before.frameMs.mean;$a=$groups.after.frameMs.mean
$comparison.frameMs=[ordered]@{before=$b;after=$a;saved=$b-$a;savedPercent=100*($b-$a)/$b}
$b=$groups.before.fps.average;$a=$groups.after.fps.average
$comparison.fps=[ordered]@{before=$b;after=$a;gain=$a-$b;gainPercent=100*($a-$b)/$b}
$result=[ordered]@{runs=$runs;groups=$groups;comparison=$comparison;limitations=@('Before collected Oct23, candidate completed Oct24 after interruption; temporal/system confound.','No rows excluded. Nonunit frame gaps must invalidate per-frame correspondence.','First collection delta includes warm15; use subsequent deltas only and disclose first-frame uncertainty.','Current-thread ProfilerRecorder values; engine recording OFF in both; no GPU or player/target-device claim.','Frame GC allocations include benchmark observer and Editor, not just views.','First run initialization and later steady frames remain distinguishable in raw data.')}
$result|ConvertTo-Json -Depth 12|Set-Content -LiteralPath (Join-Path $root 'summary.json') -Encoding utf8
$comparison|ConvertTo-Json -Depth 4
