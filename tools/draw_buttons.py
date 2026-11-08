"""Draw Pause / Play / Menu HUD buttons in the style of the dlc3 skin's Grid button.

Source: textures/grid_button_dlc3.png, the Grid button captured in game at 1440p
(the skin's own textures are packed). Its glyph is painted over with the face
colour and a new glyph is drawn. Output: textures/HUD/custom/<Name>-<State>.dds,
64x64 RGBA, states A (normal), R (rollover), S (pressed), D (disabled), plus a
preview sheet textures/preview.png.

    py -3 tools\\draw_buttons.py
"""
import os

from PIL import Image, ImageChops, ImageDraw, ImageEnhance, ImageFilter, ImageOps, DdsImagePlugin  # noqa: F401 (registers DDS)

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "textures", "grid_button_dlc3.png")
OUT = os.path.join(ROOT, "textures", "HUD", "custom")
SIZE, SS = 64, 8  # texture size, supersampling for antialiased glyphs

# Regions on the 64x64 button, measured on the capture.
FACE = (11, 9, 50, 43)        # orange face, above the dark bottom lip
GLYPH = (19, 13, 45, 39)      # where the Grid glyph sits
RIM = (3, 3, 61, 61)          # silver rim; outside is transparent
RIM_RADIUS = 6
GLYPH_TOP, GLYPH_BOTTOM = (252, 162, 112), (248, 126, 86)  # glyph gradient, sampled


def clean_face(button):
    """Paint the face with its own colour, sampled left and right of the glyph."""
    px = button.load()
    x0, y0, x1, y1 = FACE
    for y in range(y0, y1):
        left, right = px[x0 + 2, y][:3], px[x1 - 3, y][:3]
        for x in range(x0, x1):
            t = (x - x0) / (x1 - x0 - 1)
            px[x, y] = tuple(round(a + (b - a) * t) for a, b in zip(left, right)) + (255,)
    return button


def glyph_layer(shapes):
    """Glyph as an RGBA layer: vertical gradient fill through a supersampled mask."""
    mask = Image.new("L", (SIZE * SS, SIZE * SS), 0)
    draw = ImageDraw.Draw(mask)
    for kind, coords in shapes:
        pts = [c * SS for c in coords]
        if kind == "rect":
            draw.rounded_rectangle(pts, radius=SS, fill=255)
        else:
            draw.polygon(list(zip(pts[0::2], pts[1::2])), fill=255)
    mask = mask.resize((SIZE, SIZE), Image.LANCZOS)
    fill = Image.new("RGBA", (SIZE, SIZE))
    _, gy0, _, gy1 = GLYPH
    for y in range(SIZE):
        t = min(max((y - gy0) / (gy1 - gy0), 0), 1)
        colour = tuple(round(a + (b - a) * t) for a, b in zip(GLYPH_TOP, GLYPH_BOTTOM))
        for x in range(SIZE):
            fill.putpixel((x, y), colour + (255,))
    fill.putalpha(mask)
    return fill


def glyphs():
    gx0, gy0, gx1, gy1 = GLYPH
    cx, cy = (gx0 + gx1) / 2, (gy0 + gy1) / 2
    h = gy1 - gy0 - 4
    bar, gap = 6.5, 5.5
    left = (21, 26, 32, 16, 32, 22, 44, 22, 44, 30, 32, 30, 32, 36)
    right = tuple(64 - x if i % 2 == 0 else x for i, x in enumerate(left))
    return {
        "FacingLeft": [("poly", left)],
        "FacingRight": [("poly", right)],
        "FacingAuto": [("poly", (22, 36, 29, 16, 35, 16, 42, 36, 37, 36, 32, 21, 27, 36)), ("rect", (27, 28, 37, 32))],
        # Chevron up: the facing buttons open in a row above the toggle.
        "FacingMenu": [("poly", (20, 29.5, 32, 18.5, 44, 29.5, 40, 33.5, 32, 26.5, 24, 33.5))],
        "Pause": [("rect", (cx - gap / 2 - bar, cy - h / 2, cx - gap / 2, cy + h / 2)),
                  ("rect", (cx + gap / 2, cy - h / 2, cx + gap / 2 + bar, cy + h / 2))],
        "Play": [("poly", (cx - h * 0.38, cy - h / 2, cx + h * 0.5, cy, cx - h * 0.38, cy + h / 2))],
        "Menu": [("rect", (cx - h / 2, cy - h / 2 + i * (h - 4.5) / 2, cx + h / 2, cy - h / 2 + i * (h - 4.5) / 2 + 4.5))
                 for i in range(3)],
    }


