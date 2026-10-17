# 사용자 선택③ 적용

2026-10-18 (Asia/Seoul). 사용자가 비교판의③을 선택해 따뜻한 고대 설비 색보정을 저장했다. 복구 기준 `7e2e007`.

- 기존 `Assets/Settings/CoreGuardianColorGrading.asset`만 Unity 에디터 API로 수정했다. 대비26/채도7/노출-0.20EV, SplitToning 그림자RGB(.46,.49,.54)/밝은색RGB(.54,.515,.48)/balance5. 기존 ColorAdjustments+SplitToning 두 항목만 유지한다. 카메라HDRfalse/posttrue 유지, Bloom/톤매핑/비네트/블러 추가 없음.
- [실제 적용 화면](04_applied_warm.png)은 선택 시안 `03_warm_machinery.png`와 SHA256 `9D3AE7FC9E3AD59C73F3052B7686B59559255C76200DD3017993AE3B48EBC9B4`로 바이트 동일하다. 같은1920×1080 Edit 카메라이며 Overlay HUD/전투 몬스터는 포함되지 않는다. 저장값 재조회는 [applied_warm.json](applied_warm.json)을 따른다.
- 씬 SHA256 `665E5E58FEC304889E0331190972FA992849F459D74A0D1CC7C2737FACF8C69D` 유지. 맵/경로/입구/원본 아트/전투 수치/HUD/선택선/강화/입자/포탑 효과 변경 없음. 메모리 실행 diagnostics0, 종료Playfalse/씬1/dirtyfalse·현재콘솔오류0/경고0 확인.
- 새 Play/자동검사/사람 플레이/밸런스 측정/빌드/실행파일/push 없음. 적용 화면 확인이며 전투 중 HUD·숫자 판독/다수 전투/성능의 새 검증이 아니다. Notion은 수정하지 않았으므로 해당 문서의35/12/-0.45는 이전 값이다. 10월17일 실제 전투 비교 결과를 이번 설정의 검증으로 사용하지 않는다.

기존35/12/-0.45와 밝은 색조 중립 설정은 이전 값이다. 현재는 사용자 선택③이 우선한다. 미추적 오디오·밸런스/BGM·다른 작업과 font_tools_vendor 접근 오류 삭제 표시는 커밋 대상에서 제외하고 보존했다.
