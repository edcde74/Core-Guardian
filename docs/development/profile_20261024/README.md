# 최신 프로파일 녹화 분석 — 2026-10-24

조건: Editor, 적128·타워15, 현재 코드(SRP Batcher OFF + 윤곽 출력 배칭). Profiler 일반 녹화(Deep OFF), warm15 후 90프레임.
`Prototype.Update`·`Session`에는 녹화 동안만 임시 마커(`CGX.*`)를 넣었고, 녹화 후 원래대로 되돌렸다(git 변경 없음).
원본: `profile_latest.data`(Profiler 창에서 열기, 약 695MB), `profile_latest_hierarchy.json`, `profile_latest_flat_self.json`.
녹화 부하로 PlayerLoop가 9.29ms다(녹화 없이 7.8~8.2ms).

## PlayerLoop 9.29ms 구성
|구간|ms|원인|
|---|---:|---|
|**강조·공유 윤곽 LateUpdate**|**2.34**||
|└ 부품 수집 Collect (145회)|0.59|개체마다 부품 목록 순회·필터·bounds 계산|
|└ 마스크 메시·버퍼 구성 Record|0.56|매 프레임 부품 약 640개 정점을 다시 채움|
|└ Present 자체|0.49|개체 상태 관리·소스 목록 갱신|
|└ 구조 검사 SpriteReferences.Read|0.32||
|└ 출력 배치 Output / 정렬 Sort / 제출 Execute|0.15 / 0.08 / 0.14||
|**Prototype.Update**|**2.38**||
|└ 적 보행 애니메이션 (125회)|0.80|마리당 약 6µs|
|└ 적 루프 나머지|0.38|위치·체력바·버프 표식|
|└ 타워 동기화 (15기)|0.41|타워마다 적 위치 색인을 다시 만든다(`TowerTargetForView`의 Invalidate)|
|└ Tick 규칙|0.61|버프 0.14 / 타워 사격 0.14 / 투사체 0.17 / 스폰 0.09|
|**그림자 LateUpdate (145개)**|**0.86**|구조 검사 0.56 + 위치 갱신 0.24|
|**렌더 콜백 강조 밝기**|**0.61**|적용 0.44 + 복원 0.17 (머티리얼 교체 왕복)|
|컬링·스프라이트 준비|0.48|SpriteRenderer.PrepareNode 668회 0.29|
|UI Canvas 갱신|0.43|TMP 텍스트 생성 12회/프레임 0.14|
|GC 수거(증분)|0.27||
|Editor 비동기 작업 처리(ExecuteTasks)|0.25|프로젝트 코드에는 async가 없다. Editor 도구 쪽으로 추정되며 빌드에는 없음|
|기타 엔진·제출|약 1.4||

구조 검사(SpriteReferences.Read)는 강조와 그림자를 합쳐 0.88ms(290회)다.

## GC (프레임당 약 23KB)
- 피해 숫자 생성 4.5KB: 풀에 미리 만든 수가 3개라 초반에 TMP 오브젝트를 새로 만든다(`CreateDamageNumber` → AddComponent).
- HUD 피해 집계 행 생성 7.2KB: 타워가 처음 피해를 줄 때 행(Text)을 새로 만든다.
- 둘 다 전투 초반의 일회성 증가다. 계속 쌓이지는 않지만 초반 최저 FPS를 떨어뜨린다.
- 타워 사격 1.3KB, 타워 동기화 1.3KB: 매 프레임 작은 할당이 있다.
- Editor ExecuteTasks 4KB: 빌드 무관으로 추정.

## 참고
EditorLoop가 프레임당 11.5ms(녹화 포함)다. 그래서 Editor FPS는 게임 코드 시간보다 낮게 나온다. 빌드에는 이 비용이 없다.
