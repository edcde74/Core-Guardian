"""Encode actual Unity captures losslessly; verify fixed reference and foot regions."""
from pathlib import Path
import json
from PIL import Image

folder = Path(__file__).resolve().parent
results = {}
for name, duration in (("walk", 1000 / 48), ("idle", 40)):
    frames = [Image.open(p).convert("RGBA") for p in sorted((folder / (name + "_frames")).glob("*.png"))]
    assert frames, name
    reference = (0, 0, 288, 1344) if name == "walk" else (0, 0, 360, 360)
    reference_bytes = frames[0].crop(reference).tobytes()
    assert all(frame.crop(reference).tobytes() == reference_bytes for frame in frames), "Source comparison moved"
    if name == "idle":
        feet = (370, 270, 710, 345)
        feet_bytes = frames[0].crop(feet).tobytes()
        assert all(frame.crop(feet).tobytes() == feet_bytes for frame in frames), "Idle feet moved"
    frames[0].save(folder / (name + "_original.webp"), save_all=True, append_images=frames[1:], duration=duration, loop=0, lossless=True, method=6)
    frames[len(frames) // 4].save(folder / (name + "_poster.png"))
    results[name] = {"frames": len(frames), "sourceColumnFixed": True, "uniqueFrames": len({frame.tobytes() for frame in frames}), "lossless": True}
    if name == "idle":
        results[name]["feetFixed"] = True
(folder / "capture_checks.json").write_text(json.dumps(results, indent=2), encoding="utf-8")
print(json.dumps(results))
