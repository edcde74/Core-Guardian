"""Run an audio script with this folder's isolated packages, without global changes."""
import sys, runpy
from pathlib import Path
HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(HERE/'.local/packages'))
if len(sys.argv)<2:
    raise SystemExit('Usage: python python_audio.py path/to/audio_script.py [arguments]')
target=Path(sys.argv[1]).resolve()
sys.argv=sys.argv[1:]
sys.path.insert(0,str(target.parent))
runpy.run_path(str(target),run_name='__main__')
