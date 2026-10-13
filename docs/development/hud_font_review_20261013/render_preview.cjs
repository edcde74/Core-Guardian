const {chromium}=require('C:/Users/edcde/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('fs');const path=require('path');const {pathToFileURL}=require('url');
const cards=JSON.parse(fs.readFileSync(path.join(__dirname,'card_samples.json'),'utf8'));
(async()=>{
  const browser=await chromium.launch({headless:true,channel:'msedge'});
  try{
    const page=await browser.newPage({viewport:{width:1664,height:1330},deviceScaleFactor:1});
    const errors=[];page.on('pageerror',e=>errors.push(e.message));
    await page.goto(pathToFileURL(path.join(__dirname,'preview.html')).href);
    await page.evaluate(()=>document.fonts.ready);
    const result=await page.evaluate(()=>({fonts:[...document.fonts].map(f=>({family:f.family,status:f.status})),images:[...document.images].every(i=>i.complete&&i.naturalWidth>0),overflow:[...document.querySelectorAll('.fit')].filter(e=>e.scrollHeight>e.clientHeight+1||e.scrollWidth>e.clientWidth+1).map(e=>({text:e.textContent,width:e.clientWidth,scrollWidth:e.scrollWidth,height:e.clientHeight,scrollHeight:e.scrollHeight})),size:{width:document.querySelector('.board').offsetWidth,height:document.querySelector('.board').offsetHeight}}));
    await page.locator('.board').screenshot({path:path.join(__dirname,'font_comparison.png')});
    for(const id of ['a','b','c'])await page.locator('.option.'+id).screenshot({path:path.join(__dirname,'option_'+id+'.png')});
    await page.locator('.option.a .install').first().hover();
    await page.screenshot({path:path.join(__dirname,'hover_preview.png'),fullPage:true});
    const cardChecks=[];
    for(const id of ['a','b','c'])for(const card of cards){
      cardChecks.push(await page.evaluate(({id,card})=>{
        const title=document.querySelector('.option.'+id+' .card-title');
        const effect=document.querySelector('.option.'+id+' .card-effect');
        title.textContent=card.title;effect.textContent=card.effect;
        return {option:id,title:card.title,titleFits:title.scrollHeight<=title.clientHeight+1&&title.scrollWidth<=title.clientWidth+1,effectFits:effect.scrollHeight<=effect.clientHeight+1&&effect.scrollWidth<=effect.clientWidth+1};
      },{id,card}));
    }
    const summary={...result,cardCount:cards.length,cardChecks,errors};
    fs.writeFileSync(path.join(__dirname,'render_check.json'),JSON.stringify(summary,null,2)+'\n');
    console.log(JSON.stringify({...result,cardCount:cards.length,cardFailures:cardChecks.filter(c=>!c.titleFits||!c.effectFits),errors}));
    if(result.overflow.length||errors.length||!result.images||result.fonts.some(f=>f.status!=='loaded')||cardChecks.some(c=>!c.titleFits||!c.effectFits))process.exitCode=1;
  }finally{await browser.close()}
})().catch(e=>{console.error(e);process.exitCode=1});