def rim_mask():
    mask = Image.new("L", (SIZE * SS, SIZE * SS), 0)
    ImageDraw.Draw(mask).rounded_rectangle([c * SS for c in RIM], radius=RIM_RADIUS * SS, fill=255)
    return mask.resize((SIZE, SIZE), Image.LANCZOS)


def states(normal):
    """Rollover brighter, pressed darker and 1px lower, disabled greyed out."""
    rgb, alpha = normal.convert("RGB"), normal.getchannel("A")
    rollover = ImageEnhance.Brightness(rgb).enhance(1.15)
    pressed = Image.new("RGB", rgb.size)
    pressed.paste(ImageEnhance.Brightness(rgb).enhance(0.85), (0, 1))
    pressed_alpha = Image.new("L", rgb.size, 0)
    pressed_alpha.paste(alpha, (0, 1))
    grey = ImageOps.grayscale(rgb).convert("RGB")
    disabled = ImageEnhance.Brightness(Image.blend(rgb, grey, 0.75)).enhance(0.7)
    out = {"A": normal, "R": rollover.convert("RGBA"), "S": pressed.convert("RGBA"), "D": disabled.convert("RGBA")}
    for key, alpha_layer in (("R", alpha), ("S", pressed_alpha), ("D", alpha)):
        out[key].putalpha(alpha_layer)
    return out


def main():
    os.makedirs(OUT, exist_ok=True)
    base = Image.open(SRC).convert("RGBA").resize((SIZE, SIZE), Image.LANCZOS)
    base = clean_face(base)
    base.putalpha(rim_mask())
    sheet = Image.new("RGBA", (SIZE * 4 + 50, (SIZE + 10) * len(glyphs()) + 10), (40, 33, 46, 255))
    for row, (name, shapes) in enumerate(glyphs().items()):
        button = Image.alpha_composite(base, glyph_layer(shapes))
        for col, (state, image) in enumerate(states(button).items()):
            image.save(os.path.join(OUT, f"{name}-{state}.dds"), "DDS")
            sheet.paste(image, (10 + col * (SIZE + 10), 10 + row * (SIZE + 10)), image)
    sheet.resize((sheet.width * 3, sheet.height * 3), Image.NEAREST).save(os.path.join(ROOT, "textures", "preview.png"))
    print(f"wrote {OUT}")
    draw_skins()


