# 현재 자료 찾기

프로젝트 기본 경로: `C:/Users/edcde/OneDrive/Desktop/CD`. 다른 경로가 주어지면 해당 프로젝트의 인수인계를 기준으로 찾는다. 아래 주소는 문서 담당 범위를 안내하며 실행 전에 최신 페이지를 fetch한다. 필요한 하위 페이지·원본 데이터만 따라 읽는다. fetch의 잘림·누락 블록과 접근 실패를 확인하고 마지막 편집 시각을 보존한다.

| 담당 | 노션 페이지 ID |
|---|---|
| 루트 | 3df389dbfc6881bc858bce200d1bb742 |
| 게임 루프 | 3e6389dbfc68819fbfe5ca17f14460d3 |
| 웨이브 편성 | 3e6389dbfc6881c5aa2bed3c32047474 |
| 웨이브 규칙 | 3e6389dbfc68815db2e7e95be4598092 |
| 적/보스 | 3df389dbfc6881ea9f5ec4e0f7b8084c |
| 플레이어 전투 | 3df389dbfc6881f8be26ff810ba7c18e |
| 플레이어 성장 | 3e6389dbfc68811283ffe9b73d0f0da8 |
| 타워 | 3df389dbfc6881a2b037fbce63f2f534 |
| 재화 | 3e3389dbfc688160a9b7efcc83505cb3 |
| 증강 | 3e6389dbfc6881bf9600d5f42fb68a44 |
| 피해 계산 | 3ea389dbfc6881919546c09d4ae7ade6 |
| 레벨/배치 | 3e9389dbfc688159ae8cec214b4e3e25 |
| 플레이테스트 | 3df389dbfc6881fc9f1bd004adfc5999 |

URL: `https://www.notion.so/<ID>`. 검색은 코어 가디언 루트 아래를 우선하고 디멘션 디펜스 자료와 섞지 않는다. 문서에 다른 시스템의 상세 규칙을 복사하지 않고 담당 페이지를 연결한다.

실행 경로 탐색 출발점:

- `Assets/Scripts/CoreGuardianPrototype.cs`: 씬 CSV 참조와 보드/경로, Session 연결.
- `Assets/Game/Scripts/CoreGuardian/CombatData.cs`: 데이터 로딩, 비용, 피해 계산.
- `Assets/Game/Scripts/CoreGuardian/Session.cs`: 라운드, 사거리·타깃·버프·투사체·구매 처리.
- `Assets/Resources/CoreGuardian/{enemies,towers,stage,upgrades,waves}.csv`: 후보 실행 데이터. 씬 참조를 확인한 뒤 근거로 사용.
- 인수인계 링크의 날짜별 계측 JSON/실행기: 방법과 정상 구매·전투 사용 여부를 함께 확인. 강제 피해 검사는 별도 분류.

분석 산출물은 `docs/development/balance_analysis_<date>/` 같은 Assets 밖의 새 경로에 저장한다. 기존 씬·프리팹·원본 및 결과를 보존한다.
