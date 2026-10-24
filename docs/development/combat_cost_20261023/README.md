# 적128·타워15 비용 구분

복구 기준 1de8ced. 제품 코드/씬/설정 변경 없이 진단 소스만 메모리 컴파일한다. A 보류/B 제한·원본 출력/fallback·SpriteAtlas 보류 유지.

예정: 동일 진단 Session 입력(기존 FrameProbe), 1920x1080/ortho8.5/time1, warm15. 일반 CPU/Rendering/Memory/UI/Audio 캡처90프레임1회로 렌더·마스크 구분. 미표식 포탄·효과 메서드가 구분되지 않으면 Deep30프레임1회 추가. 개선 전후 실험이 아닌 비용 귀속 조사이므로 조건당3회 반복 대신 작은 단회 진단이며 절감량/율 N/A. Deep 결과와 일반 FPS를 직접 비교하지 않는다. 변경의 효과/목표기기60FPS 판정 없음.

기존 도구의 ProfilerRecorder.ToArray/CopyTo 경로를 사용하지 않는다. Profiler 원시 frame view로 CPU 호출과 self/inclusive를 구분한다. 종료 시 자체 Play와 자체 Profiler 설정만 복원한다. 사용자 미추적·폰트 도구 접근오류 보존.
