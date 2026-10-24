$ErrorActionPreference='Stop'
$root=$PSScriptRoot
function Stats($values){
    $v=@($values | Sort-Object)
    if($v.Count -eq 0){throw 'Empty metric'}
    [ordered]@{count=$v.Count;min=$v[0];max=$v[-1];mean=($v|Measure-Object -Average).Average;p95=$v[[Math]::Ceiling($v.Count*.95)-1]}
}
$sides=[ordered]@{}
foreach($side in @('baseline','a2')){
    $all=@();$allMarkers=@();$allCounts=@();$runs=@()
    $completion=Get-Content "$root/$side/completion.json" -Raw | ConvertFrom-Json
    if($completion.error -or $completion.completedRuns -ne 3){throw "Incomplete $side"}
    foreach($i in 0..2){
        $f=@(Get-Content "$root/$side/run_${i}_frames.json" -Raw | ConvertFrom-Json)
        $c=@(Get-Content "$root/$side/run_${i}_counts.json" -Raw | ConvertFrom-Json)
        $valid=@($f | Where-Object {$_.frameTimeMs -gt 0 -and -not [double]::IsNaN($_.frameTimeMs) -and -not [double]::IsInfinity($_.frameTimeMs)})
        $markers=@($valid|Where-Object {$_.tickMs -gt 0})
        if($markers.Count -ne 180 -or $valid.Count -lt 180 -or $valid.Count -gt 181 -or $c.Count -ne 180){throw "Unexpected frame count $side/$i"}
        if(@($c | Where-Object {$_.enemies -ne 128 -or $_.towers -ne 15}).Count){throw "Population drift $side/$i"}
        $all+=$valid;$allMarkers+=$markers;$allCounts+=$c
        $stats=Stats $valid.frameTimeMs
        $runs+=@{run=$i;frameMs=$stats;fpsMean=1000/$stats.mean;fpsMin=1000/$stats.max;fpsMax=1000/$stats.min;tickMs=(Stats $markers.tickMs);viewMs=(Stats $markers.viewMs);gcBytes=(Stats $valid.gcBytes);tickGC=(Stats $markers.tickGC);viewGC=(Stats $markers.viewGC);firstFrameMs=$valid[0].frameTimeMs;extraFramesWithoutTick=$valid.Count-$markers.Count;invalidFrames=$f.Count-$valid.Count}
    }
    $frame=Stats $all.frameTimeMs
    $sides[$side]=[ordered]@{runs=$runs;frameMs=$frame;fpsMean=1000/$frame.mean;fpsMin=1000/$frame.max;fpsMax=1000/$frame.min;tickMs=(Stats $allMarkers.tickMs);viewMs=(Stats $allMarkers.viewMs);gcBytes=(Stats $all.gcBytes);tickGC=(Stats $allMarkers.tickGC);viewGC=(Stats $allMarkers.viewGC);drawCalls=(Stats $allCounts.drawCalls);setPass=(Stats $allCounts.setPass);monoUsed=(Stats $allCounts.monoUsed);totalAllocated=(Stats $allCounts.totalAllocated)}
}
$equivalence=@(foreach($i in 0..2){
    $b=Get-Content "$root/baseline/run_${i}_outcome.json" -Raw | ConvertFrom-Json
    $a=Get-Content "$root/a2/run_${i}_outcome.json" -Raw | ConvertFrom-Json
    $checks=[ordered]@{}
    foreach($key in @('events','CoreEnergy','Gold','Crystals','phase','enemies','projectiles','damage')){
        $checks[$key]=(($b.$key | ConvertTo-Json -Depth 20 -Compress) -ceq ($a.$key | ConvertTo-Json -Depth 20 -Compress))
    }
    [ordered]@{run=$i;baselineEvents=$b.events.Count;currentEvents=$a.events.Count;checks=$checks;equal=(@($checks.Values|Where-Object {-not $_}).Count -eq 0)}
})
$deltas=[ordered]@{}
foreach($key in @('frameMs','tickMs','viewMs','gcBytes','tickGC','viewGC','drawCalls','setPass')){
    $b=$sides.baseline[$key].mean;$a=$sides.a2[$key].mean
    $deltas[$key]=[ordered]@{before=$b;after=$a;saved=$b-$a;percent=if($b -eq 0){$null}else{100*($b-$a)/$b}}
}
$result=[ordered]@{sides=$sides;equivalence=$equivalence;costDeltas=$deltas;fpsIncrease=$sides.a2.fpsMean-$sides.baseline.fpsMean;fpsIncreasePercent=100*($sides.a2.fpsMean-$sides.baseline.fpsMean)/$sides.baseline.fpsMean;limits=@('Editor injected workload only','Fixed simulation delta, FPS uses native frameTimeMs instead of Time.deltaTime','All 542 valid native frames retained per side including profiler-start and two no-Tick boundary frames; marker metrics use 540 actual ticks','Full GC includes diagnostic observers and frame sampling','Enemy replacements outside Tick/View markers','Editor memory includes caches and Profiler buffers; no retained-memory improvement claim','No matched pixel comparison or normal 30-round performance result')}
[IO.File]::WriteAllText("$root/analysis_a2.json",(($result | ConvertTo-Json -Depth 30).Replace("`r`n","`n")),[Text.UTF8Encoding]::new($false))
[ordered]@{deltas=$deltas;beforeFps=$sides.baseline.fpsMean;afterFps=$sides.a2.fpsMean;fpsIncreasePercent=$result.fpsIncreasePercent;equivalence=$equivalence} | ConvertTo-Json -Depth 10
