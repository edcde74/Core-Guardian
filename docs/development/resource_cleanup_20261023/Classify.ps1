$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$audit=Get-Content -Raw -LiteralPath "$root/inventory.json"|ConvertFrom-Json
# Runtime folders are established by the current source loaders, not by Resources membership alone.
$runtime=@('Assets/Resources/CoreGuardian/Hud/','Assets/Resources/CoreGuardian/OriginalIdle/Player/','Assets/Resources/CoreGuardian/OriginalMotion/','Assets/Resources/CoreGuardian/OriginalWalk/','Assets/Resources/CoreGuardian/VerticalWalk/','Assets/Resources/CoreGuardian/PhotoShadows/','Assets/Resources/CoreGuardian/PlayerFacing/')
$runtimeExact=@('Assets/Resources/CoreGuardian/Art/CombatB.png')
$codeFiles=@(Get-ChildItem -LiteralPath Assets -Recurse -File -Filter '*.cs')
$code=@($codeFiles|ForEach-Object {[pscustomobject]@{path=$_.FullName;content=[IO.File]::ReadAllText($_.FullName)}})
$rows=@($audit.images|ForEach-Object {
    $image=$_;$reasons=[Collections.Generic.List[string]]::new()
    if($image.live){$reasons.Add('live scene object reference')}
    foreach($asset in $image.referencedBy){$reasons.Add('serialized: '+$asset)}
    if($runtimeExact -contains $image.path){$reasons.Add('runtime Resources.LoadAll CombatArt')}
    foreach($prefix in $runtime){if($image.path.StartsWith($prefix)){$reasons.Add('runtime Resources dynamic family: '+$prefix)}}
    if($image.path -match '(^|/)(TextMesh Pro|Fonts?)/'){$reasons.Add('font resource outside sprite cleanup scope')}
    foreach($source in $code){if($source.content.Contains($image.path)){$reasons.Add('literal code asset path: '+$source.path.Substring((Get-Location).Path.Length+1))}}
    # Text documentation references are recorded separately; imported documentation images are moved out of Assets, not discarded.
    $kind=if($image.path.StartsWith('Assets/docs/')){'documentation-image'}elseif($image.path -match '/(Source|Revision2/Source)/'){'editable-source'}elseif($image.path -match '/Hud/20261013/'){'hud-source'}elseif($image.path -match 'AS_Magic_Aura_Construction_Kit_FREE/'){ 'effect-library'}elseif($image.path -match 'RejectedAnimations|Resources/CoreGuardian/Animation/.*/atlas\.png'){ 'unused-animation-sheet'}else{'unused-image'}
    [pscustomobject]@{path=$image.path;guid=$image.guid;sha256=$image.sha256;metaHash=$image.metaHash;bytes=$image.bytes;kind=$kind;action=if($reasons.Count){'keep'}else{'delete'};reasons=@($reasons);referencedBy=$image.referencedBy}
})
$rows|ConvertTo-Json -Depth 8|Set-Content -LiteralPath "$root/manifest.json" -Encoding utf8
$rows|Group-Object action,kind|ForEach-Object {[pscustomobject]@{group=$_.Name;count=$_.Count;MiB=[math]::Round(($_.Group.bytes|Measure-Object -Sum).Sum/1MB,3)}}|Format-Table -AutoSize
