# 코어 가디언 Unity 인수인계

최종 갱신: 2026-10-17 (Asia/Seoul). 현재 사용자 지시와 최신 담당 노션이 우선이다. 새 대화용 복사 텍스트: [resume_20261016.md](resume_20261016.md).

## 재개 시 먼저 확인

- 최신 게임 변경 커밋: `edb7a28`. 문서 커밋이나 병행 작업이 추가될 수 있으므로 실제 HEAD/변경을 확인한다. 사용자·다른 채팅의 변경을 덮어쓰거나 되돌리지 않는다. [로컬 Git 규칙](LOCAL_GIT.md).
- 최신 **10월17일 패키지 복구 후 확인**: CD/Unity6000.6.2f1 연결ready(pid11208/port7800),전투 씬active·dirty=false,Play/paused/compiling/compilationFailed=false,TimeScale1/captureDelta0. 콘솔오류0/경고0. 빈 패키지 캐시68개를 동일 버전으로 복원하고 기존 Bee/ScriptAssemblies/ScriptMapper는 Library/Recovery_20261017_0034에 보존했다. 게임 코드/manifest/lockfile 변경 없음. [복구 근거·절차](package_recovery_20261017/README.md).
- 마지막 게임 수정 검증과 별도로 **이번 복구 후 EditMode102/102·PlayMode19/19 completed**를 다시 확인했다. 최초 비동기 EditMode는 실행기 씬복구 예외로 중단돼 통과로 세지 않고 종료 확인 후 동기로 재실행했다. 사용자 Play 강제 중단/프로세스 강제 종료/게임 빌드/실행/Notion 변경 없음. 기존 [데이터·계측 전제 점검](balance_preflight_20261016_2203/README.md)은 보존한다.
- 원본 기반 애니메이션의 외형/자연스러움 최종 사용자 검토가 남는다. 새 피드백을 먼저 반영하고, 확인 이후에는 기존 밸런스 실측 우선순위를 유지한다. 다음 제작 항목을 임의 확정하지 않는다.

## 현재 애니메이션·그림자·체력바

- 플레이어: 기존 `player.png` 도트를 분리/재배치한8방향, 평상시 내려놓은 팔 스윙·실제 타깃 조준·이동 양발 교대·정지 착지 블렌드. 공격 자세와 도구 끝 투사체 원점은 실제 발사 이벤트에 연결한다. [기본 제작](player_facing_20261016/README.md), [보행 수정](player_walk_polish_20261016/README.md).
- D2 상행 상체: 배낭에 잘못 섞인 맨팔과 앞 얼굴 잔여를 정리했다. [상체 제작/실제 W 검증](player_up_repair_20261016/README.md).
- D2 상행 하체: 옆모습 양다리 단순 반전 대신 원본 바지/부츠로 나란한 무릎·뒤꿈치/같은 밑창 높이를 제작했다. D2 고관절 기준과 다리 회전±1.5° 적용, 다른7방향 보존. 실제 씬 W 입력 전후각36프레임/X0/Y+0.6 확인. [현재 하체/영상](player_rear_lowerbody_20261016/README.md).
- **전체 플레이어 리소스 재생성 순서:** `player_walk_polish_20261016/BuildSourceFacings.Main` → `player_up_repair_20261016/BuildRearPlayer.Main(true)` → `player_rear_lowerbody_20261016/BuildRearLowerBody.Main(true)`. 마지막 후처리 누락 시 잘못된 옆모습 하체로 복귀한다. 과거 빌더/영상은 이력이며 현재 승인 외형으로 가정하지 않는다.
- 몬스터: 사용자가 확인한 X축 원본 관절 보행 보존. Y+ 뒷방향/Y- 앞방향 파생4프레임, 공격형 양발 교대·상행 정강이 전체 폭 유지. [Y축 방향](vertical_facing_20261016/README.md), [양발 교대](vertical_gait_fix_20261016/README.md), [상행 폭](rear_leg_width_20261016/README.md), [원본 접지](foot_contact_20261016/README.md). 파생 아트 최종 승인은 별도다.
- 몬스터와 플레이어 모두 정지 발을 복제한 `바닥 밀착선`만 애니메이션 연결 시 비활성화했다. 중앙 타원 두 레이어는 하나의 실루엣으로 유지한다. 원본 PNG/프리팹/씬은 보존한다. [몬스터 전후](monster_single_shadow_20261016/README.md).
- 체력바: 공통 높이 대신 병종별 원본/상하 프레임 상단+4px 여백. 창/깃발과 겹치지 않게 포함하고 보행 중 높이를 고정한다. 기존 색·폭/두께·HP비율 유지. [최신 그림자/체력바·실제 씬 캡처·102/19 검증](health_shadow_20261016/README.md).
- 포탑 원본 외형/공격과 승인 보스 안광V3 보존. 몬스터 검토 전 [MONSTER_ART_STYLE.md](../art/MONSTER_ART_STYLE.md)와 현재 병사3종 원본/동일 배율 비교판을 직접 확인한다. 단순 팔레트/반전 일치나 검사 수치만으로 자연스러움을 승인하지 않는다.

## 병행 VFX·기획 보존

- **10월16~17일 카메라 후처리 비교 완료·게임 미적용:** 현재 URP/Linear/HDR지원,원래 카메라HDRfalse·후처리false·Volume0를 확인했다. 임시 씬·카메라·Volume으로 색감/명암 정돈(우선추천),약한Bloom,강한 분위기 비교를 실제 URP로 캡처하고 원래 상태를 보존했다. 최종sRGB 출력/인터랙티브2안/코어확대/브라우저오류0 확인. 저장씬 배치 아트 비교이며 전투HUD/실제 피해숫자/성능 검증은 남아 있다. 수정 부위 한정HDR발광은 별도 후보,흐림·색수차·그레인은 기본 제외 제안이다. 선택·영구 적용으로 간주하지 않는다. [비교·현재값·남은 검증](postprocess_study_20261016/README.md).

