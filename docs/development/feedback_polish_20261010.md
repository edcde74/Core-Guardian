# 투사체 크기·처치 골드 정렬 수정 — 2026-10-10

## 사용자 피드백과 범위

사용자는 공격 투사체를 조금 더 크게 표시하고, 작은 처치 골드 심볼 바로 옆에 숫자가 나오도록 요청했다. 설치·강화 선택 체감은 아직 잘 모르겠다는 답변이므로 미확인 상태로 유지한다. 이번 작업은 표시 수정이며 전투 수치·재화 규칙·웨이브·유닛 크기 변경이 아니다.

최신 노션 [효과 아트·타이포그래피](https://www.notion.so/3f4389dbfc688118965ecb7a1ffaaf3c)를 읽고 기존 B 효과·E2 기술병 탄·Pixelify Sans Bold·즉시 획득 표시는 유지했다. 이번 사용자 크기·정렬 지시를 우선했다. 노션은 읽기만 했으며 로컬 구현·검증 기록을 갱신했다.

## 원인과 수정

기존 골드 심볼은 SpriteRenderer로 월드 위치 +0.93에 생성됐고 0.85초 동안 월드 높이 0.25만큼 떠올랐다. 숫자는 Canvas에 별도로 생성됐으며 월드 위치 +1.05, 가로 lane 42/세로 lane 12, 초당 Canvas 42 높이 이동을 적용했다. 좌표계·출발점·이동·페이드가 서로 달라 심볼과 숫자가 떨어져 보였다.

- 기술병 투사체 폭 0.16 → **0.20**, 포탑 두 종류 폭 0.22 → **0.275**. 모두 25% 증가. 이동·공격 판정은 기존 값 유지.
- 골드 심볼을 기존 HUD Canvas의 `GoldFeedback` 그룹 안에 숫자와 함께 표시한다. 승인 GoldGain 스프라이트 재사용, 기준 Canvas 크기 36×36, 클릭 차단 없음.
- `GoldNumber`를 왼쪽 정렬하고 심볼 오른쪽과 4 Canvas 단위 간격으로 붙였다. TMP 선호 너비를 사용해 `+123` 등 자리 수가 늘어도 함께 정렬된다.
- 그룹 전체가 같은 위치에서 초당 Canvas 42 높이로 떠오르고 같은 alpha로 사라진다. 수명 0.85초 유지. 처치 피해 숫자보다 위에 분리해 표시한다.
- 만료·재설정 시 부모 그룹을 삭제해 심볼과 숫자를 함께 정리한다. 기존 피해·코어 숫자는 각각 기존 위치·색·크기로 표시한다.

변경: `CoreGuardianPrototype.cs`, `CoreGuardianCombatArt.cs`, `CoreGuardianHud.cs`. 씬 YAML·메타·원본 프리팹 수정 없음.

## 검증

- 수정 전 실제 역할 검증의 처치에서 `VerifyGoldFeedback.CheckGroup`이 `Gold symbol is not grouped in the HUD with its number`로 실패했다. 수정 후 같은 발생 경로에서 통과했다.
- 실제 세 발사 주체의 투사체 스프라이트 EngineerChip/SingleBolt/MultiBolt와 적용 폭 0.20/0.275/0.275 확인.
- 실제 적 처치 표시 `+3`, 동일 부모 GoldFeedback, 심볼36·간격4, 승인 스프라이트·폰트, raycastTarget=false, 중복 월드 골드 심볼 없음 확인.
- 경과0.6초 조건에서 그룹 상승25.2, 심볼·숫자 alpha0.5 일치 확인. 수명 경과 후 양쪽 모두 삭제 확인.
- 별도 `+123` 표시에서 TMP 선호 너비71.43, 적용 너비72, 간격4 확인. 실제 골드 잔액을123으로 변경한 검사가 아니다.
- 역할 재설정 시 GoldFeedback/GoldGain/GoldNumber가 모두 정리됨 확인.
- 기존 EditMode **28/28**, PlayMode **6/6** 통과. 결과는 아래 JSON에 저장했다.
- 종료 후 BuildCombatPrototype.Verify 내부 success=true, A 길·참조·적/포탑3종·초기 플레이어(15,10)·saved=true. 에디터 ready, Play stopped, compiling=false, domainReloadInProgress=false. 마지막 컴파일 실패 플래그false 및 현재 Unity 콘솔 오류0 확인. 임시 시간 정지는 검증 종료 시1로 복구했다.

실제 씬의 역할 검증 모드에서 공격·처치를 주입한 화면 검토다. 자연 전투나 인간 플레이 난이도 검증으로 해석하지 않는다. 적용은 완료했지만 새 크기의 사용자 체감 확인은 남아 있다. 선택 체감·최종 밸런스는 미확인이다.

## 기록

- [전용 런타임 검사](VerifyGoldFeedback.cs): 기존 VerifyCombatArt.Begin → CheckProjectilesAndHit 후 CheckGroup·CheckProjectileSizes. 별도 CheckMotionAndExpiry → CheckExpired, Diagnostic 이후 CheckReset. 정지 검증 종료 시 반드시 Time.timeScale=1 복구.
- [EditMode 결과](feedback_polish_editmode_20261010.json)
- [PlayMode 결과](feedback_polish_playmode_20261010.json)
- [수정 전 화면](resource_cleanup_20261023/imported_evidence/development/gold_feedback_before_20261010.png)
- [수정 후 화면](resource_cleanup_20261023/imported_evidence/development/gold_feedback_after_20261010.png)

다음 확인: 사용자 실제 플레이에서 투사체 크기와 골드 묶음 판독 확인. 설치·강화 선택 체감은 별도 플레이테스트에서 계속 기록한다.
