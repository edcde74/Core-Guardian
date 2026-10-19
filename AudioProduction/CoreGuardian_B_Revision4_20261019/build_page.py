from pathlib import Path
import json,shutil,html
HERE=Path(__file__).resolve().parent;PREV=HERE.parent/'CoreGuardian_B_Revision3_20261019'
cat=json.loads((HERE/'catalog.json').read_text(encoding='utf-8'))
shutil.copyfile(PREV/'style.css',HERE/'style.css')
app=(PREV/'app.js').read_text(encoding='utf-8').replace('coreguardian-feedback-r3-20261019-v1','coreguardian-feedback-r4-20261019-v1').replace('다른 컨셉 비교 3차 선택','증강 등급 보상음 4차 선택').replace('CoreGuardian_B_Revision3_Choices','CoreGuardian_B_Revision4_Choices').replace('5회 반복','3회 반복')
app=app.replace('2차 잠정 선호: 철거① / 라운드 시작③ (이번 새 번호와 별개)','잠정 선호 유지: 철거3차② 마력 회수 (연출에 따라 재검토) / 라운드 시작2차③')
app=app.replace('2차 골드③·플래티넘③은 선택 아님: 통통 튀는 느낌 거부','이전 골드·플래티넘은 미채택. 이번 번호는 4차 전용')
app=app.replace("['2차 골드③','2차 플래티넘③']","['2차 골드③','2차 플래티넘③','3차 골드①','3차 플래티넘①']")
app=app.replace('철거/시작은 잠정, 골드/플래티넘 이전③은 거부.','철거3차②/시작2차③은 잠정, 이전 골드/플래티넘은 미채택.')
(HERE/'app.js').write_text(app,encoding='utf-8',newline='\n')
def audio(url,label):return '<audio data-extra controls preload="none" src="'+url+'" aria-label="'+html.escape(label)+'"></audio>'
body=(PREV/'index.html').read_text(encoding='utf-8')
body=body.replace('코어 가디언 · 다른 컨셉 비교','코어 가디언 · 증강 등급 보상음 검사').replace('CONCEPTS / ROUND 03','AUGMENT / ROUND 04').replace('CONCEPT COMPARISON','TIER REWARD AUDITION')
body=body.replace('같은 강도 변형에서 벗어나<br>다른 재료와 동작으로 비교.','실버를 기준으로<br>골드와 플래티넘의 가치 비교.')
body=body.replace('다른 재료.<br><em>다른 동작.</em>','귀한 보상.<br><em>더 큰 힘의 해방.</em>').replace('철거는 기계·마력·석재, 증강은 패·중량 봉인·압력 해방으로 나눴습니다.<br>골드·플래티넘의 통통 튀는 음정 악기와 추가 타격을 뺐습니다.','선택한 실버③ → 새 골드 → 새 플래티넘을 이어 들어보세요.<br>황동·석실·마력 문양의 세 가지 보상 컨셉을 준비했습니다.')
body=body.replace('<b>3</b><span>비교 항목</span>','<b>2</b><span>재제작 등급</span>').replace('<b>9</b><span>새 컨셉 청취본</span>','<b>6</b><span>새 보상음</span>').replace('0 / 3','0 / 2').replace('max="3"','max="2"').replace('선택 0/3','선택 0/2').replace('3개 항목','2개 항목')
body=body.replace('#round-start',' #tier-sets'.strip()).replace('라운드 시작음이란? ↓','등급 연속 비교 ↓')
start=body.index('<section class="review-panel"><details>');end=body.index('<div class="tools">',start)
kept_end=body.index('</details></section>',start)+len('</details></section>');kept=body[start:kept_end]
silver=next(c for c in cat['retained'] if c['id']=='aug_silver')
panel='<section id="tier-sets" class="review-panel"><h2>먼저 실버 → 골드 → 플래티넘으로 비교하기</h2><p>세트마다 기준 실버③은 동일한 원본입니다. 각 소리 사이에는 0.7초 여백이 있습니다. 먼저 효과음만 듣고, 필요하면 선택 BGM③ 위에서도 확인해 주세요. 실제 게임 녹음이 아닌 청취용 합성입니다.</p><div class="kept-grid">'
for i,r in enumerate(cat['tier_sets']):
 times=' / '.join(f'{t:.2f}초' for t in r['starts'])
 panel+='<article class="kept"><strong>'+str(i+1)+' · '+r['name']+'</strong><small>실버 / 골드 / 플래티넘 시작: '+times+'</small><p>효과음만</p>'+audio(r['dry'],'세트 '+str(i+1)+' 효과음 등급 비교')+'<p>선택 BGM③과 함께</p>'+audio(r['music'],'세트 '+str(i+1)+' BGM 등급 비교')+'</article>'
panel+='</div><p>검사 포인트: 골드가 실버보다 귀하고 묵직한가 / 플래티넘에서 규모가 더 커지는가 / 통통 튀거나 전자 알림처럼 들리는가. 단순히 더 큰 소리인지도 구분해 주세요. 골드와 플래티넘을 다른 번호로 선택해도 됩니다.</p></section>'
panel+='<section class="review-panel"><details><summary>기준 실버③ · 잠정 철거②/라운드 시작③ 확인</summary><div class="kept-grid"><article class="kept"><strong>실버 · 2차③ 원본</strong>'+audio(silver['url'],'기준 실버 2차③')+'</article>'
for c in cat['tentative']:panel+='<article class="kept"><strong>'+c['title']+' · 잠정</strong>'+audio(c['url'],c['title']+' 잠정 선호')+'</article>'
panel+='</div><p>철거는3차②마력회수로 잠정 선택했으며 연출에 따라 재검토합니다. 기존 게임 연결을 변경하지 않았습니다.</p></details></section>'
body=body[:start]+panel+kept+body[end:]
body=body.replace('이번①②③은 3차 컨셉 번호입니다. 2차 번호와 별개이며 모두 미선택으로 시작합니다. 골드·플래티넘의 2차③은 거부로 기록했습니다.','이번①②③은 4차 보상 컨셉 번호입니다. 이전 골드·플래티넘은 미채택으로 보존하고, 새 후보는 미선택으로 시작합니다.')
body=body.replace('3차 컨셉','4차 보상 컨셉').replace('Kenney CC0·기존 취득 음원의 로컬 파생과 새 비음정 폴리.','선택 실버의 초기 접촉·Kenney CC0 타격 파생과 새 고정 배음/동시 화음 합성.').replace('CONCEPT CHOICES','TIER REWARD CHOICES')
(HERE/'index.html').write_text(body,encoding='utf-8',newline='\n')
server=(PREV/'serve.py').read_text(encoding='utf-8').replace('8884','8885');(HERE/'serve.py').write_text(server,encoding='utf-8',newline='\n')
(HERE/'.gitignore').write_text('.local/\n__pycache__/\n',encoding='utf-8',newline='\n')
(HERE/'Open_Revision4.cmd').write_bytes(b'@echo off\r\nstart "" "%~dp0index.html"\r\n')
