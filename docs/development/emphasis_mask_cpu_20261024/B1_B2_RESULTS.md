# B1/B2 독립 실행 결과 — 둘 다 미채택·제품 원본 복구

2026-10-24. 사용자 `B1/B2 진행` 승인으로 구현·회귀·각512조합1회·각warm15/sample90×3을 완료했다. 실패 반복/추가 Profiler 캡처는 없었다. 두 단계를 합친 후보는 실행하지 않았다.

## 결과

동일한 유효 원본3회를 재사용했다. B1 미채택 후 원본을 복구하고 B2를 독립 측정했다. 강조 안에 마스크가 포함되므로 두 절감을 더하지 않는다.

|평균 CPU ms|원본|B1|절감/율|B2|절감/율|
|---|---:|---:|---:|---:|---:|
|Mask.Present|4.877387|4.616157|0.261229 / 5.36%|4.455327|0.422060 / 8.65%|
|Emphasis.Present|5.923614|5.637147|0.286468 / 4.84%|5.518260|0.405354 / 6.84%|
|Mask.Output|1.345681|1.352243|−0.006562 / −0.49%|1.043021|0.302661 / 22.49%|
|BoundsCrop|0.848276|0.823911|0.024365 / 2.87%|0.822773|0.025503 / 3.01%|
|CacheLookup|0.984119|0.918473|0.065646 / 6.67%|0.928628|0.055491 / 5.64%|

- 원본Mask 반복:4.871869/4.878227/4.882064ms.
- B1:4.609718/4.668097/4.570658ms. B2:4.259732/4.192732/4.913517ms.
- B2 Output은각0.996873/0.981797/1.150392ms로 원본1.338176~1.352747ms보다 모두 낮다. **출력 속성 재구성 축소의 부분 개선**은 관측됐지만 마스크 전체는3회차에서 원본과 변동폭이 겹친다. 이상치를 버리지 않았다.
- 둘 다 평균 절감0.5ms미만으로 큰비용 목표 미달, 마스크1ms목표 미달. 자동 채택하지 않고 두 제품파일을 시작working-tree 원본으로 복구했다. B2를 논리적 실패나 픽셀 회귀로 분류하지 않는다.

## 구현과 원인

**B1:** cache Begin이 원래 만들던 Part에 readonly Sprite/Alpha/DrawMatrix 접근자를 추가해 같은 호출 QueuePage에서 재사용했다. 원본 Begin/Compatible/record 복사와 producer 값복사, 콜백·crop·sorting·fallback 순서는 유지했다. A의 별도144B snapshot 배열을 추가하지 않는다. 원본의 필수 Part 수집·캐시 비교·큐 등록·출력 기하/속성 작업이 남으므로 getter 재사용만으로 큰 절감이 나오지 않았다. Output은 줄지 않았으며 전체 프레임 차이를 getter 효과로 귀속하지 않는다.

**B2:** normal MPB의 정확한 texture참조/실제width·height/origin/color/clamp된radius가 같으면 Clear/Set 재구성을 건너뛰고 renderer.SetPropertyBlock은 매번 호출했다. contact/overlay/Release/Invalidate/자원 생성에서 무효화했다. Output 평균0.303ms를 줄였으나 마스크 생성·입력 준비·기하·매번재적용은 남는다. 3회차에는 다른 구간도 함께 느려졌으므로 전체Mask 악화를 MPB 회귀로 단정하지 않는다. 이3회만으로 외부 부하/GC/Editor 중 어느 하나가 변동 원인인지 확정할 수 없다.

## 조건과 전체 비용

Unity6000.6.2f1 Editor, Ryzen5600X/RTX3070/32681MB,1920×1080,ortho8.5,VSync1,target−1,고정simulation dt1/60,적128/타워15,기존 일반계측·Profiler recording/Deep OFF. 각105프레임(15+90)×3, 두 후보630프레임; 원본 추가측정 없음. 각270표본 모두gap1·물량일치. 임시 GameView index3 선택 후 각회 원래index3로 복원. 일반 벽시계 프레임과 simulation dt를 구분한다.

|조건/회|평균frame ms|최저/최고/평균 FPS|평균GC B/frame|GC collections|
|---|---:|---|---:|---:|
|원본1|24.920893|14.44 / 55.51 / 40.13|387351.32|1|
|원본2|25.021559|17.94 / 55.74 / 39.97|242851.79|1|
|원본3|25.107438|17.56 / 52.51 / 39.83|57323.57|1|
|B1-1|23.637744|21.39 / 59.41 / 42.31|243197.97|1|
|B1-2|25.019526|18.98 / 54.03 / 39.97|303016.61|1|
|B1-3|23.187263|20.90 / 59.26 / 43.13|303734.66|1|
|B2-1|22.854144|19.54 / 60.88 / 43.76|304849.60|1|
|B2-2|22.949172|20.50 / 59.14 / 43.57|55202.96|0|
|B2-3|25.667397|17.00 / 57.91 / 38.96|234832.91|1|

