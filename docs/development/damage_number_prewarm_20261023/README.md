# 최초 피해 숫자 준비 및 전체 스크립트 조사

2026-10-23, HEAD `1de8ced`, 미커밋. 사용자 승인: HUD 초기화에서 TMP Settings와 최초 HitDamage3개 준비, 기존 풀 재사용/초과 생성/원본 표시 유지, 짧은 일반 비교 및 전체 스크립트 정적 조사. 추가 후보 수정·새 Deep Profile·빌드·실행파일·push·commit 없음. [계획/승인 범위](PLAN.md), [전체 조사와 다음 후보](SCRIPT_AUDIT.md).

## 구현과 실제 검증

- [CoreGuardianHud.cs](../../../Assets/Scripts/Runtime/Presentation/UI/CoreGuardianHud.cs): Configure의 전투 이벤트 구독 전에 `TMP_Settings.instance`를 확인하고 실제 damageFont/damageMaterial로 HitDamage3개를 만든다. 투명한 `0123456789` 메시를 ForceMeshUpdate 후 기존 hitNumberPool로 반환한다. Gold/Core 숫자 사전 생성은 추가하지 않았다.
- 첫 피격은 준비된 객체를 사용하고, 더 필요하면 기존 CreateFloatingNumber로 확장한다. 0.85초 수명·색상·32px·정렬·드리프트·레이어·효과 수·골드/코어 표시와 기존128개 비활성 보유 한도는 유지했다.
- 마커: `CG.HUD.PrepareDamageNumbers`, `CG.HUD.CreateDamageNumber`. 준비 비용을 없앴다고 하지 않고 전투 전으로 옮긴다.
- [원본 소스](CoreGuardianHud_before.cs), [대상 테스트](../../../Assets/Scripts/Tests/PlayMode/CoreGuardianDamageNumberPoolPlayModeTests.cs). 수정 전 실제 [RED1/1실패](red.json): 준비0개/기대3개. [최초 GREEN1/1통과](green.json), 재질·glyph 정점/UV/색상 fallback 동등성을 추가한 [최종 결과](green_final.json). 숨김·첫3개 재사용·4번째 생성·수명·반환 후 재사용 확인. 전체 HUD 픽셀·모든 숫자/언어/기기·재시작 잔류 전수 검증은 아니다.
- 신규 테스트 첫 시도는 internal EnemySnapshot 생성자 사용 오류로 컴파일 실패하여 실행0건이었다. 테스트를 실제 Session.ApplyDamage 경로로 수정한 뒤 컴파일 오류0/유효 RED를 확인했다. 0건을 통과로 세지 않았다.
- [독립 소스 검토](REVIEW.md): 수정 지적 없음. 기존 씬/설정·사용자 파일은 보존했다.

## 일반 모드 전후 관측

Unity6000.6.2f1 Editor, Ryzen5 5600X/RTX3070/RAM32681MB,1920×1080/ortho8.5/VSync1/targetFrameRate-1. 원본 출력·native batch OFF, 적128(Attack112/Buff16)/타워15, 동일 Diagnostic 주입·고정 simulation dt1/60, warm15/sample90 **조건당1회**, Deep/recording/할당스택 OFF. [환경 전](before_benchmark_environment.json) / [환경 후](after_benchmark_environment.json), [원시 전](before.json) / [원시 후](after.json), [분석 스크립트](analyze.py) / [전체 수치](summary.json). 두 구간 모두90유효·gap0. p95/p99는 짧은 단회 표본이라 판정 생략.

