param([int]$GameProcessId)
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class GameWindowCapture {
 [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left,Top,Right,Bottom; }
 [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
 [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr window,out RECT rect);
 [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr window,out RECT rect);
 [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr window,IntPtr dc,uint flags);
 [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
}
"@
[GameWindowCapture]::SetProcessDPIAware() | Out-Null
$taskGame=Get-Process -Id $GameProcessId
$taskGame.Refresh()
$taskHandle=$taskGame.MainWindowHandle
if($taskHandle -eq [IntPtr]::Zero){throw 'Game window is not ready'}
$taskRect=New-Object GameWindowCapture+RECT
[GameWindowCapture]::GetClientRect($taskHandle,[ref]$taskRect) | Out-Null
$taskWidth=$taskRect.Right-$taskRect.Left
$taskHeight=$taskRect.Bottom-$taskRect.Top
$taskBitmap=New-Object System.Drawing.Bitmap($taskWidth,$taskHeight)
$taskGraphics=[System.Drawing.Graphics]::FromImage($taskBitmap)
$taskDC=$taskGraphics.GetHdc()
try{$taskCaptured=[GameWindowCapture]::PrintWindow($taskHandle,$taskDC,3)}finally{$taskGraphics.ReleaseHdc($taskDC)}
$taskOutput=Join-Path $PSScriptRoot 'windows_player_20261002.png'
$taskBitmap.Save($taskOutput,[System.Drawing.Imaging.ImageFormat]::Png)
$taskGraphics.Dispose();$taskBitmap.Dispose()
[GameWindowCapture]::SetForegroundWindow($taskHandle) | Out-Null
@{width=$taskWidth;height=$taskHeight;captured=$taskCaptured;path=$taskOutput;processId=$GameProcessId;title=$taskGame.MainWindowTitle}|ConvertTo-Json
