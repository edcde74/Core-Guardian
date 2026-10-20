# 1차 최적화 — 본체 참조와 RT 재사용

적용/계측 2026-10-20, 정리/최종 상태 재확인 2026-10-21 Asia/Seoul. 복구 기준 HEAD `a135d9d`. 사용자가 4항목 해결방안 보고 후 “첫 번째거부터 진행”을 승인했다. 이 기록은 첫 항목의 구현·전후 계측을 담당한다. 품질/측정 계약은 [공통 규칙](../OPTIMIZATION_RULES.md), 조사 근거/대안은 [해결방안 보고](../optimization_solutions_research_20261020.md)를 따른다.

## 적용 범위와 결과
- `CoreGuardianSelectionOutline`: RT의 할당 용량과 사용 픽셀 영역을 분리했다. 각 축은 부족할 때만 커지고 작은 자세로 돌아와도 축소하지 않는다. 전체 clear → 기존 크기의 viewport → 사용 영역 UV로 출력한다. ARGB32/Linear/Point/Clamp·기존 crop·픽셀 두께·접촉 밴드·shader는 유지한다. 소실된 RT는 IsCreated로 복구한다.
- 비활성 방향 레이어까지 SpriteRenderer 참조를 보관한다. 매 Present에서 enabled/active/sprite/alpha를 다시 판단하고, 자식 수/부모 변화·파괴를 검사해 구조 변경 때 다시 조회한다. 이동 Transform.hasChanged는 무효화 기준으로 사용하지 않는다. **기존 Transform에 컴포넌트만 추가하는 호출자는 InvalidateSources를 호출해야 한다.** 현재 rig는 최초 Present 전에 생성되며 이 계약과 런타임 자식 생성/재부모/파괴를 검사했다.
- quad 정점/UV 배열을 재사용하고, 최상단 정렬은 기존 layer value→sortingOrder→동률 Last 의미를 보존하는 순회로 바꿨다. SpriteMesh의 기존 캐시는 유지한다.
- `PhotoShadow`, `ObjectEmphasis`의 참조 조회도 같은 캐시를 각 소비자별로 사용한다. 본체 실루엣 RT 자체는 여전히 각자 합성한다. 공통 마스크/아틀라스/명령 통합, HUD/오브젝트 풀링, Session 변경은 미적용이다.
- 전투 수치·CSV·아트/애니메이션·shader·효과 개수/수명·사운드·ProjectSettings·Packages는 이번 최적화에서 변경하지 않았다. 변경은 제품 소스3개와 관련 기존 테스트1개다. [구현 diff](implementation.patch), [전](before_hashes.json)/[후](after_hashes.json) 해시를 보존한다.

**판정: Editor 주입 스트레스에서 부분 개선 확인.** 최종 적128/포탑15 각3회에서 그림자 LateUpdate 15.355→11.339ms, 강조 13.519→10.225ms, GC.Alloc 중앙 517,978→217,966B/frame. RT 교체 총횟수/관측프레임은 221.047→1.420회/frame이다. 묶음 평균 FPS는 9.486→25.302, 최저3.924→9.987, 최고24.132→29.932다. [회차별 FPS·절감량/율·메모리·한계](RESULTS.md), [분포 원시 집계](comparison.json), [분석](analyze.py)/[표 생성](summarize.py).

