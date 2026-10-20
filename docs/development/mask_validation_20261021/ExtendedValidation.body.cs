// Appended to the archived CGReferenceOutline source; diagnostic only, outside Assets.
public static class CGExtendedValidation
{
    const string Root="docs/development/mask_validation_20261021/";
    static bool exactSize;
    static bool effectsOnly;
    public static object RunEffects(int role){effectsOnly=true;return Run(role);}
    public static object RunExact(){exactSize=true;return Run(3);}
    static System.Reflection.BindingFlags F=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
    static int MaskCount()=>Resources.FindObjectsOfTypeAll<RenderTexture>().Count(t=>t.name=="Selected unit union mask");
    static object MaskDiff(CGReferenceOutline a,CoreGuardianSelectionOutline b)
    {
        var x=(RenderTexture)typeof(CGReferenceOutline).GetField("mask",F).GetValue(a);var y=(RenderTexture)typeof(CoreGuardianSelectionOutline).GetField("mask",F).GetValue(b);
        var saved=RenderTexture.active;var t=new Texture2D(x.width,x.height,TextureFormat.RGBA32,false);
        try{RenderTexture.active=x;t.ReadPixels(new Rect(0,0,x.width,x.height),0,0);t.Apply();var p=t.GetPixels32();RenderTexture.active=y;t.ReadPixels(new Rect(0,0,x.width,x.height),0,0);t.Apply();var q=t.GetPixels32();int diff=0;for(int i=0;i<p.Length;i++)if(!p[i].Equals(q[i]))diff++;return new{x.width,x.height,newWidth=y.width,newHeight=y.height,diff};}
        finally{RenderTexture.active=saved;UnityEngine.Object.DestroyImmediate(t);}
    }
    public static object Run(int roleIndex)
    {
        if(Application.isPlaying||UnityEngine.Profiling.Profiler.enabled||UnityEngine.FrameDebugger.enabled)throw new System.Exception("Idle EditMode required");
        string[] roles={"Attack","Defense","Buff","Boss","Player","Single","Multi","Support"};
        string[] names={"enemy_attack","enemy_defense","enemy_buff","","Player","tower_single","tower_multi","tower_support"};
        string role=roles[roleIndex],path=role=="Boss"?"Assets/Resources/CoreGuardian/Animation/Boss/BossAnimated.prefab":"Assets/Art/ScreenTests/20261008_Depth/Prefabs/"+names[roleIndex]+".prefab";
        var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        var owned=new List<GameObject>();var mats=new List<Material>();var rows=new List<object>();
        var target=new GameObject[2];var anim=new CoreGuardianUnitAnimation[2];
        var old=new CGReferenceOutline[2,3];var now=new CoreGuardianSelectionOutline[2,3];
        var outputOld=new MeshRenderer[2,3];var outputNow=new MeshRenderer[2,3];
        var contacts=new Material[2];var scans=new Material[2];
        var cameraGo=new GameObject("Owned extended camera");owned.Add(cameraGo);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraGo,scene);
        var cam=cameraGo.AddComponent<Camera>();cam.enabled=false;cam.scene=scene;cam.overrideSceneCullingMask=UnityEditor.SceneManagement.EditorSceneManager.GetSceneCullingMask(scene);cam.orthographic=true;cam.cullingMask=1<<31;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.clear;
        RenderTexture image=null;Texture2D read=null;var active=RenderTexture.active;int baseline=MaskCount(),cases=0,failed=0,empty=0,maxDifference=0;long peakBytes=0;
        try
        {
            for(int i=0;i<2;i++)
            {
                target[i]=UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path));owned.Add(target[i]);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(target[i],scene);
                target[i].transform.position=new Vector3(10000+i*.15f,10000+i*.12f,0);
                // Give overlapping units a defined front/back order. Equal-order prefab clones
                // otherwise change transparent body batching even in a reference-vs-reference control.
                target[i].GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="2D Sprite").sortingOrder+=i*20;
                anim[i]=CoreGuardianUnitAnimation.Attach(target[i],role);
                foreach(var r in target[i].GetComponentsInChildren<SpriteRenderer>(true))r.gameObject.layer=effectsOnly?0:31;
                contacts[i]=new Material(Resources.Load<Shader>("CoreGuardian/PhotoShadowContact"));scans[i]=new Material(Resources.Load<Shader>("CoreGuardian/UpgradeScan"));mats.Add(contacts[i]);mats.Add(scans[i]);
                for(int e=0;e<3;e++)
                {
                    var a=new GameObject("Owned reference "+i+" "+e);var b=new GameObject("Owned current "+i+" "+e);owned.Add(a);owned.Add(b);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(a,scene);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(b,scene);
                    old[i,e]=a.AddComponent<CGReferenceOutline>();old[i,e].enabled=false;now[i,e]=b.AddComponent<CoreGuardianSelectionOutline>();now[i,e].enabled=false;
                    typeof(CoreGuardianSelectionOutline).GetMethod("EnsureMask",F).Invoke(now[i,e],new object[]{512,512});
                }
            }
            int[,] sizes={{256,256},{384,216},{216,384}};float[] zoom={1.7f,.85f,3.4f};Vector2[] dirs={Vector2.left,Vector2.right,Vector2.up,Vector2.down};
            for(int resolution=0;resolution<3;resolution++)
            {
                if(image!=null){cam.targetTexture=null;image.Release();UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(read);}
                int w=sizes[resolution,0],h=sizes[resolution,1];image=new RenderTexture(w,h,24);image.Create();cam.targetTexture=image;cam.aspect=(float)w/h;read=new Texture2D(w,h,TextureFormat.RGBA32,false);
                for(int z=0;z<3;z++)for(int layout=0;layout<3;layout++)for(int d=0;d<4;d++)
                {
                    cam.orthographicSize=zoom[z];cam.transform.position=target[0].transform.position+new Vector3(layout==2?zoom[z]*cam.aspect:0,.45f,-10);target[1].SetActive(layout==1);
                    for(int i=0;i<2;i++)
                    {
                        anim[i].Move(dirs[(d+i)%4],.07f);anim[i].Aim(dirs[d],.07f);if(layout==1)anim[i].ReceiveHit();if(layout==2)anim[i].Fire(dirs[d],4);anim[i].Present(.07f);
                        contacts[i].SetFloat("_Band",(role=="Boss"?22:16)*h/(2*zoom[z])/63.52941f);scans[i].SetFloat("_BandY",target[i].transform.position.y+.4f);scans[i].SetFloat("_Opacity",1);
                        for(int e=0;e<3;e++)
                        {
                            if(exactSize){var mf=typeof(CoreGuardianSelectionOutline).GetField("mask",F);var rt=(RenderTexture)mf.GetValue(now[i,e]);if(rt!=null){rt.Release();UnityEngine.Object.DestroyImmediate(rt);mf.SetValue(now[i,e],null);}}
                            old[i,e].widthPixels=now[i,e].widthPixels=1+d%2;
                            if(e==0){old[i,e].Present(target[i].transform,cam);now[i,e].Present(target[i].transform,cam);}
                            if(e==1){old[i,e].PresentGroundContact(target[i].transform,cam,contacts[i]);now[i,e].PresentGroundContact(target[i].transform,cam,contacts[i]);}
                            if(e==2){old[i,e].PresentOverlay(target[i].transform,cam,scans[i]);now[i,e].PresentOverlay(target[i].transform,cam,scans[i]);}
                            outputOld[i,e]=(MeshRenderer)typeof(CGReferenceOutline).GetField("output",F).GetValue(old[i,e]);outputNow[i,e]=(MeshRenderer)typeof(CoreGuardianSelectionOutline).GetField("output",F).GetValue(now[i,e]);
                            if(outputOld[i,e]!=null)outputOld[i,e].gameObject.layer=31;if(outputNow[i,e]!=null)outputNow[i,e].gameObject.layer=31;
                        }
                    }
                    // Body + approved cast shadow remain identical; compare each output and all three together.
                    var baseVisible=new bool[2,3];for(int i=0;i<2;i++)for(int e=0;e<3;e++){baseVisible[i,e]=old[i,e].IsVisible;if(old[i,e].IsVisible!=now[i,e].IsVisible)throw new System.Exception("Visibility mismatch");}
                    for(int effect=0;effect<4;effect++)
                    {
                        var visible=new bool[2,3];for(int i=0;i<2;i++)for(int e=0;e<3;e++)visible[i,e]=(i==0||layout==1)&&(effect==3||effect==e)&&baseVisible[i,e];
                        for(int i=0;i<2;i++)for(int e=0;e<3;e++){if(outputNow[i,e]!=null)outputNow[i,e].enabled=false;if(e==1)old[i,e].PresentGroundContact(target[i].transform,cam,contacts[i]);if(e==2)old[i,e].PresentOverlay(target[i].transform,cam,scans[i]);if(outputOld[i,e]!=null)outputOld[i,e].enabled=visible[i,e];}
                        cam.Render();RenderTexture.active=image;read.ReadPixels(new Rect(0,0,w,h),0,0);read.Apply();var before=read.GetPixels32();
                        // Both consumers must bind their own material texture immediately before rendering.
                        for(int i=0;i<2;i++)for(int e=0;e<3;e++){if(outputOld[i,e]!=null)outputOld[i,e].enabled=false;if(e==1)now[i,e].PresentGroundContact(target[i].transform,cam,contacts[i]);if(e==2)now[i,e].PresentOverlay(target[i].transform,cam,scans[i]);if(outputNow[i,e]!=null)outputNow[i,e].enabled=visible[i,e];}
                        cam.Render();RenderTexture.active=image;read.ReadPixels(new Rect(0,0,w,h),0,0);read.Apply();var after=read.GetPixels32();int different=0,drawn=0;
                        for(int p=0;p<before.Length;p++){if(!before[p].Equals(after[p]))different++;if(before[p].a>0)drawn++;}
                        cases++;if(drawn==0)empty++;if(different>0){failed++;maxDifference=System.Math.Max(maxDifference,different);if(failed==1){System.IO.File.WriteAllBytes(Root+role+"_first_mismatch.png",read.EncodeToPNG());read.SetPixels32(before);read.Apply();System.IO.File.WriteAllBytes(Root+role+"_first_reference.png",read.EncodeToPNG());var debugRows=new List<object>();for(int i=0;i<2;i++)for(int e=0;e<3;e++){var a=outputOld[i,e];var b=outputNow[i,e];debugRows.Add(new{i,e,oldBounds=a.bounds.ToString("F8"),newBounds=b.bounds.ToString("F8"),oldOrder=a.sortingOrder,newOrder=b.sortingOrder,maskDiff=MaskDiff(old[i,e],now[i,e]),oldVertices=a.GetComponent<MeshFilter>().sharedMesh.vertices.Select(p=>p.ToString("F8")).ToArray(),newVertices=b.GetComponent<MeshFilter>().sharedMesh.vertices.Select(p=>p.ToString("F8")).ToArray()});}System.IO.File.WriteAllText(Root+role+"_first_geometry.json",Newtonsoft.Json.JsonConvert.SerializeObject(debugRows,Newtonsoft.Json.Formatting.Indented));}}
                        rows.Add(new{resolution,w,h,zoom=zoom[z],layout,d,effect,different,drawn});
                        if(resolution==0&&z==0&&layout==1&&d==0&&effect==3)System.IO.File.WriteAllBytes(Root+role+"_composite.png",read.EncodeToPNG());
                    }
                    long bytes=0;for(int i=0;i<2;i++)for(int e=0;e<3;e++){var rt=(RenderTexture)typeof(CoreGuardianSelectionOutline).GetField("mask",F).GetValue(now[i,e]);if(rt!=null)bytes+=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(rt);}peakBytes=System.Math.Max(peakBytes,bytes);
                }
            }
        }
        finally
        {
            var ownedMaskSizes=new List<object>();foreach(var go in owned)if(go!=null)foreach(var component in go.GetComponentsInChildren<MonoBehaviour>(true))if(component is CoreGuardianSelectionOutline||component is CGReferenceOutline){var rt=(RenderTexture)component.GetType().GetField("mask",F).GetValue(component);if(rt!=null)ownedMaskSizes.Add(new{component=component.GetType().Name,rt.width,rt.height});}
            System.IO.File.WriteAllText(Root+role+"_owned_sizes.json",Newtonsoft.Json.JsonConvert.SerializeObject(ownedMaskSizes,Newtonsoft.Json.Formatting.Indented));
            RenderTexture.active=active;cam.targetTexture=null;if(image!=null){image.Release();UnityEngine.Object.DestroyImmediate(image);}if(read!=null)UnityEngine.Object.DestroyImmediate(read);
            // Preview-scene non-ExecuteAlways behaviours never awaken, so invoke owned cleanup explicitly.
            foreach(var go in owned)if(go!=null)foreach(var component in go.GetComponentsInChildren<MonoBehaviour>(true))
                if(component!=null&&(component is CoreGuardianSelectionOutline||component is CGReferenceOutline||component is CoreGuardianPhotoShadow))component.GetType().GetMethod("OnDestroy",F)?.Invoke(component,null);
            foreach(var go in owned)if(go!=null)UnityEngine.Object.DestroyImmediate(go);foreach(var m in mats)if(m!=null)UnityEngine.Object.DestroyImmediate(m);UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
        }
        var result=new{role,cases,failed,empty,maxDifference,peakBytes,baseline,after=MaskCount(),rows};System.IO.File.WriteAllText(Root+role+(effectsOnly?"_effects":exactSize?"_exact":"")+"_pixels.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return new{role,exactSize,effectsOnly,cases,failed,empty,maxDifference,peakBytes,baseline,after=MaskCount()};
    }
}
