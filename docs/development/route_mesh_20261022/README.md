# 고정 길 메시 합치기

2026-10-22 Asia/Seoul. 사용자 승인: 외부 에셋 대신 공식 `Mesh.CombineMeshes()` 사용. 복구 기준 HEAD `4b4e4a5`. B 마스크 배칭과 A 보류 정책은 변경하지 않았다.

## 현재 적용과 사용

- `Assets/Editor/CoreGuardian/RouteMeshBuilder.cs`의 `Tools > Core Guardian > Route Mesh > Combine Road Tiles` / `Restore Road Tiles`.
- 대상은 `CoreGuardian_MapArt_B/CoreGuardian_Route01_VisualOnly` 하나다. 원본 GameObject/스프라이트/재질을 삭제하거나 변경하지 않고 대상 SpriteRenderer.enabled만 전환한다. 활성 원본 242개 중 219개를 정렬 1/2의 MeshRenderer 2개로 합쳤고 23개는 원본 표시한다.
- 동일 정렬에서 실제 면적이 겹치는 모서리와 `Road stone (25, 8)`, `Road stone (15, 5)`, `Road stone (10, 10)`, `Low curb (25, 8) (0, -1)`의 필요한 원본을 보존한다. 합쳐도 되는 범위가 검증 없이 자동 확대되지는 않는다. 길 배치를 수정하면 원복 후 다시 합치고 화면을 비교해야 한다.
- 원본 텍스처를 그대로 쓰며 새 텍스처/셰이더/패키지가 없다. HDR tint는 현재 Linear 작업 공간으로 변환해 float vertex color에 보존한다. 뒤집기/월드 변환/정렬 레이어·순서·게임 레이어를 반영한다. SpriteMask, 외부 PropertyBlock, Simple 외 모드, 다른 셰이더, 애니메이션/스크립트가 있는 대상은 사전 거부한다.
- 생성한 메시·재질만 고유 이름으로 저장한다. 기존 에셋은 덮어쓰지 않고 다른 사용자 에셋도 일괄 저장하지 않는다. 반복 적용 중 생성한 이전 에셋은 Undo 참조 보존 때문에 남겨두었다. 마지막 실제 참조는 [shipping_state.json](shipping_state.json).
- 사용자가 기존 사용자 변경을 포함한 씬 저장을 명시 승인해 **씬 저장 완료**. 격리 수정 후 대상7/7 통과, 씬 clean/원래7루트/테스트루트0/메시·재질 persistent 참조 정상/Draw41/SetPass5를 최종 확인했다([저장 후 상태](saved_final_state.json), [최종 검사](saved_tests.json)). 사용자 소유의 기존 변경과 섞인 씬은 도구·기록 커밋에 포함하지 않고 저장된 작업 파일로 유지한다. 빌드·실행파일·push·Play 없음.

## 실측

Unity6000.6.2f1 / 기존 Ryzen5600X·RTX3070 / URP2D, 정지 Editor, 현재 기본 카메라 1920x1080. 적을 주입하지 않았다. [직전 통계](applied_before.json), [최종 실제 통계](shipping_state.json).

|지표|전|후|절감량 / 절감률|
|---|---:|---:|---:|
|Game View Draw Calls|258|41|217 / 84.109%|
|SetPass|2|5|−3 / −150% (악화)|
|Triangles|532|532|0 / 0%|
|길 표시 renderer 수|242|25 (메시2+원본23)|217 / 89.669%|
|Profiler CG.Route.CameraRender 평균 ms|0.356216|0.265348|0.090868 / 25.509%|

시간은 게임 프레임이 아니라 **동기 Camera.Render CPU 구간**이다. 조건당 3회, 회당 warm10+sample60, 합계180개. `ProfilerRecorder`의 raw marker samples(TimeNanoseconds, current thread, 프레임 내 합산 OFF)를 사용했으며 Profiler driver/녹화 버퍼를 변경하지 않았다. GPU 시간은 미측정이다. 원본은 [pixel_comparison.json](pixel_comparison.json)의 beforeMeasure/afterMeasure, 재집계는 [render_timing_summary.json](render_timing_summary.json). 같은 호출 안에서 읽은 UnityStats는 지연된 같은 프레임 값258을 반환했으므로 그 필드로 draw 절감을 계산하지 않았다. 호출이 끝난 뒤 새 Game View의 실제 최종 통계41을 사용했다.

