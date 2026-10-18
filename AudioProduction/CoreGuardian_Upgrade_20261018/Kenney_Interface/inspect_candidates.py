import hashlib, json, struct
from pathlib import Path

root = Path(__file__).resolve().parent
names = ['confirmation_001', 'confirmation_002', 'glass_003', 'maximize_006', 'pluck_001']
report = []
for name in names:
    path = root / 'Originals' / 'Audio' / (name + '.ogg')
    raw = path.read_bytes()
    start = raw.index(b'\x01vorbis')
    rate = struct.unpack_from('<I', raw, start + 12)[0]
    page = 0
    last = 0
    while page < len(raw):
        assert raw[page:page + 4] == b'OggS'
        granule = struct.unpack_from('<Q', raw, page + 6)[0]
        if granule != 0xffffffffffffffff:
            last = granule
        segments = raw[page + 26]
        page += 27 + segments + sum(raw[page + 27:page + 27 + segments])
    report.append(dict(file=path.name, duration=last/rate, sample_rate=rate, sha256=hashlib.sha256(raw).hexdigest()))
(root / 'candidates.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report, indent=2))
