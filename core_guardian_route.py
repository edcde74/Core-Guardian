from PIL import Image, ImageDraw, ImageFont


S = 2
W, H = 1400, 850
image = Image.new("RGB", (W * S, H * S), "#20252c")
draw = ImageDraw.Draw(image)


def box(x0, y0, x1, y1, fill, outline=None, width=1, radius=0):
    xy = (x0 * S, y0 * S, x1 * S, y1 * S)
    if radius:
        draw.rounded_rectangle(xy, radius * S, fill=fill, outline=outline, width=width * S)
    else:
        draw.rectangle(xy, fill=fill, outline=outline, width=width * S)


font_path = r"C:\Windows\Fonts\malgun.ttf"
bold_path = r"C:\Windows\Fonts\malgunbd.ttf"
title_font = ImageFont.truetype(bold_path, 31 * S)
sub_font = ImageFont.truetype(font_path, 18 * S)
label_font = ImageFont.truetype(bold_path, 20 * S)

draw.text((75 * S, 39 * S), "코어 가디언  |  프로토타입 전장 동선", font=title_font, fill="#f5f7fa")
draw.text((77 * S, 83 * S), "첨부 이미지의 경로 재구성 · 타워 설치 타일과 플레이어 이동 영역은 미정", font=sub_font, fill="#b6c0ca")

# Orange rectangle is the battlefield boundary. The fine grid only echoes the
# reference's gray floor tiles; it does not mark buildable or walkable cells.
box(110, 145, 1267, 718, "#b5b9ba", outline="#ff8a37", width=5)
for x in range(137, 1267, 51):
    draw.line([(x * S, 147 * S), (x * S, 716 * S)], fill="#d4d7d6", width=2 * S)
for y in range(172, 718, 49):
    draw.line([(112 * S, y * S), (1265 * S, y * S)], fill="#d4d7d6", width=2 * S)

# Coordinates trace the eleven straight sections visible in the attachment.
original = [
    (52, 264), (112, 264), (112, 139), (212, 139),
    (212, 289), (356, 289), (356, 192), (503, 192),
    (503, 267), (552, 267), (552, 117), (590, 117),
]
points = [((110 + (x - 52) * 2.15) * S, (145 + (y - 58) * 1.95) * S) for x, y in original]
draw.line(points, fill="#292d31", width=38 * S, joint="curve")
for x, y in points[1:-1]:
    r = 19 * S
    draw.ellipse((x-r, y-r, x+r, y+r), fill="#292d31")
draw.line(points, fill="#e53732", width=6 * S, joint="curve")

# Endpoint markers sit on the path and identify only facts confirmed by the user.
sx, sy = points[0]
ex, ey = points[-1]
for x, y, color in [(sx, sy, "#34d7c7"), (ex, ey, "#ffcf59")]:
    r = 13 * S
    draw.ellipse((x-r, y-r, x+r, y+r), fill=color, outline="#172026", width=3 * S)

box(34, 564, 156, 609, "#15292b", outline="#34d7c7", width=2, radius=10)
draw.text((51 * S, 573 * S), "적 시작점", font=label_font, fill="#e5fffa")
box(1164, 204, 1293, 249, "#352b1b", outline="#ffcf59", width=2, radius=10)
draw.text((1193 * S, 213 * S), "코어", font=label_font, fill="#fff4d5")

image = image.resize((W, H), Image.Resampling.LANCZOS)
image.save("core_guardian_route.png")
