# 전체 게임 스크립트 최적화 정적 조사

2026-10-23. HEAD `1de8ced`, 미커밋 작업 트리 기준. 시작107개 + 이번 회귀 테스트1개 = **Assets C#108개, 런타임54 / Editor4 / 테스트50, 12,385줄**. [파일별 범위·SHA256·위험 패턴·직렬화 참조](script_inventory.json), [표 형태 전체 색인](script_inventory.csv), [재현 가능한 정적 조사](audit_scripts.py).

전체 파일 내용을 패턴 스캔하고 런타임은 호출 경로/수명주기/기존 풀·캐시를 분야별로 검토했다. Editor/테스트를 Player 매 프레임 비용으로 합산하지 않았다. 패턴에 걸렸다는 이유만으로 병목이나 수정 필요를 확정하지 않는다. Library/PackageCache의 외부 패키지, 과거 docs 진단 스크립트는 게임 스크립트 전수 범위에서 제외했다. 전체 테스트 실행·새 Deep 캡처는 하지 않았다.

## 현재 비용이 확인된 부분

- **초기 피격 TMP 초기화:** 앞선 Deep에서 피해 숫자3개 생성62.959ms, 포함된 TMP Settings 최초 Resources.Load42.573ms. 이번 수정으로 Configure에서 설정과 숫자3개를 준비했다. 일반 전후 결과는 [실험 정본](README.md). 전투 중 추가 풀 확장은 유지하므로 모든 숫자 생성 비용을 없앤 것이 아니다.
- **강조/마스크:** 이번 after90에서 Emphasis.Present 평균5.611ms, 내부 Mask.Present4.672ms. 중첩이므로 합하지 않는다. 숫자 준비와 별개인 지속 비용이다. 렌더 제출·GPU 시간이 아니라 이 CPU 마커 범위다.
- **넓은 그림자:** 이번 after90 평균0.846ms. 이름 분류 캐시는 이미 적용됐으므로 같은 작업을 다시 제안하지 않는다.
- **HUD 피해 목록 최초 확장:** 앞선 일반 Profiler에서 HUD.Update249,978~426,612B가 확인됐다. 이미 행 재사용 중이며 신규 출처가 생길 때 `CreateDamageRow`가 남는다. TMP 숫자 준비와 다른 경로다.

## 수정 후보와 구체적인 처리 방향

아래 ms·절감률은 **미측정**이다. 우선순위는 실제 확인 비용, 반복 수, 위험도에 따른 다음 조사 순서이며 임의 구현 승인이 아니다.

