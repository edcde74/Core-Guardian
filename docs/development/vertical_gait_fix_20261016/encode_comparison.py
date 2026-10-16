"""Encode actual Unity-rendered frames for inspection; no source sprite editing."""
from pathlib import Path
import json
from PIL import Image

folder = Path(__file__).resolve().parent
before = [Image.open(p).convert("RGBA") for p in sorted((folder / "before_frames").glob("*.png"))]
after = [Image.open(p).convert("RGBA") for p in sorted((folder / "after_frames").glob("*.png"))]
assert len(before) == len(after) == 48
comparisons = []
for a, b in zip(before, after):
    out = Image.new("RGBA", (480, 576))
    out.paste(a.crop((0, 0, 240, 576)), (0, 0))
    out.paste(b.crop((0, 0, 240, 576)), (240, 0))
    comparisons.append(out)
comparisons[0].save(folder / "before_after.webp", save_all=True, append_images=comparisons[1:], duration=40, loop=0, lossless=True, method=6)
comparisons[12].save(folder / "comparison.png")
checks = {"frames": 48, "renderScale": 3, "columns": "before / after", "rows": "Down / Up", "uniqueAfterFrames": len({p.tobytes() for p in after})}
for name, rect in {"leftBoot": (98, 218, 120, 251), "rightBoot": (121, 218, 143, 251)}.items():
    checks[name + "DistinctRenderRegions"] = len({p.crop(rect).tobytes() for p in after})
    assert checks[name + "DistinctRenderRegions"] > 4
(folder / "render_checks.json").write_text(json.dumps(checks, indent=2), encoding="utf-8")
print(json.dumps(checks))