## 최종 비교 조건
- Unity6000.6.2f1 / CLI1.0.0-beta.13(당시 최신 확인) / URP17.6.0 / Ryzen5 5600X / RTX3070 / RAM32681MB / Windows11. 실제 전투씬,1920×1080,VSync1,targetFrameRate-1,배속1,captureDelta0. 각 단계 environment.json에 저장했다.
- 실제 Game/Session에서 적128(매8번째 Buff, 나머지 Attack), HP100000·CoreDamage0·Gold0 주입, 경로 거리20+(i%12)*0.3. 기본 병종 speed/defense/buff 값 사용. 진단 재화로32기 설치 시도 중 **실제15기**(Multi/Support)를 동일 건설 가능 칸 순서로 설치했다. 공격·애니메이션·강조·접촉·HUD·사운드는 활성 상태다.
- 최초 보조 `before/after`는 이전 ProfilerProbe의32/128 각3회다. 진단 초기 자동 생성 적이 섞여32~33/128~129명이 된 구간이 있어 최종 수치로 사용하지 않는다. 프레임 지연/실패도 삭제하지 않았다. 특히 after/normal_3의601ms 프레임은 Main Thread EditorLoop551.53ms/PlayerLoop48.41ms로 확인됐다. 평균 향상과 달리 보조 실험 최저FPS가 악화한 사실도 원본에 남긴다.
- 이후 `before_controlled/after_controlled`에서 진단 전투 시작 직후 Session의 비공개 waveTime을.001로 주입해 자동 첫 적 생성을 건너뛰었다. **제품 Session 코드는 변경하지 않았다.** 관측 전 구간 실제 적128/포탑15, 관측 mask274를 확인했다. 워밍업30 + 관측100 엔진프레임 ×3회/단계. 사전에 정한 길이/반복은 동일하다.
- 원본3파일을 HEAD에서 보관한 전 버전과 새 버전으로 바꿔 같은 에디터/도구로 각각 수집했다. [SwapSources.py](SwapSources.py)는 대상3파일 전체가 저장한 예상 바이트와 일치할 때만 교체하며 외부 수정이 있으면 중지한다. 최종 소스는 후 버전으로 복원/대조했다. 사용자 변경을 git restore/reset으로 되돌리지 않았다.
- 자유 실행의 실제 unscaled dt이며 결정론적 고정 dt/입력 재생은 아니다. 측정시간이 달라 자세·위치·투사체 수가 동일 위상이 아니고, 느린 전 버전은 Session의 최대.05초 하위 Tick도 더 많이 수행한다. 따라서 Session 자체 개선/순수 GPU 비용/동일 전투 결말 비교로 확대하지 않는다. 같은 자세 출력은 아래 별도 원본 코드 대조가 담당한다.
- Profiler CPU/Rendering/Memory/UI/Audio, profileEditor=false, Deep=false, allocation stacks=false. 실제 `ProfilerDriver.SaveProfile` 및 Main/Render Thread RawFrameDataView 분석이다. FPS용 연속 엔진 delta는 각100개, 무효/누락0. 유효 Main PlayerLoop는 각98/99/99개로 수신 경계가 달라 표본 수를 따로 기록했다. RT 교체는 워밍업 후274개 소유자의 실제 mask 참조 변경을 매 관측프레임 추적했다. 조회/추적 오버헤드는 양쪽 동일하다.
- 도구/Editor 오버헤드를 포함한 진단이며 정상30R 밸런스/사람 체감/standalone FPS가 아니다. 목표 PC/FPS 예산 달성으로 표시하지 않는다. 새 Deep/Frame Debugger는 기존 조사에서 원인을 확보했으므로 이번 반복에 겹치지 않았다. 유효 GPU 시간·Memory Profiler snapshot은 미측정, Profile Analyzer/Memory Profiler 패키지 미설치 상태를 유지했다.

