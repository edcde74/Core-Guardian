# GitHub 도구 후보와 Core Guardian 적용 판단

확인:2026-10-22 Asia/Seoul. 사용자 요청으로 공개 저장소 README/문서와 일부 패키지 자료를 검색·조회했다. **설치/프로젝트 연결/코드 반입/벤치마크는 아직 없다.** 현재 병목 정본은 [RESULTS.md](RESULTS.md), 진행 설계/계획은 [DESIGN.md](DESIGN.md)/[PLAN.md](PLAN.md).

## 우선 후보

|후보|저장소에서 확인한 기능|현재 프로젝트 적합성 판단|판정|
|---|---|---|---|
|[Barnaff/UnityRuntimeAtlas](https://github.com/Barnaff/UnityRuntimeAtlas)|런타임 atlas 생성, Skyline/MaxRects/Guillotine/Shelf, 다중page, non-readable texture 지원, SpriteRenderer/Image/RawImage 연결을 README에 명시|A2의 texture packing/페이지 관리 참고 후보. 실제 동적cutout UV·원본PPU/Point·수명 검증과 Unity6000.6/URP17.6 호환은 미검증. B의 매frame GPU mask생산/정렬배칭을 해결한다는 근거는 없음|가장 직접적인 기능 후보지만 바로 설치 권장하지 않음|
|[Unity-Technologies/brg-shooter](https://github.com/Unity-Technologies/brg-shooter)|공식 BatchRendererGroup 직접 사용/Burst/JobSystem 샘플. README는Unity2022.3.5이상 요구|대량제출/instance data/명령분리 구조 참고. 현재 SpriteRenderer/SortingGroup/투명정렬·개체별union mask를 그대로 교체할 수 있는 drop-in 도구가 아님. 기존2D경로를 BRG로 전환하는 비용/회귀가 예상됨|공식 설계 참고 우선, 패키지 도입 보류|
|[Unity-Technologies/ProjectAuditor](https://github.com/Unity-Technologies/ProjectAuditor)|assets/settings/scripts의 정적 분석 보고서. 현재GitHub README가 해당버전 지원중단과 최신패키지 문서로의 이동을 명시|설정/코드/asset 문제를 찾는 보조진단이며 Draw/SetPass를 자동으로 묶는 renderer가 아님. 사용할 경우 GitHub옛버전보다 현재Editor지원 공식패키지/내장버전 확인이 먼저|추가진단 후보, 자동설치 없음|
|[mob-sakai/ParticleEffectForUGUI](https://github.com/mob-sakai/ParticleEffectForUGUI)|ParticleSystem mesh를CanvasRenderer로 출력. 추가Camera/RenderTexture/Canvas 불필요, mesh sharing, URP/HDRP 지원을 README에 명시|UI particle을 많이 표시할 때 유효한 후보. 현재 주요비용인 본체SpriteRenderer와 개체별silhouette/contact 출력은 적용대상이 다름. 월드이펙트를 UI로 옮기는 변경도 본승인에 포함되지 않음|UI particle 병목이 입증된 경우만 별도검토|

기능 설명은 제작자가 README에 명시한 범위이며 우리 게임에서 효과를 실측한 결과가 아니다. 샘플이나저장소의 성능수치를 우리게임 예상절감률로 사용하지 않는다.

## 호환성·유지보수·라이선스 확인 범위

- RuntimeAtlas 저장소 페이지에는105commits/1star/0fork 및 MIT 표시가 보였다. 별수만으로 품질을 판단하지 않으며 대규모 검증/상용 안정성의 근거로 쓰지 않는다. package.json·CHANGELOG·LICENSE 상세 조회와 일부 Runtime 폴더 조회는 웹 cache 오류로 실패했다. **정확한 최소 Unity 버전·의존 패키지·최근 commit 날짜·라이선스 원문은 미확인**이므로 “현재 버전에서 검증된 도구”라고 추천하지 않는다. README 사용 예에 Bilinear/repack이 포함돼 원문을 그대로 복사하지 않는다. 도입한다면 Point/원본 pixel·재배치 시 sprite 참조 유효성·재시작 release를 먼저 시험한다.
- brg-shooter의 공식 README와 Unity 설명은 확인했다. 설치/샘플 실행·우리 URP17.6 호환성 검증과 현재 license 원문 확인은 하지 않았다. Shader/DOTS 인스턴싱/데이터 수명 전환이 필요한지 소스 심화 검토가 먼저다.
- ProjectAuditor는 GitHub README의 지원 중단 표시를 확인하고 [현재 안내 문서](https://docs.unity3d.com/Packages/com.unity.project-auditor@0.10/manual/index.html)도 조회했다.0.10이 우리 Editor의 최신/권장 버전이라고 확정한 것은 아니다. 향후 설치 시 패키지 레지스트리·현재 Editor 내장 여부·규칙 package를 공식 Unity 패키지 스킬로 확인한다. GitHub README는 Unity Package Distribution License를 명시한다.
- UIParticle README는 기능/설치 방법/mesh sharing을 확인했다. 현재 최신 릴리스 날짜/우리 프로젝트의 UI 이펙트 부하는 검증하지 않았다. 다른 파티클 도구와의 호환성까지 보장하지 않는다.

## 기타 후보의 범위

- [Unity 공식 Entities Graphics 샘플](https://github.com/Unity-Technologies/EntityComponentSystemSamples/tree/master/GraphicsSamples)은 URP/HDRP 샘플 폴더까지 확인했다. ECS 전환은 현재 문제의 필수 해결책이라는 근거가 없어 A/B 기본 계획에 추가하지 않는다.
- [Stefaaan06/Unity-Mesh-Combiner](https://github.com/Stefaaan06/Unity-Mesh-Combiner), [dawid-t/Mesh-Combiner](https://github.com/dawid-t/Mesh-Combiner)는 일반 Mesh 합치기 후보다. 현재 개체별 보행/부분 sprite·투명 순서·mask/효과 수명을 유지하는 도구라는 검증이 없다. 정적 mesh용 결합을 움직이는2D 전체에 일괄 적용하지 않는다.
- [dexsper/TextureArrayPipeline](https://github.com/dexsper/TextureArrayPipeline)은 검색 결과상 URP texture array/vertex slice/mesh baker 후보다. 전체 소스 검증이나 현재 Unity 호환 검증을 하지 않았다. 현재 이미 있는 mask Texture2DArray 경로와 역할이 겹칠 수 있어 추가 의존성으로 채택하지 않는다.

## 추천

현재는 **A/B를 기존 API 안에서 구현하고, RuntimeAtlas의 packing/페이지 관리와 Unity 공식 BRG의 데이터·제출 분리를 참고**하는 순서가 가장 직접적이다. 외부 도구를 검증할 때도 기존 패키지에 즉시 추가하지 않고 호환성/라이선스·픽셀 동등성·실제 Draw/SetPass·CPU/메모리 비용을 같은 조건으로 확인한다. 이 판정은 현재617개 FrameDebugger 이벤트/개별 mask145개·출력274개라는 프로젝트 관측에 기반한 적합성 추론이다.
