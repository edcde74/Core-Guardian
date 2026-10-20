import pathlib, hashlib, json, zipfile
root=pathlib.Path(__file__).parent
archive=root/'raw_profiler_captures.zip'
manifest=[]
with zipfile.ZipFile(archive,'w',compression=zipfile.ZIP_DEFLATED,compresslevel=4,allowZip64=True) as z:
    for p in sorted(root.rglob('*.data')):
        h=hashlib.sha256()
        with p.open('rb') as f:
            for chunk in iter(lambda:f.read(1024*1024),b''):h.update(chunk)
        rel=p.relative_to(root).as_posix()
        manifest.append(dict(file=rel,size=p.stat().st_size,sha256=h.hexdigest()))
        z.write(p,rel)
with zipfile.ZipFile(archive) as z:
    for item in manifest:
        h=hashlib.sha256()
        with z.open(item['file']) as f:
            for chunk in iter(lambda:f.read(1024*1024),b''):h.update(chunk)
        if h.hexdigest()!=item['sha256']:raise RuntimeError(item['file'])
(root/'capture_archive_manifest.json').write_text(json.dumps(dict(archive=archive.name,archiveBytes=archive.stat().st_size,verifiedAllEntriesSha256=True,files=manifest),indent=2),encoding='utf-8')
print('Verified lossless archive:',archive.stat().st_size,'bytes;',len(manifest),'captures')
