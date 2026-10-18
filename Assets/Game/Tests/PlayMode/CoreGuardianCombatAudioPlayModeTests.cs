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
            var played=audioType.GetProperty("PlayedCount");
            audioType.GetMethod("Configure").Invoke(audio,new object[]{session});
            audioType.GetMethod("Configure").Invoke(audio,new object[]{session});
            int singleShots=0,multiShots=0;
            session.ProjectileLaunched+=p=>{if(p.Source=="Tower:"+tower.Id)singleShots++;if(p.Source=="Tower:"+multi.Id)multiShots++;};
            session.StartCombat();
            foreach(float distance in new[]{0f,1f,2f})session.DebugSpawn(EnemyKind.Defense,distance).Health=10000;
            session.Tick(.05f,new Vector2(-100,-100));
            Assert.That(singleShots,Is.EqualTo(1));Assert.That(multiShots,Is.GreaterThan(0));
            Assert.That((int)played.GetValue(audio),Is.EqualTo(1),"Only the real single shot should play, with no duplicate subscription");
            var voices=audio.GetComponentsInChildren<AudioSource>();Assert.That(voices.Length,Is.EqualTo(4));
            foreach(var source in voices){Assert.That(source.loop,Is.False);Assert.That(source.spatialBlend,Is.Zero);}
            yield return new WaitForSecondsRealtime(.85f);
            var preview=audioType.GetMethod("Preview");
            for(int i=0;i<3;i++)
            {
                candidate.SetValue(null,i);Assert.That(preview.Invoke(audio,null),Is.EqualTo(true));
                Assert.That(voices.Any(v=>v.isPlaying&&v.clip.name=="03_Aura"),Is.True,"Old candidate preferences must not change approved single audio");
            }
            preview.Invoke(audio,null);
            Assert.That(preview.Invoke(audio,null),Is.EqualTo(false),"Pool must reject a fifth overlapping sound");
            audio.enabled=false;Assert.That(voices.All(v=>!v.isPlaying),Is.True);audio.enabled=true;
            var audition=audioType.GetMethod("PreviewAudition");
            foreach(var role in new[]{"Multi","Player","Support"})for(int index=0;index<3;index++)
            {
                foreach(var voice in voices)voice.Stop();
                Assert.That(audition.Invoke(audio,new object[]{role,index}),Is.EqualTo(true),role+index);
                Assert.That(voices.Any(v=>v.isPlaying&&v.clip.name.StartsWith("0"+(index+1))),Is.True);
            }
            Assert.That(audition.Invoke(audio,new object[]{"Invalid",0}),Is.EqualTo(false));
            foreach(var voice in voices)voice.Stop();
            var canvas=game.transform.Find("CombatHUD");
            var buttonType=Type.GetType("UnityEngine.UI.Button, UnityEngine.UI");
            var sliderType=Type.GetType("UnityEngine.UI.Slider, UnityEngine.UI");
            var menu=canvas.Find("DeveloperMenu").GetComponent(buttonType);
            yield return new WaitForSecondsRealtime(.25f); // HUD phase refresh closes developer tools on phase changes.
            var click=buttonType.GetProperty("onClick").GetValue(menu);click.GetType().GetMethod("Invoke").Invoke(click,null);
            yield return new WaitForSecondsRealtime(.25f);
            var panel=canvas.Find("SoundControls");Assert.That(panel.gameObject.activeInHierarchy,Is.True);
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
