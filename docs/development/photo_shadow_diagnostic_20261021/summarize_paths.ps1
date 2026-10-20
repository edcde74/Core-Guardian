$ErrorActionPreference='Stop'
function Stats([double[]]$v){
 $s=@($v|Sort-Object);if(!$s.Count){return $null}
 $pos=($s.Count-1)*.95;$lo=[math]::Floor($pos);$hi=[math]::Ceiling($pos)
 [pscustomobject]@{n=$s.Count;mean=($s|Measure-Object -Average).Average;p95=$s[$lo]+($s[$hi]-$s[$lo])*($pos-$lo);min=$s[0];max=$s[-1]}
}
$summary=@();$maxima=@()
foreach($side in 'before','after','extended'){
 $allDelta=@();$allPhoto=@()
 foreach($run in 0..2){
  $label="normal_${run}_n128"
  $frames=@(Get-Content "$PSScriptRoot/$side/${label}_frames.json" -Raw|ConvertFrom-Json)
  $dt=@($frames|Where-Object {$_.deltaMs -gt 0 -and -not [double]::IsInfinity($_.deltaMs) -and -not [double]::IsNaN($_.deltaMs)}|ForEach-Object deltaMs)
  $allDelta+=$dt
  $raw=@(Get-Content "$PSScriptRoot/$side/${label}_markers.json" -Raw|ConvertFrom-Json|Where-Object threadName -eq 'Main Thread')
  $photo=@(foreach($f in $raw){$property=$f.costs.psobject.Properties|Where-Object Name -like '*CoreGuardianPhotoShadow.LateUpdate*'|Select-Object -First 1;if($property){[pscustomobject]@{frame=$f.frame;main=$f.frameTimeMs;photo=$property.Value.totalMs;self=$property.Value.selfMs}}})
  $allPhoto+=@($photo.photo)
  $maxima+=[pscustomobject]@{side=$side;run=$run;rawFrames=$raw.Count;photoFrames=$photo.Count;worst=($photo|Sort-Object photo -Descending|Select-Object -First 1)}
  $delta=Stats $dt
  $summary+=[pscustomobject]@{side=$side;run=$run;observed=$frames.Count;invalid=$frames.Count-$dt.Count;delta=$delta;fpsMin=1000/$delta.max;fpsMax=1000/$delta.min;fpsAvg=$dt.Count*1000/($dt|Measure-Object -Sum).Sum;photo=Stats @($photo.photo);enemies=@($frames.enemies|Sort-Object -Unique);towers=@($frames.towers|Sort-Object -Unique)}
 }
 $delta=Stats $allDelta
 $summary+=[pscustomobject]@{side=$side;run='pooled';delta=$delta;fpsMin=1000/$delta.max;fpsMax=1000/$delta.min;fpsAvg=$allDelta.Count*1000/($allDelta|Measure-Object -Sum).Sum;photo=Stats $allPhoto}
}
$paths=@()
foreach($side in 'after','extended'){
 $rows=@(Get-Content "$PSScriptRoot/$side/photo_paths.json" -Raw|ConvertFrom-Json)
 foreach($group in ($rows|Group-Object capture)){
  $valid=@($group.Group|Where-Object calls -gt 0)
  $stages=@{}
  foreach($r in $valid){foreach($p in $r.stages.psobject.Properties){if(!$stages.ContainsKey($p.Name)){$stages[$p.Name]=0.0};$stages[$p.Name]+=$p.Value}}
  $means=@{};foreach($name in $stages.Keys){$means[$name]=$stages[$name]/$valid.Count}
  $paths+=[pscustomobject]@{side=$side;capture=$group.Name;valid=$valid.Count;photo=Stats @($valid.photoMs);maxCall=Stats @($valid.maxPhotoCallMs);stageMeanMs=$means;worst=($valid|Sort-Object photoMs -Descending|Select-Object -First 1)}
 }
}
$summary|ConvertTo-Json -Depth 7|Set-Content "$PSScriptRoot/frame_summary.json"
$maxima|ConvertTo-Json -Depth 6|Set-Content "$PSScriptRoot/max_photo_summary.json"
$paths|ConvertTo-Json -Depth 10|Set-Content "$PSScriptRoot/path_summary.json"
$summary|Select-Object side,run,@{n='frameMean';e={$_.delta.mean}},@{n='p95';e={$_.delta.p95}},fpsMin,fpsMax,fpsAvg,@{n='photoMean';e={$_.photo.mean}},@{n='photoMax';e={$_.photo.max}}|Format-Table -AutoSize
$paths|Select-Object side,capture,valid,@{n='cast';e={$_.stageMeanMs.'CG.PhotoShadow.Cast'}},@{n='contact';e={$_.stageMeanMs.'CG.PhotoShadow.Contact'}},@{n='parts';e={$_.stageMeanMs.'CG.Mask.Parts'}},@{n='execute';e={$_.stageMeanMs.'CG.Mask.Execute'}},@{n='output';e={$_.stageMeanMs.'CG.Mask.Output'}}|Format-Table -AutoSize
