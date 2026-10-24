import json, pathlib, zipfile, hashlib
root=pathlib.Path(__file__).parent
files=sorted(root.glob('*/*.data'))
manifest=[]
with zipfile.ZipFile(root/'profiler_raw.zip','w',compression=zipfile.ZIP_DEFLATED,compresslevel=1,allowZip64=True) as z:
    for p in files:
        z.write(p,p.relative_to(root).as_posix())
        h=hashlib.sha256()
        with p.open('rb') as f:
            for chunk in iter(lambda:f.read(8*1024*1024),b''):h.update(chunk)
        with z.open(p.relative_to(root).as_posix()) as f:
            restored=hashlib.sha256()
            for chunk in iter(lambda:f.read(8*1024*1024),b''):restored.update(chunk)
        manifest.append(dict(path=p.relative_to(root).as_posix(),bytes=p.stat().st_size,sha256=h.hexdigest(),zipRestoredSHA256=restored.hexdigest(),verified=h.digest()==restored.digest()))
(root/'archive_manifest.json').write_text(json.dumps(manifest,indent=2))
print(json.dumps(dict(files=len(files),zipBytes=(root/'profiler_raw.zip').stat().st_size,verified=all(x['verified'] for x in manifest))))
