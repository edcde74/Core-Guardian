import pathlib,zipfile,hashlib,json
root=pathlib.Path(__file__).parent
files=sorted([*root.rglob('*.data'),*root.rglob('*_markers.json')])
manifest=[]
with zipfile.ZipFile(root/'profiler_raw.zip','w',compression=zipfile.ZIP_DEFLATED,compresslevel=3,allowZip64=True) as z:
 for f in files:
  digest=hashlib.sha256(f.read_bytes()).hexdigest();entry=f.relative_to(root).as_posix();z.write(f,entry);manifest.append(dict(path=entry,bytes=f.stat().st_size,sha256=digest))
with zipfile.ZipFile(root/'profiler_raw.zip') as z:
 for m in manifest:
  with z.open(m['path']) as f:
   digest=hashlib.file_digest(f,'sha256').hexdigest()
  if digest!=m['sha256']:raise ValueError(m['path'])
(root/'archive_manifest.json').write_text(json.dumps(dict(verified=True,entries=manifest),indent=2),encoding='utf-8')
print('Verified',len(files),'raw entries; ZIP bytes', (root/'profiler_raw.zip').stat().st_size)
