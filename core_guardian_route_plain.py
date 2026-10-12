from PIL import Image, ImageDraw, ImageFont


W, H = 1920, 1080
image = Image.new("RGB", (W, H), "#bfc5c5")
draw = ImageDraw.Draw(image)

# D inward-core route. No tile grid, HUD, build locations, player marker,
# or other map annotation is drawn in this presentation image.
points = [
    (276, 526), (564, 526), (564, 310), (1428, 310),
    (1428, 814), (780, 814), (780, 526), (1140, 526),
    (1140, 670),
]

draw.line(points, fill="#252a2e", width=66, joint="curve")
for x, y in points[1:-1]:
    draw.ellipse((x-33, y-33, x+33, y+33), fill="#252a2e")
for x, y in (points[0], points[-1]):
    draw.ellipse((x-33, y-33, x+33, y+33), fill="#252a2e")
draw.line(points, fill="#e33f3b", width=9, joint="curve")

# Small legend requested for the red center line.
font = ImageFont.truetype(r"C:\Windows\Fonts\malgunbd.ttf", 30)
draw.line(((110, 115), (184, 115)), fill="#e33f3b", width=9)
draw.text((208, 94), "빨간 선 = 적 경로", font=font, fill="#252a2e")

image.save("core_guardian_route_plain_1920x1080.png")
