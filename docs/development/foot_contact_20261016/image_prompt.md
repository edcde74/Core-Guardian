# 이미지 편집 입력과 적용 방식

도구: built-in image_gen. 편집 대상: 현재 enemy_attack_v2.png. 공통 화풍 참조: current_monsters_reference.png. 투명 배경.

## 사용 프롬프트

EDIT TARGET: first attached image, the exact existing 42x64 pixel attack soldier sprite facing screen-left holding a spear. Second image is style reference ONLY. This is a surgical pixel-art repair, NOT a redesign. Keep every pixel of the helmet, scarf, torso, arms, spear, belt, upper legs and the screen-right leg identical. The SCREEN-LEFT boot in the TARGET (which becomes the player's SCREEN-RIGHT boot when the game horizontally mirrors this sprite) floats above the ground. Lower only that screen-left boot/sole about 3 ORIGINAL pixels to make its sole reach the same ground-contact baseline as the other boot. Extend/adjust the matching bottom shin by about 3 pixels using existing armor colors so the ankle stays connected. Keep boot shape and foot width identical. Retain the original native 42x64 integer grid, original palette, angular low-detail shaded pixel clusters, no new colors, no antialiasing. Render the exact small sprite at an integer nearest-neighbor magnification on a transparent background. No shadows, ground, writing, annotations, comparison panels, extra sprites or other changes. Do not change the other foot. Preserve the full spear and transparent margins. Output only the corrected existing sprite. This repairs the original standing silhouette so BOTH boots feel grounded; it does not create a new walking pose.

## 픽셀 보존 정리

generated_reference_only.png는 도구 출력의 보존본이며 게임에 적용한 원본이 아니다. 출력에 수정 대상 밖의 픽셀 차이가 있어 전체 생성 이미지를 게임 스프라이트로 교체하지 않았다. 실제 원본을 확대·측정한 결과 발 높이 차이는4px이었다. ApplySource.cs에서 원본 발 영역만 정수4px 이동하고 원본 정강이의 기존 색면으로 연결했다. 실제 적용은 enemy_attack_v2.png와 그로부터 분리한 Attack/LeftLower.png다. 머리·몸통·무기·다른 발은 원본 픽셀을 유지한다. 별도 새 팔레트·장비·외형을 추가하지 않았다.