# The other HUD skins. Each one's Grid button was cut from a 1440p screenshot to
# textures/skins/grid_<skin>.png (64x64 at the texture's scale). Its glyph is
# painted over with the face colour and our glyphs are drawn in the skin's glyph
# colour, moved from the dlc3 glyph centre (32, 26) to this skin's. The skin set
# is picked by the installer from the game's CurrentHUDSkin.
SKINS = {
    # shape (mask), glyph box to clean, glyph centre, glyph colours top/bottom
    "base": {"mask": ("round", (2, 2, 62, 61), 8), "glyph": (15, 17, 48, 50), "centre": (31.5, 33.5),
             "colour": ((226, 212, 196), (205, 188, 170))},
    "dlc2": {"mask": ("round", (2, 2, 62, 61), 8), "glyph": (15, 17, 48, 50), "centre": (31.5, 33.5),
             "colour": ((226, 212, 196), (205, 188, 170))},
    "pro": {"mask": ("round", (7, 3, 58, 59), 12), "glyph": (18, 16, 46, 47), "centre": (32, 31.5), "scale": 0.9,
            "colour": ((50, 235, 70), (25, 205, 45))},
    # Elite: a generated blank knob (textures/skins/knob_elite_source.png, 1254 px, its
    # knob centre at (620.5, 672), radius 440.5) placed where the game's knob sits,
    # plus the white pointer light on the rim at the "0" end of the dial.
    "elite": {"art": ("knob_elite_source.png", (620.5, 672), 440.5, (33.5, 35.5), 26.5), "pointer": (285, 385),
              "centre": (33.3, 30.1), "scale": 0.8,
              "colour": ((222, 228, 236), (190, 198, 210))},
    # High Seas: the whole face is cleaned, edge to edge, so no patch shows.
    "dlc1": {"mask": ("rect", (0, 0, 64, 63), 0), "glyph": (6, 10, 58, 58), "flat": (12, 30), "centre": (33, 32),
             "colour": ((70, 205, 255), (35, 175, 245))},
}
SKIN_OUT = os.path.join(ROOT, "textures", "HUD", "skins")


