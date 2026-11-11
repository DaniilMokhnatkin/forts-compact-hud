"""Draw pixel-precise game cursors from the Duo / Neo ImageGen concept sheets.

The concept PNGs are references, not raster inputs. All masks below are native
geometry on Forts' 64x64 canvas; the hotspot is always (32, 32).
"""
import math
from PIL import Image, ImageChops, ImageDraw, ImageFilter

from draw_crosshairs import C, SIZE, SS, OUTLINE, canvas, dot, line, ring, save

RED = (255, 66, 72, 255)
CYAN = (48, 231, 255, 255)
MINT = (113, 255, 166, 255)
AMBER = (255, 208, 64, 255)
VIOLET = (179, 164, 255, 255)
WHITE = (255, 255, 255, 255)


def bar(d, x0, y0, x1, y1, w=2.4):
    line(d, C + x0, C + y0, C + x1, C + y1, w)


def path(d, points, w=2.4, closed=False):
    pts = [((C + x) * SS, (C + y) * SS) for x, y in points]
    if closed:
        pts.append(pts[0])
    d.line(pts, fill=255, width=round(w * SS), joint="curve")


def arc(d, r, start, end, w=2.4):
    d.arc(((C - r) * SS, (C - r) * SS, (C + r) * SS, (C + r) * SS),
          start, end, fill=255, width=round(w * SS))


def radial(d, angles, inner, outer, w=2.4):
    for angle in angles:
        a = math.radians(angle)
        bar(d, math.cos(a) * inner, math.sin(a) * inner,
            math.cos(a) * outer, math.sin(a) * outer, w)


def plus(d, r=3, w=1.8):
    bar(d, -r, 0, r, 0, w)
    bar(d, 0, -r, 0, r, w)


def diamond(d, r=4, w=1.8):
    path(d, [(0, -r), (r, 0), (0, r), (-r, 0)], w, closed=True)


def duo_cross(d):
    radial(d, (0, 90, 180, 270), 6, 18, 2.8)


def duo_horseshoe(d):
    arc(d, 15, -65, 245, 2.8)


def duo_wings(d):
    for x, y in ((1, 0), (-1, 0), (0, 1)):
        bar(d, x * 6, y * 6, x * 19, y * 19, 2.8)


def duo_focus(d):
    for x in (-1, 1):
        for y in (-1, 1):
            path(d, [(x * 5, y * 11), (x * 11, y * 11), (x * 11, y * 5)], 2.8)


def duo_tripod(d):
    radial(d, (-90, 30, 150), 6, 20, 2.8)


def duo_diamond(d):
    for angle in (0, 90, 180, 270):
        a = math.radians(angle)
        pts = [(x * math.cos(a) - y * math.sin(a),
                x * math.sin(a) + y * math.cos(a)) for x, y in [(-6, -10), (0, -16), (6, -10)]]
        path(d, pts, 2.8)


def duo_bowl(d):
    path(d, [(-17, -5), (-10, 10), (10, 10), (17, -5)], 3)


def duo_diagonal(d):
    radial(d, (45, 135, 225, 315), 8, 23, 2.8)


def duo_parentheses(d):
    arc(d, 15, 135, 225, 2.8)
    arc(d, 15, -45, 45, 2.8)


def duo_aperture(d):
    for a in (0, 90, 180, 270):
        arc(d, 15, a + 14, a + 76, 2.6)
    radial(d, (0, 90, 180, 270), 17, 22, 2.6)


def duo_chevron(d):
    path(d, [(-13, 13), (0, 0), (13, 13)], 3)


def duo_notch(d):
    path(d, [(-14, -10), (-14, 11), (14, 11), (14, -10)], 2.6)
    bar(d, -14, 0, -8, 0)
    bar(d, 8, 0, 14, 0)


def duo_hex(d):
    pts = [(math.cos(math.radians(a)) * 14, math.sin(math.radians(a)) * 14)
           for a in range(0, 360, 60)]
    path(d, pts, 2.6, closed=True)
    radial(d, (0, 180), 15, 23, 2.8)


def duo_pointers(d):
    for angle in (-90, 30, 150):
        a = math.radians(angle)
        pts = [(x * math.cos(a) - y * math.sin(a),
                x * math.sin(a) + y * math.cos(a)) for x, y in [(7, 0), (18, -5), (18, 5)]]
        d.polygon([((C + x) * SS, (C + y) * SS) for x, y in pts], fill=255)


def duo_range(d):
    radial(d, (0, 180), 6, 21, 2.8)
    for x in (-21, 21):
        bar(d, x, -6, x, 6, 2.8)


def duo_optic(d):
    for a in (-90, 30, 150):
        arc(d, 17, a + 12, a + 108, 2)
    radial(d, (-90, 30, 150), 8, 23, 2.6)


DUO = {
    "cross": ("Duo Крест", duo_cross),
    "horseshoe": ("Подкова", duo_horseshoe),
    "wings": ("Крылья", duo_wings),
    "focus": ("Фокус", duo_focus),
    "tripod": ("Тренога", duo_tripod),
    "diamond": ("Грани", duo_diamond),
    "bowl": ("Чаша", duo_bowl),
    "diagonal": ("Диагональ", duo_diagonal),
    "parentheses": ("Скобки", duo_parentheses),
    "aperture": ("Апертура", duo_aperture),
    "chevron": ("Остриё", duo_chevron),
    "notch": ("Выемка", duo_notch),
    "hex": ("Соты", duo_hex),
    "pointers": ("Указатели", duo_pointers),
    "range": ("Дальномер", duo_range),
    "optic": ("Три луча", duo_optic),
}