|지표/통계|전|후|절감량|절감률/증가율|한계|
|---|---:|---:|---:|---:|---|
|frame ms 평균|27.077|23.728|3.348|12.37%|단회 Editor 관측|
|frame ms 중앙|24.436|21.731|2.705|11.07%|단회|
|frame ms 최대|128.742|48.105|80.638|62.63%|각 구간의 최대 프레임은 서로 다름|
|FPS 최소/최대/평균|7.77 /57.89 /36.93|20.79 /55.82 /42.14|N/A|평균 +5.21 /+14.11%|N×1000/시간합; 목표기기 아님|
|engine22 wall ms|128.742|36.357|92.385|71.76%|앞선 초기 지연 위치. 번호만으로 동일 콜백 개수/단독 인과를 보장하지 않음|
|GC 할당 B/frame 평균|486276|56263|430013|88.43%|전의14.55MB 급증 출처 미확정, 전부 이번 수정 효과로 배분하지 않음|
|GC 할당 B/frame 중앙|23016.5|21108|1908.5|8.29%|단회|
|GC 할당 최대 B/frame|14553605|515458|14038147|96.46%|동일 할당 호출 비교 아님|
|CollectionCount(0) 증가|5|1|4|N/A|정지시간이나 game 단독 수거 횟수로 해석하지 않음|
|GC.Collect marker 평균 ms|0|0|0|N/A|이번 일반 모드에서 이 마커 정지0; 수거 자체0을 뜻하지 않음|
|incremental marker 평균 ms|0.06861|0.000512|0.06809|N/A|동일 GC 작업량 미보장|
|Main Thread 평균 ms|27.070|23.720|3.350|12.37%|대기 포함|
|Emphasis.Present 평균 ms|6.254|5.611|0.643|10.28%|미수정 경로 변동도 있어 개선 단독 인과 주장 금지|
|내부 Mask.Present 평균 ms|5.189|4.672|0.517|9.96%|Emphasis와 중복 합산 금지|
|PhotoShadow.Present 평균 ms|0.905|0.846|0.059|6.57%|해당 코드는 이번에 미수정|
|Draw 평균|1579.10|1593.19|-14.09|-0.89%|증가 관측; 드로우콜 개선 작업 아님|
|SetPass 평균|187.24|193.34|-6.10|-3.26%|증가 관측|

after engine22의 CreateDamageNumber 마커0ms. 이후 프레임28/29/30/31/34/37에는 추가 생성이 있으며 마커 각각0.856/0.559/0.950/1.044/0.985/1.039ms였다. **최초3개 준비만 해결했으며 전투 중 추가 생성 전체 제거가 아니다.** 기존 풀 확장 fallback을 유지한 의도된 결과다.

[preparation.json](preparation.json): 준비 마커0.5832ms, 숫자3개/활성0개, 직접 소유한 GameObject/Component/Mesh 런타임 크기 합19,599B(19.14KiB). 공유 font/material/atlas·TMP 내부 managed buffer·프로세스 전체 잔류/peak는 미포함이다. 대상 PlayMode 테스트 이후 같은 Editor에서 측정해 리소스/엔진 캐시가 데워졌을 수 있다. 이 값을 **완전한 cold launch의 최초 TMP 설정 로드 비용**으로 사용하지 않는다. 완전한 초기 비용/메모리 비교는 미완료다.

## 실패·한계와 복원

- 시작할 때 stopped/idle/clean이었지만 Recording이 ON이었다. 사용자에게 확인 후 “사용 중 아님, 끄고 진행” 승인을 받아 껐다. 기존 사용자 검사를 임의 중단하지 않았다.
- 수정 후 첫 비교는 `1920×1080/ortho8.5` 조건 guard에서 **표본0으로 종료**. [실패](after_first_attempt_failure.json), [당시 복원](after_first_attempt_restored.json). 당시 guard가 실제 width/height/camera를 남기지 않아 어떤 값이 달랐는지 확정하지 않는다.
- 별도 사용자 승인 후 현재 Game View index3/target1920×1080을 확인하고 같은 인덱스를 임시 고정, Play 전환 직후 최대15 Editor update만 조건 안정화를 허용한 단회 재실행이90표본으로 완료됐다. [화면 전](game_view_before.json) / [복원](game_view_restored.json), [완료](after_benchmark_completion.json), [측정 복원](after_benchmark_restored.json). 이후 실제 최종 상태는 [final_state.json](final_state.json).
- 변경 전14.55MB/5.23MB 급증의 상세 할당 출처를 이번 기록OFF 비교에서 확정하지 못했다. engine22는 CollectionCount 변화0/GC.Collect0이었다. 할당 급증과 수거 정지를 구분한다. 일반 스톨의 감소 관측을 TMP 하나의 확정 절감량으로 과장하지 않는다.
- 전후 샘플은 정상30R 자연 플레이나 목표 GTX750Ti/RAM4~8GB 검증이 아니다. 단회90프레임은 기존 승인 길이를 유지한 예비 비교이며 기본3반복/새 cold process/Player/GPU/전체메모리는 미검증. GPU 시간N/A. 향후 추가 측정은 별도 승인 필요.
- 보호5파일 해시는 [전](protected_before.json) / [후](protected_after.json), 소스/도구 해시는 [source_hashes.json](source_hashes.json). 기존 미추적/씬/설정·A 보류/B 제한·실제 아틀라스 패킹 보류·원본 출력·native batch OFF 유지. 자동 커밋 없음.
