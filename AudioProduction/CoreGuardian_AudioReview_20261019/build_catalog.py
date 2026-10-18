"""Build a local audition catalogue. Never writes game assets or original audio."""
import array, hashlib, json, math, struct, sys, wave
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
DERIVED = HERE / 'derived'
DERIVED.mkdir(exist_ok=True)
RPG = 'Assets/Leohpaz/RPG_Essentials_Free/'
UI = 'AudioProduction/CoreGuardian_Upgrade_20261018/Kenney_Interface/Originals/Audio/'
JINGLE = 'AudioProduction/CoreGuardian_Victory_20261019/Kenney_MusicJingles/Originals/Audio/'
RUN = 'Assets/Resources/CoreGuardian/Audio/'
GROUPS = []

def clip(title, path, note='', source=''):
    return dict(title=title, path=path, note=note, source=source or ('Kenney · CC0' if 'Kenney' in path else '기존 취득 팩 · Asset Store EULA' if path.startswith('Assets/Leohpaz') else '현재 프로젝트 리소스'))

def group(key, title, category, options, current=None, note='', kind='compare'):
    assert len(options) == 3
    GROUPS.append(dict(id=key, title=title, category=category, clips=options, current=current, note=note, kind=kind))

def variants(key, source):
    # Audition-only transformations of approved 16-bit PCM; keep original samples intact.
    with wave.open(str(ROOT / source), 'rb') as w:
        channels, rate, width = w.getnchannels(), w.getframerate(), w.getsampwidth()
        raw = w.readframes(w.getnframes())
    assert width == 2
    values = array.array('h', raw)
    if sys.byteorder != 'little': values.byteswap()
    original = [v / 32768 for v in values]
    result = []
    for mode in ('short', 'soft'):
        samples = original.copy()
        if mode == 'short':
            frames = max(1, round(len(samples) / channels * .82))
            samples = samples[:frames * channels]
            fade = min(frames, round(rate * .06))
            for frame in range(frames - fade, frames):
                gain = (frames - 1 - frame) / max(1, fade - 1)
                for channel in range(channels): samples[frame * channels + channel] *= gain
        else:
            # Gentle high frequency reduction, no extra oscillators or gun samples.
            state = [0.] * channels
            alpha = 1 - math.exp(-2 * math.pi * 3600 / rate)
            for i, v in enumerate(samples):
                channel = i % channels
                state[channel] += alpha * (v - state[channel])
                samples[i] = state[channel] * .94
        output = DERIVED / f'{key}_{mode}.wav'
        data = array.array('h', (round(max(-1, min(.999969, v)) * 32767) for v in samples))
        if sys.byteorder != 'little': data.byteswap()
        with wave.open(str(output), 'wb') as w:
            w.setnchannels(channels); w.setsampwidth(2); w.setframerate(rate); w.writeframes(data.tobytes())
        result.append(clip('짧게 정리한 비교본' if mode == 'short' else '고역을 줄인 비교본', output.relative_to(ROOT).as_posix(), '현재 승인음 파생 · 게임 미적용', '승인 원본 파생 · 청취 페이지 전용'))
    return result

for key, title, source in [
    ('single', '단일 포탑 발사', RUN+'TowerShot/03_Aura.wav'),
    ('multi', '멀티 포탑 발사', RUN+'MultiShot/B_BrighterAttack.wav'),
    ('player', '플레이어 공격', RUN+'PlayerShot/03_Glyph.wav')]:
    group(key, title, '전투', [clip('현재 적용음', source, '승인된 원본 그대로')]+variants(key, source), 0,
          '②③은 비교용 파생본입니다. 멀티의 세 포신 리듬과 기존 게임 발사 규칙은 바꾸지 않았습니다.' if key == 'multi' else '현재 적용음은 보존됩니다. ②③은 이 페이지에서만 듣는 비교용 파생본입니다.')

