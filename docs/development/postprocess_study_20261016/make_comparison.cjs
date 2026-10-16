const fs=require('fs'),path=require('path');
const sharp=require('C:/Users/edcde/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
(async()=>{
const entries=await Promise.all(['original','color','gentle_glow'].map(async k=>[k,'data:image/webp;base64,'+(await sharp(path.join(__dirname,k+'.png')).webp({lossless:true,effort:6}).toBuffer()).toString('base64')]));
const html=fs.readFileSync(path.join(__dirname,'comparison-template.html'),'utf8').replace('ASSET_DATA',JSON.stringify(Object.fromEntries(entries)));
if(Buffer.byteLength(html)>=1000000)throw new Error('Inline visual exceeds 1MB');
const destination='C:/Users/edcde/.codex/visualizations/2026/10/16/01a14423-076c-7e21-9dce-77c1eb3cbf11/core-guardian-postprocessing.html';
fs.writeFileSync(destination,html);console.log(JSON.stringify({destination,bytes:Buffer.byteLength(html)}));
})().catch(e=>{console.error(e);process.exit(1)});
