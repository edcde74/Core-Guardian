from pathlib import Path
import json
from PIL import Image

root = Path(__file__).resolve().parent
for name in ['live_before_mount_fix', 'live_after']:
    report = json.loads((root / f'{name}.json').read_text(encoding='utf-8'))
    files = sorted((root / f'{name}_frames').glob('*.png'))
    assert len(files) == 80
    frames = [Image.open(p).convert('RGB').resize((880,520),Image.Resampling.NEAREST) for p in files]
    times = report['times']
    durations = [max(10,round((times[i+1]-times[i])*1000/10)*10) for i in range(79)]
    durations.append(durations[-1])
    frames[0].save(root/f'{name}.gif',save_all=True,append_images=frames[1:],duration=durations,loop=0,optimize=False)
    print(f'{name}: 80 actual camera frames, nearest 2x, {sum(durations)}ms; HUD excluded in crop')
