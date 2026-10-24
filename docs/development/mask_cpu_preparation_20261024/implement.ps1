$ErrorActionPreference='Stop'
$root='docs/development/mask_cpu_preparation_20261024'
$outline='Assets/Scripts/Runtime/Presentation/Rendering/CoreGuardianSelectionOutline.cs'
$cache='Assets/Scripts/Runtime/Presentation/Rendering/CoreGuardianBodyMaskCache.cs'
Copy-Item -LiteralPath $outline -Destination "$root/CoreGuardianSelectionOutline_instrumented_baseline.cs"
Copy-Item -LiteralPath $cache -Destination "$root/CoreGuardianBodyMaskCache_before.cs"
Copy-Item -LiteralPath "$root/CoreGuardianMaskSourceFrame_candidate.cs" -Destination 'Assets/Scripts/Runtime/Presentation/Rendering/CoreGuardianMaskSourceFrame.cs'
$s=[IO.File]::ReadAllText((Join-Path $pwd $outline)).Replace("`r`n","`n")
$s=$s.Replace('    static readonly ProfilerMarker sourceFilterSample=',"    readonly CoreGuardianMaskSourceFrame sourceFrame=new CoreGuardianMaskSourceFrame();`n    static readonly ProfilerMarker sourceFilterSample=")
$s=$s.Replace("        using(boundsCropSample.Auto())`n        {`n        bounds=sources[0].bounds;foreach(var r in sources)bounds.Encapsulate(r.bounds);", "        using(boundsCropSample.Auto())`n        {`n        sourceFrame.Capture(sources);`n        bounds=sourceFrame.Values[0].Bounds;foreach(var r in sourceFrame.Values)bounds.Encapsulate(r.Bounds);")
$s=$s.Replace('CoreGuardianBodyMaskCache.Begin(maskBinding,request,out sample)','CoreGuardianBodyMaskCache.Begin(maskBinding,request,sourceFrame.Samples,out sample)')
$s=$s.Replace('var front=sources[0];int frontLayer=SortingLayer.GetLayerValueFromID(front.sortingLayerID);','var front=sourceFrame.Values[0];int frontLayer=SortingLayer.GetLayerValueFromID(front.SortingLayerID);')
$s=$s.Replace("        foreach(var r in sources)`n        {`n            int layer=SortingLayer.GetLayerValueFromID(r.sortingLayerID);", "        foreach(var r in sourceFrame.Values)`n        {`n            int layer=SortingLayer.GetLayerValueFromID(r.SortingLayerID);")
$s=$s.Replace('r.sortingOrder>=front.sortingOrder','r.SortingOrder>=front.SortingOrder').Replace('output.sortingLayerID=front.sortingLayerID;output.sortingOrder=front.sortingOrder+4;','output.sortingLayerID=front.SortingLayerID;output.sortingOrder=front.SortingOrder+4;')
$s=$s.Replace("        foreach(var source in sources)`n        {`n            var shape=Geometry(source.sprite);", "        foreach(var source in sourceFrame.Values)`n        {`n            var shape=Geometry(source.Sprite);")
$s=$s.Replace('vp,shape.texture,source.color.a,','vp,shape.texture,source.Color.a,')
$s=$s.Replace('static Matrix4x4 PartMatrix(SpriteRenderer part)=>part.transform.localToWorldMatrix*Matrix4x4.Scale(new Vector3(part.flipX?-1:1,part.flipY?-1:1,1));','static Matrix4x4 PartMatrix(CoreGuardianMaskSourceSample part)=>part.LocalToWorld*Matrix4x4.Scale(new Vector3(part.FlipX?-1:1,part.FlipY?-1:1,1));')
$s=$s.Replace('readonly List<SpriteRenderer> parts=new List<SpriteRenderer>();','readonly List<CoreGuardianMaskSourceSample> parts=new List<CoreGuardianMaskSourceSample>();')
$start=$s.IndexOf('    void DrawMaskParts(Matrix4x4 vp)');$end=$s.IndexOf('    void EnsureResources()',$start)
if($start -lt 0 -or $end -lt 0){throw 'DrawMaskParts segment missing'}
$segment=$s.Substring($start,$end-$start).Replace('foreach(var part in sources)','foreach(var part in sourceFrame.Values)').Replace('part.sprite','part.Sprite').Replace('part.color.a','part.Color.a')
$s=$s.Substring(0,$start)+$segment+$s.Substring($end)
$s=$s.Replace('internal void ReleasePresentation(){CoreGuardianMaskOutputBatch.Release(this);','internal void ReleasePresentation(){sourceFrame.Clear();CoreGuardianMaskOutputBatch.Release(this);')
[IO.File]::WriteAllText((Join-Path $pwd $outline),$s,[Text.UTF8Encoding]::new($false))
$s=[IO.File]::ReadAllText((Join-Path $pwd $cache)).Replace("`r`n","`n")
$insert=@'
        internal Part(in CoreGuardianMaskSourceSample captured,CoreGuardianMaskAtlas atlas)
        {
            renderer=captured.Renderer;sprite=captured.Sprite;source=captured.Source;sourceVersion=captured.SourceVersion;
            mapped=source;layer=-1;
            if(atlas!=null&&atlas.TryGetSprite(sprite,out var item,out _)){mapped=item.page;layer=item.layer;}
            mappedVersion=ReferenceEquals(mapped,source)?sourceVersion:mapped.updateCount;
            alpha=captured.Color.a;flipX=captured.FlipX;flipY=captured.FlipY;matrix=captured.LocalToWorld;
        }
'@
$s=$s.Replace('        public bool Equals(Part b)=>',$insert+"`n"+'        public bool Equals(Part b)=>')
$s=$s.Replace("    internal static bool Begin(Lease lease,in Request request,out Sample sample)`n    {", "    internal static bool Begin(Lease lease,in Request request,out Sample sample)=>Begin(lease,request,null,out sample);`n    internal static bool Begin(Lease lease,in Request request,IReadOnlyList<CoreGuardianMaskSourceSample> capturedSources,out Sample sample)`n    {")
$s=$s.Replace('        lease.scratch.Clear();foreach(var source in request.sources)lease.scratch.Add(new Part(source,request.atlas));',"        lease.scratch.Clear();`n        if(capturedSources==null)foreach(var source in request.sources)lease.scratch.Add(new Part(source,request.atlas));`n        else for(int i=0;i<capturedSources.Count;i++)lease.scratch.Add(new Part(capturedSources[i],request.atlas));")
[IO.File]::WriteAllText((Join-Path $pwd $cache),$s,[Text.UTF8Encoding]::new($false))
'A candidate applied; original APIs and cache compatibility comparisons retained.'
