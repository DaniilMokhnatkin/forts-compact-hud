"""Crosshair variants for Forts' aiming cursor (ui/textures/mouse_target*.dds).

Each variant is white with a dark outline so it reads on sky, rock and fire,
drawn supersampled on the game's 64x64 canvas. "cross" is the existing pack's
crosshair, copied as is. Output: textures/crosshairs/<id>/mouse_target.dds and
mouse_target_painted.dds (same art), plus textures/crosshairs/<id>.png previews.

    py -3 tools\\draw_crosshairs.py
"""
import os
import shutil

from PIL import Image, ImageDraw, ImageFilter, DdsImagePlugin  # noqa: F401 (registers DDS)

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "textures", "crosshairs")
CROSS_SRC = os.path.join(ROOT, "textures", "crosshair_cross.dds")  # from the earlier crosshair pack
SIZE, SS = 64, 8
C = SIZE / 2
FILL = (255, 255, 255, 255)
OUTLINE = (20, 20, 24, 235)


def canvas():
    return Image.new("L", (SIZE * SS, SIZE * SS), 0)


def s(v):
    return v * SS


def line(draw, x0, y0, x1, y1, w):
    draw.line([s(x0), s(y0), s(x1), s(y1)], fill=255, width=round(s(w)))


def dot(draw, r):
    draw.ellipse([s(C - r), s(C - r), s(C + r), s(C + r)], fill=255)


def ring(draw, r, w):
    draw.ellipse([s(C - r), s(C - r), s(C + r), s(C + r)], outline=255, width=round(s(w)))


def dot_ring(d):
    ring(d, 11, 2.2)
    dot(d, 2.4)


def gap_cross(d):
    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        line(d, C + dx * 6, C + dy * 6, C + dx * 17, C + dy * 17, 2.6)
    dot(d, 1.8)


def brackets(d):
    a, l = 13, 7  # half-size of the square, arm length
    for sx in (-1, 1):
        for sy in (-1, 1):
            x, y = C + sx * a, C + sy * a
            line(d, x, y, x - sx * l, y, 2.4)
            line(d, x, y, x, y - sy * l, 2.4)
    dot(d, 2)


def scope(d):
    ring(d, 14, 2)
    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        line(d, C + dx * 9, C + dy * 9, C + dx * 21, C + dy * 21, 2)
    dot(d, 1.4)


def fine_cross(d):
    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        line(d, C + dx * 3.5, C + dy * 3.5, C + dx * 24, C + dy * 24, 1.6)


ARMS = ((1, 0), (-1, 0), (0, 1), (0, -1))


def polyline(draw, points, w, closed=False):
    pts = [(s(x), s(y)) for x, y in points]
    if closed:
        pts.append(pts[0])
    draw.line(pts, fill=255, width=round(s(w)), joint="curve")


def bold_cross(d):
    for dx, dy in ARMS:
        line(d, C + dx * 5, C + dy * 5, C + dx * 20, C + dy * 20, 4.2)


def small_dot(d):
    dot(d, 3.2)


def double_ring(d):
    ring(d, 8, 2)
    ring(d, 16, 1.6)
    dot(d, 1.4)


def square(d):
    h = 10
    polyline(d, [(C - h, C - h), (C + h, C - h), (C + h, C + h), (C - h, C + h)], 2.2, closed=True)
    dot(d, 1.8)


def diamond(d):
    h = 14
    polyline(d, [(C, C - h), (C + h, C), (C, C + h), (C - h, C)], 2.2, closed=True)
    dot(d, 1.8)


def x_cross(d):
    for dx, dy in ((1, 1), (1, -1), (-1, 1), (-1, -1)):
        line(d, C + dx * 5, C + dy * 5, C + dx * 16, C + dy * 16, 2.4)
    dot(d, 1.6)


def t_cross(d):
    # CS "T" crosshair: no top arm, so nothing covers the target above the aim point.
    for dx, dy in ((1, 0), (-1, 0), (0, 1)):
        line(d, C + dx * 5, C + dy * 5, C + dx * 18, C + dy * 18, 2.6)
    dot(d, 1.8)


