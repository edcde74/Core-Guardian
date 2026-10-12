# 스킬 검증 · 2026-10-13

- 스킬: `docs/skills/core-guardian-hud-ux/`. skill-creator initializer로 생성, 한국어 UI 메타 작성, SKILL.md와 소스/지표 참고 문서 작성. `quick_validate.py` 실제 실행 결과 `Skill is valid!`.
- 독립 사용 시험: `hud_skill_forward`가 부모 보고서 없이 스킬·최신 소스·노션 원문 사본·과거 사진 2장으로 7항목 분석/배치안 3개를 작성. 읽기 전용·피해/스펙/시간 구분·통합 전투력 유보·노션/구현 차이·현재/과거 자료 구분을 지켰고 필수 지시 누락을 발견하지 못함. 사람 UX 검증이나 게임 회귀 통과를 뜻하지 않음.
- 별도 코드 감사 `hud_code_audit`는 초기 소스를 읽었으며 그 뒤 다른 작업이 증강을 추가함. 부모가 최신 소스를 다시 읽고 증강/보스 구현 존재를 정정. 초기 감사의 ‘미구현’ 결론을 최종 보고서에 적용하지 않음.
- Unity: 읽기 API로 열린 씬/상태 확인. `ReadHudSnapshot.cs` dry-run diagnostics 없음, actual 실행 성공. 현재 Canvas/모든 자식 RectTransform·Text·raycast·활성 여부를 `unity_snapshot.json`에 저장. 새 GameObject/에셋 생성·화면 상태 변경 없음. 다른 작업이 켠 Play 중 읽었고 새 Play를 시작/정지하지 않음. 마지막 상태 조회는 stopped/compiling=false. 선택 증강 패널은 관측 당시 비활성이라 실사용 검증은 남음.
- 화면: 기존 1280×720 자연 전투/역할검증 PNG 2장을 직접 확인. 직접 새 캡처는 Play가 종료돼 얻지 못했지만, 마무리 중 다른 작업이 저장한 `augmentation_selection_20261013.png`를 직접 열어 최신 증강 화면을 확인. 강제 처치 진단 조건과 해당 작업의 클릭/회귀 기록을 연결하고 이 분석의 직접 조작/사람 검증과 구분. 과거 화면을 현재 증강 배치 증거로 쓰지 않음.
- 개인 설치: `C:/Users/edcde/.codex/skills/core-guardian-hud-ux`에 새 폴더로 복사, 기존 폴더 덮어쓰기 없음. 설치 경로에서도 `quick_validate.py` 결과 `Skill is valid!`.
- 정적 배치: `layout_audit.py` 6해상도 계산·4모델 검사 통과. 주 패널 면적은 부분 사각형 합으로 제한. 실제 리사이즈/글자 판독/보드 가림률 실측 아님. 최신 소스 SHA는 결과 JSON에 기록.
- 코드 증거: `snapshot_sources.py`는 분석 디렉터리에만 현재 소스 6개 원문/SHA/관측 시각을 저장. 스냅샷 밖 동시 변경까지 검증했다고 주장하지 않음.
- 와이어프레임: `render_wireframes.py`로 기존 경로와 위치를 참고한 새 개념도 PNG 3장 작성. 게임 화면 픽셀/원본 이미지 수정 없음. A안 직접 열어 한글/레이아웃 확인. 승인된 UI 구현 아님.
- 재실행: 프로젝트 루트에서 `C:/Users/edcde/anaconda3/python.exe -X utf8 docs/development/hud_ux_analysis_20261013/layout_audit.py`. 검증/자료 수집 스크립트는 Assets 밖이며 런타임 게임에 포함하지 않음.
- 게임/노션/UI/씬/프리팹 변경, 게임 테스트·새 Play 실행 없음. 문서와 스킬만 작성. 기존 사용자/다른 작업의 미커밋 게임 변경을 보존.
