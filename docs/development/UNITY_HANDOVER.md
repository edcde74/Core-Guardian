# 코어 가디언 인수인계

갱신: 2026-10-23 Asia/Seoul. 현재 사용자 지시·최신 담당 Notion 우선. [WORK_ROUTING.md](WORK_ROUTING.md)에서 필요한 분야만 읽는다.

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