group('hit','몬스터 피격','전투',[clip('현재 · Hit',RUN+'MonsterHit/03_Hit.wav'),clip('Impact flesh',RPG+'10_Battle_SFX/15_Impact_flesh_02.wav'),clip('Claw',RPG+'10_Battle_SFX/03_Claw_03.wav')],0,'현재는 살아남는 타격에 적용됩니다. 원본 이름은 제작자 분류입니다.')
group('death','몬스터 처치','전투',[clip('현재 · Flesh',RUN+'MonsterDeath/02_Flesh.wav'),clip('Enemy death',RPG+'10_Battle_SFX/69_Enemy_death_01.wav'),clip('Flee',RPG+'10_Battle_SFX/51_Flee_02.wav')],0,'치명 타격은 현재 처치음만 재생합니다.')
group('core','코어 피해','전투',[clip('현재 · Aura Glyph 2',RUN+'CoreDamage/06_Aura_Glyph_2.wav'),clip('Block',RPG+'10_Battle_SFX/39_Block_03.wav'),clip('Absorb',RPG+'8_Buffs_Heals_SFX/39_Absorb_04.wav')],0,'현재 연속 피해 묶음과 코어음 우선권은 게임에 그대로 남습니다.')
group('boss_spawn','보스 등장','전투',[clip('현재 · Encounter',RUN+'BossSpawn/01_Encounter.wav'),clip('Charge',RPG+'8_Atk_Magic_SFX/45_Charge_05.wav'),clip('Hit jingle',JINGLE+'Hit jingles/jingles_HIT00.ogg')],0,'현재는 보스가 처음 표시될 때 한 번 재생합니다.')
group('boss_accel','보스 가속 전환','전투',[clip('현재 · Speed up',RUN+'BossAcceleration/01_SpeedUp.wav'),clip('Maximize 05',UI+'maximize_005.ogg'),clip('Hit jingle 01',JINGLE+'Hit jingles/jingles_HIT01.ogg')],0,'현재 체력 50% 이하 전환 알림입니다. 가속 규칙은 변경하지 않습니다.')
group('install','포탑 설치 완료','성장·설치',[clip('현재 · Unequip',RUN+'TowerInstall/02_Unequip.wav'),clip('Use item',RPG+'10_UI_Menu_SFX/051_use_item_01.wav'),clip('Unpause',RPG+'10_UI_Menu_SFX/098_Unpause_04.wav')],0,'지원 포탑도 설치 성공 알림은 공통이며, 전투 효과음은 무음입니다.')
group('upgrade_ok','강화 성공','성장·설치',[clip('현재 · Confirmation 01',RUN+'UpgradeSuccess/01_Confirmation.ogg'),clip('Confirmation 02',UI+'confirmation_002.ogg'),clip('Glass 03',UI+'glass_003.ogg')],0,'이미 거부한 Attack buff / Defense buff / Revive는 제외했습니다. 성공 스캔 B 0.8초 유지.')
group('upgrade_fail','강화 실패·재화 부족','성장·설치',[clip('현재 · Error 06',RUN+'UpgradeFailure/03_Error006.ogg'),clip('Error 01',UI+'error_001.ogg'),clip('Error 03',UI+'error_003.ogg')],0,'현재는 강화 시도 실패에만 연결되어 있습니다.')
group('install_fail','설치 실패 알림','성장·설치',[clip('Error 02',UI+'error_002.ogg'),clip('Error 04',UI+'error_004.ogg'),clip('Error 05',UI+'error_005.ogg')],note='추가 검토 항목입니다. 현재 게임에는 별도 설치 실패음이 없습니다.')
group('remove','포탑 철거 알림','성장·설치',[clip('Back 01',UI+'back_001.ogg'),clip('Drop 01',UI+'drop_001.ogg'),clip('Scratch 02',UI+'scratch_002.ogg')],note='추가 검토 항목입니다. 철거 규칙이나 환불은 변경하지 않습니다.')
group('round_start','라운드 전투 시작','라운드·결과',[clip('현재 · Confirmation 04',RUN+'RoundStart/03_Confirmation004.ogg'),clip('Open 01',UI+'open_001.ogg'),clip('Maximize 01',UI+'maximize_001.ogg')],0,'현재 자동·수동 전투 시작에 한 번 재생합니다.')
group('round_complete','라운드 완료','라운드·결과',[clip('현재 · Confirmation 02',RUN+'RoundComplete/01_Confirmation002.ogg'),clip('Confirmation 03',UI+'confirmation_003.ogg'),clip('Maximize 07',UI+'maximize_007.ogg')],0,'현재 일반 완료·증강 선택 진입용이며 최종 승리와 구분됩니다.')
group('victory','최종 승리','라운드·결과',[clip('현재 · Steel 00',RUN+'Victory/02_Steel00.ogg'),clip('Steel 02',JINGLE+'Steel jingles/jingles_STEEL02.ogg'),clip('Pizzicato 02',JINGLE+'Pizzicato jingles/jingles_PIZZI02.ogg')],0,'차분한 악기 멜로디 방향을 유지합니다. 최초 거부된 UI 승리음은 제외했습니다.')
group('defeat','패배 알림','라운드·결과',[clip('Steel 01',JINGLE+'Steel jingles/jingles_STEEL01.ogg'),clip('Steel 04',JINGLE+'Steel jingles/jingles_STEEL04.ogg'),clip('Pizzicato 01',JINGLE+'Pizzicato jingles/jingles_PIZZI01.ogg')],note='아직 미선택·미적용입니다. 앞서 제시한 패배 후보 3개입니다.')
group('ui_click','UI 클릭·선택','UI',[clip('Click 01',UI+'click_001.ogg'),clip('Click 02',UI+'click_002.ogg'),clip('Select 02',UI+'select_002.ogg')],note='추가 검토 항목입니다. 모든 클릭에 적용할지는 별도 판단합니다.')
group('ui_open','패널 열기','UI',[clip('Open 02',UI+'open_002.ogg'),clip('Open 03',UI+'open_003.ogg'),clip('Switch 03',UI+'switch_003.ogg')],note='추가 검토 항목입니다. 자동 갱신마다 재생하는 정책이 아닙니다.')
group('ui_close','패널 닫기·접기','UI',[clip('Close 01',UI+'close_001.ogg'),clip('Close 02',UI+'close_002.ogg'),clip('Back 02',UI+'back_002.ogg')],note='추가 검토 항목입니다. 현재 손잡이 외형·조작 규칙은 그대로입니다.')

