# 사운드 제작 스킬·로컬 도구 설치 · 2026-10-19

사용자가 게임 효과음/BGM 제작에 필요한 Codex 스킬과 기술을 GitHub/다운로드 경로에서 받아오라고 요청했다. 시작 HEAD ab6f119. 기존 A/B 시안과 원본·게임 상태를 유지하고 도구만 준비했다.

## 설치한 스킬

사용자 전역 `C:/Users/edcde/.codex/skills`에 설치했다. 다음 사용자 턴부터 사용할 수 있다. GitHub 커밋을 고정해 공식 skill-installer로 설치했고 SKILL 원문 바이트 일치·참조 파일 존재·라이선스 동봉을 확인했다. 세 폴더에는 Markdown/라이선스 문서만 있으며 다운로드 스크립트를 실행하는 스킬은 아니다.

| 스킬 | 출처 | 용도·상태 |
|---|---|---|
| audio-design | [gamedev-skills](https://github.com/gamedev-skills/awesome-gamedev-agent-skills/tree/0a70cfc64672d512c5b5b6006b5dcb5cf90dbdbd/skills/disciplines/audio-design) · Apache-2.0 | 오디오 계열/믹스/덕킹/반복변형/음악 레이어·전환 설계. 엔진 중립 설명과 Godot 예시를 포함하며 Unity 코드로 복사하지 않는다. |
| sound-effects | [ElevenLabs 공식](https://github.com/elevenlabs/skills/tree/32f48f6ebf4a97e963a824ade7d5cb14beacf774/sound-effects) · MIT | 텍스트 기반 효과음/루프 생성 API 사용법. 스킬만 설치, SDK·API키·계정 미연결/생성 미실행. |
| music | [ElevenLabs 공식](https://github.com/elevenlabs/skills/tree/32f48f6ebf4a97e963a824ade7d5cb14beacf774/music) · MIT | BGM/징글 생성과 구간별 구성 API 사용법. 스킬만 설치, SDK·API키·계정 미연결/생성 미실행. |

스킬은 제작 절차/사용법이다. 설치 자체가 음원을 생성하거나 음색 품질을 보장하지 않는다. ElevenLabs 스킬의 MIT는 스킬 문서 라이선스이며 생성 음원의 무료 상업사용 허가가 아니다. 공식 [가격/상업사용 표시](https://elevenlabs.io/pricing)·[음악 문서](https://elevenlabs.io/docs/overview/capabilities/music)·[효과음 문서](https://elevenlabs.io/docs/overview/capabilities/sound-effects)를 확인했다. API키·이용권한·생성 당시 플랜/출력 이용조건을 확인한 뒤 별도 요청 범위에서 사용한다. 결제·생성·업로드·자격증명 열람/저장은 하지 않았다.

audio-design의 일반적인 LUFS 예시·랜덤피치·비트 동기화는 현재 게임 변경 승인이 아니다. 현재 고정피치1/멀티묶음/지원전투무음/저장음량 등 승인 정책을 우선하며 기획/믹스 제안에만 활용한다. 행동 타이밍/밸런스를 음악 박자에 맞추도록 임의 변경하지 않는다. Unity 가져오기/메모리/믹서 구현은 기존 optimize-audio/audio-setup-mixers 스킬과 실제 에디터 상태에 맞춰 별도 진행한다.

## 설치한 로컬 제작 도구

경로 `.local/packages`에만 PyPI 바이너리 휠로 설치했다. 전역 Python/PATH/Unity 패키지·게임 배포물은 변경하지 않았다. 전체 의존성11개 버전/휠URL/SHA256은 sources.json·requirements-lock.txt에 기록했다. 설치 바이너리/패키지·일회성 QA 출력은 Git 제외다.

| 도구 | 확인 버전 | 역할 |
|---|---|---|
| [Spotify Pedalboard](https://github.com/spotify/pedalboard) | 0.9.26 | EQ·압축·잔향·리미터, 오프라인 음색 편집. GPLv3. |
| [pyloudnorm](https://github.com/csteinmetz1/pyloudnorm) | 0.2.0 | LUFS 기반 음량 분석. MIT. |
| [Mido](https://github.com/mido/mido) | 1.3.3 | 음표/리듬/템포를 MIDI로 저장·편집. 악기 음원 렌더러는 별도다. MIT. |
| [imageio-ffmpeg](https://github.com/imageio/imageio-ffmpeg) / FFmpeg | 0.6.0 / 7.1 | WAV/OGG 등의 변환과 오디오 처리. 래퍼BSD-2-Clause, 번들 FFmpeg의 실제 빌드/라이선스는 별도다. |
| NumPy/SciPy/SoundFile | 2.5.4/1.18.1/0.14.0 | 파형·필터·파일 입출력. 해당 패키지 동봉 라이선스 보존. |

이 도구들은 외부 음원 생성 비용 없이 로컬 편집·분석에 쓸 수 있다. 샘플·악기·외부 모델의 사용권은 각 원본을 확인한다. GPL 도구/FFmpeg를 Unity 런타임이나 배포물에 넣지 않았다. 새 AI 음악 모델·사운드폰트·DAW·외부 VST는 이번에 설치하지 않았다.

## 실행/복구

확인 Python: `C:/Users/edcde/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe`.

프로젝트 루트에서 이 Python으로 `AudioProduction/AudioTools_20261019/python_audio.py <오디오스크립트.py>`를 실행하면 해당 프로세스 안에서만 독립 패키지를 사용한다. `verify_tools.py`는 단독 실행도 가능하다. SDK 미설치인 ElevenLabs 예시는 이 로컬 러너만으로 실행되지 않는다.

재설치: 위 Python의 `-m pip install --target AudioProduction/AudioTools_20261019/.local/packages --only-binary=:all: -r AudioProduction/AudioTools_20261019/requirements-lock.txt`. 기존 폴더를 덮어쓰거나 삭제하기 전 실제 상태를 확인한다. 휠 다운로드 해시는 sources.json과 대조한다. Windows 설치 임시파일 ACL 때문에 첫 import가 거부될 수 있어 이번 새 공개 패키지 폴더에만 부모 접근권한 상속을 복원했다. 전역 권한 변경 없음.

## 실제 확인과 한계

`verification.json`: 오프라인 효과처리, WAV→OGG→WAV, MIDI4음표 저장/재읽기, LUFS 분석, 입력원본해시 불변5개 확인. 러너 경유 재실행도 통과했다. 입력은 이전 A장면 복사본이며 출력은 `.local/qa`에만 있다. 도구 기능 검사이며 음색/음악성/게임 믹스 승인이 아니다.

최초 리미터 검사에서는 threshold=-2dB가 출력 ceiling이라는 가정 때문에 실패했다. 설치 API 문서가 0dB hard clipper임을 확인하고 일회성 검사 출력에 명시적 -3dB 최종 Gain을 추가해 재검사했다. 이 체인은 최종 마스터링 프리셋이 아니다.

이전 컨셉 파일을 읽기 분석한 결과 A -22.10LUFS/B -22.55LUFS. 시안 파일을 수정하거나 자동 보정하지 않았다. 미터 값은 사람의 음색 평가를 대신하지 않는다.

Unity 조회/Play/컴파일/검사·게임Assets/씬/음량 변경, 밸런스 측정/변경, 게임 빌드/게임실행/push 없음. 기존 PackageManagerSettings 변경·미추적 자료·font_tools_vendor 오류 삭제표시를 보존했다. Notion 변경 없음. 다음은 기존 A/B 컨셉 사용자 피드백이며 도구 설치를 방향 선택/적용 승인으로 확대하지 않는다.
