const fs = require('fs');
const path = require('path');
const root = __dirname;
const hud = path.resolve(root, '../../../Assets/Resources/CoreGuardian/Hud');
const art = n => 'data:image/png;base64,' + fs.readFileSync(path.join(hud, n + '.png')).toString('base64');
const icon = (n, cls='') => `<img class="icon ${cls}" src="${art(n)}" alt="">`;
const options = [
  ['a', 'A · 갈무리', '추천', '제목 Galmuri11 Bold · 본문 Galmuri11', '픽셀 아트와 통일감 · 정보가 많은 HUD용'],
  ['b', 'B · Neo둥근모', '레트로', '제목·본문 NeoDunggeunmo · 기호 갈무리', '둥근 픽셀 획 · 고전 RPG 분위기'],
  ['c', 'C · 검은고딕 조합', '강한 제목', '제목 Black Han Sans · 본문 Galmuri11', '굵은 증강 제목 · 금속 프레임의 무게감'],
];
const button = (name, text, cls='') => `<button class="frame button ${cls}">${icon(name)}<span class="fit">${text}</span></button>`;
const sheet = ([id,title,tag,fonts,desc]) => `<article class="option ${id}">
  <header class="option-head"><div class="option-title">${title}<em>${tag}</em></div><p>${fonts}</p><small>${desc}</small></header>
  <div class="game-ui">
    <div class="frame status">${icon('Core')}<span class="fit">코어 20/20</span>${icon('Gold')}<span class="fit">골드 1,250</span>${icon('Crystal')}<span class="fit">수정 180</span></div>
    <div class="sample-label">설치 버튼 · 실제 204 × 96</div>
    <div class="install-row">${button('Install','단일 고속<br>100수정','install')}${button('Install','범위 공격<br>100수정','install')}</div>
    <div class="sample-label">강화 버튼 · 실제 488 × 80</div>
    <section class="frame growth"><div class="growth-title heading">${icon('Upgrade')}<span class="fit">플레이어 능력치 강화</span></div>
      ${button('Damage','공격력 10 → 12<br>+2 · 100골드','upgrade')}
      ${button('Speed','공격속도 1 → 1.15<br>+0.15 · 100골드','upgrade')}
    </section>
    <div class="sample-label">증강 앞면 · 실제 350 × 460</div>
    <section class="frame card"><div class="card-content">
      ${icon('Augment','card-symbol')}<div class="card-title heading fit">포탑 전투</div>
      <div class="card-effect fit">현재·이후 설치하는 모든 공격 포탑<br>공격력 버프 +60%<br>지원 포탑 버프와 합산</div>
      <button class="card-choose fit">다시 클릭하여 선택</button>
    </div></section>
  </div>
</article>`;
const html = `<!doctype html><html lang="ko"><meta charset="utf-8"><title>코어 가디언 · HUD 폰트 시안</title><style>
@font-face{font-family:Galmuri;src:url('fonts/Galmuri11.ttf')}@font-face{font-family:GalmuriBold;src:url('fonts/Galmuri11-Bold.ttf')}@font-face{font-family:Neo;src:url('fonts/NeoDunggeunmo.ttf')}@font-face{font-family:BlackHan;src:url('fonts/BlackHanSans-Regular.ttf')}
*{box-sizing:border-box}body{margin:0;background:#10161c;color:#f7eddb;font-family:'Malgun Gothic',sans-serif}.board{width:1664px;padding:32px 28px 24px}.board-head{height:100px;border-bottom:1px solid #39434c;margin-bottom:24px;display:flex;justify-content:space-between;align-items:flex-start}.eyebrow{font-size:12px;letter-spacing:3px;color:#89c7ce;margin-bottom:10px}h1{font-size:28px;margin:0;font-weight:700}.context{font-size:13px;line-height:1.8;color:#a9b6c0;text-align:right}.columns{display:grid;grid-template-columns:repeat(3,520px);gap:24px}.option{width:520px}.option-head{height:104px;padding:4px 4px 0}.option-title{font-size:23px;font-weight:bold;display:flex;gap:14px;align-items:center}em{font-style:normal;font-size:12px;color:#8de0e5;border:1px solid #427c82;padding:4px 8px;border-radius:3px}.option-head p{font-size:13px;color:#bcc6cf;margin:10px 0 6px}.option-head small{font-size:13px;color:#80919f}.game-ui{--body:Galmuri;--title:GalmuriBold;font-family:var(--body);font-weight:400}.b .game-ui{--body:Neo;--title:Neo}.c .game-ui{--title:BlackHan}.heading{font-family:var(--title);font-weight:400}.frame{position:relative;border:0;background:none;color:#fff2dd;isolation:isolate}.frame::before{content:'';position:absolute;inset:0;border-style:solid;border-width:11.2px 12.8px;border-image:url('${art('Panel')}') 28 32 fill stretch;z-index:-1;pointer-events:none}.button::before{border-image-source:url('${art('Button')}')}.button:hover::before{border-image-source:url('${art('Hover')}')}.button:active{filter:brightness(.8)}.button:active span{transform:translateY(3px)}.icon{width:30px;height:30px;object-fit:contain;image-rendering:pixelated;flex:none}.fit{display:block}.status{height:72px;padding:16px;display:flex;align-items:center;gap:8px;font-size:20px}.status .fit{margin-right:8px;white-space:nowrap}.sample-label{font-family:'Malgun Gothic',sans-serif;font-size:11px;color:#7b909e;letter-spacing:.7px;margin:16px 4px 8px}.install-row{display:flex;gap:8px;padding-left:16px}.button{font-family:var(--body);font-weight:400;padding:16px 16px 16px 54px;cursor:pointer;text-align:left}.button .icon{position:absolute;left:16px;top:calc(50% - 15px)}.install{width:204px;height:96px;font-size:16px;line-height:22px}.growth{width:520px;height:244px;padding:16px}.growth-title{display:flex;gap:8px;align-items:center;font-size:24px;height:32px;margin-bottom:12px}.upgrade{display:block;width:488px;height:80px;margin-bottom:4px;font-size:20px;line-height:24px}.card{width:350px;height:460px;margin:0 auto}.card::before{border-image-source:url('${art('Disabled')}');filter:sepia(.28) saturate(1.6)}.card-content{position:absolute;top:24px;left:24px;width:302px;height:412px}.card-symbol{position:absolute;top:28px;left:111px;width:80px;height:80px}.card-title{position:absolute;left:14px;top:128px;width:274px;height:48px;font-size:26px;line-height:32px;display:flex;align-items:center;justify-content:center;text-align:center}.card-effect{position:absolute;left:14px;top:188px;width:274px;height:144px;font-size:20px;line-height:26px;display:flex;align-items:center;justify-content:center;text-align:center;white-space:pre-line}.card-choose{position:absolute;left:14px;top:354px;width:274px;height:34px;font-family:var(--body);font-size:18px;line-height:26px;text-align:center;color:#fff2dd;background:none;border:0;cursor:pointer}.card-choose:hover{color:#8de0e5}.foot{margin-top:22px;padding-top:16px;border-top:1px solid #39434c;font-size:12px;color:#93a5b1;display:flex;justify-content:space-between}.toolbar{display:flex;gap:8px}.toolbar button{background:#25303b;color:#d9e6ec;border:1px solid #475966;padding:8px 16px;cursor:pointer}.toolbar button[aria-pressed=true]{border-color:#86d9df;color:#86d9df}.dense{font-size:12px}
</style><body><main class="board"><div class="board-head"><div><div class="eyebrow">CORE GUARDIAN / TYPE EXPLORATION 01</div><h1>금속 프레임 위의 한글, 세 가지 방향</h1></div><div class="context">공식 TTF 실물로 렌더링 · 게임 적용 전 시안<br>같은 스프라이트 / 같은 문구 / 같은 상자 크기<br>비교용 글자 크기 16 · 18 · 20 · 24 · 26px</div></div><div class="columns">${options.map(sheet).join('')}</div><div class="foot"><span>추천 A — 작은 정보와 픽셀 아트의 통일감. C — 증강 이름을 더 강하게 강조할 때.</span><span>브라우저 시안 · Unity 렌더링과 픽셀 정렬은 선택 후 검증</span></div></main><script>document.fonts.ready.then(()=>document.body.dataset.fontsReady='true');</script></body></html>`;
fs.writeFileSync(path.join(root,'preview.html'),html.replace('</style>', '.b .game-ui{--body:Neo,Galmuri}.card-effect{word-break:keep-all}</style>'));
const cards=[];
for(let g=0;g<6;g++){
  const rows=JSON.parse(fs.readFileSync(path.join(root,'../hud_text_fit_20261013/final_cards_'+g+'.json'),'utf8')).rows;
  for(let i=0;i<3;i++){
    const get=part=>rows.find(r=>r.path.endsWith('/AugmentationCard'+i+'/CardBody/Front/'+part)).text;
    cards.push({title:get('Title'),effect:get('Effect')});
  }
}
fs.writeFileSync(path.join(root,'card_samples.json'),JSON.stringify(cards,null,2)+'\n');
fs.writeFileSync(path.join(root,'sources.json'),JSON.stringify({downloaded:'2026-10-13',unchangedGame:true,fonts:[{name:'Galmuri11 / Bold',source:'https://github.com/quiple/galmuri',license:'OFL-1.1'},{name:'NeoDunggeunmo',source:'https://github.com/neodgm/neodgm/releases/tag/v1.601',license:'OFL-1.1'},{name:'Black Han Sans',source:'https://github.com/google/fonts/tree/main/ofl/blackhansans',license:'OFL-1.1'}]},null,2)+'\n');
console.log('Generated preview.html');