def chevron(d):
    # An upside-down V (^): its tip is the aim point.
    a = 15
    polyline(d, [(C - a, C + a), (C, C), (C + a, C + a)], 3.2)


def chevron_double(d):
    a, step = 12, 9
    for i in range(2):
        y = C + i * step
        polyline(d, [(C - a, y + a), (C, y), (C + a, y + a)], 2.6)


def sniper(d):
    # Thin ring crossed by hairlines, like a tank's sniper view.
    ring(d, 17, 1.6)
    for dx, dy in ARMS:
        line(d, C + dx * 4, C + dy * 4, C + dx * 24, C + dy * 24, 1.4)
    dot(d, 1.1)


def mildot(d):
    for dx, dy in ARMS:
        line(d, C + dx * 4, C + dy * 4, C + dx * 24, C + dy * 24, 1.4)
        for r in (12, 19):
            d.ellipse([s(C + dx * r - 2), s(C + dy * r - 2), s(C + dx * r + 2), s(C + dy * r + 2)], fill=255)


def arcade(d):
    # World of Tanks arcade gun marker: small ring with wings left and right.
    ring(d, 8, 2)
    for dx in (1, -1):
        line(d, C + dx * 8, C, C + dx * 22, C, 2.2)
    line(d, C, C + 8, C, C + 14, 2.2)
    dot(d, 1.5)


VARIANTS = {
    "dot_ring": ("Кольцо", dot_ring),
    "gap_cross": ("Разрыв", gap_cross),
    "brackets": ("Рамка", brackets),
    "scope": ("Прицел", scope),
    "fine_cross": ("Тонкий", fine_cross),
    "bold_cross": ("Жирный", bold_cross),
    "dot": ("Точка", small_dot),
    "double_ring": ("Два кольца", double_ring),
    "square": ("Квадрат", square),
    "diamond": ("Ромб", diamond),
    "x_cross": ("Икс", x_cross),
    "t_cross": ("Т-образный", t_cross),
    "chevron": ("Шеврон", chevron),
    "chevron_double": ("Два шеврона", chevron_double),
    "sniper": ("Снайпер", sniper),
    "mildot": ("Мил-дот", mildot),
    "arcade": ("Аркада", arcade),
}


def render(shape):
    mask = canvas()
    shape(ImageDraw.Draw(mask))
    outline = mask.filter(ImageFilter.MaxFilter(2 * SS + 1))  # ~1px dark rim at 64px
    mask = mask.resize((SIZE, SIZE), Image.LANCZOS)
    outline = outline.resize((SIZE, SIZE), Image.LANCZOS)
    image = Image.new("RGBA", (SIZE, SIZE), OUTLINE[:3] + (0,))
    image.putalpha(outline.point(lambda a: a * OUTLINE[3] // 255))
    white = Image.new("RGBA", (SIZE, SIZE), FILL)
    white.putalpha(mask)
    return Image.alpha_composite(image, white)


def save(variant, image):
    folder = os.path.join(OUT, variant)
    os.makedirs(folder, exist_ok=True)
    for name in ("mouse_target", "mouse_target_painted"):
        image.save(os.path.join(folder, name + ".dds"), "DDS")
    image.resize((128, 128), Image.LANCZOS).save(os.path.join(OUT, variant + ".png"))


def main():
    os.makedirs(OUT, exist_ok=True)
    cross = Image.open(CROSS_SRC).convert("RGBA")
    folder = os.path.join(OUT, "cross")
    os.makedirs(folder, exist_ok=True)
    for name in ("mouse_target", "mouse_target_painted"):
        shutil.copyfile(CROSS_SRC, os.path.join(folder, name + ".dds"))
    cross.resize((128, 128), Image.NEAREST).save(os.path.join(OUT, "cross.png"))
    for variant, (_, shape) in VARIANTS.items():
        save(variant, render(shape))
    print(f"wrote {len(VARIANTS) + 1} crosshairs to {OUT}")


if __name__ == "__main__":
    main()
