// Native pixel VFX study. Renders original sprites unchanged; no Unity asset writes.
const fs=require('fs'),path=require('path'),assert=require('assert');
const sharp=require('C:/Users/edcde/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
const root=path.resolve(__dirname,'../../..'),W=288,H=112,STEP=40;
const types=['attack','defense','buff'];
const specs=[
 {id:'01_engineer',name:'기술병 · 금속 타격',kind:0,main:[242,179,75],light:[255,242,188],duration:160,flash:.18,crop:[102,933,230,137],size:[9,5],particles:[[-1,-1,5],[-1,1,5],[1,-1,4],[1,1,3]]},
 {id:'02_single',name:'단일 포탑 · 방향성 수정 타격',kind:1,main:[56,177,183],light:[192,244,230],duration:200,flash:.22,crop:[478,936,326,125],size:[12,5],particles:[[1,-.3,8],[1,.35,7],[-.7,-1,4],[-.7,1,4]]},
 {id:'03_multi',name:'범위 포탑 · 소형 수정 파편',kind:2,main:[70,156,177],light:[169,223,225],duration:120,flash:.14,crop:[917,927,260,143],size:[10,6],particles:[[-1,-1,4],[1,-1,3],[1,1,4]]}
];
function px(b,x,y,color,a=1){x=Math.round(x);y=Math.round(y);if(x<0||y<0||x>=W||y>=H)return;let n=(y*W+x)*4,da=b[n+3]/255,oa=a+da*(1-a);if(oa<=0)return;for(let i=0;i<3;i++)b[n+i]=Math.round((b[n+i]*da*(1-a)+color[i]*a)/oa);b[n+3]=Math.round(oa*255)}
function rect(b,x,y,w,h,c,a=1){for(let yy=0;yy<h;yy++)for(let xx=0;xx<w;xx++)px(b,x+xx,y+yy,c,a)}
function line(b,x,y,dx,dy,n,c,a=1){for(let k=0;k<n;k++)px(b,x+dx*k,y+dy*k,c,a)}
function stamp(b,im,x,y,bright=0){for(let yy=0;yy<im.height;yy++)for(let xx=0;xx<im.width;xx++){const p=(yy*im.width+xx)*4,a=im.data[p+3]/255;if(a){const color=[0,1,2].map(i=>Math.round(im.data[p+i]*(1-bright)+[255,241,212][i]*bright));px(b,x+xx,y+yy,color,a)}}}
const digits={'0':['111','101','101','101','111'],'1':['010','110','010','010','111'],'2':['110','001','010','100','111'],'3':['110','001','010','001','110'],'4':['101','101','111','001','001'],'6':['011','100','111','101','111'],'8':['111','101','111','101','111']};
function number(b,text,x,y,a=1){for(let pass=0;pass<2;pass++)for(let n=0;n<text.length;n++)for(let yy=0;yy<5;yy++)for(let xx=0;xx<3;xx++)if(digits[text[n]][yy][xx]==='1'){if(!pass)rect(b,x+n*4+xx-1,y+yy-1,3,3,[19,23,27],a);else px(b,x+n*4+xx,y+yy,[255,240,173],a)}}
function impact(b,s,x,y,age,seed=0){if(age<0||age>=s.duration)return;const p=age/s.duration,a=p<.5?1:(1-p)*2,expand=Math.floor(p*5);
 if(p<.35){const r=s.kind===1?3:2;line(b,x-r,y,1,0,r*2+1,s.light);line(b,x,y-r,0,1,r*2+1,s.light);px(b,x-1,y-1,s.main);px(b,x+1,y+1,s.main)}
 for(let i=0;i<s.particles.length;i++){const [dx,dy,len]=s.particles[i],dist=2+p*len,xx=x+dx*dist,yy=y+dy*dist;
  if(s.kind===0){line(b,xx,yy,dx<0?-1:1,dy<0?-1:1,p<.6?2:1,s.main,a);px(b,xx,yy,s.light,a*.8)}
  else if(s.kind===1){line(b,xx,yy,1,0,p<.5?3:2,s.main,a);px(b,xx,yy,s.light,a)}
  else {px(b,xx,yy,s.main,a);if(i===0&&p<.65)px(b,xx+1,yy,s.light,a)}
 }
}
function upscale(b,scale){const ow=W*scale,oh=H*scale,out=Buffer.alloc(ow*oh*4);for(let y=0;y<H;y++){const row=Buffer.alloc(ow*4);for(let x=0;x<W;x++)for(let k=0;k<scale;k++)b.copy(row,(x*scale+k)*4,(y*W+x)*4,(y*W+x)*4+4);for(let k=0;k<scale;k++)row.copy(out,(y*scale+k)*ow*4)}return out}
function visualHits(times,gap=100){const result=[];for(const t of times){if(!result.length||t-result[result.length-1]>=gap)result.push(t)}return result}
function numberGroups(times){const bins=new Map();for(const time of times){const key=Math.floor((time-times[0])/200);if(!bins.has(key))bins.set(key,{start:time,times:[]});bins.get(key).times.push(time)}return [...bins.values()]}
let enemies=[],projectiles=[];
function render(s,time,stress=false){const b=Buffer.alloc(W*H*4);for(let p=0;p<b.length;p+=4){b[p]=35;b[p+1]=41;b[p+2]=47;b[p+3]=255}
 for(let col=0;col<3;col++){
  const left=col*96,enemy=enemies[col],x=left+64,y=71;
  // Repeated input is a synthetic visual stress probe, not a balance measurement.
  const hits=stress?Array.from({length:20},(_,i)=>500+i*45):[600+(s.kind===2?0:col*70)];
  const visible=visualHits(hits),age=time-[...visible].reverse().find(t=>t<=time),flash=Number.isFinite(age)&&age<65?s.flash*(1-age/65):0;
  // Original sprite anchored to fixed feet. No knockback, hitstop, slow or scale change.
  for(let yy=-2;yy<=2;yy++)for(let xx=-12;xx<=12;xx++)if(xx*xx/144+yy*yy/4<=1)px(b,x+xx,100+yy,[19,24,28]);
  stamp(b,enemy,left+Math.floor(64-enemy.width/2),98-enemy.height,flash);
  rect(b,left+48,12,32,1,[69,79,83]);rect(b,left+48,12,26,1,[87,138,111]);
  for(const hit of hits){const travel=(time-(hit-320))/320;if(travel>=0&&travel<1){const im=projectiles[s.kind],tx=left+8+travel*52;stamp(b,im,Math.round(tx-im.width/2),Math.round(y-im.height/2));line(b,Math.round(tx-im.width/2)-3,y,1,0,2,s.main,.35)}}
  for(const hit of visible)impact(b,s,x-5,y,time-hit);
  // Dense hits grouped visually; the examples are illustrative numbers, not sampled damage.
  const shown=stress?numberGroups(hits):hits.map(start=>({start,times:[start]}));
  for(let i=0;i<shown.length;i++){const group=shown[i],nAge=time-group.start,value=group.times.filter(t=>t<=time).length*12;if(nAge>=0&&nAge<480&&value){const yy=42-Math.floor(nAge/70)-(nAge<80?1:0);number(b,String(value),left+67+(i%2)*10,yy,nAge<300?1:(480-nAge)/180)}}
  rect(b,left+10,106,77,1,[60,70,75]);
 }return b}
async function loadSprite(file){const {data,info}=await sharp(file).ensureAlpha().raw().toBuffer({resolveWithObject:true});return {data,width:info.width,height:info.height}}
async function encode(s,stress=false){const count=stress?55:46,frames=[];for(let i=0;i<count;i++)frames.push(upscale(render(s,i*STEP,stress),3));const file=s.id+(stress?'_dense':'')+'.gif';await sharp(Buffer.concat(frames),{raw:{width:W*3,height:H*3*count,channels:4,pageHeight:H*3}}).gif({delay:Array(count).fill(STEP),loop:0,colours:192,dither:0,effort:4}).toFile(path.join(__dirname,file));const meta=await sharp(path.join(__dirname,file),{animated:true}).metadata();assert(meta.pages>1);return {file,frames:meta.pages,width:meta.width,height:meta.pageHeight,inputHitsPerTarget:stress?20:1,visibleImpactsPerTarget:stress?visualHits(Array.from({length:20},(_,i)=>500+i*45)).length:1}}
async function main(){
 enemies=await Promise.all(types.map(type=>loadSprite(path.join(root,'Assets/Art/ScreenTests/20260930/Revision2/Sprites/enemy_'+type+'_v2.png'))));
 const atlas=path.join(root,'Assets/Resources/CoreGuardian/Art/CombatB.png'),meta=await sharp(atlas).metadata();
 for(const s of specs){const [x,y,width,height]=s.crop;const {data,info}=await sharp(atlas).extract({left:x,top:meta.height-y-height,width,height}).resize(s.size[0],s.size[1],{kernel:'nearest',fit:'fill'}).ensureAlpha().raw().toBuffer({resolveWithObject:true});projectiles.push({data,width:info.width,height:info.height})}
 const result=[];for(const s of specs){result.push(await encode(s));result.push(await encode(s,true));await sharp(upscale(render(s,640),3),{raw:{width:W*3,height:H*3,channels:4}}).png().toFile(path.join(__dirname,s.id+'_poster.png'));
  const tiles=[];for(let i=0;i<6;i++){const b=Buffer.alloc(W*H*4);impact(b,s,16,16,i*s.duration/6);tiles.push(await sharp(b,{raw:{width:W,height:H,channels:4}}).extract({left:0,top:0,width:32,height:32}).png().toBuffer())}
  await sharp({create:{width:192,height:32,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).composite(tiles.map((input,i)=>({input,left:i*32,top:0}))).png().toFile(path.join(__dirname,s.id+'_sheet.png'));
 }
 // Source and impact stages on three original enemy types; every panel uses the same 3x grid.
 const times=[240,600,640,720,1200],contact=Buffer.alloc(W*3*times.length*H*3*3*4);const cw=W*3*times.length,ch=H*3*3;
 for(let row=0;row<3;row++)for(let col=0;col<times.length;col++){const frame=upscale(render(specs[row],times[col]),3);for(let yy=0;yy<H*3;yy++)frame.copy(contact,((row*H*3+yy)*cw+col*W*3)*4,yy*W*3*4,(yy+1)*W*3*4)}
 await sharp(contact,{raw:{width:cw,height:ch,channels:4}}).png().toFile(path.join(__dirname,'all_phases.png'));
 const checks={scope:'Offline pixel VFX studies, no Unity execution or real damage samples',audio:false,sourceSpriteSizes:enemies.map(im=>[im.width,im.height]),integerPreviewScale:3,perTargetVisualCooldownMs:100,sourceFilesChanged:false,animations:result};
 // Verify complete fade recovery to exact untouched source pixels and distinct source responses.
 for(const s of specs)assert(render(s,0).equals(render(s,1500)),'Effect failed to clear: '+s.id);
 assert(!render(specs[0],640).equals(render(specs[1],640)),'Player and single effects identical');assert(!render(specs[1],640).equals(render(specs[2],640)),'Tower effects identical');
 checks.fullyRecoveredPixelsIdentical=true;checks.attackResponsesDistinct=true;
 const groups=numberGroups(Array.from({length:20},(_,i)=>500+i*45));assert.equal(groups.reduce((sum,g)=>sum+g.times.length*12,0),240);checks.denseNumberGroups=groups.map(g=>g.times.length*12);checks.groupedNumberSumPreserved=true;
 fs.writeFileSync(path.join(__dirname,'checks.json'),JSON.stringify(checks,null,2));fs.writeFileSync(path.join(__dirname,'specs.json'),JSON.stringify(specs,null,2));
 const panels=specs.map(s=>`<section><h2>${s.name}</h2><img src="${s.id}.gif" alt="${s.name} 단발 명중 시안"><details><summary>집중 공격 시각 검사</summary><img src="${s.id}_dense.gif" alt="집중 공격 표현"></details></section>`).join('');
 fs.writeFileSync(path.join(__dirname,'preview.html'),`<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>코어 가디언 타격 시안 3종</title><style>body{margin:0;padding:24px;background:#171c21;color:#e7e5de;font:15px system-ui}main{max-width:900px;margin:auto}h1{font-size:24px}h2{font-size:18px}p{color:#b8c1c6;line-height:1.7}section{padding:18px;background:#23292f;border:1px solid #414b51;border-radius:10px;margin:20px 0}img{display:block;width:100%;height:auto;image-rendering:pixelated}summary{padding:12px 0;color:#a4d2ca;cursor:pointer}</style><main><h1>타격 VFX · 공격별 시안 3종</h1><p>좌→우: 공격형 / 수비형 / 버프형 · 모두 같은 도트 3배<br>소리 없음 · 게임 미적용 · 시안용 숫자와 타이밍</p>${panels}<p>집중 공격은 대상당 45ms 간격 20회 입력을 사용한 시각 검사입니다. 명중 효과는 100ms 간격으로 제한하고 숫자는 일부를 묶어 표시한 제안입니다. 실제 게임 피해 판정·공격속도·밸런스 결과가 아닙니다.</p></main></html>`);
 console.log(JSON.stringify(checks,null,2));
}
main().catch(e=>{console.error(e);process.exitCode=1});