# Reproduce the current deterministic card formula outside Unity for reference only.
reveal = []
for grade, label in enumerate(['실버','골드','플래티넘']):
    rate = 44100; duration = .35 + grade*.2; frequency = 440*(1+grade*.5)
    samples = array.array('h')
    for i in range(int(rate*duration)):
        t = i/rate
        value = .16*math.exp(-t*(8-grade*2))*(math.sin(t*frequency*2*math.pi)+.4*math.sin(t*frequency*3*math.pi))
        samples.append(round(value*32767))
    output = DERIVED / f'augmentation_{grade}.wav'
    with wave.open(str(output),'wb') as w:
        w.setnchannels(1);w.setsampwidth(2);w.setframerate(rate);w.writeframes(samples.tobytes())
    reveal.append(clip(label+' · 현재 공식 재현',output.relative_to(ROOT).as_posix(),'코드 공식으로 만든 참고 청취본 · 게임 녹음 아님','프로젝트 기존 합성 공식'))
group('augmentation','증강 카드 공개 · 현재 3등급','증강',reveal,note='기존 코드의 등급별 3음을 참고용으로 재현했습니다. 세 등급 중 하나를 고르는 비교 항목이 아닙니다.',kind='reference')
group('bgm','BGM · 기존 제작 시안 3개','BGM',[
    clip('Fortress March','AudioProduction/CoreGuardian_BGM_20261013/01_Fortress_March.wav','2026-10-13 기존 초안 · 미적용','프로젝트 기존 직접 제작'),
    clip('Crystal Engine','AudioProduction/CoreGuardian_BGM_20261013/02_Crystal_Engine.wav','2026-10-13 기존 초안 · 미적용','프로젝트 기존 직접 제작'),
    clip('Watchline Embers','AudioProduction/CoreGuardian_BGM_20261013/03_Watchline_Embers.wav','2026-10-13 기존 초안 · 미적용','프로젝트 기존 직접 제작')],note='기존 BGM 파일을 읽기 참조만 합니다. 편곡·원본·게임 적용을 변경하지 않았습니다.')

def metadata(path):
    raw=path.read_bytes()
    if path.suffix.lower()=='.wav':
        with wave.open(str(path),'rb') as w: duration=w.getnframes()/w.getframerate()
    else:
        start=raw.index(b'\x01vorbis');rate=struct.unpack_from('<I',raw,start+12)[0];page=0;last=0
        while page<len(raw):
            assert raw[page:page+4]==b'OggS'
            granule=struct.unpack_from('<Q',raw,page+6)[0]
            if granule!=0xffffffffffffffff:last=granule
            n=raw[page+26];page+=27+n+sum(raw[page+27:page+27+n])
        duration=last/rate
    return round(duration,3), hashlib.sha256(raw).hexdigest()

for g in GROUPS:
    for c in g['clips']:
        path=(ROOT/c['path']).resolve()
        assert path.is_relative_to(ROOT) and path.is_file(),str(path)
        c['duration'],c['sha256']=metadata(path)
        c['url']='../../'+c['path']

catalog=dict(title='코어 가디언 · 사운드 데스크',version='2026-10-19',groups=GROUPS,
    support='지원 포탑 전투 효과음은 사용자 지시로 무음입니다. 이 페이지에서 새 후보를 만들거나 적용하지 않습니다.',
    notes='현재 적용음 + 비교 후보 2개. 선택은 이 브라우저의 비교 메모이며 게임에 자동 적용되지 않습니다.')
(HERE/'catalog.json').write_text(json.dumps(catalog,ensure_ascii=False,indent=2),encoding='utf-8')
(HERE/'catalog.js').write_text('window.AUDIO_CATALOG = '+json.dumps(catalog,ensure_ascii=False)+';\n',encoding='utf-8')
print(json.dumps(dict(groups=len(GROUPS),clips=sum(len(g['clips']) for g in GROUPS),current=sum(g['current'] is not None for g in GROUPS),unique_files=len({c['path'] for g in GROUPS for c in g['clips']})),ensure_ascii=False))
