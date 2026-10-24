"""Read-only source/reference audit. Writes evidence under this directory only."""
from pathlib import Path
from collections import Counter
import hashlib,json,re,csv

root=Path(__file__).resolve().parents[3]
out=Path(__file__).parent
extensions={'.shader','.hlsl','.cginc','.compute','.shadergraph','.shadersubgraph'}
files=sorted(p for base in ('Assets','Packages') for p in (root/base).rglob('*') if p.is_file() and p.suffix in extensions)
refs=[]
for p in (root/'Assets').rglob('*'):
    if p.suffix in ('.mat','.asset','.prefab','.unity'):
        s=p.read_text(encoding='utf-8-sig',errors='replace')
        guids=set(re.findall(r'guid:\s*([a-f0-9]{32})',s))
        if guids:refs.append((p.relative_to(root).as_posix(),guids))
rows=[]
for p in files:
    s=p.read_text(encoding='utf-8-sig')
    # Source counts, not compiled GPU instructions or generated pass counts.
    clean=re.sub(r'/\*.*?\*/|//[^\n]*','',s,flags=re.S)
    meta=Path(str(p)+'.meta');m=re.search(r'^guid:\s*(\w+)',meta.read_text(encoding='utf-8-sig'),re.M) if meta.exists() else None
    name=re.search(r'\bShader\s+"([^"]+)"',clean)
    graph=[]
    if p.suffix in ('.shadergraph','.shadersubgraph'):
        decoder=json.JSONDecoder();position=0
        while position<len(s):
            while position<len(s) and s[position].isspace():position+=1
            if position==len(s):break
            obj,end=decoder.raw_decode(s,position);graph.append(obj);position=end
    rows.append(dict(path=p.relative_to(root).as_posix(),sha256=hashlib.sha256(p.read_bytes()).hexdigest(),lines=len(s.splitlines()),shader_name=name[1] if name else None,explicit_passes=len(re.findall(r'\bPass\s*\{',clean)),surface_program=bool(re.search(r'#pragma\s+surface',clean)),sample_call_sites=len(re.findall(r'\b(?:SAMPLE_TEXTURE\w*|LOAD_TEXTURE\w*|tex2D\w*|texCUBE\w*)\s*\(',clean)),loops=len(re.findall(r'\b(?:for|while)\s*\(',clean)),pragmas=re.findall(r'^\s*#pragma\s+(.+)',clean,re.M),includes=re.findall(r'#include\s+"([^"]+)"',clean),serialized_references=[f for f,guids in refs if m and m[1] in guids],graph_types=dict(Counter(o.get('m_Type','unknown') for o in graph)),graph_documents=len(graph)))
(out/'shader_inventory.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
with (out/'shader_inventory.csv').open('w',encoding='utf-8-sig',newline='') as f:
    writer=csv.writer(f);writer.writerow(['path','lines','shader_name','explicit_passes','surface_program','sample_call_sites','loops','serialized_ref_count','sha256'])
    for row in rows:writer.writerow([row[k] for k in ('path','lines','shader_name','explicit_passes','surface_program','sample_call_sites','loops')]+[len(row['serialized_references']),row['sha256']])
old=json.loads((root/'docs/development/damage_number_prewarm_20261023/script_inventory.json').read_text(encoding='utf-8'))
drift=[r['path'] for r in old if not (root/r['path']).exists() or hashlib.sha256((root/r['path']).read_bytes()).hexdigest()!=r['sha256']]
cs=list((root/'Assets').rglob('*.cs'))
summary=dict(files=len(rows),extensions=dict(Counter(Path(r['path']).suffix for r in rows)),custom=sum('/Resources/CoreGuardian/' in r['path'] for r in rows),lines=sum(r['lines'] for r in rows),csharp_current_count=len(cs),csharp_changed_since_previous_audit=drift,csharp_added_since_previous_audit=sorted(set(p.relative_to(root).as_posix() for p in cs)-set(r['path'] for r in old)))
(out/'scope.json').write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding='utf-8')
protected={r['path']:r['sha256'] for r in rows}
protected.update({p.relative_to(root).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in cs})
for relative in ('Assets/Scenes/CoreGuardian_CombatPrototype_20261008.unity','ProjectSettings/PackageManagerSettings.asset','ProjectSettings/TimeManager.asset','ProjectSettings/EditorSettings.asset','Assets/Art/Editor/ProjectWelcome/2d-template.png.meta'):
    protected[relative]=hashlib.sha256((root/relative).read_bytes()).hexdigest()
snapshot=out/'protected_before.json'
if not snapshot.exists():snapshot.write_text(json.dumps(protected,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(summary,ensure_ascii=False))
for r in rows:
    print(Path(r['path']).name,'passes='+str(r['explicit_passes']),'surface='+str(r['surface_program']),'refs='+str(len(r['serialized_references'])),'graphdocs='+str(r['graph_documents']))
