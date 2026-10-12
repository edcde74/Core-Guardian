# 로컬 Git 작업과 복구

2026-10-13 구성. CD 루트의 로컬 저장소를 사용하며 GitHub 연결은 하지 않는다.

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

복구 사본을 Unity로 열면 Library는 다시 생성된다. 원본 폴더에 `reset --hard`, `clean`, `restore`를 임의로 실행하지 않는다. 원격 저장소가 없는 로컬 커밋은 PC 고장에 대비한 외부 백업을 대신하지 않는다.
