param([switch]$Apply)
$ErrorActionPreference='Stop'
$root=$PSScriptRoot
$project=(Get-Location).Path
$manifest=Get-Content -Raw -LiteralPath "$root/manifest.json"|ConvertFrom-Json
$mapping=@{}
foreach($row in $manifest|Where-Object {$_.action -eq 'delete' -and $_.kind -eq 'documentation-image'}){
    $mapping[[IO.Path]::GetFullPath((Join-Path $project $row.path))]=Join-Path $root ('imported_evidence/'+$row.path.Substring('Assets/docs/'.Length))
}
$changes=@()
$docs=@(rg --files docs -g '*.md' -g '!development/font_tools_vendor/**' 2>$null)
foreach($path in $docs){
    $full=[IO.Path]::GetFullPath((Join-Path $project $path));$original=[IO.File]::ReadAllText($full);$updated=$original
    foreach($match in [regex]::Matches($original,'\]\(([^)]+)\)')){
        $target=$match.Groups[1].Value
        if($target -match '^(https?:|#|codex:)' -or $target -match '\s+"'){continue}
        try{$resolved=[IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetDirectoryName($full)) $target))}catch{continue}
        if(-not $mapping.ContainsKey($resolved)){continue}
        $newTarget=[IO.Path]::GetRelativePath([IO.Path]::GetDirectoryName($full),$mapping[$resolved]).Replace('\','/')
        $changes+=[pscustomobject]@{document=$path;old=$target;new=$newTarget}
        $updated=$updated.Replace(']('+$target+')',']('+$newTarget+')')
    }
    if($updated -ne $original -and $Apply){[IO.File]::WriteAllText($full,$updated,[Text.UTF8Encoding]::new($false))}
}
$changes|ConvertTo-Json -Depth 5|Set-Content -LiteralPath "$root/document_links.json" -Encoding utf8
[pscustomobject]@{applied=[bool]$Apply;links=$changes.Count;documents=@($changes.document|Sort-Object -Unique).Count}|ConvertTo-Json
