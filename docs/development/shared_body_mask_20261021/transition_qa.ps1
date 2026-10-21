$ErrorActionPreference='Stop'
$root=$PSScriptRoot
function Eval([string]$code){
 $raw=& unity command eval --format json --caller plugin --skill unity-cli -- --code $code
 $reply=$raw|ConvertFrom-Json
 if(-not $reply.success -or -not $reply.data.result.success){throw ($raw -join "`n")}
 return $reply.data.result.result
}
$before=Eval 'return new{play=UnityEditor.EditorApplication.isPlaying,compile=UnityEditor.EditorApplication.isCompiling,enabled=UnityEditor.EditorSettings.enterPlayModeOptionsEnabled,options=(int)UnityEditor.EditorSettings.enterPlayModeOptions};'
if($before.play -or $before.compile){throw 'Idle EditMode required'}
$before|ConvertTo-Json|Set-Content "$root/transition_before.json"
$settingsHash=(Get-FileHash ProjectSettings/EditorSettings.asset).Hash
$rows=@();$ownPlay=$false
try {
 foreach($option in 0..3){
  Eval "UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=true;UnityEditor.EditorSettings.enterPlayModeOptions=(UnityEditor.EnterPlayModeOptions)$option;return true;"|Out-Null
  & unity command editor_play --format json --caller plugin --skill unity-cli|Out-Null
  $ownPlay=$true
  Start-Sleep -Seconds 10
  & unity status --until-ready --format json|Out-Null
  $first=Eval 'Application.runInBackground=true;return Time.frameCount;'
  Start-Sleep -Seconds 1
  $live=Eval 'var t=typeof(CoreGuardianSelectionOutline).Assembly.GetType("CoreGuardianBodyMaskCache");var f=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic;return new{frame=Time.frameCount,entries=t.GetProperty("EntryCount",f).GetValue(null),bindings=t.GetProperty("BindingCount",f).GetValue(null)};'
  Eval 'Application.runInBackground=false;return true;'|Out-Null
  & unity command editor_stop --format json --caller plugin --skill unity-cli|Out-Null
  $ownPlay=$false
  Start-Sleep -Seconds 6
  & unity status --until-ready --format json|Out-Null
  $after=Eval 'var t=typeof(CoreGuardianSelectionOutline).Assembly.GetType("CoreGuardianBodyMaskCache");var f=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic;return new{play=UnityEditor.EditorApplication.isPlaying,entries=t.GetProperty("EntryCount",f).GetValue(null),bindings=t.GetProperty("BindingCount",f).GetValue(null),allocated=t.GetProperty("AllocatedCount",f).GetValue(null)};'
  $rows+=[pscustomobject]@{option=$option;firstFrame=$first;live=$live;after=$after}
  $rows|ConvertTo-Json -Depth 5|Set-Content "$root/transition_results.json"
  if($live.frame -le $first -or $after.play -or $after.bindings -ne 0 -or $after.allocated -ne 0){throw 'Transition contract failed'}
 }
}finally{
 if($ownPlay){Eval 'Application.runInBackground=false;return true;'|Out-Null;& unity command editor_stop --format json --caller plugin --skill unity-cli|Out-Null}
 $enabled=if($before.enabled){'true'}else{'false'}
 Eval "UnityEditor.EditorSettings.enterPlayModeOptionsEnabled=$enabled;UnityEditor.EditorSettings.enterPlayModeOptions=(UnityEditor.EnterPlayModeOptions)$($before.options);return true;"|Out-Null
 [pscustomobject]@{beforeHash=$settingsHash;afterHash=(Get-FileHash ProjectSettings/EditorSettings.asset).Hash;enabled=$before.enabled;options=$before.options}|ConvertTo-Json|Set-Content "$root/transition_restored.json"
}
$rows|ConvertTo-Json -Depth 5
