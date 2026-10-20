import json,pathlib,statistics
p=pathlib.Path(__file__).parent
r=json.loads((p/'comparison.json').read_text())
sides={s:[x for x in r if x['side']==s] for s in ['before_controlled','after_controlled']}
def central(s,k,field='totalMs'):return statistics.median(next(v[field]['median'] for name,v in x['markers'].items() if k==name or (k+'.' not in name and k in name)) for x in sides[s])
# Match the full invoke marker explicitly to avoid similarly named nested markers.
def marker(s,substring):return statistics.median(next(v['totalMs']['median'] for k,v in x['markers'].items() if substring in k) for x in sides[s])
rows=[]
for name,k in [('그림자 LateUpdate ms','CoreGuardianPhotoShadow.LateUpdate'),('강조 LateUpdate ms','CoreGuardianObjectEmphasis.LateUpdate')]:rows.append((name,marker('before_controlled',k),marker('after_controlled',k)))
rows.append(('GC.Alloc B/frame',statistics.median(x['markers']['GC.Alloc']['gcBytes']['median'] for x in sides['before_controlled']),statistics.median(x['markers']['GC.Alloc']['gcBytes']['median'] for x in sides['after_controlled'])))
rows.append(('프레임 delta ms',*[statistics.median(x['frames']['deltaMs']['median'] for x in sides[s]) for s in sides]))
resources={s:[json.loads((p/s/(x['label'].replace('_frames','')+'_resources.json')).read_text()) for x in sides[s]] for s in sides}
rows.append(('RT 교체 회/frame',*[sum(x['replacements'] for x in resources[s])/300 for s in sides]))
lines=['# 최종 전후 수치','', '적128/실제 포탑15, before_controlled ↔ after_controlled. 비용 표는 각 회차 프레임 중앙값의 중앙값(3회), RT 교체만 총횟수/300관측프레임이다. 원시 분포·p95·최대·표본 수는 comparison.json과 원본을 따른다.','', '|지표|전|후|절감량|절감률|','|---|---:|---:|---:|---:|']
for name,b,a in rows:lines.append(f'|{name}|{b:.3f}|{a:.3f}|{b-a:.3f}|{(b-a)/b*100:.2f}%|')
lines+=['','FPS는 각100프레임. 평균=N×1000/ΣdeltaMs. 최소/최대는 유효 스파이크를 포함한다. 모든 회차 누락/무효0건.','', '|단계/회차|유효 Main 프레임|최저 FPS|최고 FPS|평균 FPS|delta 중앙 ms|delta p95 ms|','|---|---:|---:|---:|---:|---:|---:|']
summary={}
for side in sides:
 for i,x in enumerate(sides[side]):
  f=x['fps'];dt=x['frames']['deltaMs'];lines.append(f'|{side}/{i}|{x["validMainFrames"]}|{f["min"]:.3f}|{f["max"]:.3f}|{f["average"]:.3f}|{dt["median"]:.3f}|{dt["p95"]:.3f}|')
 frames=sum([json.loads((p/side/(x['label']+'.json')).read_text()) for x in sides[side]],[])
 summary[side]={'min':min(x['fps']['min'] for x in sides[side]),'max':max(x['fps']['max'] for x in sides[side]),'average':len(frames)*1000/sum(x['deltaMs'] for x in frames),'seconds':sum(x['deltaMs'] for x in frames)/1000}
lines+=['','|3회 묶음(300관측프레임)|최저 FPS|최고 FPS|평균 FPS|관측 초|','|---|---:|---:|---:|---:|']
for s,v in summary.items():lines.append(f'|{s}|{v["min"]:.3f}|{v["max"]:.3f}|{v["average"]:.3f}|{v["seconds"]:.3f}|')
lines+=['','|회차|RT 교체 전→후 (100프레임)|마스크 수 전→후|마지막 현재 RT 추정bytes 전→후|','|---|---:|---:|---:|']
for i,(b,a) in enumerate(zip(resources['before_controlled'],resources['after_controlled'])):lines.append(f'|{i}|{b["replacements"]} → {a["replacements"]}|{b["maskCount"]} → {a["maskCount"]}|{b["bytes"]:,} → {a["bytes"]:,}|')
lines+=['','메모리는 Profiler.GetRuntimeMemorySizeLong 합계이며 GPU VRAM/Memory Profiler snapshot이 아니다. 100프레임의 실시간 길이와 자세·경로 위치가 달라 마지막 RT 크기도 다르므로 이 값을 메모리 절감률로 쓰지 않는다. 새 방식은 동일한 작은 자세에서도 과거 최대 용량을 보유하므로 더 많은 메모리가 필요할 수 있다.','', '표의 변화는 참조·배열/정렬·RT 재사용 묶음 효과다. 개별 기법에 절감량을 분배하지 않는다. Session 코드는 바꾸지 않았으나 dt가 짧아져 하위 Tick 횟수와 투사체 위상이 달라진다. 전체 프레임 향상을 해당 CPU 함수 절감량으로 단순 합산하거나 GPU 시간 절감으로 단정하지 않는다.']
(p/'RESULTS.md').write_text('\n'.join(lines)+'\n',encoding='utf-8');(p/'fps_aggregate.json').write_text(json.dumps(summary,indent=2))
print('\n'.join(lines[:12]))
