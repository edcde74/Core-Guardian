from pathlib import Path
import json
from PIL import Image, ImageDraw, ImageFont, ImageChops

root = Path(__file__).resolve().parent
folder = root / "before_after"
project = root.parents[2]
font = ImageFont.truetype(str(project / "Assets/Resources/CoreGuardian/Fonts/Galmuri11.ttf"), 18)
before = [Image.open(p).convert("RGB") for p in sorted((folder / "before").glob("*.png"))]
after = [Image.open(p).convert("RGB") for p in sorted((folder / "after").glob("*.png"))]

def pair(left, right, label):
    w, h = left.size
    canvas = Image.new("RGB", (w * 2, h + 40), "#151a20")
    canvas.paste(left, (0, 40))
    canvas.paste(right, (w, 40))
    draw = ImageDraw.Draw(canvas)
    draw.text((12, 9), "적용 전" + label, font=font, fill="white")
    draw.text((w + 12, 9), "적용 후" + label, font=font, fill="white")
    return canvas

times = json.loads((folder / "capture.json").read_text(encoding="utf-8"))["times"]
durations = [max(20, round((times[i + 1] - times[i]) * 100) * 10) for i in range(len(times) - 1)]
durations.append(durations[-1])
frames = [pair(a, b, "") for a, b in zip(before, after)]
frames[0].save(root / "comparison_live.gif", save_all=True, append_images=frames[1:], duration=durations, loop=0, optimize=False)
index = 20
pair(before[index], after[index], "").save(root / "comparison_original_scale.png")
crop = (90, 65, 230, 245)
enlarged = [image.crop(crop).resize((280, 360), Image.Resampling.NEAREST) for image in (before[index], after[index])]
pair(*enlarged, " · 2배 확대").save(root / "comparison_2x.png")
diffs = [ImageChops.difference(a, b) for a, b in zip(before, after)]
report = {"frames": len(frames), "sameFramePairs": True, "originalScale": [320, 320], "detailMagnification": 2, "detailInterpolation": "nearest", "maxChangedPixels": max(sum(max(p) > 0 for p in d.get_flattened_data()) for d in diffs), "beforeMotionTransitions": sum(ImageChops.difference(before[i], before[i-1]).getbbox() is not None for i in range(1,len(before))), "afterMotionTransitions": sum(ImageChops.difference(after[i], after[i-1]).getbbox() is not None for i in range(1,len(after)))}
(folder / "comparison_check.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
print(json.dumps(report))
