"""Generate standalone schematic diagrams, never edit a game screenshot/asset."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

out = Path(__file__).resolve().parent
font_path = 'C:/Windows/Fonts/malgun.ttf'
def font(size): return ImageFont.truetype(font_path, size)
for key, title in [('a','A · 기존 패널 + 전후 비교'),('b','B · 하단 좌우 성장 상세'),('c','C · 작은 성장 요약 상시')]:
    im = Image.new('RGB', (1280, 840), '#101923')
    d = ImageDraw.Draw(im)
    d.text((20,12), title, font=font(26), fill='white')
    d.text((20,52), '미적용 개념도 · 기존 1280×720 경로/배치 참고 · 값은 형식 예시',font=font(17),fill='#bac5cf')
    oy=90
    d.rectangle((0,oy,1279,oy+719),fill='#23312b')
    route=[(68,350),(360,350),(360,207),(970,207),(970,511),(510,511),(510,360),(818,360),(818,430)]
    d.line([(x,y+oy) for x,y in route],fill='#64705f',width=30)
    d.ellipse((800,400+oy,836,436+oy),fill='#82c1cc')
    d.text((590,280+oy),'전투·이동 시야',font=font(25),fill='#b0c5b9')
    def box(x,y,w,h,label,color='#23465b',small=False):
        d.rectangle((x,y+oy,x+w,y+h+oy),fill=color,outline='#86b7c9',width=2)
        d.multiline_text((x+10,y+oy+8),label,font=font(16 if small else 19),fill='white',spacing=8)
    box(333,8,613,43,'코어 · 라운드 · 골드 · 수정 · 증강 수',small=True)
    box(253,667,773,45,'단계 / 시작     타워 설치                         플레이어 강화',small=True)
    box(1083,69,186,115,'누적 피해\n이름·실제 피해\n독립 접기 ↔',small=True)
    if key=='a':
        box(11,80,220,240,'선택 타워 #ID  ↔\n현재 → 다음\n증가량 · 강화 단계\n수정 비용\n강화 / 파괴')
        box(807,495,220,155,'플레이어  ↕\n현재 → 다음\n증가량 · 단계\n골드 비용')
        box(400,607,380,44,'구매 성공: 대상 · 증가량',small=True)
    elif key=='b':
        box(11,490,220,165,'타워 #ID  ↕\n현재 → 다음 / 차이\n단계 · 수정\n강화 / 파괴',small=True)
        box(253,495,328,155,'설치 메뉴  ↕\n기존 분류 / 종류 선택\n선택·취소 규칙 유지')
        box(807,495,220,155,'플레이어  ↕\n현재 → 다음 / 차이\n단계 · 골드',small=True)
        box(590,575,200,80,'성공 / 완료 결과\n짧게 표시\n상세에서 재확인',small=True)
    else:
        box(253,619,330,34,'플레이어: 핵심 스펙 · 직후 증가량',small=True)
        box(620,619,406,34,'선택 타워: 역할별 스펙 · 직후 증가량',small=True)
        box(807,495,220,105,'상세는 독립 접기\n비교 · 비용 · 단계',small=True)
        box(11,80,220,100,'타워 상세 ↔\n선택 없으면 요약 숨김',small=True)
    d.text((20,817),'준비 단계: 이번 전투 시간/보상 잠깐 표시 · 증강 카드/시간/조작 규칙 유지 · 통합 전투력 없음',font=font(16),fill='#c7d2dd')
    im.save(out/f'wireframe_{key}.png')
print('Generated three schematic wireframes; no game files read or changed.')
