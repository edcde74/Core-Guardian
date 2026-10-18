코어 가디언 프로젝트를 이어서 진행해줘.
프로젝트: C:\Users\edcde\OneDrive\Desktop\CD
다음 작업은 접지감 다듬기 계속이다. 밸런스나 사운드로 작업을 전환하지 마.

먼저 docs/development/UNITY_HANDOVER.md → AGENTS.md → docs/development/LOCAL_GIT.md → 이 문서 → docs/development/shadow_reference_extract_20261018/README.md 순서로 읽고, 실제 Git HEAD/변경과 Unity 연결·열린 씬·Play·컴파일·저장 상태를 확인해. 과거 종료 상태를 현재로 가정하지 마.

■ 사용자 의도와 반복하면 안 되는 실패
- 플레이어·몬스터·코어·타워 본체 원본은 그대로 두고, 별도 그림자만으로 접지감을 다듬는다.
- 사용자는 스타크래프트 참고 ① 공간·명암 시안의 그림자를 좋아했지만, 본체 재디자인을 승인한 것이 아니다. 생성기가 플레이어를 머리 크고 다리 짧은 로봇처럼 바꾼 전체/비교 시안은 원본 외형 기준으로 쓰지 마.
- 사용자는 무기·팔다리가 그대로 드러나는 가늘고 복잡한 실루엣 투사와, 임의로 만든 둔한 다각형 모두 위치/모양이 다르다며 거부했다.
- 요구는 “내가 보내준 사진에서 그림자만 따와”. 새 도형으로 비슷하게 해석하지 말고 지정 사진의 실제 경계·폭·붙는 위치를 먼저 직접 봐.
- 사진의 그림자는 발 아래로 멀리 내려앉는 모양이 아니다. 본체 뒤쪽에서 오른쪽으로 넓고 뭉뚝하게 붙는다. 발/받침과 떨어뜨리거나 긴 캡슐처럼 만들지 마.

■ 반드시 열어볼 기준과 최신 결과
담당 폴더: docs/development/shadow_reference_extract_20261018/
1. reference_user_selected.png — 사용자 지정 exec-64c1db7a-4fd7-4356-86cf-e32b0ba049b7.png의 바이트 동일 보존본. 하단 ① 공간·명암 행의 그림자를 본다.
2. direct_extraction_proof.png — 사진의 각 개체와 직접 분리한 그림자 비교.
3. actual_originals_direct_extraction_2x.png — 실제 원본 렌더에 추출 그림자만 합성한8종 비교. 각 쌍 왼쪽 현재/오른쪽 합성.
4. player_direct_extraction_4x.png — 실제 플레이어 체형과 그림자 위치 확대.
5. *_shadow_extracted.png / extracted_shadow_pngs.zip — 종류별 투명 PNG8종.
6. extraction.json / body_preservation.json / extract.py — 추출 ROI·시드·발 앵커·스케일·합성 및 확인 범위.

■ 현재까지 한 것과 한계
- 지정 사진의 보이는 그림자 픽셀 영역을 연결 선택하고 얇은 돌 줄눈 혼입을 정리해 분리했다. 본체에 가려진 그림자를 새로 상상해 채우지 않았다.
- 입력은 평탄화 RGB 사진이다. 투명 알파는 정확한 원본 복구가 아니라 어두운 갈색/40%로 재구성했다. 바이트 동일한 원본 알파나 완벽한 경계 추출이라고 과장하지 마.
- 사진 속 발/받침 상대 위치를 기준으로 실제 원본 크기에 맞게 스케일/이동해 합성했다. 이 앵커·크기 맞춤도 다음 시안에서 사진과 직접 대조할 대상이다.
- 실제 본체 마스크 안 RGB 변화는8종 모두0픽셀. 이는 정지 이미지 합성에서 본체를 보존한 결과이며 움직임/자연스러움/전체 회귀 통과가 아니다.
- 최신 추출물은 시안만이며 최종 아트 승인이나 게임 적용이 아니다. 보스/방향별 보행·정지·가림/다수 겹침/성능은 미검증이다.

