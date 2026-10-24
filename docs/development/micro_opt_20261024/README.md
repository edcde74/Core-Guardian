# 결과 동일 미세 최적화 묶음 — 2026-10-24

사용자 "지금 최적화 할 수 있는거 싹다 진행해" → "다시 진행해". 기준 HEAD `bdbda95`. 대상 15개 파일은 시작 시 HEAD와 동일(미커밋 변경 없음)했으므로 HEAD가 복구 기준이다. 같은 시각 다른 세션의 미커밋 작업(`CoreGuardianObjectEmphasis.cs`, `CoreGuardianSharedOutline.cs`, SharedOutline 셰이더 2개, 문서 폴더)은 건드리지 않았고 이 커밋에 포함하지 않았다.

## 변경 (모두 출력·동작 동일을 의도한 변경, 묶음 변경)
|파일|변경|
|---|---|
|`TMP_SDF-Mobile.shader`|`#pragma enable_d3d11_debug_symbols` 삭제. Unity 문서상 이 pragma는 모든 API에서 셰이더 최적화를 끈다.|
|`CoreGuardianAudioCatalog.cs`|`Category`의 `Enum.Parse`를 문자열이 바뀔 때만 수행. `ForClip`은 첫 일치 결과를 clip별로 재사용(여전히 같은 clip을 가리킬 때만).|
|`CoreGuardianSoundSettings.cs`|카테고리 PlayerPrefs 키 문자열을 한 번만 생성. PlayerPrefs는 기존처럼 매번 읽는다(테스트가 직접 쓰는 계약 유지).|
|`CoreGuardianCombatAudio.cs`|빈 voice 찾기 LINQ → 같은 순서의 루프.|
|`CoreGuardianTowerGlow/CoreAmbient/UpgradeFeedback/MaskProducerBatch/SelectionOutline.cs`|매 프레임 문자열 셰이더 속성 → `Shader.PropertyToID`.|
|`CoreGuardianMaskProducerBatch.cs`|`Discard`의 캡처 람다 `RemoveAll` → 같은 순서의 제자리 필터.|
|`CoreGuardianSelectionOutline.cs`|bounds z 두께가 0이면 비트 단위로 같은 꼭짓점 4개만 화면 투영(8→4).|
|`CoreGuardianPlayerMotion.cs`|매 프레임 `new[]` 부품 배열 → 재사용 배열.|
|`CoreGuardianCombatArt.cs`|포탄 스프라이트 선택의 LINQ+`"Tower:"+Id` 문자열 생성 → `TowerState.AttackSource` 비교 루프.|
|`CoreGuardianPrototype.cs`|설치 미리보기의 매 프레임 `GetComponentsInChildren` → 생성 시 1회 수집(표시 중일 때만 색 적용). `OverUI` 결과 List 재사용.|
|`CoreGuardianHud.cs`|`Refresh`의 `refresh.ToArray()` → 재사용 스냅샷(재진입 시 기존 ToArray). 적 수/보스 LINQ → 루프.|
|`TowerBaseShade.shader`|강도 0인 6행째부터는 텍스처를 읽기 전에 discard(알파 0 블렌딩은 원래도 대상 불변).|
|`UpgradeScan.shader`|같은 두 discard의 순서만 바꿔 밴드 밖은 마스크를 읽지 않음.|

## 검증
- 재컴파일 오류 0. 수정 셰이더 3종 isSupported, 메시지 0.
- EditMode **303/303 통과**.
- PlayMode **53/55**. 실패 2개(`CoreFeedbackPlayModeTests.ActualCoreExit…` Expired enemy handle, `PlayerFacingPlayModeTests.NormalGridSteps…` Expected >0 was 0)는 [next_opt_20261024](../next_opt_20261024/README.md)에 이 변경 이전부터 있던 실패로 기록된 것과 같은 테스트·같은 메시지.
- 전체 EditMode 실행이 `outline_sampling_20261023/held_final_*.csv`에 행을 다시 추가했다(기존에도 관측된 테스트 출력). 복원하지 않았고 커밋하지 않았다.

## 성능
**미측정.** 이 묶음은 할당·문자열 해시·텍스처 읽기를 줄이는 작은 변경이며 개별 절감 ms는 확인하지 않았다. 같은 조건 전후 비교(warm15/sample90 ×3, Deep OFF)는 별도 실행이 필요하다. 판정: 동작 회귀 없음(테스트 범위), 성능 미측정.

## 보류 (이번에 적용하지 않음)
- 마스크 RT R8 전환: 기존 테스트가 ARGB32 복사·RGBA 전채널 비교를 전제한다. 테스트 계약 변경과 픽셀 QA가 먼저 필요.
- 피해 숫자 하위 Canvas: `FindObjectsByType<Canvas>().Length==1` 계약과 충돌.
- `CoreGuardianSpriteReferences` 이벤트화: 비활성 오브젝트의 계층 변경 메시지 전달 보장 미확인. next_opt 문서도 설계 검토 선행으로 둠.
- ObjectEmphasis 관련 변경: 다른 세션의 미커밋 공유 윤곽 작업과 겹쳐 제외.
- 문자열 비교 Ordinal 전환: Session의 문화권 테스트 계약과 얽혀 이득 대비 위험이 커서 제외.
