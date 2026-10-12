from PIL import Image
from pathlib import Path
import json
root=Path(__file__).parent
palette=json.loads((root/'palette_v3.json').read_text())
result=[]
for kind in ['normal','enraged']:
    im=Image.open(root/f'boss_{kind}_v3_64x80.png').convert('RGBA')
    allowed={tuple(x) for x in palette['base']+(palette['status'] if kind=='enraged' else [])}
    colors={tuple(x[:3]) for x in im.get_flattened_data() if x[3]}
    alpha=set(im.getchannel('A').get_flattened_data())
    bbox=im.getchannel('A').getbbox()
    assert im.size==(64,80)
    assert alpha=={0,255}
    assert colors<=allowed
    assert bbox[3]==74
    result.append(dict(state=kind,size=im.size,alpha=sorted(alpha),bbox=bbox,colors=len(colors),palette_match=True,bottom_row=73))
(root/'validation_v3.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps(result))
