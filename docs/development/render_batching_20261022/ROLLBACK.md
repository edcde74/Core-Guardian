# A/B 작업 중지 및 복원

2026-10-22 사용자 요청: 작업 중지, A/B 작업 전으로 복원.

- 제품 기준 `1278449`(기존 구현·검증 `5943523`). 소스·리소스·기존 검사·렌더 설정·Packages의 해당 기준 대비 diff 없음.
- A1/A2는 실제 화면 픽셀 회귀로 미채택했다. A2 최초 렌더 중단 캡처의 낮은 Draw/SetPass는 무효다. 수정 후에도 픽셀 회귀 및 native frame 비용 증가로 채택하지 않았다. 후보는 `candidates/`에 보관하며 Assets에서 제외했다.
- B는 allocator 신규 검사 파일 작성까지만 진행했다. 구현은 없으며 검사와 meta를 `candidates/cancelled_mask_page/`로 옮겼다. 추가 검사는 실행하지 않았다.
- 사용자 전투씬·PackageManagerSettings·TimeManager·UniversalRP 4파일의 SHA256은 `hashes_before.json`과 일치한다. 사용자 미추적 파일·폰트 접근오류 표시는 건드리지 않았다.
- Play 중지/scene clean 상태에서 기존 Profiler buffer를 재로드하고 driver=true, Profiler OFF로 복원했다. 추가 Play·테스트·빌드·실행파일·push는 없다.
- 계획·GitHub 조사·실패 원본 및 native 캡처는 기록으로 보존한다. Git 이력을 reset하지 않았다. A/B는 자동 재개하지 않는다.

최종 Editor 상태: [rollback_state.json](rollback_state.json). 정상30R·장시간/GPU·잔류 메모리의 기존 미완료는 이전 런타임 검증 기록을 따른다.
