import json, statistics
from pathlib import Path

root = Path(__file__).parent
result = {}
for label in ('before', 'after'):
    frames = json.loads((root / (label + '.json')).read_text(encoding='utf-8'))['frames']
    def values(name):
        return [f['counters'][name]['value'] for f in frames if name in f['counters'] and f['counters'][name]['valid']]
    times = [f['ms'] for f in frames]
    gc = values('GC Allocated In Frame')
    result[label] = {
        'samples': len(frames), 'gaps': sum(f['gap'] != 1 for f in frames),
        'frame_ms': {'mean':statistics.mean(times),'median':statistics.median(times),'min':min(times),'max':max(times)},
        'fps': {'min':1000/max(times),'max':1000/min(times),'average':len(times)*1000/sum(times)},
        'gc_B_frame': {'mean':statistics.mean(gc),'median':statistics.median(gc),'max':max(gc)},
        'collections': sum(f['collectionsSincePrevious'] for f in frames),
        'top_frames': [dict(frame=f['frame'],ms=f['ms'],projectiles=f['projectiles'],gc=f['counters'].get('GC Allocated In Frame'),main=f['counters'].get('Main Thread'),create=f['counters'].get('CG.HUD.CreateDamageNumber')) for f in sorted(frames,key=lambda f:f['ms'],reverse=True)[:5]],
        'first_engine22': [f for f in frames if f['frame']==22],
        'markers_mean_ms': {name:statistics.mean(values(name))/1e6 for name in ('Main Thread','CG.Emphasis.Present','CG.PhotoShadow.Present','CG.HUD.CreateDamageNumber') if values(name)},
    }
result['deltas'] = {}
for stat in ('mean','median','max'):
    b,a=result['before']['frame_ms'][stat],result['after']['frame_ms'][stat]
    result['deltas']['frame_ms_'+stat]={'saved':b-a,'saved_percent':(b-a)/b*100}
(root/'summary.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({k:{key:value for key,value in v.items() if key not in ('top_frames','first_engine22')} for k,v in result.items()},ensure_ascii=False,indent=2))
for k in ('before','after'):
    print(k,'top',json.dumps(result[k]['top_frames'][:2]),'frame22',json.dumps(result[k]['first_engine22']))
