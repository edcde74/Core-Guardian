Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase
$cgRoot = (Get-Location).Path
$cgOut = Join-Path $cgRoot 'docs/art/20261015_Animation'
function Write-Loop([string]$name, [string[]]$roles, [bool]$playerViews) {
    $cgEncoder = New-Object System.Windows.Media.Imaging.GifBitmapEncoder
    $cgFrameCount = 4
    for ($cgFrame = 0; $cgFrame -lt $cgFrameCount; $cgFrame++) {
        $cgColumns = 4
        $cgRows = if ($playerViews) { 2 } else { $roles.Count }
        $cgBitmap = New-Object System.Drawing.Bitmap ($cgColumns*96*2), ($cgRows*112*2)
        $cgGraphics = [System.Drawing.Graphics]::FromImage($cgBitmap)
        $cgGraphics.Clear([System.Drawing.Color]::FromArgb(35,40,46))
        $cgGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
        $cgGraphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
        for ($cgRow=0; $cgRow -lt $cgRows; $cgRow++) {
            $cgRole = if($playerViews) { 'Player' } else { $roles[$cgRow] }
            $cgAtlas = [System.Drawing.Bitmap]::FromFile((Join-Path $cgRoot "Assets/Resources/CoreGuardian/Animation/$cgRole/atlas.png"))
            try {
                for ($cgColumn=0; $cgColumn -lt 4; $cgColumn++) {
                    $cgDirection = if($playerViews) { $cgRow*4+$cgColumn } else { $cgColumn }
                    $cgSource = New-Object System.Drawing.Rectangle ($cgFrame*96), ($cgDirection*112), 96,112
                    $cgDestination = New-Object System.Drawing.Rectangle ($cgColumn*96*2), ($cgRow*112*2),192,224
                    $cgGraphics.DrawImage($cgAtlas,$cgDestination,$cgSource,[System.Drawing.GraphicsUnit]::Pixel)
                }
            } finally { $cgAtlas.Dispose() }
        }
        $cgGraphics.Dispose()
        $cgStream = New-Object System.IO.MemoryStream
        $cgBitmap.Save($cgStream,[System.Drawing.Imaging.ImageFormat]::Png)
        $cgBitmap.Dispose()
        $cgStream.Position=0
        $cgDecoder = New-Object System.Windows.Media.Imaging.PngBitmapDecoder $cgStream,([System.Windows.Media.Imaging.BitmapCreateOptions]::PreservePixelFormat),([System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad)
        $cgMetadata = New-Object System.Windows.Media.Imaging.BitmapMetadata 'gif'
        $cgMetadata.SetQuery('/grctlext/Delay',[uint16]14)
        $cgMetadata.SetQuery('/grctlext/Disposal',[byte]2)
        if($cgFrame -eq 0) {
            $cgMetadata.SetQuery('/appext/application',[System.Text.Encoding]::ASCII.GetBytes('NETSCAPE2.0'))
            $cgMetadata.SetQuery('/appext/data',[byte[]](3,1,0,0,0))
        }
        $cgEncoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($cgDecoder.Frames[0],$null,$cgMetadata,$null))
        $cgStream.Dispose()
    }
    $cgFile = [System.IO.File]::Create((Join-Path $cgOut "$name.gif"))
    try { $cgEncoder.Save($cgFile) } finally { $cgFile.Dispose() }
}
Write-Loop 'monster_walk_2x' @('Attack','Defense','Buff','Boss') $false
Write-Loop 'player_attack_8way_2x' @('Player') $true
