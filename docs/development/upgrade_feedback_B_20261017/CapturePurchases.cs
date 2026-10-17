using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoreGuardian;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

public static class CaptureAppliedUpgradeB
{
    public static async Task<object> Main()
    {
        var game=UnityEngine.Object.FindAnyObjectByType<CoreGuardianPrototype>();if(!Application.isPlaying||!game.Ready)throw new Exception("Owned Play required");
        bool adapter=game.enabled,movement=game.player.enabled;var position=game.player.transform.position;var source=Camera.main;
        game.enabled=false;game.player.enabled=false;
        string dir=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"docs/development/upgrade_feedback_B_20261017");Directory.CreateDirectory(Path.Combine(dir,"frames"));
        GameObject cameraRoot=null;RenderTexture rt=null;Texture2D image=null;var old=RenderTexture.active;
        try
        {
            game.Diagnostic();game.Place(TowerKind.Single,new Vector2Int(18,15));game.Place(TowerKind.Multi,new Vector2Int(23,15));game.Place(TowerKind.Support,new Vector2Int(28,15));
            game.player.transform.position=game.World(new Vector2(13,15))+Vector3.up*.11f;
            cameraRoot=new GameObject("Temporary Applied Upgrade Capture"){hideFlags=HideFlags.DontSave};var camera=cameraRoot.AddComponent<Camera>();camera.CopyFrom(source);camera.enabled=false;
            float density=source.pixelHeight/(2*source.orthographicSize);var center=game.World(new Vector2(20.5f,15));
            camera.transform.SetPositionAndRotation(new Vector3(center.x,center.y+.5f,source.transform.position.z),source.transform.rotation);
            camera.orthographicSize=115/density;camera.aspect=800/230f;camera.allowHDR=false;camera.allowMSAA=false;
            var data=camera.GetUniversalAdditionalCameraData();data.SetRenderer(-1);data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.None;data.volumeLayerMask=source.GetUniversalAdditionalCameraData().volumeLayerMask;
            rt=new RenderTexture(800,230,24,RenderTextureFormat.ARGB32);rt.Create();camera.targetTexture=rt;image=new Texture2D(800,230,TextureFormat.RGB24,false);
            var hud=game.transform.Find("CombatHUD");Action<string> click=name=>hud.GetComponentsInChildren<Button>(true).Single(b=>b.name==name).onClick.Invoke();
            await Task.Delay(100);
            click("PlayerGrowth");click("damage");
            foreach(var tower in game.Session.Towers.ToArray())
            {game.HandleWorldClick(tower.Position);click("TowerBuyTab");click(tower.Kind==TowerKind.Support?"Tower_buffDamage":"Tower_damage");}
            var feedback=game.GetComponent<CoreGuardianUpgradeFeedback>();if(feedback.TriggerCount!=4)throw new Exception("Four actual successful purchases required");
            var times=new List<float>();var active=new List<int>();float start=Time.realtimeSinceStartup;
            for(int i=0;i<36;i++)
            {
                await Task.Delay(25);camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,800,230),0,0);image.Apply();
                File.WriteAllBytes(Path.Combine(dir,"frames",i.ToString("D3")+".png"),image.EncodeToPNG());times.Add(Time.realtimeSinceStartup-start);active.Add(feedback.ActiveCount);
                if(i==5)ScreenCapture.CaptureScreenshot(Path.Combine(dir,"actual-purchase-hud.png"));
            }
            if(!active.Contains(4)||active.Last()!=0)throw new Exception("Actual scan must be visible and finish during capture");
            return new {passed=true,frames=36,times,active,triggers=feedback.TriggerCount,density,conditions="Actual Session success via HUD purchase callbacks,diagnostic resources;stationary relocated player and adapter paused;production scan ages with normal game deltaTime,no effect-clock injection;800x230 same pixel density camera crop without HUD;separate actual 1920x1080 Overlay screenshot;not human play or balance"};
        }
        finally
        {
            RenderTexture.active=old;if(cameraRoot!=null)UnityEngine.Object.DestroyImmediate(cameraRoot);if(image!=null)UnityEngine.Object.DestroyImmediate(image);
            if(rt!=null){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}game.player.transform.position=position;game.player.enabled=movement;game.enabled=adapter;
        }
    }
}