def knob_from_art(file, centre, radius, target, target_radius, pointer):
    """Scale the knob art so its knob lands on the game's knob (at 8x, then down),
    and light the pointer: a white dot with a soft glow, drawn on the art."""
    art = Image.open(os.path.join(ROOT, "textures", "skins", file)).convert("RGBA")
    # The pointer is a short white tick on the rim, radial, aimed at the "0" end.
    (px0, py0), (cx, cy) = pointer, centre
    dx, dy = px0 - cx, py0 - cy
    n = (dx * dx + dy * dy) ** 0.5
    ux, uy = dx / n, dy / n
    inner = (px0 - ux * 40, py0 - uy * 40)
    outer = (px0 + ux * 48, py0 + uy * 48)
    halo = Image.new("L", art.size, 0)
    ImageDraw.Draw(halo).line([inner, outer], fill=150, width=78)
    halo = halo.filter(ImageFilter.GaussianBlur(18))
    glow = Image.new("RGBA", art.size, (255, 255, 255, 0))
    glow.putalpha(halo)
    tick = Image.new("RGBA", art.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(tick)
    d.line([inner, outer], fill=(255, 255, 255, 255), width=38)
    for end in (inner, outer):  # round caps
        d.ellipse([end[0] - 19, end[1] - 19, end[0] + 19, end[1] + 19], fill=(255, 255, 255, 255))
    glow = Image.alpha_composite(glow, tick)
    art = Image.alpha_composite(art, glow)
    scale = target_radius / radius * SS
    big = art.resize((round(art.width * scale), round(art.height * scale)), Image.LANCZOS)
    canvas = Image.new("RGBA", (SIZE * SS, SIZE * SS), (0, 0, 0, 0))
    canvas.alpha_composite(big, (round(target[0] * SS - centre[0] * scale), round(target[1] * SS - centre[1] * scale)))
    return canvas.resize((SIZE, SIZE), Image.LANCZOS)


def skin_mask(shape):
    kind, box, radius = shape
    mask = Image.new("L", (SIZE * SS, SIZE * SS), 0)
    d = ImageDraw.Draw(mask)
    if kind == "circle":
        (cx, cy), r = box, radius
        d.ellipse([(cx - r) * SS, (cy - r) * SS, (cx + r) * SS, (cy + r) * SS], fill=255)
    else:
        d.rounded_rectangle([c * SS for c in box], radius=radius * SS, fill=255)
    return mask.resize((SIZE, SIZE), Image.LANCZOS)


def clean_box(button, box):
    """Rows across the glyph box take the face colour interpolated between its edges."""
    px = button.load()
    x0, y0, x1, y1 = box
    for y in range(y0, y1):
        left, right = px[x0 - 1, y][:3], px[x1, y][:3]
        for x in range(x0, x1):
            t = (x - x0 + 1) / (x1 - x0 + 1)
            px[x, y] = tuple(round(a + (b - a) * t) for a, b in zip(left, right)) + (255,)


def clean_disc(button, centre, radius):
    """Elite knob: the dark centre disc filled with its own median colour."""
    px = button.load()
    cx, cy = centre
    ring = sorted(px[round(cx + radius * 0.92 * c), round(cy + radius * 0.92 * s)][:3]
                  for c, s in ((1, 0), (-1, 0), (0, 1), (0, -1), (0.7, 0.7), (-0.7, 0.7), (0.7, -0.7), (-0.7, -0.7)))
    colour = ring[len(ring) // 2]
    ImageDraw.Draw(button).ellipse([cx - radius, cy - radius, cx + radius, cy + radius], fill=colour + (255,))


def skin_glyph_layer(shapes, centre, colours, scale=1.0):
    moved = []
    for kind, coords in shapes:  # scale about the dlc3 glyph centre, then move to this skin's
        moved.append((kind, tuple((c - (32 if i % 2 == 0 else 26)) * scale + centre[i % 2] for i, c in enumerate(coords))))
    mask = Image.new("L", (SIZE * SS, SIZE * SS), 0)
    draw = ImageDraw.Draw(mask)
    for kind, coords in moved:
        pts = [c * SS for c in coords]
        if kind == "rect":
            draw.rounded_rectangle(pts, radius=SS, fill=255)
        else:
            draw.polygon(list(zip(pts[0::2], pts[1::2])), fill=255)
    mask = mask.resize((SIZE, SIZE), Image.LANCZOS)
    fill = Image.new("RGBA", (SIZE, SIZE))
    top, bottom = colours
    y0, y1 = centre[1] - 13, centre[1] + 13
    for y in range(SIZE):
        t = min(max((y - y0) / (y1 - y0), 0), 1)
        colour = tuple(round(a + (b - a) * t) for a, b in zip(top, bottom))
        for x in range(SIZE):
            fill.putpixel((x, y), colour + (255,))
    fill.putalpha(mask)
    return fill


def draw_skins():
    sheet = Image.new("RGBA", (SIZE * len(glyphs()) + 10 * (len(glyphs()) + 1), (SIZE + 10) * len(SKINS) + 10), (70, 58, 40, 255))
    for row, (skin, spec) in enumerate(SKINS.items()):
        if spec.get("art"):
            base = knob_from_art(*spec["art"], spec["pointer"])
        else:
            base = Image.open(os.path.join(ROOT, "textures", "skins", f"grid_{skin}.png")).convert("RGBA")
        if spec.get("art"):
            pass
        elif spec.get("flat"):  # flat face: one sampled colour
            colour = base.getpixel(spec["flat"])[:3] + (255,)
            ImageDraw.Draw(base).rectangle([spec["glyph"][0], spec["glyph"][1], spec["glyph"][2] - 1, spec["glyph"][3] - 1], fill=colour)
        elif spec["glyph"]:
            clean_box(base, spec["glyph"])
        else:
            clean_disc(base, *spec["disc"])
        if not spec.get("art"):
            base.putalpha(skin_mask(spec["mask"]))
        out = os.path.join(SKIN_OUT, skin)
        os.makedirs(out, exist_ok=True)
        for col, (name, shapes) in enumerate(glyphs().items()):
            button = Image.alpha_composite(base, skin_glyph_layer(shapes, spec["centre"], spec["colour"], spec.get("scale", 1.0)))
            for state, image in states(button).items():
                image.save(os.path.join(out, f"{name}-{state}.dds"), "DDS")
            sheet.paste(button, (10 + col * (SIZE + 10), 10 + row * (SIZE + 10)), button)
    sheet.resize((sheet.width * 2, sheet.height * 2), Image.NEAREST).save(os.path.join(ROOT, "textures", "preview_skins.png"))
    print(f"wrote {SKIN_OUT}")


if __name__ == "__main__":
    main()
