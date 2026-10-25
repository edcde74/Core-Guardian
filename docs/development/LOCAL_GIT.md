# 로컬 Git 작업과 복구

2026-10-13 구성. CD 루트의 로컬 저장소를 사용한다. 2026-10-24부터 GitHub origin(https://github.com/edcde74/Core-Guardian)이 연결되어 있다. 업로드는 사용자가 명시 요청할 때만 아래 "GitHub 업로드" 절대로 한다.

## 추적 범위

- `Assets`와 모든 `.meta`, `Packages`, `ProjectSettings`, 개발 문서·검증 기록·원본 아트 시안.
- `.gitignore`는 Unity 캐시, 임시 파일, 로컬 에디터 설정, IDE 생성 파일, 빌드를 제외한다. 제외는 파일 삭제가 아니다.
- `HUDDesigner`는 기존 별도 저장소이므로 CD 저장소에서 제외한다. 내부 파일과 Git 설정은 그대로 보존한다.
- 로컬 `core.autocrlf=false`로 초기 커밋의 파일 바이트를 보존한다. 새 줄바꿈 정책은 별도 검토한다.

## 주요 기능 수정 전후

프로젝트 폴더의 PowerShell에서 실행한다. 커밋 전에는 Unity에서 저장을 마치고 컴파일·Play·저장 상태를 확인한다. Git은 디스크에 저장된 파일만 기록한다.

```powershell
git status --short
git diff --stat
# 수정 전: 기존 변경 내용을 검토하고 포함할 파일만 지정한다.
git add -- <파일 또는 폴더>
git diff --cached --stat
git commit -m "Checkpoint before <기능명>"

# 수정 후: 관련 검증과 인수인계 갱신을 마친 뒤 저장한다.
git add -- <변경 파일 또는 폴더>
git diff --cached --stat
git commit -m "Implement <기능명>"
git status --short
```

변경이 없으면 수정 전 빈 커밋을 만들지 않고 현재 HEAD를 복구 기준으로 기록한다. 사용자의 미커밋 변경을 발견하면 내용과 소유 범위를 확인하고 보존한다. 검증하지 않은 상태는 완료로 표시하지 않는다.

## 복구와 비교

```powershell
git log --oneline -10
git show --stat <커밋ID>
git diff <수정전커밋> <수정후커밋> -- Assets
# 현재 프로젝트를 건드리지 않는 별도 복구 사본 (비어 있는 새 경로 사용)
git worktree add --detach ..\CD_Recovery <커밋ID>
```

복구 사본을 Unity로 열면 Library는 다시 생성된다. 원본 폴더에 `reset --hard`, `clean`, `restore`를 임의로 실행하지 않는다. GitHub에 올리지 않은 로컬 커밋은 PC 고장에 대비한 외부 백업이 아니다.

## GitHub 업로드 (2026-10-24 구성)

- 원격: `origin` = https://github.com/edcde74/Core-Guardian , 원격 `main`은 2026-10-24 기준 `2092e20`이고 로컬 `github-main` 브랜치와 같다.
- **GitHub main은 로컬 작업 브랜치(`codex/shared-body-mask`)와 기록이 다르다.** 처음 올릴 때 `.git`이 8.6GB(과거 `.rgba` 원본 캡처 다수)였고, 100MB 넘는 프로파일러 파일 14개를 LFS 포인터로 바꿔 기록을 새로 썼기 때문이다. 그래서 **로컬 브랜치를 그대로 push 하지 않는다.**
- 올리는 방법: `github-main`을 부모로 삼아 로컬 HEAD의 트리를 새 커밋으로 만들고 그 커밋을 `origin main`에 push 한다.
  - LFS 경로 14개는 원격(github-main)의 포인터 파일을 그대로 쓰고, `.gitattributes`도 원격 것을 쓴다.
  - 새로 생긴 `.rgba`/`.r32` 원본 덤프는 넣지 않는다(`.png`·요약·JSON만).
  - `.gitignore`가 `docs/development/**/*.data`, `/PerfLogs/`, `/Builds/`를 제외한다.
  - 2026-10-24에 쓴 스크립트(`Temp/cg_pub.cmd`, `cg_push.cmd`)는 Unity 재시작으로 없어졌다. 2026-10-25부터 Temp 밖 스크립트를 쓴다(Git Bash, 프로젝트 루트):
    1. `bash docs/development/git_publish/cg_build_publish.sh` — 업로드 커밋을 만들고(push 안 함) `PUBLISH_SHA`와 github-main 대비 diff, `.rgba/.r32/.data/.zip` 포함 여부를 출력한다. 로컬 브랜치·작업 폴더·인덱스는 건드리지 않는다(임시 인덱스 사용).
    2. 출력 확인 후 `bash docs/development/git_publish/cg_push.sh <PUBLISH_SHA>` — origin main에 push하고 github-main을 옮긴다.
  - Unity MCP `eval`은 메인 스레드 5초 제한이 있으므로 git 작업은 cmd를 백그라운드로 실행하고 로그 파일로 확인한다.
- 올린 뒤 `github-main`을 새 원격 main으로 옮겨 다음 업로드의 부모로 쓴다.
