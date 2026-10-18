# 사용자 사진에서 그림자 직접 추출 — 2026-10-18

사용자가 이전 실루엣 투사와 둔한 다각형 모두 위치/모양이 다르다고 지적하고, 지정 이미지에서 그림자만 직접 따오라고 지시했다. 앞으로 이 사진의 실제 노출 그림자/발·받침 상대 위치를 기준으로 하며 새 도형으로 재해석한 이전 두 시안을 승인 상태로 보지 않는다.

## 원본과 추출

입력은 `C:/Users/edcde/.codex/generated_images/01a14c3e-6f40-7cb0-a137-905a4e6a0ae7/exec-64c1db7a-4fd7-4356-86cf-e32b0ba049b7.png` (1774×887) 하단행이다. 잘못 전달된 `/C:/` 접두어에서 앞 슬래시만 제거해 실제 파일을 직접 열었다.

- 각 개체의 실제 그림자 색 영역을 시드 연결 선택으로 분리했다. 같이 선택된 가는 돌 줄눈 가지는 마스크 정리(Min5→Max5)로 제거했다. 원본 픽셀 영역 선택·정리이며 새 타원/다각형/실루엣 투사/이미지 생성이 아니다.
- 본체에 가려진 부분은 추측해 채우지 않고 **사진에서 보이는 그림자만** 분리했다. 입력은 배경까지 합쳐진 RGB이므로 원래 투명 알파를 정확히 복구할 수 없다. 투명 PNG의 색은 어두운 갈색/알파40%로 재구성했으며, 원본 바닥 RGB를 그대로 가져와 오염시키지는 않는다. 픽셀 단위 완전 동일 알파 추출이라고 주장하지 않는다.
- `{Attack,Defense,Buff,Player,Core,Single,Multi,Support}_shadow_extracted.png`는 종류별 투명 PNG다. [원본 좌표 유지한 그림자 전용 전체 PNG](shadows_from_reference_original_coordinates.png), [사진과 직접 추출 결과](direct_extraction_proof.png), [사진 위 선택영역 확인](selected_source_pixels_overlay.png), [정확한 선택 영역/시드/앵커/스케일](extraction.json).

## 원본에 합성한 시안

- 실제 원본 전투 렌더는 앞선 `shadow_blunt_preview_20261018/current_actual.png`와 본체 마스크를 재사용한다. 이번에 게임이나 Unity를 바꾸지 않았다. 그림자를 몸체 맨 아래에서 아래로 새로 뻗게 하지 않고, 사진 속 밑면/발 앵커에 대한 실제 그림자 위치를 크기 비례로 옮겼다. 회전/반전/새 모양 생성 없음.
- [실제 플레이어4배](player_direct_extraction_4x.png), [8종 원본 합성2배](actual_originals_direct_extraction_2x.png), [원래 카메라 크기 합성](extracted_shadow_on_actual_originals.png), [합성용 별도 그림자 레이어](separate_shadow_layer.png).
- 원래 본체 마스크를 위에 복원해 본체 픽셀을 유지했다. 마스크 안 RGB 변화는 **8종 모두0픽셀**이다: [검사](body_preservation.json). 기존 게임 크기/정지 자세의 합성이며 실제 움직임/보스/방향별 애니메이션 검증이나 게임 적용이 아니다.
- 재현은 [extract.py](extract.py). 사용자가 직접 그림자 추출을 지시한 범위에서 픽셀 선택·분리·합성으로 처리했다. 기존 생성형 본체의 잘못된 플레이어 비율을 새로 가져오지 않았다.
- **시안만·사용자 피드백 대기**. Assets/프리팹/씬/코드/데이터/Notion 수정·Play 제어·빌드/새 자동검사 없음. 병행 사운드 변경은 보존했다. 원래 그림자의 알파/가려진 형태/동적 투사는 별도 미확정이다.
