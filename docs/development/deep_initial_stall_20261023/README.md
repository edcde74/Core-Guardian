# 초기 전투 Deep Profile — 첫 데미지 숫자의 TMP 지연 초기화 확인

2026-10-23 Asia/Seoul. 사용자 명시 승인: Deep Profile로 한 번 자세히 검사해 문제 위치 보고. 제품 수정·아트/HUD 변경·빌드·실행파일·push·커밋은 하지 않았다. 복구 기준 실제 HEAD는 종료 확인을 따른다. 기존 A/B 제한·SpriteAtlas 보류·원본 출력·네이티브 본체 배칭 OFF 유지.

## 실행과 복원

Unity MCP가 현재 도구 목록에 없어 공식 Unity CLI를 사용했다. 샌드박스에서 Editor 검색 파일 접근이 거부돼 승인된 권한으로 기존 Editor 연결을 읽었다. 새 Unity/도구 설치 없음. 중단된 initial_stall Probe는 결과 파일이 없었으며 실제 Editor stopped/검사 없음/씬 clean/Deep OFF 확인 후 시작했다.

[DeepProbe.cs](DeepProbe.cs)의 Prepare가 원래 상태를 [저장](original_state.json)하고 Deep ON·profileEditor ON·스크립트 재로딩을 요청했다. EditorLoop/Scene/Game 뷰 비용도 구분하기 위한 설정이다. 다음 Start에서 같은 부하를 구성했다: 적128(Attack112/Buff16),타워15,1920×1080/ortho8.5,VSync1/targetFrameRate-1,timeScale1/captureDelta1/60. 준비15프레임 뒤 **12프레임·1회**,30초 watchdog, Deep·일반 recording은 측정 창에서만 활성화. 할당 호출스택/FrameDebugger OFF, raw ToArray/CopyTo/ResolveMethodInfo/전체 트리 추출 없음. [환경](deep_benchmark_environment.json)/[관측12개](deep.json)/[완료](deep_benchmark_completion.json).

자동 Play 종료 후 원래 배경/시간/driver 복원, 분석 시도 뒤 Deep/profileEditor를 원래 OFF로 복구하고 재로딩했다. 최초 자동 분석은1백만 raw sample 안전 상한에 걸려 [건너뜀](hierarchy.json), raw 순회 없음. 수신 완료 후 확인된 Deep 프레임 샘플은 약173만~314만. [HierarchyOnly.cs](HierarchyOnly.cs)에서 Unity native 집계 Hierarchy로 한 프레임 상위120개만 읽었다: native build39ms+탐색41ms. [결과](hierarchy_frame3.json).

실시간 lastFrameIndex는 profiler 수신 지연으로 게임 frameCount와 바로 대응하지 않았다. 확정된 frameTime 메타데이터로 실제 긴 Profiler4/5를 선택하고 [FocusedHierarchy.cs](FocusedHierarchy.cs)에서 동일 이름을 병합한 게임 가지를 각180개까지 읽었다. 새 Play/캡처 아님. native build43/57ms·탐색89/105ms, [집계](focused_hierarchy.json). 거대한 raw tree 순회/재귀 self-time 계산을 하지 않았다. 원시 .data 전체 아카이브는 새로 쓰지 않았으며 보존 근거는12개 카운터 JSON·집계 호출 경로/비용·관련 소스 해시다.

## 확인된 초기 피격 지연 경로

`Session.MoveProjectiles → ApplyDamage → HUD.ShowDamageNumber → FloatingNumber → CreateFloatingNumber → GameObject.AddComponent<TextMeshProUGUI> → TextMeshProUGUI.Awake → TMP_Settings.instance → Resources.Load("TMP Settings")`

|Profiler4의 경로|Deep inclusive 시간|호출 수|해석|
|---|---:|---:|---|
|Session.StepTick|76.695ms|1|전체 전투 tick, 아래 항목 포함|
|Session.MoveProjectiles|69.314ms|1|이동 계산만의 비용 아님, 피해/표시 이벤트 포함|
|HUD.ShowDamageNumber|63.947ms|3|피해 적용 동기 이벤트의 데미지 표시|
|HUD.CreateFloatingNumber|62.959ms|3|풀이 비어 새 TMP 숫자 생성|
|TextMeshProUGUI.Awake|49.220ms|3|새 컴포넌트 초기화|
|TMP_Settings.instance|42.787ms|3|첫 설정 로드 포함|
|Resources.Load(TMP Settings)|42.573ms|1|설정 및 의존 리소스 최초 로드, 관리 할당130850B 관측|

