# 재개 점검 — 남은 검증과 후속 범위

2026-10-21 Asia/Seoul. 사용자 요청은 기준 `9f68e21`에서 재개해 남은 검증·후속 최적화 범위를 먼저 확인하는 것이다. 이번 작업은 문서/Git 대조와 Unity 읽기 조회만 수행했다. 새 Play·테스트 실행·계측·설정 변경·빌드·실행파일 실행·push는 없다.

## 현재 확인
- HEAD `9f68e21`. 게임 소스의 기준 대비 변경 없음. 기존 설정 차이는 PackageManagerSettings의 entity ID/loadAssets, ProjectSettings의 Standalone `APP_UI_EDITOR_ONLY`, TimeManager의 유리수 직렬화다. 기존 미추적 자료와 폰트 도구 접근오류/삭제 표시는 복원·삭제·스테이징하지 않는다. Git 소유자 SID가 달라 전역 설정 대신 명령별 `-c safe.directory=...`로 읽었다.
- MCP 연결 프로젝트 CD, Unity6000.6.2f1. 전투씬 하나 loaded/active, dirty=false. Play stopped, paused=false, compiling/updating=false, Profiler/Deep=false. 테스트 상태는 이전 완료 total=0, 빌드는 idle. 상태 조회는 다른 채팅의 작업 예약 여부까지 증명하지 않으므로 실제 검사 직전에 다시 확인한다.
- `list_tests(mode=playmode)`는 전체53건, `CoreGuardianAnimationPlayModeTests`6건을 반환했다. 모두 `Fortress.Game.PlayModeTests`, Explicit=false다. 기존 [실행0건](play_animation.json)의 원인은 미확정이다. 목록 존재는 실행 성공이 아니다.
- Frame Debugger 상태를 읽는 eval은 두 namespace/API의 CS0234로 실패했다. 제품 컴파일 실패가 아니며 프로젝트 파일은 변경하지 않았다. 해당 항목을 제외한 상태 조회는 성공했다. Frame Debugger의 현재 활성 여부는 미확인이다.
- [Notion 계획](https://www.notion.so/3ff389dbfc6881779abdeb5eeeb1cda4)을 재조회했다. 마지막 편집2026-10-20T13:04:58.690Z, 상태는 시작 전이며 로컬 1차 구현보다 앞선 내용이다. 이번에 수정하지 않았다. 구현·성과는 [현재 기록](README.md)과 [원시 수치/한계](RESULTS.md)가 담당한다.

## 먼저 마칠 검증
1. **보행 PlayMode 6건 실행 문제**: 현재 `run_tests` 스키마는 mode=playmode, filter_type=testName/assembly/category, filter는 부분 일치다. 테스트 실행기/실제 필터 적용 경로를 읽어 기존0건의 원인을 좁힌 뒤 클래스 필터 또는 명시된 한 메서드로 유효 실행을 확인한다. 원인을 바꾸지 않은 반복은 하지 않는다. 계획 기대 수는 클래스6건(한 메서드 진단 시1건)이다. 0건이면 미검증으로 남긴다.
2. **RT 최대·장기 잔류**: 같은 적/포탑·경로/카메라 조건에서 요구 크기와 보유 용량, RT 생성/해제, 피크·안정 구간·개체 퇴장/재시작 후 잔류를 기록한다. 관측 길이/반복은 실행 전에 고정한다. 과거 서로 다른 자세의 종료 RT 합계로 절감률을 계산하지 않는다.
3. **카메라·해상도·출력 동등성**: 확대→축소, 해상도 변경, 화면 가장자리, 방향/보행/피격/강화/선택과 앞뒤 중첩을 같은 위상으로 대조한다. 접촉 밴드·1/2px 외곽선·UV/clear 경계도 포함한다. 과거 고립576건은 전체 게임/모든 해상도 검증을 대신하지 않는다.

실행 전 [대상 QA](../TARGETED_QA.md)의 사용 중 상태·테스트 필터·임시 상태 보존을 적용한다. 현재 설정 자동 재추가/재직렬화가 보이면 다음 실행을 멈추고 [기존 한계](settings_followup.json)와 비교한다. 이미 완료된 EditMode176건·선택 PlayMode4건을 새 변경/실패 근거 없이 전부 반복하지 않는다. 정상30R·사람 체감/청취·standalone 검증은 위 항목과 구분한다.

## 후속 최적화 경계
|순서|후보|범위 결정 전 확인할 것|
|---|---|---|
|다음 구현 후보|동일 본체·카메라·합성 조건의 실루엣 공유, 소비자별 출력 유지|현재 재사용 버전의 중복 합성 비용 재계측, pose/alpha/threshold/padding·소비 순서·소유권 계약|
|공유 검증 이후 후보|마스크 아틀라스/명령 통합|공유 효과와 분리, tile 경계·이주·정렬·대형 RT 메모리|
|별도 실험|표시 오브젝트 풀·피해 HUD 행 재사용|표시 수/수명·이벤트/소리·정렬·접힘 집계 보존, 초기 준비/확장/잔류 비용|
|별도 실험|Session 버프 공급자 목록·재사용 버퍼·top3|동일 seed/dt/입력의 중간 타깃·이벤트 순서와 결과 대조|

후속 구현은 이번 범위 확인으로 승인된 것으로 처리하지 않는다. 구체 설계/조사 근거는 [해결방안 정본](../optimization_solutions_research_20261020.md)을 따른다. 새 성능 수치/개선 보장은 없다. 목표기기·해상도·배속·최대 부하/FPS 예산은 아직 미확정이며60FPS를 합의/달성으로 기록하지 않는다. 전후 비용·최소/최대/평균FPS·원본·품질 판정은 [최적화 규칙](../OPTIMIZATION_RULES.md)이 담당한다.