- 전 최소/최대/평균: 0.2555 / 0.5156 / 0.356216ms. 후: 0.2062 / 1.2240 / 0.265348ms. 후 최대값 악화를 제거하지 않았다. 순차 Editor 캡처이며 순서 효과 한계가 있다.
- 최저/최고/평균 **게임 FPS는 N/A**: Play나 정상 게임 프레임 계측을 하지 않았다. Camera.Render 시간의 역수를 게임 FPS로 쓰지 않는다. draw 감소가 저사양60FPS나 전투FPS 개선을 보장하지 않는다.
- 최초 리소스 관측은 메시97,792B + 재질3,888B 추가. 텍스처 복제 없음. Undo/이전 bake 에셋/에디터 캐시·전체 잔류·GPU peak를 포함하지 않으므로 메모리 절감 판정은 하지 않는다.

## 검증과 실패

- 신규 대상 EditMode 최초5개는 구현 미존재로 실패(RED), 구현 후5/5. fallback/겹침 보존 추가 후7/7 통과. **저장 승인 후 최종 격리 fixture에서도7/7 통과**([원본](saved_tests.json)). 기존72/169개와 합산하거나 반복하지 않았다.
- HDR tint 최초 이관 실패: 줌8.5/4.25에서145,494/481,753픽셀 차이. [최초 실패](pixel_initial_failed.json), after_initial_failed PNG. Linear 변환 보완 뒤83/337픽셀. GPU point raster 및 정렬 동률 영역을 원본 유지하는 방식으로 좁혔다. 후속 실패/중간 원본도 같은 폴더에 보존했다.
- native 정렬 동률은 source renderer를 껐다 켜는 원복만으로도 과거 원본 캡처와150/338픽셀 차이를 냈다([원복 원본 대조](original_after_restore_control.json)). 이를 메시 자체 회귀나 전체 픽셀 동등성으로 혼동하지 않는다.
- **최종 실제 적용 직전 원본과 직후 결과**는 줌8.5/4.25 모두0픽셀 차이: [shipping_pixels.json](shipping_pixels.json), shipping_before/after PNG. Overlay HUD 미포함, 정지 화면 두 조건의 범위다. 다른 카메라/배율·전투 중첩 전수 통과는 아니다.
- Restore 메뉴는 원본242/메시0/컴포넌트 없음으로 복귀했고, Undo 뒤 원본23/메시2/유효한 에셋 참조를 확인했다([원복/Undo](restore_undo.json)). 독립 코드 검토가 지적한 transient 리소스 Undo 파괴를 Undo API로 보완했다.

## 사용자 상태와 승인 저장 완료

- 시작 시 씬 dirty=true/Play stopped/compiling false. 사용자 기존 M: 전투씬, PackageManagerSettings, TimeManager. 미추적/폰트 도구 접근오류는 건드리지 않았다.
- 기존 공식 EditMode runner와 Undo가 자체 빈 `Route mesh test` 루트를 남겼고, 후속 runner에서 그 중9개가 사용자 씬 파일에 저장된 것을 발견했다. 현재 Hierarchy의 자체 빈 루트15개는 이름·빈 자식·Transform 단독·시작 시 부재를 확인한 뒤 제거했다. 원래7개 루트만 남았다. 테스트 fixture를 PreviewScene과 자체 Undo group 정리로 수정하고 컴파일을 확인했다.
- 씬 파일은 baseline hash `6E362DD8...73CB5A`에서 `7F0CBEE7...8FDF7`로 달라졌다. 차이에 자체 빈9개 루트가 포함됨을 확인했다. [전 해시](user_disk_before.json), [후 해시](user_disk_after.json). 설정2파일 해시는 동일하다. 씬 파일 불변이라고 주장하지 않는다.
- 자체 기록을 제거한 현재 씬을 저장하려 했으나 **자동 승인 검토가 거절**했다: 이미 dirty인 사용자 씬 전체 저장은 다른 미저장 사용자 변경까지 영속화할 수 있으므로 별도 승인이 필요하다는 이유다. 저장을 우회하거나 runner를 추가 실행하지 않았다.
- 이후 사용자가 현재 사용자 변경을 포함한 저장을 명시 승인했다. Unity Editor API로 저장하고, 격리 fixture 대상7개를 실행해7/7 확인했다. 이후 active scene clean/7루트/테스트루트0, disk에서도 `Route mesh test` 없음, persistent 메시·재질 참조 정상, Draw41/SetPass5를 확인했다. 현재 씬에는 정상 원본7루트와 도로 병합이 저장됐고 자체 빈 테스트 루트 기록은 제거됐다. 저장 전 disk는 `Logs/CG_RouteMesh_20261022/before_authorized_save.bytes`로 보존했다. 설정2파일은 기존 해시 동일([최종 해시](authorized_save_hashes.json)). 씬은 승인한 변경을 포함하므로 기존 해시와 다르다.

