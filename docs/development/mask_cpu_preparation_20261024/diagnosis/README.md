# 출력 불일치 원인 분리 조사

> 후속 승인 실행: 제안한 격리 수정을 실제 두 사례8렌더로 검사해 자기/교차차이0·윤곽외활성Renderer0 확인. 제품원본복구. 전체512/성능은미검증. [격리검사 정본](../isolation_check/README.md). 아래 미실행 표기는 이 문서 작성 당시 상태이며 후속 실행 범위는 정본을 따른다.

2026-10-24. 승인: 사례320(정상)/321(실패), 원본 각2회·후보 각2회, 최대8회 렌더. Play 없음, 후보 로딩/원본 복구 재컴파일2회. 추가 성능 측정/제품 수정 없음.

## 확인한 사실

1. 실제8회 완료. 원본 자기 비교·후보 자기 비교·원본/후보 교차 비교 모두 두 사례에서 **다른 픽셀0·채널0·최대채널차0**. 렌더러/재질/텍스처/행렬/정렬/기하/카메라의 기록된 의미값 차이0. 객체 identity와 globalTime은 의미 비교에서 제외했으며 원자료에 보존했다. 따라서 이번 두 사례에서 후보 회귀를 재현하지 못했다. 전체512통과나 성능효과로 확대하지 않는다.
2. 지난321번 원본 hash는 `8F6ACA362FD5C8AC34C75126090E90C1CF1CB6811BD5AED452F376FD1018D28C`, 지난후보와 이번원본/후보는 모두 `1F5F3EBCD754989365AC4BCF56BBBF6AA3E66B72403D3188F53622602718452F`. **동일 원본 코드도 지난원본과 다른 출력**을 냈다. 지난 차이를 후보 변경의 인과회귀라고 판정할 수 없다. 이번 기록을 추가하는 행위도 getter 호출 등 관측 효과를 가질 수 있어 지난 상황 재현을 보장하지 않는다.
3. 확정된 검사 결함: PixelProbe.cs는 “original outline only”라면서 `List<SpriteRenderer>`와 `GetComponentsInChildren<SpriteRenderer>`만 껐다. CoreGuardianTowerGlow와 CoreGuardianTowerGrounding은 별도 MeshRenderer를 생성한다. 실제 단독 사례에는 TowerGlow4+TowerBaseShade1+SelectionOutline1, 겹침에는 각각2배가 활성 상태였다. 즉 목적 출력 외 **5개/10개**가 비교에 포함됐다. 원본 코드의 TowerGlow.shader는 Blend One One이며 원자료에서 alpha0/nonzeroRGB 픽셀도604/903개 확인했다. PNG를 보이는 모양만으로 판단하거나 alpha0 RGB를 일괄 무시해서는 안 된다.
4. 지난74건은 Single49(단독18/겹침31),Multi14(모두겹침),Support11(모두겹침)로 포탑에 한정됐다. 잔류 포탑 mesh와 일치하는 분포지만 **그것만으로 정확한 픽셀 차이의 인과원인을 입증하지는 못한다**. 동일 정렬값·z의 여러 투명 렌더러/재질의 정렬과 누적 연산은 조사 가설이다. 이번8회는 차이0이므로 차이를 유발한 특정 순서·상태를 증명하지 못했다.

## 판정

확정된 것은 **검사 격리 실패와 재현성 검증 부족**이다. 후보가 전체512에서 동등하다는 판정도, 후보가74건을 깨뜨렸다는 판정도 미확정이다. 지난 검사는 해시만 보존해 실제 차이 픽셀과 당시 renderer/material 상태를 복원할 수 없다. 이번8회가 모두 같았으므로 범위를 넘겨 반복하지 않았다.

## 구체적 수정안 — 미실행

`IsolatedPixelProbe_proposed.cs`에 최소 변경을 작성했다. mask를 준비한 뒤 body 아래의 **모든 Renderer**를 비활성화하고, 실제 활성 renderer 집합이 별도로 생성된 outline output 집합과 정확히 같은지 검사한다. 예상 밖 renderer가 하나라도 있으면 렌더 전에 실패한다. 제품·원본 아트·발광·그림자를 바꾸는 수정이 아니라 임시 fixture의 관측 대상을 분리하는 변경이다.

향후 실제 사용 전 raw RGBA·PNG·차이 픽셀·출력 상태 저장을 이 도구에도 결합하고, **원본 자기 비교를 통과한 뒤 후보 비교**해야 한다. 전체 합성 화면 검사는 별도로 정의하고 발광/음영을 포함했다고 표시해야 한다. 이 소스는 제안용이며 로드/실행하지 않았다. 새512비교·추가렌더·성능측정은 승인하지 않은 상태다.

## 증거

- `original.json`, `candidate.json`:각4렌더,mask/기하·카메라·렌더 전후 상태·raw경로.
- `original/candidate_*_320/321.rgba`와PNG:원본 픽셀. `comparison.json`, `Compare.ps1`:분석 재현.
- `comparison.png`, `difference_320/321.png`:원본/후보/차이. 차이는 검정(0)이다.
- `historical_mismatch_distribution.json`, `pixel_content.json`:기존74분포/투명RGB 통계.
- 최초 상태기록은 SpriteRenderer에없는MeshFilter를 C# ?.로 읽어 Unity fake-null 예외로 **카메라 렌더 전0건 종료**했다. null검사를 보정하는 도구오류 단회수정 후 실행. `logging_failure*.json`에 실패를 보존했다. 추가 카메라 렌더는 발생하지 않았다.

## 종료

두 제품 작업시작원본 hash동일·보호10파일동일,helper Assets밖으로재격리,복구컴파일성공. final_state.json: Play/changing/compiling/updatingfalse·씬clean7roots·Profiler/Deep/FrameDebuggerOFF·nativeBatchOFF·1920×1080. 빌드/실행파일/커밋/push없음. 기존사용자변경보존.
