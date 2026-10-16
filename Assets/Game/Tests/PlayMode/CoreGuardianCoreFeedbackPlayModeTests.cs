using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using CoreGuardian;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class CoreGuardianCoreFeedbackPlayModeTests
{
    [UnityTest]
    public IEnumerator ActualCoreExitKeepsDamageAndExistingHit_AndShowsRedNumberAboveRightWithMonsterFont()
    {
#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
        yield return null;
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CoreGuardianPrototype")).First(t=>t!=null);
        var game=(Component)UnityEngine.Object.FindObjectsByType(type,FindObjectsSortMode.None).Single();
        var session=(Session)type.GetProperty("Session").GetValue(game);var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        float old=Time.timeScale;
        try
        {
            Time.timeScale=0;session.StartCombat();int before=session.CoreEnergy;
            var enemy=session.DebugSpawn(EnemyKind.Attack,10000);
            type.GetMethod("Update",flags).Invoke(game,null);
            Assert.That(session.CoreEnergy,Is.EqualTo(before-enemy.Spec.CoreDamage));
            var core=UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).First(r=>r.name=="2D Sprite"&&r.sprite!=null&&r.sprite.name=="core");
            var block=new MaterialPropertyBlock();core.GetPropertyBlock(block);Assert.That(block.GetFloat("_HitFlashAmount"),Is.GreaterThan(.4f));
            Assert.That(GameObject.Find("CoreHit"),Is.Not.Null);
            var hud=game.GetComponents<Component>().First(c=>c.GetType().Name=="CoreGuardianHud");
            hud.GetType().GetMethod("ShowDamageNumber",flags).Invoke(hud,new object[]{enemy,12});
            hud.GetType().GetMethod("LateUpdate",flags).Invoke(hud,null);
            var number=GameObject.Find("CoreDamageNumber");Assert.That(number,Is.Not.Null);
            var text=number.GetComponents<Component>().First(c=>c.GetType().Name=="TextMeshProUGUI");
            var monster=GameObject.Find("HitDamage").GetComponents<Component>().First(c=>c.GetType().Name=="TextMeshProUGUI");
            Assert.That(text.GetType().GetProperty("font").GetValue(text),Is.SameAs(monster.GetType().GetProperty("font").GetValue(monster)));
            var color=(Color)text.GetType().GetProperty("color").GetValue(text);Assert.That(color.r,Is.GreaterThan(color.g*2));
            var rt=number.GetComponent<RectTransform>();Assert.That(rt.pivot.x,Is.Zero);
            var canvas=number.GetComponentInParent<Canvas>();Canvas.ForceUpdateCanvases();
            var corners=new Vector3[4];rt.GetWorldCorners(corners);
            var screenCore=Camera.main.WorldToScreenPoint(core.bounds.max);
            var screenLeft=RectTransformUtility.WorldToScreenPoint(null,corners[0]);
            Assert.That(screenLeft.x,Is.GreaterThan(screenCore.x),"Number left edge must clear core right edge");
            Assert.That(RectTransformUtility.WorldToScreenPoint(null,rt.position).y,Is.GreaterThan(screenCore.y),"Number anchor must be above core top");
            var coreLabel=game.transform.Find("CombatHUD/TopStatus/Status_Core").GetComponent<UnityEngine.UI.Text>();
            Assert.That(coreLabel.color.b,Is.LessThan(1),"Upper core label reacts alongside core");
        }
        finally{Time.timeScale=old;}
#else
        yield return null;
#endif
    }
}
