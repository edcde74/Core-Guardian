from PIL import Image, ImageDraw, ImageFont


S = 2
W, H = 1400, 910
X, Y, T = 105, 150, 59
COLS, ROWS = 20, 11
font_path = r"C:\Windows\Fonts\malgun.ttf"
bold_path = r"C:\Windows\Fonts\malgunbd.ttf"
title_font = ImageFont.truetype(bold_path, 31 * S)
sub_font = ImageFont.truetype(font_path, 19 * S)
label_font = ImageFont.truetype(bold_path, 19 * S)
foot_font = ImageFont.truetype(font_path, 17 * S)


variants = [
    {
        "key": "steps",
        "title": "A  계단 전진형",
        "idea": "전선이 왼쪽에서 오른쪽으로 전진 · 플레이어가 위험 구간을 따라 이동",
        "nodes": [(0,10),(5,10),(5,7),(10,7),(10,4),(15,4),(15,2),(19,2)],
        "pads": [(3,8),(7,9),(8,5),(12,6),(13,2),(17,4)],
    },
    {
        "key": "perimeter",
        "title": "B  외곽 순환형",
        "idea": "경로가 전장 바깥을 크게 순환 · 가운데를 활용한 방어 선택",
        "nodes": [(0,2),(4,2),(4,8),(16,8),(16,2),(12,2)],
        "pads": [(2,5),(6,6),(10,9),(14,6),(18,4),(10,4)],
    },
    {
        "key": "short_s",
        "title": "C  짧은 S자형",
        "idea": "짧은 경로와 적은 굴곡 · 누수 대응의 긴박감 확인",
        "nodes": [(0,5),(7,5),(7,9),(13,9),(13,3),(19,3)],
        "pads": [(3,3),(5,7),(9,7),(11,5),(15,5),(17,1)],
    },
]


def render(v):
    image = Image.new("RGB", (W*S,H*S), "#18222b")
    d = ImageDraw.Draw(image)
    def tx(x,y,t,font,color): d.text((x*S,y*S),t,font=font,fill=color)
    def cell(c,r): return ((X+(c+0.5)*T)*S,(Y+(r+0.5)*T)*S)

    tx(75,37,"코어 가디언  |  " + v["title"],title_font,"#f6f8fa")
    tx(77,85,v["idea"],sub_font,"#b6c4ce")
    d.rectangle((X*S,Y*S,(X+COLS*T)*S,(Y+ROWS*T)*S),fill="#b9c1c3",outline="#e28948",width=5*S)
    for c in range(1,COLS):
        xx=(X+c*T)*S
        d.line(((xx,Y*S),(xx,(Y+ROWS*T)*S)),fill="#dce1e1",width=2*S)
    for r in range(1,ROWS):
        yy=(Y+r*T)*S
        d.line(((X*S,yy),((X+COLS*T)*S,yy)),fill="#dce1e1",width=2*S)

    pts=[cell(*p) for p in v["nodes"]]
    d.line(pts,fill="#252e34",width=43*S,joint="curve")
    for px,py in pts[1:-1]:
        rr=22*S
        d.ellipse((px-rr,py-rr,px+rr,py+rr),fill="#252e34")
    d.line(pts,fill="#e64640",width=7*S,joint="curve")

    for c,r in v["pads"]:
        px,py=cell(c,r)
        rr=19*S
        d.ellipse((px-rr,py-rr,px+rr,py+rr),fill="#126d7b",outline="#73edee",width=4*S)
        d.ellipse((px-5*S,py-5*S,px+5*S,py+5*S),fill="#eaffff")

    for (px,py),color in [(pts[0],"#57dfb9"),(pts[-1],"#ffd16f")]:
        rr=15*S
        d.ellipse((px-rr,py-rr,px+rr,py+rr),fill=color,outline="#17242b",width=4*S)

    # Endpoint labels are placed outside the board to avoid implying tiles.
    sy=pts[0][1]//S
    ey=pts[-1][1]//S
    d.rounded_rectangle((10*S,(sy-54)*S,139*S,(sy-12)*S),radius=9*S,fill="#12332d",outline="#57dfb9",width=2*S)
    tx(29,sy-48,"적 시작",label_font,"#edfff8")
    label_x=min(1242,pts[-1][0]//S+22)
    if label_x>1180: label_x=1197
    d.rounded_rectangle((label_x*S,(ey+22)*S,(label_x+126)*S,(ey+65)*S),radius=9*S,fill="#40321d",outline="#ffd16f",width=2*S)
    tx(label_x+36,ey+29,"코어",label_font,"#fff5db")

    d.rounded_rectangle((105*S,831*S,1285*S,884*S),radius=12*S,fill="#27343d")
    d.line(((132*S,858*S),(207*S,858*S)),fill="#e64640",width=7*S)
    tx(224,842,"적 경로",label_font,"#f6f8fa")
    cx,cy=545*S,857*S
    d.ellipse((cx-17*S,cy-17*S,cx+17*S,cy+17*S),fill="#126d7b",outline="#73edee",width=3*S)
    tx(574,842,"타워 설치 후보",label_font,"#f6f8fa")
    tx(1010,846,"배치 검토안",foot_font,"#b6c4ce")
    return image.resize((W,H),Image.Resampling.LANCZOS)


for v in variants:
    render(v).save("core_guardian_"+v["key"]+".png")

existing=Image.open("core_guardian_new_level_draft.png").convert("RGB")
sheet=Image.new("RGB",(2000,1400),"#18222b")
items=[render(v) for v in variants]+[existing]
for i,item in enumerate(items):
    thumb=item.resize((970,655),Image.Resampling.LANCZOS)
    x=15+(i%2)*1000
    y=15+(i//2)*690
    sheet.paste(thumb,(x,y))
sheet.save("core_guardian_level_comparison.png")
