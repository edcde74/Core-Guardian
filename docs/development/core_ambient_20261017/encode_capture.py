from pathlib import Path
import json
from PIL import Image, ImageChops

root = Path(__file__).resolve().parent
paths = sorted((root / "frames").glob("*.png"))
frames = [Image.open(path).convert("RGB") for path in paths]
samples = json.loads((root / "animation_capture.json").read_text(encoding="utf-8"))["samples"]
durations = [max(20, round((samples[i + 1]["time"] - samples[i]["time"]) * 100) * 10) for i in range(len(samples) - 1)]
durations.append(durations[-1])
frames[0].save(root / "core_ambient_live.gif", save_all=True, append_images=frames[1:], duration=durations, loop=0, optimize=False)
changed = sum(ImageChops.difference(frames[i], frames[i - 1]).getbbox() is not None for i in range(1, len(frames)))
report = {"frames": len(frames), "changingTransitions": changed, "width": frames[0].width, "height": frames[0].height, "durationMs": sum(durations), "resized": False, "source": "Actual camera render crop; no HUD; no temporary zoom"}
(root / "gif_check.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
print(json.dumps(report))