## 품질·기능 검사
- [용량 계약](Contract.cs): 변경 전 작은 크기에서 RT 유지=false/양축 최대 보존=false → 후 true/true. 최초 진단 스크립트 GetInstanceID가 현재 엔진에서 obsolete error였고 ReferenceEquals로 수정한 뒤 유효 결과를 저장했다. [전](contract_before.json), [후](contract_after.json). 제품 컴파일 오류가 아니다.
- EditMode 전체 **176/176**, 관련 GPU/캐시3건 포함. [결과](edit_tests.json). 겹친 스프라이트의 정확한 union/ring, 작은 자세 복귀 후 픽셀 동일, 접촉/강화 shader 출력, 비활성 방향·알파·sprite·동적 자식/부모/파괴·명시 무효화를 검사했다.
- 선택/접기/설치/플레이어·포탑 검사 PlayMode **4/4**를 별도 완료했다. [결과](play_selection.json).
- [원본 대조 코드](PixelComparison.cs)는 HEAD 원본 클래스를 임시 CGReferenceOutline로 메모리 컴파일하고 승인된 실제 프리팹8종(병사3/보스/플레이어/포탑3)을 사용했다. 4방향×3위상(피격/발사 포함)×1/2px×outline/contact/scan=**576건**, 가시성/출력 픽셀 불일치0. 후 버전 RT를256×256으로 넉넉히 만들어 UV 부분 사용을 강제로 검사했다. [개별 결과](pixel_comparison.json), [출력 예시](quality_optimized.png). outline192/192·scan192/192는 실제 비어 있지 않은 출력이며 contact는164/192, 나머지28건은 양쪽 모두 빈 결과다. 검사 카메라256×256·고립 출력, contact Band16 조건이며 실제 전체 게임 화면/모든 밴드/해상도 검사를 대신하지 않는다.
- 실제 전투씬에서128적/15포탑을 재주입하고 프레임 진행(1639→1642)을 엔진에서 확인, ScreenSpaceOverlay HUD 포함 [1920×1080 화면](scene_after.png)을 저장/직접 확인했다. 초기 스트레스 한 화면으로 정상30R/사람 직접 플레이/청취 승인으로 확대하지 않는다. 캡처 도구가 Assets/docs 아래에 저장한 자체 파일은 문서 폴더로 옮기고 자체 새 폴더/meta만 에디터 API로 정리했다. 기존 Assets/docs 자료는 유지했다.
- **추가 보행 PlayMode 검증은 미완료다.** 목록에는6개가 존재하나 필터/테스트 재검색/공식 CLI 및 어셈블리 필터가0건으로 반환했다. [0건 원본](play_animation.json)은 통과로 세지 않는다. 검사 도구 내부 원인은 미확정이고 제품 코드로 임의 수정하지 않았다. 이전 사용량 제한으로 상태 조회 자동 승인 검토가 실행되지 않은 시도가 있었으나 2026-10-21 재개 후 연결/상태 재확인은 정상 수행했다.

## 메모리와 남은 한계
RT를 축소하지 않아 각 개체의 과거 최대 용량을 개체 파괴까지 보유한다. 해상도/카메라 확대 후 감소·장기 세션에서는 메모리 절충이 있다. 현재 RT만 합한 종료 시점 추정은 전18.32~18.34MB/후12.83MB지만 서로 다른 위치/자세에서의 값이므로 메모리 절감 성과가 아니다. 동일하게 작아진 자세는 기존 정확 크기 방식보다 새 용량이 더 클 수 있다. GPU VRAM/최대/장기 잔류를 검증했다고 하지 않는다. RT 재생성 감소를 모든 합성·Draw Call 제거로 해석하지 않는다.

다음은 같은 본체 마스크 공유의 별도 범위 결정이다. 장기 메모리·카메라/해상도 변경·접촉 밴드/앞뒤 중첩과 미완료 보행 테스트를 보강할 수 있다. 이번 승인으로 다른 항목이나 렌더 품질/효과 제한을 적용하지 않는다.

## 원본·종료 상태
- [원본 압축](profiler_raw.zip):18개 .data +18개 markers JSON, 약514MB. [압축/검증 스크립트](archive.py)와 [SHA256 manifest](archive_manifest.json)로36개 항목의 복호화 해시를 대조했다. 미압축 원본도 로컬에 남기고 폴더 .gitignore로 중복 추적만 제외했다. frames/environment/progress/completion/resources JSON은 단계 폴더에 직접 남긴다.
- [상태 복원](restored.json): 자체 Play종료/paused=false, 씬1 dirty=false, 컴파일false, TimeScale1/captureDelta0/backgroundfalse, Profiler/Deep/alloc/FrameDebugger=false. 저장음량0.195603669·Edit AudioListener1. 최종 상태는 다음 Unity 작업 때 다시 확인한다.
- 기존 사용자 ProjectSettings2개/미추적/폰트 도구 접근오류 표시는 보존한다. 빌드·실행파일 실행·패키지 설치·GitHub push는 하지 않았다. 노션 계획은 재조회했으나 그 페이지의 과거 ‘시작 전’ 상태는 이번 로컬 구현보다 이전 기록이며 수정하지 않았다.
