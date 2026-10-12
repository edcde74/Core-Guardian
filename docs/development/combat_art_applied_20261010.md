# 전투 아트 적용 — 2026-10-10

사용자 선택: B 보강 금속 스파크, 기술병 E2 금속 부품 탄, 피해 숫자 Pixelify Sans Bold.

## 적용 결과

- 내장 imagegen으로 투명 정지 원본을 제작하고 Unity Sprite Editor API로 9종 분리. 기술병 금속 부품 탄, 단일·범위 포탑 수정 탄, 적 피격·처치, 적 이동 버프, 포탑 공격력 버프, 코어 피해, 골드 획득.
- `Assets/Resources/CoreGuardian/Art/CombatB.png`: Point, 압축·mipmap 없음. 현재 임포트 PPU128은 이번 VFX 배치용이며 전체 게임의 최종 픽셀 규격을 뜻하지 않는다. 원본 프리팹·그림자·카메라·씬 YAML을 수정하지 않았다.
- 원본 Pixelify Sans 가변 TTF에서 fontTools로 정적 Bold700 생성. `Assets/Art/Combat/20261010/Fonts/PixelifySans-Bold.ttf`와 SIL OFL1.1 동봉.
- `Assets/Resources/CoreGuardian/Fonts/PixelifySansBoldDamage.asset`: 정적 TMP 512×512 숫자·부호 atlas. 외곽선 재질 함께 저장. 피해 숫자 크림색32px, 골드 금색 +숫자, 코어 피해 빨간 -숫자. 한글 HUD는 기존 폰트 유지.
- 실제 투사체 위치·방향을 따라 아트 표시. 효과가 전투 판정·타게팅·사거리·보상에 영향을 주지 않는다. 적 처치와 코어 도착을 구분한다.
- 피격·처치 효과는 승인 정지 스프라이트의 짧은 표시·투명도 전환이다. 보행·공격 프레임 애니메이션은 미제작이다.

## 검증

- TMP 원본 Bold와 숫자 글리프, Resources 로딩, Canvas 메시 생성 검증 통과: `combat_font_install_20261010.json`.
- 9종 스프라이트 분리·임포트 검증 통과: `combat_art_import_20261010.json`.
- 실제 씬에서 3종 투사체, 버프 대상2종, 실제 HP감소 숫자12·25, 처치 골드+3, 코어 피해-2, 코어 도착 골드 표시 없음, raycast 차단 없음, 재설정 후 효과 정리 확인: `combat_art_runtime_20261010.json`.
- EditMode22/22, PlayMode5/5 통과. 정식1라운드 골드45·수정20·코어20·피해375와 포탑 접촉 중심, 플레이어 칸 설치 후 수동 이동·재진입 차단 회귀 통과.
- 캡처는 실제 Game view 1920×1080이나 별도 역할 검증 모드에서 일부 피격·처치를 주입한 시각 검토 화면이다. 정식1라운드 캡처로 해석하지 않는다.
- 검증용 범위 포탑은(13,16)에 배치했다. 이전 예시(17,15)는 현재 검증 적 시작 위치에서 사거리 밖이므로 세 발사 주체가 모두 활성화되도록 배치를 조정했다. 게임 능력치·정식 배치 변경 없음.
- 검증 종료 후 timeScale1 복구, Play stopped, 최신 씬 saved 확인.
- Notion 우선순위 아트 문서에 채택 방향과 적용 범위를 반영하고 재조회 확인. 문서 상태 속성은 변경하지 않았다.

![실제 역할 검증 화면](C:/Users/edcde/OneDrive/Desktop/CD/docs/development/combat_art_applied_20261010.png)

## 제작·재검증 스크립트

- `InstallCombatFont.cs`: 공식 동봉 TMP Essential Resources 비대화형 설치 및 TMP 생성/검증. 최초 설치에서 Unity 원본 확인 모달을 처리했다.
- `InstallCombatArt.cs`: 원본 알파 범위에서 셀별 rect를 정하고 에디터 API로 분리. 재실행 시 기존 sprite ID 유지.
- `VerifyCombatArt.cs`: Play mode에서 런타임 표현 검증. 검증 중 시간 정지를 사용하므로 종료 시 Resume 후 Stop 필요.

현재 적용은 최신 Unity 전투 씬에서 확인한다. 기존 Windows 실행 파일은 재빌드하지 않았다.
