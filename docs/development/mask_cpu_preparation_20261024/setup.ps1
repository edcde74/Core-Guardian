$ErrorActionPreference='Stop'
$root='docs/development/mask_cpu_preparation_20261024'
$source='Assets/Scripts/Runtime/Presentation/Rendering/CoreGuardianSelectionOutline.cs'
Copy-Item -LiteralPath $source -Destination "$root/CoreGuardianSelectionOutline_before.cs"
git -c safe.directory=C:/Users/edcde/OneDrive/Desktop/CD diff --binary -- Assets/Scripts/Runtime/Presentation/Rendering/CoreGuardianSelectionOutline.cs Assets/Scripts/Runtime/Presentation/Rendering/CoreGuardianBodyMaskCache.cs Assets/Scripts/Runtime/Presentation/Rendering/CoreGuardianMaskProducerBatch.cs | Out-File "$root/starting_rendering.diff" -Encoding utf8
$protected=@('Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity','ProjectSettings/PackageManagerSettings.asset','ProjectSettings/TimeManager.asset','ProjectSettings/EditorSettings.asset','ProjectSettings/ProjectSettings.asset','Assets/Scripts/Runtime/Presentation/Rendering/CoreGuardianBodyMaskCache.cs','Assets/Scripts/Runtime/Presentation/Rendering/CoreGuardianMaskProducerBatch.cs','Assets/Scripts/Runtime/Presentation/Rendering/CoreGuardianObjectEmphasis.cs','Assets/Scripts/Runtime/Presentation/Rendering/CoreGuardianPhotoShadow.cs','Assets/Scripts/Runtime/Presentation/UI/CoreGuardianHud.cs')
$protected|ForEach-Object {Get-FileHash -LiteralPath $_}|Select-Object Path,Hash|ConvertTo-Json|Set-Content "$root/protected_before.json" -Encoding utf8
$s=[IO.File]::ReadAllText((Join-Path $pwd $source)).Replace("`r`n","`n")
$s=$s.Replace('    static readonly ProfilerMarker presentSample=',"    static readonly ProfilerMarker sourceFilterSample=new ProfilerMarker(`"CG.Mask.SourceFilter`");`n    static readonly ProfilerMarker boundsCropSample=new ProfilerMarker(`"CG.Mask.BoundsCrop`");`n    static readonly ProfilerMarker quadUploadSample=new ProfilerMarker(`"CG.Mask.QuadUpload`");`n    static readonly ProfilerMarker presentSample=")
$s=$s.Replace("        sources.Clear();`n        foreach(var r in sourceRenderers", "        using(sourceFilterSample.Auto())`n        {`n        sources.Clear();`n        foreach(var r in sourceRenderers")
$s=$s.Replace('        if(sources.Count==0){ReleasePresentation();return;}',"        }`n        if(sources.Count==0){ReleasePresentation();return;}")
$start=$s.IndexOf('        var bounds=sources[0].bounds;')
$end=$s.IndexOf('        var request=new CoreGuardianBodyMaskCache.Request', $start)
if($start -lt 0 -or $end -lt 0){throw 'Expected bounds/crop segment missing'}
$segment=$s.Substring($start,$end-$start)
$segment=$segment.Replace('var bounds=','bounds=').Replace('var lo=','lo=').Replace('var hi=','hi=').Replace('var viewport=','viewport=').Replace('var maskLo=','maskLo=').Replace('var maskHi=','maskHi=')
$segment=$segment.Replace('int radius=Mathf.Clamp(widthPixels,0,8),pad=radius+2;','radius=Mathf.Clamp(widthPixels,0,8);pad=radius+2;')
$segment=$segment.Replace('int w=Mathf.CeilToInt(hi.x-lo.x),h=Mathf.CeilToInt(hi.y-lo.y);','w=Mathf.CeilToInt(hi.x-lo.x);h=Mathf.CeilToInt(hi.y-lo.y);')
$segment=$segment.Replace('int maskWidth=Mathf.CeilToInt(maskHi.x-maskLo.x),maskHeight=Mathf.CeilToInt(maskHi.y-maskLo.y);','maskWidth=Mathf.CeilToInt(maskHi.x-maskLo.x);maskHeight=Mathf.CeilToInt(maskHi.y-maskLo.y);')
$replacement="        Bounds bounds;Vector2 lo,hi,maskLo,maskHi;Rect viewport;int radius,pad,w,h,maskWidth,maskHeight;`n        using(boundsCropSample.Auto())`n        {`n"+$segment+"        }`n"
$s=$s.Substring(0,$start)+$replacement+$s.Substring($end)
$s=$s.Replace('        quad.vertices=quadVertices;quad.RecalculateBounds();','        using(quadUploadSample.Auto()){quad.vertices=quadVertices;quad.RecalculateBounds();}')
$s=$s.Replace('quad.uv=quadUV;','using(quadUploadSample.Auto())quad.uv=quadUV;')
[IO.File]::WriteAllText((Join-Path $pwd $source),$s,[Text.UTF8Encoding]::new($false))
$s=[IO.File]::ReadAllText((Join-Path $pwd 'docs/development/view_sync_20261023/Benchmark.cs'))
$s=$s.Replace('CGViewBenchmark','CGMaskPreparationBenchmark').Replace('docs/development/view_sync_20261023/','docs/development/mask_cpu_preparation_20261024/')
$s=$s.Replace('"CG.Views.All","CG.Views.Enemies","CG.Views.Towers","CG.Views.Projectiles",','"CG.Mask.SourceFilter","CG.Mask.BoundsCrop","CG.Mask.QuadUpload","CG.Mask.Resources","CG.Mask.CacheLookup","CG.Mask.Output","CG.Mask.Parts","CG.Mask.MeshUpload","CG.Mask.Execute","CG.Mask.PageProducer","CG.SpriteReferences.Read",')
$s=$s.Replace('throw new Exception("Expected 1920x1080 / ortho8.5")','throw new Exception("Expected 1920x1080 / ortho8.5; actual "+Screen.width+"x"+Screen.height+" / "+Camera.main.orthographicSize)')
$s=$s.Replace('if(warm-->0)return;','if(gap!=1)throw new Exception("Nonunit engine frame gap: "+gap);'+"`n            if(warm>0){warm--;previousCollection=GC.CollectionCount(0);return;}")
[IO.File]::WriteAllText((Join-Path $pwd "$root/Benchmark.cs"),$s,[Text.UTF8Encoding]::new($false))
@'
# SDD ledger — plan: docs/superpowers/plans/2026-10-24-mask-cpu-preparation.md
User approved proceeding with the plan; Task1 first, no expansion before its go/no-go gate. No commits/build/exe/push, no new worktree against explicitly requested running checkout.
HEAD: 1de8ced4f60dd16d9ec50ff5a9cd76f24c8d88b9. Preserve existing SelectionOutline15-line diff using working-tree backup, not git restore.
Initial Editor: stopped, not changing/paused/compiling/updating; clean combat scene7roots; recording/deep/profileEditor/FrameDebugger OFF; backgroundfalse/capture0/scale1;1920x1080/ortho8.5. No running tests.
Task1 in progress: only SourceFilter, BoundsCrop, QuadUpload scopes added. BoundsCrop locals lifted without expression/order changes. QuadUpload sums vertices+RecalculateBounds and separate UV setter; excludes vertex/UV construction.
Benchmark derives existing bounded fixture. 3 x (15warm+90sample). Warm frames update GC reference; gap!=1 invalidates and stops. Runtime30s safety deadline; stop following runs after any failure. Recorders, Deep OFF, no raw hierarchy traversal.
Pre-flight: Task1 metrics gate Task2; source snapshots, cache overloads and tests are not implemented. Existing restoration code unsubscribes Poll, disposes recorders, stops own Play and confirms editor restoration.
'@ | Set-Content "$root/progress.md" -Encoding utf8
'Original preserved and markers/benchmark prepared.'
