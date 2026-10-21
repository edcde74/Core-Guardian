# HUD 갱신 버퍼·참조 재사용

2026-10-21 Asia/Seoul. 복구 기준 `b8ddb2f`. 사용자가 제시된 세 항목(피해 집계 정렬/서명 할당, 능력치 HUD 참조 캐시, 같은 값의 불필요한 갱신 감소)에 진행 승인했다. 기존 실제 플레이·자동 테스트·성능 측정 보류 지시는 유지한다.

## 반영

|소스|변경|
|---|---|
|`Assets/Scripts/CoreGuardianHud.cs`|피해 집계의 LINQ 정렬 배열·`string.Join` 서명을 재사용 List와 정렬된 key/value 비교로 대체. 이전 화면 상태도 List로 보관. 피해 행·막대·이름·아이콘은 달라질 때만 대입|
|`Assets/Scripts/CoreGuardianHud.Abilities.cs`|능력치 셀의 RectTransform/Text/Image와 구분선 참조를 생성 시 저장. 갱신마다 만들던 키/값 배열과 Find/GetComponent 탐색 제거. 숫자 입력·지원 여부·숫자 형식이 같으면 문자열 서식을 다시 만들지 않음|
|`Assets/Scripts/CoreGuardianHud.Inspector.cs`|고정 탭 배치·눌림 origin·철거 버튼 label 크기를 생성 시 설정. 단계/접기/개발 도구/설치 취소/포탑 심볼 참조 보관. 활성화·좌표·크기·텍스트는 같은 값이면 대입 생략|

기존 .2초 및 이벤트 갱신 시점, 피해량 내림차순/동률 소스명 순서, 최대 피해 대비 막대 비율, 스크롤·접기·모달·포탑 선택/강화 구매 조건, 지원 포탑 전용 항목과 수치 표시는 유지하는 코드다. 피해 비교에는 원래 dictionary 열거 순번을 마지막 키로 넣어 문화권 비교가 같은 별도 문자열의 안정 정렬도 보존한다. 숫자 표시 형식의 소수 구분자·부호·NaN/무한대 표기가 바뀌면 능력치 캐시를 다시 서식화한다. 피해 정수의 부호 표기 변경도 다시 갱신한다.

지원 포탑 전환/라운드 집계 초기화/행 수 감소·증가·순위 교체는 매 갱신에 상태를 비교한다. 정렬 목록과 셀/위젯 참조는 HUD 인스턴스 수명 동안 보관하며 최대 용량까지 메모리를 유지한다. 전체 HUD의 무할당/Canvas 재빌드0을 주장하지 않는다. 기존 callback snapshot(`refresh.ToArray`), 일부 상태/강화 설명 문자열, 증강 모달의 버튼 조회, 클릭 시 탐색 등은 그대로다. 이 작업은 그 경로의 비용 절감을 입증하지 않는다.

## 실제 확인과 한계

- 수정 제품 소스3개와 기록의 `git diff --check` 통과.
- 독립 읽기 전용 코드 리뷰: Build 초기 Refresh 순서, 참조 캐시, 안정 정렬/문화권, 빈 집계/리셋, 숫자 캐시, 탭 눌림 origin에서 수정 필요 사항 보고 없음. 실행 확인은 아니다.
- 공식 Unity MCP `recompile(focus=false)`: completed, failed=false, errors=[], compilationFailed=false. 컴파일 후 Console ground truth 오류0/경고2. 해당 경고는 기존 `CoreGuardianCombatArt`의 CS0618 2건이며 변경한 HUD 소스의 경고가 아니다. [기록](verification.json), [경고 원본](compile_warnings.json), [소스 해시](implementation_hashes.json).
- 전투씬1, dirty=true(수정 전부터 사용자 상태), Play stopped/컴파일·domainReload=false, 실행 중 Unity 테스트 없음. 씬/프리팹 저장·전환·설정 변경 없이 소스만 수정했다. [종료 조회](final_state.json).
- TimeManager/PackageManagerSettings/ProjectSettings 3파일 SHA256이 기존 본체 공유 종료 기록과 일치했다. 사용자 설정2파일 변경·미추적 자료·폰트 도구 접근오류 표시는 보존했다. 새 Play·테스트·Profiler·빌드·실행파일·push는 수행하지 않았다.

|전후 지표|변경 전|변경 후|절감량/율·판정|
|---|---|---|---|
|최소/최대/평균 FPS·프레임 ms·CPU/GC|미측정|미측정|N/A, 측정 보류|
|Canvas/Layout 비용·할당/피크/잔류 메모리|미측정|미측정|N/A, 측정 보류|
|화면/스크롤/탭/접기·클릭/지원 전환 동등성|기존 자료만 존재|이번 실행 미검증|검사 보류|

나중에 검사를 재개하면 피해 순위 동률/교체·0행/집계 리셋/재등장 행, 지원↔공격 포탑·구매↔조회, 숫자/문화권 변경, 눌린 탭·접기·개발 메뉴·증강 모달을 우선한다. [.2초 갱신 및 실제 동작 QA](../TARGETED_QA.md)와 [동일 조건 전후 계측](../OPTIMIZATION_RULES.md)을 따른다. 기존 마스크 성과나 이전 컴파일 결과를 이번 성능/동등성 통과로 사용하지 않는다.

관련 구현은 [표시/Session 재사용](../runtime_reuse_20261021/README.md), [적 본체 풀](../enemy_body_pool_20261021/README.md), 최신 승인·다음 행동은 [인수인계](../UNITY_HANDOVER.md)를 따른다.
