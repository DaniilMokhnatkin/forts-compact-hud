"""Draw Pause / Play / Menu HUD buttons in the style of the dlc3 skin's Grid button.

Source: textures/grid_button_dlc3.png, the Grid button captured in game at 1440p
(the skin's own textures are packed). Its glyph is painted over with the face
colour and a new glyph is drawn. Output: textures/HUD/custom/<Name>-<State>.dds,
64x64 RGBA, states A (normal), R (rollover), S (pressed), D (disabled), plus a
preview sheet textures/preview.png.

    py -3 tools\\draw_buttons.py
"""
import os

from PIL import Image, ImageDraw, ImageEnhance, ImageOps, DdsImagePlugin  # noqa: F401 (registers DDS)

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


if __name__ == "__main__":
    main()
