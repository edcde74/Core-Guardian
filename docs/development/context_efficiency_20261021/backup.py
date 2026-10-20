"""One-time, read-only source backup. Does not change skills, config, or Unity files."""
from pathlib import Path
import hashlib, json, subprocess, zipfile

ROOT = Path(__file__).resolve().parent
HOME = Path('C:/Users/edcde/.codex')
def sha(data): return hashlib.sha256(data).hexdigest()
def git(*args):
    return subprocess.check_output(['git', '-c', 'safe.directory=C:/Users/edcde/OneDrive/Desktop/CD', *args])

assert not (ROOT/'baseline.json').exists(), 'Never overwrite the original backup'
docs = ['AGENTS.md', 'docs/development/UNITY_HANDOVER.md',
        'docs/development/resume_optimization_latest_20261020.md',
        'docs/development/optimization_next_work_20261020.md']
originals = {}
for name in docs:
    data = Path(name).read_bytes()
    destination = ROOT/'before'/(name+'.original')
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_bytes(data)
    text = data.decode('utf-8-sig').replace('\r\n', '\n')
    originals[name] = dict(sha256=sha(data), bytes=len(data), chars=len(text), lines=len(text.splitlines()))

sources = {'local': HOME/'skills',
           'unity_official': HOME/'plugins/cache/unity-agent-plugin/unity/0.1.8-beta/skills',
           'superpowers': HOME/'plugins/cache/openai-curated-remote/superpowers/6.4.2/skills'}
entries = []
with zipfile.ZipFile(ROOT/'skills_before.zip', 'w', zipfile.ZIP_DEFLATED) as archive:
    for label, folder in sources.items():
        for file in sorted(folder.rglob('*')):
            if not file.is_file() or any(p in {'.git', '__pycache__', 'node_modules'} for p in file.parts): continue
            data = file.read_bytes()
            name = label+'/'+file.relative_to(folder).as_posix()
            archive.writestr(name, data)
            entries.append(dict(entry=name, source=str(file), bytes=len(data), sha256=sha(data)))
with zipfile.ZipFile(ROOT/'skills_before.zip') as archive:
    for item in entries: assert sha(archive.read(item['entry'])) == item['sha256']

protected = {}
for raw in git('ls-files', '-z', '--', 'Assets', 'Packages', 'ProjectSettings').split(b'\0'):
    if not raw: continue
    name = raw.decode('utf-8')
    file = Path(name)
    protected[name] = sha(file.read_bytes()) if file.is_file() else None
baseline = dict(head=git('rev-parse', 'HEAD').decode().strip(), documents=originals,
                protected=protected, configSha256=sha((HOME/'config.toml').read_bytes()),
                skillRoots={k:str(v) for k,v in sources.items()},
                skillCount=sum(x['entry'].endswith('/SKILL.md') for x in entries),
                skillsArchiveVerified=True,
                unityDiff=git('diff', '--', 'Assets', 'Packages', 'ProjectSettings').decode('utf-8'))
(ROOT/'baseline.json').write_text(json.dumps(baseline, indent=2), encoding='utf-8')
(ROOT/'skills_manifest.json').write_text(json.dumps(entries, indent=2), encoding='utf-8')
print(json.dumps(dict(backedUpDocuments=len(originals), skills=baseline['skillCount'],
                     skillFiles=len(entries), protectedFiles=len(protected),
                     archiveBytes=(ROOT/'skills_before.zip').stat().st_size, verified=True)))
