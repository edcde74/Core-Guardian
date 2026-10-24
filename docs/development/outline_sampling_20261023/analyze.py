import json,statistics
from pathlib import Path
r=Path(__file__).parent
result={}
for label in ('before','after'):
    data=json.loads((r/(label+'.json')).read_text())['frames']
    ms=[f['ms'] for f in data]
    def numbers(name):return [f['counters'][name]['value'] for f in data if name in f['counters'] and f['counters'][name]['valid']]
    gc=numbers('GC Allocated In Frame')
    result[label]=dict(samples=len(data),gaps=sum(f['gap']!=1 for f in data),frame_ms=dict(mean=statistics.mean(ms),median=statistics.median(ms),min=min(ms),max=max(ms)),fps=dict(min=1000/max(ms),max=1000/min(ms),average=len(ms)*1000/sum(ms)),gc_B_frame=dict(mean=statistics.mean(gc),median=statistics.median(gc),max=max(gc)),collections=sum(f['collectionsSincePrevious'] for f in data),draw=statistics.mean(f['draw'] for f in data),setpass=statistics.mean(f['setpass'] for f in data),markers_mean_ms={name:statistics.mean(numbers(name))/1e6 for name in ('Main Thread','PlayerLoop','CG.Mask.Present','CG.Emphasis.Present','CG.Emphasis.Tint','CG.PhotoShadow.Present','GC.Collect','GarbageCollector.CollectIncremental') if numbers(name)},top_frames=[dict(frame=f['frame'],ms=f['ms'],projectiles=f['projectiles'],gc=f['counters']['GC Allocated In Frame']['value']) for f in sorted(data,key=lambda f:f['ms'],reverse=True)[:3]])
result['frame_mean_delta_ms']=result['before']['frame_ms']['mean']-result['after']['frame_ms']['mean']
result['frame_mean_saved_percent']=result['frame_mean_delta_ms']/result['before']['frame_ms']['mean']*100
result['fps_average_change_percent']=(result['after']['fps']['average']/result['before']['fps']['average']-1)*100
result['logical_fixture']={}
for phase in ('original','central','early'):
    row=(r/(phase+'_logical_reads.csv')).read_text().splitlines()[0].split(',')
    pixels,lo,hi,total=map(int,row)
    result['logical_fixture'][phase]=dict(output_pixels=pixels,min=lo,max=hi,total=total,mean=total/pixels)
result['gpu_ms']=None
result['limitations']=['One 90-frame Editor run per condition; no GPU profiler support on current D3D12.','Instrumented GPU shader logical source call counts are not hardware texture requests or GPU timing.','Do not sum nested CPU markers or attribute all wall differences to shader.']
(r/'summary.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(result,ensure_ascii=False,indent=2))
