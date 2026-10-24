# 마스크 호출 경로 실행 결과 — 목표 미달·후보 미채택·원본 복구

2026-10-24 Asia/Seoul. HEAD1de8ced, 기존 미커밋 보존. 사용자 `함 해봐`로 [실행 계획](../../superpowers/plans/2026-10-24-mask-submission-path.md)을 승인했다. 컴파일 응답 지연 뒤 `한 줄 복구 후 기존 범위 계속`도 명시 승인받았다.

## 판정

카메라 입력을 같은 호출에서 재사용하는 후보를 구현했다. 후보 대상52테스트와 격리512조합은 통과했으나 **마스크 준비+Flush 합계 절감0.367280ms/5.25%로 누적1ms목표 미달**이다. 후보는 채택하지 않고 제품4파일을 시작working-tree 원본으로 복구했다. 새 helper/테스트/meta는 candidate/에 보존했다.

완료 마스크의 프레임 간 재사용(Task3)은 사전 적용성 gate가 낮아 구현하지 않았다. Task3용 시간축16시퀀스·추가512·후보315프레임도 실행하지 않았다. 효과 부족을 픽셀/동작 실패로 분류하지 않는다. 독립 코드 검토는 사용량 제한으로 미완료이며 검토 통과를 주장하지 않는다.

## 일반 계측의 직접 비용

새 동일 계측 원본/후보 각warm15+sample90×3. 총540유효 표본, gap누락0·적128/포탑15 불일치0. Present와CacheFlush는 별개 호출 구간이라 합산했다. CacheFlush 안의 PageProducer/CopyBack 및 강조 안의 Present를 다시 더하지 않는다.

|CPU 평균 ms|원본|후보|절감|절감률|
|---|---:|---:|---:|---:|
|CG.Mask.Present|4.509021|4.414803|0.094218|2.09%|
|CG.Mask.CacheFlush|2.493020|2.219957|0.273063|10.95%|
|두 구간 합계|7.002041|6.634760|0.367280|5.25%|
|PageProducer (Flush 내부)|0.421121|0.393574|0.027547|6.54%|
|CopyBack (Flush 내부)|0.072269|0.070804|0.001465|2.03%|
|Emphasis.Present (Mask 포함)|5.499220|5.309867|0.189353|3.44%|
|Mask.Output (Present 내부)|1.224723|1.188826|0.035897|2.93%|

합계 반복 범위:원본6.802966~7.338010ms,후보6.529978~6.779551ms. 3회 평균의 방향은 낮지만 절대효과가 목표보다 작다. 전체 frame은23.235299→22.326017ms,총관측시간 기준평균FPS43.03797→44.79079. GC평균263051.17→180694.97B/frame,수거합4→2회. 다른scope/GC/Editor변동이 있으므로 전체frame0.909ms차이를 전부 이 후보에 귀속하지 않는다. 반복별 최저/최고/평균FPS·GC·frame은 [표](performance_table.md), 분포·Draw/SetPass·다른scope는 [원자료 집계](performance_runs.json)에 보존했다.

이 원본은 새 Flush 마커를 넣은 동일계측 원본이다. 과거4.877ms원본을 이번4.509ms와 뒤섞지 않는다. 새 원본3회를 수집한 이유는 과거 자료에 전체Flush/copyback계측이 없었기 때문이다.

## 사전 진단과 Task3 생략 근거

warm15+진단15프레임1회에서2175요청/2175재생성,1450copy를 집계했다. frame을 제외한 정확한 완료입력이 같은 요청은198건(9.10%)이었다:Multi144/Support39/core15. 움직이는 적에서는 crop변경이 먼저 걸렸다. 분류는 첫 불일치이며 crop/pose등 동시변화의 배타적 원인분해가 아니다.

해당198요청의 재생성 준비 구간은 약0.076307ms였다. 완료 RT재사용을 하더라도 필수입력 수집·범위계산·출력준비는 남는다. 적128/포탑15에서 이 방식만으로 누적1ms를 기대할 근거가 부족해 계획대로 Task3을 생략했다. 4.88ms에9.1%를 곱해절감량이라고 하지 않는다.

진단에서Present6.453993/Flush3.345733/PageProducer0.423413/CopyBack0.073333ms였다. 진단분류의추가연산·별도짧은실행이므로 일반원본/후보시간과비교하지않는다. 당시Flush에서producer/copy를뺀약2.849ms를전부카메라getter비용이라고확정하지않는다. [진단15표본](after_diagnostic.json),[역할/원인 집계](diagnostic_counts.json),[요약](diagnostic_summary.json). 재사용Benchmark의일반정책문자열에는90표본설명이남아있지만실제진단완료·데이터는15표본이다.

## 구현과 효과가 작은 이유

- `CoreGuardianMaskCameraState` readonlystruct를 EnsureResources의자원생성콜백뒤에읽었다. Request/QueuePage/Page획득이동일호출의view/projection/viewport/target/지원조건을소비했다. 프레임전역카메라캐시나owner별snapshot배열은추가하지않았다.
- BodyMaskCache.Flush는렌더경계에서카메라를freshCapture하고같은동기호출의slot검증과중첩Producer.FlushPrepared에전달했다. public TryAcquire/IsValid와단독Producer.Flush는freshCapture를유지했다.
- 기존crop/ScreenToWorld/행렬연산·지원범위·원본outputRenderer/Material/MPB재적용·fallback·부품별queued값소유는유지했다. B1/B2·프레임간재사용·페이지직접출력·출력배칭활성화는합치지않았다.

