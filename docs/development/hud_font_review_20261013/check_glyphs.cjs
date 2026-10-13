// Read Unicode cmap records without installing or modifying system fonts.
const fs=require('fs'),path=require('path');
const cards=JSON.parse(fs.readFileSync(path.join(__dirname,'card_samples.json'),'utf8'));
const titleText=cards.map(c=>c.title).join('')+'플레이어 능력치 강화';
const bodyText=cards.map(c=>c.effect).join('')+'코어 20/20 골드 1,250 수정 180 단일 고속 100수정 범위 공격 공격력 10 → 12 +2 · 100골드 공격속도 1 → 1.15 +0.15 · 100골드 다시 클릭하여 선택';
function glyphs(file){
  const b=fs.readFileSync(path.join(__dirname,'fonts',file));let cmap;
  for(let i=0;i<b.readUInt16BE(4);i++){let t=12+i*16;if(b.toString('ascii',t,t+4)==='cmap')cmap=b.readUInt32BE(t+8)}
  const supported=new Set();
  for(let i=0;i<b.readUInt16BE(cmap+2);i++){
    let r=cmap+4+i*8,platform=b.readUInt16BE(r),encoding=b.readUInt16BE(r+2);
    if(platform!==0&&!(platform===3&&(encoding===1||encoding===10)))continue;
    let o=cmap+b.readUInt32BE(r+4),format=b.readUInt16BE(o);
    if(format===12){for(let j=0;j<b.readUInt32BE(o+12);j++){let g=o+16+j*12,start=b.readUInt32BE(g),end=b.readUInt32BE(g+4),gid=b.readUInt32BE(g+8);for(let c=start;c<=end;c++)if(gid+c-start)supported.add(c)}}
    if(format===4){let n=b.readUInt16BE(o+6)/2,end=o+14,start=end+2*n+2,delta=start+2*n,range=delta+2*n;for(let j=0;j<n;j++){let s=b.readUInt16BE(start+j*2),e=b.readUInt16BE(end+j*2),d=b.readInt16BE(delta+j*2),v=b.readUInt16BE(range+j*2);for(let c=s;c<=e&&c<65535;c++){let gid=v?b.readUInt16BE(range+j*2+v+2*(c-s)):(c+d)&65535;if(v&&gid)gid=(gid+d)&65535;if(gid)supported.add(c)}}}
  }
  return supported;
}
// Neo's missing arrow uses the explicitly declared Galmuri fallback, checked above.
const configs=[['Galmuri11.ttf',bodyText+titleText],['Galmuri11-Bold.ttf',titleText],['NeoDunggeunmo.ttf',(bodyText+titleText).replaceAll('→','')],['BlackHanSans-Regular.ttf',titleText]];
const result=configs.map(([file,text])=>{const set=glyphs(file);return{file,missing:[...new Set([...text].filter(c=>!/[\s]/.test(c)&&!set.has(c.codePointAt(0))))]}});
fs.writeFileSync(path.join(__dirname,'glyph_check.json'),JSON.stringify(result,null,2)+'\n');console.log(JSON.stringify(result));if(result.some(r=>r.missing.length))process.exitCode=1;
