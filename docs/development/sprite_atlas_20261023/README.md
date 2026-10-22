# SpriteAtlas 호환 처리·패킹 기본 적용 보류

2026-10-23. 사용자 승인: 조사에서 제안한 순서대로 아틀라스와 선행 호환 문제 처리, 이후 추가될 몬스터·타워 고려. 복구 기준 `bf76436`. 사용자 설정2개 및 기존 빈 `CG_Enemies.spriteatlasv2`는 보존했다.

**최종 상태: 호환/제작 코드는 반영하되 실제 패킹은 기본 비활성으로 복구했다.** 최종 후보 표본43.33FPS와 같은 최종 런타임 코드의 원본 표시52.30FPS를 비교해, 악화 가능성과 메모리 증가가 있는 후보를 기본 활성으로 남기지 않았다. 관리 아틀라스2개는 Include in Build OFF/packables0이며, 원본 표시와 B92/92 유효 상태를 확인했다([held_final_state.json](held_final_state.json)).

## 검증한 후보 범위 — 현재 기본 비활성

- `Assets/Art/Atlases/Sprites/CG_Enemies_Common_01.spriteatlasv2`: 기본 적 이미지·앞뒤 보행·BossEye36개, 실제1페이지1024×512.
- `Assets/Art/Atlases/Sprites/CG_Towers_Common_01.spriteatlasv2`: 타워 본체·고정/가동부9개, 실제1페이지256×256.
- 후보 검증 당시 Master/V2·Include in Build ON·Point·mipmap/ReadWrite/rotation/tight OFF·padding4·Max2048·무압축. 최종 보류 상태는 포함/패킹 소유권을 해제했다. 기존 PNG/이름/Resources key/씬 참조는 유지한다.
- 포탄·피격 등9종은 이미 CombatB.png1텍스처다. 추가 아틀라스로 재포장하지 않았다. 전투 효과 입력 텍스처가2개 이상이 되면 같은 제작 파이프라인이 `CG_CombatFX_Common`을 생성하도록 준비했다. 현재 포탄 단독 Draw 개선은 없다.
- 플레이어·코어·환경/저장 길 메시·UI/TMP·넓은 그림자 원본은 이번 패킹에서 제외했다. A 보류와 B 방어형/보스 페이지 래스터 fallback 범위를 유지한다.

## 해결한 호환 문제와 유지한 예외

1. OriginalWalk/OriginalMotion의 런타임 부위 자르기를 textureRect 기반 공통 Cut으로 연결했다. 원본Texture와 UV 변환을 생성 Sprite에 계승한다. 원본 데이터는 `Assets/Data/Presentation/Resources/CoreGuardian/SpriteAtlases/Sources.asset`(45개)에 저장한다.
2. SpriteAtlas의 Tight Packing을 꺼도 Tight 입력 메시의 투명 여백이 잘리는 실제 실패를 확인했다(초기 보행21개 중10개 실패). 해당 입력은 FullRect로 유지한다. 초기 변경 meta33개는 spriteMeshType1→0뿐이고 PNG 픽셀은 변경하지 않았다.
3. B의 기존 원본Texture2DArray를 그대로 사용한다. TryGetSprite가 원본 키와 원본 UV를 돌려주고, FullRect 꼭짓점은 원본 픽셀 모서리에 맞춰 부동소수 취소 오차를 줄였다. 실제 패킹45개 중40개가 기존 B 입력이고5개는 기존 미지원 fallback이다. 기존 전체 manifest92/92 입력 유효성을 최종 확인했다.
4. 소스 메시/임포트 후처리 코드가 바뀌면 Unity 의존성 hash도 바뀐다. 기존 배열과 원본 알파를 전수 대조한 입력만 hash를 갱신한다. alpha가 다르면 자동 승인을 하지 않고 명시 재베이크가 필요하다고 실패한다. 배열 페이지는 새로 만들거나 교체하지 않았다.
5. 회전하는 옆 보행 부품을 패킹한 첫 실제 렌더에서4~57픽셀 차이가 발생했다. `OriginalWalk`의20개 입력은 아틀라스에서 제외하고 원본 표시를 유지했다. 이 부분의 직접 표시 배칭은 미완료다. 최초 실패 결과는 [quality_first_failed.json](quality_first_failed.json)에 보존한다.
6. 타워 발광과 바닥 음영도 원본 텍스처/원본 좌표로 샘플링해 미세 색상 차이를 제거했다. 해당 효과의 원본 재질·색·수명·정렬은 유지한다.
7. 독립 검토에서 Domain Reload OFF일 때 보행 static cut 캐시와 새 세션의 매핑 초기화 시점 불일치를 발견했다. 매핑 초기화 전에 해당 소유 Sprite를 정리하고 두 캐시를 비운다. 재현 테스트 RED→GREEN, 반복 Play 표본까지 확인했다. 후보 성능 표본은 이 수정 후 다시 수집했다.

