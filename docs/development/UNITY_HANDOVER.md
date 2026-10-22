# 코어 가디언 인수인계

갱신: 2026-10-23 Asia/Seoul. 현재 사용자 지시·최신 담당 Notion 우선. [WORK_ROUTING.md](WORK_ROUTING.md)에서 필요한 분야만 읽는다.

## 현재 상태와 승인 범위

- 2026-10-23 설치 표시 최적화: 사용자 승인으로 칸별858개 sprite 생성/삭제를 재사용 메시1개로 변경했다. 기존색·alpha·간격·정렬·CanInstall 및 default Lit 유지. 두 줌 Camera.Render 차이0, 신규 EditMode4+기존 설치 PlayMode4 통과. 고정 설치 비교에서 draw 중앙값915→58, 관측 frame 평균5.246→3.766ms. 정상 전투/저사양 성능 통과는 아니며 raw 범위·spike·한계는 [설치 표시 정본](placement_overlay_20261023/README.md)에 있다.
- 설치 검증 종료: Play stopped/scene clean/원래7루트/자체 overlay mesh·material0/compiling false. background false/time1/capture0/paused false/Profiler OFF/driver true, 기존 auto-tick true16 유지. 씬과 사용자 설정2파일 hash3/3 동일. 추가 씬 저장·빌드·실행파일·push 없음. A 보류/B 적용 제한은 그대로다.

- 2026-10-22 후속: 사용자가 공식 `Mesh.CombineMeshes()`로 고정 길 배경 병합을 승인했다. 현재 에디터 메모리에는 길219개→메시2개 + 원본23개 보존, Game View Draw258→41/SetPass2→5/Triangles532 유지, 최종 적용 직전·직후 두 줌 픽셀 차이0. 도구·한계·원복/Undo·전체 프레임 미측정은 [길 메시 정본](route_mesh_20261022/README.md)을 따른다. A 보류/B 제한은 유지한다.
- **씬 저장 완료:** 자동 승인 검토가 처음 거절한 뒤, 사용자가 기존 사용자 변경을 포함한 저장을 명시 승인했다. 병합+자체 빈 테스트 루트 제거 상태를 Unity API로 저장하고 PreviewScene 격리 fixture에서 대상7/7 통과. 최종 scene clean/원래7루트/테스트루트0/persistent 메시·재질 참조 정상/Draw41/SetPass5, 설정2파일 기존 해시 동일. 저장된 씬은 사용자 기존 변경과 섞여 로컬 도구·기록 커밋에는 포함하지 않는다. 자세한 상태는 [저장 후 상태](route_mesh_20261022/saved_final_state.json)와 길 정본을 따른다.