■ 기존 게임 구현과 구분
- 포탑3종의 이전 접지 구현은27b92d8로 이미 게임에 있다. CoreGuardianTowerGrounding/CoreGuardianUnitAnimation/TowerBaseShade.shader에서 접촉·주변·짧은 투사 그림자와 하단5px 별도 명암을 연결했다. 사용자는 실제 차이가 약하다고 평가했다. 최신 사진 추출 시안을 이미 적용했다고 착각하거나 기존 구현을 임의 롤백하지 마.
- 그 구현의 담당은 docs/development/tower_grounding_20261018/README.md. 이전 투사/다각형 시안은 shadow_only_preview_20261018 / shadow_blunt_preview_20261018의 거부 이력이다.
- 실제 플레이어 씬 참조는 PlayerStart_Prototype 아래 2D Sprite의 player.png이고, Play에서는 CoreGuardianPlayerMotion이 Resources/CoreGuardian/PlayerFacing/D0~D7의 Body/Arm/LeftLeg/RightLeg를 조합한다. PNG 한 장만 보고 실제 모습을 확인했다고 하지 마.
- 몬스터 관련 검토 전 docs/art/MONSTER_ART_STYLE.md와 현행 병사3종 실제 스프라이트/비교판을 연다. 원본 체형·팔레트·방향별 파생/기존 애니메이션은 보존한다.

■ 다음 진행
- 사용자 후속 “접점만 보완”에 따른 최신 검토본은 `shadow_reference_refine_20261018/contact_polish/README.md`와 해당 폴더의 `player_before_after_4x.png`/`all_before_after_2x.png`다. 직전 위치 보정은 유지하고 접점2px 안 작은틈 최대1px 연결·약한농도전이만 보완, 반투명 포함 본체8종0px차/접점범위밖0px차. 정지 시안만·게임미적용/보행미검증, 다음은 외형피드백이다.
- 2026-10-18 후속 접점 보정 시안: `docs/development/shadow_reference_refine_20261018/README.md`와 `player_before_after_4x.png`/`all_before_after_2x.png`를 먼저 확인한다. 지정 사진 추출 경계·폭·축척·알파는 유지하고 위치만8종 조정했다. 실제 불투명 발/받침의 뒤쪽 오른 경계에 맞춰 플레이어(-5,-4)px, 포탑 위7~8px 등 이동했다. 본체 마스크8종0px차, 시안만·미승인/미적용이다. 시작HEAD8825248/Unity읽기조회ready·stopped·compilingfalse·전투씬1dirtyfalse/PlayerFacing32개 직접열람, 새Play/렌더/자동검사 없음. 현재 HEAD는 다시 확인한다.
- 기준 사진과 최신 추출/합성 이미지를 직접 비교하고, 현재 원본 발/받침에 대한 위치·폭·크기·본체 뒤로 붙는 범위를 다듬는 시안을 이어간다.
- 원본을 재생성하거나 무기/발 실루엣 투사·새 타원/다각형·받침/소켓/앵커 추가로 돌아가지 마.
- 정지 합성, 실제 씬 카메라, 정상 입력 이동/방향전환/정지, 사람 직접 플레이를 구분한다. 촬영 배율·자세·HUD 포함 여부를 명시한다.
- 최신 시안 미적용 상태를 유지하며 다음 사용자 피드백과 해당 지시 범위에 맞춰 진행한다. 새 게임 변경이 필요하면 먼저 실제 상태와 중복 그림자/정렬을 확인한다.

■ 보호 사항
- 전투 씬 Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity 하나 유지. 비교 씬 재생성·씬 YAML 직접 편집 금지. Unity CLI/Editor API 사용.
- 사용자/다른 채팅 Play나 검사를 멈추지 마. 자체 임시 리소스·시간·카메라·입력은 복원한다.
- 현재 맵39×22/0.69·자연석 바닥·길목1안·땅굴②·혼합 환경 장식 유지. 맵 배경은 resume_map_latest_20261018.md 참고.
- 색보정③/상시강조B②·초록2px 선택선·HUD/강화스캔0.8초·코어 입자·발광/반동·체력바·본체 도트와 수치는 보존한다.
- 사운드는 다른 작업에서 변경 중일 수 있다. Assets의 사운드 테스트/오디오/코드 변경·미추적 자료를 되돌리거나 이번 시각 작업 커밋에 섞지 마. 최신 사운드 내용은 UNITY_HANDOVER의 담당 링크를 따른다.
- 밸런스는 사용자 별도 일정, 빌드/실행파일/GitHub push는 별도 명시 요청 때만.
- 그림자 추출·패키징 커밋 eb8d513/49fc776은 확인한 이력이다. 이후 HEAD/병행 변경은 재조회한다. 이번 인수인계 정리는 게임/Notion 변경·새 Unity 조회/자동검사 없이 문서와 기준 이미지 보존만 수행했다.
