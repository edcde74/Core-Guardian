# 공격형 옆 보행 네이티브 배칭 — Draw 감소 확인, 기본 OFF

2026-10-23. 복구 기준 HEAD `1de8ced`, 미커밋. 사용자 승인 범위인 Attack 옆 보행5부품 구현·픽셀 검사·적128/타워15 단회 비교를 수행했다. **Draw는 감소했지만 CPU/GC 비용과 미해결 복원 문제 때문에 기본 OFF·채택 보류**다.

## 최종 구현·품질

원본 Sprite/SpriteRenderer·Transform 애니메이션·정렬 키를 유지하고 순서는 Unity에 맡긴다. 원본5텍스처를 변경 없이 CopyTexture로 Texture2DArray에 복사하며 카메라 구간에 공통 carrier/재질을 연결한다. GPU 버퍼의 원본 선형/HDR tint를 임시 alpha 인덱스로 참조하고 종료 시 원본 입력을 복원한다. 수동 합성 메시 방식은 폐기했다.

Attack 옆 보행만 대상이다. 기존 PropertyBlock·투명 alpha·피격/미지원 재질·다른 종류/방향·palette 초과는 원본 fallback. 기존 A/B 제한·SpriteAtlas 패킹 보류 유지.

- [최종 대상 검사](native_tests.json) **7/7 통과**: 기본 OFF, 원본 Sprite/Renderer·정렬 보존·정상 복원, 외부 렌더러 입력, fallback, 흰색/일반 tint/HDR1.15 픽셀 비교.
- [격리 품질](quality.json) **16쌍 모두 RGBA 차이0**: 좌우×4위상×단일/두 개체 겹침, 256×256/ortho2. [재현](Quality.cs). 모든 상태·전 방향·전체 화면·정상30R·아트 승인까지 의미하지 않는다.

## 동일 부하 비교

Unity6000.6.2f1 Editor, Ryzen5600X/RTX3070/RAM32681MB, 1920×1080/ortho8.5, VSync1/targetFrameRate-1. Attack112+Buff16·실제 타워15, timeScale1·고정 simulation dt1/60. 조건당 warm15/sample90 **각1회**의 승인된 비교다. FPS는 고정 dt 역수가 아니라 실제 wall frame interval로 계산했다. 양쪽90프레임 모두 적128/타워15·누락/무효시간0. 포탄 양쪽 min0/max36/평균6.733으로 일치.

|지표/단위/평균|원본|후보|절감량|절감률|반복/변동폭·한계|
|---|---:|---:|---:|---:|---|
|Native Draw (Standard+SRP)|1579.52|1020.43|559.09|35.40%|각90개; 범위1554~1787 / 998~1276|
|UnityStats Draw|1579.89|1020.13|559.76|35.43%|독립 보조 카운터|
|Standard Draw|184.93|188.50|-3.57|-1.93%|소폭 증가|
|SRP Draw|1394.59|831.93|562.66|40.35%|네이티브 배칭으로 전환|
|SetPass|188.43|189.98|-1.54|-0.82%|소폭 증가|
|전체 wall frame ms|30.234|28.667|1.566|5.18%|단회 변동 포함, 개선 확정 아님|
|Main Thread ms|30.213|28.663|1.550|5.13%|대기/중첩 포함, GPU 아님|
|CG.AttackBodyBatch ms|0.0018|3.8123|-3.8105|약-210265%|Main Thread에 포함, 합산 금지|
|GC B/frame|172414.82|410615.13|-238200.31|-138.15%|진단 할당 포함, 전체 증가 원인 미확정|

|FPS/프레임 통계|원본|후보|
|---|---:|---:|
|최저 FPS|6.78|13.80|
|최고 FPS|51.32|43.19|
|평균 FPS (총프레임/총시간)|33.08|34.88|
|프레임 ms 중앙값|25.039|27.429|
|프레임 ms 최대|147.571|72.477|

