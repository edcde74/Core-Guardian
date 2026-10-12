from PIL import Image, ImageDraw, ImageFont


S = 2
W, H = 1500, 980
image = Image.new("RGB", (W * S, H * S), "#18222b")
draw = ImageDraw.Draw(image)
font_path = r"C:\Windows\Fonts\malgun.ttf"
bold_path = r"C:\Windows\Fonts\malgunbd.ttf"
title = ImageFont.truetype(bold_path, 31 * S)
sub = ImageFont.truetype(font_path, 18 * S)
label = ImageFont.truetype(bold_path, 18 * S)
small = ImageFont.truetype(font_path, 16 * S)


def text_at(x, y, value, font, color):
    draw.text((x * S, y * S), value, font=font, fill=color)


text_at(73, 37, "코어 가디언  |  새 전장 초안", title, "#f6f8fa")
text_at(75, 83, "기존 경로를 따르지 않고 만든 독립적인 레벨 제안", sub, "#aebbc7")

X, Y, T = 140, 155, 59
COLS, ROWS = 20, 12
draw.rectangle((X*S, Y*S, (X+COLS*T)*S, (Y+ROWS*T)*S), fill="#b9c1c3", outline="#e28948", width=5*S)
for c in range(1, COLS):
    x = (X+c*T)*S
    draw.line(((x, Y*S), (x, (Y+ROWS*T)*S)), fill="#dce1e1", width=2*S)
for r in range(1, ROWS):
    y = (Y+r*T)*S
    draw.line(((X*S, y), ((X+COLS*T)*S, y)), fill="#dce1e1", width=2*S)


def cell(c, r):
    return ((X+(c+0.5)*T)*S, (Y+(r+0.5)*T)*S)


# A new inward route: the enemy traverses an outer defense ring and then
# returns toward an inner core. The existing reference layout is not traced.
nodes = [(0, 5), (4, 5), (4, 2), (16, 2), (16, 9),
         (7, 9), (7, 5), (12, 5), (12, 7)]
points = [cell(*node) for node in nodes]
draw.line(points, fill="#252e34", width=43*S, joint="curve")
for x, y in points[1:-1]:
    rr = 22*S
    draw.ellipse((x-rr,y-rr,x+rr,y+rr), fill="#252e34")
draw.line(points, fill="#e64640", width=7*S, joint="curve")

# Candidate construction cells. They are intentionally few and spread over
# the route's three distinct attack windows; none is a committed placement.
candidates = [(2,3), (6,3), (10,3), (14,4), (14,8), (9,7)]
for c, r in candidates:
    cx, cy = cell(c,r)
    rr = 19*S
    draw.ellipse((cx-rr,cy-rr,cx+rr,cy+rr), fill="#126d7b", outline="#73edee", width=4*S)
    draw.ellipse((cx-5*S,cy-5*S,cx+5*S,cy+5*S), fill="#eaffff")

sx, sy = points[0]
ex, ey = points[-1]
for cx, cy, color in [(sx,sy,"#57dfb9"),(ex,ey,"#ffd16f")]:
    rr=15*S
    draw.ellipse((cx-rr,cy-rr,cx+rr,cy+rr),fill=color,outline="#17242b",width=4*S)

draw.rounded_rectangle((24*S,430*S,176*S,477*S),radius=10*S,fill="#12332d",outline="#57dfb9",width=2*S)
text_at(42, 438, "적 시작", label, "#edfff8")
draw.rounded_rectangle((817*S,620*S,971*S,667*S),radius=10*S,fill="#40321d",outline="#ffd16f",width=2*S)
text_at(862, 628, "코어", label, "#fff5db")

draw.rounded_rectangle((141*S,887*S,1359*S,948*S),radius=12*S,fill="#27343d")
draw.line(((171*S,916*S),(249*S,916*S)),fill="#e64640",width=7*S)
text_at(267, 901, "적 이동 경로", label, "#f6f8fa")
cx,cy=618*S,917*S
draw.ellipse((cx-17*S,cy-17*S,cx+17*S,cy+17*S),fill="#126d7b",outline="#73edee",width=3*S)
text_at(648, 901, "타워 설치 후보", label, "#f6f8fa")
text_at(1004, 906, "모든 배치는 검토안", small, "#b6c4ce")

image.resize((W,H),Image.Resampling.LANCZOS).save("core_guardian_new_level_draft.png")
