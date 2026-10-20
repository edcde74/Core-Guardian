"""Offline document and protected-file checks; no Unity/Play, no network."""
from pathlib import Path
import hashlib, json, re, subprocess, zipfile

ROOT = Path(__file__).resolve().parent
WORKSPACE = ROOT.parents[2]
def sha(data): return hashlib.sha256(data).hexdigest()
def read(path): return path.read_text(encoding='utf-8-sig')
base = json.loads(read(ROOT/'baseline.json'))
manifest = json.loads(read(ROOT/'skills_manifest.json'))
errors = []
for name, metadata in base['documents'].items():
    if sha((ROOT/'before'/(name+'.original')).read_bytes()) != metadata['sha256']:
        errors.append('Backup mismatch: '+name)
for name, digest in base['protected'].items():
    p = WORKSPACE/name
    actual = sha(p.read_bytes()) if p.is_file() else None
    if actual != digest: errors.append('Protected file changed: '+name)
config = Path('C:/Users/edcde/.codex/config.toml')
if sha(config.read_bytes()) != base['configSha256']: errors.append('Codex configuration changed')
with zipfile.ZipFile(ROOT/'skills_before.zip') as archive:
    for item in manifest:
        if sha(archive.read(item['entry'])) != item['sha256']: errors.append('Archive mismatch: '+item['entry'])
        if sha(Path(item['source']).read_bytes()) != item['sha256']: errors.append('Installed skill changed: '+item['source'])

active = [WORKSPACE/'AGENTS.md', WORKSPACE/'docs/development/UNITY_HANDOVER.md',
          WORKSPACE/'docs/development/WORK_ROUTING.md', WORKSPACE/'docs/development/WORKFLOW.md',
          WORKSPACE/'docs/development/TARGETED_QA.md',
          ROOT/'README.md', ROOT/'RESULTS.md',
          *sorted((WORKSPACE/'docs/development/task_rules').glob('*.md')),
          WORKSPACE/'docs/development/resume_optimization_latest_20261020.md',
          WORKSPACE/'docs/development/optimization_next_work_20261020.md']
links = []
for p in active:
    text = read(p)
    if '\ufffd' in text: errors.append('Unicode replacement character: '+str(p))
    for target in re.findall(r'\]\(([^)]+)\)', text):
        target = target.split('#')[0]
        if not target or '://' in target: continue
        resolved = (p.parent/target).resolve()
        links.append(str(resolved))
        if not resolved.exists(): errors.append('Missing link: '+str(p)+' -> '+target)
    for target in re.findall(r'`((?:docs|Assets)/[^`]+\.(?:md|png|unity))`', text):
        if not (WORKSPACE/target).exists(): errors.append('Missing project reference: '+target)

mapping = json.loads(read(ROOT/'rule_mapping.json'))
if len(mapping)!=34 or len({m['originalIndex'] for m in mapping})!=34: errors.append('Incomplete rule mapping')
for m in mapping:
    dest = m['destination']
    if dest.startswith('docs/development/task_rules/') and m['rule'] not in read(WORKSPACE/dest):
        errors.append('Original task rule not preserved: '+str(m['originalIndex']))
test_names = set()
for row in read(WORKSPACE/'docs/development/TARGETED_QA.md').splitlines():
    test_names.update(re.findall(r'CoreGuardian\w*Tests', row))
for name in test_names:
    if len(list((WORKSPACE/'Assets/Game/Tests').rglob(name+'.cs')))!=1: errors.append('Missing/ambiguous QA class: '+name)

measurements = {}
for p in active:
    text = read(p)
    measurements[p.relative_to(WORKSPACE).as_posix()] = dict(chars=len(text), bytes=len(p.read_bytes()), lines=len(text.splitlines()))
old_start = sum(base['documents'][p]['chars'] for p in ['AGENTS.md','docs/development/UNITY_HANDOVER.md'])
new_start = sum(measurements[p]['chars'] for p in ['AGENTS.md','docs/development/UNITY_HANDOVER.md','docs/development/WORK_ROUTING.md'])
root_before = base['documents']['AGENTS.md']['chars']
root_after = measurements['AGENTS.md']['chars']
scenarios = {}
for name, extra in [('document','WORKFLOW.md'),('art','task_rules/ART.md'),('hud','task_rules/HUD.md'),('balance','task_rules/BALANCE.md')]:
    new = new_start+measurements['docs/development/'+extra]['chars']
    scenarios[name] = dict(beforeCommonChars=old_start, afterCommonAndRoutingChars=new, reductionPercent=100*(old_start-new)/old_start,
                           unchangedDomainSourcesExcludedFromBoth=True)
scenarios['performance'] = dict(beforeChars=old_start+len(read(WORKSPACE/'docs/development/OPTIMIZATION_RULES.md')),
                                afterChars=new_start+measurements['docs/development/TARGETED_QA.md']['chars']+len(read(WORKSPACE/'docs/development/OPTIMIZATION_RULES.md')))
scenarios['performance']['reductionPercent']=100*(1-scenarios['performance']['afterChars']/scenarios['performance']['beforeChars'])
result = dict(passed=not errors, errors=errors, measurements=measurements,
              originalRuleCount=34, mappedRules=len(mapping), verbatimTaskRules=sum(m['destination'].startswith('docs/development/task_rules/') for m in mapping),
              checkedLinks=len(links), checkedQAClasses=len(test_names), protectedFiles=len(base['protected']),
              unchangedSkillFiles=len(manifest), skills=base['skillCount'], configUnchanged=sha(config.read_bytes())==base['configSha256'],
              rootCharsBefore=root_before, rootCharsAfter=root_after, rootReductionPercent=100*(1-root_after/root_before),
              startCharsBefore=old_start, startCharsAfter=new_start, startReductionPercent=100*(1-new_start/old_start),
              scenarios=scenarios, actualTokenUsageMeasured=False, UnityInvoked=False,
              caveat='Character-volume comparison only. Existing domain evidence and mandatory skills still load when needed; no billing/token savings guarantee.')
(ROOT/'verification.json').write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
print(json.dumps({k:v for k,v in result.items() if k not in ['measurements','scenarios']}, ensure_ascii=True))
if errors: raise SystemExit(1)