평균 FPS +1.81(+5.46%)지만 중앙 프레임 시간은 악화했다. 유효 스파이크를 제거하지 않았다. 각90개/단회이므로 p95/p99 안정 추정·통계적 FPS 개선 판정은 하지 않는다. 후보560부품이90프레임 내내 적용됐다. 소유 array+carrier 최대140032B, GPU 색상 buffer4080B는 전체 peak/잔류 메모리 검증이 아니다.

[baseline](native_baseline.json)/[candidate](native_candidate.json), [분석 결과](native_summary.json), [분석 코드](analyze_native.py), [실행 코드](Benchmark.cs), [환경](benchmark_environment.json). Recorder capacity1+wrap+LastValue만 사용했고 Deep/Frame Debugger/호출트리 추출은 하지 않았다. 승인된 짧은 범위여서 기본3회 반복은 미수행. GPU/순수 Render Thread·장기 플레이·목표기기60FPS 검증은 미완료다. 이전52.30/43.33FPS 실험과 조건이 달라 직접 비교하지 않는다.

## 검토 결과·다음 제안

읽기 전용 소스 검토에서 확인한 미해결 사항:

1. 초기화가 Flush의 try 바깥에 있어 생성/복사 중 예외 시 부분 리소스 정리가 보장되지 않는다. 재시도가 참조를 덮어쓰면 누적될 수 있다.
2. Flush 이후 다른 콜백이 material을 바꾸면 Restore의 material 일치 조건 때문에 임시 alpha/MPB가 남을 수 있다. ObjectEmphasis가 begin/end 사이 비활성화될 때도 명시적인 batch Restore가 필요하다. 정상 복원 검사 통과를 이 예외 경로 검증으로 확대하지 않는다.
3. renderer/camera마다 shader.name을 가져오는 반복 문자열 할당이 있다. Shader 참조 캐시로 줄일 수 있으나 GC 증가 전체의 원인이라고 단정하지 않는다.

다음 제안은 **복원/초기화 안전성 보완 + 반복 shader 조회·material/MPB 연결 비용 축소**, 해당 회귀 검사와 동일한 짧은 비교다. 사용자 승인 후 진행하며 기본 활성화는 별도 판단한다. 남은 약1020 Draw의 다른 종류/그림자/마스크까지 자동 확대하지 않는다.

## 실패 이력 보존

- 수동 메시 [최초8쌍](quality_failed_initial.json) 모두45~80픽셀 차이. [단일 부품](single_part_diagnosis.json)은0이었지만 [순서 시도](order_diagnosis.json)·[회귀](order_fix_failed_test.json)에서 생성 역순/EntityId 일반화에 실패. [겹침 실패](quality_manual_order_failed.json)1679픽셀 차이. [당시 소스](ManualBodyBatch_failed.cs).
- 새 Sprite를 만드는 native 시도도 순서가 달라 실패. [당시 C#](NativeDisplaySprite_failed.cs)/[shader](NativeDisplaySprite_failed.shader). 최종 코드는 원본 Sprite를 유지한다.
- 이후 vertex tint 양자화로 일반 tint 최대1채널 차이·HDR 큰 차이 발생. 원본 선형 tint GPU 버퍼로 수정 후 최종7/7·16쌍 통과. quality_order_fix_failed 이미지는 실패 당시 자료다.
- 초기 [RED](red.json)/[GREEN](green.json)은 폐기된 수동 메시 결과다. 최초 진단의 내부 반복만 중단한 문제는 현재 Quality.cs에서 첫 실패 시 전체 중단하도록 수정했다.

## 종료 상태

[자동 종료](benchmark_completion.json)/[복원 확인](benchmark_restored.json): Play stopped, compiling false, Deep/recording OFF, driver true, background false, captureDeltaTime0, timeScale1, candidate OFF, 전투씬 clean. [보호 파일](protected_before.json)의 씬·사용자 설정2개·EditorSettings·기존 meta5개 해시 모두 동일 확인. 사용자 파일/미추적 보존. 씬 저장·설정/원본 이미지 변경·빌드·실행파일 실행·push·커밋 없음.
