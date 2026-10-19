# 밸런싱·Unity 공식 AI 스킬 설치와 적용

2026-10-19. 사용자 요청으로 공개 GitHub 자료를 조사하고 밸런스 스킬과 Unity 공식 에이전트 플러그인을 실제 설치했다. 시작 HEAD는 `ac08d17`이다. 기존 전투 결과·CSV·씬·사운드·아트·HUD와 미커밋/미추적 자료는 보존했다.

## 설치한 항목

|항목|출처·고정 커밋|설치 위치·상태|용도|
|---|---|---|---|
|headless-game-balance|[nitzangames/headless-game-balance](https://github.com/nitzangames/headless-game-balance), `0b0a759c5ad446d5d677da52721d2ce6203b503d`, MIT|`C:/Users/edcde/.codex/skills/headless-game-balance`, 원본5파일 일치·형식 검사 통과|실제 게임 로직 재사용, 정책별 대응 시드, 분포·실패/꼬리 위험·수지 기록|
|Unity 공식 에이전트 플러그인|[Unity-Technologies/unity-agent-plugin](https://github.com/Unity-Technologies/unity-agent-plugin), `cf6b2da24e424b0a60d560a57f39f676cb6f79f3`, Unity Companion License|`unity@unity-agent-plugin` `0.1.8-beta`, installed=true/enabled=true. `C:/Users/edcde/.codex/plugins/cache/unity-agent-plugin/unity/0.1.8-beta`|Unity CLI·UI·물리·아트/최적화 등32스킬. 기존 Pipeline 연결을 활용|

Codex 공식 `skill-installer`로 밸런스 스킬을 설치했고 Unity README의 Codex marketplace/plugin 명령으로 공식 플러그인을 설치했다. 새 스킬의 자동 검색은 다음 사용자 턴부터 가능하다. 이번에는 설치한 원문을 직접 읽고 동봉 도구와 Unity CLI를 실제 사용했다. 이전 개인 `skills/Unity` 폴더는 삭제/덮어쓰기하지 않았다. 공식 플러그인은 별도 `unity:` 이름 공간이다.

Unity CLI는 실제 설치 `1.0.0-beta.13`이며 self-update --check에서 최신 버전도 같았다. 기존 프로젝트는 `com.unity.pipeline 0.7.0-exp.1`로 연결되어 있어 Unity 패키지를 추가하거나 다른 MCP를 중복 연결하지 않았다.

사용자가 언급한 이름은 정확한 URL이 없어 Unity 공식 에이전트 플러그인으로 적용했다. 이름이 비슷한 [IvanMurzak/Unity-MCP](https://github.com/IvanMurzak/Unity-MCP)(Apache-2.0)의 도구/스킬 생성 방식도 확인했지만 별도 MCP 백엔드를 이 프로젝트에 설치한 것은 아니다. 이는 위 공식 플러그인과 서로 다른 제품이다. 다른 밸런싱 후보의 출처/커밋/경로는 `repository_inventory.json`에 기록했고, 명시 라이선스를 확인하지 못한 후보는 재배포하거나 설치하지 않았다.

## 실제 적용

1. 설치한 SKILL.md와 simulation-contract.md, 동봉 summarize_runs.py 전체를 읽었다. GitHub 다운로드는 선언문/참고 문서/요약 도구5파일이며 원본과 설치본을 바이트 비교했다.
2. 기존 8회 정상 전투 JSON은 그대로 두고 `apply_installed_skills.py`가 별도 `derived/` JSONL로 변환했다. 실제 동봉 `summarize_runs.py`를 정책마다 실행해 결과를 저장했다. 의미 없는 전체 혼합 승률을 새 결론으로 사용하지 않았다.
3. 기존 seed는 정상 생성자에서 미지정이었으므로 `seed=null`, `paired_seed_eligible=false`로 명시했다. PRNG 초기 상태 기록을 지정 시드로 꾸미거나 이 자료를 headless 결정론적 시뮬레이션으로 재분류하지 않았다.8조건 각1회는 정책별 관측이며 승률·분위수의 모집단 추정이 아니다.
4. Unity 공식 스킬의 CLI/실행 검증 지침을 읽고 현재 연결 조회와 명령 스키마 검색에 사용했다. Play 진입만으로 실행 검증이라 하지 않고, 후속 실제 측정은 frameCount/게임시계 진행을 증명해야 한다는 기준을 적용한다. 이번 설치 작업에서 새 Play나 검사는 실행하지 않았다.
5. 프로젝트 AGENTS에 기존 전용 밸런스 스킬+headless 스킬·Unity 공식 스킬의 사용 경로를 연결했다. 프로젝트 규칙·최신 Notion·강한 증강 보상의 목표가 외부 스킬의 예제 승률/평준화 목표보다 우선한다.

검증: [application_verification.json](application_verification.json), [plugin_status.json](plugin_status.json), [skill_validation.json](skill_validation.json), [정책별 도구 출력](derived/skill_summaries.json). 스킬33개(공식32+밸런스1)의 기본 YAML/필수 필드를 확인했다. 엄격한 일반 스킬 검사기는26개 통과/7개 Unity 확장 메타데이터 키 미지원이었다.7개는 공식 원본의 `required_packages`/`required_editor_version`/`enabled`/`modes`를 보존했고 기본 필드만 별도 검사했다. 이를 일반 검사기33개 전체 통과나 모든 스킬 기능 실행으로 표현하지 않는다.

## 다음 밸런스 계측 계약

- 질문을 먼저 고정한다. 예: ‘동일한 전투 조건에서 둘째 포탑의 유효 경로 피복 또는10R플레이어 이동이 보스 처치/증강 진입을 바꾸는가?’
- 기존 `MeasureNaturalRounds.cs`와 `Session`을 재사용한다. 독립 계산은 정적 분석으로, 같은 `CombatData.Load`/`Session`을 호출하는 고정 스텝 실행은 시뮬레이션으로, 실제 씬 Update는 정상 전투 계측으로 구분한다. 렌더링 없는 별도 계산을 실제 Play라고 하지 않는다.
- 게임 로직을 재작성하지 않는다. Session에는 이미 선택적 seed 생성자가 있으므로 별도 데이터/Session을 쓰는 분석 시뮬레이션에서 대응 시드를 사용할 수 있다. 실제 씬의 기본 생성자는 seed 미지정이므로, 기존 씬 데이터/Session을 몰래 교체해 ‘정상 초기 실제 Play’라고 하지 않는다. 실제 씬의 시드 주입이 필요한 구현은 별도 사용자 지시 범위에서 다룬다.
- 비교 시 데이터 SHA/실행 코드/정책 버전·배치·구매 시점·난수 시드/추첨·dt·종료 조건을 고정한다. 먼저 소규모 동일 시드 재현성을 확인하고 실패 원인을 읽은 뒤 대조군과 비교한다. 일괄 수백 회 실행으로 사용자의 공유 에디터를 점유하지 않는다.
- 멀티의 위치→지원 적용 대상→플레이어 이동을 각각 나눠 비교한다. 플레이어 공격력/공속·전투 중 즉시 구매도 한 번에 하나씩 바꾼다.
- 메트릭은1~2R 첫 성장/플레이어 기여,3~9R 누수·투자/수지,10R 보스 처치/도착/남은HP·교전 시점,11R 실제 증강 후보/선택/보상과 성능이다. 평균뿐 아니라 개별 실패·시간/손실 분포를 보고한다.
- 강한 증강 빌드는 설계 보상이다. 임의로50%승률/모든 빌드 평준화/최강 빌드 약화를 목표로 삼지 않는다. 사람1배속의 재미·인지/회복 가능성은 별도 검증한다.
- 새 수치 적용·Notion확정·빌드/실행파일·push는 하지 않는다. Unity 실행 전 공유 상태 확인과 임시 변경 복원을 유지한다.

## 재검증

번들 Python으로 `apply_installed_skills.py`를 실행한다. 공식 검사기의 PyYAML6.0.3은 사용자 임시 `codex-skill-validation-20261019` 폴더에만 설치했다. 관리형 샌드박스에서는 그 폴더의 패키지 읽기가 제한되어 정상 import가 되지 않을 수 있으므로, 실제 검증은 해당 폴더를 읽을 수 있는 권한으로 실행했다. 게임 프로젝트에 Python 패키지를 넣지 않았다.

기존 보호 파일1,337개의 해시 변경0, 기존8회 자료의 추가 집계만 실행, 새Unity Play/테스트/게임 수치·씬/음량/Notion변경0이다. 이전 8회 정상 실측과 새 설치/도구 적용을 구분한다.
