param([Parameter(Mandatory=$true)][ValidateSet('baseline','current')][string]$Side)
$ErrorActionPreference='Stop'
$workspace=(Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$backup=Join-Path $workspace 'Logs/CG_RuntimeValidation_20261022'
$manifest=Get-Content -LiteralPath (Join-Path $backup 'source_manifest.json') -Raw | ConvertFrom-Json
# Caller must confirm idle Unity and hold AssetDatabase/reload locks until this finishes.
# Validate every source and destination before changing any file. Never overwrite unknown edits.
foreach($item in $manifest){
    $target=Join-Path $workspace $item.Path
    $hash=(Get-FileHash -LiteralPath $target).Hash
    if($hash -ne $item.CurrentHash -and $hash -ne $item.BaselineHash){throw "Unknown edit: $($item.Path)"}
    $expected=if($Side -eq 'current'){$item.CurrentHash}else{$item.BaselineHash}
    if((Get-FileHash -LiteralPath (Join-Path "$backup/$Side" $item.Path)).Hash -ne $expected){throw "Backup mismatch: $($item.Path)"}
}
foreach($item in $manifest){[IO.File]::WriteAllBytes((Join-Path $workspace $item.Path),[IO.File]::ReadAllBytes((Join-Path "$backup/$Side" $item.Path)))}
[pscustomobject]@{Side=$Side;Files=$manifest.Count;TimestampUtc=[DateTime]::UtcNow} | ConvertTo-Json
