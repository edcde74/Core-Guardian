$ErrorActionPreference='Stop'
$root=Join-Path $PSScriptRoot ''
$before=Get-Content (Join-Path $root 'original.json') -Raw|ConvertFrom-Json
$after=Get-Content (Join-Path $root 'candidate.json') -Raw|ConvertFrom-Json
function Flatten($value,$prefix,$map){
    if($null -eq $value){$map[$prefix]='null';return}
    if($value -is [System.Management.Automation.PSCustomObject]){foreach($p in $value.PSObject.Properties){if($p.Name -notin @('identity','globalTime','saved')){Flatten $p.Value ($prefix+'.'+$p.Name) $map}};return}
    if($value -is [System.Array]){for($i=0;$i -lt $value.Length;$i++){Flatten $value[$i] ($prefix+'['+$i+']') $map};return}
    $map[$prefix]=[string]$value
}
$pairs=@(@{name='original_self_320';a=$before.rows[0];b=$before.rows[2]},@{name='original_self_321';a=$before.rows[1];b=$before.rows[3]},@{name='candidate_self_320';a=$after.rows[0];b=$after.rows[2]},@{name='candidate_self_321';a=$after.rows[1];b=$after.rows[3]},@{name='cross_320';a=$before.rows[0];b=$after.rows[0]},@{name='cross_321';a=$before.rows[1];b=$after.rows[1]})
$summary=@(foreach($pair in $pairs){
    $a=$pair.a;$b=$pair.b;$ab=[IO.File]::ReadAllBytes((Join-Path (Get-Location) ($a.saved.prefix+'.rgba')));$bb=[IO.File]::ReadAllBytes((Join-Path (Get-Location) ($b.saved.prefix+'.rgba')))
    if($ab.Length -ne $bb.Length){throw 'Pixel size mismatch'}
    $pixels=0;$channels=0;$maxDelta=0;$examples=@();$width=$a.rect[2];$minX=$width;$minY=$a.rect[3];$maxX=-1;$maxY=-1
    for($i=0;$i -lt $ab.Length;$i+=4){$different=$false;for($c=0;$c -lt 4;$c++){$delta=[Math]::Abs([int]$ab[$i+$c]-[int]$bb[$i+$c]);if($delta -gt 0){$different=$true;$channels++;$maxDelta=[Math]::Max($maxDelta,$delta)}};if($different){$pixels++;$x=($i/4)%$width;$y=[Math]::Floor(($i/4)/$width);$minX=[Math]::Min($minX,$x);$minY=[Math]::Min($minY,$y);$maxX=[Math]::Max($maxX,$x);$maxY=[Math]::Max($maxY,$y);if($examples.Count -lt 12){$examples+=@{x=$x;y=$y;before=@($ab[$i..($i+3)]);after=@($bb[$i..($i+3)])}}}}
    $am=@{};$bm=@{};Flatten $a '' $am;Flatten $b '' $bm
    $diffs=@(foreach($key in @(@($am.Keys)+@($bm.Keys)|Sort-Object -Unique)){if($am[$key] -cne $bm[$key]){[pscustomobject]@{path=$key;before=$am[$key];after=$bm[$key]}}})
    [pscustomobject]@{name=$pair.name;id=$a.id;differentPixels=$pixels;differentChannels=$channels;maxChannelDelta=$maxDelta;localDifferenceBounds=@($minX,$minY,$maxX,$maxY);examples=$examples;stateDifferences=$diffs}
})
$summary|ConvertTo-Json -Depth 35|Set-Content (Join-Path $root 'comparison.json')
$summary|Select-Object name,differentPixels,differentChannels,maxChannelDelta,@{n='stateDifferenceCount';e={$_.stateDifferences.Count}}|Format-Table -AutoSize
$summary|Where-Object {$_.name -like 'cross*'}|ForEach-Object {$_.stateDifferences|Select-Object -First 12}|ConvertTo-Json -Depth 8
