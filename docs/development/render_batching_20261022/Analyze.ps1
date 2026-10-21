$ErrorActionPreference='Stop'
$root=$PSScriptRoot
function Stats($values) {
    $all=@($values | ForEach-Object {[double]$_})
    $valid=@($all | Where-Object {$_ -gt 0 -and -not [double]::IsNaN($_) -and -not [double]::IsInfinity($_)} | Sort-Object)
    if(!$valid.Count){return @{count=0;invalid=$all.Count}}
    $sum=($valid | Measure-Object -Sum).Sum
    return @{count=$valid.Count;invalid=$all.Count-$valid.Count;mean=$sum/$valid.Count;min=$valid[0];max=$valid[-1];p95=$valid[[math]::Ceiling($valid.Count*.95)-1];p99=$valid[[math]::Ceiling($valid.Count*.99)-1];fpsMin=1000/$valid[-1];fpsMax=1000/$valid[0];fpsMean=$valid.Count*1000/$sum}
}
$trials=@(0..5 | ForEach-Object {
    $trial=$_
    $capture=Get-Content "$root/settings_$trial.json" -Raw | ConvertFrom-Json
    $native=@(Get-Content "$root/native_$trial.json" -Raw | ConvertFrom-Json)
    [pscustomobject]@{trial=$trial;srp=$capture.srp;statsFrames=$capture.rows.Count;nativeFrames=$native.Count;draw=($capture.rows.draw | Measure-Object -Average).Average;setPass=($capture.rows.setPass | Measure-Object -Average).Average;observedNative=Stats $native.frameMs;observedUnscaled=Stats $capture.rows.dtMs;nativeGC=($native.gc | Measure-Object -Average).Average;gpuNonzero=@($native.threads | Where-Object {$_.frameGpuTimeMs -gt 0}).Count}
})
$on=@($trials | Where-Object srp)
$off=@($trials | Where-Object {-not $_.srp})
$onDraw=($on.draw|Measure-Object -Average).Average;$offDraw=($off.draw|Measure-Object -Average).Average
$onSet=($on.setPass|Measure-Object -Average).Average;$offSet=($off.setPass|Measure-Object -Average).Average
$output=[ordered]@{kind='Frozen injected rendering settings diagnostic; no normal combat or product improvement verdict';trials=$trials;on=@{draw=$onDraw;setPass=$onSet};off=@{draw=$offDraw;setPass=$offSet};drawSaved=$onDraw-$offDraw;drawSavedPct=100*($onDraw-$offDraw)/$onDraw;setPassSaved=$onSet-$offSet;setPassSavedPct=100*($onSet-$offSet)/$onSet;limits=@('180 stats rows per trial; native captures contain 178/179 frames due recording boundary; no one-to-one join or imputed frames','Time.timeScale=0, driver game.enabled=false; animation and unscaled effects are not fully frozen','No pixel equivalence test for the temporary SRP setting; original restored','Native frame samples report observed FPS only; no full contiguous-window FPS claim','Profiler overhead included; GPU profiler reports unsupported in current Editor/D3D12','No product changes or savings have been accepted')}
$output|ConvertTo-Json -Depth 16|Set-Content "$root/analysis.json"
$trials | Select-Object trial,srp,statsFrames,nativeFrames,draw,setPass,@{n='nativeMs';e={$_.observedNative.mean}},@{n='minFPS';e={$_.observedNative.fpsMin}},@{n='maxFPS';e={$_.observedNative.fpsMax}},@{n='meanFPS';e={$_.observedNative.fpsMean}}
