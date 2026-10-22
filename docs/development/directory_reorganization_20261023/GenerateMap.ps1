$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$inventory=Get-Content -Raw "$root/inventory_before.json"|ConvertFrom-Json
function Destination([string]$p){
    $file=[IO.Path]::GetFileName($p);$ext=[IO.Path]::GetExtension($p).ToLowerInvariant()
    if($p.StartsWith('Assets/Scenes/')){return $p}
    if($p.StartsWith('Assets/_Recovery/')){return 'Assets/Scenes/Recovery/'+$(if($file -eq '0.unity'){'CombatRecovery_Original.unity'}else{'CombatRecovery_Additional.unity'})}
    if($p -eq 'Assets/Game/Fortress.Game.asmdef'){return 'Assets/Scripts/Runtime/Domain/Fortress.Game.asmdef'}
    if($p.StartsWith('Assets/Game/Scripts/')){return $p.Replace('Assets/Game/Scripts/CoreGuardian/','Assets/Scripts/Runtime/Domain/Rules/').Replace('Assets/Game/Scripts/','Assets/Scripts/Runtime/Domain/')}
    if($p.StartsWith('Assets/Game/Tests/')){return $p.Replace('Assets/Game/Tests/','Assets/Scripts/Tests/')}
    if($p.StartsWith('Assets/Editor/')){return 'Assets/Scripts/Editor/Rendering/'+$file}
    if($p.StartsWith('Assets/Scripts/')){
        $group=switch -Regex ($file){'Audio|Sound' {'Audio';break};'Hud|AugmentationView' {'UI';break};'Mask|Outline|PhotoShadow|HitFlash|Emphasis|Glow|Grounding|Feedback|CoreAmbient|CoreParticle' {'Rendering';break};'Motion|Walk|Idle|UnitAnimation|CombatAnimation' {'Characters';break};'Prototype|GridMovement' {'Bootstrap';break};'CombatArt' {'Combat';break};default {'World'}}
        return 'Assets/Scripts/Runtime/'+$(if($group -eq 'Bootstrap'){'Bootstrap/'}else{'Presentation/'+$group+'/'})+$file
    }
    if($p.StartsWith('Assets/Settings/')){return 'Assets/Settings/'+$(if($ext -eq '.inputactions'){'Input/'}else{'Rendering/'})+$file}
    if($p.StartsWith('Assets/Welcome/')){
        if($ext -in @('.cs','.asmdef')){return 'Assets/Scripts/Editor/ProjectWelcome/'+$file}
        if($ext -in @('.png','.jpg')){return 'Assets/Art/Editor/ProjectWelcome/'+$file}
        if($ext -eq '.asset'){return 'Assets/Settings/Editor/'+$file}
        return 'Assets/Documentation/ProjectWelcome/'+$file
    }
    if($p.StartsWith('Assets/Leohpaz/')){return $p.Replace('Assets/Leohpaz/','Assets/Audio/Libraries/')}
    if($p.StartsWith('Assets/TextMesh Pro/')){
        if($p.Contains('/Shaders/')){return $p.Replace('Assets/TextMesh Pro/Shaders/','Assets/Art/Rendering/Shaders/TextMeshPro/')}
        if($p.Contains('/Resources/Fonts & Materials/')){return $p.Replace('Assets/TextMesh Pro/','Assets/Fonts/TextMeshPro/')}
        if($p.Contains('/Resources/TMP Settings.asset')){return 'Assets/Settings/Resources/TMP Settings.asset'}
        if($p.Contains('/Resources/')){return $p.Replace('Assets/TextMesh Pro/','Assets/Art/UI/TextMeshPro/')}
        if($p.Contains('/Fonts/')){return $p.Replace('Assets/TextMesh Pro/','Assets/Fonts/Libraries/TextMeshPro/')}
        return $p.Replace('Assets/TextMesh Pro/','Assets/Documentation/ThirdParty/TextMeshPro/')
    }
    if($p.StartsWith('Assets/AS_Magic_Aura_Construction_Kit_FREE/')){
        $part=$p.Substring('Assets/AS_Magic_Aura_Construction_Kit_FREE/'.Length).Replace('Demo Scene/','')
        if($ext -in @('.wav','.mp3','.ogg')){return 'Assets/Audio/Libraries/MagicAura/SoundEffects/'+$file}
        if($ext -in @('.pdf','.txt','.md')){return 'Assets/Documentation/ThirdParty/MagicAura/'+$file}
        if($ext -eq '.prefab'){return 'Assets/Prefabs/Effects/MagicAura/'+$file}
        return 'Assets/Art/Effects/MagicAura/'+$part
    }
    if($p.StartsWith('Assets/Resources/')){
        $key=$p.Substring('Assets/Resources/'.Length);$category=''
        switch -Regex ($key){
            '^CoreGuardian/Audio/' {$category='Audio';break}
            '^CoreGuardian/Fonts/' {$category='Fonts';break}
            '\.csv$' {$category='Data/Balance';break}
            '\.shader$' {$category='Art/Rendering/Shaders';break}
            '^CoreGuardian/MaskAtlas/' {$category='Art/Atlases/MaskData';break}
            '^CoreGuardian/Hud/' {$category='Art/UI';break}
            '^CoreGuardian/(PlayerFacing|OriginalIdle)/' {$category='Art/Characters/Player';break}
            '^CoreGuardian/OriginalMotion/Player/' {$category='Art/Characters/Player';break}
            '^CoreGuardian/OriginalMotion/' {$category='Art/Characters/Towers';break}
            '^CoreGuardian/(OriginalWalk|VerticalWalk)/' {$category='Art/Characters/Enemies';break}
            '^CoreGuardian/PhotoShadows/' {$category='Art/Effects/Shadows';break}
            '^CoreGuardian/Animation/' {$category=$(if($ext -eq '.prefab'){'Prefabs/Characters/Enemies'}else{'Data/Animation'});break}
            '^CoreGuardian/Art/BossEye/' {$category='Prefabs/Characters/Boss';break}
            '^CoreGuardian/Art/' {$category='Art/Effects/Combat';break}
            default {throw "Unclassified resource $p"}
        }
        return 'Assets/'+$category+'/Resources/'+$key
    }
    if($p.StartsWith('Assets/Art/')){
        if($ext -eq '.spriteatlasv2'){return 'Assets/Art/Atlases/Sprites/'+$file}
        if($p.Contains('/Prefabs/')){return 'Assets/Prefabs/Gameplay/Units/'+$file}
        if($p.Contains('/ContactShapes/')){return 'Assets/Data/Presentation/ContactShapes/'+$file}
        if($p.Contains('/RejectedAnimations/')){return 'Assets/Documentation/Art/RejectedAnimationMetadata/'+($p -split '/')[-2]+'_'+$file}
        if($p.Contains('/Combat/20261010/Fonts/')){return 'Assets/Fonts/Libraries/PixelifySans/'+$file}
        if($p.Contains('/Hud/')){return 'Assets/Documentation/Art/UI/'+$file}
        if($ext -eq '.mat'){
            $name=$file -replace ' ','_'
            if($p.Contains('/CombinedRoute/')){$number=[regex]::Match($file,'\d+').Value;$name='Route_Material_'+$(if($number){([int]$number).ToString('00')}else{'Default'})+'.mat'}
            return 'Assets/Art/Rendering/Materials/Environment/'+$name
        }
        if($p.Contains('/CombinedRoute/') -and $ext -eq '.asset'){
            $match=[regex]::Match($file,'Route_Order(\d+)(?: (\d+))?');$name='Route_Order_'+([int]$match.Groups[1].Value).ToString('00');if($match.Groups[2].Success){$name+='_Part_'+([int]$match.Groups[2].Value).ToString('00')};return 'Assets/Art/Environment/Meshes/Routes/'+$name+'.asset'
        }
        if($p.Contains('/Environment/')){return 'Assets/Art/Environment/'+$(if($ext -eq '.asset'){'Tiles/'}else{'Backgrounds/'})+$file}
        if($p.Contains('/MagitechFortress/')){return $p.Replace('Assets/Art/MagitechFortress/','Assets/Art/Environment/MagitechFortress/')}
        if($ext -eq '.asset'){return 'Assets/Art/Environment/Tiles/'+$file}
        if($ext -in @('.png','.jpg')){
            $role=switch -Regex ($file){'^enemy' {'Enemies';break};'^tower' {'Towers';break};'^player' {'Player';break};'^core' {'Core';break};default {'Environment'}}
            if($role -eq 'Environment'){return 'Assets/Art/Environment/Tiles/Sprites/'+$file}
            return 'Assets/Art/Characters/'+$role+'/Sprites/'+$file
        }
        return 'Assets/Documentation/Art/'+$file
    }
    if($p.StartsWith('Assets/docs/')){return $p.Replace('Assets/docs/','Assets/Documentation/Legacy/')}
    throw "Unclassified asset $p"
}
$map=@($inventory|ForEach-Object {[pscustomobject]@{source=$_.path;destination=Destination $_.path;guid=$_.guid;hash=$_.hash;metaHash=$_.metaHash;resourceKey=$_.resourceKey;className=$_.className;assembly=$_.assembly;bytes=$_.bytes}})
$duplicate=@($map|Group-Object destination|Where-Object Count -gt 1)
if($duplicate.Count){$duplicate|ForEach-Object {Write-Output ($_.Group|ConvertTo-Json)};throw 'Destination collision'}
foreach($row in $map){if($row.source -ne $row.destination -and (Test-Path -LiteralPath $row.destination)){throw ('Destination exists: '+$row.destination)}}
$map|ConvertTo-Json -Depth 6|Set-Content -LiteralPath "$root/move_map.json" -Encoding utf8
$map|Group-Object {($_.destination -split '/')[1]}|Select-Object Count,Name|Sort-Object Name|Format-Table -AutoSize
[pscustomobject]@{files=$map.Count;moves=@($map|Where-Object {$_.source -ne $_.destination}).Count;resourceKeysPreserved=@($map|Where-Object resourceKey).Count}|ConvertTo-Json
