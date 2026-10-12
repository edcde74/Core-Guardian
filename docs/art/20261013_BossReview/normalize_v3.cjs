const sharp=require('C:/Users/edcde/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const fs=require('fs');const path=require('path');
const out=__dirname;const root=path.resolve(out,'../../..');
const refs=['enemy_attack_v2.png','enemy_defense_v2.png','enemy_buff_v2.png'].map(n=>path.join(root,'Assets/Art/ScreenTests/20260930/Revision2/Sprites',n));
async function main(){
 if(process.argv[2]==='reference'){
  const layers=[];for(let i=0;i<3;i++){const m=await sharp(refs[i]).metadata();const b=await sharp(refs[i]).resize(m.width*8,m.height*8,{kernel:'nearest'}).png().toBuffer();layers.push({input:b,left:i*400+Math.floor((400-m.width*8)/2),top:680-m.height*8});}
  await sharp({create:{width:1200,height:720,channels:4,background:{r:36,g:40,b:46,alpha:1}}}).composite(layers).png().toFile(path.join(out,'current_monsters_reference.png'));return;
 }
 const pixels=[];for(const f of refs){const {data}=await sharp(f).ensureAlpha().raw().toBuffer({resolveWithObject:true});for(let i=0;i<data.length;i+=4)if(data[i+3]>=200)pixels.push([data[i],data[i+1],data[i+2]]);}
 const dist=(a,b)=>a.reduce((s,v,i)=>s+(v-b[i])**2,0);
 const centers=[pixels[0].slice()];for(let k=1;k<24;k++){let best=pixels[0],bd=-1;for(const p of pixels){const d=Math.min(...centers.map(c=>dist(p,c)));if(d>bd){bd=d;best=p;}}centers.push(best.slice());}
 for(let t=0;t<12;t++){const sum=centers.map(()=>[0,0,0,0]);for(const p of pixels){let j=0;for(let k=1;k<24;k++)if(dist(p,centers[k])<dist(p,centers[j]))j=k;for(let c=0;c<3;c++)sum[j][c]+=p[c];sum[j][3]++;}for(let k=0;k<24;k++)if(sum[k][3])centers[k]=sum[k].slice(0,3).map(v=>v/sum[k][3]);}
 const palette=centers.map(c=>pixels.reduce((best,p)=>dist(c,p)<dist(c,best)?p:best,pixels[0])).map(p=>p.slice());
 const status=[[232,91,34],[255,160,65]];fs.writeFileSync(path.join(out,'palette_v3.json'),JSON.stringify({source:refs.map(f=>path.relative(root,f)),base:palette,status},null,2));
 const src=path.join(out,'boss_hammer_v3_generated.png');const meta=await sharp(src).metadata();const cells=[];
 for(let i=0;i<2;i++){
  const left=i?Math.floor(meta.width/2):0;const w=i?meta.width-left:left+Math.floor(meta.width/2);
  let {data,info}=await sharp(src).extract({left,top:0,width:w,height:meta.height}).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  let x0=info.width,y0=info.height,x1=-1,y1=-1;
  for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++){const j=(y*info.width+x)*4;if(data[j+3]>=180){x0=Math.min(x0,x);y0=Math.min(y0,y);x1=Math.max(x1,x);y1=Math.max(y1,y);}}
  if(x1<0)throw Error('Empty state');cells.push({left:left+x0,top:y0,width:x1-x0+1,height:y1-y0+1});
 }
 const scale=Math.min(62/Math.max(...cells.map(c=>c.width)),67/Math.max(...cells.map(c=>c.height)));
 const results=[];
 for(let i=0;i<2;i++){
  const c=cells[i],w=Math.round(c.width*scale),h=Math.round(c.height*scale);
  let {data,info}=await sharp(src).extract(c).resize(w,h,{kernel:'nearest'}).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  const pal=i?palette.concat(status):palette;
  for(let j=0;j<data.length;j+=4){if(data[j+3]<180){data[j]=data[j+1]=data[j+2]=data[j+3]=0;continue;}const p=[data[j],data[j+1],data[j+2]];let best=pal[0];for(const q of pal)if(dist(p,q)<dist(p,best))best=q;data[j]=best[0];data[j+1]=best[1];data[j+2]=best[2];data[j+3]=255;}
  const b=await sharp(data,{raw:info}).png().toBuffer();const dest=path.join(out,`boss_${i?'enraged':'normal'}_v3_64x80.png`);
  await sharp({create:{width:64,height:80,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).composite([{input:b,left:Math.floor((64-w)/2),top:74-h}]).png().toFile(dest);results.push(dest);
 }
 const files=refs.concat(results);const layers=[];
 for(let i=0;i<files.length;i++){const m=await sharp(files[i]).metadata();const b=await sharp(files[i]).resize(m.width*6,m.height*6,{kernel:'nearest'}).png().toBuffer();layers.push({input:b,left:i*400+Math.floor((400-m.width*6)/2),top:590-(i<3?m.height:74)*6});}
 const label=['현재 공격형 42×64','현재 수비형 33×48','현재 버프형 35×80','보스 일반 64×80','보스 분노 64×80'];
 const svg=Buffer.from(`<svg width="2000" height="680"><style>text{font-family:Malgun Gothic;font-size:25px;fill:#eee}</style><text x="25" y="42">현재 적용 몬스터 기준 · 모든 스프라이트 동일한 Nearest 6배 확대</text>${label.map((s,i)=>`<text x="${i*400+32}" y="645">${s}</text>`).join('')}</svg>`);
 await sharp({create:{width:2000,height:680,channels:4,background:{r:36,g:40,b:46,alpha:1}}}).composite(layers.concat([{input:svg,left:0,top:0}])).png().toFile(path.join(out,'current_monsters_vs_boss_v3.png'));
 console.log(JSON.stringify({paletteColors:palette.length,cells,normalized:results},null,2));
}
main().catch(e=>{console.error(e);process.exit(1)});
