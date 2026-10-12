"""Read-only CSV arithmetic for the verified CoreGuardian CombatData/Session schema.
Not a battle simulator. Only writes new reports outside Unity Assets/game folders.
"""
import argparse
import csv
import hashlib
import json
import math
from pathlib import Path


def rows(path):
    with path.open(encoding="utf-8-sig", newline="") as stream:
        return list(csv.DictReader(line for line in stream if line.strip() and not line.lstrip().startswith("#")))


def number(value):
    result = float(value)
    if not math.isfinite(result) or result < 0:
        raise ValueError(f"Invalid non-negative number: {value}")
    return result


def integer(value):
    result = number(value)
    if result != int(result):
        raise ValueError(f"Expected integer: {value}")
    return int(result)


def damage(value, defense):
    return math.floor(max(0, value * (1 - defense / 100)) + .5)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path, required=True)
    parser.add_argument("--through", type=int, default=10)
    parser.add_argument("--out", type=Path, required=True)
    args = parser.parse_args()
    project, out = args.project.resolve(), args.out.resolve()
    if args.through < 1:
        parser.error("--through must be positive")
    protected = [project / p for p in ("Assets", "Packages", "ProjectSettings", ".git", "Library", "UserSettings")]
    if out == project or any(out == p or p in out.parents for p in protected):
        parser.error("Output must be an analysis folder outside game/config/cache folders")
    outputs = [out / name for name in ("rounds.json", "rounds.csv", "growth.json", "pressure.svg")]
    if any(p.exists() for p in outputs):
        parser.error("Refusing to overwrite existing analysis outputs; use a fresh directory")
    base = project / "Assets/Resources/CoreGuardian"
    files = {name: base / (name + ".csv") for name in ("enemies", "towers", "stage", "upgrades", "waves")}
    raw = {name: rows(path) for name, path in files.items()}
    expected = {
        "enemies": "id health speed coreDamage gold defense buffRadius buffSpeed status",
        "towers": "id cost damage rate range projectileSpeed buffDamage status",
        "stage": "id value", "upgrades": "id target stat delta cost costIncrement",
        "waves": "id wave enemy count interval start"}
    for name, data in raw.items():
        if not data or set(data[0]) != set(expected[name].split()):
            raise ValueError(f"Unsupported {name} schema; inspect current loader before adapting")
        if len({r["id"] for r in data}) != len(data):
            raise ValueError(f"Duplicate {name} id")
    enemies = {r["id"]: r for r in raw["enemies"]}
    settings = {r["id"]: number(r["value"]) for r in raw["stage"]}
    waves = {}
    for row in raw["waves"]:
        wave = integer(row["wave"])
        if wave < 1 or integer(row["count"]) < 1 or number(row["interval"]) <= 0:
            raise ValueError("Invalid wave schedule")
        if row["enemy"] not in enemies:
            raise ValueError("Unknown enemy reference")
        waves.setdefault(wave, []).append(row)
    result, cumulative_gold, cumulative_crystals = [], int(settings["startGold"]), 0
    complete_prefix = True
    for wave in range(1, args.through + 1):
        if wave not in waves:
            complete_prefix = False
            result.append({"round": wave, "evidence": "missing_runtime_wave", "enemy_count": None,
                           "hp": None, "gold_all_kills": None, "crystal_reward": None,
                           "cumulative_gold": None, "cumulative_reward_crystals": None,
                           "last_spawn_seconds": None, "hp_per_spawn_second": None})
            continue
        count, hp, gold, last = 0, 0, 0, 0.0
        for row in waves[wave]:
            n, enemy = integer(row["count"]), enemies[row["enemy"]]
            count += n
            hp += n * integer(enemy["health"])
            gold += n * integer(enemy["gold"])
            last = max(last, number(row["start"]) + (n - 1) * number(row["interval"]))
        reward = int(settings.get(f"waveReward{wave}", settings["waveReward"]))
        cumulative_gold += gold
        cumulative_crystals += reward
        result.append({"round": wave, "evidence": "static_all_kills_completion_assumption",
                       "enemy_count": count, "hp": hp, "gold_all_kills": gold, "crystal_reward": reward,
                       "cumulative_gold": cumulative_gold if complete_prefix else None,
                       "cumulative_reward_crystals": cumulative_crystals if complete_prefix else None,
                       "last_spawn_seconds": round(last, 4),
                       "hp_per_spawn_second": round(hp / last, 4) if last > 0 else None})
    def price(step):
        return int(settings["playerFirstCost"] + (step-1)*settings["playerSecondIncrease"]
                   + settings["playerIncreaseStep"]*(step-1)*(step-2)/2)
    def earliest(cost):
        return next((r["round"] for r in result if r["cumulative_gold"] is not None
                     and r["cumulative_gold"] >= cost), None)
    growth = {"evidence": "static_arithmetic_not_simulation", "damage_formula": "floor(max(0,D*(1-defense/100))+.5)",
              "player_same_stat": [], "player_split_first_steps": [], "player_damage_steps": [], "tower_dps": []}
    for step in range(1, 5):
        cost = sum(price(n) for n in range(1, step+1))
        growth["player_same_stat"].append({"step": step, "cost": price(step), "cumulative_cost": cost,
                                          "affordable_by_round_end": earliest(cost)})
        growth["player_split_first_steps"].append({"different_stats": step, "cost": price(1)*step,
                                                  "affordable_by_round_end": earliest(price(1)*step)})
    delta = number(next(r["delta"] for r in raw["upgrades"] if r["target"] == "Player" and r["stat"] == "damage"))
    for step in range(4):
        value = settings["playerDamage"] + step*delta
        for kind, enemy in enemies.items():
            hit = damage(value, number(enemy["defense"]))
            shots = math.ceil(number(enemy["health"])/hit) if hit > 0 else None
            growth["player_damage_steps"].append({"step": step, "enemy": kind, "input_damage": value,
                "hit": hit, "sustained_dps": hit*settings["playerRate"], "shots": shots,
                "attack_interval_seconds_excluding_flight": (shots-1)/settings["playerRate"] if shots else None})
    for tower in raw["towers"]:
        if tower["id"] == "Support":
            continue
        for kind, enemy in enemies.items():
            dps = damage(number(tower["damage"]), number(enemy["defense"]))*number(tower["rate"])
            growth["tower_dps"].append({"tower": tower["id"], "enemy": kind, "per_target_dps": round(dps, 4),
                "max_targets_sustained_dps": round(dps*(3 if tower["id"] == "Multi" else 1), 4)})
    manifest = {"project": str(project), "sources_sha256": {str(p.relative_to(project)): hashlib.sha256(p.read_bytes()).hexdigest() for p in files.values()},
                "limitations": ["No Unity execution or live loaded-value inspection", "No Notion rules inferred", "No boss/augmentation simulation",
                    "All kills and completion assumed; no spending in cumulative totals", "HP/spawn span is a density index, not required defense DPS"],
                "rounds": result}
    out.mkdir(parents=True, exist_ok=True)
    outputs[0].write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
    with outputs[1].open("w", encoding="utf-8-sig", newline="") as stream:
        writer = csv.DictWriter(stream, fieldnames=list(result[0]))
        writer.writeheader()
        writer.writerows(result)
    outputs[2].write_text(json.dumps(growth, ensure_ascii=False, indent=2), encoding="utf-8")
    observed = [r for r in result if r["hp_per_spawn_second"] is not None]
    maximum = max((r["hp_per_spawn_second"] for r in observed), default=1)
    bars = []
    for i, r in enumerate(result):
        x = 65+i*65
        value = r["hp_per_spawn_second"]
        height = 200*value/maximum if value is not None else 0
        bars.append(f'<rect x="{x}" y="{265-height:.2f}" width="38" height="{height:.2f}" fill="#4376cc"/><text x="{x}" y="285">R{r["round"]}</text><text x="{x}" y="{250-height:.2f}">{value:.1f}</text>' if value is not None else f'<text x="{x}" y="285">R{r["round"]}</text><text x="{x}" y="245">N/A</text>')
    outputs[3].write_text(f'<svg xmlns="http://www.w3.org/2000/svg" width="{args.through*65+100}" height="340"><rect width="100%" height="100%" fill="white"/><g font-family="Arial" font-size="13" fill="#222"><text x="30" y="25">Static HP / spawn span (HP/s). NOT required DPS or measured difficulty.</text><path d="M45 55V265H{args.through*65+70}" stroke="#888" fill="none"/>{"".join(bars)}<text x="30" y="325">Source: active-candidate CSV; all spawned HP; missing rounds remain N/A.</text></g></svg>', encoding="utf-8")
    print(json.dumps({"outputs": [str(p) for p in outputs], "missing_rounds": [r["round"] for r in result if r["hp"] is None]}))


if __name__ == "__main__":
    main()
