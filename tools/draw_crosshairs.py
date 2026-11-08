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


VARIANTS = {
    "dot_ring": ("Кольцо", dot_ring),
    "gap_cross": ("Разрыв", gap_cross),
    "brackets": ("Рамка", brackets),
    "scope": ("Прицел", scope),
    "fine_cross": ("Тонкий", fine_cross),
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
