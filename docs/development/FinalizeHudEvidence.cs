using System.IO;
using UnityEngine;
using UnityEditor;
public static class FinalizeHudEvidence
{
    public static string Main(){string dest="docs/development/hud_sprite_review_20261013";string[] files={"HudAppliedPanels.png","HudAppliedBacks.png","HudAppliedReveal.png","HudAppliedPanelsFinal.png","HudAppliedTiersFinal.png","HudAppliedFrontFinal.png"};int count=0;
        foreach(var name in files){string source="Assets/"+name;if(File.Exists(source)){File.Copy(source,dest+"/"+name,true);AssetDatabase.DeleteAsset(source);count++;}}
        string initial="Assets/docs/development/hud_sprite_review_20261013/hud_applied_initial.png";if(File.Exists(initial)){File.Copy(initial,dest+"/hud_applied_initial.png",true);AssetDatabase.DeleteAsset(initial);count++;}
        string empty="Assets/docs/development/hud_sprite_review_20261013";if(Directory.Exists(empty)&&Directory.GetFileSystemEntries(empty).Length==0)AssetDatabase.DeleteAsset(empty);
        AssetDatabase.Refresh();AssetDatabase.SaveAssets();return "Moved "+count+" HUD screenshots out of Assets; original scenes unchanged";
    }
}
