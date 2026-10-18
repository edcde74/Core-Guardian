"""Offline audio-tool smoke checks. Source audio and Unity are read-only."""
from pathlib import Path
import sys, json, subprocess, hashlib, importlib.metadata as metadata
HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
sys.path.insert(0, str(HERE/'.local/packages'))
import numpy as np
import soundfile as sf
import pyloudnorm as pyln
import mido, imageio_ffmpeg
from pedalboard import Pedalboard, HighpassFilter, Compressor, Reverb, Limiter, Gain

qa=HERE/'.local/qa'; qa.mkdir(exist_ok=True)
src=ROOT/'AudioProduction/CoreGuardian_ConceptAB_20261019/audio/A_scene.wav'
before=hashlib.sha256(src.read_bytes()).hexdigest()
x,sr=sf.read(src,dtype='float32',always_2d=True)
# Pedalboard Limiter hard-clips at 0dB, rather than using threshold as output ceiling.
# Explicit final attenuation leaves headroom in this disposable function-check render.
board=Pedalboard([HighpassFilter(cutoff_frequency_hz=50),Compressor(threshold_db=-20,ratio=2),Reverb(room_size=.12,wet_level=.06,dry_level=.94),Limiter(threshold_db=-2),Gain(gain_db=-3)])
y=board(x.T,sr).T
assert y.shape==x.shape and np.isfinite(y).all() and np.max(np.abs(y))<1
sf.write(qa/'processed.wav',y,sr,subtype='PCM_16')
ffmpeg=imageio_ffmpeg.get_ffmpeg_exe()
ver=subprocess.run([ffmpeg,'-version'],capture_output=True,text=True,check=True).stdout.splitlines()[0]
for args in [
 ['-i',str(qa/'processed.wav'),'-c:a','libvorbis','-q:a','5',str(qa/'encoded.ogg')],
 ['-i',str(qa/'encoded.ogg'),'-c:a','pcm_s16le',str(qa/'decoded.wav')]]:
 subprocess.run([ffmpeg,'-nostdin','-hide_banner','-loglevel','error','-y',*args],check=True,capture_output=True)
decoded,dr=sf.read(qa/'decoded.wav',always_2d=True)
assert dr==sr and abs(len(decoded)-len(x))<sr*.02 and np.isfinite(decoded).all()
mid=mido.MidiFile();track=mido.MidiTrack();mid.tracks.append(track)
track.append(mido.MetaMessage('set_tempo',tempo=mido.bpm2tempo(120)))
for note in [60,64,67,72]:
 track.append(mido.Message('note_on',note=note,velocity=70,time=0))
 track.append(mido.Message('note_off',note=note,velocity=0,time=240))
mid.save(qa/'midi_roundtrip.mid')
assert len([m for m in mido.MidiFile(qa/'midi_roundtrip.mid').tracks[0] if m.type=='note_on'])==4
meter=pyln.Meter(sr)
measurements={}
for style in ['A','B']:
 path=src.with_name(style+'_scene.wav');z,r=sf.read(path)
 measurements[style]={'lufs':float(pyln.Meter(r).integrated_loudness(z)),'peak_dbfs':float(20*np.log10(np.max(np.abs(z))))}
assert hashlib.sha256(src.read_bytes()).hexdigest()==before
packages={n:metadata.version(n) for n in ['numpy','scipy','soundfile','pedalboard','pyloudnorm','mido','imageio-ffmpeg']}
report={'checks':{'dsp_render':True,'wav_ogg_wav':True,'midi_roundtrip':True,'loudness_analysis':True,'source_unchanged':True},'packages':packages,'ffmpeg':ver,'ffmpeg_path':ffmpeg,'concept_measurements':measurements,'note':'Tool function checks only. No direct listening, Unity changes or API calls.'}
(HERE/'verification.json').write_text(json.dumps(report,indent=2),encoding='utf-8',newline='\n')
print(json.dumps(report,indent=2))
