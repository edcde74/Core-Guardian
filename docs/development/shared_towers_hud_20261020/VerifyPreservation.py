import hashlib,json
from pathlib import Path
root=Path(__file__).resolve().parents[3]
folder=Path(__file__).resolve().parent
before=json.loads((folder/'protected_before.json').read_text(encoding='utf-8-sig'))
after={p.relative_to(root).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in (root/'Assets').rglob('*') if p.is_file()}
report={'baseline_files':len(before),'changed':[k for k,v in before.items() if k in after and after[k]!=v],'missing':[k for k in before if k not in after],'added':[k for k in after if k not in before]}
(folder/'preservation_report.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