|순서|현재 코드/발생 상황|제안|보존해야 할 계약|
|---|---|---|---|
|1|[Session.cs:399](../../../Assets/Scripts/Runtime/Domain/Rules/Session.cs#L399) 피해 콜백이 구독돼 있으면 명중마다 [SessionModels.cs:85](../../../Assets/Scripts/Runtime/Domain/Rules/SessionModels.cs#L85)의 List.Remove로 재탐색·배열 이동. 같은 tick에 P발 중 H발 명중하면 최악 O(P×H). HUD/Art/Audio가 실제 구독한다.|명중 전후 관찰 가능한 목록 계약을 유지하면서 삭제 위치/순회를 줄이는 방법을 먼저 작게 검증. 단순 swap-remove로 바꾸지 않는다.|콜백에서 현재 포탄이 보여야 함, 앞선 제거 반영, 생존 순서·중복 멤버십·재진입/예외 보존. 기존 SessionTests263~295,446~497 관련. 위험도 높음.|
|2|[Session.cs:47](../../../Assets/Scripts/Runtime/Domain/Rules/Session.cs#L47) 뷰용 공간 인덱스를 만들지만 각 TowerTargetForView에서 Invalidate→Matches로 적 전체를 다시 확인. FindTower도 타워 전부 stamp 검사, HasTowerSupport도 호출마다 Refresh.|외부 콜백 없는 뷰 동기화 구간에서 한 번 검증한 읽기 전용 결과를 공유. 현재 공격 시뮬레이션의 콜백 무효화는 유지.|다중 타깃 우선순위·동률, 외부 재정렬/변경, 공개 API의 유효성 보장. Dictionary가 있다는 이유로 O(1)이라고 하지 않는다.|
|3|[CoreGuardianPrototype.cs:110](../../../Assets/Scripts/Runtime/Bootstrap/CoreGuardianPrototype.cs#L110), [CombatAnimation.cs:44](../../../Assets/Scripts/Runtime/Presentation/Characters/CoreGuardianCombatAnimation.cs#L44). 적 위치를 World로 여러 번 변환하고 Tilemap/Grid/Transform 네이티브 getter 반복, 정지 타워 위치도 매 프레임 재지정.|같은 동기화 구간의 격자 원점/변환·적 월드 위치를 재사용하고 바뀐 값만 반영.|보드/Transform 변경 시 무효화, 애니메이션·발사 원점·체력바·타깃 위치의 같은 프레임 동기화.|
|4|[Session.cs:358](../../../Assets/Scripts/Runtime/Domain/Rules/Session.cs#L358) 버프 반경은 적×버프원 전수 Distance. 적128/버프16이면 자기 제외 전 후보 검사2048/tick. 저FPS·배속에서는 Tick의 .05초 substep마다 반복.|공간 후보 제한/전용 버프원 인덱스 검토. 기존 공격 인덱스는 이동 뒤 생성되므로 그대로 쓰면 이동 전 aura 계약이 달라진다.|이동 전 위치·원본 source 순서·최대 버프·반경 경계·NaN 처리·동일 전투 결과.|
|5|[CoreGuardianAudioCatalog.cs:14](../../../Assets/Scripts/Runtime/Presentation/Audio/CoreGuardianAudioCatalog.cs#L14), [CombatAudio.cs:89](../../../Assets/Scripts/Runtime/Presentation/Audio/CoreGuardianCombatAudio.cs#L89), [SoundSettings.cs:16](../../../Assets/Scripts/Runtime/Presentation/Audio/CoreGuardianSoundSettings.cs#L16). 최대 전투4+음악2 음량 경로에서 매 프레임 Enum.Parse, enum→문자열 key, PlayerPrefs 조회 반복.|카탈로그에서 enum/key를 1회 해석하고 변경된 음량만 native source에 반영. 필요시 승인된 setter 기반 값 무효화.|음량 조절 즉시 반영·duck/crossfade·무음 상태·기존 PlayerPrefs 호환. 작은 지속 GC 후보이며 최대 CPU 병목으로 확정하지 않음.|
|6|[CoreGuardianAudioCatalog.cs:34](../../../Assets/Scripts/Runtime/Presentation/Audio/CoreGuardianAudioCatalog.cs#L34) ForClip은 모든 cue를 FirstOrDefault로 순회하면서 각 cue.Clip getter를 호출. 앞에서 못 찾은 첫 효과음 때 아직 안 쓴 오디오 Resources.Load가 연쇄될 수 있음.|Clip→cue 역색인 또는 이미 아는 cue를 PlayClip까지 전달. 실제 lazy clip load 시점을 따로 확인.|기존 우선순위·같은 clip 중복 정의 시 첫 일치 정책·보이스 제한/효과음 횟수 유지. 신규 음원 사전 로드로 자동 확대하지 않음.|
|7|[CoreGuardianCombatArt.cs:39](../../../Assets/Scripts/Runtime/Presentation/Combat/CoreGuardianCombatArt.cs#L39) 새/재사용 포탄의 Sprite 선택에서 Towers.FirstOrDefault 안에 "Tower:"+t.Id를 반복 생성.|기존 Session.TowerFromSource/소스ID 해석을 재사용해 불필요한 문자열 조합 제거.|철거 직후 남은 포탄의 기본 SingleBolt fallback, Player 분기·MultiBolt 유지.|
|8|[CoreGuardianHud.cs:448](../../../Assets/Scripts/Runtime/Presentation/UI/CoreGuardianHud.cs#L448) 0.2초마다 refresh.ToArray, 숨은 구매 버튼의 라벨/가격 문자열·LINQ, Selected/Upgrade lookup 반복. Changed 이벤트 때도 실행.|스냅샷 배열 재사용과 변경 감지·표시 상태에 따른 계산 생략. 공개 콜백이 refresh 목록을 변경할 수 있는지 먼저 확인.|숨겨진 패널을 다시 열 때 최신 정보, 버튼 활성·모달·가격·문화권 숫자 형식. 피해 집계는 접어도 지속.|
|9|[CoreGuardianHud.cs:498](../../../Assets/Scripts/Runtime/Presentation/UI/CoreGuardianHud.cs#L498) 새 공격 출처가 등장하면 Text/Image/Layout 행 생성. 이번 TMP 수정 대상 아님.|실제 초기 활성 공격 출처 수만큼 피해 행을 준비하고 레이아웃 최초 확장 비용을 이동하는 별도 작업.|포탑 전체 피해 집계·정렬·최대값 대비 막대·표시 행 수 유지. 무제한 미리 생성 금지.|
|10|[CoreGuardianPlayerMotion.cs:71](../../../Assets/Scripts/Runtime/Presentation/Characters/CoreGuardianPlayerMotion.cs#L71) Present마다 4개 renderer 임시 배열. [OriginalWalk.cs:149](../../../Assets/Scripts/Runtime/Presentation/Characters/CoreGuardianOriginalWalk.cs#L149) 고정 뼈 길이·휴지 자세 외적을 매 다리/프레임 재계산.|Pose가 소유한 배열을 재사용하고 고정 IK 상수만 구성 시 계산.|위상/방향·발 위치·동일 float 연산 결과 픽셀 확인. 플레이어1개 임시 배열의 절감은 작음. 보행 프레임/부품 수 축소 아님.|
|11|[SelectionOutline.cs:80](../../../Assets/Scripts/Runtime/Presentation/Rendering/CoreGuardianSelectionOutline.cs#L80), [MaskProducerBatch.cs:77](../../../Assets/Scripts/Runtime/Presentation/Rendering/CoreGuardianMaskProducerBatch.cs#L77). 모든 가시 개체의 bounds/투영·파트 상태/행렬·mesh/buffer 업로드·공유 페이지→개별RT copyback 지속.|기존 마커로 구조 검증·마스크 생산·업로드·copyback을 구분한 뒤 바뀌지 않은 입력만 생략할 수 있는 경로 검토.|움직이는 원본 보행은 실제로 입력이 변함. 정렬/마스크/픽셀/fallback을 유지해야 하며 공통 텍스처만으로 처리 전체를 공유할 수 없음. A 보류/B 제한 유지.|
|12|[MaskPage.cs:76](../../../Assets/Scripts/Runtime/Presentation/Rendering/CoreGuardianMaskPage.cs#L76) 페이지2048² ARGB32는 개당16MiB, 카메라별 고수위 페이지 보유. 개별 body RT와 생산 mesh/buffer도 함께 있음.|재시작/최대 부하 뒤 잔류·peak를 별도로 확인하고 필요하면 유휴 회수 정책 검토.|지금 누수 확정 아님. 매 프레임 해제하면 재할당/스톨 유발. 원본RT 제거·메모리 스냅샷·새 장기 측정 자동 수행 금지.|
|13|[CoreGuardianAugmentationView.cs:43](../../../Assets/Scripts/Runtime/Presentation/UI/CoreGuardianAugmentationView.cs#L43) 카드 공개 때 최대52+20 spark 각각 GameObject/Image/코루틴 생성 후 파괴, 카드/설치 메뉴 전환도 재구성.|증강 공개 시점 전용으로 기존 개수/수명을 유지한 효과 풀링/코루틴 통합 검토.|전투 상시 비용과 구분. 공개/접기/StopAllCoroutines·입자 수/수명·사운드 타이밍 유지.|

## 이미 적용됐거나 현재 우선순위에서 제외한 것

- 모델/적 본체/포탄뷰/히트·사망 효과/숫자/총구 효과는 이미 풀링한다. 새 풀 시스템을 중복 구현하지 않는다. 풀 수용 한도는 비활성 보유량이며 활성 적·효과 표시 상한이 아니다. 처음 늘어날 때의 생성 비용은 별도로 남는다.
- 마스크 리소스 재사용, 동일 본체 마스크 공유, Source geometry 캐시, 피해 행 재사용/변경 감지, 체력바 fill-state Sprite 캐시, 길/설치 메시 결합도 이미 있다.
- CombatB9종은1텍스처. 반복 표시가 많다는 이유로 CombatB를 다시 패킹해도 기존 draw가 자동 결합되지 않는다.
- 네이티브 Attack 배칭은 draw 감소 근거가 있어도 CPU/GC 및 복원 문제가 있어 **OFF**. 이번 변경/조사로 재활성화하지 않았다.
- BeanProjectile/EnemyController/TowerTargeting/LightningTower는 직렬화된 씬·프리팹·asset 참조0이며 현재 Prototype/Session 경로에서 생성·호출도 찾지 못했다. 이들의 Destroy/GetComponent를 현128마리 병목으로 제시하지 않는다. 파일은 보존한다.
- Editor4개는 명시 bake/import/build hook이다. AssetDatabase/ReadPixels/배열 처리를 전투 매 프레임 비용으로 오인하지 않는다. 에셋 일괄 재import나 bake를 이번 조사에서 실행하지 않았다.
- 테스트50개는 보호해야 하는 동작/회귀 범위 확인 대상이며 게임 비용 합계에서 제외. 이번에는 신규 피해 숫자 테스트1개만 실제 실행했다.

## 다음 작업 제안

작은 변경부터 하려면 **7번 포탄 Sprite 문자열 조회 + 5번 오디오 enum/key 중복**이 범위가 작다. 큰 CPU 절감 후보는 **2~3번 뷰 동기화 중복과 11번 마스크**이고, **1번 포탄 삭제**는 동시 명중이 많을 때 유리할 수 있으나 재진입 계약을 먼저 풀어야 한다. 이 후보들의 ms/GC 기여는 아직 확인하지 않았다. 지속 최대 비용 순위와 구현 위험도 순위를 구분해 승인 후 한 단위씩 검증한다. GPU 순위/목표기기60FPS는 이번 정적 조사로 확정하지 않는다.