- 경로 `C:\Users\edcde\OneDrive\Desktop\CD`, 브랜치 `codex/shared-body-mask`. B 구현·검증 `bc9c847`, 구현 커밋 기록 `3c4ecb7`, Notion 동기화 기록 `48dc7d5`가 이번 인수인계 전 HEAD다. 제품 소스/리소스/검사의 추가 미커밋 변경은 없다. 사용자 씬·설정 M3, 폰트 도구 접근불가 D369 및 미추적 자료는 보존한다.
- 사용자가 B안 재개·계속을 승인해 **공용 GPU 페이지 마스크 생성 배칭**을 반영했다. A는 보류·미채택이다. B의 출력 renderer/material/order와 개별 RT/copyback은 원본을 유지한다. 방어형·보스와 지원 밖 조건은 원본 마스크 생성이다. 적용 범위·실패·결정은 [B 결과 정본](render_batching_20261022/B_RESULTS.md)을 따른다. 과거 [A/B 복원 기록](render_batching_20261022/ROLLBACK.md)은 B 재개 전 이력이다.
- HUD/표시·적 본체·모델 풀링, Session 버퍼/탐색/타깃/탄환 정리·캐시 최적화는 앞선 `5943523`에서 구현·누적 검증했다. 현재30R 성장/물량·밸런스, 아트·방향/보행·그림자·강조·HUD·효과 수/수명·사운드·입력 반응을 줄이지 않는다. 전투 우선순위와 최초/현재 대상 유지 계약을 보존한다.
- 사용자 요청으로 Notion 4페이지를 갱신·읽기 검증했다: [최적화 계획](https://www.notion.so/3ff389dbfc6881779abdeb5eeeb1cda4), [플레이테스트 및 변경 이력](https://www.notion.so/3df389dbfc6881fc9f1bd004adfc5999), [프로젝트 메인](https://www.notion.so/3df389dbfc6881bc858bce200d1bb742), [후속 제작·검증 순서](https://www.notion.so/3f4389dbfc688171b84dc19b138925f4). RT 재사용/동일 본체 공유/CPU·GC 누적/B의 조건별 성과·실패·비용·미완료를 구분했다. 기존 하위7페이지와 DB 연결을 유지했다. 현재 ‘구현 미착수’ 표시는 수정했고 과거 날짜별 이력은 보존했다.

## 실제 검증 결과

- 최신 B: 같은128적/실제15포탑·고정dt·3회에서 Draw2087.311→1948.954(**−6.628%**), SetPass458.993→319.426(**−30.407%**), native 평균36.217→30.169ms, 평균FPS27.612→33.147(**+20.047%**). 조건당542native/540실제Tick, 이벤트/최종 상태3회 일치. Editor 주입 조건의 부분 개선이며 정상30R/장시간/Player/GPU 성능 통과나 당초70%/50% 목표 달성은 아니다. [최종 분석](render_batching_20261022/b2_benchmark/analysis_shipping.json).
- 최신 B 대상검사 **신규24+관련기존47+실제씬30R·재시작1=72개 통과**. 4종132적/15포탑·줌8.5/4.25 정지 화면은 원본 대비 차이0(Overlay HUD 미포함). 기존169개 전체를 반복하지 않았으며 검사 수를 합산해 새 전체 통과 수로 쓰지 않는다. [검사 집계](render_batching_20261022/shipping_test_summary.json), [4종 화면](render_batching_20261022/b_all_kinds_scene_pixels.json).
- 앞선 CPU/GC 누적: Tick−38.616%·View−9.599%·Main Thread GC.Alloc/frame−61.504%, 평균FPS30.169→30.026(−0.476%)로 FPS 개선은 확인되지 않았다. EditMode127+PlayMode42=169개 통과. 보스 미존재 기본 handle의 HUD 오류도 수정했다. [누적 결과](runtime_validation_20261021/RESULTS.md). B와 조건/비교 기준이 달라 개선율을 합산하지 않는다. 최초 RT 재사용 및 동일 본체 공유의 별도 성과는 각 담당 문서와 Notion 검사 이력에 둔다.
- B 페이지당16MiB 추가 GPU 저장공간 및 개별RT 복사 비용이 있다. 메모리 절감·전체 잔류 검증 완료가 아니다. 최종 독립 검토는 서비스 제한으로 전체 완료되지 않았으며 지적된 결함은 RED→GREEN으로 수정했다. 상세 한계는 B 정본에 보존한다.
- 마지막 실행 검증 종료 시 scene clean/Play stopped/compiling false, 자체 마스크GPU 객체0, 사용자 씬·설정6파일 해시 동일. Profiler driver true/Profiler OFF/background false/time1/capture0/paused false. 빈 Profiler 백업 LoadProfile의 자체 진단 오류2건은 삭제하지 않았고, 저장본4바이트를 확인해 빈 버퍼(first/last−1)로 복원했다. [종료 상태](render_batching_20261022/shipping_final_state.json), [복원](render_batching_20261022/shipping_profiler_restore.json), [사용자 해시](render_batching_20261022/shipping_user_hashes.json).
- 이후 Notion 갱신·이번 인수인계는 문서 작업만 수행했다. **현재 Unity 상태를 새로 조회하거나 Play·검사·빌드·실행하지 않았다.** 재개 시 마지막 실행 상태를 현재로 가정하지 말고 확인한다.
- 기존 Windows64 일반 빌드 `Builds/CoreGuardian_Windows_20261022_062018/CoreGuardian.exe`는 사용자 요청으로 생성, Succeeded/오류0/경고499/35.383초이며 실행하지 않았다. **B 반영 전 빌드**다. ZIP 및 폴더 모두 보존한다. 이후 새 빌드·실행파일 실행·push·CLI 업데이트는 하지 않았다.

## 미완료와 다음 행동

1. 실제 HEAD/diff·사용자 파일·연결/씬/dirty/Play/컴파일·다른 검사부터 확인한다. 사용자 Play/다른 검사를 중단·중복하지 않는다. 충분한 기존169개/최신72개 검사는 새 변경·실패·불확실성 없이 반복하지 않는다. 이번 인수인계 자체는 추가 실행 권한이 아니다.
2. **남은 정상30R 자연 플레이·장시간 실제 부하, GPU/순수 Render Thread·Memory Profiler peak/전체 잔류 검증부터** 이어간다. 목표기기/FPS/배속 예산은 미확정이다. 현 D3D12 Editor는 GPU Profiler 미지원, Memory Profiler package 미검출이다. 무효0을 절감으로 해석하거나 패키지 설치/그래픽 API 변경을 자동 수행하지 않는다.
3. 모든 방향/보행 위상·선택/피격/강화·접촉/앞뒤 중첩·재등장 효과, 실제 조작·청취, 모델 풀링 직전 버전 단독 효과는 남아 있다. 기존 보스 전체 합성 미확정9건과 원래 순간 지연도 별도 미완료다. 사용자 본 범위의 깜박임·잘림·방향 이상 없음 보고를 전체 전수 통과로 확대하지 않는다.
4. B의 현재 제한·원본 출력·개별RT와 방어형/보스 fallback을 유지한다. **A 재검토·출력 직접 배칭·RT 제거는 미채택/후속**이며 임의 확대하지 않는다. 이미 구현한 pooling/cache를 중복 구현하지 않는다.
5. 사용자 씬·설정·미커밋/미추적·개인정보/분석 설정·폰트 도구 접근오류 표시를 임의 복원/삭제하지 않는다. 전투씬은 `Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity` 하나 유지, 씬 YAML 직접수정 금지. 빌드·실행파일 실행·push는 별도 명시 요청 때만 한다.

## 필요한 정본

- [최적화 규칙](OPTIMIZATION_RULES.md), [대상 QA](TARGETED_QA.md), [로컬 Git](LOCAL_GIT.md), [작업 흐름](WORKFLOW.md).
- [최신 B 구현·실측·품질·실패/한계](render_batching_20261022/B_RESULTS.md), [CPU/GC 누적 결과](runtime_validation_20261021/RESULTS.md), [RT 재사용](mask_reuse_20261020/RESULTS.md), [동일 본체 공유·적용 제한](shared_body_mask_20261021/RESULTS.md).
- [모델 풀/API 수명](model_pooling_20261021/README.md), [적 본체 풀](enemy_body_pool_20261021/README.md), [표시/Session 재사용](runtime_reuse_20261021/README.md), [HUD 갱신](hud_refresh_reuse_20261021/README.md), [남은 런타임 구현](remaining_runtime_20261021/README.md).
- [기존 보스 등 미확정](mask_validation_20261021/README.md), [현재30R 기준](aggressive_30r_20261020/README.md). 원본 native `.data`·비교 소스/해시·씬 백업은 각 정본이 가리키는 `Logs/`에 보존한다. 예전 소스 교체 manifest는 중간 버전이므로 현재 파일에 무조건 복원 스크립트를 적용하지 않는다.
