# 뷰 동기화 후보 검토

2026-10-24. Task1+2만 검토. 전체 미커밋 변경이 아니라 이번 두 제품 파일과 신규 테스트·계측을 대상으로 했다. 독립 소스 검토자는 Unity를 조작하지 않았다. 첫 검토가 사용량 제한으로 중단된 뒤 같은 검토자가 나머지 한 번의 검토를 마쳤다.

## Important 수정 및 검증

`Enemy/Tower → Present`는 보행/원본 rig 자식을 SetActive할 수 있다. 이 자식의 OnEnable/OnDisable이 Grid나 모델을 바꾸면 aura 자신의 활성 상태가 그대로여도, Present 전 currentWorld를 aura에 재사용하는 것은 기존 동작과 달라진다.

- 별도 회귀 `EnemyPresentCallbackRecomputesAnAlreadyActiveAuraPosition`: 잘못된 후보에서 위치 오차 3.605551로 실패(RED).
- aura 생성/배치는 원래처럼 Present 뒤 World를 다시 읽도록 복구했다. 포탑은 같은 안전한 재사용 범위를 입증하지 못해 원래 좌표 호출을 유지했다. 적 본체→애니메이션 현재 좌표만 재사용한다.
- 이 수정 후 신규5/5 통과. 실제 모델 제거→몸체 풀 반환→같은 ID 새 세대 생성→동일 몸체 대여를 거쳐 첫 보행 상태·위치·이전 tile 초기화를 확인했다.
- 이전 tile은 매번 현재 Grid에서 World로 변환한다. 기존 두 인자 Enemy API는 dt=0에서 불필요한 World 평가를 하지 않도록 helper의 계산 여부 인자로 보존한다.

## 측정/검증 제한에 대한 처리

- warm15에서 previousCollection이 갱신되지 않으므로 첫 표본의 collectionsSincePrevious는 warm 구간을 포함한다. 분석에서는 첫 표본을 제외한 수거 차이와 첫 표본의 불확실한 값을 별도 표기한다. 프레임 자체를 삭제하지 않는다.
- capacity1 recorder와 wall interval의 대응은 gap=1인 경우만 유효하다. 분석 결과에 각 실행의 비단위 gap 수를 기록한다.
- 10월23일 원본과 10월24일 후보 사이에 작업 중단이 있다. 같은 기기·화면·부하/고정 dt 조건이라도 시간에 따른 시스템/Editor 변동을 통제한 인과 실험은 아니다.
- 최초 회귀는 같은 handle의 수동 Release만 검증했으나, 최종에는 실제 풀 경로와 같은 ID 새 세대를 추가로 검증했다. 캐릭터 rig의 동일 위상 sprite/위치/회전/색상/enabled 비교이며 전체 HUD 픽셀 비교·아트 승인으로 확대하지 않는다.

## 검토하지 않은 범위

임시 Session 진단 카운터의 제품 채택, 무관한 미커밋 변경, Task3 조회 캐시 설계, 전체 아트/정상30R/목표기기/GPU 성능은 이번 승인 범위 밖이다. 현재 호출 수·원본JSON·직접 CPU 비용으로만 이번 후보를 판정한다. 기존 ID 관리 자체는 새로 도입한 동작이 아니다.

재검토 에이전트나 추가 실험을 반복하지 않았다. 위 Important의 실행 재현→보강→대상 테스트 통과로 처리했다. 최종 채택 판정은 README가 담당한다.
