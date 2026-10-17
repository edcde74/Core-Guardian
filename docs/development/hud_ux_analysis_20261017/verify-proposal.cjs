const {chromium}=require('C:/Users/edcde/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('fs');
(async()=>{
 const browser=await chromium.launch({headless:true,executablePath:'C:/Program Files/Google/Chrome/Application/chrome.exe'});const page=await browser.newPage({viewport:{width:1024,height:900}});const errors=[];
 page.on('pageerror',e=>errors.push(e.message));
 await page.goto('file:///C:/Users/edcde/OneDrive/Desktop/CD/Temp/hud-ux-preview.html');
 const frame=page.frameLocator('iframe');await frame.locator('.cg-mock').first().waitFor();
 const results=[];
 for(const width of [1024,736,320]){
  await page.setViewportSize({width,height:1100});
  for(const layout of ['minimal','bottom','drawer']){
   await frame.locator('[data-layout]').evaluateAll((nodes,l)=>nodes.forEach(n=>n.hidden=n.dataset.layout!==l),layout);
   const panel=frame.locator(`[data-layout="${layout}"]`);
   for(const phase of ['준비','설치 목록','설치 모드','전투','플레이어 강화','증강']){
    await panel.locator('select').selectOption(phase);
    const check=await panel.evaluate(el=>{const b=el.getBoundingClientRect();return [...el.querySelectorAll('button,select,h4,.cg-top,.cg-stage,.cg-inspector,.cg-damage')].filter(n=>n.getClientRects().length&&getComputedStyle(n).visibility!=='hidden').filter(n=>{const r=n.getBoundingClientRect();return r.left<b.left-1||r.right>b.right+1;}).map(n=>n.className||n.textContent);});
    if(check.length)throw Error(`${width}/${layout}/${phase} overflow ${check}`);
   }
   await panel.locator('select').selectOption('플레이어 강화');
   const screenWidth=await panel.locator('.cg-screen').evaluate(el=>el.clientWidth);
   await panel.locator('.cg-screen').click({position:{x:screenWidth/2,y:118}});
   if(await panel.locator('.cg-inspector').isVisible())throw Error(width+'/'+layout+' floor did not fold');
   await panel.locator('[data-action="reopen"]').click();
   if(!await panel.locator('.cg-stats').isVisible())throw Error('reopen not spec');
   await panel.locator('select').selectOption('증강');
   await panel.locator('[data-action="cards"]').click();
   if(await panel.locator('.cg-cards').isVisible())throw Error('cards fold failed');
   if(!await panel.locator('.cg-augment-head').isVisible())throw Error('timer missing');
   await panel.locator('[data-action="cards"]').click();
   if(!await panel.locator('.cg-cards').isVisible())throw Error('cards reopen failed');
   results.push({width,layout,states:6,floorFold:true,reopenSpec:true,augmentationTimerVisible:true});
  }
 }
 await page.setViewportSize({width:1024,height:1000});
 await frame.locator('[data-layout]').evaluateAll(ns=>ns.forEach(n=>n.hidden=n.dataset.layout!=='bottom'));
 await frame.locator('[data-layout="bottom"] select').selectOption('준비');
 await frame.locator('[data-layout="bottom"] .cg-mock').screenshot({path:'C:/Users/edcde/OneDrive/Desktop/CD/docs/development/hud_ux_analysis_20261017/layout-preview.png'});
 const result={kind:'HTML proposal verification only; not Unity QA',results,errors};
 fs.writeFileSync('C:/Users/edcde/OneDrive/Desktop/CD/docs/development/hud_ux_analysis_20261017/proposal-check.json',JSON.stringify(result,null,2));
 console.log(JSON.stringify(result));await browser.close();if(errors.length)process.exitCode=1;
})().catch(e=>{console.error(e);process.exit(1);});
