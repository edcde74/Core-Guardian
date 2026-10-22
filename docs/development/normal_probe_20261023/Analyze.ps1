param([string]$InputRoot=$PSScriptRoot)
$ErrorActionPreference='Stop'
$root=$InputRoot
if(Test-Path -LiteralPath "$root/result.json"){
    $r=Get-Content -Raw -LiteralPath "$root/result.json" | ConvertFrom-Json
}else{
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive=[IO.Compression.ZipFile]::OpenRead("$root/raw.zip")
    try{
        $reader=[IO.StreamReader]::new($archive.GetEntry('result.json').Open())
        try{$r=$reader.ReadToEnd()|ConvertFrom-Json}finally{$reader.Dispose()}
    }finally{$archive.Dispose()}
}
if($r.running){throw 'Observation is still running; no final verdict'}
function FrameStats($rows) {
    $values=@($rows | ForEach-Object {$_.ms} | Where-Object {$_ -gt 0 -and -not [double]::IsNaN($_) -and -not [double]::IsInfinity($_)} | Sort-Object)
    if($values.Count -eq 0){return $null}
    $sum=($values|Measure-Object -Sum).Sum
    [pscustomobject]@{count=$values.Count; invalid=$rows.Count-$values.Count; meanMs=$sum/$values.Count; medianMs=$values[[math]::Floor(($values.Count-1)*0.5)]; p95Ms=$values[[math]::Ceiling($values.Count*0.95)-1]; p99Ms=$values[[math]::Ceiling($values.Count*0.99)-1]; minMs=$values[0]; maxMs=$values[-1]; minFps=1000/$values[-1]; maxFps=1000/$values[0]; avgFps=$values.Count*1000/$sum; frameGaps=($rows|Where-Object {$_.gap -ne 1}|Measure-Object).Count}
}
$markers=@($r.frames[0].counters.psobject.Properties.Name | ForEach-Object {
    $name=$_;$entries=@($r.frames|ForEach-Object {$_.counters.$name});$v=@($entries|Where-Object {$_.valid -and $null -ne $_.value}|ForEach-Object {$_.value}); $positive=@($v|Where-Object {$_ -gt 0})
    $distinct=@($v|Sort-Object -Unique).Count
    [pscustomobject]@{name=$name;count=$v.Count;invalid=$entries.Count-$v.Count;distinct=$distinct;usable=$distinct -gt 1 -and $v.Count -gt 0;positive=$positive.Count;zeroOrNegative=$v.Count-$positive.Count;meanRaw=if($v.Count){($v|Measure-Object -Average).Average}else{$null};maxRaw=if($v.Count){($v|Measure-Object -Maximum).Maximum}else{$null}}
})
$summary=[pscustomobject]@{
    running=$r.running;reason=$r.reason;elapsedWallSec=$r.elapsed;completed=$r.completed;wave=$r.wave;phase=$r.phase;errors=$r.errors;gameRenderedFrames=$r.renderedFrames;renderedPerWallSecond=$r.renderedFrames/$r.elapsed
    allObserved=(FrameStats @($r.frames))
    combatOnly=(FrameStats @($r.frames|Where-Object {$_.phase -eq 'Combat'}))
    byWave=@($r.frames|Group-Object wave|ForEach-Object {[pscustomobject]@{wave=$_.Name;stats=FrameStats @($_.Group);peakAlive=($_.Group.alive|Measure-Object -Maximum).Maximum;peakProjectiles=($_.Group.projectiles|Measure-Object -Maximum).Maximum}})
    counters=$markers
    memory=[pscustomobject]@{samples=$r.memory.Count;first=$r.memory[0];last=$r.memory[-1];sampledPeakAllocated=($r.memory.totalAllocated|Measure-Object -Maximum).Maximum;sampledPeakMono=($r.memory.monoUsed|Measure-Object -Maximum).Maximum;sampledPeakMaskBytes=($r.memory|ForEach-Object {($_.renderTextures.bytes|Measure-Object -Sum).Sum}|Measure-Object -Maximum).Maximum}
    caveats=@('Single current Editor observation; no comparable before; savings N/A','Early normal rounds under scripted legal decisions; not human play or 30R completion','120s run recorder values frozen due to missing WrapAroundWhenCapacityReached; those costs/GC/draw are unusable. Separate fixed 15s counter check only','Warmup 30 frames; LastValue read on Editor update can lag; inclusive CPU counters not additive','Resource scans every 5s and JSON checkpoints every 10s can affect following frame; no spikes removed','GPU and pure Render Thread unmeasured; no graphics API or package changes','Observed interval memory peaks only; no Memory Profiler snapshot or whole-process leak verdict; processWorkingSet API returned 0 and is invalid','VSync and auto tick/Editor scheduling affect FPS; not low-end Player benchmark; render count separately reported')
}
$summary|ConvertTo-Json -Depth 18|Set-Content -LiteralPath "$root/summary.json" -Encoding utf8
$summary|Select-Object reason,elapsedWallSec,completed,wave,phase,allObserved,combatOnly,errors|ConvertTo-Json -Depth 5
$markers|Where-Object {$_.name -in @('Main Thread','PlayerLoop','GPU Frame Time','GC Allocated In Frame','Draw Calls Count','SetPass Calls Count')}|Format-Table -AutoSize
