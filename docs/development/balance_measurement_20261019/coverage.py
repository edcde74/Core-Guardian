"""Static circle/axial-route intersection length; no exposure-time, cooldown or target simulation."""
import json,math
from pathlib import Path
root=Path(__file__).resolve().parent
d=json.loads((root/'scene_loader.json').read_text())
route=[tuple(map(int,s.strip('()').split(','))) for s in d['route']]
def covered(center,radius):
    total=0;segments=[]
    for a,b in zip(route,route[1:]):
        axis=0 if a[1]==b[1] else 1;other=1-axis
        off=abs(a[other]-center[other])
        span=math.sqrt(max(0,radius**2-off**2)) if off<=radius else 0
        length=max(0,min(max(a[axis],b[axis]),center[axis]+span)-max(min(a[axis],b[axis]),center[axis]-span)) if off<=radius else 0
        total+=length
        if length>0:segments.append({'from':a,'to':b,'length':length})
    return {'center':center,'radius':radius,'covered_route_tiles':total,'segments':segments}
result={'evidence':'static_geometry_not_combat_or_perceived_pressure','route_length':sum(math.dist(a,b) for a,b in zip(route,route[1:])),
        'cases':[covered(c,r) for c,r in [((12,13),3.5),((12,13),3),((14,13),3.5),((14,13),3),((16,11),3.5),((16,11),3),((13,14),3),((16,12),3)]]}
(root/'coverage.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(result,indent=2))
