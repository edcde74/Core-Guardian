"""Restore only empty Unity-generated cache folders at the locked version.
No package manifest, lockfile, game source, scene, or existing package file edits.
"""
import argparse, concurrent.futures, hashlib, io, json, pathlib, shutil, tarfile, urllib.request

ROOT = pathlib.Path(__file__).resolve().parents[3]
CACHE = (ROOT / 'Library/PackageCache').resolve()
BUILTIN = pathlib.Path('C:/Program Files/Unity/Hub/Editor/6000.6.2f1/Editor/Data/Resources/PackageManager/BuiltInPackages')
REPORT = pathlib.Path(__file__).resolve().parent
LOCK = json.loads((ROOT / 'Packages/packages-lock.json').read_text(encoding='utf-8'))['dependencies']

def plan():
    rows=[]
    for dest in sorted(CACHE.iterdir()):
        if not dest.is_dir() or (dest/'package.json').exists(): continue
        if any(dest.iterdir()): raise RuntimeError(f'Nonempty cache without metadata; inspect manually: {dest.name}')
        name=dest.name.split('@')[0]
        dep=LOCK[name]
        source=BUILTIN/name if dep['source']=='builtin' else None
        if source:
            meta=json.loads((source/'package.json').read_text(encoding='utf-8'))
            if meta['name']!=name or meta['version']!=dep['version']: raise RuntimeError(f'Builtin version mismatch: {name}')
        elif dep['source']!='registry': raise RuntimeError(f'Unsupported source: {name}')
        rows.append(dict(name=name,version=dep['version'],source=dep['source'],destination=str(dest.resolve()),builtin=str(source) if source else None))
    return rows

def download(row):
    with urllib.request.urlopen('https://packages.unity.com/'+row['name'],timeout=45) as req: meta=json.load(req)['versions'][row['version']]
    url=meta['dist']['tarball']
    if not url.startswith('https://download.packages.unity.com/'): raise RuntimeError('Unexpected download origin')
    with urllib.request.urlopen(url,timeout=90) as req: data=req.read()
    sha=hashlib.sha1(data).hexdigest()
    if sha!=meta['dist']['shasum']: raise RuntimeError('Package checksum mismatch')
    stage=(ROOT/'Temp/PackageRecovery_20261017'/row['name']).resolve();stage.mkdir(parents=True,exist_ok=True)
    with tarfile.open(fileobj=io.BytesIO(data),mode='r:gz') as archive:
        members=archive.getmembers()
        for member in members:
            parts=pathlib.PurePosixPath(member.name).parts
            if not parts or parts[0]!='package' or '..' in parts or member.issym() or member.islnk(): raise RuntimeError('Unsafe package archive member')
            target=stage.joinpath(*parts[1:]).resolve()
            if not target.is_relative_to(stage): raise RuntimeError('Archive path escapes staging')
            if member.isdir(): target.mkdir(parents=True,exist_ok=True)
            elif member.isfile():
                target.parent.mkdir(parents=True,exist_ok=True)
                with archive.extractfile(member) as src, target.open('wb') as dst: shutil.copyfileobj(src,dst)
        actual=json.loads((stage/'package.json').read_text(encoding='utf-8'))
        if actual['name']!=row['name'] or actual['version']!=row['version']: raise RuntimeError('Archive metadata mismatch')
    return row, stage, sha, len(data)

def install(row, source, sha=None, size=None):
    dest=pathlib.Path(row['destination']).resolve()
    if not dest.is_relative_to(CACHE) or dest.parent!=CACHE: raise RuntimeError('Destination escapes package cache')
    if any(dest.iterdir()): raise RuntimeError('Destination no longer empty; refusing to overwrite')
    shutil.copytree(source,dest,dirs_exist_ok=True)
    return dict(name=row['name'],version=row['version'],source=row['source'],files=sum(f.is_file() for f in dest.rglob('*')),sha1=sha,downloadBytes=size)

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--apply',action='store_true');args=parser.parse_args()
    rows=plan();(REPORT/'recovery_plan.json').write_text(json.dumps(rows,indent=2),encoding='utf-8')
    print(json.dumps(dict(emptyPackages=len(rows),builtin=sum(r['source']=='builtin' for r in rows),registry=sum(r['source']=='registry' for r in rows))),flush=True)
    if args.apply:
        results=[]
        for row in rows:
            if row['builtin']:
                result=install(row,pathlib.Path(row['builtin']));results.append(result);print('Restored '+row['name'],flush=True)
        registry=[r for r in rows if not r['builtin']]
        with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:
            for row,stage,sha,size in pool.map(download,registry):
                result=install(row,stage,sha,size);results.append(result);print('Restored '+row['name'],flush=True)
        (REPORT/'recovery_results.json').write_text(json.dumps(results,indent=2),encoding='utf-8')
        print(json.dumps(dict(restored=len(results),missing=sum(not (d/'package.json').exists() for d in CACHE.iterdir() if d.is_dir()))),flush=True)
