$ErrorActionPreference='Stop'
. "$PSScriptRoot/analyze.ps1" | Out-Null
$pooled=@();$costs=@();$resources=@()
foreach($side in @('before','after')){
 $frames=@();$native=@()
 foreach($run in 0..2){
  $frames+=@(Get-Content "$PSScriptRoot/$side/normal_${run}_n128_frames.json" -Raw|ConvertFrom-Json)
  $native+=@(Get-Content "$PSScriptRoot/$side/normal_${run}_n128_markers.json" -Raw|ConvertFrom-Json)
  $res=Get-Content "$PSScriptRoot/$side/normal_${run}_n128_resources.json" -Raw|ConvertFrom-Json
  $resources+=[pscustomobject]@{side=$side;run=$run;bound=$res.maskCount;boundBytes=$res.bytes;cache=$res.cache}
 }
 $valid=@($frames|Where-Object {$_.deltaMs -gt 0 -and -not [double]::IsNaN($_.deltaMs) -and -not [double]::IsInfinity($_.deltaMs)})
 $dt=Stats @($valid.deltaMs)
 $pooled+=[pscustomobject]@{side=$side;n=$frames.Count;excluded=$frames.Count-$valid.Count;deltaMs=$dt;fpsMin=1000/$dt.max;fpsMax=1000/$dt.min;fpsAvg=$valid.Count*1000/($valid.deltaMs|Measure-Object -Sum).Sum;draw=Stats @($frames.drawCalls);setPass=Stats @($frames.setPass)}
 $main=@($native|Where-Object threadName -eq 'Main Thread');$active=@($main|Where-Object {$_.costs.'CG.Mask.Present'.calls -gt 0})
 foreach($name in @('CG.Mask.Present','CG.Mask.Execute','CG.Mask.Parts','CG.Mask.CacheLookup','CG.PhotoShadow.Contact','Assembly-CSharp.dll!::CoreGuardianObjectEmphasis.LateUpdate() [Invoke]','GC.Alloc')){
  $ms=@($active|ForEach-Object {if($null -ne $_.costs.$name){$_.costs.$name.totalMs}else{0}})
  $calls=@($active|ForEach-Object {if($null -ne $_.costs.$name){$_.costs.$name.calls}else{0}})
  $gc=@($active|ForEach-Object {if($null -ne $_.costs.$name){$_.costs.$name.gcBytes}else{0}})
  $costs+=[pscustomobject]@{side=$side;name=$name;nativeMain=$main.Count;active=$active.Count;time=Stats $ms;calls=Stats $calls;gc=Stats $gc}
 }
}
$deltas=@()
foreach($key in @('fpsAvg','fpsMin','fpsMax')){$b=$pooled[0].$key;$a=$pooled[1].$key;$deltas+=[pscustomobject]@{name=$key;before=$b;after=$a;change=$a-$b;changePercent=($a-$b)/$b*100}}
foreach($key in @('draw','setPass','deltaMs')){$b=$pooled[0].$key.mean;$a=$pooled[1].$key.mean;$deltas+=[pscustomobject]@{name=$key;before=$b;after=$a;saved=$b-$a;savedPercent=($b-$a)/$b*100}}
[pscustomobject]@{pooled=$pooled;costs=$costs;resources=$resources;deltas=$deltas}|ConvertTo-Json -Depth 8|Set-Content "$PSScriptRoot/summary.json"
$deltas|Format-Table
