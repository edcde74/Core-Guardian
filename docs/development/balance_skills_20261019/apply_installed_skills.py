"""Apply downloaded balance tooling to existing evidence; never launches Unity or changes data."""
import hashlib, importlib.util, json, os, subprocess, sys, re
from pathlib import Path
root=Path(__file__).resolve().parent
project=root.parents[2]
skills=Path('C:/Users/edcde/.codex/skills')
plugin=Path('C:/Users/edcde/.codex/plugins/cache/unity-agent-plugin/unity/0.1.8-beta')
sys.path.insert(0,str(Path(os.environ['TEMP'])/'codex-skill-validation-20261019'))
spec=importlib.util.spec_from_file_location('skill_validator',skills/'.system/skill-creator/scripts/quick_validate.py')
validator=importlib.util.module_from_spec(spec);spec.loader.exec_module(validator)
checks=[]
for folder in [skills/'headless-game-balance',*sorted((plugin/'skills').iterdir())]:
 if not (folder/'SKILL.md').is_file(): continue
 ok,message=validator.validate_skill(folder)
 accepted=ok;extensions=[]
 if not ok and message.startswith('Unexpected key(s)'):
  import yaml
  fm=yaml.safe_load(re.match(r'^---\n(.*?)\n---',(folder/'SKILL.md').read_text(encoding='utf-8'),re.S).group(1))
  allowed={'name','description','license','allowed-tools','metadata'}
  extensions=sorted(set(fm)-allowed)
  accepted=set(extensions)<={'required_packages','required_editor_version','enabled','modes'} and isinstance(fm.get('name'),str) and isinstance(fm.get('description'),str) and 0<len(fm['description'])<=1024
 checks.append({'path':str(folder),'strict_validator_pass':ok,'base_frontmatter_pass':bool(accepted),'official_extension_keys':extensions,'message':message})
(root/'skill_validation.json').write_text(json.dumps(checks,ensure_ascii=False,indent=2),encoding='utf-8')
assert all(r['base_frontmatter_pass'] for r in checks),[r for r in checks if not r['base_frontmatter_pass']]
originals=project/'docs/development/balance_measurement_20261019'
paths=sorted(originals.glob('*.json'))
summarizer=skills/'headless-game-balance/scripts/summarize_runs.py'
summaries=[];converted=[]
for p in paths:
 d=json.loads(p.read_text(encoding='utf-8-sig'))
 if not isinstance(d,dict) or 'policy' not in d or 'outcome' not in d: continue
 record={'seed':None,'won':d['completedWave']==11 and d['outcome']=='Victory',
 'reached_stage':d['rounds'][-1]['wave'],'duration_seconds':d['elapsedGame'],
 'balance_version':'18279e1/current-csv','policy_version':p.stem,'source':str(p.relative_to(project)),
 'seed_provenance':d['randomSeed'],'paired_seed_eligible':False,'failure_reason':None if d['outcome']=='Victory' else 'core_destroyed',
 'core':d['final']['wallet']['CoreEnergy'],'boss_killed':d['rounds'][9]['bossKilled'],
 'gold':d['final']['wallet']['Gold'],'crystals':d['final']['wallet']['Crystals']}
 converted.append(record)
 dest=root/'derived'/f'{p.stem}.jsonl';dest.parent.mkdir(exist_ok=True)
 dest.write_text(json.dumps(record,ensure_ascii=False)+'\n',encoding='utf-8')
 result=subprocess.run([sys.executable,str(summarizer),str(dest),'--compact'],check=True,capture_output=True,text=True,encoding='utf-8')
 summaries.append({'policy_version':p.stem,'evidence':'descriptive_single_run_only','seed_eligibility':'unpaired_original_constructor_seed_unspecified',
 'summary':json.loads(result.stdout)})
(root/'derived/policies.jsonl').write_text(''.join(json.dumps(r,ensure_ascii=False)+'\n' for r in converted),encoding='utf-8')
(root/'derived/skill_summaries.json').write_text(json.dumps(summaries,indent=2),encoding='utf-8')
assert len(summaries)==8 and all(r['summary']['runs']==1 for r in summaries)
protected=json.loads((originals/'protected_before.json').read_text(encoding='utf-8-sig'))
changed=[r['path'] for r in protected if not (project/r['path']).is_file() or hashlib.sha256((project/r['path']).read_bytes()).hexdigest().upper()!=r['sha256']]
assert not changed,changed
installed=skills/'headless-game-balance';review=root/'review/headless-game-balance'
source_files=[p for p in review.rglob('*') if p.is_file() and '.git' not in p.parts]
assert all((installed/p.relative_to(review)).is_file() and (installed/p.relative_to(review)).read_bytes()==p.read_bytes() for p in source_files)
result={'base_frontmatter_checks_pass':len(checks),'strict_validator_pass':sum(r['strict_validator_pass'] for r in checks),'official_unity_extension_metadata_count':sum(bool(r['official_extension_keys']) for r in checks),'official_unity_skills':len(checks)-1,'downloaded_headless_files_verified':len(source_files),
 'existing_runs_processed':len(summaries),'paired_seed_runs':0,'protected_game_files':len(protected),'changed_game_files':changed,
 'new_unity_play_or_tests':False,'interpretation':'per-policy descriptive results only; not a pooled win rate, deterministic simulation, population estimate or human difficulty approval'}
(root/'application_verification.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(json.dumps(result,indent=2))