def neo_prism(w, a):
    radial(w, (0, 180), 9, 21, 2.6)
    diamond(a)


def neo_fang(w, a):
    path(a, [(-11, 11), (0, 0), (11, 11)], 2.8)
    for x in (-1, 1):
        bar(w, x * 12, 15, x * 18, 20, 2.6)


def neo_halo(w, a):
    for angle in (-90, 30, 150):
        arc(w, 17, angle + 14, angle + 106, 2.8)
    plus(a)


def neo_fork(w, a):
    for x in (-13, 13):
        bar(w, x, -16, x, 16, 2.8)
    path(a, [(-7, -3), (-3, -3), (0, 0), (3, -3), (7, -3)], 2.2)


def neo_triad(w, a):
    radial(w, (-90, 30, 150), 15, 21, 2.8)
    path(a, [(0, -8), (6.93, 4), (-6.93, 4)], 2.2, closed=True)


def neo_needle(w, a):
    radial(w, (0, 180), 8, 23, 1.8)
    bar(w, -4, 9, 4, 9, 1.8)
    bar(w, -2, 15, 2, 15, 1.8)
    plus(a, 3, 2)


def neo_eclipse(w, a):
    arc(w, 17, 35, 325, 2.8)
    path(a, [(-4, -6), (0, 0), (-4, 6)], 2.6)


def neo_kite(w, a):
    duo_diamond(w)
    path(a, [(-3, -3), (3, -3), (3, 3), (-3, 3)], 1.8, closed=True)


def neo_anchor(w, a):
    path(w, [(-22, 4), (-14, 4), (-9, 12), (9, 12), (14, 4), (22, 4)], 2.8)
    plus(a)


def neo_beacon(w, a):
    path(w, [(-20, -9), (-11, 0), (-20, 9)], 2.8)
    path(w, [(20, -9), (11, 0), (20, 9)], 2.8)
    for y in (-1, 1):
        bar(a, 0, y * 6, 0, y * 13, 2.2)


def neo_orbit(w, a):
    for angle in (0, 90, 180, 270):
        arc(a, 15, angle + 16, angle + 74, 2.8)
    ring(w, 3, 1.6)


def neo_horizon(w, a):
    duo_range(w)
    path(a, [(0, 0), (-5, 8), (5, 8)], 2, closed=True)


def neo_petal(w, a):
    for angle in (0, 90, 180, 270):
        arc(w, 15, angle + 15, angle + 75, 3.5)
    diamond(a)


def neo_pulse(w, a):
    radial(w, (45, 135, 225, 315), 9, 22, 3)
    plus(a)


def neo_gate(w, a):
    for x in (-1, 1):
        path(w, [(x * 9, -13), (x * 16, -13), (x * 16, 13), (x * 9, 13)], 2.8)
    plus(a)


def neo_star(w, a):
    radial(w, (-90, -30, 30, 90, 150, 210), 12, 21, 2.4)
    path(a, [(math.cos(math.radians(t)) * 5, math.sin(math.radians(t)) * 5)
             for t in range(30, 390, 60)], 1.8, closed=True)


NEO = {
    "prism": ("Призма", neo_prism, CYAN),
    "fang": ("Клык", neo_fang, AMBER),
    "halo": ("Ореол", neo_halo, CYAN),
    "fork": ("Камертон", neo_fork, AMBER),
    "triad": ("Триада", neo_triad, MINT),
    "needle": ("Игла", neo_needle, CYAN),
    "eclipse": ("Затмение", neo_eclipse, AMBER),
    "kite": ("Кайт", neo_kite, VIOLET),
    "anchor": ("Якорь", neo_anchor, MINT),
    "beacon": ("Маяк", neo_beacon, AMBER),
    "orbit": ("Орбита", neo_orbit, CYAN),
    "horizon": ("Горизонт", neo_horizon, AMBER),
    "petal": ("Лепесток", neo_petal, MINT),
    "pulse": ("Импульс", neo_pulse, VIOLET),
    "gate": ("Врата", neo_gate, CYAN),
    "star": ("Звезда", neo_star, AMBER),
}


def render(white, accent, colour):
    combined = ImageChops.lighter(white, accent)
    outline = combined.filter(ImageFilter.MaxFilter(2 * SS + 1))
    image = Image.new("RGBA", (SIZE, SIZE), OUTLINE[:3] + (0,))
    outline = outline.resize((SIZE, SIZE), Image.Resampling.LANCZOS)
    image.putalpha(outline.point(lambda v: v * OUTLINE[3] // 255))
    for mask, fill in ((white, WHITE), (accent, colour)):
        layer = Image.new("RGBA", image.size, fill)
        layer.putalpha(mask.resize(image.size, Image.Resampling.LANCZOS))
        image = Image.alpha_composite(image, layer)
    return image


def main():
    for key, (_, shape) in DUO.items():
        white, accent = canvas(), canvas()
        shape(ImageDraw.Draw(white))
        dot(ImageDraw.Draw(accent), 2.1)
        save("duo_" + key, render(white, accent, RED))
    for key, (_, shape, colour) in NEO.items():
        white, accent = canvas(), canvas()
        shape(ImageDraw.Draw(white), ImageDraw.Draw(accent))
        save("neo_" + key, render(white, accent, colour))
    print(f"Wrote {len(DUO)} Duo and {len(NEO)} Neo crosshairs")


if __name__ == "__main__":
    main()
