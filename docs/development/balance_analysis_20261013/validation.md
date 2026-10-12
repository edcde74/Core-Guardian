# 밸런스 스킬 제작·검증 — 2026-10-13

## 산출물과 권한

- 원본: [core-guardian-balance](../../skills/core-guardian-balance/SKILL.md). 설치: `C:/Users/edcde/.codex/skills/core-guardian-balance`.
- 구성: SKILL.md, UI 메타데이터, 자료 위치/계산 방법 references2개, 정적 분석 Python1개. 기본 자동 선택 허용. 호출: `$core-guardian-balance 1~10라운드의 초반 성장과 첫 증강 전 지루함을 분석해 줘.`
- [시험 보고서](report.md): 요청6항목 순서, 해결안3개와 장단점/추천/사람 플레이 검증, 확인·미확인 자료 포함.
- [노션 조회 사본](notion_sources.json), [계산과 출처](static/rounds.json), [성장](static/growth.json), [표](static/rounds.csv), [그래프](static/pressure.svg).
- 게임 데이터/코드/씬/프리팹/아트/노션 변경 없음. Assets 밖 분석 파일·스킬·필수 인수인계만 작성. 새 Play 실행·강제피해·수급주입·게임 회귀 테스트 없음.

## 실제 검증

1. skill-creator initializer 사용. 한국어 UI 메타데이터의 기본 로컬 인코딩을 발견해 UTF-8로 재생성. bundled Python에는 PyYAML이 없어 사용자 Anaconda Python의 `-X utf8`로 bundled quick_validate.py 실행. 원본과 설치본 모두 `Skill is valid!` 확인.
2. analyze_rounds.py를 현재5종CSV에서 실행. 1~9R 처치골드510/완료수정205,9R HP635/마지막생성22.8,10R 누락null 확인. 원본 SHA256을 manifest에 저장. 시뮬레이션/실측이 아니며 미구현10R 값을 만들어 넣지 않음.
3. [verify_analysis.py](verify_analysis.py)9개 행동 검사 통과. 임시 원자료 복사에서 정적 합계·능력치별 집중/분산가격·반올림/방어100이상0·처치발수·10R누락·기존출력덮어쓰기차단·게임/설정/Git/캐시폴더쓰기차단·스키마변경시실패·원본CSV해시불변 확인. 실제 게임 테스트 통과와 구분.
4. 독립 읽기 전용 에이전트에 스킬+최소 원자료와 같은 현실 요청을 전달, 부모 결론/보고서는 제공하지 않음. 노션은 이번 조회 사본 사용. 에이전트 Unity연결은 인스턴스 미발견으로 실패했으며 정적/실행 차이를 보고. 부모는 MCP ready/씬/Play/compile/save/time을 따로 확인.
5. 독립 결과는6항목·근거유형·강한빌드유지·미구현/미측정을 구분. 증강 효과식만 맞고 실제 획득 불가인 복수등급 재선택 예시 문제 발견. 예시를 실버1회+플래티넘1회36DPS 가정으로 교체하고 카드풀/후보고갈/기회수 검사 명시. 재검증에서10R직후두카드오인방지·재선택검사보완 통과, 추가 필수 수정 없음.
6. SVG는 정적 CSV에서 생성,10R N/A. XML/표데이터 검사. PNG는 Anaconda Matplotlib/NumPy 호환 실패·bundled Matplotlib 없음으로 생성하지 않고 SVG 유지. 런타임 패키지를 수정/설치하지 않음.
7. 최종 재검사: 설치본5파일과 원본 바이트 일치, 한국어 UI YAML UTF-8/호출 프롬프트 확인, Python AST/SVG XML 파싱 통과, 실행 CSV의 현재 SHA256과 보고서 manifest 일치. 처음 파일 수 검사를6으로 잘못 기대해 실패했으나 실제 구성5개로 바로잡아 통과. 새 Unity 조회에서도 ready/stopped/compiling=false/dirty=false 유지.

## Unity 조회와 미검증

CD,Unity6000.6.2f1,ready,compiling=false,domainReload=false,Play stopped. 전투 씬 loaded/active/dirty=false, timeScale1. CSV5종 씬참조와 로딩 경로는 정적 확인, 새 Play 메모리값은 측정하지 않음.

console_status 조회에서 현재 오류3/경고52였으며 후속console(error,tail10)은 중복 글로벌 조명2건(BuildWindowsPlayer 호출 기록)·메인스레드API타임아웃1건을 반환. 후속 응답 현재groundTruth는 오류0/경고0/컴파일실패false. 과거/현재 스냅샷을 구분하며 로그를 지우거나 빌드/오류를 수정하지 않음. 동시 진행된 Windows 빌드의 인수인계/커밋을 끝에 다시 확인해 보존했으며 해당 작업 검증을 이 분석의 실적으로 계산하지 않음.

기존 정상API 자동실행은2026-10-10,5배속·준비스킵·정책별1회. 사람1배속체감·반복통계·최적배치·10R/증강실전·밸런스/아트승인은 미검증. 후속은 보고서6항목 참조.

## 복구·재실행

시작 시 사용자 계정Git상태clean, 복구 기준HEAD `db8abec`. 샌드박스 계정은 벤더파일 접근 실패를 삭제처럼 표시해 사용자 계정의 읽기 전용status로clean을 확인했다. 작업 중 별도 Windows 빌드 커밋 `c2d16f0`과 인수인계 갱신을 발견, 최신 내용을 다시 읽고 보존. 사용자 변경을 되돌리지 않음. 완료 시 이 작업 문서만 로컬 커밋,GitHub/push없음.

```powershell
& 'C:/Users/edcde/anaconda3/python.exe' -X utf8 C:/Users/edcde/.codex/skills/.system/skill-creator/scripts/quick_validate.py docs/skills/core-guardian-balance
& 'C:/Users/edcde/anaconda3/python.exe' -X utf8 docs/development/balance_analysis_20261013/verify_analysis.py
# 새 출력 폴더만 사용
& 'C:/Users/edcde/anaconda3/python.exe' -X utf8 docs/skills/core-guardian-balance/scripts/analyze_rounds.py --project . --through 10 --out docs/development/balance_analysis_new_run
```
