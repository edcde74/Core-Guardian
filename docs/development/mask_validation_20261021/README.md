# RT 재사용 잔여 검증

2026-10-21 Asia/Seoul. 시작 HEAD `db83801`. 사용자가 보행 실행 문제·RT 메모리·카메라/해상도·중첩 자동 검증을 승인했다. 후속 마스크 공유/아틀라스/HUD/Session 구현, 빌드/실행파일/push는 범위 밖이다. 제품 소스·셰이더·원본/CSV·씬·패키지는 변경하지 않았다. 기존 PlayMode 테스트의 낡은 준비 조건만 수정했다.

## 보행 PlayMode
- 현재 공식 MCP 실행기 내부 `CollectTestNames`를 같은 TestRunnerApi 트리에 적용하면6건을 선택한다. [진단](filter_diagnostic.json), [트리](test_tree.json). 클래스/메서드 모두 Runnable·Explicit=false. 과거0건은 이번에 재현되지 않아 원인을 확정하지 않는다. 패키지나 새 테스트 실행기를 만들지 않았다.
- `run_tests`: mode=playmode, filter_type=testName, filter=CoreGuardianAnimationPlayModeTests, async_tests=true, timeout180, 기대6건. 최초 [4통과/2실패](play_animation.json), 준비 조건 수정 후 [5통과/1실패](play_animation_fixture_update.json), 최종 [6통과/0실패](play_animation_final.json).
- 고정 경로 거리77은 현재 플레이어 사거리 안을 보장하지 않았다. 실제 씬/경로는 유지하고 시작점의 고HP 적 옆으로 플레이어를 배치해 실제 발사/원본 아트/발사 rig를 검사한다. 발사 후 Idle 검사에는 이동 정지를 명시해 정지 블렌드를 진행한다. 기존 웨이포인트 보행4방향 검사는 유지한다.
- 포탑 검사는 승인된 PhotoShadow.Attach가 교체 대상의 옛 그림자를 숨긴 뒤, 발사 동안 그 설치 상태를 유지하는지 검사하도록 수정했다. 새 사진 그림자 활성과 Multi/Support의 교체 레이어 비활성도 명시적으로 검사한다. 승인 근거는 [사진 그림자 정본](../photo_shadow_applied_20261018/README.md).

## 출력 픽셀 비교
[기존 원본 대조](../mask_reuse_20261020/PixelComparison.cs)의 CGReferenceOutline을 그대로 메모리 컴파일했다. [확장 코드](ExtendedValidation.cs)는 임시 PreviewScene에서8종×해상도3(256×256/384×216/216×384)×줌3(1.7→.85→3.4)×배치3(단독/2개겹침/가장자리)×방향4×출력4(선택선/접촉/강화/동시)를 비교한다. 1/2px 두께와 이동·피격·발사 위상을 포함한다. 현재 RT512×512를 먼저 할당해 부분UV 사용을 강제한다. 게임 GameView 해상도나 사용자 카메라 설정 변경을 대신하지 않는다.

**효과 출력만의 비교3,456건: 차이0.** 양쪽 모두 빈85건은 별도로 기록했고, 비어 있지 않은 출력은3,371건이다. 각 역할의 `*_effects_pixels.json`이 원본이다. 원본과 현재의 각 개체 마스크를 독립적으로 합성/정렬하며, 화면 전체 합집합으로 바꾸지 않았다.

본체/사진 cast까지 합친 최초 비교는7종 각432건 차이0, 보스는9건 차이가 났다. [초기 보스](Boss_pixels_initial.json), [원본끼리 대조군](Boss_control_pixels.json), [정렬 순서를 분리한 원본끼리 대조군](Boss_control_sorted_pixels.json). 원본끼리도 같은9조건에서 차이가 생겼고, [동일 크기 RT 대조](Boss_exact_pixels.json)에서도9건이다. [마스크/geometry 조사](Boss_first_geometry.json)는6개 소비자의 원시 마스크 차이0·동일 정점/정렬을 확인했다. 따라서 **전체 본체 합성 비교는 미확정**이며 최적화 회귀/동등성 통과 어느 쪽으로도 단정하지 않는다. 투명 본체의 렌더 순서/배칭 영향은 의심되나 원인을 확정하지 않았다. 게임 코드를 임의 수정하지 않았다.

초기 프리뷰 카메라 씬 연결 누락으로 [432건 모두 빈 무효 결과](Attack_pixels_invalid_preview.json)가 발생했다. camera.scene/scene culling mask를 연결한 뒤 유효 출력으로 재검사했다. PreviewScene의 non-ExecuteAlways 동작은 정리 콜백이 자동 실행되지 않아 첫 실행에서 자체 RT12개가 남았다. 이후 명시 정리를 적용해 각 실행 전후45개 유지, 최초 실패의 고유 크기/개수(512²×6·59×128×3·93×90×3)를 [재현](Attack_owned_sizes.json)하고 live consumer 없음 확인 후 [자체12개만 정리](diagnostic_cleanup.json)했다. 마지막33개는 최초 유효 비교 전 기준33개와 같다. 기존 리소스 전체 정리/전역 unload는 하지 않았다.