## 신규 리소스 추가

- 파일 위치/이름·Resources key 보존 기준은 [디렉터리 정본](../directory_reorganization_20261023/README.md)을 따른다.
- 일반 적 본체는 `Art/Characters/Enemies/Sprites`, 앞뒤 프레임은 기존 `Resources/CoreGuardian/VerticalWalk` 소유 폴더에 추가한다. 타워는 `Art/Characters/Towers` 아래에 추가한다. 기존 적 옆 보행 `OriginalWalk`는 원본 표시를 유지한다.
- 등록 정책의 폴더는 하위를 포함한다. 후보 활성 시 신규 적/타워 입력은 import 후처리가 FullRect를 보장하며, 개별 PNG마다 수동 등록하지 않는다. 관리 대상 폴더를 다른 기본 아틀라스에 중복 등록하면 제작기가 오류로 중단한다.
- `CoreGuardianSpriteAtlasPipeline.Synchronize(true,true)`는 idle Edit Mode에서 후보 패킹/미리보기와 원본 매핑 동기화에 사용한다. `Synchronize(false,false)`는 패킹 소유권까지 해제해 원본 표시로 복구한다. 기본 인자는 비활성이다. 향후 사용자 요청 빌드의 IPreprocessBuildWithReport도 현재 후보 보류 상태를 유지하며 자동으로 켜지 않는다. 새 B 입력은 자동 확대하지 않고 기존 fallback을 유지한다.
- 새 종류/방향/부품을 추가하면 Pack Preview와 Sprite Atlas Analyzer에서 페이지 수·빈 공간·텍스처 메모리를 확인하고 해당 부위/방향 렌더를 검증한다. 이 기록은 신규 아트의 품질/밸런스 승인까지 대신하지 않는다. 현재2개 아틀라스를 무조건 유지해야 한다는 제한은 없으며, 큰 보스/다른 텍스처 설정/다른 동시 사용 조건이 늘면 별도 분리한다.

## 실제 검증

- 신규 좌표/B/실제 패킹 픽셀/신규 import/세션 캐시 계약5개 통과. 좌표2개·신규 import·캐시 초기화는 구현 전 예상 실패를 확인했다([red.json](red.json), [import_red.json](import_red.json), [reset_red.json](reset_red.json), [compatibility.json](compatibility.json)). 보행21+타워 조준8/발광3/접지3+마스크2+아웃라인3+넓은 그림자10 = 전체 대상55/55 통과. 실제 패킹 검사는 try/finally에서 잠깐 후보를 켠 뒤 보류 상태로 복원한다. 관련 코드 변경 전후의 대상 검사만 실행했고 전체 게임 suite는 반복하지 않았다.
- [quality.json](quality.json): 적3종·타워3종×4방향의 실제 PreviewScene Camera.Render24쌍, 각256×256/ortho2/동작 .12초에서 패킹 표시와 원본 텍스처 참조 표시가 픽셀 차이0. 배경 외 유효 픽셀 존재를 검사했다. 초기 camera.scene 미지정 결과는 빈 렌더 가능성이 있어 폐기하고 명시 scene/유효 픽셀 검사를 넣은 결과만 사용했다. 정상 키 입력·모든 위상·전체 HUD·native 줌 전수 통과는 아니다.
- 실제 자체 Play에서 적128·타워15, 표시용 아틀라스 텍스처 사용과 마스크miss0·PhotoDynamicContact0 확인([live.json](live.json), [live_sprite_mapping.json](live_sprite_mapping.json)). 최신 전체 mask manifest92/92 유효, 배열11개 유지([final_state.json](final_state.json)).
- 테스트 중 원본 PNG를 가정하던 기존 Gait 검사2곳은 실제 GPU 텍스처 읽기/원본 Sprite importer 조회로 수정했다. 기대 픽셀/피벗/동작 기준을 완화하지 않았다. Unity Object의 동일 native 참조를 서로 다른 managed wrapper로 돌려주는 경우는 Unity equality로 비교한다.
- 진단 eval_file의 전체 클래스 로딩/미지원 Convert.ToHexString/using 없는 extension 호출 오류는 코드 실행 전 컴파일 거절이며 게임 변경/검증 성공으로 계산하지 않았다. 클래스 진단은 공식 run_script로 수행했다. 테스트 진행 중 에디터 TextureInspector의 SerializedObjectNotCreatableException이 있었으며 로그를 지우지 않았다. 최종 컴파일 오류 없음; 플레이 게임 예외로 확대하지 않는다.

