using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class CapturePlayerBatch
{
    public static void Run()
    {
        try
        {
            var result=CapturePlayer.Main();Debug.Log(result);
            File.WriteAllText("docs/development/player_facing_20261016/capture_completed.txt",result.ToString());
            EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
