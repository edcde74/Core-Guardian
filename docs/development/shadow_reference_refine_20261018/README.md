# 사진 추출 그림자 접점 보정 시안 — 2026-10-18

사용자 요청에 따라 [기준 사진](../shadow_reference_extract_20261018/reference_user_selected.png) 하단 ‘① 공간·명암’을 직접 열고 이전 추출/합성과 대조했다. 본체 뒤쪽에서 오른쪽으로 붙이는 **위치 보정 시안**이다. 사용자 승인·게임 적용 상태가 아니다.

## 이번 변경

- [이전/보정 8종, 동일 Nearest 2배](all_before_after_2x.png), [플레이어 이전/보정 4배](player_before_after_4x.png).
- [사진/이전/보정 플레이어](player_reference_before_after_4x.png), [단일](single_reference_before_after_4x.png), [지원](support_reference_before_after_4x.png), [코어](core_reference_before_after_4x.png). 사진 열은 과거 생성 이미지의 사용자 지정 참고이며 크기를 정규화했다. 사진의 본체를 합성에 사용하지 않았다. 실제 원본 두 열만 같은 캡처의 동일 4배다.
- 이전 배치는 본체 합집합 마스크의 투명 여백을 포함한 밑면 기준이었다. 이번에는 불투명 발·받침의 실제 최하단을 찾고, 그보다 위쪽의 오른쪽 외곽과 사진에서 추출한 오른쪽 노출 그림자의 왼 경계를 대응시켰다. 발/받침을 새로 추가하거나 움직이지 않았다.
- 이미지 좌표(오른쪽+, 아래+) 이동: 창병(-1,-3), 방패병(-9,-4), 군기병(-4,-5), 플레이어(-5,-4), 코어(-2,-5), 단일(-2,-7), 멀티(-5,-8), 지원(-2,-7)px. 이는 실제 크기 1920×1080 캡처에서의 이동량이며 확대판 픽셀 수가 아니다.
- 접점 높이는 병사/플레이어 불투명 최하단에서8px 위, 구조물14px 위를 작업 가정으로 삼았다. 사진의 접촉 경계와 맞추는 정지 시안이며 모든 방향/보행에 유효한 런타임 앵커 규칙으로 확정하지 않았다.
- 경계·폭·크기는 직접 대조한 뒤 이번 패스에서는 기존 추출을 유지했다. 오른쪽 덩어리를 붙이기 위해 **이동만** 바꿨으며 기존 종류별 균등 축척/Nearest/40% 알파 유지, 새 타원·다각형·세부 실루엣·누락 영역 채우기 없음. `{role}_source_shadow.png`는 이전 추출 PNG와 바이트 동일하다. `{role}_registered_shadow.png`는 기존 축척을 적용한 캡처 해상도 리소스다. 위치·원본해시·본체검사는 [registration.json](registration.json), 재현은 [refine.py](refine.py).

## 실제 참조 확인과 검증 범위

- 시작 실제 HEAD `8825248`(인수인계 예상 `705a505` 이후 사운드 인수인계 커밋). `Assets/Packages/ProjectSettings` 추적 변경 없음, 다른 작업 미추적 자료와 `font_tools_vendor` 삭제 표시 보존. 변경 전 복구 기준은8825248이며 빈 체크포인트 커밋은 만들지 않았다.
- 연결 Unity6000.6.2f1/CD ready·Play stopped/paused false·compiling false·컴파일 실패 false. 전투 씬1/root7/dirty false, TimeScale1/captureDelta0을 읽기 조회했다. 테스트 상태는 과거 사운드1/1 completed였으며 이번에 새 테스트를 실행하지 않았다. 종료 재조회도ready/stopped/compiling false·동일 씬1/dirty false.
- 실제 `CoreGuardianPrototype`의 enemyArt3/towerArt3 프리팹과 player 참조, 코어 Sprite 경로를 에디터 API로 확인했다. 플레이어의 root SpriteRenderer는disabled, 자식`2D Sprite`는enabled이며 player.png를 참조한다. 이 상태와 런타임 분할 렌더를 구분한다.
- `CoreGuardianPlayerMotion`이 `PlayerFacing/D0~D7`의 Body/Arm/LeftLeg/RightLeg32개를 로드하는 코드를 읽고 에디터에서32개 경로를 조회했다. [실제32개 분할 PNG 동일2배](player_facing_resources_2x.png)를 직접 열었다. 현행 병사3종 원본과 공통 비교판도 직접 열었다. 새 Play·방향별 보행 검증을 했다는 뜻이 아니다.
- 본체 이미지 입력은 이전 실제 카메라 정지 렌더 `shadow_blunt_preview_20261018/current_actual.png`/본체 마스크를 재사용했다. 이는 D5 수동 정지 Present와 임시 개체 나열의 기존 캡처다. **이번 새 Unity 렌더·정상 전투 화면·정상 키 입력·사람 플레이가 아니다.** Overlay HUD 미포함, 원래캡처1920×1080/63.52941px per world unit, 확대판은 후처리Nearest2배/4배다. 이번 Unity 조회는 읽기 전용이며 임시 개체/카메라/입력/시간 설정을 만들지 않았다.
- 정지 합성 후 불투명 본체 마스크(R>254) 안 RGB 변화8종 모두0px. 투명/반투명 가장자리의 자연스러움, 보스, 보행/방향별 추종/겹침/성능/전체 회귀/최종 아트 승인을 증명하지 않는다. 원래 코어 바닥 청색 표현과 기존 중앙 그림자도 그대로 들어 있다.
- RGB 사진에서 추출한 노출 영역만 사용했다. 알파40%는 재구성이며 몸에 가린 부분은 복구하지 않았다. 같은 사진의 본체와 실제 원본의 외형이 달라 모든 경계를 동시에 맞추는 데 한계가 있다. 새 모양으로 메우지 않고 사용자 외형 판단 대상으로 남긴다.
- 기존27b92d8 포탑 접지 구현 유지. 게임 Assets/씬/프리팹/데이터/사운드/Notion 변경·빌드·실행파일 실행·push 없음.
- 저장 중 공유 Git 인덱스 경합을 확인했다. 이번 시안30개 파일을 경로 지정으로 스테이징한 뒤, 병행 사운드 커밋 `eea49b4`(`Record core damage audio listening candidates`)가 그 파일들도 포함했다. 해당 커밋의 실제stat으로 확인했다. 병행 이력은 되돌리거나 재작성하지 않았으며 후속 커밋은 이 기록과 시안 재현 코드/JSON의 LF 줄바꿈 정리만 경로 지정으로 저장한다. 이번 대화에서 사운드 내용을 편집하지 않았다.

사용자가 접점만 조금 더 보완하라고 요청해 [접점 미세 보완 시안](contact_polish/README.md)을 후속 제작했다. 현재 검토 대상은 그 비교판이며 여전히 게임 미적용이다. 이 문서의 위치 보정은 직전 단계 기록이다.
