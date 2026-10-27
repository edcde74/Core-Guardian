using CoreGuardian;
using UnityEngine;

// 개발 도구 테스트 버튼 (2026-10-27 사용자 요청). Shown only while the developer menu is open.
public sealed partial class CoreGuardianHud
{
    int cheatCard=1,cheatWave=1,cheatEnemy;
    void BuildCheatPanel()
    {
        const float W=190,H=48,Gap=8;
        var panel=Box("DevCheats",canvas,new Vector2(0,1),new Vector2(334,-72),new Vector2(16*2+W*3+Gap*2,16+6*(H+Gap)));
        Vector2 At(int col,int row)=>new Vector2(16+col*(W+Gap),-16-row*(H+Gap));
        Vector2 Size(int cols)=>new Vector2(W*cols+Gap*(cols-1),H);
        bool Combat()=>game.Session.Phase==SessionPhase.Combat;
        bool Prep()=>game.Session.Phase==SessionPhase.Preparation;
        void Done(){game.Notify(game.Session.LastMessage);Refresh();}
        Button("Cheat_Gold",panel,At(0,0),Size(1),()=>"골드 +10000",()=>{game.Session.DebugAddGold(10000);Done();});
        Button("Cheat_Crystal",panel,At(1,0),Size(1),()=>"수정 +10000",()=>{game.Session.DebugAddCrystals(10000);Done();});
        Button("Cheat_Core",panel,At(2,0),Size(1),()=>"코어 회복",()=>{game.Session.DebugRestoreCore();Done();});
        Button("Cheat_KillAll",panel,At(0,1),Size(1),()=>"적 전부 처치",()=>{game.Session.DebugKillAll();Done();},Combat);
        Button("Cheat_CompleteRound",panel,At(1,1),Size(1),()=>"라운드 즉시 완료",()=>{if(game.Session.DebugCompleteRound())Done();},()=>Combat()||Prep());
        Button("Cheat_Blast",panel,At(2,1),Size(1),()=>"범위 폭발 초기화",()=>{game.Session.DebugResetAreaBlast();Done();});
        Button("Cheat_OpenAugment",panel,At(0,2),Size(1),()=>"증강 화면 열기",()=>{if(game.Session.DebugOpenAugmentation())Done();},Prep);
        Button("Cheat_CardDown",panel,At(1,2),Size(1),()=>"증강 번호 -1",()=>{cheatCard=Wrap(cheatCard-Step(),game.Session.DebugAugmentCount);});
        Button("Cheat_CardUp",panel,At(2,2),Size(1),()=>"증강 번호 +1",()=>{cheatCard=Wrap(cheatCard+Step(),game.Session.DebugAugmentCount);});
        Button("Cheat_GrantCard",panel,At(0,3),Size(3),()=>cheatCard+"번 "+game.Session.DebugAugmentName(cheatCard)+" 증강 받기   (번호 버튼 Shift+클릭 = ±10)",()=>{if(game.Session.DebugGrantAugmentationWithMessage(cheatCard))Done();},()=>game.Session.Phase!=SessionPhase.Augmentation);
        Button("Cheat_WaveDown",panel,At(0,4),Size(1),()=>"라운드 -1",()=>{cheatWave=Mathf.Clamp(cheatWave-1,1,game.Session.LastWave);});
        Button("Cheat_WaveUp",panel,At(1,4),Size(1),()=>"라운드 +1",()=>{cheatWave=Mathf.Clamp(cheatWave+1,1,game.Session.LastWave);});
        Button("Cheat_SetWave",panel,At(2,4),Size(1),()=>cheatWave+"라운드로 이동",()=>{if(game.Session.DebugSetWave(cheatWave))Done();},Prep);
        Button("Cheat_EnemyNext",panel,At(0,5),Size(1),()=>"적: "+game.Session.DebugEnemyName(CheatEnemyId()),()=>{var ids=game.Session.DebugEnemyIds;cheatEnemy=(cheatEnemy+1)%Mathf.Max(1,ids.Length);});
        Button("Cheat_Spawn1",panel,At(1,5),Size(1),()=>"1마리 소환",()=>{game.Session.DebugSpawnEnemy(CheatEnemyId(),1);Done();},Combat);
        Button("Cheat_Spawn10",panel,At(2,5),Size(1),()=>"10마리 소환",()=>{game.Session.DebugSpawnEnemy(CheatEnemyId(),10);Done();},Combat);
        refresh.Add(()=>{if(!developerOpen)cheatWave=Mathf.Clamp(game.Session.Wave,1,game.Session.LastWave);});
        developerViews.Add(panel.gameObject);
    }
    int CheatEnemyId(){var ids=game.Session.DebugEnemyIds;return ids.Length==0?0:ids[Mathf.Clamp(cheatEnemy,0,ids.Length-1)];}
    static int Step()=>UnityEngine.InputSystem.Keyboard.current!=null&&UnityEngine.InputSystem.Keyboard.current.shiftKey.isPressed?10:1;
    static int Wrap(int value,int max){if(max<=0)return 1;return (value-1+max*10)%max+1;}
}
