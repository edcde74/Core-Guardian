from pathlib import Path
import subprocess,json,hashlib,sys
root=Path(__file__).parent
paths=['Assets/Scripts/CoreGuardianSelectionOutline.cs','Assets/Scripts/CoreGuardianPhotoShadow.cs','Assets/Scripts/CoreGuardianObjectEmphasis.cs']
if sys.argv[1]=='save':
 for p in paths:
  (root/('after_'+Path(p).name+'.txt')).write_bytes(Path(p).read_bytes())
  old=subprocess.check_output(['git','-c','safe.directory=C:/Users/edcde/OneDrive/Desktop/CD','show','a135d9d:'+p]);(root/('before_'+Path(p).name+'.txt')).write_bytes(old)
else:
 side=sys.argv[1];other='before' if side=='after' else 'after'
 for p in paths:
  if Path(p).read_bytes()!=(root/(other+'_'+Path(p).name+'.txt')).read_bytes():raise Exception('Source changed externally: '+p)
 for p in paths:Path(p).write_bytes((root/(side+'_'+Path(p).name+'.txt')).read_bytes())
 print('Swapped only the three verified owned sources to',side)
