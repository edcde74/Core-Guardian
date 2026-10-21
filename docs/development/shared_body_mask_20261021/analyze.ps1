$ErrorActionPreference='Stop'
function Stats([double[]]$Values) {
 if(-not $Values.Count){return $null}
 $sorted=@($Values|Sort-Object)
 function Q([double]$p){$pos=($sorted.Count-1)*$p;$lo=[math]::Floor($pos);$hi=[math]::Ceiling($pos);return $sorted[$lo]+($sorted[$hi]-$sorted[$lo])*($pos-$lo)}
 [pscustomobject]@{n=$Values.Count;mean=($Values|Measure-Object -Average).Average;median=(Q .5);p95=(Q .95);p99=(Q .99);min=$sorted[0];max=$sorted[-1]}
}
$rows=@();$markers=@()
foreach($side in @('before','after')) {
 foreach($run in 0..2) {
  $label="normal_${run}_n128"
  $frames=@(Get-Content -LiteralPath (Join-Path $PSScriptRoot "$side/${label}_frames.json") -Raw|ConvertFrom-Json)
  $valid=@($frames|Where-Object {$_.deltaMs -gt 0 -and -not [double]::IsInfinity($_.deltaMs) -and -not [double]::IsNaN($_.deltaMs)})
  $raw=@(Get-Content -LiteralPath (Join-Path $PSScriptRoot "$side/${label}_markers.json") -Raw|ConvertFrom-Json)
  $main=@($raw|Where-Object threadName -eq 'Main Thread')
  $render=@($raw|Where-Object {$_.threadName -match '^Render Thread'})
  $game=@($main|Where-Object {$null -ne $_.costs.PlayerLoop}|ForEach-Object {$_.costs.PlayerLoop.totalMs})
  $gc=@($main|ForEach-Object {if($null -ne $_.costs.'GC.Alloc'){$_.costs.'GC.Alloc'.gcBytes}else{0}})
  $dt=Stats @($valid|ForEach-Object deltaMs)
  $rows+=[pscustomobject]@{side=$side;run=$run;observed=$frames.Count;invalid=$frames.Count-$valid.Count;enemies=@($frames.enemies|Sort-Object -Unique);towers=@($frames.towers|Sort-Object -Unique);draw=Stats @($frames.drawCalls);setPass=Stats @($frames.setPass);deltaMs=$dt;fpsMin=1000/$dt.max;fpsMax=1000/$dt.min;fpsAvg=$valid.Count*1000/($valid.deltaMs|Measure-Object -Sum).Sum;playerLoopMs=Stats $game;mainThreadMs=Stats @($main.frameTimeMs);renderThreadMs=Stats @($render.frameTimeMs);gcAllocBytes=Stats $gc;totalAllocatedBytes=Stats @($frames.totalAllocated);monoUsedBytes=Stats @($frames.monoUsed)}
  $names=@{}
  foreach($frame in $main){foreach($property in $frame.costs.psobject.Properties){if($property.Name -notmatch 'CG\.|CoreGuardian|CommandBuffer|Mesh\.Set|GC\.Alloc|WaitFor'){continue};if(-not $names.ContainsKey($property.Name)){$names[$property.Name]=@{totalMs=0.0;selfMs=0.0;calls=0L;gcBytes=0L}};$acc=$names[$property.Name];$v=$property.Value;$acc.totalMs+=$v.totalMs;$acc.selfMs+=$v.selfMs;$acc.calls+=$v.calls;$acc.gcBytes+=$v.gcBytes}}
  foreach($name in $names.Keys|Sort-Object){$v=$names[$name];$markers+=[pscustomobject]@{side=$side;run=$run;name=$name;validMainFrames=$main.Count;totalMsPerFrame=$v.totalMs/$main.Count;selfMsPerFrame=$v.selfMs/$main.Count;callsPerFrame=$v.calls/$main.Count;gcBytesPerFrame=$v.gcBytes/$main.Count}}
 }
}
$rows|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $PSScriptRoot 'comparison.json') -Encoding UTF8
$markers|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $PSScriptRoot 'marker_summary.json') -Encoding UTF8
$rows|Select-Object side,run,@{n='drawMedian';e={$_.draw.median}},@{n='passMedian';e={$_.setPass.median}},@{n='deltaMedian';e={$_.deltaMs.median}},fpsMin,fpsMax,fpsAvg|ConvertTo-Json
