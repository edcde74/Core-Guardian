"""Read existing measurement JSON; verify ledgers and produce derived comparison, never game edits."""
import json, csv
from pathlib import Path
root=Path(__file__).resolve().parent
enemies={r['id']:r for r in csv.DictReader((root.parents[2]/'Assets/Resources/CoreGuardian/enemies.csv').read_text(encoding='utf-8-sig').splitlines())}
rows=[];runs=[];checks=[];event_wave_corrections=[]
for f in sorted(root.glob('*.json')):
    d=json.loads(f.read_text(encoding='utf-8-sig'))
    if not isinstance(d,dict) or 'policy' not in d or 'outcome' not in d: continue
    label=f.stem
    events=d['events']; aug=next((e['detail']['selected'] for e in events if e['kind']=='augmentation_selected'),None)
    for e in events:
        if e['kind']=='core_arrival_sample':
            true_wave=next((r['wave'] for r in d['rounds'] if any(a['Id']==e['detail']['Id'] for a in r['arrivals'])),None)
            if true_wave is not None and true_wave!=e['wave']:event_wave_corrections.append({'run':label,'enemy_id':e['detail']['Id'],'raw_event_wave':e['wave'],'round_from_arrival_id':true_wave,'game':e['game'],'note':'Round completion increments Session.Wave before observer samples exit; raw preserved'})
    for r in d['rounds']:
        start=r['combatStart']['wallet'];end=r['end']['wallet'];prep=r['preparationStart']['wallet']
        damage=r['damage']; kills=r['kills'];gain=sum(int(enemies[k]['gold'])*n for k,n in kills.items())
        bonus_gold=bonus_crystals=0
        if start['CoreEnergy']==end['CoreEnergy']:
            for c in r['combatStart']['selected']:
                if c.endswith('_Flawless'):
                    grade=c.split('_')[0];bonus_gold+={'Silver':10,'Gold':30,'Platinum':60}[grade];bonus_crystals+={'Silver':5,'Gold':15,'Platinum':30}[grade]
        checks.append({'run':label,'wave':r['wave'],'gold':end['Gold']-start['Gold']==gain+bonus_gold,'crystals':end['Crystals']-start['Crystals']==r['reward']+bonus_crystals,
                       'purchase_gold':prep['Gold']-start['Gold']==sum(e['detail']['cost'] for e in r['actions'] if e['kind']=='player_upgrade' and e['detail']['ok']),
                       'purchase_crystals':prep['Crystals']-start['Crystals']==sum(e['detail']['cost'] for e in r['actions'] if e['kind'] in ('tower_upgrade','install') and e['detail']['ok']),
                       'enemy_conservation':sum(kills.values())+len(r['arrivals'])==r['expected'] if r['outcome']=='Completed' else None})
        rows.append(dict(run=label,wave=r['wave'],seconds=round(r['seconds'],3),core_loss=start['CoreEnergy']-end['CoreEnergy'],core_end=end['CoreEnergy'],
            player_damage=damage.get('Player',0),tower_damage=sum(v for k,v in damage.items() if k.startswith('Tower:')),
            total_damage=sum(damage.values()),player_share=round(damage.get('Player',0)/max(1,sum(damage.values()))*100,2),
            peak_alive=r['peakAliveSample'],minimum_remaining=round(r['minimumRemainingSample'],3),kills=sum(kills.values()),arrivals=len(r['arrivals']),
            gold_gain=gain+bonus_gold,gold_spent=prep['Gold']-start['Gold'],gold_end=end['Gold'],crystals_gain=r['reward']+bonus_crystals,
            crystals_spent=prep['Crystals']-start['Crystals'],crystals_end=end['Crystals'],boss_killed=r['bossKilled'],augmentation=aug if r['wave']==11 else None))
    runs.append(dict(run=label,outcome=d['outcome'],completed=d['completedWave'],core=d['final']['wallet']['CoreEnergy'],gold=d['final']['wallet']['Gold'],crystals=d['final']['wallet']['Crystals'],
       game_seconds=round(d['elapsedGame'],2),real_seconds=round(d['elapsedReal'],2),augmentation=aug,data_unchanged=d['dataUnchanged'],errors=d['errors'],volume_unchanged=d['volumeBefore']==d['volumeAfter'],
       first_player_upgrade=next(({'wave':e['wave'],'game':e['game']} for e in events if e['kind']=='player_upgrade'),None),
       tower_purchases=[{'wave':e['wave'],'kind':e['kind'],'detail':e['detail']} for e in events if e['kind'] in ('install','tower_upgrade')]))
result={'runs':runs,'rounds':rows,'checks':checks,'event_wave_corrections':event_wave_corrections,'all_ledger_checks_pass':all(all(v is not False for k,v in c.items() if k not in ('run','wave')) for c in checks)}
(root/'comparison.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
if rows:
    with (root/'comparison.csv').open('w',newline='',encoding='utf-8-sig') as f:
        w=csv.DictWriter(f,fieldnames=rows[0].keys());w.writeheader();w.writerows(rows)
print(json.dumps({'runs':[{k:v for k,v in r.items() if k not in ('tower_purchases',)} for r in runs], 'ledger_checks':len(checks), 'pass':result['all_ledger_checks_pass']},ensure_ascii=False,indent=2))
