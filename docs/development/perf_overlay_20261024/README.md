# 빌드용 성능 표시·기록 (2026-10-24, Claude)

사용자 요청: "실행파일로 빌드해서 프레임 잴 때 오른쪽 위에 프레임 수와 성능 측정 도구·기록 기능". 일반 빌드 포함으로 승인받아 구현했다.

## 구성
- 스크립트: `Assets/Scripts/Runtime/Presentation/UI/CoreGuardianPerfOverlay.cs` (Assembly-CSharp)
  - `RuntimeInitializeOnLoadMethod(AfterSceneLoad)`로 `DontDestroyOnLoad` 오브젝트를 만든다. **씬 YAML·HUD·밸런스 변경 없음.** `Restart()`로 씬을 다시 불러와도 유지된다.
  - 캔버스는 처음 F3을 누를 때 만든다(sortingOrder 5000, GraphicRaycaster 없음 → 클릭을 막지 않음). 위치는 오른쪽 위 (-16,-8), 320×130으로 피해 패널(-140부터) 바로 위. 배경은 `CoreGuardianHudSkin.Frame(image,"Panel")`(반투명 검정 Image는 화면에서 거의 안 보였음). 글꼴 Galmuri11 14pt.
- 키
  - F3: 표시 켜기/끄기. 표시 항목: FPS·1% low(최근 1024프레임), 프레임 평균/최대 ms, CPU 메인(Present 대기 제외)·렌더 스레드·GPU ms, GC KB/프레임, 메모리 MB, 적·포탑 수, 라운드, 배속(`TestSpeed`), VSync 상태, 기록 시간
  - F4: 기록 시작/정지 → `<실행 파일 폴더>/PerfLogs/perf_yyyyMMdd_HHmmss.csv`(1초 1줄) + `_summary.txt`. 에디터에서는 프로젝트 루트 `PerfLogs/`(.gitignore 처리). 종료 시 자동 저장.
  - F5: 측정 모드. `QualitySettings.vSyncCount=0`, `Application.targetFrameRate=-1`. 다시 누르거나 종료하면 원래 값으로 복원.
  - 실행 인수: `-perfhud`, `-perflog`, `-perf`
- CSV 열: time_s, round, phase, speed, enemies, towers, uncapped, fps, fps_1pct_low, frame_ms_avg, frame_ms_max, cpu_main_ms, cpu_render_ms, gpu_ms, gc_kb_per_frame, mem_mb
- 요약: 시작·길이·프레임 수, 실행 종류(Player/Editor, Development 여부), 해상도·주사율·품질·VSync, CPU/GPU/RAM, 전체 평균 FPS·1%/0.1% low·최악 프레임·평균 CPU/GPU/GC, 라운드별(전투 단계만) 통계
- 1% low = 가장 느린 1% 프레임의 평균 시간을 FPS로 바꾼 값
- 값 출처: `FrameTimingManager`(CPU/렌더/GPU) → 없으면 `ProfilerRecorder("Main Thread")`. GC는 `ProfilerRecorder("GC Allocated In Frame")` → 없으면 `GC.GetAllocatedBytesForCurrentThread()` 차이. 메모리는 `Total Used Memory` → 없으면 `Profiler.GetTotalAllocatedMemoryLong()`.
- 설정 변경: `PlayerSettings.enableFrameTimingStats = true`(빌드에서 CPU/GPU 시간을 읽기 위해 필요).
- 부하: 표시를 끄고 기록하지 않을 때는 매 프레임 키 입력 확인만 한다. 표시 중에는 0.5초마다 Text 갱신(약 1KB 할당 + 드로우콜 1~2개). 측정 중 영향을 줄이려면 표시를 끄고 F4만 쓴다. 켜거나 기록을 시작한 직후 2프레임은 버린다(시작 전 시간이 섞이기 때문).

## 검증
- 컴파일 오류 0(2회).
- Editor Play 약 1분(사용자 승인 "한번 보자"): 표시 위치·HUD 비겹침 확인, F4 기록 CSV 62줄 + 요약 생성 확인. 첫 버전은 기록 첫 프레임 2659ms가 섞이고 배경이 안 보여 위 두 가지를 고쳤다. 고친 화면은 Editor에서 재확인하려다 Play 직후 eval 5초 타임아웃으로 중단(재시도 없이 Play 종료).
- 일반 빌드 `Builds/CoreGuardian_20261024_perf/` 성공(오류 0, 48초, 174MB). zip `Builds/CoreGuardian_20261024_perf.zip`.
- 사용자 실행(2026-10-25 16:32, 준비 단계만, 측정 모드 ON, 1920x1080 창, 144Hz, Ryzen 5 5600X / RTX 3070):
  - `perf_20261025_163208`: 2.1초, 평균 1074 FPS, CPU 메인 0.7ms
  - `perf_20261025_163211`: 16.1초, 평균 1374 FPS, 1% low 143.8, 최악 8.5ms, CPU 메인 0.5ms, GPU 0.1ms
  - **GC는 "-"**: 일반 빌드에서는 두 방법 모두 값이 없다.
  - 측정 모드에서도 매초 가장 느린 1%(약 14프레임)가 약 7ms(=144Hz 한 주기)로, 1% low가 약 144에 붙어 있다(frame_ms_max 6.8~8.5). 원인 미확인(창 모드 합성·표시 갱신 등 후보, 확인 안 함).

- 사용자 실행(2026-10-25 17:14, 새 빌드 `Builds/CoreGuardian_20261025_fix/`, 3배속 5~30라운드 682초, 최대 적 60·포탑 27): `perf_20261025_171421`
  - 전투 평균 144FPS(VSync 상한), 1% low 대부분 약 137, 최악 20.8ms 1회, GPU 약 0.3ms, 메모리 284 → 314MB.
  - **측정 모드는 처음 5초만 ON**(F5가 다시 눌려 꺼짐). VSync 중에는 `cpu_main_ms`에 화면 대기가 섞여 적이 많을수록 값이 줄어드는 모양(6.7 → 3.1ms)이 나온다 → 전투 CPU 비용으로 쓰지 않는다.
  - 기록 방법 안내: F5 한 번(측정 모드 확인) → F4 시작 → F4 정지. F5는 누를 때마다 켜고 끄기.

## 한계·후속 후보
- VSync 켜짐 상태의 CPU 메인 칸이 대기 시간을 포함하는 문제(표시 또는 보정 필요).
- 일반 빌드 GC 측정 대체 방법 없음.
- 전투 구간·목표 조건(적 약 130 + 포탑 32, 3배속) 빌드 실측 없음.
- 측정 모드에서 1% low가 약 144(약 7ms 프레임)에 붙는 원인 미확인.