이 시간들은 포함 관계이므로 합산 금지. **포탄 이동으로 보이던 비용의 상당 부분이 피해 이벤트 안에서 동기 실행하는 첫 데미지 숫자 생성/설정 로딩이었다.** 일반 UI.Text 피해 목록 확장과 이 TMP 피격 숫자는 서로 다른 경로다. 기존 GC 추적의 HUD.Update→UI.Text 생성 결과와 이번 Session→TMP 경로를 섞지 않는다.

소스 [HUD](../../../Assets/Scripts/Runtime/Presentation/UI/CoreGuardianHud.cs)의 FloatingNumber는 pool이 비면 CreateFloatingNumber를 호출한다. Configure는 damageFont/material을 미리 로드하지만 TMP Settings 로드나 숫자 컴포넌트 초기화/사전 생성은 하지 않는다. 생성은 실제 피해 순간 AddComponent부터 시작한다. 풀은 존재하며 재사용하지만 첫 사용/확장 비용을 피하지 못한 구조다.

설정 파일 Assets/Settings/Resources/TMP Settings.asset의 assetVersion은2이며 패키지 s_CurrentAssetVersion도2다. 설정 누락/버전 불일치라고 보고하지 않는다. 패키지 TMP_Settings.instance 구현457~463행이 첫 접근 때 Resources.Load를 호출함을 소스와 대조했다. [관련 소스 해시](source_hashes.json).

바로 다음 Profiler5에서는 CreateFloatingNumber 경로가 방문된 게임 가지에 없고 Session.StepTick은8.165ms였다. 동일 피격 수를 고정한 개선 전후 실험은 아니며, 첫 생성이 발생한 순간의 경로 차이를 보여준다.

## 지속 비용·Editor/Deep 영향

Profiler4/5에서 강조 준비56.875/58.832ms·SynchronizeViews24.732/25.637ms가 계속 관측됐다. 마스크 갱신·부품/좌표/타워 타깃 조회가 지속 비용 후보다. **Deep 계측 시간이므로 일반 상태의5ms/2ms와 같은 단위 조건으로 비교하거나 개선 예상값으로 쓰지 않는다.** 기존 recording OFF 결과와 함께 우선순위만 판단한다.

Profiler4/5 MainThread는301.107/304.910ms, Scene/Game 등 Editor 뷰 재그리기 구간은55.817/150.047ms, Profiler5 데이터 통합10.756ms도 포함됐다. 따라서 이 전체를 게임 로직 시간 또는 GPU 시간으로 부르면 안 된다. Deep과 profileEditor를 켠 효과가 크다. 순수 GPU 시간은 이번에도 미측정.

12개 wall FPS 평균4.47/최저3.03/최고5.74, 평균223.726ms 내외이며 **진단 부하 상태의 수치**다. 일반 게임 FPS나 이전100ms 스톨의 정확한 원래 기여율로 쓰지 않는다. GC.CollectionCount(0) 증가1회·GC.Collect marker 최대10.223ms·증분 marker 최대3.018ms도 이 Deep 관측 안의 결과다. 앞선 일반90프레임 수거0 결과와 조건이 달라 일반 전투의 GC 정지 원인으로 확대하지 않는다. [계산 원본](summary.json).

## 판정과 다음 수정 방향

확인한 것은 초기 피격에서 **TMP 숫자 객체의 지연 생성·TMP Settings 최초 동기 로딩**이 발생하며, 해당 전투 tick 안에서 큰 시간을 차지한다는 사실이다. 기존 일반 모드109~148ms 스톨 전체가 이 경로 하나 때문이라는 최종 인과/절감량 판정은 수정 후 같은 일반 조건 비교 전이다. 미확정4~6MB 급증의 출처도 이번 결과로 단정하지 않는다.

수정 제안은 준비/초기화 단계에서 TMP 설정·필요한 첫 숫자 객체의 초기화를 끝내 기존 풀에 반환해 두는 것이다. 숫자 표시 수/수명/외형/사운드를 줄이지 않고, 새 pooling 시스템을 중복 추가하지 않는다. 초기 비용을 숨기는 것이 아니라 전투 중 지연에서 준비 단계 비용으로 이동시키는 절충이므로 준비 시간/메모리도 함께 기록해야 한다. 제품 수정은 이번 보고에서 실행하지 않았다.

실제 최종 확인: Play stopped/compiling false/Deep·recording·profileEditor·callstacks OFF/driver true/background false/captureDeltaTime0/timeScale1/scene clean. [보호5파일](protected_after.json) 해시 모두 동일. 사용자 미커밋/미추적/설정 보존, 빌드/실행파일/push/커밋 없음.
