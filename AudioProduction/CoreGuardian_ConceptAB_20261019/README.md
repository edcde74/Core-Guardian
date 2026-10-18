# 두 가지 사운드 방향 청취 시안 · 2026-10-19

사용자는 개별22항목의 선택을 계속하기 전에 한 게임의 사운드로 들리는 전체 컨셉을 잡고 싶다고 했다. 중심은 ‘경쾌한 전술 방어’이며, 밝고 통통 튀는 A와 단단하고 빠른 B를 직접 듣고 판단하도록 두 시안을 요청했다. 아직 어느 방향도 선택/게임 적용 승인되지 않았다.

- A: pluck_001/002의 짧은 튕김·둥근 타격을 공통 재료로 사용. UI/설치/공격/성공 알림에 같은 질감을 반복한다.
- B: bong_001/glass_001/switch_004의 체결·낮은 몸통 울림을 공통 재료로 사용. 공격/설치와 성장/결과 알림을 같은 재료 계열로 연결한다.
- 기존 승인 Aura/Glyph/Hit의 짧은 파생본을 각각의 타격 재료와 혼합한다. 종전 거부된 기계총·대지/화염·옛 합성 공격음은 사용하지 않는다.
- 시작/강화/라운드 완료/최종 승리는 공통 샘플의 상대 음정 0/4/7 계열로 관계를 만들고 패배는 하행으로 대비한다. 이는 샘플 재생속도 변형이며 절대 조율/음악적 완성도를 검증한 것이 아니다.

## 듣기

`index.html` 또는 `Open_Concept_AB.cmd`로 오프라인 청취 가능. WAV는 페이지에 내장된다. 로컬 서버 `serve.py`는 127.0.0.1:8880 전용이며 index.html만 제공한다. 기존8879 전체청취 서버/페이지는 보존했다.

`audio/A_scene.wav`와 `audio/B_scene.wav` 각24초. 동일 타임라인·전체 RMS 일치(-24.81dBFS), peak A0.76/B0.578. 전체 RMS 일치는 지각 음량/LUFS 일치 보장이 아니다. 원본 파일마다 정규화한 뒤 역할별 레벨·장면 공통 안전 gain을 적용했다. BGM 없이 효과음의 재질과 리듬을 비교한다.

0–2초 UI/설치/시작 → 2–5초 개별공격 → 5초강화 → 6–12초중첩 → 12초일반완료 → 14–18초재설치/강화/공격 → 18초최종승리 → 22초별도패배예시. 승리 직후 패배는 청취 편집의 별도 예시이며 게임 흐름이 아니다.

각11개 개별음도 A/B로 나란히 제공한다. 멀티는 시안에서도0/85/170ms 세 타격을 유지한다. 지원전투 소리는 없다. 보스/코어위험/증강/BGM은 이번 첫 방향 비교에 포함하지 않았다. 22항목 최종 세트나 정상 다수전투 믹스가 아니다.

## 출처와 사용 조건

2026-10-19 공식 [Kenney Interface](https://kenney.nl/assets/interface-sounds) CC0 표시와 로컬 동봉 License.txt의 상업사용 가능 문구 확인. 기존 보유 소스만 읽었고 원본 해시11개를 기록/재확인했다.

기존 승인 마력/피격 원본은 기존 취득 [Magic Aura](https://assetstore.unity.com/packages/vfx/particles/spells/free-magic-aura-construction-kit-154136), [RPG Essentials FREE](https://assetstore-fallback.unity.com/packages/audio/sound-fx/rpg-essentials-sound-effects-free-227708) 파생이다. 공식 제품 페이지와 [Unity FAQ](https://assetstore.unity.com/browse/eula-faq)를 재조회했다. 비SDK 수정과 게임 통합 상업사용은 가능하며 원본/파생 음원의 단독 외부 배포는 하지 않는다. 이 시안은 소유자 PC 로컬 검토용이다.

## 제작·확인 범위

- `build.py`: numpy+soundfile로 오프라인 편집. soundfile0.14.0은 이 폴더 `.local`에만 설치했고 전역환경/Unity 패키지는 변경하지 않았다. 스크립트명 build는 오디오 편집을 뜻하며 게임 빌드가 아니다.
- 24개 WAV(11×2+장면2)의44100Hz/유한샘플/peak<.99 확인, 원본11개 SHA256 재확인. 정확한 소스와 사건은 manifest.json.
- 브라우저 A/B 모두24초·readyState4·오류없음·재생위치 진행 확인. 에이전트 직접 청취/음색 승인/끝까지 재생 완료를 주장하지 않는다.
- Unity 조회/Play/검사·게임Assets/씬/저장음량 변경 없음. 기존 기능QA 결과를 이번 시안의 검증으로 세지 않는다. 사람 청취와 방향 선택이 다음 단계다.

시작 HEAD da8d1e6. 기존 PackageManagerSettings 변경·미추적 자료·font_tools_vendor 접근 오류 삭제 표시 보존. 다음은 A/B의 전체 인상과 전투/성장/결과별 사용자 피드백이며 적용은 별도 지시에 따른다.
