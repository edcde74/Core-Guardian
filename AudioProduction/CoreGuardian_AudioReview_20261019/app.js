/* Local audition notes only. No game integration or write endpoint. */
'use strict';
const catalog = window.AUDIO_CATALOG;
const $ = (q) => document.querySelector(q);
const storageKey = 'core-guardian-audio-review-20261019-v1';
let saved = {};
try { saved = JSON.parse(localStorage.getItem(storageKey) || '{}') || {}; } catch (_) {}
const choices = saved.choices || {}, notes = saved.notes || {};
let volume = typeof saved.volume === 'number' ? Math.max(0,Math.min(100,saved.volume)) : 15;
let filter = '전체', token = 0, active = null, queueAudio = null, releaseQueue = null;
const players = new Map();
const labels = ['①','②','③'];
function persist() {
  try { localStorage.setItem(storageKey,JSON.stringify({choices,notes,volume})); }
  catch (_) { $('#save-status').textContent = '브라우저 저장을 사용할 수 없습니다. 결과를 복사하거나 JSON으로 저장해 주세요.'; }
}
function el(tag, cls, text) { const n=document.createElement(tag); if(cls)n.className=cls; if(text!==undefined)n.textContent=text; return n; }
function stop(message=true) {
  token++;
  if(releaseQueue){releaseQueue();releaseQueue=null;}
  for(const a of players.values()) { a.pause(); a.currentTime=0; }
  if(queueAudio){queueAudio.onended=null;queueAudio.onerror=null;queueAudio=null;}
  active=null;
  if(message) $('#now').textContent='재생을 멈췄습니다.';
}
function volumeUpdate() {
  $('#volume').value=volume; $('#volume-label').textContent=volume+'%';
  for(const a of players.values())a.volume=volume/100;
}
async function playQueue(items) {
  stop(false); const run=token;
  for(let n=0;n<items.length;n++) {
    if(run!==token)return;
    const {g,i}=items[n], a=players.get(g.id+':'+i); queueAudio=a; active=a;
    a.currentTime=0;
    $('#now').textContent=`${n+1}/${items.length} · ${g.title} ${labels[i]} ${g.clips[i].title}`;
    try {
      const ended=new Promise((resolve,reject)=>{releaseQueue=resolve;a.onended=resolve;a.onerror=()=>reject(new Error('오디오를 불러오지 못했습니다.'));});
      await a.play(); await ended;
    } catch(e) {
      if(run===token){$('#now').textContent=`재생 실패: ${g.title} — 파일로 직접 열었으면 로컬 서버로 다시 열어 주세요.`;stop(false);} return;
    }
    if(run!==token)return;
    a.onended=null;a.onerror=null;releaseQueue=null;
    await new Promise(resolve=>setTimeout(resolve,300));
  }
  if(run===token){queueAudio=null;active=null;$('#now').textContent='이어 듣기를 마쳤습니다.';}
}
for(const g of catalog.groups) {
  const section=el('section','group');section.id=g.id;section.dataset.category=g.category;
  const head=el('div','group-head'), title=el('div');
  title.append(el('div','category',g.category+(g.current!==null?' · 현재 적용':g.kind==='reference'?' · 등급별 참고':' · 미적용 후보')),
    el('h2','',g.title));
  const sequential=el('button','','▶ 3개 이어 듣기');sequential.addEventListener('click',()=>playQueue(g.clips.map((_,i)=>({g,i}))));
  head.append(title,sequential);section.append(head,el('p','group-note',g.note));
  const cards=el('div','cards');
  for(const [i,c] of g.clips.entries()) {
    const chosen=choices[g.id]===undefined?g.current:choices[g.id];
    const card=el('article','card'+(g.current===i?' current':'')+(chosen===i&&g.kind!=='reference'?' chosen':''));
    const top=el('div','card-top');top.append(el('span','number',labels[i]));
    if(g.current===i)top.append(el('span','badge','현재 적용'));
    else if(g.kind==='reference')top.append(el('span','badge','현재 등급별 소리'));
    else top.append(el('span','badge','비교 후보'));
    card.append(top,el('h3','',c.title),el('p','clip-note',c.note));
    const audio=el('audio');audio.controls=true;audio.preload='none';audio.src=c.url;audio.volume=volume/100;
    audio.setAttribute('aria-label',g.title+' '+labels[i]+' '+c.title);
    audio.addEventListener('play',()=>{
      if(queueAudio!==audio){token++;if(releaseQueue){releaseQueue();releaseQueue=null;}if(queueAudio){queueAudio.onended=null;queueAudio.onerror=null;queueAudio=null;}active=audio;$('#now').textContent=g.title+' '+labels[i]+' · '+c.title;}
      for(const other of players.values())if(other!==audio)other.pause();
    });
    audio.addEventListener('ended',()=>{if(queueAudio!==audio)$('#now').textContent='재생을 마쳤습니다.';});
    players.set(g.id+':'+i,audio);card.append(audio);
    const details=el('details');details.append(el('summary','',`${c.duration.toFixed(2)}초 · 원본 / 출처 보기`),el('p','',c.source),el('p','',c.path));card.append(details);
    if(g.kind!=='reference') {
      const choice=el('label','choice'),input=el('input');input.type='radio';input.name=g.id;input.value=i;input.checked=chosen===i;
      input.addEventListener('change',()=>{choices[g.id]=i;for(const sibling of cards.children)sibling.classList.remove('chosen');card.classList.add('chosen');persist();});
      choice.append(input,document.createTextNode('이 소리로 비교 메모'));card.append(choice);
    } else card.append(el('div','reference-label','등급별 구성 · 택일 대상 아님'));
    cards.append(card);
  }
  const memo=el('div','memo'),label=el('label','','메모'),input=el('textarea');input.rows=1;input.id='memo-'+g.id;label.htmlFor=input.id;
  input.placeholder='좋은 점, 아쉬운 점, 원하는 느낌';input.value=notes[g.id]||'';input.addEventListener('input',()=>{notes[g.id]=input.value;persist();});
  memo.append(label,input);section.append(cards,memo);$('#groups').append(section);
}
const filters=['전체','현재 적용','미적용 후보',...new Set(catalog.groups.map(g=>g.category))];
for(const name of filters){const b=el('button',name===filter?'active':'',name);b.addEventListener('click',()=>{filter=name;for(const x of $('#filters').children)x.classList.toggle('active',x===b);applyFilter();});$('#filters').append(b);}
function applyFilter(){let count=0;const query=$('#search').value.trim().toLowerCase();for(const g of catalog.groups){const section=document.getElementById(g.id);const match=filter==='전체'||filter==='현재 적용'&&g.current!==null||filter==='미적용 후보'&&g.current===null&&g.kind!=='reference'||filter===g.category;section.hidden=!(match&&JSON.stringify(g).toLowerCase().includes(query));if(!section.hidden)count++;}$('#count').textContent=count+' / '+catalog.groups.length+'개 항목';$('#empty').hidden=count!==0;}
function result(){return {page:catalog.title,version:catalog.version,note:'청취 비교 메모. Unity 자동 적용 없음. 지원 포탑 전투 무음 유지.',groups:catalog.groups.map(g=>{const selected=choices[g.id]===undefined?g.current:choices[g.id];return {id:g.id,title:g.title,current:g.current===null?null:g.clips[g.current].path,selected:g.kind==='reference'||selected==null?null:g.clips[selected].path,number:g.kind==='reference'||selected==null?null:selected+1,memo:notes[g.id]||''};})};}
function resultText(){return ['코어 가디언 전체 사운드 비교 메모','지원 포탑 전투 무음 유지 · 게임 적용 요청과 별도',...result().groups.map(g=>`${g.title}: ${g.number===null?'미선택 / 등급별 참고':g.number+'번'+(g.selected===g.current?' (현재 적용 유지)':' (비교 후보)')}${g.memo?' — '+g.memo:''}`)].join('\n');}
$('#copy').addEventListener('click',async()=>{const text=resultText();try{await navigator.clipboard.writeText(text);$('#save-status').textContent='선택 결과를 복사했습니다. 채팅에 붙여 주세요.';}catch(_){const box=$('#copy-fallback');box.hidden=false;box.value=text;box.focus();box.select();$('#save-status').textContent='아래 내용을 Ctrl+C로 복사해 주세요.';}});
$('#download').addEventListener('click',()=>{const url=URL.createObjectURL(new Blob([JSON.stringify(result(),null,2)],{type:'application/json'})),a=el('a');a.href=url;a.download='CoreGuardian_Audio_Choices.json';a.click();setTimeout(()=>URL.revokeObjectURL(url),1000);});
$('#play-current').addEventListener('click',()=>playQueue(catalog.groups.filter(g=>g.current!==null).map(g=>({g,i:g.current}))));
$('#stop').addEventListener('click',()=>stop());$('#volume').addEventListener('input',()=>{volume=Number($('#volume').value);volumeUpdate();persist();});$('#search').addEventListener('input',applyFilter);
volumeUpdate();applyFilter();
