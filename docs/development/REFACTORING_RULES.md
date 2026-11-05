# 리팩토링 규칙

2026-10-28 사용자 확정 방향. 코어 가디언 코드·셰이더 리팩토링에 적용한다. 기준 스킬: unity-perf `code-standards`(검토 절차·하드 규칙), `csharp-zero-gc`, `unity-performance`. 최적화가 섞이면 [OPTIMIZATION_RULES](OPTIMIZATION_RULES.md)를 함께 따른다.

## 범위
- 대상: `Assets/Scripts` 전체(Runtime·Editor·Tests), 우리 셰이더 `Assets/Art/Rendering/Shaders/Resources/CoreGuardian/*.shader` 12개와 테스트용 셰이더 2개.
- 제외: TextMeshPro 셰이더·cginc·hlsl(Unity 기본 복사본), `Assets/_Recovery/`, 씬·프리팹·CSV·아트·설정 파일.

## 절대 규칙 (어기면 그 묶음은 실패)
1. **동작 100% 동일.** 화면·동작·밸런스·소리·이벤트 순서·난수 사용 순서를 바꾸지 않는다.
2. **바꾸지 않는 이름·순서:** MonoBehaviour·ScriptableObject 클래스 이름과 파일 이름, 직렬화 필드 이름(인스펙터·씬 값), `Resources` 경로 문자열, 셰이더 이름·프로퍼티 이름·구조체 배치, CSV 열 이름, 오브젝트 이름(코드·테스트가 이름으로 찾음), enum 값의 이름과 순서(씬 배열 번호·경로로 쓰임), 테스트가 문자열(리플렉션)로 찾는 타입·필드·메서드 이름, 이벤트 구독 순서(`Start`의 `Configure` 호출 순서), `DefaultExecutionOrder` 값, static 필드 초기화 순서.
3. **버그·성능 문제는 고치지 않고 목록으로 보고**한다. 사용자가 고른 것만 따로 고치고, 성능은 측정 전후 비교(OPTIMIZATION_RULES).
4. 정리하면서 **새 메모리 할당을 만들지 않는다**(전투 중 매 프레임 경로). LINQ·문자열 합치기·람다 캡처를 새로 넣지 않는다.
5. 셰이더는 **출력 픽셀이 같아야** 한다(연산 순서·정밀도·키워드·패스 유지). 정리만 하고 계산식은 바꾸지 않는다.
6. 묶음마다 EditMode·PlayMode 전부 통과 후 커밋. 테스트를 고칠 때 검사 내용을 약하게 하지 않는다.
7. 안 쓰는 코드·파일 삭제는 목록으로 보고하고 사용자 승인 후에만.

## 코드 모양 (자동 정리 도구가 정본)
- 정리 순서: `dotnet format whitespace --folder --include Assets/Scripts/` → `dotnet format style <csproj> --diagnostics IDE0011`(중괄호) → `dotnet csharpier format Assets/Scripts`(줄 나눔, `.csharpierrc.yaml`). 도구는 `.config/dotnet-tools.json`(프로젝트 전용, `dotnet tool restore`).
- 정리 후 확인: IL 지문(`Logs/claude_fp2.cs`, 게임 어셈블리 3개)이 변경 전과 같아야 서식만 바뀐 것이다.
- 들여쓰기 4칸, 중괄호는 새 줄, 쉼표·연산자·키워드 뒤 띄어쓰기.
- 한 줄 120자 이하, 한 줄에 문장 하나.
- `if`·`for`·`foreach` 본문은 중괄호. 단 멈춤 문장(`return`·`continue`·`break`) 하나뿐이면 중괄호 없이 다음 줄에 들여 씀(자동 정리 도구 방식).
- 파일: UTF-8 BOM 없음, 줄바꿈 LF.
- `using`은 맨 위, 안 쓰는 것 제거. 네임스페이스는 지금 상태를 유지한다(전투 규칙 8개 파일만 `CoreGuardian`, 나머지는 전역). 테스트가 타입 이름 문자열로 찾기 때문에 네임스페이스를 옮기면 테스트가 깨진다. 파일 범위 네임스페이스는 쓰지 않음(C# 9).

## 이름
- 타입·메서드·속성·이벤트·상수: PascalCase. private 필드·지역 변수·매개변수: camelCase(밑줄 없음, 현재 방식 유지). 인터페이스는 `I` 접두.
- 단위가 있는 값은 이름에 단위를 붙인다: `Seconds`, `Tiles`, `Px`, `Ratio`.
- bool은 `is/has/can` 또는 상태 형용사(`Alive`, `Frozen`).

## 구조
- 한 함수 50줄, 매개변수 4개, 중첩 3단을 넘으면 나눈다(기본값).
- 한 클래스는 한 문장으로 설명되는 책임 하나. 큰 파일은 `partial` 또는 역할별 클래스로 나눈다(공개 이름은 유지).
- 의존 방향: Domain(규칙) → Presentation(화면) 참조 금지. 화면은 규칙의 이벤트·읽기 전용 값을 구독한다.
- 숫자는 이름 있는 상수로. 기획 수치는 CSV가 정본이므로 코드에 남은 기획 수치는 목록으로 보고(옮기는 건 데이터 작업).
- 빈 `catch` 금지. 실패를 무시하면 이유를 주석으로.
- `UnityEngine.Object`에 `?.`·`??` 금지(파괴된 오브젝트 판정이 틀림) → `!= null` 비교, 또는 `.OrNull()`(CoreGuardianUnityObjectExtensions)을 거친 뒤에만 `?.`·`??`.
- 전역에서 쓰기 가능한 static·public 필드 금지(읽기 전용 속성으로).
- 전투 중 매 프레임 경로의 반복·중첩 함수는 주석으로 복잡도와 N의 최대치를 적는다(예: `O(적 수), 최대 약 150`).
- 주석은 '왜'를 적는다. 공개 API에는 요약 주석.

## 셰이더
- 프로퍼티 블록·패스 순서 유지. 공통 코드가 겹치면 `.hlsl`로 묶을 수 있으나 결과 동일 확인(픽셀 비교 테스트) 후에만.
- 이름 규칙: 프로퍼티 `_PascalCase`, 지역 변수 camelCase, 상수 대문자 금지(PascalCase).

## 진행
1. **읽기:** code-standards 검토 절차대로 4관점(데이터·이벤트 계약 / 수명·비동기 순서 / 에셋·설정 증거 / 모듈 간 일관성·규칙)을 나눠 한 줄씩 읽고, 독립 최종 검토로 거짓 지적을 걸러 목록(P0~P3)을 만든다.
2. **보고:** 리팩토링 목록과 버그·성능 목록을 따로 보여주고 고르게 한다.
3. **수정 묶음:** ① 도구 설정 ② 전투 규칙(Domain) ③ 렌더링·셰이더 ④ 캐릭터·전투 표현 ⑤ HUD·UI ⑥ 게임 시작·입력(Bootstrap) ⑦ 편집기 도구·테스트. 묶음마다 테스트·커밋, 노션 기록.
