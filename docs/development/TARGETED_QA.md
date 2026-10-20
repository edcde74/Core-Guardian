# Unity 대상별 QA

이 문서는 테스트 선택/실행 계약이다. 엔진 제어·명령 사용법은 공식 Unity CLI 스킬과 `references/playmode-verification-loop.md`를 재사용한다. 새 테스트 실행기·새 스킬을 만들지 않는다.

## 실행 전
1. 사용자 요청과 변경 파일에서 검증 질문을 정한다. 기존 결과는 해당 커밋·조건·도구 설정이 일치할 때만 재사용한다. 대상 테스트 파일/현재 명령 스키마를 확인하고 계획한 필터·기대 테스트 수를 기록한다. 아래 목록은 고정 실행 수가 아니다.
2. 연결/프로젝트/열린 씬/dirty/Play/컴파일·다른 검사·Profiler 상태를 조회한다. 사용 중이면 중단하거나 테스트를 겹치지 않고 독립 준비만 한다. 프로젝트 범위에 필요한 설정만 읽는다.
3. 자신이 변경하거나 테스트가 건드릴 수 있는 상태를 저장한다: 시간/배속/일시정지/카메라/입력/백그라운드/Profiler·음량/PlayerPrefs·ProjectSettings/define 차이. 처음부터 있던 변경은 자신의 복원 대상으로 처리하지 않는다. 광범위한 설정 저장·복원은 피한다.

## 선택 색인
경로는 `Assets/Game/Tests/{EditMode,PlayMode}/` 아래 기존 클래스다. 실제 변경과 관계있는 클래스/메서드만 고른다. 추가 회귀 범위는 변경 영향이나 실패로 정당화한다.

|변경|EditMode 후보|PlayMode 후보/보조 확인|
|---|---|---|
|마스크/강조/그림자|CoreGuardianSelectionOutlineTests, CoreGuardianObjectEmphasisTests, CoreGuardianPhotoShadowTests|CoreGuardianSelectionOutlinePlayModeTests; 같은 위상 픽셀 대조|
|보행/방향/피격|CoreGuardianOriginalGaitTests, CoreGuardianPlayerFacingTests, CoreGuardianHitFlashTests|CoreGuardianAnimationPlayModeTests, CoreGuardianPlayerFacingPlayModeTests, CoreGuardianHitFlashPlayModeTests; 실제 경로/정상 키 입력|
|HUD/설치/접기|변경한 표시/계산의 기존 테스트|CoreGuardianHudInteractionTests, CoreGuardianPlacementHudPlayModeTests, CoreGuardianSelectionOutlinePlayModeTests|
|포탑 원본/발광|CoreGuardianTowerGlowTests, CoreGuardianSingleTowerAimTests, CoreGuardianTowerGroundingTests|CoreGuardianTowerGlowPlayModeTests, CoreGuardianSingleTowerAimPlayModeTests|
|사운드|CoreGuardianSoundTests, CoreGuardianAudioRoutingTests|CoreGuardianCombatAudioPlayModeTests, CoreGuardianSelectedAudioPlayModeTests; 청취는 별도|
|전투/라운드/성장|CoreGuardianSessionTests, CoreGuardianThirtyRoundTests, CoreGuardianAugmentationTests|해당 시나리오의 기존 런타임 테스트; 밸런스는 별도 대응 시드/정책|
|문서만 변경|링크/규칙/범위/해시 비교|Play/빌드 불필요|

## 결과·중단·복원
- 요청 접수/Play 진입은 성공 판정이 아니다. 비동기 작업은 완료 상태·실제 테스트 수·실패를 확인한다. **0건은 미검증**이다. 엔진 프레임 진행·실제 렌더/Overlay·사람 조작·청취·기능 QA·성능을 구분한다.
- 0건/명령 오류는 필터·현재 목록/스키마·명시 테스트·진행 중 상태를 조사한다. 원인을 바꾸지 않은 같은 재시도는 하지 않는다. 근거 있는 수정 후 재시도1회; 여전히 무효면 미검증으로 기록하고 독립 검증을 진행하거나 구체적인 차단 원인을 보고한다. 실행 실패를 숨기거나 검증 필요성을 생략하는 예산 제한은 아니다.
- Profiler는 정상 전후 캡처와 Deep/Frame Debugger 등 진단 부하를 분리한다. 조건·표본 수·원본·최소/최대/평균 FPS·절감량/율·실패/악화·품질은 [최적화 규칙](OPTIMIZATION_RULES.md)을 따른다. 이미 충분한 검증은 새 변경/실패/불확실성이 없으면 반복하지 않는다.
- 자체 작업만 복원하고 상태/디스크 diff를 다시 확인한다. 설정 재저장/분석 심볼 자동 추가가 보이면 다음 실행을 멈추고 원인과 원래 차이를 비교한다. 이를 이유로 전역 설정/패키지를 임의 변경하지 않는다. [최근 설정 한계](mask_reuse_20261020/README.md#최종-설정-재대조2026-10-21)를 참고한다.
- 원본·요약·실패·미완료·복원 결과를 날짜별 기록에 보존한다. 빌드와 실행파일 실행은 별도 명시 요청이 있을 때만 한다.
