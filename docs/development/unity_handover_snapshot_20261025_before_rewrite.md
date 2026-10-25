# 코어 가디언 인수인계

갱신: 2026-10-24 Asia/Seoul. 현재 사용자 지시·최신 담당 Notion 우선. [WORK_ROUTING.md](WORK_ROUTING.md)에서 필요한 분야만 읽는다.

## 현재 재개 — 마스크 호출 경로 실행 종료 / 후보 미채택·제품 원본 복구

- 2026-10-24 Notion 담당3페이지 최신화·재조회확인 완료: [최적화 계획](https://www.notion.so/3ff389dbfc6881779abdeb5eeeb1cda4), [변경 이력](https://www.notion.so/3df389dbfc6881fc9f1bd004adfc5999), [후속 작업 순서](https://www.notion.so/3f4389dbfc688171b84dc19b138925f4). 과거본문/링크그대로보존. [반영 확인](mask_submission_path_20261024/notion_update_20261024.json). 이번문서작업에서Unity조회/조작없음.
- 사용자 `함 해봐`와 응답지연 뒤 `한 줄 복구 후 기존 범위 계속` 승인 범위를 실행했다. 새 원본/카메라입력재사용 후보 각warm15/sample90×3, 후보52테스트/512조합차이0. 합산CPU 절감0.367ms로1ms목표미달·미채택. 완료마스크 프레임간재사용은 진단gate미달로 구현/측정 생략. [수치·원인·검증한계·후보·승인/실패 정본](mask_submission_path_20261024/README.md).
- 제품4파일 시작working-tree원본hash일치·helper/새테스트Assets밖격리·복구컴파일성공·보호10파일동일. CLI최종조회:씬clean7roots/Play·compile·ProfilerOFF/nativeBatchOFF. **Profiler버퍼는시작0..11→최종−1/−1차이,원인/시점미확정·임의재로드없음.** 독립코드검토는사용량제한으로미완료. 빌드/실행파일/커밋/push없음.
- 다음은잔여Flush의세부원인분리다. 남은시간전체를카메라getter나드라이버대기로단정하지않는다. normalProfiler1회/상위2프레임진단과새제품수정은별도승인전미실행. 이전보류정책/사용자기존변경보존.

## 직전 실행 — B1/B2 독립 검증 완료 / 둘 다 미채택·제품 원본 복구

- 사용자 `B1/B2 진행` 승인 범위 완료. 각후보512조합차이0·성능warm15/sample90×3, B1대상45/B2대상54테스트통과. 두단계모두큰비용목표미달로미채택. B2출력부분개선과전체Mask반복변동을구분했다. 수치·실패원인·검증한계·후보소스·새검토는 [B1/B2 결과 정본](emphasis_mask_cpu_20261024/B1_B2_RESULTS.md).
- 제품2파일시작working-tree원본hash동일·새테스트Assets밖격리·복구컴파일성공·보호10동일. 씬clean7roots/Play·compile·ProfilerOFF/nativeBatchOFF·기존Profiler버퍼0..11유지. 빌드/실행파일/커밋/push없음, 기존변경/보류정책보존.
- 다음제안은같은Present내기존Request의카메라정보재사용으로QueueInputs반복native읽기축소다. 원인과남은구간은정본참조. **다음제품수정/추가측정미승인**, B1+B2합산효과미측정이며자동활성화하지않는다.

## 직전 실행 — CPU 마스크 준비 A 중단 / 미채택·제품 원본 복구

- 최신 승인 실행: 강조/마스크를 우선 처리하는 [실측·원인 정본](emphasis_mask_cpu_20261024/README.md). 수정 격리512쌍 모두차이0,후보warm15/sample90×3완료. Mask4.877→4.545ms(0.333/6.82%),Emphasis5.924→5.550ms(0.374/6.31%)로큰비용목표미달·미채택. Capture구간증가약0.424ms가절감일부상쇄,추가sample배열payload153.56KiB확인. 일반Profiler90행1회로Capture/Queue/Geometry/Binding등분해. nativeview하나−1alias와counter90/native88경계한계를정본에기록했고추가반복없음.
- 제품3파일/보호10hash동일복구·helper격리·컴파일성공·씬clean7roots/Play/ProfilerOFF·기존Profiler버퍼0..11복원확인. 다음은 [기존cache입력재사용과MPB재구성축소](emphasis_mask_cpu_20261024/NEXT_PROPOSAL.md)이며직전승인은수정안구체화까지다. 새B1/B2구현·측정은승인전미실행. 기존보류정책·사용자변경보존,빌드/실행파일/커밋/push없음.

- 최신: 사용자 “검사해”·연결오류후 “후보4회만진행” 추가승인으로 수정한윤곽격리 두사례320/321 총8렌더완료. 원본자기/후보자기/교차픽셀차이0·윤곽외활성Renderer0. 혼합검사와수정검사의원본출력720/1118픽셀차이로다른출력혼입확인. [격리검사 정본](mask_cpu_preparation_20261024/isolation_check/README.md). 전체512·후보성능3회는미검증/미실행,채택안함. 제품원본복구·보호10동일·씬clean/PlayOFF확인. 추가512/성능측정승인없음.

- 후속 사용자 승인: 두 사례320/321·원본2회/후보2회씩 최대8렌더의 원인분리조사 완료. 두 사례 모두 자기/교차픽셀차이0. 지난321원본과 이번321원본이 달랐고, 검사가 포탑발광/음영MeshRenderer를 남긴 격리오류를 확인했다. 지난74건을 제품회귀로 단정할수없으며 구체적차이원인은미확정. [원인분리·원자료·미실행검사수정안](mask_cpu_preparation_20261024/diagnosis/README.md). 8회후추가렌더없음·제품원본복구·보호10동일·씬clean/PlayOFF. 새512비교/성능측정승인없음.

- 사용자 승인으로 원본warm15/sample90×3·A구현·대상47테스트 통과. 별도 승인받은 검사 경로 보정512쌍도 출력해시74건 차이(mask/geometry/rect차이0)로 중단했다. 원인 미확정, 추가 반복·후보 성능3회 미실행·절감량 미검증. [승인·실패·원본·결과 정본](mask_cpu_preparation_20261024/README.md).
- 제품2파일 시작working-tree 원본 복구·신규helper/테스트 Assets밖 격리. 복구 컴파일 성공·보호10파일hash동일. HEAD1de8ced·기존미커밋 보존. 실제Play/컴파일/updatingfalse·씬clean7roots·Profiler/Deep/FrameDebuggerOFF·nativeBatchOFF. [최종상태](mask_cpu_preparation_20261024/final_state.json).
- 원본Mask4.872~4.882ms·Output1.338~1.353ms·QuadUpload0.295~0.305ms는 절감량이 아니다. 다음은 실패원인/검사 신뢰성 별도조사 범위 결정이며 추가 조사·비교·측정·구현 승인 없음. A보류/B제한·아틀라스보류·원본/fallback·강조/그림자/TMP 유지. 빌드/실행파일/커밋/push 없음.

## 직전 재개 — C# 뷰 동기화 검증 완료 / 후보 미채택·제품 원본 복구

- 승인된 Task1+2의 비용 구분·좌표 후보·대상 검증·원본/후보 warm15+sample90×3을 완료했다. 직접 CPU 차이는 반복 변동과 겹쳤고 전체 FPS 개선이 없어 **미채택**했다. 두 제품 파일은 시작 원본으로 바이트 복구·diff0, HEAD `1de8ced`·기존 후속 변경 미커밋 보존. [실제 수치·조건·원본·실패·복원 정본](view_sync_20261023/README.md).
- Present 자식 활성 콜백의 좌표 회귀를 재현·보강하고 실제 풀/같은 ID 새 세대를 포함한 신규5개와 기존29개를 통과했다. 최종 후보는 적 본체→애니메이션 좌표만 재사용하고 aura/포탑 live 계산은 유지했다. 제품 채택/전체 HUD·정상30R·목표기기 검증과 구분한다. 후보·테스트는 Assets 밖 격리. [검토/보강](view_sync_20261023/REVIEW.md).
- 최종 확인: Play/changing/compiling/updating false, 저장된 단일 전투씬7roots, recording/Deep/profileEditor OFF, backgroundfalse/capture0/scale1/nativeBatchOFF, consoleErrors0. 보호6파일 hash동일. 사용자 파일·기존 강조/그림자/TMP·A 보류/B 제한·아틀라스 보류·원본/fallback 유지. 빌드/실행파일/커밋/push 없음. [상태](view_sync_20261023/final_state.json), [보호 검사](view_sync_20261023/protected_after.json).
- 다음은 결과 보고 후 새 범위를 결정하는 것이다. **Task3·다른 최적화·재측정 승인은 없으며 자동 시작하지 않는다.** 사용량/timeout 중단과 화면 조건 실패, 사용자가 별도 승인한 후보2 단회 재실행은 정본에 기록했다. 아래 계획 대기 문구는 작성 당시 기록이고 현재 상태는 이 절을 따른다.

## 직전 계획 상태 — C# 뷰 동기화 계획 완료 / 당시 구현 승인 대기

- 사용자 요청으로 [C# 뷰 동기화 중복 축소 계획](../superpowers/plans/2026-10-23-view-synchronization.md)을 작성했다. 현재 HEAD는 `1de8ced`(이번 인수인계에서 재확인), 후속 변경은 미커밋이다. **계획 작성·Notion 최신화만 완료했으며 뷰 동기화 제품 수정·새 테스트/측정·Unity 조작은 하지 않았다.**
- 다음 제안은 `SynchronizeViews`의 적/포탑/포탄 CPU 비용을 구분하고, 같은 개체의 현재 world 좌표를 본체·애니메이션·버프 표시에 재사용하는 후보 하나부터 검증하는 것이다. 안정 적 처리의 World 호출3~4→2회는 소스상 예상이며 절감ms/FPS는 미측정이다. 이전 tile을 현재 Grid로 변환하는 의미·풀 재사용·dt0·콜백 변화·보행/조준을 보존한다.
- 포탑 `FindTower`/타깃 공간 인덱스/지원 목록의 반복 검증은 별도 후속 후보다. 공개 live API의 중간 변이 감지·재진입 계약을 무작정 제거하지 않는다. 실제 비용과 안전한 준비/소비 경계를 확인한 뒤 별도 설계/구현 승인을 받는다. 좌표 후보와 마스크/렌더/셰이더·포탄 판정·목록 공유를 한꺼번에 바꾸지 않는다.
- **승인 범위:** 현재 새 구현/계측 승인은 없다. 다음 실행 전에 대상·방법·부하·시간을 설명하고 명시 허락을 기다린다. 계획의 조건당warm15/sample90×3은 제안일 뿐 승인/실행 결과가 아니며 과거 단회90프레임 승인을 재사용하지 않는다. 재로딩·Play/종료·Profiler/설정 조작도 구체 승인 범위에 포함돼야 한다. 불명확한 결과를 승인 없이 반복하지 않는다.
- 사용자에게 윤곽 후보 악화 관측과 단회/GC 차이의 한계를 보고했다. GPU 시간이 유효하지 않은 shader 미세 후보보다 직접 CPU 비용을 확인할 뷰 동기화를 다음으로 계획했다. 호출 수·Draw 감소만으로 성공이라 하지 않고 대상 CPU ms·GC·전체 프레임·동작 동등성으로 판단한다. 목표기기60FPS·정상30R·장시간/GPU·별도MB 급증 출처는 미완료다.
- 사용자 요청으로 Notion 담당3페이지를 갱신하고 fetch로 반영을 확인했다: [최적화 계획](https://www.notion.so/3ff389dbfc6881779abdeb5eeeb1cda4), [플레이테스트·변경 이력](https://www.notion.so/3df389dbfc6881fc9f1bd004adfc5999), [후속 작업 순서](https://www.notion.so/3f4389dbfc688171b84dc19b138925f4). 적용된 강조/그림자/TMP와 보류 native batch·윤곽 원본 복구, C#/shader 조사, 최신 뷰 계획/승인 대기를 구분했다. 기존 링크·이력 보존, 나머지 무관한 기획서 수정 없음.
- 현재 Unity 상태는 이번 계획/Notion/인수인계 작업에서 조회하지 않았다. 아래 윤곽 실험의 종료 상태는 당시 실제 기록이며 현재라고 단정하지 않는다. 사용자 씬·설정·미추적 파일 보존, A 보류/B 제한·아틀라스 패킹 보류·원본 출력/fallback·native body batch OFF 유지. 후보 자동 재활성화·불필요한 재검사·임의120초·자동 빌드/실행파일 실행/커밋/push 금지.
- 재개 행동: AGENTS→이 문서→위 계획을 읽고 현재 승인 대기를 유지한다. 사용자가 계획 실행을 구체 승인하면 그 범위만 진행하며 사용자 Play/다른 검사는 중단·중복하지 않는다. 이 인수인계 요청 자체는 추가 검사/Unity 조작 권한이 아니다.

## 직전 실험 — 윤곽 중복 조회 축소 후보 보류 / 원본 복구

- 사용자 “진행해” 승인으로 SelectionOutline 중앙 재사용→조기 종료+명시LOD0 후보를 순차 구현/검증했다. **일반 프레임 비교가 악화돼 제품shader는 시작 원본으로 바이트복구/diff0.** 후보는 고유 Hidden 이름의 EditMode fixture와 docs 소스로 격리했다. [실제 결과·원본·실패/한계](outline_sampling_20261023/README.md), [검토](outline_sampling_20261023/REVIEW.md). HEAD1de8ced·미커밋, 이번 새 제품 최적화 적용0.
- 대상 원본2pass2fail→중앙3pass1fail→조기종료4/4pass. 검토 후 고립float neighbor경계 보강한 최종 격리후보5/5pass, 기존 실제union렌더2/2pass. 실제Attack/Buff4방향·4위상·단독/겹침·강조1px+검사2px64쌍차이0. 테스트계측판의8031출력픽셀 논리 호출10→9→2~9(평균4.884)는 하드웨어texture요청/GPU시간/FPS성과가 아니다.
- 적128/타워15·1920×1080/ortho8.5·각warm15/sample90 단회: 평균frame23.053→24.653ms(+6.94%), FPS최소/최대/평균18.11/59.79/43.38→17.09/57.66/40.56(평균-6.49%). 후보에GC수거1회/3.49MB급증, 원본수거0으로 shader의 인과악화로 확정하지 않지만 채택 근거도 없어 복구했다. D3D12 GPUProfiler supported=false로 GPUms미검증. 중앙-only 실전 비교·추가반복/추적은 하지 않았다.
- 실제 최종상태: Play stopped/compilingfalse/scene clean7roots/Deep·recording·profileEditor·GPU·callstacks OFF/backgroundfalse/capture0/scale1/nativebatchOFF/mask entries·bindings·pages0. 보호파일/hash는 정본verification. 사용자 씬/설정·기존C#변경·미추적 보존, A보류/B제한·아틀라스보류·원본표시 유지. 빌드/실행파일/커밋/push 없음.
- 악화/보류 결과는 사용자에게 보고 완료했다. 다음 CPU 뷰 계획과 승인 상태는 위 최신 절을 따른다. 추가측정·GPU환경변경·중앙-only채택·다른CPU/Shader수정은 새 구체 승인 전 대기한다.

## 직전 조사 — C# / 셰이더 분리 보고

- 사용자 승인으로 셰이더 관련38파일(shader29/hlsl1/cginc4/Graph4: custom11/TMP27)을 정적 조사하고 C# 연결/재질 참조를 대조했다. 기존 C#108개는 이전 조사 hash와 변경/추가0개여서 분석을 재사용했다. [통합 진입](script_shader_audit_20261023/README.md), [C# 보고](script_shader_audit_20261023/CSHARP_REPORT.md), [셰이더 보고](script_shader_audit_20261023/SHADER_REPORT.md).
- 활성 SelectionOutline은 폭1의 본체 밖 fragment에서 코드상 최대10회 mask 읽기, TowerGlow5회/CoreAmbient 발광9회. 실제 피해 숫자는 TMP Mobile SDF1Pass/OUTLINE_ON/atlas1회 경로다. custom11개는 모두 소스 Pass1개. BatchedMaskOutput은 Play 등록 건너뜀/AttackBodyBatch 기본OFF/contact 런타임 호출자 없음으로 현재 병목에서 분리했다. GPU 비용/실행 Pass 수를 소스 토큰 개수로 확정하지 않는다.
- CPU는 뷰/타깃/World·마스크 준비 중복, shader는 같은 윤곽 판정의 반복 읽기를 각각 후속 후보로 보고했다. 기존 마스크 CPU4.672ms를 GPU시간으로 부르지 않는다. 후보 수정/성과 없음. 다음 실행·수정은 구체 승인 전 보류.
- 이번에는 Unity 연결/Play/Profiler/재로딩·제품/씬/설정 수정·테스트/빌드/실행파일/커밋/push를 하지 않았다. 보고서와 파일별 정적 색인만 생성·보존검사했다. 현재 Unity 상태는 조회하지 않았으며 이전 종료 상태를 현재로 단정하지 않는다. 사용자 파일·원본 출력·A 보류/B 제한/아틀라스 보류/native batch OFF 정책 유지.

## 직전 작업 — 최초 피해 숫자 준비 / 전체 스크립트 정적 조사

- 사용자 승인으로 TMP 설정과 HitDamage3개를 HUD 초기화에서 투명 glyph 메시까지 준비해 기존 풀로 반환한다. 출력·수명 .85초·초과 생성·Gold/Core·효과 수 유지. 제품 변경은 HUD1파일, 신규 회귀 테스트1개. HEAD `1de8ced`·미커밋. [구현/원본/수치/실패/한계](damage_number_prewarm_20261023/README.md).
- 대상 테스트 RED1/1실패→GREEN1/1통과, 최종1/1통과: 숨김·3개 재사용·4번째 확장·수명/반환·원본 fallback 대비 glyph 정점/UV/색상·공유 재질 일치. 독립 소스 검토 지적 없음. 전체 HUD 픽셀/모든 기기/언어·재시작 전수 통과 아님.
- 일반 적128·타워15/각warm15/sample90 단회: frame평균27.077→23.728ms, 최대128.742→48.105ms, FPS최소/최대/평균7.77/57.89/36.93→20.79/55.82/42.14. 기존 지연위치 engine22는128.742→36.357ms·후보 CreateNumber0. 후속28~37프레임에 추가 생성0.56~1.04ms가 남아 모든 생성 제거 아님. 전의14.55MB 할당 급증 출처·Editor 변동을 분리 못해 차이 전체를 수정의 인과 효과/목표기기60FPS 통과로 보지 않는다.
- 준비 관측0.5832ms, 비활성3개·직접 소유객체/컴포넌트/메시 합19,599B. 공유 리소스/managed TMP buffers 제외이며 앞선 테스트로 데워진 Editor일 수 있어 cold 초기 비용/전체 메모리는 미검증이다.
- Assets C#108개(시작107+이번 테스트1): 런타임54/Editor4/테스트50 정적 색인·패턴/호출경로 조사. [후보13개·소스 위치·구현 위험·기존 풀/캐시/비활성 경로](damage_number_prewarm_20261023/SCRIPT_AUDIT.md), 파일별 hash/범위는 같은 폴더 inventory. 포탄 명중 삭제·뷰 타깃/캐시 재검증·격자 World 변환·버프 aura 전수검사·오디오 enum/key/clip조회·HUD 반복생성·보행 상수/임시배열을 후보로 기록했다. 추가 제품 수정/측정 승인으로 확대하지 않았다. 지속 Emphasis5.611ms/내부Mask4.672ms가 실제 남은 CPU 비용이며 중첩 합산 금지.
- 시작 Recording ON은 사용자가 사용 중이 아니라고 확인하고 끄도록 승인했다. 후보 첫 비교는 화면 조건guard에서 표본0 종료·복원; 사용자 추가 승인 후 현재1920×1080/index3 유지·단회 재실행90개 완료·동일 화면 선택 복원. 최종 실제 상태와 보호5파일 해시는 담당 폴더 final_state/protected_after가 담당한다. 사용자 파일 보존·A 보류/B 제한·SpriteAtlas 보류·원본 표시/native batch OFF·빌드/실행파일/push/커밋 없음.
- 다음 행동은 이 조사 결과를 사용자에게 보고하고 다음 한 단위 수정 범위를 정하는 것이다. 후보 ms/할당 기여와14.55MB 출처는 미확정. 추가 Play/프로파일링/다른 후보 수정은 별도 구체 승인 전 보류한다.

## 직전 조사 — 승인된 초기 전투 Deep Profile 1회

- 사용자 명시 승인으로 Deep/profileEditor ON·재로딩 후 적128/타워15·warm15/sample12 1회 캡처했다. 이후 자체 Play 종료·Deep/profileEditor OFF·재로딩 및 실제 복원 확인. 제품 수정 없음. [호출 경로·원본·한계](deep_initial_stall_20261023/README.md).
- 초기 피격의 `MoveProjectiles→ApplyDamage→HUD.ShowDamageNumber→CreateFloatingNumber→TMP.Awake→TMP_Settings.instance→Resources.Load`를 확인했다. Profiler4에서 새 숫자3개 생성62.959ms(포함된 TMP Settings 최초 로드1회42.573ms), Session.StepTick76.695ms. 다음 프레임 StepTick8.165ms. Deep 시간이며 기존 일반100ms 스톨 전체 기여율/절감량으로 확정하지 않는다. 기존 일반 UI.Text 피해 목록 확장과 구분한다.
- 강조/마스크·SynchronizeViews가 지속 비용으로 남고, Deep 프레임에는 Editor 뷰 재그리기55.8~150.0ms와 Profiler 통합도 포함됐다. 전체를 게임/GPU 비용으로 보지 않는다. Deep 구간 GC 수거1회·최대10.223ms는 일반 모드 결과와 구분한다. 4~6MB 별도 급증 출처는 미확정.
- raw175만 이상 안전 상한으로 자동 raw 추출은 건너뛰고, 이미 저장된 native 병합 Hierarchy만 제한 조회했다(각180항목·native build43/57ms·탐색89/105ms). 프로파일러 수신 지연을 확인해 확정된 프레임 시간으로 대응했다. 새 Play/추가 캡처 없음.
- 다음 수정 제안은 기존 숫자 풀을 초기화/준비 단계에서 예열하고 TMP 최초 로드를 전투 전에 끝내는 것이다. 표시 수/수명/아트 유지, 준비 비용/메모리와 일반 모드 초기 피격 전후 확인 필요. 이번 승인은 진단/보고이며 제품 수정은 하지 않았다.
- 최종 실제 상태: Play stopped/compiling false/Deep·recording·profileEditor·callstacks OFF/driver true/background false/captureDeltaTime0/timeScale1/scene clean. 보호5파일 해시 동일·사용자 파일 보존. A/B 제한·SpriteAtlas 보류·네이티브 배칭 OFF, 빌드/실행파일/push/커밋 없음.

## 직전 작업 — 넓은 그림자 이름 조회 캐시

- 사용자 승인으로 PhotoShadow의 반복 renderer.name 조회를 구조 Revision 기반 분류 캐시로 바꾸고 OnEnable/명시 무효화 때 갱신한다. 넓은 그림자·원본/숨김 규칙 유지. 이름만 바꾸는 호출자는 InvalidateSources 필요. 소스 반영·HEAD `1de8ced`·미커밋, A/B 제한·SpriteAtlas 보류·네이티브 본체 배칭 OFF 유지.
- 그림자13/13(안정 Present 무할당2개 포함)·공유 윤곽3/3 통과. 같은 원본 개체9종·18쌍 픽셀 차이0. EditMode 수명주기 테스트 오류/실패도 보존했다. [원본/결과/한계](shadow_name_cache_20261023/README.md).
- 적128/타워15·각warm15/sample90: 그림자Present1.659→0.850ms(-0.809ms/-48.78%), 전체 GC 중앙127182→20402B/frame(-83.96%), 평균165149→121456B/frame(-26.46%). 평균FPS42.16→43.62 단회 관측, 목표기기 통과/전체 성능 개선 확정 아님.
- 수정본106프레임에서4.82MB 단발 할당이 남았다. recording OFF라 출처 미확정, 반복 이름 할당 감소와 MB 급증 해결을 구분한다. 다음은 남은 급증/HUD 생성 비용 구분이며 추가 검사/수정은 새 구체 승인 전 보류.
- 자동 종료·실제 복원: Play stopped/compiling false/Deep·recording·callstacks·profileEditor OFF/driver true/background false/captureDeltaTime0/timeScale1/scene clean/native batch OFF. 보호5파일 해시 동일. 사용자 파일 보존, 빌드/실행파일/push/커밋 없음.

## 직전 조사 — GC 할당 급증 출처

- 사용자 승인으로 일반 Profiler warm15/sample90 1회, 상위2프레임 Main Thread만 추적했다. Deep/할당스택 OFF·재로딩/제품 수정 없음. 각9345/8561샘플·19/14ms로 상한 이내 완료. [추적/원본/미확정](gc_source_20261023/README.md).
- 상위2프레임 HUD.Update 경로426612/249978B, PhotoShadow.Cast는각106966B. HUD는 Text 생성/OnEnable 집중, 피해 행은 이미 재사용되므로 초기 확장과 상시 생성을 구분한다. 그림자는 renderer.name 반복 조회가 확인된 지속 할당 경로다.
- 이번 최대560836B로 **앞선5~6MB 급증은 재현되지 않아 출처 미확정**. CollectionCount(0)1879→1879·GC.Collect0ms·증분 마커 최대0.0018ms로 큰 수거 정지 근거 없음. 앞선 평균 증가를 GC 수거 정지로 부르지 않는다. Thread 할당 API의0은 무효로 기록했다.
- 자동 종료·실제 복원: Play stopped/compiling false/Deep·recording·callstacks·profileEditor OFF/driver true/background false/captureDeltaTime0/timeScale1/scene clean/native batch OFF. 보호5파일 해시 동일. 빌드/실행파일/push/커밋 없음.
- 다음 제안은 PhotoShadow 이름 분류 캐시로 반복 getter 할당을 줄이고 원본 그림자/숨김/동적 구조 회귀·같은 짧은 비교를 확인하는 것이다. 새 수정/검사는 구체 범위 승인 전 보류. HUD/미재현5~6MB 문제까지 해결됐다고 하지 않는다.

## 직전 작업 — 강조 CPU 중복 처리 축소

- 사용자 승인으로 강조의 본체 참조를 윤곽 마스크에 전달해 중복 구조 검사·색상 사전 조회를 줄이고 실제 적용한 tint만 복원하도록 변경했다. 활성 부품 규칙·밝기/윤곽/원본/정렬 유지. 소스 반영·HEAD `1de8ced`·미커밋. 네이티브 본체 배칭은 OFF, A/B 제한·SpriteAtlas 보류 유지.
- 최종 강조4/4·독립 윤곽3/3 통과, 같은 원본 개체의 좌우/4위상/단일·겹침16쌍 픽셀 차이0. 별도 재생성 개체 캡처의 겹침 차이는 원인 미확정으로 보존. [결과/원본/검사 한계](emphasis_cpu_20261023/README.md).
- 적128·타워15/조건별warm15/sample90: 준비5.701→5.017ms, 색상 적용0.798→0.773ms, 두 구간 합0.708ms/10.90% 절감 관측. 전체 강조 수명주기/중첩 마스크 합이 아님. 평균FPS40.40→44.56이나 단회 Editor 관측. GC 평균263368→350920B/frame·SetPass 증가 관측으로 전체 성능 통과 판정 보류, GC 원인 미확정. 첫 후보의 비활성 부품 순회 악화도 보존했으며 해당 수정본에만 추가90프레임 비교했다.
- 자동 종료/실제 복원 확인: Play stopped/compiling false/Deep·recording OFF/driver true/background false/captureDeltaTime0/timeScale1/scene clean/native batch OFF. 보호5파일 해시 동일. 사용자 파일 보존, 빌드/실행파일/push/커밋 없음.
- 다음 제안은 GC 스파이크 출처 확인이다. 추가 수정·Play/프로파일링·다른 시스템 확대는 새 승인 전 진행하지 않는다. 목표기기60FPS·정상 장기 플레이·전체 메모리 검증은 미완료.

## 직전 작업 — 공격형 옆 보행 네이티브 배칭 후보

- 사용자 승인으로 Attack 옆 보행5부품을 원본 Sprite/SpriteRenderer·정렬 유지 + 공통 Texture2DArray/재질 방식으로 구현했다. 이전 수동 메시 후보는 픽셀/순서 실패로 폐기. 기본 OFF·원본 출력·A/B 제한·SpriteAtlas 패킹 보류 유지. 복구 HEAD `1de8ced`, 미커밋.
- 실제 대상7/7 통과·격리 좌우/4위상/단일 및 겹침16쌍 차이0. 적128(Attack112/Buff16)·타워15, 고정 simulation dt1/60·warm15/sample90 각1회: Draw 평균1579.52→1020.43(-559.09/-35.40%). FPS 최소/최대/평균 원본6.78/51.32/33.08 → 후보13.80/43.19/34.88. 단회 Editor 관측이며 FPS 개선 확정·목표기기60FPS 검증이 아니다.
- 후보 marker 평균3.812ms·GC 평균+238200B/frame로 부담이 크다. 소스 검토에서 초기화 예외 시 부분 리소스 정리, 외부 material 교체/중간 비활성화 시 임시 alpha/MPB 복원 문제도 확인해 채택 보류. [상세 결과·원본·미해결](attack_body_batch_20261023/README.md).
- 측정 자동 종료/실제 복원 확인: Play stopped/compiling false/Deep·recording OFF/driver true/background false/captureDeltaTime0/timeScale1/scene clean/candidate OFF. 보호5파일 해시 동일. 사용자 파일·설정·미추적 보존, 빌드/실행파일/push/커밋 없음.
- 다음 제안은 후보의 반복 shader 조회·material/MPB 연결 비용 축소와 복원/초기화 안전성 보완, 대상 회귀 검사·같은 짧은 비교다. **사용자 승인 전 추가 수정·재검사·활성화하지 않는다.** 남은 다른 종류/그림자/마스크로 확대하지 않는다.

## 앞선 재개 중단 기록 — 2026-10-23 10:35 KST

- 시작 HEAD `1de8ced`. 제품 코드/씬/설정 변경 없이 `combat_cost_20261023/`에 진단 기록을 생성했다. 일반90프레임/유효 PlayerLoop89개에서 적128·타워15 유지, 강조5.630ms·넓은 그림자1.856ms·Prototype.Update2.355ms·CombatArt.Update0.0106ms 평균 inclusive 관측. 중첩 시간 합산 금지, 개선 전후/목표기기 검증 아님.
- 불필요한 작업을 중단하라는 사용자 지시로 추가 진단·압축 작업 중단. Deep30프레임 원본은 저장됐지만 세부 호출 추출이 Unity 메인 스레드를 오래 점유 중이다. 자체 Play 종료/Profiler·Deep OFF/배경 false 복원 요청은 2초 timeout으로 **실행·복원 미확인**. 강제 종료하지 않았다. 다음 작업은 추가 검사가 아니라 현재 추출 종료 여부와 자체 상태 복원 확인부터다. 아래 이전 종료 상태를 현재로 가정하지 않는다.
- `combat_cost_20261023/`는 미완료·미커밋. 원본 `.data`는 유지하며 압축은 검증 실패 상태이므로 유효 아카이브로 쓰지 않는다. 기존 사용자 파일·후보 보류 유지. 빌드/실행파일/push 없음.

## 현재 위치·승인 범위

- 프로젝트 `C:\Users\edcde\OneDrive\Desktop\CD`, 브랜치 `codex/shared-body-mask`. 이번 문서 정리 시작 HEAD `87490f2`(Notion 최신화), 최신 게임/호환 코드 `bca9dd8`. 작업별 복구 기준은 담당 정본을 따른다. 이번 인수인계는 문서만 변경하며 Unity 연결/Play 상태를 새로 조회하지 않았다.
- 목표는 GTX750Ti·RAM4~8GB급 안정적60FPS. 현재30R 성장/물량·밸런스와 승인 원본·방향/보행·넓은 그림자·강조/HUD·효과 수/수명·사운드·입력 반응을 유지한다. 사용자 관심은 **적128·타워15에서 실제로 가장 비싼 처리, 특히 포탄/효과**다. 임의120초 측정이나 끝난 검사를 반복하지 않는다.
- **SpriteAtlas 호환/제작 기반은 준비했지만 실제 패킹은 보류·원본 표시로 복구했다.** A 보류와 B 제한·원본 출력/개별RT·방어형/보스 fallback을 유지한다. 현재 게임에 표시용 아틀라스가 적용됐다고 설명하지 않는다. 이번 인수인계 자체는 추가 실행 권한이 아니다.

## 최신 구현·실제 검증

- 부위 좌표/원본 UV·B 원본 키 연결, 신규 입력 FullRect, 폴더 기반 제작/빌드 전 동기화, Domain Reload OFF의 보행 캐시/매핑 초기화 불일치를 수정했다. 후보는 적 기본/앞뒤/BossEye36개와 타워9개였다. 옆 보행 OriginalWalk20개는 픽셀 회귀로 제외, 타워 발광/음영도 원본 샘플링 유지. 현재 관리 후보2개는 Include in Build OFF/packables0이고 빌드 hook도 보류 유지. 기존 사용자 `CG_Enemies`는 별도 보존한다.
- **포탄·피격9종은 이미 CombatB.png1텍스처 공유.** 단순 재포장으로 현재 Draw가 줄지 않아 추가 아틀라스는 생성하지 않았다. 포탄 이동/충돌/탐색의 실제 병목은 이번에 확정하지 않았다.
- 적128·타워15 고정,1920×1080/ortho8.5/time1·warm15/sample90 각1회 최종 코드 비교: **원본 평균52.30FPS / 후보43.33FPS**, 후보 Editor Texture2D 약4.50MiB 증가·Draw/SetPass 소폭 증가 방향으로 보류. 동적 전투/Editor 변동을 분리하지 못해 확정 인과 효과·목표기기60FPS 판정으로 쓰지 않는다. 초기 좋은 후보52.85 표본만 채택하지 않았고 모든 실패/원본을 보존했다.
- 대상55/55 통과·후보 격리 렌더24쌍 차이0(256×256/ortho2/단일 위상)·기존 B manifest92/92 유효. 모든 방향/위상·정상 조작·전체 HUD/native 줌 전수·아트 승인과 구분한다. [결과/구현/실패/추가 규칙](sprite_atlas_20261023/README.md), [최종 수치](sprite_atlas_20261023/final_comparison.json), [검토](sprite_atlas_20261023/REVIEW.md).
- 마지막 실제 실행은 후보 해제 후 **원본 표시 적128·타워15/90프레임**이며 error=null·현재 콘솔 오류0을 확인했다. 이후 대상 검사/최종 조회: Play stopped/compiling false/scene clean/원래7루트/자체 mask RT0·Profiler OFF/driver true/background false, 씬·사용자 설정2개·EditorSettings hash4개 동일. 당시 종료 상태이며 재개 시 현재로 가정하지 않는다. [최종 상태](sprite_atlas_20261023/held_final_state.json).

## 리소스·디렉터리·Notion

- 미사용 게임 이미지69개 제거·연결170개 보존, 기록 캡처13개는 삭제하지 않고 문서 폴더로 이동/동일 hash 확인. 삭제 전 씬 저장은 사용자 명시 승인, 복구 `5ae39a1`/정리 `4281424`. 소리·폰트·코드·설정·프리팹·씬 삭제가 아니다. [정리/복구](resource_cleanup_20261023/README.md), [재사용 후보](resource_cleanup_20261023/REUSE_CANDIDATES.md).
- 기존 Assets552개 분류·551개 이동, Art/Audio/Data/Documentation/Fonts/Prefabs/Scenes/Scripts/Settings9분류. 복구 `c1203f1`/완료 `babac77`. GUID/meta/의존성·Resources key·assembly 보존과 대상22개 검증 완료. 당시 안내 포함553개, 이번 신규6개 이후559개(.meta 제외). [위치 색인](directory_reorganization_20261023/DIRECTORY_INDEX.md), [이동표](directory_reorganization_20261023/path_map.csv), [검증 정본](directory_reorganization_20261023/README.md). 과거 진단 코드는 재실행 전 이동표로 경로를 확인한다.
- Notion 담당7페이지 최신화·재조회 완료(`87490f2`). 호환 준비/패킹 보류·최종 수치/실패/한계·리소스 보존·파일 위치·후속 순서를 담당별로 반영하고 기존 하위페이지/DB 보존. [동기화 대상/확인](sprite_atlas_20261023/NOTION_SYNC.md). [최적화 계획](https://www.notion.so/3ff389dbfc6881779abdeb5eeeb1cda4), [검증 이력](https://www.notion.so/3df389dbfc6881fc9f1bd004adfc5999), [디렉터리](https://www.notion.so/401389dbfc6881c0b3dbec4c2050a56f), [후속 순서](https://www.notion.so/3f4389dbfc688171b84dc19b138925f4).

## 앞선 채택 작업 — 상세는 담당 정본

- 길219개→메시2개+픽셀 민감 원본23개, 설치 표시858개→재사용 메시1개 적용. 사용자 미리보기 승인으로 동적 발밑 접촉만 제거하고 넓은 PhotoCastShadow 유지. 과거 접촉 제거60프레임 비교40.35→45.00FPS는 별도 예비 결과이며 최신 표본과 합산하지 않는다. [길](route_mesh_20261022/README.md), [설치](placement_overlay_20261023/README.md), [접촉 제거](contact_shadow_removed_20261023/README.md).
- 마스크 RT/참조 재사용·동일 본체 공유·HUD/표시/적 본체/모델 풀링·Session 반복 비용과 B 생성 배칭은 이미 구현했다. 누적169개/당시 B 대상72개 등 충분한 검사는 새 변경/실패 없이 반복하지 않는다. B 페이지당16MiB·copyback 비용이 있어 메모리 절감/전체 잔류 통과가 아니다. [B 결과/제한](render_batching_20261022/B_RESULTS.md), [누적 CPU/GC](runtime_validation_20261021/RESULTS.md), [RT 재사용](mask_reuse_20261020/RESULTS.md), [동일 본체 공유](shared_body_mask_20261021/RESULTS.md).
- 과거 정상 초반120초/카운터15초 관측은 최신 실행이 아니다. ToArray 충돌·고정 카운터 무효·메모리 증가/스파이크 한계를 보존한다. ProfilerRecorder.ToArray/CopyTo 충돌 경로와 capacity1 덮어쓰기 없는 옛 도구를 재사용하지 않는다. [당시 관측](normal_probe_20261023/README.md).
- 기존 Windows64 일반 빌드 `Builds/CoreGuardian_Windows_20261022_062018/CoreGuardian.exe`와 ZIP은 **B 반영 전**, 보존한다. 당시 빌드 성공만 확인했고 실행하지 않았다. 이후 새 빌드/실행/push 없음. Unity CLI1.0.0-beta.14는 이미 설치·확인했으므로 옛 MCP 안내만 보고 재설치하지 않는다.

## 미완료와 다음 행동

1. 실제 HEAD/diff·사용자 파일·현재 Unity 연결/씬/dirty/Play/컴파일·다른 검사부터 확인한다. 사용자 Play/다른 검사를 중단·중복하지 않는다.
2. **원본 표시/보류 유지 상태에서 적128·타워15의 실제 비용 구분**부터 작게 진행한다. 포탄 이동/충돌/타깃 탐색, 렌더 제출·재질 전환·마스크 중 무엇이 비싼지 유효한 같은 조건의 근거를 확인한다. 최소/최대/평균FPS·frame ms·원본·한계를 보고한다. 제거 전 Contact4.579ms를 현재 병목으로 다시 제시하지 않는다. CombatB 재포장이나 기존 pooling/cache를 중복 구현하지 않는다.
3. SpriteAtlas 재활성화·옆 보행 직접 표시 배칭은 품질/비용 근거 확보 전 보류. A·B 출력 직접 배칭/개별RT 제거도 자동 확대하지 않는다. 신규 종류/방향/부품은 소유 폴더/Resources 계약을 유지하고 별도 품질을 확인한다. 현재 코드 검사는 새 아트 승인까지 뜻하지 않는다.
4. 정상30R 자연 플레이·장시간 부하, 유효 GPU/순수 Render Thread·Memory Profiler peak/전체 잔류, 반복 재시작·전 방향/위상/중첩/재등장·정상 조작/청취는 별도 미완료다. [기존 보스 합성 미확정9건](mask_validation_20261021/README.md)도 유지한다. D3D12 GPU Profiler 미지원/Memory Profiler package 미검출을 무효0 성과로 쓰지 않고 설치/API 변경을 자동 수행하지 않는다. 구체 CPU·해상도·배속/최대 부하 예산도 확정 전이다.
5. 사용자 씬·설정·미추적·개인정보/분석 설정·폰트 도구 접근오류 표시는 보존한다. 현재 unstaged는 `ProjectSettings/PackageManagerSettings.asset`, `ProjectSettings/TimeManager.asset`, 범위 밖 Unity 직렬화 차이 `Assets/Art/Editor/ProjectWelcome/2d-template.png.meta`의3개이며 임의 복원/삭제/포괄 커밋하지 않는다. 전투씬 `Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity` 하나 유지, YAML 직접수정 금지. 빌드·실행파일 실행·push는 별도 명시 요청 때만 한다.

## 공통 진입 문서

[최적화 규칙](OPTIMIZATION_RULES.md), [대상 QA](TARGETED_QA.md), [로컬 Git](LOCAL_GIT.md), [작업 흐름](WORKFLOW.md), [현재30R 기준](aggressive_30r_20261020/README.md). 밸런스·아트/HUD·사운드를 실제로 다룰 때만 해당 보호 규칙을 추가로 읽는다. 상세 이력/원본은 담당 정본에 두며 옛 소스 교체 manifest를 현재 파일에 무조건 복원하지 않는다.