바꾼것은카메라native입력중복일뿐, 매slot의RT.IsCreated/lease조회/정확행렬비교·Unity객체비교·pending수집/완료처리/Discard는여전히남는다. Present의source검증·bounds/crop·quad/UV업로드·정렬·MPB설정도남는다. 공개entry에서전체CameraState를읽는비용도생긴다. 이원인들은소스에서확인한잔여작업이며각각의ms는분리계측하지않았다.

일반계측후보Flush2.219957ms중producer0.393574/copy0.070804를제외한약1.75558ms가남았다. **이잔여시간을특정메서드나드라이버대기라고확정할수없다.** 이번결과는카메라getter재사용만으로전체잔여구간을없앨수있다는가설을지지하지않는다.

따라서다음은새구조후보를곧바로더하는대신, Flush의pending수집/slot검증/완료처리/Discard와엔진호출의경계를구분해실제로큰항목을찾는것이다. 필요할경우계획에기재한normalProfiler warm15/sample90 1회·상위2프레임제한조사를별도승인받는다. 이번에그추가캡처나새수정은실행하지않았다. 출력기하재사용역시새근거를먼저확인할별도후보다.

## 검증·실패·한계

- 완료재사용진단분류3개:missing method RED→GREEN. 완료동일입력/pose변경/pending미완료를구분했다. 이3개는진단구현검증이며Task3제품재사용을검증한것이아니다.
- 후보camera3:missing state RED→GREEN. 같은호출내캡처값소비,다음public호출fresh상태/다른camera분리,자원생성콜백camera변경반영을확인했다.
- 기존sharing31/page5/producer9/outline3통과. 실제Play camera변경직전Flush fallback1통과. 후보52개. Play검사는queued원본마스크의불투명출력존재/완료상태이며전체영상동일이나풀전체흐름검증은아니다.
- 격리512/512:mask/geometry/rect/output차이0,isolationFailures0. [비교](pixel_comparison.json),[후보512원본](pixels_after.json),PNG/RGBA각512개. [재사용원본](../emphasis_mask_cpu_20261024/pixels_before.json). EditMode격리출력검사를전체body합성/HUD/모든camera/정상30R검증으로확대하지않는다.
- 컴파일중CoreGuardianMaskPage.Release의`out var v`선언누락오류가한번발생했다. 응답지연으로후속명령중단·사용자명시승인후한줄복구/재컴파일1회,실제상태완료failedfalse/compilationFailedfalse확인후검증재개. 후보측정/픽셀실패로감추지않는다.
- 독립fresh검토1회는요청했지만검토자사용량제한으로최종보고를받지못했다. 새검토자를우회투입하지않았으며검토완료/무결함판정을기록하지않는다. 제품은원본으로복구돼있다.
- GPU시간/드라이버대기/상위원인귀속·전체heap/장기잔류는미측정. struct는호출범위의값이며heap할당0이나메모리무회귀를별도실측한것은아니다.

## 실행 조건·복원

일반Editor조건:Unity6000.6.2f1/5600X/RTX3070/32681MB,1920×1080/ortho8.5/VSync1/target−1/고정simulationdt1/60,적128·타워15. Profilerrecording/Deep/FrameDebuggerOFF. 본측정원본3+후보3=630프레임,진단30프레임,합660프레임;회귀테스트/512렌더별도. 각측정후Play정상종료·GameView index3→3복원. 추가Profiler캡처/Task3측정없음.

제품4파일은 [시작원본](original/)과hash동일([확인](product_restored.json));보호10파일동일([확인](protected_after.json)). 후보/helper/새테스트meta는[candidate/](candidate/)에격리. 원본복구재컴파일성공.

최종MCP연결도구가세션에서사라져CLI로조회했다. 샌드박스연결파일읽기거부후권한검토를거친조회만수행했다. [현재상태](final_state_cli.json):단일전투씬clean7roots,Play/changing/compile/updatefalse,Profiler/Deep/profileEditor/stack/FrameDebuggerOFF,backgroundfalse/capture0/scale1/nativeBatchOFF,1920×1080.

**Profiler버퍼차이:**실행시작first/last0..11,최종조회−1/−1로비어있었다. 발생시점/원인미확정이며버퍼보존성공으로보고하지않는다. 무승인대형profile로드/추가조회로복구하지않았다. 사용자씬/설정·기존미커밋·미추적은보존했다. 빌드/실행파일/커밋/push/Notion변경없음.

원자료: [일반전후집계](performance_runs.json),[평균계산](performance_summary.json),[반복표](performance_table.md),[계산스크립트](Analyze.ps1),[진행/승인/실패](progress.md). 각before_N/after_N.json과environment/completion/restored원본보존. 다음구현·추가진단은현재승인범위에포함되지않는다.
