# 다음 수정안 — 준비된 입력 재사용과 출력 속성 재구성 축소

상태: 사용자 **B1/B2 진행** 승인으로 아래 두 단계 구현·검증·각512/성능3회 완료. 둘 다 큰비용목표 미달로 미채택·제품 시작원본 복구. [최종 결과](B1_B2_RESULTS.md)가 현재 상태의 정본이며 아래 내용은 실행한 설계다.

## 문제와 근거

기존 A는 getter를 한 번 모으지만 별도의 144B sample 목록을 만들고 이를 cache Part로 다시 복사한다. 전체 마스크 절감은0.333ms에 그쳤고 sample 배열 payload만157248B가 추가됐다. 일반 Profiler90표본의 CaptureSources는약0.79ms다. 범위를 더 키우는 snapshot 설계보다 **원래 필요했던 cache 입력을 뒤 단계에서 재사용**하는 쪽을 우선한다.

OutputBinding은같은진단에서약0.55ms였다. SetOutputMask는매번MPB를Clear하고Texture·TexelSize·Origin을쓰며,일반윤곽은Color·Width도다시쓴다. RT내용이바뀌더라도Texture객체·물리크기·Origin·색·폭이그대로면payload전체를재구성할필요는없다. 다만외부MPB변경을다음Present가복구하는계약은유지해야한다.

## B1: cache Lease.scratch의 기존 Part 재사용

대상: CoreGuardianBodyMaskCache.cs, CoreGuardianSelectionOutline.cs와 관련 회귀 테스트.

1. 원본 Begin은기존순서대로renderer입력에서Lease.scratch를채운다. 별도SourceFrame목록을추가하지않는다. bounds/crop은기존위치에서계산하며EnsureResources뒤콜백의최신입력을반영한다.
2. Part의Sprite/Texture/alpha/flip/localToWorld를읽는내부readonly접근만추가하고,Begin이끝난직후같은Present의QueuePage에서쓴다. list를다른프레임/소비자에보관하지않는다. 기존Begin/Request/Compatible/record복사계약은유지한다.
3. producer Draw에는지금처럼값을복사한다. 다음Begin·Release·pool재사용이기존queuedDraw를바꾸지못하도록검증한다.
4. 원본fallback의DrawMaskParts와sorting/bounds를불필요하게이경로로확대하지않는다. broad snapshot의비용과메모리중복을없애는것이목적이다.

예상효과는미측정이다. CaptureSources0.79ms전부가순절감은아니다. 원본Begin의필수입력읽기는남고native QueuePage확인/geometry/제출도남는다.

## B2: MPB payload는 값이 바뀔 때만 재구성, renderer에는 매번 재적용

대상: CoreGuardianSelectionOutline.cs와 기존 outline/contact/overlay 회귀 테스트.

1. 같은일반윤곽모드에서Texture참조·실제RTwidth/height·maskOriginPx·정확한Color·clamp후Width를비교한다. 바뀔때만소유MPB의Clear/SetTexture/SetVector/SetColor/SetInt를수행한다.
2. **output.SetPropertyBlock은매Present수행**한다. 외부가renderer의MPB를바꿔도기존처럼복구하며,성능만위해외부변이계약을삭제하지않는다.
3. contact/overlay의SetOutputMask,Release/Invalidate/자원재생성에서일반윤곽payload캐시를무효화한다. 모드전환시Band등다른속성이원래대로clear되는지도검증한다.
4. outputmaterial·enabled·정렬·quad·shader·RT/page/copyback은이단계에서바꾸지않는다. 폭/색/RT크기·손실/카메라·여백변화는기존동작을유지한다.

전체OutputBinding0.55ms를없앤다는약속이아니다. reapply와필요한업데이트비용은남고,소량의owner별metadata메모리가추가될수있어함께측정한다.

## 이후에 볼 구간 — 이번 B1/B2와 묶지 않음

- QueueInputs약0.78ms: camera지원조건/Request/View.Matches의반복native읽기를동일Present의일관된카메라입력으로공유할수있는지검토한다. 전역프레임불변을가정하지않는다. **slot검증은이미QueuePage의synchronous경로에서한번검증한형태**이므로같은최적화를다시제안하지않는다.
- Emphasis.Structure약0.89ms: 구조검사와active목록재구축을분리한다. 공개변이/rig생성·재부모화·활성콜백을유지해야하므로무조건dirty방식으로바꾸지않는다.
- OutputGeometry약0.69ms: 이중quad업로드자체는일반성능약0.30ms였고나머지계산/좌표변환이남는다. Point raster를보존하며단계별로검토한다.

## 검증과 실행 경계

각단계를독립버전으로검증해합친효과를개별기법성과로분배하지않는다. 새계약은RED→GREEN,기존영향필터만실행한다. B1에는같은frame입력변경·자원콜백·queued값소유/다른camera/다음Begin, B2에는외부MPB복원·모드전환·폭/색/RT변화검사를둔다.

이번에수집한원본512결과와기존원본3회는조건/계측이같으면재사용한다. 각후보의격리512조합1회와warm15/sample90×3으로비교하고,실패시자동반복하지않는다. 마스크1ms목표·0.5ms미만큰비용목표미달·변동/품질/GC/메모리조건을유지한다. 새구조수정이므로사용자에게파일·방법·반복/부하·시간·복원을제시한후별도승인받아야한다.
