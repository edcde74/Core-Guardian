from pathlib import Path
import json
from PIL import Image, ImageDraw, ImageFont, ImageChops

root = Path(__file__).resolve().parent
project = root.parents[2]
font = ImageFont.truetype(str(project / "Assets/Resources/CoreGuardian/Fonts/Galmuri11.ttf"), 20)
small = ImageFont.truetype(str(project / "Assets/Resources/CoreGuardian/Fonts/Galmuri11.ttf"), 16)
labels = {
    "Single": ["1 · 발사 섬광", "2 · 포신 충전 흐름", "3 · 이중 맥동"],
    "Multi": ["1 · 3포구 동시", "2 · 하부 → 포구", "3 · 중앙 → 양옆"],
    "Support": ["1 · 수정 호흡", "2 · 상승 흐름", "3 · 좌우 교대"],
}
report = []
stills = []
for role, names in labels.items():
    sources = [Image.open(p).convert("RGB") for p in sorted((root / role).glob("*.png"))]
    if len(sources) != 48:
        raise RuntimeError(f"{role}: expected 48, got {len(sources)}")
    rows = []
    individuals = [[], [], []]
    for index, source in enumerate(sources):
        row = Image.new("RGB", (1152, 368), "#151a20")
        for variant in range(3):
            # Preserve the exact same source pixel density for all nine variants; only enlarge by nearest 2x.
            crop = source.crop((variant * 240 + 24, 44, variant * 240 + 216, 204)).resize((384, 320), Image.Resampling.NEAREST)
            tile = Image.new("RGB", (384, 368), "#151a20")
            draw = ImageDraw.Draw(tile)
            draw.text((12, 8), names[variant], font=font, fill="#f1f4f7")
            cycle = (index / 20) % 1.2
            state = ("지원 반응" if role == "Support" else "발사") if .5 <= cycle < .68 else "복귀" if .68 <= cycle < .9 else "대기 / 충전"
            draw.text((260, 14), state, font=small, fill="#9ab6c3")
            tile.paste(crop, (0, 48))
            individuals[variant].append(tile)
            row.paste(tile, (variant * 384, 0))
        rows.append(row)
    rows[0].save(root / f"{role}_three_options.gif", save_all=True, append_images=rows[1:], duration=50, loop=0, optimize=False)
    for variant, frames in enumerate(individuals):
        frames[0].save(root / f"{role}_{variant + 1}.gif", save_all=True, append_images=frames[1:], duration=50, loop=0, optimize=False)
    rows[6].save(root / f"{role}_charging.png")
    rows[10].save(root / f"{role}_firing.png")
    stills.append(rows[10])
    pair_different = []
    for a, b in ((0, 1), (1, 2), (0, 2)):
        differing = 0
        for frame in sources:
            left = frame.crop((a * 240 + 24,44,a * 240 + 216,204))
            right = frame.crop((b * 240 + 24,44,b * 240 + 216,204))
            if any(p != q and ((p[2] > p[0]+12 and p[1] > p[0]+4) or (q[2] > q[0]+12 and q[1] > q[0]+4)) for p,q in zip(left.get_flattened_data(),right.get_flattened_data())):
                differing += 1
        pair_different.append({"a": a+1,"b": b+1,"differentFrames": differing})
    transitions = sum(ImageChops.difference(sources[i], sources[i-1]).getbbox() is not None for i in range(1,48))
    report.append({"role": role,"variants":3,"frames":48,"fps":20,"changingTransitions":transitions,"pairDifferences":pair_different})
sheet = Image.new("RGB", (1152, 1104), "#151a20")
for i, image in enumerate(stills):
    sheet.paste(image, (0, i * 368))
sheet.save(root / "all_nine_firing.png")
(root / "encoding_check.json").write_text(json.dumps({"nearestMagnification":2,"roleRows":report,"individualGifs":9,"pairDifferenceScope":"Blue/cyan foreground pixels only; excludes gray floor differences"},indent=2),encoding="utf-8")
print(json.dumps(report))
