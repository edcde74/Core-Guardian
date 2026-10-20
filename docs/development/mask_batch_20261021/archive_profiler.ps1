$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
$rootPath = $PSScriptRoot
$zipPath = Join-Path $rootPath 'profiler_raw.zip'
if (Test-Path -LiteralPath $zipPath) { throw 'Archive already exists' }
$manifest = @()
$archive = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in Get-ChildItem -LiteralPath (Join-Path $rootPath 'after') -Filter '*.data') {
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $file.Name, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
        $manifest += [pscustomobject]@{name=$file.Name;bytes=$file.Length;sha256=(Get-FileHash -LiteralPath $file.FullName).Hash}
    }
} finally { $archive.Dispose() }
$archive = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
try {
    foreach ($item in $manifest) {
        $stream = $archive.GetEntry($item.name).Open()
        try { $hash=[Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($stream)) } finally { $stream.Dispose() }
        if ($hash -ne $item.sha256) { throw "Archive hash mismatch: $($item.name)" }
    }
} finally { $archive.Dispose() }
$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $rootPath 'archive_manifest.json') -Encoding utf8
Write-Output "Verified $($manifest.Count) entries; zip bytes=$((Get-Item -LiteralPath $zipPath).Length)"
