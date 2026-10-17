using System;
using System.Collections;
using System.Linq;
using CoreGuardian;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class CoreGuardianUpgradeFeedbackPlayModeTests
{
    static Type Find(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).FirstOrDefault(t=>t!=null);
    static void Call(Component g,string method,params object[] args)=>g.GetType().GetMethod(method).Invoke(g,args);
    static void Click(Component g,string name)=>g.transform.Find("CombatHUD").GetComponentsInChildren<Button>(true).Single(b=>b.name==name).onClick.Invoke();
    [UnityTest]
    public IEnumerator RealSuccessfulPlayerAndAllTowerPurchasesScan_WhileRejectedPurchasesDoNot()
    {
#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
#endif
        yield return null;var game=(Component)UnityEngine.Object.FindObjectsByType(Find("CoreGuardianPrototype"),FindObjectsSortMode.None).Single();
        var type=Find("CoreGuardianUpgradeFeedback");Assert.That(type,Is.Not.Null,"Chosen B feedback is not connected yet");
        var feedback=game.GetComponent(type);Assert.That(feedback,Is.Not.Null);
        Func<int> triggers=()=>(int)type.GetProperty("TriggerCount").GetValue(feedback);
        Func<int> active=()=>(int)type.GetProperty("ActiveCount").GetValue(feedback);
        float old=Time.timeScale;
        try
        {
            Time.timeScale=0;
            Click(game,"PlayerGrowth");Click(game,"damage");Assert.That(triggers(),Is.Zero,"Initial zero gold cannot produce a success scan");
            Call(game,"Diagnostic");Click(game,"PlayerSpecs");Click(game,"PlayerBuyTab");Click(game,"damage");Assert.That(triggers(),Is.EqualTo(1));
            var player=(Component)game.GetType().GetField("player").GetValue(game);
            Assert.That(type.GetMethod("IsScanning").Invoke(feedback,new object[]{player.transform}),Is.EqualTo(true));
            Call(game,"UpgradePlayer","rate");Assert.That(triggers(),Is.EqualTo(2));Assert.That(active(),Is.EqualTo(1),"Rapid repeat restarts the one scan without stacking copies");
            Call(game,"UpgradePlayer","invalid-stat");Assert.That(triggers(),Is.EqualTo(2));
            var kinds=new[]{TowerKind.Single,TowerKind.Multi,TowerKind.Support};
            for(int i=0;i<kinds.Length;i++)
            {
                var cell=new Vector2Int(14+i*3,15);Call(game,"Place",kinds[i],cell);Call(game,"HandleWorldClick",cell);
                Click(game,"TowerBuyTab");Click(game,kinds[i]==TowerKind.Support?"Tower_buffDamage":"Tower_damage");
                Assert.That(triggers(),Is.EqualTo(3+i));
                int selected=(int)game.GetType().GetProperty("SelectedTower").GetValue(game);
                var target=(Transform)game.GetType().GetMethod("TowerView").Invoke(game,new object[]{selected});
                Assert.That(type.GetMethod("IsScanning").Invoke(feedback,new object[]{target}),Is.EqualTo(true));
            }
            Assert.That(active(),Is.EqualTo(4));
            Call(game,"StartCombat");Call(game,"UpgradeTower","damage");Assert.That(triggers(),Is.EqualTo(5),"Rejected combat tower upgrade cannot trigger success");
            Time.timeScale=1;yield return new WaitForSeconds(.7f);Assert.That(active(),Is.Zero,"Scan expires without leaving tinted artwork");
        }
        finally{Time.timeScale=old;}
    }
}
