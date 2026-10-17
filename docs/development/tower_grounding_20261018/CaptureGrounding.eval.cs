        if(UnityEditor.EditorApplication.isPlaying||UnityEditor.EditorApplication.isCompiling)throw new System.Exception("Idle stopped editor required");
        const string dir="docs/development/tower_grounding_20261018/";
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();bool dirty=scene.isDirty;
        var game=UnityEngine.Object.FindFirstObjectByType<CoreGuardianPrototype>();var source=Camera.main;
        var host=new GameObject("Temporary grounding capture"){hideFlags=HideFlags.HideAndDontSave};
        var baseline=new System.Collections.Generic.List<Action>();var samples=new System.Collections.Generic.List<object>();var points=new System.Collections.Generic.List<object>();
        var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.Create();var previous=RenderTexture.active;
        try
        {
            var camGo=new GameObject("Temporary capture camera");camGo.transform.SetParent(host.transform,false);
            var camera=camGo.AddComponent<Camera>();camera.CopyFrom(source);camera.enabled=false;
            camera.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);camera.targetTexture=rt;
            var data=camera.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();data.SetRenderer(-1);data.renderPostProcessing=source.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing;
            data.antialiasing=UnityEngine.Rendering.Universal.AntialiasingMode.None;
            for(int i=0;i<3;i++)
            {
                var root=UnityEngine.Object.Instantiate(game.towerArt[i],host.transform);root.transform.position=game.World(new Vector2(13+4*i,7));
                foreach(var depth in root.GetComponentsInChildren<ArtDepthSorting>())depth.enabled=false;
                var body=root.GetComponentsInChildren<SpriteRenderer>().Single(r=>r.name=="2D Sprite");body.sortingOrder=900;
                foreach(var r in root.transform.Find("그림자").GetComponentsInChildren<SpriteRenderer>(true))
                {
                    var p=r.transform.localPosition;var s=r.transform.localScale;var c=r.color;bool active=r.gameObject.activeSelf;
                    baseline.Add(()=>{r.transform.localPosition=p;r.transform.localScale=s;r.color=c;r.gameObject.SetActive(active);});
                }
                var unit=CoreGuardianUnitAnimation.Attach(root,new[]{"Single","Multi","Support"}[i]);unit.Present(.5f);
                var shade=root.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name=="TowerBaseShade");
                var cast=root.transform.Find("그림자/짧은 투사 그림자");baseline.Add(()=>{shade.enabled=false;cast.gameObject.SetActive(false);});
                var screen=camera.WorldToScreenPoint(body.bounds.center);
                points.Add(new{role=new[]{"Single","Multi","Support"}[i],x=screen.x,y=1080-screen.y});
                samples.Add(new {role=i,bodyBottom=body.bounds.min.y,contactBottom=root.transform.Find("그림자/접촉 그림자").GetComponent<SpriteRenderer>().bounds.min.y});
            }
            System.Action<string> save=path=>{camera.Render();RenderTexture.active=rt;var t=new Texture2D(1920,1080,TextureFormat.RGB24,false);try{t.ReadPixels(new Rect(0,0,1920,1080),0,0);t.Apply();System.IO.File.WriteAllBytes(dir+path,t.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(t);}};
            save("applied_edit.png");foreach(var action in baseline)action();save("before_edit.png");
            var json=Newtonsoft.Json.JsonConvert.SerializeObject(new{points,samples,cameraZoomChanged=false,density=1080/(2*source.orthographicSize),note="Temporary instantiated original prefabs in actual scene, manually presented idle pose; camera capture, no HUD; source camera and scene untouched"},Newtonsoft.Json.Formatting.Indented);
            System.IO.File.WriteAllText(dir+"capture.json",json);
        }
        finally{RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(host);}
        return new {scene=scene.path,dirtyBefore=dirty,dirtyAfter=scene.isDirty,scenes=UnityEngine.SceneManagement.SceneManager.sceneCount};