총270프레임/관측시간으로 계산한 평균FPS:원본39.9734/B1 41.7568/B2 41.9752. 전체GC평균229175.56/B1 283316.41/B2 198295.16B/frame. 프레임별FPS 산술평균을 쓰지 않았다. GC/다른scope 변동과 시간차가 있어 전체향상을 각기법 인과효과로 확정하지 않는다. 반복별GC·frame분포·Draw/SetPass·다른CPU마커는 아래JSON에 모두 보존했다. 전체품질/목표기기60FPS/정상30R/장기메모리·GPU 검증이 아니다.

B1 신규저장목록 없음(기존 Part/Lease 버퍼 재사용). B2 추가 필드는 bool1+reference1+int3+Vector2+Color:64bit reference8B 가정시논리45B/owner이며 객체padding/실제heap증가 측정은 아니다. 대형 snapshot 배열153.56KiB를 다시 도입하지 않았다. 종료시 후보자체를 제거했다.

## 검증과 한계

- B1 새Edit 입력테스트1: missing property RED→GREEN. 기존sharing31/producer9/outline3, 새실제Play큐소유1:합45통과. Play검사는 같은lease scratch를 직접 덮어쓴뒤 Flush해 불투명 출력존재를 확인한다. 다음Begin/Release/pool전체흐름을 한 번에 검증하거나 중심/전체픽셀동일을 단언하지 않는다. 입력테스트는 다음Present의 pose/alpha/flip갱신을 확인한다. 콜백순서는소스검토로보존확인했고새콜백변이시나리오는직접실행하지않았다.
- B2 새3: missing method RED→GREEN. 외부MPB복원/접촉·overlay모드전환/Release/Invalidate/RT크기·카메라·정확색·폭변화를 검사했다. 일부캐시갱신은private prepare 반환값검증이며 모든속성별renderer반영을각각검증했다는의미는아니다. 기존sharing31/outline3/emphasis4/shadow13:합54통과.
- 각단계512/512·mask/geometry/rect/output차이0·격리실패0. EditMode의실제원본출력한정검사이며Play모든body합성/HUD/모든camera검증으로확대하지않는다.
- 독립검토:critical/important결함없음,위범위한계명시. 검토자는Unity조작/수정없음.

## 다음에 우선 줄일 대상

이제 단순renderer getter 재사용이나 MPB setter만으로1ms를 기대하는 접근을 반복하지 않는다. 출력setter에는 약0.303ms 부분개선근거가 있으므로 나중에 다른 큰개선과 조합할 후보로 보존하되 두효과를더해확정하지않는다.

직전 승인진단의 QueueInputs약0.78ms와 OutputGeometry약0.69ms가 다음 분리대상이다(진단값이며일반성능절감예측아님). 첫구체안은 **이미생성한 Request의 camera/viewport/projection/target정보를같은Present의지원조건과페이지준비에재사용**하는것이다. 추가source배열이나frame전역캐시없이반복native읽기를줄이고,Resources콜백이후의한호출내경계를유지한다. Page.QueueShape의slot검증은이미synchronous경로에서생략되므로그것을또최적화했다고하지않는다. 다음후보의새제품수정·추가측정은아직미승인이다. 기하업로드약0.30ms전체를삭제하는가정이나페이지복사전체가CPU라는가정도하지않는다.

## 원자료·복원

- [B1 반복·분포·GC](b1/performance_runs.json),[B2](b2/performance_runs.json),[통합계산](b1_b2_summary.json). 각폴더Analyze.ps1로원본3+후보3재계산.
- [B1 픽셀](b1/pixel_comparison.json),[B2 픽셀](b2/pixel_comparison.json). 각폴더pixels_after.json과512RGBA/PNG; 공통원본pixels_before.json/이미지재사용.
- [계획](../../superpowers/plans/2026-10-24-prepared-mask-inputs-output.md),[제품원본](Outline_before.cs),[cache원본](Cache_before.cs). 각폴더후보소스/테스트/검사JSON보존.
- [최종Unity상태](b1_b2_final_state.json),[보호파일10/10동일](b1_b2_protected_after.json). 제품2파일시작원본hash일치,helper/새테스트Assets밖격리,복구컴파일성공. Play/changing/compile/updatefalse·씬clean7roots·Profiler/Deep/profileEditor/stack/FrameDebuggerOFF·backgroundfalse/capture0/scale1·nativeBatchOFF·기존Profiler버퍼0..11유지.
- 사용자기존변경·씬·설정·미추적보존. 빌드/실행파일/커밋/push없음.
