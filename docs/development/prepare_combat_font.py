from pathlib import Path
import sys, shutil, hashlib, json

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT/'docs/development/font_tools_vendor'))
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont

source = ROOT/'docs/art/20261010_CombatArtReview/Fonts/PixelifySans'
dest = ROOT/'Assets/Art/Combat/20261010/Fonts'
dest.mkdir(parents=True, exist_ok=True)
original = TTFont(source/'PixelifySans-Variable.ttf')
bold = instantiateVariableFont(original, {'wght':700}, inplace=False, updateFontNames=True)
bold.save(dest/'PixelifySans-Bold.ttf')
shutil.copyfile(source/'OFL.txt',dest/'OFL.txt')
check = TTFont(dest/'PixelifySans-Bold.ttf')
assert 'fvar' not in check
assert all(ord(c) in check.getBestCmap() for c in '0123456789+-.,')
report = {'source':'docs/art/20261010_CombatArtReview/Fonts/PixelifySans/PixelifySans-Variable.ttf','output':'Assets/Art/Combat/20261010/Fonts/PixelifySans-Bold.ttf','tool':'fontTools '+__import__('fontTools').__version__,'wght':check['OS/2'].usWeightClass,'isStatic':'fvar' not in check,'family':check['name'].getDebugName(1),'style':check['name'].getDebugName(2),'sha256':hashlib.sha256((dest/'PixelifySans-Bold.ttf').read_bytes()).hexdigest(),'license':'OFL.txt copied unchanged; original license specifies no Reserved Font Name'}
(ROOT/'docs/development/combat_font_static_20261010.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
