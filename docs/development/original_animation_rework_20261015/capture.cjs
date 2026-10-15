const {chromium}=require('C:/Users/edcde/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const fs=require('fs');
const path=require('path');
(async()=>{
 const folder=__dirname;
 const browser=await chromium.launch({headless:true,channel:'msedge'});
 const page=await browser.newPage({viewport:{width:1120,height:1100},deviceScaleFactor:1});
 const errors=[];page.on('pageerror',e=>errors.push(String(e)));
 await page.addInitScript(()=>{window.requestAnimationFrame=fn=>{window.pendingDraw=fn;return 1;};});
 await page.goto('file:///'+path.join(folder,'preview.html').replace(/\\/g,'/'));
 await page.waitForFunction(()=>window.preview);
 const checks=await page.evaluate(()=>window.preview.checks);
 if(checks.some(c=>c.differentPixels!==0||c.movingPixels===0))throw Error(JSON.stringify(checks));
 await page.evaluate(()=>{window.preview.select('idle');window.preview.draw(1000);});
 await page.locator('canvas#board').screenshot({path:path.join(folder,'standing_exact.png')});
 await page.evaluate(()=>{window.preview.select('fire');window.preview.draw(1090);});
 await page.locator('canvas#board').screenshot({path:path.join(folder,'attack_peak.png')});
 await page.evaluate(()=>{window.preview.draw(1700);});
 await page.locator('canvas#board').screenshot({path:path.join(folder,'recovered_exact.png')});
 const idle=fs.readFileSync(path.join(folder,'standing_exact.png')),restored=fs.readFileSync(path.join(folder,'recovered_exact.png'));
 // Labels are identical at rest too; this catches failure to return to the original pose.
 if(!idle.equals(restored))throw Error('Standing and recovered canvas differ');
 await page.evaluate(()=>window.preview.select('fire'));
 fs.mkdirSync(path.join(folder,'frames'),{recursive:true});
 for(let i=0;i<36;i++){
  await page.evaluate(t=>window.preview.draw(t),1700+i*40);
  await page.locator('canvas#board').screenshot({path:path.join(folder,'frames',String(i).padStart(3,'0')+'.png')});
 }
 fs.writeFileSync(path.join(folder,'verification.json'),JSON.stringify({checks,recoveredCanvasIdentical:true,pageErrors:errors,scope:'Source-pixel cutout preview only; no Unity integration, no balance result'},null,2));
 if(errors.length)throw Error(errors.join('\n'));
 console.log(JSON.stringify({checks,recoveredCanvasIdentical:true,pageErrors:errors}));
 await browser.close();
})().catch(e=>{console.error(e);process.exit(1);});
