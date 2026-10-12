"""Behavior checks of the analysis helper, with temporary copies; no game mutation."""
import hashlib
import importlib.util
import json
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

sys.dont_write_bytecode = True

project = Path(__file__).resolve().parents[3]
script = project / "docs/skills/core-guardian-balance/scripts/analyze_rounds.py"
spec = importlib.util.spec_from_file_location("cg_balance", script)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
paths = list((project / "Assets/Resources/CoreGuardian").glob("*.csv"))
before = {str(p): hashlib.sha256(p.read_bytes()).hexdigest() for p in paths}
checks = []
with tempfile.TemporaryDirectory(prefix="cg-balance-check-") as temp:
    root = Path(temp)
    copied = root / "project/Assets/Resources/CoreGuardian"
    copied.mkdir(parents=True)
    for path in paths:
        shutil.copy2(path, copied / path.name)
    out = root / "reports"
    command = [sys.executable, str(script), "--project", str(root / "project"), "--out", str(out)]
    subprocess.run(command, check=True, capture_output=True)
    rounds = json.loads((out / "rounds.json").read_text(encoding="utf-8"))["rounds"]
    growth = json.loads((out / "growth.json").read_text(encoding="utf-8"))
    assert rounds[8]["cumulative_gold"] == 510 and rounds[8]["cumulative_reward_crystals"] == 205
    assert rounds[8]["hp"] == 635 and rounds[8]["last_spawn_seconds"] == 22.8
    checks.append("Known current economy/schedule matches raw CSV totals")
    assert rounds[9]["hp"] is None and rounds[9]["cumulative_gold"] is None
    checks.append("Missing round10 stays unknown rather than zero or fabricated")
    assert [r["affordable_by_round_end"] for r in growth["player_same_stat"][:3]] == [3, 5, 8]
    assert [r["affordable_by_round_end"] for r in growth["player_split_first_steps"]] == [3, 5, 6, 8]
    checks.append("Per-stat concentrated and split costs remain distinct")
    assert module.damage(12.5, 0) == 13 and module.damage(12.5, 1) == 12
    assert module.damage(50, 100) == 0 and module.damage(50, 150) == 0
    checks.append("Half-up hit rounding and zero floor match CombatRules")
    shots = [r["shots"] for r in growth["player_damage_steps"] if r["enemy"] == "Attack"]
    assert shots == [3, 3, 2, 2]
    checks.append("Growth reports hit-count breakpoints, not DPS alone")
    result = subprocess.run(command, capture_output=True)
    assert result.returncode != 0 and b"Refusing to overwrite" in result.stderr
    checks.append("Existing analysis output cannot be overwritten")
    for protected in ("Assets", "Packages", "ProjectSettings", ".git", "Library", "UserSettings"):
        trial = command[:-1] + [str(root / "project" / protected / "analysis")]
        result = subprocess.run(trial, capture_output=True)
        assert result.returncode != 0 and b"Output must" in result.stderr
    checks.append("Writes into Unity/config/cache/Git folders rejected")
    wave = copied / "waves.csv"
    wave.write_text(wave.read_text(encoding="utf-8-sig").replace("enemy,count", "enemyKind,count"), encoding="utf-8")
    result = subprocess.run(command[:-1] + [str(root / "fresh")], capture_output=True)
    assert result.returncode != 0 and b"Unsupported waves schema" in result.stderr
    assert not (root / "fresh").exists()
    checks.append("Schema drift fails before output rather than silent guesses")
assert before == {str(p): hashlib.sha256(p.read_bytes()).hexdigest() for p in paths}
checks.append("Original project CSV hashes unchanged after behavioral checks")
print(json.dumps({"passed": len(checks), "checks": checks}, ensure_ascii=False, indent=2))
