# 발밑 동적 접촉 그림자 제거 · 2026-10-23

- 복구 기준 HEAD `21857ff`. 변경 전 해당 소스/테스트 추적 diff 없음. 사용자 씬·설정·미추적 파일은 보존한다.
- 사용자가 `Logs/CG_ContactPreview_20261023/before.png` / `without-contact.png` 실제 동일 프레임 확대 비교를 보고 영구 제거 승인. 본체/원본 발 도트/애니메이션/넓은 투사 스프라이트는 유지한다.
- `CoreGuardianPhotoShadow`의 PhotoDynamicContact 생성·공유 접촉 재질 lease·프레임별 접촉 마스크 갱신·불필요한 Camera.main 조회 제거. PhotoCastShadow 위치/스케일/정렬/기존 중첩 억제는 유지. 공개 Present(Camera) 호출 계약 유지. 다른 선택/피격/강화 마스크와 공통 API·셰이더 리소스는 건드리지 않는다.
- 제거 회귀 기대값으로 그림자 테스트를 먼저 변경: [RED](red.json) 10건 중 제거 대상5건 예상 실패. 구현 후 [GREEN](green.json) 10/10, [전용 마스크 재질 미생성/기존 공유재질](materials.json) 2/2 통과. 프리팹으로 플레이어/공격/방어/버프/보스/코어/포탑3종 범위 확인.
- [자체 실제 Play](live.json): frame1421, PhotoDynamicContact0, 플레이어/코어 PhotoCastShadow visible=true. 사람 조작·장시간 전투 검증은 아니다. 종료 Play stopped/compiling false/scene dirty=false, Profiler recording=false/driver=true/background=false 복구. 씬 저장·빌드·실행파일·push 없음.
- 범위 한정 독립 코드 리뷰 actionable findings 없음. diff --check 통과.
- 이번은 승인된 표시 제거이며 성능 재측정 없음. CPU/GPU/최저·최고·평균 FPS/메모리 절감량 N/A. 이전 128~129적·15타워 표본의 Contact4.579ms는 과거 관측치이며 이번 절감량으로 간주하지 않는다. 정상30R/저사양60FPS/장시간 잔류 검증은 미완료다.
