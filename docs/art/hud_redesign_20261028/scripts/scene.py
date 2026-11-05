from PIL import Image, ImageDraw
CELL = 0.69/0.01875   # 36.8 sprite px
bg = Image.open('/mnt/user-data/uploads/CD/Assets/Art/Environment/Backgrounds/NaturalCourtyard_01_ClearBorder.png').convert('RGBA')
cols, rows = 7, 4
crop = bg.crop((60, 941-60-rows*69, 60+cols*69, 941-60))
Wd, Hd = round(cols*CELL), round(rows*CELL)
ground = crop.resize((Wd, Hd), Image.LANCZOS)
def scene(bname):
    s = ground.copy()
    d = ImageDraw.Draw(s)
    # footprint highlight 2x2 at col0-1,row2-3
    fx0, fy0 = round(1.0*CELL), round(2*CELL)-6
    d.rectangle([fx0, fy0, fx0+round(2*CELL)-1, fy0+round(2*CELL)-1], outline=(80, 220, 255, 140))
    b = Image.open(f'out/{bname}.png')
    bx = fx0 + round(CELL) - b.width//2; by = fy0 + round(2*CELL) - b.height
    t = Image.open('out/ref_single.png'); p = Image.open('out/ref_player.png')
    s.alpha_composite(t, (round(4.5*CELL) - t.width//2, round(4*CELL) - t.height - 8))
    s.alpha_composite(b, (bx, by))
    s.alpha_composite(p, (round(6.0*CELL) - p.width//2, round(4*CELL) - p.height - 8))
    return s
if __name__ == '__main__':
    for n in ('h1A', 'h1B', 'h1C', 'h2A', 'h2B', 'h2C'):
        scene(n).save(f'out/scene_{n}.png')
    im = scene('h1A'); im.resize((im.width*3, im.height*3), Image.NEAREST).save('v_scene.png')
