$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
$outDir=$PSScriptRoot
$html=[IO.File]::ReadAllText((Join-Path $outDir 'preview_boot_v2.html'))
$boot=ConvertFrom-Json ([regex]::Match($html,'const glyph=(\[[^;]+\]);').Groups[1].Value.Replace("'",'"'))
$arrow=ConvertFrom-Json ([regex]::Match($html,'const arrow=(\[[^;]+\]);').Groups[1].Value.Replace("'",'"'))
$colors=@('#00000000','#15282c','#a7c2b5','#567f76','#344c48')
$icon=New-Object Drawing.Bitmap 18,14
for($y=0;$y -lt $boot.Count;$y++){for($x=0;$x -lt $boot[$y].Length;$x++){$index=[int]::Parse($boot[$y].Substring($x,1));if($index){$icon.SetPixel($x+1,$y+2,[Drawing.ColorTranslator]::FromHtml($colors[$index]))}}}
for($y=0;$y -lt $arrow.Count;$y++){for($x=0;$x -lt 5;$x++){if($arrow[$y][$x] -eq '1'){$px=$x+12;$py=$y+4;foreach($d in @(@(-1,0),@(1,0),@(0,-1),@(0,1))){$icon.SetPixel($px+$d[0],$py+$d[1],[Drawing.ColorTranslator]::FromHtml($colors[1]))}}}}
for($y=0;$y -lt $arrow.Count;$y++){for($x=0;$x -lt 5;$x++){if($arrow[$y][$x] -eq '1'){$icon.SetPixel($x+12,$y+4,[Drawing.ColorTranslator]::FromHtml($colors[2]))}}}
$icon.Save((Join-Path $outDir 'slow_boot_v2.png'),[Drawing.Imaging.ImageFormat]::Png)
function Save-Enlarged($source,$factor,$path){
 $result=New-Object Drawing.Bitmap ($source.Width*$factor),($source.Height*$factor)
 $g=[Drawing.Graphics]::FromImage($result)
 $g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
 $g.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::Half
 $dest=New-Object Drawing.Rectangle 0,0,$result.Width,$result.Height
 $g.DrawImage($source,$dest,0,0,$source.Width,$source.Height,[Drawing.GraphicsUnit]::Pixel)
 $result.Save($path,[Drawing.Imaging.ImageFormat]::Png)
 $g.Dispose();$result.Dispose()
}
$solo=New-Object Drawing.Bitmap 32,24
$g=[Drawing.Graphics]::FromImage($solo);$g.Clear([Drawing.ColorTranslator]::FromHtml('#23292f'));$g.DrawImageUnscaled($icon,7,5);$g.Dispose()
Save-Enlarged $solo 8 (Join-Path $outDir 'boot_preview_v2.png')
$sheet=New-Object Drawing.Bitmap 216,112
$g=[Drawing.Graphics]::FromImage($sheet);$g.Clear([Drawing.ColorTranslator]::FromHtml('#23292f'))
$pen=New-Object Drawing.Pen ([Drawing.ColorTranslator]::FromHtml('#48535a'))
$types=@('attack','defense','buff')
for($i=0;$i -lt 3;$i++){$file=Join-Path (Resolve-Path "$outDir/../../..").Path ('Assets/Art/ScreenTests/20260930/Revision2/Sprites/enemy_'+$types[$i]+'_v2.png');$im=[Drawing.Image]::FromFile($file);$g.DrawLine($pen,($i*72+10),103,($i*72+62),103);$g.DrawImageUnscaled($im,($i*72+[math]::Floor((72-$im.Width)/2)),(102-$im.Height));$g.DrawImageUnscaled($icon,($i*72+50),52);$im.Dispose()}
$g.Dispose();$pen.Dispose()
Save-Enlarged $sheet 4 (Join-Path $outDir 'monster_comparison_boot_v2.png')
$icon.Dispose();$solo.Dispose();$sheet.Dispose()
Write-Output 'Rendered: 18x14 transparent icon, 8x icon preview, three original monsters with marker at identical 4x scale.'