## 최종 코드에서 패킹/원본 표시 비교

1920×1080/ortho8.5/time1, Editor6000.6.2f1/URP, 기존 같은 씬·같은 진단 입력. 양쪽 적128·타워15를 모든 표본에서 유지했고 워밍업15프레임 뒤90프레임씩 각1회 수집했다. 캐시 수정 후 패킹 후보를 먼저 측정하고, 패킹만 해제한 같은 최종 런타임 코드를 비교했다. 원본 [final_unpacked.json](final_unpacked.json)·[after.json](after.json), 재현 [FrameProbe.cs](FrameProbe.cs), 계산 [final_comparison.json](final_comparison.json). 프레임 평균FPS=표본수×1000/프레임ms합, spike 제외 없음.

|지표|원본 표시(최종 유지)|패킹 후보(보류)|관측 차이|
|---|---:|---:|---:|
|평균FPS|52.303|43.326|−8.976, −17.16%|
|최소/최대FPS|24.173/62.873|25.881/57.703|각90표본 극값|
|평균frame ms|19.119|23.081|+3.961, +20.72%|
|Draw 평균|1577.422|1581.267|+3.844|
|SetPass 평균|186.111|188.556|+2.444|
|Editor 로드 Texture2D runtime bytes|165110913|169825431|+4714518, 약4.50MiB|

판정: **후보 기본 적용 보류**. 짧은 동적 전투의 각1회 비교라 포탄/위상·편집기 스케줄 변동을 분리하지 못했고,17.16%를 확정적 인과 효과로 주장하지 않는다. 그러나 최종 표본은 FPS 악화 방향·Draw/SetPass 증가·메모리 증가여서 기본 활성 근거가 없다. Texture2D bytes는 전체 Editor 로드 객체 기준이며 GPU peak/독립 Player 메모리·전체 잔류가 아니다. GTX750Ti 안정적60FPS 통과 아님. CPU/GPU 세부 시간은 이번 짧은 probe에서 수집하지 않았다.

이전 단계의 최초 유효 기준은51.43FPS([before.json](before.json)), 캐시 검토 전 후보는52.85FPS([after_before_cache_fix.json](after_before_cache_fix.json))였다. 이 좋은 표본만 채택하지 않고 최종 코드 결과를 별도 보존했다. 옛 [summary.json](summary.json)은 최초 기준과 최종 후보를 나란히 둔 중간 계산이며 최종 판단 표는 위의 같은 최종 코드 비교다.

최초 기준 시도는 적125~126으로 조건 불일치해 [before_invalid_counts.json](before_invalid_counts.json)에 보존하고 비교에서 제외했다. 진단 Session의 weak starter3개 자동 생성을 막고 정확히128개를 주입했다. 후보 해제 첫 시도에서 Include in Build OFF만으로는 Sprite의 패킹 소유권이 남아 런타임 texture가 없어지는 실패가 발생했다. 즉시 자체 Play를 종료했고, packables까지 비우고 입력을 재임포트하는 방식으로 복구했다. 이 실패는 성능 표본이 없으며 최종 원본 표시 Play는 error=null/90표본·콘솔 현재 오류0을 확인했다. ProfilerRecorder.ToArray/CopyTo는 사용하지 않았다. 임의120초/정상30R/장시간 측정을 추가하지 않았다.

## 종료·남은 범위

전투씬/사용자 설정2개/EditorSettings hash4개 동일, Play stopped/scene clean/원래7루트/컴파일 완료/Profiler OFF·driver true·background false 복구. 씬 저장·빌드·실행파일·push 없음. 사용자 설정2개와 범위 밖 ProjectWelcome 텍스처 meta의 Unity Android 기본값 직렬화 차이는 커밋에서 제외하고 보존한다.

소스/문서 diff whitespace 검사는 통과했다. Unity API가 생성한 atlas/meta의 빈 필드 뒤 공백은 기본 직렬화 형식이므로 직접 수정하지 않았다. 기존 PNG 변경은 없고 캐릭터 meta diff는 spriteMeshType1→0뿐이다.

남음: 실제 Draw/SetPass 병목에 맞춘 유효한 표시 배칭·옆 보행의 안전한 처리, 정상 조작·모든 위상/화면 크기 회귀, Player/목표기기 성능·장시간 GPU/전체 잔류. 아틀라스2개 제작과 호환 코드는 준비됐지만 기본 패킹은 보류이며, 현재 게임 표시가 아틀라스 적용됐다고 설명하지 않는다. 후속 비용은 기존처럼 적128·타워15의 동일 조건에서 작은 범위로 판단한다.
