"""Read-only pixel/import audit; does not create or edit game images."""
import hashlib
import json
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[3]
original = root / 'Assets/Art/ScreenTests/20260930/Sprites/player.png'
source = Image.open(original).convert('RGBA')
palette = {c for c in source.get_flattened_data() if c[3]}
folder = root / 'Assets/Resources/CoreGuardian/PlayerFacing'
views = []
new_pixels = 0
settings = []
for direction in range(8):
    layers = {}
    for name in ('Body', 'Arm', 'LeftLeg', 'RightLeg'):
        path = folder / f'D{direction}/D{direction}_{name}.png'
        layer = Image.open(path).convert('RGBA')
        assert layer.size == source.size
        new_pixels += sum(c not in palette for c in layer.get_flattened_data() if c[3])
        layers[name] = layer
        meta = path.with_suffix('.png.meta').read_text(encoding='utf-8')
        settings.append('spritePixelsToUnits: 32' in meta and 'filterMode: 0' in meta and 'enableMipMap: 0' in meta)
    composite = Image.new('RGBA', source.size)
    for name in ('LeftLeg', 'RightLeg', 'Body', 'Arm'):
        composite.alpha_composite(layers[name])
    views.append(composite)

visible_diff = sum((a[3] or b[3]) and a != b for a, b in zip(source.get_flattened_data(), views[5].get_flattened_data()))
print(json.dumps(dict(sourceSHA256=hashlib.sha256(original.read_bytes()).hexdigest(),
    canvas=list(source.size), directions=8, layers=32, pixelsOutsideSourcePalette=new_pixels,
    originalDirectionVisiblePixelDifferences=visible_diff, point32PPUNoMips=all(settings),
    distinctComposites=len({v.tobytes() for v in views})), indent=2))
assert new_pixels == 0 and visible_diff == 0 and all(settings)
