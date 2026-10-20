$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.IO.Compression
$zipPath=Join-Path $PSScriptRoot 'profiler_raw.zip'
if(Test-Path -LiteralPath $zipPath){throw 'Archive already exists'}
$manifest=@()
$archive=[IO.Compression.ZipFile]::Open($zipPath,[IO.Compression.ZipArchiveMode]::Create)
try {
 foreach($side in @('before','after','extended')) {
  foreach($file in Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot $side) -Filter '*.data' -File) {
   $name=$side+'/'+$file.Name
   [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive,$file.FullName,$name,[IO.Compression.CompressionLevel]::Optimal)|Out-Null
   $manifest+=[pscustomobject]@{name=$name;bytes=$file.Length;sha256=(Get-FileHash -LiteralPath $file.FullName).Hash}
  }
 }
}finally{$archive.Dispose()}
$archive=[IO.Compression.ZipFile]::OpenRead($zipPath)
try {
 foreach($item in $manifest) {
  $entry=$archive.GetEntry($item.name)
  if($entry.Length -ne $item.bytes){throw 'Archive length mismatch'}
  $stream=$entry.Open();$sha=[Security.Cryptography.SHA256]::Create()
  try{$hash=[BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-','')}finally{$sha.Dispose();$stream.Dispose()}
  if($hash -ne $item.sha256){throw ('Archive hash mismatch: '+$item.name)}
 }
}finally{$archive.Dispose()}
$manifest|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $PSScriptRoot 'archive_manifest.json') -Encoding UTF8
"Verified $($manifest.Count) entries; zip bytes=$((Get-Item -LiteralPath $zipPath).Length)"
