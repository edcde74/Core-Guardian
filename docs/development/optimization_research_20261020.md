# 최적화 공식 자료·게임 사례 1차 조사

2026-10-20. 문서/검색으로 확인한 범위이며 게임 코드·설정 변경·Unity 조회/새 Play·패키지 설치·빌드/실행은 없다. 공통 계약은 [OPTIMIZATION_RULES](OPTIMIZATION_RULES.md)다. 조사 결과는 적용 후보이며 효과 보장이 아니다.

## Unity 공식 유튜브
공식 Unity 채널의 제목·게시자·공개 설명을 검색 결과에서 확인했다. 직접 영상 열기는 오류로 본문/자막을 확보하지 못했다. 전체 시청/실습 완료는 아니며 구체 기법은 아래 공식 문서로 교차 확인했다. 영상에만 있는 세부 주장은 보류한다.
- [Boosting your game performance with Unity 6 Profiling tools — Unite 2024](https://www.youtube.com/watch?v=_cV1B2hqXGI), Unity/Peter Hall, 2024-10-09. 설명상 Timeline의 고비용 호출, Profile Analyzer의 느린 프레임 비교, Memory Profiler의 할당/중복 분석을 다룬다. 프로젝트에는 병목 분리와 전후 캡처 비교가 연결된다.
- [Optimizing smarter, not harder with Unity's performance tools — Unite 2025](https://www.youtube.com/watch?v=S4xF-eE1pBg), Unity/Peter Hall, 2025-12-16. 설명상 Unity6.3의 Project Auditor·Profiler Overview와 도구 연계를 다룬다. 현재 기록 Unity6000.6.2f1과 도구 버전/설치 상태의 호환성은 적용 전에 확인한다. 도구를 자동 설치하지 않았다.

## 교차 확인한 공식 문서
- [Profile Analyzer 전후 비교](https://unity.com/how-to/optimize-your-game-unity-profile-analyzer): 반복 가능한 같은 구간을 캡처하고 두 데이터셋을 비교하며 원본 Profiler 캡처를 보존하는 접근. 단일 프레임만으로 효과를 판단하지 않는 근거.
- [Unity6.6 풀링/재사용](https://docs.unity.com/en-us/engine/6000.6/manual/analysis/performance-memory/managed-memory/optimizing-code-managed-memory/reusable-code), [Unity Learn 풀링 예제](https://learn.unity.com/tutorial/use-object-pooling-to-boost-performance-of-c-scripts-in-unity?version=2020.3): 반복 생성/삭제 대신 재사용 검토. 오래된 Learn 예제는 개념 참고이며 실제 API는 현재 버전으로 확인한다. 코어 가디언은 피해 HUD/탄환/피드백 생성 비용과 풀의 메모리 절충을 각각 측정할 후보.
- [Unity UI 최적화](https://create.unity.com/Unity-UI-optimization-tips): Canvas·Layout·UI 풀링을 조사할 출발점. Canvas 분리/레이아웃 재계산은 실제 프로파일과 계층을 보고 결정한다.

## 실제 게임 사례
- [Skul: The Hero Slayer — Unity/SouthPAW 사례](https://unity.com/resources/skul-the-hero-slayer-case-study): CPU 프로파일링과 Sprite Atlas, Tilemap/Collider 통합, 픽셀 선명도 유지 사례. 우리 게임에는 아틀라스/재질 전환과 렌더 비용을 조사하는 근거로 연결한다. 기존 PPU·원본 재제작·물리 정밀도/효과 빈도 변경은 채택하지 않는다. 글에 있는 품질 절충을 이 프로젝트의 승인으로 가져오지 않는다.
- [V Rising PS5 이식 — Unity/Stunlock 사례](https://unity.com/blog/5-key-learnings-v-rising-playstation-5-port): 네이티브 프로파일링과 플랫폼별 병목 조사, ECS/Burst/Jobs 활용 사례. 우리 게임에 ECS 전환이 필요하다는 증거는 아니다. 현재 병목의 위치와 이전 비용부터 확인하고 렌더 통합/탐색 최적화와 비교한다.

## 다음 조사·적용 후보
1. HUD 행 재사용/참조 캐시: 확인된 전행 재생성 문제를 격리해 전후 비교.
2. 탄환/숫자/피격/금화 풀링: 각각 생성률·생성/반환 CPU·할당·활성 수·풀 메모리 계측 후 범위 선택.
3. Unity 설정/임포트/렌더: 현재 설정을 먼저 덤프하고 아틀라스/재질/배칭 적합성·Canvas 비용을 확인. 설정 이전값→이후값과 시각 동등성을 함께 기록한다. VSync/FPS제한 조정으로 숫자만 바뀐 것을 작업량 절감으로 보고하지 않는다.
4. Session 탐색/임시 할당: 결과·이벤트 순서를 유지하는 범위에서 별도 검증.

현재는 설정/풀링의 적용 여부 전체를 감사하지 않았다. 사용자의 미적용 인식을 근거 없이 전체 현황 확인 완료로 바꾸지 않는다. 렌더/코드/설정 어느 후보도 이번 조사에서 적용하지 않았다.