- 몸 피격: 0.09초 복귀/최대22%/재점등 제한0.1초. 기존 파편/숫자 유지, 새 파편/숫자 반응 미채택. [담당 기록](body_hit_flash_20261016/duration_90ms.md).
- 코어 피해 B안은 **이미 적용**됐다. 짧은 흰색→붉은색 본체·상단 코어 표시 반응·추가 플래시300ms 제한. 코어 감소 숫자는 사용자가 위치를 확정했다: 본체 bounds.max 기준(-0.3,-0.38), 빨간색/Pixelify Sans Bold. [적용](core_damage_feedback_20261016/applied_B.md), [사용자 확정 위치](core_damage_feedback_20261016/user_position.md). 과거 미구현 제안으로 되돌리지 않는다.
- 평상시 효과는 작게, 보스 가속/코어 피해는 강조한다. 사운드는 별도 파트. 기본 공격에 둔화는 없으며 골드 플레이어 전투 증강의 조건부 기능이다. 둔화 시안은 보류다.
- 기존 담당 Notion 문서/DB와 이미지·하위 링크 보존. [기획 동기화](notion_sync_20261016/report.md), [VFX/사운드 분류](vfx_notion_20261016/report.md). 이번 애니메이션/문서 작업은 Notion 변경을 수행하지 않았다. 노션의 세부 최신화 여부는 필요 시 실제 담당 문서와 대조한다.
- HUD/설치/폰트/증강의 기존 승인 범위는 [정리 전 전체 기록](unity_handover_snapshot_20261016_before_animation_resume.md)과 담당 링크에 보존했다. 이번 작업으로 UI 재디자인/전투 수치 변경을 승인받은 것이 아니다.

## 밸런스 다음 순서

- 사람 직접 테스트용 현재 후보: 공격형 HP27 전 라운드 공용, 1R15마리/간격1.62초, 2R16개 개별 슬롯/간격1.53초·마지막22.95초. 최종 확정이 아니다. 재개 시 CSV→씬 참조→CombatData.Load를 확인한다.
- 이번 재개에서 Notion 플레이테스트와 로컬 기존 자료를 조회했으나 새 사람 직접 테스트 기록은 없었다. 사용자는 추가 애니메이션 피드백/직접 결과를 나중에 전달하겠다고 했다. 피드백 대기 중이며 새 Play/실측은 실행하지 않았다. **새 결과부터 확인**한다.
- `core-guardian-balance` 스킬과 [next_work_20261013.md](next_work_20261013.md)를 따른다. 1~2R 압박→3~9R 성장/실패→10R 보스 처치/도착→11R 증강 후 성능. 시간·코어 손실·피해 기여·재화 수지/배치·구매·증강·배속을 보존한다.
- `MeasureNaturalRounds.cs`의 과거 HP25/6R총HP555/간격0.9배 변형/과거 출력 경로와 증강 기록 누락을 확인했다. 이번 실제 씬 참조→CombatData.Load에서6R총HP581,1~11R수·HP의 독립CSV대조11/11일치. 아직 계측기를 수정/실행하지 않았으며 다음에는 무변형·새 출력·보스/증강 기록을 먼저 갖춘다. [확인 수치·주의점·검증 후보](balance_preflight_20261016_2203/README.md).
- 정상 구매·이동·공격 계측, 정적 계산, 사람 직접 플레이, 강제 처치/재화 주입 QA를 구분한다. 강한 증강 빌드 자체를 결함으로 보지 않는다. 밸런스/Notion 수치는 임의 변경하지 말고 측정 결과와 변경 후보/근거부터 제시한다.

## 작업·배포 규칙

- Unity 변경 전에 연결/씬/Play/컴파일/저장 상태 확인. 사용자 Play/다른 검사를 임의 중단하지 않고 공유 Unity 테스트는 겹치지 않는다. 씬 YAML/메타 직접 수정 금지, CLI/에디터 API 사용. `BuildCombatPrototype.Main`은 씬 재생성기라 임의 재실행하지 않는다.
- 실제 정상 입력의 이동→전환→정지와 원본 비율을 확인한다. Preview/실제 씬 카메라/사람 플레이, 정지 포즈/움직임, 화면 HUD 포함 여부, 임시 카메라·주입·비교 위상을 구분해 보고한다. 자체 입력 장치/카메라/시간/Play는 복원한다.
- 기존 자료/원본 프리팹/SampleScene/아트 비교 씬·AudioProduction 등 다른 작업 보존. 경로 `docs/development/font_tools_vendor`는 접근 문제가 있어 광범위 검사에서 발생한 삭제 표시를 실제 사용자 삭제나 정리 대상으로 단정하지 않는다.
- 빌드와 실행파일 실행은 사용자 명시 요청 때만. 기존 `Builds/Windows_20261015_PlacementFocus/CoreGuardian.exe`는 최신 방향/그림자/체력바 수정이 반영되지 않았다. 현재 Unity 씬으로 확인한다. GitHub push 금지.
- 완료 시 현재 구현/최근 확인/남은 판단/담당 링크를 갱신하고 로컬 커밋한다. 같은 과거 상태를 계속 덧붙이지 않는다.

## 과거 상세 이력

[이번 정리 전 전체 인수인계](unity_handover_snapshot_20261016_before_animation_resume.md), [이전 재개 텍스트](resume_snapshot_20261016_before_animation_resume.md), [10월13일 스냅샷](unity_handover_snapshot_20261013_before_resume.md). 이력의 과거 미구현/실패/에디터 상태를 현재 상태로 취급하지 않는다.
