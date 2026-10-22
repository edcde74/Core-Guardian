$ErrorActionPreference='Stop'
$root=$PSScriptRoot;$project=(Get-Location).Path
$map=Get-Content -Raw "$root/move_map.json"|ConvertFrom-Json
$prefixes=Get-Content -Raw "$root/prefix_map.json"|ConvertFrom-Json
$lookup=@{};foreach($r in $map){$lookup[[IO.Path]::GetFullPath((Join-Path $project $r.source))]=$r.destination}
$current=@('AGENTS.md','docs/art/MONSTER_ART_STYLE.md','docs/development/TARGETED_QA.md','docs/development/WORK_ROUTING.md','docs/development/UNITY_HANDOVER.md','docs/development/audio_B_applied_20261019/README.md')
$docs=@(rg --files docs -g '*.md' -g '!development/font_tools_vendor/**' 2>$null)+@('AGENTS.md')
$changes=@()
foreach($path in $docs){
    if($path.Replace('\','/').StartsWith('docs/development/directory_reorganization_20261023/')){continue}
    $full=[IO.Path]::GetFullPath((Join-Path $project $path));$before=[IO.File]::ReadAllText($full);$after=$before;$links=0
    foreach($match in [regex]::Matches($before,'\]\(([^)]+)\)')){
        $target=$match.Groups[1].Value;if($target -match '^(https?:|#|codex:)' -or $target.Contains('"')){continue}
        $plain=$target.Split('#')[0];$fragment=if($target.Contains('#')){'#'+($target -split '#',2)[1]}else{''}
        try{$absolute=[IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetDirectoryName($full)) $plain))}catch{continue}
        if(-not $lookup.ContainsKey($absolute)){continue}
        $next=[IO.Path]::GetRelativePath([IO.Path]::GetDirectoryName($full),(Join-Path $project $lookup[$absolute])).Replace('\','/')+$fragment
        if($target -ne $next){$after=$after.Replace(']('+$target+')',']('+$next+')');$links++}
    }
    if($current -contains $path.Replace('\','/')){
        foreach($r in ($map|Sort-Object {$_.source.Length} -Descending)){$after=$after.Replace($r.source,$r.destination)}
        foreach($r in $prefixes){$after=$after.Replace($r.source,$r.destination)}
    }
    if($after -ne $before){[IO.File]::WriteAllText($full,$after,[Text.UTF8Encoding]::new($false));$changes+=[pscustomobject]@{path=$path;links=$links;currentOwnerDocument=$current -contains $path.Replace('\','/')}}
}
$changes|ConvertTo-Json|Set-Content "$root/document_changes.json" -Encoding utf8
$map|Select-Object source,destination|Export-Csv -LiteralPath "$root/path_map.csv" -NoTypeInformation -Encoding utf8
$index=[Collections.Generic.List[string]]::new();$index.Add('# 프로젝트 파일 위치 색인');$index.Add('');$index.Add('기능별 경로로 정리한 현재 Assets 파일 전체 목록. Resources key, Sprite.name, C# class와 assembly 계약은 보존한다.');$index.Add('')
foreach($group in ($map|Group-Object {($_.destination -split '/')[1]}|Sort-Object Name)){
    $index.Add('## '+$group.Name);$index.Add('');foreach($r in ($group.Group|Sort-Object destination)){$index.Add('- `'+$r.destination+'`')};$index.Add('')
}
[IO.File]::WriteAllText("$root/DIRECTORY_INDEX.md",($index -join "`n").TrimEnd()+"`n",[Text.UTF8Encoding]::new($false))
[pscustomobject]@{documents=$changes.Count;links=($changes.links|Measure-Object -Sum).Sum;indexFiles=$map.Count}|ConvertTo-Json
