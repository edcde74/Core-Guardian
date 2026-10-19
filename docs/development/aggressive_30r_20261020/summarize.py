from pathlib import Path
import json,hashlib,csv,subprocess,sys
folder=Path(__file__).resolve().parent;root=folder.parents[2]
final=json.loads((folder/'simulation_expansion_final.json').read_text())['runs']
replay=json.loads((folder/'simulation_expansion_replay.json').read_text())['runs'][0]
original=next(r for r in final if r['policy']=='single' and r['seed']==109)
assert original==replay,'Deterministic replay differs'
summary={'method':'same Session, fixed dt=.05, 3 fixed policies x 4 paired seeds; not human win rate','replay_identical':True,'seeds':[17,42,73,109],'policies':{}}
for name in ['mixed','single','multi']:
 runs=[r for r in final if r['policy']==name]
 summary['policies'][name]={'n':len(runs),'completed':[r['completed'] for r in runs],'wins':sum(r['win'] for r in runs),'installations':[r['installed'] for r in runs],'player_purchases':[r['playerPurchases'] for r in runs],'shared_purchases':[r['towerPurchases'] for r in runs]}
 path=folder/(name+'.jsonl');path.write_text('\n'.join(json.dumps({'seed':r['seed'],'won':r['win'],'reached_stage':r['reached'],'duration_seconds':r['elapsed'],'score':r['core'],'failure_reason':None if r['win'] else 'core_depleted'}) for r in runs)+'\n')
 completed=subprocess.run([sys.executable,str(Path(r'C:/Users/edcde/.codex/skills/headless-game-balance/scripts/summarize_runs.py')),str(path),'--compact'],text=True,capture_output=True,check=True)
 summary['policies'][name]['installed_skill_summary']=json.loads(completed.stdout)
summary['active_csv_sha256']={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in (root/'Assets/Resources/CoreGuardian').glob('*.csv')}
before=json.loads((folder/'protected_before.json').read_text());current={p.relative_to(root).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in (root/'Assets').rglob('*') if p.is_file()}
summary['preservation']={'baseline_phase':'after parser/tests source preparation, before changing CSV','baseline_count':len(before),'changed':[k for k,v in before.items() if current.get(k)!=v],'missing':[k for k in before if k not in current],'added':[k for k in current if k not in before]}
(folder/'summary.json').write_text(json.dumps(summary,indent=2),encoding='utf-8')
print(json.dumps(summary,indent=2))
