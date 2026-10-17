# 몬스터 피격 종료 후 밝기 변화 수정

2026-10-18 (Asia/Seoul). 사용자 사진에서 피격한 몬스터가 다른 개체보다 어두워지는 현상을 확인했다. 복구 기준 `b5f8733`. B②/Inspector 설정을 유지하고 피격 종료의 렌더 상태 복원만 수정했다.

## 원인과 변경

- 처음 사용자 Play는 paused라 중단/재개하지 않고 읽기만 했다. 현재 프레임에서 생존 Attack3의HP14와 나머지HP27, 실제 본체색RGB1·피격강도0·원래Unlit재질 복귀를 확인했지만 피해받은 개체에만 속성블록이 남아 있었다. 이후 Play 중지 상태를 재조회한 뒤 자체 진단을 진행했다.
- 기존 `CoreGuardianHitFlash.Clear`는 원래 속성블록이 없었던 렌더러에도 빈 `MaterialPropertyBlock`을 넣었다. 값이 없어도 `Renderer.HasPropertyBlock()`이true로 남는 상태는 ‘블록 없음’과 렌더링 결과가 달랐다. 새 카메라 시점 본체RGB강조와 함께 쓸 때 피격 뒤 밝기 보강이 달라졌다.
- `Trigger`에서 원래 `HasPropertyBlock()` 여부를 렌더러별로 저장하고 `Clear`에서 원래 없었으면 `SetPropertyBlock(null)`, 있었으면 저장한 블록을 그대로 복구한다. 기존 사용자/다른효과의 비어있지않은 속성값은 보존하며 명시적빈블록이 원래있던 경우도 유지한다.
- 기존0.09초/최대22%/재점등제한0.1초·방향별몸체·원본/재질/색·그림자·B②윤곽/밝기/초록선택선/HUD/카메라/전투수치는 변경하지 않았다. 씬/원본아트/Inspector 저장값 변경 없음.

## 실제 진단과 검증

- [수정 전 비교](before_fix/comparison.png): 같은 세션/같은 자연 출현 몬스터/같은 자세에서 피격shader를 직접 Trigger→Advance(.12)한 진단이다. TimeScale0·게임어댑터일시비활성으로 포즈를 고정하고 실제 합성GameView1920×1080을 촬영했다. HUD 포함, 임시줌/재화/몬스터/HP/피해주입 없음. 자연 투사체 명중 검사가 아니라 표현 수명주기 직접호출 검사다.
- 수정 전 B강조ON에서 피격 전↔종료 후 개체영역1,715픽셀차이, B강조OFF 전↔후는0차이. 원래없던블록을null로정리한뒤에는 B강조ON도0차이였다. [수정전수치](before_fix/pixel_comparison.json), [당시재질/속성](before_fix/check.json).
- [수정 후 비교](comparison.png): 같은 진단을 별도 자체 새세션에서 재실행했다. 피격중에는1,717픽셀변화가있어 기존반짝임이유지되고, 피격전↔종료후는 **0픽셀차이/RGB평균차0**이다. 추가null정리로도변화0이며 이미원래렌더상태가복구됨을확인했다. [수정후수치](pixel_comparison.json), [실제속성/자세](check.json). 두세션간 최초포즈위상은 다르므로 수정전/후서로의이미지가동일하다는뜻은아니다. 각세션내동일자세전후를비교했다. 확대판은 동일영역Nearest4배이고수치비교는축소없는원본캡처영역으로수행했다.
- 새회귀검사 ‘빈원본블록은피격후에도HasPropertyBlock=false’는 수정전1실패→수정후통과. 기존4병종/보스·방향별재질/원본그림자/90ms수명/연속피격제한/기존속성블록보존 포함 **EditMode6/6통과**. [red](red_empty_block.json)/[green](green_hit_tests.json). 전체스위트·기존6개Play실패 재실행은하지않았다.
- 코드검토에서 차단문제없음을 확인했다. 자체 진단의finally에서게임어댑터enabled·강조enabled·TimeScale1/captureDelta0을복원했고 자체Play종료·전투씬1/root7/dirtyfalse·컴파일실패false·현재콘솔오류0/경고0을확인했다. Git 게임변경은HitFlash.cs와관련Edit검사뿐이다.

사람플레이/밸런스/전체회귀/다수개체성능/최종아트승인은아니다. 기존B②Inspector두께·색·밝기와사용자선택③색보정/맵/땅굴/효과를유지한다. 빌드/실행파일/push/Notion변경없음. AudioProduction/·밸런스/BGM·다른미추적작업/font_tools_vendor접근오류삭제표시를보존한다. 원본전체진단PNG는로컬에두고비교판/검사근거만명시적으로커밋한다.
