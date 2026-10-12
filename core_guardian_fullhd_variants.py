from PIL import Image, ImageDraw, ImageFont


W, H = 1920, 1080
X, Y, T = 240, 130, 72
COLS, ROWS = 20, 11
font_path = r"C:\Windows\Fonts\malgun.ttf"
bold_path = r"C:\Windows\Fonts\malgunbd.ttf"
title_font = ImageFont.truetype(bold_path, 31)
hud_font = ImageFont.truetype(font_path, 21)
tag_font = ImageFont.truetype(bold_path, 19)
small_font = ImageFont.truetype(font_path, 18)

variants = [
    ("A", "계단 전진형", [(0,10),(5,10),(5,7),(10,7),(10,4),(15,4),(15,2),(19,2)]),
    ("B", "외곽 순환형", [(0,2),(4,2),(4,8),(16,8),(16,2),(12,2)]),
    ("C", "짧은 S자형", [(0,5),(7,5),(7,9),(13,9),(13,3),(19,3)]),
    ("D", "안쪽 코어형", [(0,5),(4,5),(4,2),(16,2),(16,9),(7,9),(7,5),(12,5),(12,7)]),
]


def draw_variant(key, name, nodes):
    image = Image.new("RGB", (W,H), "#15202a")
    d = ImageDraw.Draw(image)
    d.rectangle((0,0,W,108), fill="#26333d")
    d.rectangle((0,942,W,H), fill="#26333d")
    d.text((52,26), f"{key}  {name}", font=title_font, fill="#f5f8fa")
    d.text((1370,37), "상단 HUD 공간  ·  배치 미정", font=hud_font, fill="#b7c7d2")
    d.text((54,970), "적 경로", font=tag_font, fill="#f3f7f9")
    d.line(((159,984),(235,984)), fill="#e64640", width=7)
    d.text((560,971), "하단 HUD 공간  ·  배치 미정", font=hud_font, fill="#b7c7d2")
    d.text((1498,971), "플레이어 이동 타일 미정", font=small_font, fill="#b7c7d2")

    d.rectangle((X,Y,X+COLS*T,Y+ROWS*T), fill="#bbc3c5", outline="#e28948", width=5)
    for c in range(1,COLS):
        xx=X+c*T
        d.line(((xx,Y),(xx,Y+ROWS*T)), fill="#dce1e1", width=2)
    for r in range(1,ROWS):
        yy=Y+r*T
        d.line(((X,yy),(X+COLS*T,yy)), fill="#dce1e1", width=2)

    def cell(c,r): return (X+(c+0.5)*T,Y+(r+0.5)*T)
    pts=[cell(*p) for p in nodes]
    d.line(pts, fill="#252e34", width=52, joint="curve")
    for px,py in pts[1:-1]: d.ellipse((px-26,py-26,px+26,py+26), fill="#252e34")
    d.line(pts, fill="#e64640", width=8, joint="curve")
    for (px,py),color in [(pts[0],"#57dfb9"),(pts[-1],"#ffd16f")]:
        d.ellipse((px-18,py-18,px+18,py+18), fill=color, outline="#17242b", width=4)

    # Keep labels within the board while avoiding the center of the route.
    sx,sy=pts[0]
    ex,ey=pts[-1]
    d.rounded_rectangle((sx+20,sy-58,sx+118,sy-25),radius=7,fill="#12332d",outline="#57dfb9",width=2)
    d.text((sx+32,sy-54),"시작",font=tag_font,fill="#edfff8")
    lx=max(X+15,min(ex+22,X+COLS*T-110))
    ly=ey+27 if ey<Y+ROWS*T-75 else ey-65
    d.rounded_rectangle((lx,ly,lx+95,ly+34),radius=7,fill="#40321d",outline="#ffd16f",width=2)
    d.text((lx+25,ly+4),"코어",font=tag_font,fill="#fff5db")
    return image


images=[]
for key,name,nodes in variants:
    img=draw_variant(key,name,nodes)
    img.save(f"core_guardian_fullhd_{key}.png")
    images.append(img)

sheet=Image.new("RGB",(1920,1080),"#15202a")
for i,img in enumerate(images):
    thumb=img.resize((940,529),Image.Resampling.LANCZOS)
    sheet.paste(thumb,(20+(i%2)*960,10+(i//2)*540))
sheet.save("core_guardian_fullhd_comparison.png")
