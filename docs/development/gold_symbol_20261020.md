# 골드 획득 심볼 통일

2026-10-20 사용자 요청. 복구 기준533a257. 사용자 Play가 일시정지 상태여서 변경을 보류했고, 사용자가 직접 종료했다고 알린 뒤 실제 Playstopped/씬1dirtyfalse를 확인하고 적용했다.

`CoreGuardianCombatArt.OnDamage`의 처치 골드 표시가 상단 HUD와 동일한 `CoreGuardianHudSkin.Sprite("Gold")`를 사용하도록 참조 한 곳만 교체했다. 공통 리소스는 `Assets/Resources/CoreGuardian/Hud/Gold.png`다. 기존 CombatB/GoldGain 원본은 보존한다. 획득량·표시 숫자·크기·위치·움직임/페이드·사운드 및 전투 규칙은 변경하지 않았다.

Unity 재컴파일 completed/failedfalse, 후속 editorready/compilingfalse/Playstopped 확인. 해당 Gold 스프라이트 실제 로딩 성공/경로 확인과 소스 참조 일치를 검증했다. 컴파일 직후 첫 eval은 메인 스레드 timeout이었고, ready 상태에서 재조회 성공했다. 새 Play/자동검사/사람 플레이/밸런스 검증은 하지 않았다. 씬 YAML/빌드/실행파일/push 없음.
