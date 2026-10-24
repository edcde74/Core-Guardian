param([Parameter(Mandatory=$true)][ValidateSet('baseline','current')][string]$Side)
$ErrorActionPreference='Stop'
$workspace=(Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$backup=Join-Path $workspace 'Logs/CG_B_SourceComparison_20261022'
$manifest=Get-Content (Join-Path $backup 'manifest.json') -Raw|ConvertFrom-Json
foreach($item in $manifest){
    $path=Join-Path $workspace $item.Path;$hash=(Get-FileHash -LiteralPath $path).Hash
    if($hash -ne $item.CurrentHash -and $hash -ne $item.BaselineHash){throw "Unknown edit preserved: $($item.Path)"}
    $source=Join-Path (Join-Path $backup $Side) $item.Name
    $expected=if($Side -eq 'current'){$item.CurrentHash}else{$item.BaselineHash}
    if((Get-FileHash -LiteralPath $source).Hash -ne $expected){throw "Backup hash mismatch: $($item.Path)"}
}
foreach($item in $manifest){[IO.File]::WriteAllBytes((Join-Path $workspace $item.Path),[IO.File]::ReadAllBytes((Join-Path (Join-Path $backup $Side) $item.Name)))}
[pscustomobject]@{side=$Side;files=$manifest.Count;restoreHashVerified=$true}|ConvertTo-Json
