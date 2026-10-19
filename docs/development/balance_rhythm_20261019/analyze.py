"""Static affordability and archived combat timing. No game simulation or data writes."""
import csv, hashlib, json
from pathlib import Path

OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[2]
OLD = OUT.parent / 'balance_measurement_20261019'
def read_csv(name):
    return list(csv.DictReader((ROOT/'Assets/Resources/CoreGuardian'/name).read_text(encoding='utf-8-sig').splitlines()))
stage = {r['id']: float(r['value']) for r in read_csv('stage.csv')}
enemies = {r['id']: r for r in read_csv('enemies.csv')}
waves = read_csv('waves.csv')
live = json.loads((OUT/'scene_loader.json').read_text(encoding='utf-8-sig'))
runs = []
for f in OLD.glob('*.json'):
    d = json.loads(f.read_text(encoding='utf-8-sig'))
    if isinstance(d, dict) and 'policy' in d and 'outcome' in d:
        runs.append((f.stem, d))
assert len(runs) == 8
for ref in live['refs']:
    sha = hashlib.sha256((ROOT/ref['path']).read_bytes()).hexdigest().upper()
    assert sha == ref['sha']
    assert all(next(x['sha'] for x in d['csv'] if x['path'] == ref['path']) == sha for _, d in runs)

rows = []
gold = crystal = 0
for wave in range(1, 12):
    groups = [r for r in waves if int(r['wave']) == wave]
    income = sum(int(r['count'])*int(enemies[r['enemy']]['gold']) for r in groups)
    reward = int(stage.get('waveReward'+str(wave), stage['waveReward']))
    last = max(float(r['start'])+(int(r['count'])-1)*float(r['interval']) for r in groups)
    gold += income; crystal += reward
    observations = [r for _, d in runs for r in d['rounds'] if r['wave'] == wave]
    match = next(r for r in live['rounds'] if r['wave'] == wave)
    assert (income, reward) == (match['gold'], match['reward'])
    assert abs(last-match['last']) < .001
    rows.append(dict(wave=wave,kill_gold_upper=income,cumulative_gold_upper=gold,
                     completion_crystals=reward,cumulative_crystals_after_initial_tower=crystal,
                     last_spawn_seconds=round(last,2),archived_game_seconds_min=round(min(r['seconds'] for r in observations),2),
                     archived_game_seconds_max=round(max(r['seconds'] for r in observations),2),archived_samples=len(observations)))
with (OUT/'rounds.csv').open('w',encoding='utf-8-sig',newline='') as f:
    writer=csv.DictWriter(f,fieldnames=rows[0]);writer.writeheader();writer.writerows(rows)

def threshold(total, currency):
    return next((r['wave'] for r in rows if r[currency] >= total),None)
focus_costs = [int(stage['playerFirstCost']+(i-1)*stage['playerSecondIncrease']+stage['playerIncreaseStep']*(i-1)*(i-2)/2) for i in range(1,4)]
focus = []
total = 0
for cost in focus_costs:
    total += cost
    w=threshold(total,'cumulative_gold_upper')
    focus.append(dict(cost=cost,cumulative_cost=total,earliest_combat_round=w,preparation_only_purchase_round=w+1))
reference = next(d for name,d in runs if name=='single_save_damage_late_player_1')
actions=[dict(wave=r['wave'],kind=a['kind'],game_seconds=a['game'],cost=a['detail']['cost'])
         for r in reference['rounds'] for a in r['actions'] if a['detail'].get('ok')]
result = dict(evidence='static affordability plus archived 5x scene Update; no new Play; not human 1x measurement',
              assumptions=['Initial attack tower costs 100 crystals; no demolition, augment, bonus, extra spending',
                           'Gold upper bound requires every enemy killed, including boss in R10',
                           'Crystal completion rewards require round completion; available in next preparation',
                           'Gold purchases allowed during combat; exact threshold timestamp not recorded'],
              focus_player=focus,
              split_three_player_stats=[dict(cost=100,earliest_combat_round=threshold(100*i,'cumulative_gold_upper')) for i in range(1,4)],
              save_for_installations=[1]+[threshold(cost,'cumulative_crystals_after_initial_tower')+1 for cost in (100,200)],
              same_tower_stat_upgrade_rounds=[threshold(cost,'cumulative_crystals_after_initial_tower')+1 for cost in (80,180)],
              reference_actions=actions,
              reference_first_augmentation=next(e['game'] for e in reference['events'] if e['kind']=='augmentation_open'),
              reference_first_player_purchase=next(a['game_seconds'] for a in actions if a['kind']=='player_upgrade'),
              reference_first_added_tower=next(a['game_seconds'] for a in actions if a['kind']=='install' and a['wave']>1),
              first_two_combat_seconds_range=[round(min(sum(r['seconds'] for r in d['rounds'] if r['wave']<=2) for _,d in runs),2),round(max(sum(r['seconds'] for r in d['rounds'] if r['wave']<=2) for _,d in runs),2)],
              rounded_threshold_candidates=dict(player_by_end_R2_cost_max=93,tower_upgrade_by_prep_R3_cost_max=40,
                  unchanged_upgrade_cost_requires_R1_R2_crystal_sum_at_least=80,unchanged_install_cost_requires_R1_R2_crystal_sum_at_least=100),
              rounds=rows)
(OUT/'analysis.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
protected=json.loads((OUT/'protected_before.json').read_text(encoding='utf-8'))
assert all(hashlib.sha256((ROOT/p).read_bytes()).hexdigest()==sha for p,sha in protected.items())
(OUT/'verification.json').write_text(json.dumps(dict(protected_files=len(protected),unchanged=True,archived_runs=8,
    rounds=11,all_five_csv_hashes_match_archived_runs=True,live_loader_matches_static_totals=True,new_play=False),indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in result.items() if k not in ('rounds','reference_actions','assumptions')},ensure_ascii=False,indent=2))
