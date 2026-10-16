from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

root = Path(__file__).resolve().parent
font = ImageFont.truetype(str(root.parents[2] / "Assets/Resources/CoreGuardian/Fonts/Galmuri11.ttf"), 20)
frames = []
for path in sorted((root / "frames").glob("*.png")):
    source = Image.open(path).convert("RGB")
    image = Image.new("RGB", (1152, 368), "#151a20")
    draw = ImageDraw.Draw(image)
    for i, label in enumerate(["단일 1 · 발사 섬광", "멀티 3 · 중앙 → 양옆", "지원 1 · 수정 호흡"]):
        draw.text((i * 384 + 12, 10), label, font=font, fill="white")
        image.paste(source.crop((i*240+24,44,i*240+216,204)).resize((384,320),Image.Resampling.NEAREST),(i*384,48))
    frames.append(image)
assert len(frames) == 48
frames[0].save(root / "selected_applied.gif", save_all=True, append_images=frames[1:], duration=50, loop=0, optimize=False)
frames[10].save(root / "selected_firing.png")
print("48 frames, 20fps, nearest 2x; manual firing clock using production animation")
