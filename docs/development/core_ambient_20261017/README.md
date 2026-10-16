# 코어 전용 발광·움직이는 입자 적용

2026-10-17 (Asia/Seoul). 사용자 지시: 전체 화면 대신 코어에만 먼저 적용. 시각 조합 시안과 달리 카메라 후처리/Bloom·환경 조명·경로 재질은 변경하지 않았다. 코어 원본을 보존하는 별도 국소 셰이더와 실제 ParticleSystem을 런타임에 연결했다.

## 현재 구현

- `CoreGuardianCombatArt.Configure`에서 실제 `core` 스프라이트에 `CoreGuardianCoreAmbient`를 연결한다. 씬/프리팹 저장 변경 없이 다음 Play부터 자동 생성된다.
- 수정의 청록 픽셀만 마스크로 사용한 추가 발광과 3픽셀 주변 샘플의 약한 번짐. 강도0.34±0.06, 주기3초. 별도 메시 레이어이며 원본 sprite/material/property block/Transform/그림자는 교체하지 않는다. HDR·전체 Bloom을 켜지 않는 국소 표현이다. 시안 C의 바닥 전체 조명은 적용하지 않았다.
- 사각 청록 입자: 초당4개, 수명1.4~2.1초, 최대12개, 크기0.025~0.045월드단위, Y+속도0.18~0.3. 생성·상승·옆 방향의 작은 편차·투명도 감쇠 후 소멸을 반복한다. 게임 시간/배속을 따르므로 일시정지 때 멈춘다.
- 실제 코어 피해 알림에서 기존 B안 호출 뒤 새 효과를0.3초간 숨기고 입자를 비운다. 기존 흰색→붉은색·HUD 반응과 사용자 확정 숫자 위치는 그대로다. 몸 피격0.09초·전투/재화 수치·다른 유닛 아트는 변경하지 않았다.
- 비활성/파괴 때 소유 리소스를 정리한다. 씬 교체에서 코어가 소유자보다 먼저 파괴되는 순서도 처리한다.

## 직접 확인한 검증

중단 전 같은 대화의 실제 도구 결과:

- 최초 새 EditMode1/1실패(미구현) 후 새 검사와 기존 코어 피격2/2통과.
- 씬 종료 순서의 MissingReferenceException을 재현한 추가 검사1/1실패 후 수정. 이후 전체 EditMode104/104통과. 이 결과는 토큰 제한 전 실행이며 이번 재개에서 새로104건을 실행했다고 표현하지 않는다.
- 입자 속도 축의 MinMaxCurve 형식 불일치 오류를 자체 Play에서 발견해 Z축도 TwoConstants로 수정했다.
- 실제 씬의 새 PlayMode `CoreGuardianCoreAmbientPlayModeTests`1/1통과: 동일 입자의 Y상승/잔여수명 감소, 피해 때 숨김/0.35초 후 복귀, 기존 몸 재질 복귀, 카메라 HDR/후처리 보존.
- 전체 PlayMode 최초 실행은 오류 후 취소했으며 재시도들은0건 반환이라 통과로 세지 않는다. 전체 PlayMode 회귀 완료로 주장하지 않는다.

토큰 제한 후 재개:

- Unity CLI1.0.0-beta.13/CD/Unity6000.6.2f1/pid13296/port7801. 시작 전 전투 씬active/dirty=false·Play=false·compiling/failed=false·TimeScale1/captureDelta0 확인.
- 중간 이미 실행 중인 공유 Play를 발견해 중단하지 않았다. 이후 stopped 재확인 후 자체 Play로32프레임 캡처 성공, 자체 Play 종료.
- [실제 움직임 GIF](core_ambient_live.gif):1920×1080 실제 카메라 렌더의400×400 영역을 원래 배율로 잘랐다. 임시 줌·시간/입력 주입 없음. ScreenSpaceOverlay HUD를 포함하는 캡처가 아니다. 약2.91초/32프레임,31개 프레임 전환 모두 변화 확인. 프레임 시간과 입자 수는 animation_capture.json, 인코딩 검사는 gif_check.json. 기존 정지 시안의 합성 애니메이션이 아니다.
- 캡처 종료 시 cameraTarget=null/HDR=false/post=false/Volume0,dirty=false/TimeScale1/captureDelta0 확인. 자체 Play 종료 후 현재 콘솔 오류0·경고2(기존 CombatArt FindObjectsSortMode obsolete) 확인.
- 그 뒤 `run_tests` 코어 EditMode 재검사 호출은30000ms timeout이었다. 실행기 로그에 Play 중 SceneManager 복구 예외가 있어 새 통과로 기록하지 않는다. 원본 응답: editmode_focused_after_resume.json. 이 후의 최종 에디터 응답 여부는 아래 기록을 따른다.
- 최종 `editor_status`와 `test_status`도 각각30000ms timeout이었다. 따라서 최종 Play/실행기/콘솔 상태는 미확인이다. 마지막 확인된 정상 상태는 위 캡처 직후 자체 Play 종료·오류0 시점이며, 이를 최종 상태로 확대하지 않는다. 강제 종료·사용자 Play 중단·씬 저장·패키지 복구는 하지 않았다. 다음 작업은 에디터 연결과 실행기 상태를 먼저 확인한다.

## 보존과 남은 확인

- 저장 전투 씬 SHA256은 작업 전후 `57D5EAFBE20D99627970F8CF1EA07B95DA34FEA48B2E53AB79BC0DB6CBFE3A65`로 동일하다. 삭제 씬을 복원하지 않았다. Packages/ProjectSettings 변경 없음.
- 시작 복구 기준7c0dd78. 병행 채팅의 HUD 코드/시안/커밋이 추가됐으며 보존했다. 재개 HEAD2014b38. 코어 작업 커밋에는 `CoreGuardianHud.cs`의 별도 미커밋 수정은 포함하지 않는다.
- 기능 검증과 최종 광량·입자 크기·자연스러움 승인은 별도다. 사람 직접 플레이/다수 전투 성능 계측을 수행한 것으로 표현하지 않는다. 전체 PlayMode 회귀와 현재 테스트 실행기 문제는 남은 확인이다.
- 빌드·실행파일 실행·GitHub push·Notion 수정 없음.

관련: [코어 피해B 담당 기록](../core_damage_feedback_20261016/applied_B.md), [사용자 확정 숫자 위치](../core_damage_feedback_20261016/user_position.md), [적용 전4안](../visual_options_20261017/README.md).
