# A안 갈무리 HUD 적용 · 2026-10-13

사용자가 폰트 시안 A를 선택하고 적용을 승인했다. 복구 기준2162ad4, 시작 시 관련 게임 소스·폰트·인수인계의 기존 변경 없음.

- Galmuri11 / Galmuri11 Bold 원본 TTF를 Resources/CoreGuardian/Fonts에 포함했다. OS 폰트 설치 없이 게임에 번들된다. SHA256는 시안의 공식 다운로드본과 동일하다. OFL 원문도 같은 폴더에 유지한다.
- 본문·버튼·상태·안내는 일반 갈무리, 패널 제목·단계 제목·증강 이름은 굵은 갈무리다. 기존 글자 크기/상자/줄간격/심볼·상호작용 유지.
- Unity TrueTypeFontImporter를 에디터 API로 Dynamic / HintedRaster로 설정했다. 시스템 폰트 fallback 참조 없이 폰트 데이터를 포함한다. TTF/라이선스는 파일 복사, 메타는 Unity가 생성했으며 씬 YAML·메타 직접 편집 없음.
- 기존 승인 Pixelify Sans Bold 피해/골드 숫자는 유지한다. 게임 종료 시 리소스 폰트 자산을 Destroy하지 않도록 기존 OS 폰트 해제 코드를 제거했다.
- 방금 제거한 설치/강화 중복 접기 버튼은 다시 만들지 않았다. 밸런스CSV/전투규칙/씬/원본아트/프리팹 변경 없음. 추가빌드/실행파일실행/Notion변경/GitHub push 없음.

## 실제 확인

- [설치·플레이어 강화](Panels.png), [지원 포탑·접힌 상태창](Tower.png), [플래티넘 증강3종](Cards.png) 실제1920×1080 Game View 캡처를 직접 확인했다. 브라우저 시안이 아니다. 텍스트 영역/프레임·선호 높이 및 실제 폰트 리소스 이름/글리프 검사 위반0.
- 14px 접기/테스트 배속,16px 설치,18~20px 본문·버튼,22~26px 굵은 제목을 현재 화면에서 확인. 다른 화면비/축소 화면의 픽셀 격자·사람 판독 체감은 아직 검증하지 않았다.
- 설치 목록/접힌 상태창/지원 포탑4줄,6묶음×3장의18종 실제 카드 공개 상태에서 글자 누락·영역 초과0. 카드별 JSON은 cards_0~5.json, 패널은 panels.json/compact.json/support_final.json.
- `CheckHudFontApplication.cs`는 기존 글자 배치 검사기를 재사용하며 선택 카드18종을 런타임에만 주입한다. 준비 상태 TimeScale0, 포탑은 정상 구매 API, 지원 정보는 기존 역할 진단의 재화로 검사했다. 증강 화면 진입은 기존 VerifyBossAugmentation.Boss/Choices의 강제 처치 흐름을 사용했고 난이도 시험이 아니다.
- 초기 검사에서 일반 초기 수정100을 사용한 뒤 지원 포탑을 추가 구매할 재화가 없어 SelectSupport가 거부되었다. support.json은 이때 단일 포탑을 검사한 중간 기록이다. 이후 기존 역할 진단으로 재검사하여 support_final.json에서 지원 포탑4줄 검사를 완료했다. 실행CSV를 바꾸지 않았다.
- 검사 스크립트의 FontRenderingMode 네임스페이스 오류1회는 실행 전 컴파일에서 발견해 UnityEditor로 수정했다. 게임 소스 컴파일 오류는 없다.
- 최신 PlayMode8/8 통과: 실제11라운드/재시작·정상1라운드·이동/점유와 카드 공개/중복 클릭 방지/전체 공개 전 선택/공개 중 닫기 등을 확인. 기존 EditMode73개는 반복하지 않았다.
- 검사 후TimeScale1복원·Play종료. 임시 Assets 캡처3개를 문서 폴더로 복사 후 해당 파일/빈 폴더만 에디터 API로 삭제했다. 끝 상태는 아래 최종 확인 기준이며 저장 씬은 수정하지 않았다.

최종 Unity 상태 ready/stopped/compiling=false/domainReload=false·전투씬active/loaded/dirty=false, 콘솔현재오류0/경고0·compilationFailed=false. 디스크 비교에서 CSV/씬/ProjectSettings/Packages 변경 없음.

사용자 선택한 서체 A 적용 완료. 별도 최종 사용성/다른 화면비 검증과 Windows 빌드 갱신은 남는다.
