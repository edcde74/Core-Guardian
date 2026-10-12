"""Freeze read-only source evidence; writes only this analysis directory."""
import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path
out = Path(__file__).resolve().parent
root = out.parents[2]
names = ['Assets/Scripts/CoreGuardianHud.cs','Assets/Scripts/CoreGuardianPrototype.cs',
         'Assets/Scripts/CoreGuardianCombatArt.cs','Assets/Game/Scripts/CoreGuardian/Session.cs',
         'Assets/Game/Scripts/CoreGuardian/CombatData.cs','Assets/Game/Scripts/CoreGuardian/Augmentations.cs']
sources=[]
for name in names:
    path=root/name
    if not path.exists():
        sources.append({'path':name,'missing':True})
        continue
    data=path.read_bytes()
    sources.append({'path':name,'sha256':hashlib.sha256(data).hexdigest(),'text':data.decode('utf-8-sig')})
(out/'source_snapshot.json').write_text(json.dumps({'capturedUtc':datetime.now(timezone.utc).isoformat(),'sources':sources},ensure_ascii=False,indent=2),encoding='utf-8')
print('Frozen source evidence:',len(sources))
