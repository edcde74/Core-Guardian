"""Derive FPS only from saved normal-mode engine delta samples; no new Unity run."""
import json
import math
from pathlib import Path

root = Path(__file__).parent
runs = []
groups = {}
for path in sorted((root / 'captures_valid').glob('normal_*_frames.json')):
    raw = json.loads(path.read_text(encoding='utf-8-sig'))
    valid = [x for x in raw if isinstance(x.get('deltaMs'), (int, float))
             and math.isfinite(x['deltaMs']) and x['deltaMs'] > 0]
    dt = [x['deltaMs'] for x in valid]
    ids = [x['engineFrame'] for x in raw]
    missing = sum(max(0, b-a-1) for a, b in zip(ids, ids[1:]))
    monotonic = all(b > a for a, b in zip(ids, ids[1:]))
    population = int(path.stem.split('_n')[1].split('_')[0])
    row = dict(label=path.stem.replace('_frames', ''), source=path.relative_to(root).as_posix(),
               requestedEnemies=population, observedFrames=len(raw), validFrames=len(dt),
               invalidDeltaFrames=len(raw)-len(dt), missingEngineFrames=missing,
               engineFramesStrictlyIncreasing=monotonic,
               firstEngineFrame=ids[0], lastEngineFrame=ids[-1],
               observedSeconds=math.fsum(dt)/1000,
               minFrameMs=min(dt), maxFrameMs=max(dt), meanFrameMs=math.fsum(dt)/len(dt),
               minimumFps=1000/max(dt), maximumFps=1000/min(dt),
               averageFps=len(dt)*1000/math.fsum(dt))
    runs.append(row)
    groups.setdefault(population, []).extend(dt)
combined = [dict(requestedEnemies=n, validFrames=len(dt), observedSeconds=math.fsum(dt)/1000,
                 minimumFps=1000/max(dt), maximumFps=1000/min(dt),
                 averageFps=len(dt)*1000/math.fsum(dt)) for n, dt in sorted(groups.items())]
result = dict(kind='Derived Editor engine-delta FPS under Profiler and injected stress; not standalone FPS',
              averageDefinition='valid observed frame count * 1000 / sum(valid deltaMs)',
              scope='Warmup excluded by original capture. All valid spikes retained. Repeats kept separate; combined values pool observed intervals, not wall-clock gaps between runs.',
              runs=runs, combined=combined)
(root/'fps_summary.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
lines = ['# 저장된 일반 계측의 FPS', '',
         '2026-10-20 추가 집계. 새 Unity 계측/Play는 없으며 기존 captures_valid/normal 원본100프레임씩을 계산했다.', '',
         '**Editor·Profiler 활성·HP/재화/적 주입 스트레스**의 관측 프레임 간격 기반 수치다. 실행파일FPS·정상 전투·Deep 모드 결과가 아니다.', '',
         '최저=1000/최대 프레임 시간, 최고=1000/최소 프레임 시간, 평균=유효 프레임 수×1000/프레임 시간 합계. 순간FPS 산술평균과 다르다. 유효 스파이크는 제거하지 않았다. 프레임 누락이 있으면 관측 표본 평균으로만 해석한다.', '',
         '|실험|적|표본|관측 시간 s|최저 FPS|최고 FPS|평균 FPS|프레임 시간 최소/최대 ms|누락/무효|',
         '|---|---|---|---|---|---|---|---|---|']
for r in runs:
    lines.append(f"|{r['label']}|{r['requestedEnemies']}|{r['validFrames']}|{r['observedSeconds']:.3f}|{r['minimumFps']:.2f}|{r['maximumFps']:.2f}|{r['averageFps']:.2f}|{r['minFrameMs']:.3f}/{r['maxFrameMs']:.3f}|{r['missingEngineFrames']}/{r['invalidDeltaFrames']}|")
lines += ['', '## 반복 묶음', '', '실험 사이의 준비/저장 시간은 포함하지 않고 관측 구간만 합친 값이다. 각 반복의 편차는 위 표를 유지한다.', '',
          '|적|총 표본|관측 시간 s|최저 FPS|최고 FPS|평균 FPS|', '|---|---|---|---|---|---|']
for r in combined:
    lines.append(f"|{r['requestedEnemies']}|{r['validFrames']}|{r['observedSeconds']:.3f}|{r['minimumFps']:.2f}|{r['maximumFps']:.2f}|{r['averageFps']:.2f}|")
lines += ['', '세부 정밀값·원본 링크·프레임 연속성은 [fps_summary.json](fps_summary.json), 조건·병목·한계는 [조사 README](README.md)를 따른다. 변경 후 수치와 성능 개선율은 최적화 미적용으로 N/A다.', '']
(root/'FPS.md').write_text('\n'.join(lines), encoding='utf-8')
print(json.dumps(combined, indent=2))
