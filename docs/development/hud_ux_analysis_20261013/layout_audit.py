"""Static UI geometry from reviewed CoreGuardianHud.cs values. No Unity mutations.
Rectangular coverage != measured opaque board occlusion. No runtime resize tests.
"""
import hashlib
import json
import math
from pathlib import Path

root = Path(__file__).resolve().parents[3]
out = Path(__file__).resolve().parent
hud = root / "Assets/Scripts/CoreGuardianHud.cs"
# CoreGuardianHud.Box uses identical anchorMin/anchorMax/pivot, so this model applies.
panels = {
    "TopStatus": (.5, 1, 0, -12, 920, 64),
    "BottomControls": (.5, 0, 0, 12, 1160, 68),
    "TowerInformation": (0, 1, 16, -120, 330, 400),
    "DamagePanel": (1, 1, -16, -104, 280, 490),
    "TowerInstallPanel": (0, 0, 380, 88, 492, 246),
    "PlayerGrowthPanel": (1, 0, -380, 88, 330, 250),
    "Reload": (0, 1, 1630, -16, 260, 48),
    "DamageFold": (0, 1, 1848, -610, 56, 38),
    "InstallFold": (0, 1, 380, -938, 50, 34),
    "PlayerFold": (0, 1, 1490, -938, 50, 34),
    "CancelPlacement": (0, 1, 18, -1000, 270, 48),
}


def rect(spec, width, height):
    ax, ay, px, py, w, h = spec
    x = ax*width + px - ax*w
    y_bottom = ay*height + py - ay*h
    return (x, height-y_bottom-h, w, h)  # top-left coordinates


results = []
for w, h in [(1920, 1080), (1280, 720), (960, 540), (1920, 1200), (2560, 1080), (1280, 1024)]:
    scale = math.sqrt((w/1920)*(h/1080))
    cw, ch = w/scale, h/scale
    items = []
    for name, spec in panels.items():
        x, y, rw, rh = rect(spec, cw, ch)
        visible_w = max(0, min(cw, x+rw)-max(0, x))
        visible_h = max(0, min(ch, y+rh)-max(0, y))
        items.append({"name": name, "rect_screen_px": [round(v*scale, 2) for v in (x, y, rw, rh)],
                      "area_visibility_fraction": round(visible_w*visible_h/(rw*rh), 6),
                      "fully_inside": x >= 0 and y >= 0 and x+rw <= cw+1e-6 and y+rh <= ch+1e-6})
    results.append({"resolution": [w, h], "scale": scale, "canvas_logical": [cw, ch],
                    "font18_effective_px": 18*scale, "font19_effective_px": 19*scale,
                    "font32_effective_px": 32*scale, "elements": items})
coverage = []
groups = {"main_default_without_selected_tower": ["TopStatus", "BottomControls", "DamagePanel"],
          "main_with_selected_tower": ["TopStatus", "BottomControls", "DamagePanel", "TowerInformation"],
          "main_with_selected_tower_and_both_growth_install_panels": ["TopStatus", "BottomControls", "DamagePanel", "TowerInformation", "TowerInstallPanel", "PlayerGrowthPanel"]}
for name, names in groups.items():
    area = sum(panels[n][4]*panels[n][5] for n in names)
    coverage.append({"state": name, "sum_rect_area": area, "screen_percentage": area/(1920*1080)*100,
                     "note": "Selected main panels only; rectangle sum, not all controls, union or opaque board coverage"})
report = {"evidence": "static_geometry_not_live_resize_test", "hud_source_sha256": hashlib.sha256(hud.read_bytes()).hexdigest(),
          "source": str(hud.relative_to(root)), "reference": [1920, 1080], "match": .5,
          "resolutions": results, "main_panel_rect_coverage": coverage}
(out / "layout_metrics.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
# Check the model against the historical1280x720 screenshot edges and known aspect failure.
r720 = results[1]
d = next(e for e in r720["elements"] if e["name"] == "DamagePanel")
assert d["rect_screen_px"] == [1082.67, 69.33, 186.67, 326.67]
rwide = results[4]
assert not next(e for e in rwide["elements"] if e["name"] == "InstallFold")["fully_inside"]
r1610 = results[3]
assert next(e for e in r1610["elements"] if e["name"] == "DamageFold")["area_visibility_fraction"] == 0
assert all(e["fully_inside"] for e in r720["elements"])
print(json.dumps({"output": str(out / "layout_metrics.json"), "model_checks": 4,
                  "states_percent": coverage, "clipped": [{"resolution": r["resolution"], "elements": [e["name"] for e in r["elements"] if not e["fully_inside"]]} for r in results]}, ensure_ascii=False))
