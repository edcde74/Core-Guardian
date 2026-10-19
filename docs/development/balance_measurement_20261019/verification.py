"""Fail on ledger errors, changed game assets or invalid normal-session outcomes."""
import json,hashlib,subprocess,sys
from pathlib import Path
root=Path(__file__).resolve().parent;project=root.parents[2]
subprocess.run([sys.executable,str(root/'summarize.py')],check=True,stdout=subprocess.DEVNULL)
d=json.loads((root/'comparison.json').read_text())
before=json.loads((root/'protected_before.json').read_text(encoding='utf-8-sig'))
changed=[r['path'] for r in before if not (project/r['path']).is_file() or hashlib.sha256((project/r['path']).read_bytes()).hexdigest().upper()!=r['sha256']]
scene=json.loads((root/'scene_loader.json').read_text());static=json.loads((root/'static/rounds.json').read_text())
assert {k.replace('\\','/'):v.upper() for k,v in static['sources_sha256'].items()}=={x['path']:x['sha'] for x in scene['refs']}
static_rows=static['rounds']
assert len(static_rows)==11
for a,b in zip(scene['rounds'],static_rows):
    assert a['wave']==b['round'] and a['count']==b['enemy_count'] and a['hp']==b['hp'] and a['gold']==b['gold_all_kills'] and a['reward']==b['crystal_reward'] and abs(a['last']-b['last_spawn_seconds'])<.0001
results={'completed_runs':len(d['runs']),'rounds':len(d['rounds']),'ledger_checks_pass':d['all_ledger_checks_pass'],
 'all_data_unchanged':all(r['data_unchanged'] for r in d['runs']),'all_volume_unchanged':all(r['volume_unchanged'] for r in d['runs']),
 'no_observer_runtime_errors':all(not r['errors'] for r in d['runs']),'protected_files_count':len(before),'changed_protected_files':changed}
assert results['ledger_checks_pass'] and results['all_data_unchanged'] and results['all_volume_unchanged'] and results['no_observer_runtime_errors'] and not changed,results
for f in root.glob('*.json'):
    r=json.loads(f.read_text(encoding='utf-8-sig'))
    if not isinstance(r,dict) or 'policy' not in r or 'outcome' not in r:continue
    assert r['initial']['wallet']=={'Gold':0,'Crystals':100,'CoreEnergy':20}
    assert not r['initial']['towers'] and not r['initial']['selected']
    assert r['initial']['player']['steps']=={}
    assert r['initial']['playerCell']=='(13, 9, 0)'
    assert r['restoredTimeScale']==1
    assert r['outcome'] in ('Victory','Defeat')
    last=r['rounds'][-1]['wave']
    assert [x['wave'] for x in r['rounds']]==list(range(1,last+1))
    assert last==11 if r['outcome']=='Victory' else 1<=last<=11
    assert {x['path']:x['sha'] for x in r['csv']}=={x['path']:x['sha'] for x in scene['refs']}
results['normal_session_and_output_checks_pass']=True
end=json.loads((root/'end_state.json').read_text())
audit=json.loads((root/'runtime_loader_audit_final.json').read_text())
assert len(audit)==8 and all(r['matches'] for r in audit)
assert not end['play'] and not end['paused'] and not end['compiling']
assert len(end['scenes'])==1 and not end['scenes'][0]['isDirty']
assert end['timeScale']==1 and end['captureDelta']==0 and end['volume']==scene['volume']
assert end['categories']==scene['categories'] and end['refs']==scene['refs']
results['runtime_loader_matches']=len(audit)
results['end_state_restored']=True
(root/'verification.json').write_text(json.dumps(results,indent=2),encoding='utf-8')
print(json.dumps(results,indent=2))
