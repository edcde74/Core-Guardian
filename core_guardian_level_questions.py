from PIL import Image, ImageDraw, ImageFont


base = Image.open("core_guardian_route.png").convert("RGB")
image = Image.new("RGB", (1400, 1050), "#20252c")
image.paste(base, (0, 0))
draw = ImageDraw.Draw(image)

font_path = r"C:\Windows\Fonts\malgun.ttf"
bold_path = r"C:\Windows\Fonts\malgunbd.ttf"
head = ImageFont.truetype(bold_path, 22)
body = ImageFont.truetype(font_path, 18)
num_font = ImageFont.truetype(bold_path, 20)

markers = [
    (238, 303, "1", "#65d6c8"),
    (763, 406, "2", "#8baaff"),
    (1185, 548, "3", "#ffd071"),
]
for x, y, number, color in markers:
    draw.ellipse((x - 21, y - 21, x + 21, y + 21), fill="#20252c", outline=color, width=4)
    bounds = draw.textbbox((0, 0), number, font=num_font)
    draw.text((x - (bounds[2]-bounds[0])/2, y - (bounds[3]-bounds[1])/2 - 3), number, font=num_font, fill="#ffffff")

cards = [
    (78, "1  진입 구간", "첫 포탑의 방어와 플레이어 이동을", "익힐 수 있나?", "#65d6c8"),
    (507, "2  중앙 굴곡", "한 포탑이 여러 경로를 지나치게", "덮지는 않나?", "#8baaff"),
    (936, "3  코어 앞", "새어 나온 적에게 플레이어가", "대응할 시간이 있나?", "#ffd071"),
]
for x, title, line1, line2, color in cards:
    draw.rounded_rectangle((x, 770, x + 387, 948), radius=16, fill="#303841", outline=color, width=3)
    draw.text((x + 21, 790), title, font=head, fill=color)
    draw.text((x + 21, 839), line1, font=body, fill="#f3f6f7")
    draw.text((x + 21, 868), line2, font=body, fill="#f3f6f7")

draw.text((80, 981), "검토용 구간 구분 · 타워 설치 위치, 이동 가능 타일, 타일 크기는 아직 확정하지 않음", font=body, fill="#b6c0ca")
image.save("core_guardian_level_questions.png")