## 실제 엔진 메모리/재시작
[진단 코드](MemoryLifecycle.cs), [환경](memory_environment.json), [원본](memory_lifecycle.json). Unity6000.6.2f1 Editor·1920×1080·Ryzen5600X/RTX3070. 기존 controlled 조건의 실제 씬에서 적128/실제 포탑15, 적 HP100000/CoreDamage0/Gold0 주입·배속1. 매회30프레임 워밍업+600프레임 관측+초기화 후90프레임,3회. 프레임200에8.5→4.25 줌인,400에8.5 복귀. 엔진 frameCount1196→1198로 실제 진행을 확인했다. 정상30R 밸런스·사람 플레이가 아니다.

|회차|줌인 전 RT bytes|관측 피크 bytes|초기화 후 소유 RT/bytes|관측 평균/최저/최고 FPS|
|---|---:|---:|---|---|
|0|12,832,052|59,639,476|3 / 305,992|20.276 / 1.679 / 28.058|
|1|13,048,528|58,764,360|3 / 305,992|22.558 / 5.078 / 28.075|
|2|13,052,792|58,440,960|3 / 305,992|22.091 / 3.829 / 28.645|

600프레임 관측까지 각31.30/28.14/28.90초(워밍업 포함)가 걸렸다. 원래 배율로 돌아와도 각 소유자는 과거 최대 용량을 보유한다. 초기화 후 전 회차 적0/포탑0, 소유 mask274→3, global319→48(소유 외45개 포함)로 돌아왔다. 이 짧은 반복에서 재시작 잔류 증가는 관측하지 못했지만 수십분/정상30R·GPU VRAM·Memory Profiler snapshot 검증은 아니다. Bytes는 Profiler.GetRuntimeMemorySizeLong의 소유RT 합계다. FPS는 엔진 delta 관측값이며 비교 전 버전이 없어 개선량/율은 N/A다. dt 전건을 계산 후 저장하지 않아 독립 재집계에 한계가 있다. 30프레임 간격의 리소스 표본은 순간 피크/전체 RT 교체 수를 보장하지 않는다.

Memory Profiler/Profile Analyzer 패키지는 미설치 상태를 유지했다. 초기3개 `.data`는 SaveProfile=true여도 first/last=-1·4byte라 무효다. ProfilerDriver와 Profiler.enabled 양쪽을 활성화한 [유효 캡처](valid_capture.json)는 frame0~328·381,000,092byte다. 복구 캡처는 호출 사이 진행을 포함한329프레임으로, 예정된30프레임 고정 길이나 전후 성능 비교가 아니다. CPU/Memory/Rendering 모듈을 켰지만 새 CPU 계층 분석/메모리 스냅샷 분석 완료를 뜻하지 않는다. 무효3개도 [원본 압축](profiler_raw.zip)에 남겼고 [SHA256 manifest](archive_manifest.json)로4개 항목의 복호화 해시를 대조했다. 미압축 파일은 로컬에 유지하되 중복 추적만 제외한다. [압축 스크립트](archive_profiler.ps1).

## 복원과 다음
[엔진 종료 상태](restored_engine.json): 자체 Play 종료·scene1/dirty=false·compile/update=false·Profiler/Deep/FrameDebugger=false, camera8.5, timeScale1/captureDelta0/backgroundfalse, 음량/저장음량은 시작값이다. Overlay 포함 [스트레스 화면](runtime_stress.png)을 문서 폴더로 옮기고 카메라 도구가 Assets 안에 만든 자체 빈 폴더/meta만 AssetDatabase로 삭제했다.

Inference의 기존 SENTIS 자동 추가가 재발해 추가 실행 전 AnalyticsDefineManager 구현과 차분을 대조했다. 종료 후 ProjectSettings의 해당 문자열만 시작값으로 복원했으며 [설정3파일 SHA256](restored_settings_hashes.json)이 시작값과 일치한다. runtime define에는 여전히 SENTIS가 포함될 수 있다. 프로젝트 Analytics=false를 확인했고 패키지/전역 설정은 바꾸지 않았다. 마지막 디스크 복원 뒤 추가 Play/재컴파일은 실행하지 않았다.

다음은 보스 본체 포함 비교의 판정 방법을 정리하고, 실제 게임 화면 중첩과 장시간/정상30R 검증을 보강하는 것이다. 마스크 공유 등 새 최적화는 미적용이다. 성능 예산은 [공통 규칙](../OPTIMIZATION_RULES.md)에 따라 미확정 상태다.
