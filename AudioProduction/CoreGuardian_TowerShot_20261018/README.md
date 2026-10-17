# 단일 포탑 발사음 3방향 시안

현재 상태: 사용자에게3개 모두 거부됐다. 이유는 질감·타격감 부족과 과한 전자음이다. 아래 합성 시안은 미승인 이력으로만 보존하며 게임에 적용하지 않는다. 후속 실제 무기음 후보는 [실제 음원 비교](real_weapon_candidates.md)를 따른다.

2026-10-18 (Asia/Seoul). 사용자 요청으로 실제 WAV 시안과 무료 Asset Store 대안만 준비했다. 후속 지시 ‘유료인건 다 빼’를 적용해 유료 후보는 목록에서 제외했다. 게임에 적용하지 않았다.

## 직접 제작한 청취 시안

| 번호 | 파일 접두어 | 의도 |
|---|---|---|
| ① | 01_Mechanical | 압력 타격·금속 걸쇠·짧은 복귀 소리 중심 |
| ② | 02_Metal_Crystal | 같은 기계 계열에 짧은 FM 에너지와 수정 공명을 혼합 |
| ③ | 03_Energy | 기계 비중을 줄이고 에너지 방출·고역 공명 강조 |

- 각 방향의 `_preview.wav`는 9초: 0.4/1.4/2.4초에 변형 3개를 단발 재생하고, 4초부터 초당1.2발로6회 반복한다. 정적 합성 청취용이며 실제 Unity 발사 녹음이 아니다.
- `00_ABC_comparison.wav`: 30초, 0~9초① / 10~19초② / 20~29초③.
- `_shot_01`~`03`: 방향마다0.42초 단발 변형3개, 총9개. 48kHz/16bit/mono PCM WAV.
- 단발 RMS는 같은0.42초 구간에서 -23dBFS로 맞췄다. 지각 음량/LUFS 동일성을 보장하지 않는다.
- `compose.py`는 NumPy2.3.5/Python3.12.14로 오실레이터·FM·시드 고정 잡음을 직접 합성한다. 외부 음원·샘플·사운드폰트·오디오 생성 모델·Asset Store 미리듣기를 제작 입력으로 사용하지 않았다.
- 이번 프로젝트의 게임 포함 사용·수정·상업 배포에 사용할 수 있게 제작한 원본 시안이다. 외부 음원 라이선스를 가져오지 않는다. 독점성이나 다른 모든 음원과 비유사함을 보장한다는 뜻은 아니다.

## 무료 Asset Store 대안

아래5개는 확인일 기준 상품 페이지의 FREE 및 Standard Unity Asset Store EULA 표시를 확인했다. 전 세계 무료 상품 전수 목록은 아니며 이번 발사음과 기계·에너지 보강에 관련 있는 후보 목록이다. 실제 패키지는 취득·다운로드하지 않았고 청취 선호/최종 사용 음원은 미선택이다. 제목·설명에 따른 용도 제안이며 실제 청취평이 아니다.

| 상품 | 용도 제안 | 공식 링크 |
|---|---|---|
| Shooting Sound / B.G.M | 발사음 우선 청취 후보 | https://assetstore.unity.com/packages/audio/sound-fx/shooting-sound-177096 |
| Sci-fi Evolution Gift Pack / Epic Sounds and FX | SF·에너지 계열 비교 | https://assetstore.unity.com/packages/audio/sound-fx/sci-fi-evolution-gift-pack-43104 |
| Free Weapon Sound Effects / SoundLab_1 | 무기 작동·발사 계열 비교 | https://assetstore.unity.com/packages/audio/sound-fx/weapons/free-weapon-sound-effects-388474 |
| UI - Mechanical (Free Sample Pack) / InspectorJ Audio | 짧은 기계 작동·UI 보강 후보, 완성 포탑 발사음 팩으로 확정하지 않음 | https://assetstore.unity.com/packages/audio/sound-fx/ui-mechanical-free-sample-pack-148704 |
| FREE Magic Aura Construction Kit / Aural Space | 마법 공명 보조 후보, VFX 동봉 패키지 | https://assetstore.unity.com/packages/vfx/particles/spells/free-magic-aura-construction-kit-154136 |

각 상품의 상단 미디어에 미리듣기/영상이 있다. 일부는 SoundCloud, 일부는 쿠키 허용이 필요한 외부 영상이다. 외부 미디어를 우회 다운로드하지 않았다. 무료 패키지도 본인 Unity 계정으로 취득하고 해당 좌석/라이선스 조건을 적용한다.

Unity 공식 FAQ는 적정 라이선스를 취득해 게임에 통합한 에셋의 상업 이용을 허용하고, 원본 에셋 단독 배포를 제한한다고 안내한다. 각 페이지에는 License type이 Extension Asset으로 표시되어 있으므로 팀 사용 시 좌석 조건을 확인한다. 상업 사용 가능은 스토어 음원 원본의 재배포 허가를 뜻하지 않는다.
- https://assetstore.unity.com/browse/eula-faq
- https://unity.com/legal/as-terms

## 검증·보존

`verify.py`로 생성 WAV13개를 다시 읽어48kHz/16bit/mono·단발 길이·클리핑0·양끝 샘플·DC·단발 RMS 일치를 확인했다. 파일별 peak/RMS/SHA256은 `verification.json`에 저장했다. 형식과 수치 검사이며 직접 청취·스피커 모니터링·다수 포탑 믹스·Unity 재생 검증이나 사용자 승인이 아니다.

시작 HEAD는 `ce41682`. 새 파일은 이 AudioProduction 폴더와 담당 인수인계만이며 Assets/Packages/ProjectSettings/씬·밸런스·기존BGM은 수정하지 않았다. Unity 연결/씬/Play 상태 조회·테스트·빌드/실행파일/push·Notion 수정 없음. 기존 미추적 자료와 font_tools_vendor 접근 오류 삭제 표시는 보존한다.

다음은3개 시안의 사용자 청취 피드백과 무료 스토어 후보 비교다. 음색 선택과 게임 적용은 별도다.
