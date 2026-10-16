const fs=require('fs'),path=require('path');
const root=path.resolve(__dirname,'../../..');
const image=p=>'data:image/png;base64,'+fs.readFileSync(path.join(root,p)).toString('base64');
const assets={core:image('Assets/Art/ScreenTests/20260930/Sprites/core.png'),icon:image('Assets/Resources/CoreGuardian/Hud/Core.png'),hit:image('Assets/Resources/CoreGuardian/Art/CombatB.png')};
let html=fs.readFileSync(path.join(__dirname,'template.html'),'utf8').replace('ASSET_DATA',JSON.stringify(assets));
fs.writeFileSync(path.join(__dirname,'preview.html'),html);
