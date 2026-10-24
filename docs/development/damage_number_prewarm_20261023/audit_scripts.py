"""Static index only: no Unity interaction, builds, package or project settings changes."""
import csv, hashlib, json, re
from collections import Counter
from pathlib import Path

project=Path(__file__).resolve().parents[3]
out=Path(__file__).parent
patterns={
    'update':r'\b(?:Update|LateUpdate|FixedUpdate)\s*\(',
    'alloc_or_expand':r'\bnew\s+(?:\w+[.<>,\[\]]*|\[)|\.(?:ToArray|ToList|AsReadOnly)\s*\(',
    'query':r'\.(?:Where|Select|FirstOrDefault|First|OrderBy|Count|Any|Sum)\s*\(',
    'engine_lookup':r'\b(?:GetComponent\w*|FindObjects\w*|Resources\.(?:Load\w*|FindObjects\w*))\s*[<(]',
    'create_destroy':r'\b(?:Instantiate|Destroy|DestroyImmediate|CreateInstance)\s*[<(]|\bnew\s+(?:GameObject|Material|Mesh|Texture2D|RenderTexture)',
    'native_upload':r'\.(?:SetVertices|SetUVs|SetTriangles|SetData|ExecuteCommandBuffer|SetPropertyBlock|SetPixels32|Apply)\s*\(',
}
references=[]
for path in project.joinpath('Assets').rglob('*'):
    if path.suffix in ('.unity','.prefab','.asset'):
        text=path.read_text(encoding='utf-8-sig',errors='replace')
        refs=set(re.findall(r'm_Script:\s*\{[^}]*guid:\s*([0-9a-f]+)',text))
        if refs:references.append((path.relative_to(project).as_posix(),refs))
inventory=[]
for path in sorted(project.joinpath('Assets').rglob('*.cs')):
    source=path.read_text(encoding='utf-8-sig')
    lines=source.splitlines()
    relative=path.relative_to(project).as_posix()
    scope='test' if '/Tests/' in relative else 'editor' if '/Editor/' in relative else 'runtime'
    hits={key:[i+1 for i,line in enumerate(lines) if re.search(pattern,line)] for key,pattern in patterns.items()}
    meta=Path(str(path)+'.meta')
    match=re.search(r'^guid: (\w+)',meta.read_text(encoding='utf-8-sig'),re.M) if meta.exists() else None
    refs=[p for p,guids in references if match and match[1] in guids]
    inventory.append(dict(path=relative,scope=scope,lines=len(lines),sha256=hashlib.sha256(path.read_bytes()).hexdigest(),hits=hits,serialized_references=refs,review='full-file static pattern scan; runtime call-chain review by subsystem; tests/editor excluded from player frame cost'))
(out/'script_inventory.json').write_text(json.dumps(inventory,ensure_ascii=False,indent=2),encoding='utf-8')
with (out/'script_inventory.csv').open('w',encoding='utf-8-sig',newline='') as f:
    writer=csv.writer(f);writer.writerow(['path','scope','lines','sha256','serialized_reference_count',*patterns])
    for item in inventory:writer.writerow([item['path'],item['scope'],item['lines'],item['sha256'],len(item['serialized_references']),*[len(item['hits'][k]) for k in patterns]])
print(json.dumps(dict(files=len(inventory),by_scope=Counter(x['scope'] for x in inventory),lines=sum(x['lines'] for x in inventory)),ensure_ascii=False))
for item in inventory:
    if item['path'].split('/')[-1] in ('EnemyController.cs','BeanProjectile.cs','TowerTargeting.cs','LightningTower.cs','CoreGuardianPrototype.cs'):
        print(item['path'],item['serialized_references'])
