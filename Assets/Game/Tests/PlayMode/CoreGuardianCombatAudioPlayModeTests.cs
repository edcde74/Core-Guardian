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

public class CoreGuardianCombatAudioPlayModeTests
{
    [UnityTest]
    public IEnumerator RealSingleTowerLaunchesAndDeveloperControlsRespectMuteAndVoiceLimit()
    {
#if UNITY_EDITOR
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
        yield return null;
        var gameType=Type.GetType("CoreGuardianPrototype, Assembly-CSharp");
        var game=(MonoBehaviour)UnityEngine.Object.FindObjectsByType(gameType,FindObjectsSortMode.None).Single();
        var audioType=Type.GetType("CoreGuardianCombatAudio, Assembly-CSharp");
        var settings=Type.GetType("CoreGuardianSoundSettings, Assembly-CSharp");
        var audio=(MonoBehaviour)game.GetComponent(audioType);
        var volume=settings.GetProperty("Volume");var candidate=settings.GetProperty("Candidate");
        bool hadVolume=PlayerPrefs.HasKey("CoreGuardian.SoundVolume"),hadCandidate=PlayerPrefs.HasKey("CoreGuardian.ShotCandidate");
        float oldVolume=PlayerPrefs.GetFloat("CoreGuardian.SoundVolume");int oldCandidate=PlayerPrefs.GetInt("CoreGuardian.ShotCandidate");
        float oldTime=Time.timeScale;
        try
        {
            Time.timeScale=0;game.enabled=false;
            volume.SetValue(null,.65f);candidate.SetValue(null,0);
            var session=(Session)gameType.GetProperty("Session").GetValue(game);
            // Injected functional QA: isolated board/resources, real Session shots. No balance claim.
            session.StartDiagnosticEncounter();
            session.SetBoard(30,30,new Vector2Int(20,20),new[]{Vector2Int.zero,new Vector2Int(20,0),new Vector2Int(20,20)});
            string reason;var tower=session.TryInstall(TowerKind.Single,new Vector2Int(1,1),out reason);
            Assert.That(tower,Is.Not.Null,reason);
            var multi=session.TryInstall(TowerKind.Multi,new Vector2Int(2,1),out reason);Assert.That(multi,Is.Not.Null,reason);
            var multiTwo=session.TryInstall(TowerKind.Multi,new Vector2Int(2,2),out reason);Assert.That(multiTwo,Is.Not.Null,reason);
            var support=session.TryInstall(TowerKind.Support,new Vector2Int(1,2),out reason);Assert.That(support,Is.Not.Null,reason);
            var played=audioType.GetProperty("PlayedCount");
            audioType.GetMethod("Configure").Invoke(audio,new object[]{session});
            audioType.GetMethod("Configure").Invoke(audio,new object[]{session});
            int singleShots=0,multiShots=0,multiTwoShots=0;
            session.ProjectileLaunched+=p=>{if(p.Source=="Tower:"+tower.Id)singleShots++;if(p.Source=="Tower:"+multi.Id)multiShots++;if(p.Source=="Tower:"+multiTwo.Id)multiTwoShots++;};
            session.StartCombat();
            foreach(float distance in new[]{0f,1f,2f})session.DebugSpawn(EnemyKind.Defense,distance).Health=10000;
            session.Tick(.05f,new Vector2(-100,-100));
            Assert.That(singleShots,Is.EqualTo(1));Assert.That(multiShots,Is.EqualTo(3));Assert.That(multiTwoShots,Is.GreaterThan(0));
            Assert.That((int)played.GetValue(audio),Is.EqualTo(3),"Single plus one audio volley per multi tower, with support silent");
            var voices=audio.GetComponentsInChildren<AudioSource>();Assert.That(voices.Length,Is.EqualTo(4));
            Assert.That(voices.Count(v=>v.isPlaying&&v.clip.name=="B_BrighterAttack"),Is.EqualTo(2));
            foreach(var source in voices){Assert.That(source.loop,Is.False);Assert.That(source.spatialBlend,Is.Zero);}
            yield return new WaitForSecondsRealtime(.85f);
            // Before tower cooldowns expire, a real player launch uses approved candidate3.
            int beforePlayer=(int)played.GetValue(audio);
            session.Tick(.01f,new Vector2(1,1));
            Assert.That((int)played.GetValue(audio),Is.EqualTo(beforePlayer+1));
            Assert.That(voices.Any(v=>v.isPlaying&&v.clip.name=="03_Glyph"),Is.True);
            foreach(var voice in voices)voice.Stop();
            var preview=audioType.GetMethod("Preview");
            for(int i=0;i<3;i++)
            {
                candidate.SetValue(null,i);Assert.That(preview.Invoke(audio,null),Is.EqualTo(true));
                Assert.That(voices.Any(v=>v.isPlaying&&v.clip.name=="03_Aura"),Is.True,"Old candidate preferences must not change approved single audio");
            }
            preview.Invoke(audio,null);
            Assert.That(preview.Invoke(audio,null),Is.EqualTo(false),"Pool must reject a fifth overlapping sound");
            audio.enabled=false;Assert.That(voices.All(v=>!v.isPlaying),Is.True);audio.enabled=true;
            Assert.That(audioType.GetMethod("PreviewMulti").Invoke(audio,null),Is.EqualTo(true));
            Assert.That(voices.Any(v=>v.isPlaying&&v.clip.name=="B_BrighterAttack"),Is.True);
            foreach(var voice in voices)voice.Stop();
            var canvas=game.transform.Find("CombatHUD");
            var buttonType=Type.GetType("UnityEngine.UI.Button, UnityEngine.UI");
            var sliderType=Type.GetType("UnityEngine.UI.Slider, UnityEngine.UI");
            var menu=canvas.Find("DeveloperMenu").GetComponent(buttonType);
            yield return new WaitForSecondsRealtime(.25f); // HUD phase refresh closes developer tools on phase changes.
            var click=buttonType.GetProperty("onClick").GetValue(menu);click.GetType().GetMethod("Invoke").Invoke(click,null);
            yield return new WaitForSecondsRealtime(.25f);
            var panel=canvas.Find("SoundControls");Assert.That(panel.gameObject.activeInHierarchy,Is.True);
            Assert.That(panel.Find("Listen_Support"),Is.Null);
            Assert.That(panel.Find("PreviewPlayer"),Is.Not.Null);
            Assert.That(panel.Find("PreviewMulti"),Is.Not.Null);
            Assert.That(panel.Find("Audition_Multi"),Is.Null);
            var slider=panel.Find("SoundVolume").GetComponent(sliderType);
            var fill=(RectTransform)sliderType.GetProperty("fillRect").GetValue(slider);
            var handle=(RectTransform)sliderType.GetProperty("handleRect").GetValue(slider);
            Assert.That(fill.rect.height,Is.LessThanOrEqualTo(8.01f),"Thin track must stay inside the panel");
            Assert.That(handle.rect.height,Is.LessThanOrEqualTo(28.01f));
            sliderType.GetProperty("value").SetValue(slider,0f);yield return null;
            Assert.That(volume.GetValue(null),Is.EqualTo(0f));Assert.That(voices.All(v=>v.volume==0),Is.True);
            Assert.That(preview.Invoke(audio,null),Is.EqualTo(false));
            sliderType.GetProperty("value").SetValue(slider,.37f);yield return null;
            Assert.That(voices.All(v=>Mathf.Abs(v.volume-.37f*.55f)<.00001f),Is.True);
            Assert.That(PlayerPrefs.GetFloat("CoreGuardian.SoundVolume"),Is.EqualTo(.37f));
            click.GetType().GetMethod("Invoke").Invoke(click,null);yield return new WaitForSecondsRealtime(.25f);
            Assert.That(panel.gameObject.activeSelf,Is.False);
        }
        finally
        {
            Time.timeScale=oldTime;
            if(hadVolume)PlayerPrefs.SetFloat("CoreGuardian.SoundVolume",oldVolume);else PlayerPrefs.DeleteKey("CoreGuardian.SoundVolume");
            if(hadCandidate)PlayerPrefs.SetInt("CoreGuardian.ShotCandidate",oldCandidate);else PlayerPrefs.DeleteKey("CoreGuardian.ShotCandidate");
            PlayerPrefs.Save();
        }
#else
        yield return null;
#endif
    }
}
