"""Zors.o HUD from the generated art (third_party/zors/generated): every piece is fitted onto
the area the game's own texture uses, so it lands where the original was.

    py -3 tools\\build_zors_hud.py <game folder> <out folder>

<out>/textures/...           mirrors data/ui/textures (the Zors.o layout copies it in):
  HUD/skin_base/commander/   frame, gauge, ability button (4 states), star
  HUD/skin_base/             reactor bar, gauge and button, spy panel, tabs,
                             Grid / AutoDoors / CrossBrace buttons (4 states)
  HUD/custom/                our pause / play / menu / weapon-facing buttons
  HUD/custom/zors_plate.dds  the plate behind the resource numbers
  *.dds                      the seven world icons (+ grey ones) and the crosshair
Skin_base pieces show with the game's "Default" HUD skin only (DLC skins use their packs).
"""
import os
import sys

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageOps

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
sys.path.insert(0, HERE)
import draw_buttons as db  # noqa: E402  (glyphs, glyph layers, button states)
from build_layouts import open_image  # noqa: E402

GEN = os.path.join(ROOT, "third_party", "zors", "generated")
SHEET = "cosmic-purple-ui-sprite-sheet.png"
# Pieces on the generated sheet (x0, y0, x1, y1), found from its alpha.
PIECES = {
    "commander_frame": (40, 44, 244, 248), "commander_gauge": (480, 68, 640, 228),
    "commander_button": (884, 52, 1076, 244), "commander_star": (1260, 16, 1536, 272),
    "reactor_bar": (12, 324, 356, 376), "reactor_gauge": (428, 328, 824, 376),
    "reactor_button": (848, 324, 1116, 464), "spy_panel": (428, 636, 776, 844),
}
ICONS = {  # world icons on the icon sheet (4096 x 2048)
    "extinguisher": (24, 24, 1000, 1000), "under_construction": (1052, 24, 2024, 1000),
    "upgrade": (2072, 24, 3048, 1000), "repair": (3100, 24, 4072, 1000),
    "reload": (24, 1052, 1000, 2024), "reload_banked_ammo": (1052, 1052, 2024, 2024),
    "scrap": (2072, 1048, 3048, 2024),
}
GLYPH = ((240, 232, 255), (212, 196, 246))  # light lavender, as the icons' glyphs


def clean(im):
    """Drop the generator's faint halo pixels."""
    a = im.getchannel("A").point(lambda v: 0 if v < 24 else v)
    im.putalpha(a)
    return im.crop(a.getbbox())


ORIG_DIRS = []  # the game's skin_base: backup first (a theme may have replaced files), then the game


def orig(rel):
    for d in ORIG_DIRS:
        p = os.path.join(d, rel)
        if os.path.exists(p):
            return p
    raise SystemExit("missing " + rel)


def fit(piece, rel):
    """`piece` stretched onto the opaque area of the game's texture `rel`, same canvas size."""
    game_tex = open_image(orig(rel))
    box = game_tex.getchannel("A").point(lambda v: 255 if v > 24 else 0).getbbox()
    out = Image.new("RGBA", game_tex.size, (0, 0, 0, 0))
    w, h = box[2] - box[0], box[3] - box[1]
    out.alpha_composite(piece.resize((w, h), Image.LANCZOS), (box[0], box[1]))
    return out


def states(normal, selected=None):
    s = db.states(normal, selected)
    return s


def save(img, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path, "DDS")


def button_base(name, size=64):
    """The generated square button fitted into the rim area the other skins use (3..61)."""
    im = clean(Image.open(os.path.join(GEN, name)).convert("RGBA"))
    big = Image.new("RGBA", (size * db.SS, size * db.SS))
    inner = round(58 / 64 * size * db.SS)
    big.alpha_composite(im.resize((inner, inner), Image.LANCZOS), ((big.width - inner) // 2,) * 2)
    return big.resize((size, size), Image.LANCZOS)


def extra_glyphs():
    """Grid, auto doors and cross bracing, in the dlc3 glyph frame (centre 32, 26)."""
    return {
        "Grid": [("rect", (20, 14, 30, 24)), ("rect", (34, 14, 44, 24)), ("rect", (20, 28, 30, 38)), ("rect", (34, 28, 44, 38))],
        "AutoDoors": [("poly", (22, 13, 38, 16, 38, 40, 22, 37)), ("rect", (40, 13, 44, 39))],
        "CrossBrace": [("poly", (20, 16, 24, 13, 44, 33, 40, 36)), ("poly", (40, 13, 44, 16, 24, 36, 20, 33)),
                       ("rect", (18, 12, 24, 18)), ("rect", (40, 12, 46, 18)), ("rect", (18, 34, 24, 40)), ("rect", (40, 34, 46, 40))],
    }


def glyph_glow(layer, colour=(200, 120, 255), radius=2.4, strength=1.3):
    glow = Image.new("RGBA", layer.size, colour + (0,))
    glow.putalpha(layer.getchannel("A").filter(ImageFilter.GaussianBlur(radius)).point(lambda v: min(255, int(v * strength))))
    return glow


def buttons(out, skin_base):
    normal = button_base("cosmic-purple-button-1024.png")
    on = button_base("cosmic-purple-button-on-1024.png")
    sets = [(os.path.join(out, "HUD", "custom"), db.glyphs()), (skin_base, extra_glyphs())]
    for folder, glyph_set in sets:
        for name, shapes in glyph_set.items():
            layer = db.skin_glyph_layer(shapes, (32, 32), GLYPH, 0.95)
            a = Image.alpha_composite(normal, layer)
            s = Image.alpha_composite(Image.alpha_composite(on, glyph_glow(layer)), layer)
            for state, img in states(a, s).items():
                save(img, os.path.join(folder, f"{name}-{state}.dds"))


def tabs(skin_base):
    tab = clean(Image.open(os.path.join(GEN, "cosmic-purple-tab-1536x320.png")).convert("RGBA"))
    a = fit(tab, os.path.join("tabs", "Button-Long-A.dds"))
    sel = ImageEnhance.Brightness(ImageEnhance.Color(a).enhance(1.35)).enhance(1.45)
    sel.putalpha(a.getchannel("A"))
    for state, img in states(a, sel).items():
        save(img, os.path.join(skin_base, "tabs", f"Button-Long-{state}.dds"))


def sheet_pieces(skin_base):
    sheet = Image.open(os.path.join(GEN, SHEET)).convert("RGBA")
    piece = lambda k: clean(sheet.crop(PIECES[k]))
    simple = {
        "commander_frame": "commander/COGauge_BG.dds", "commander_gauge": "commander/COGauge_Radial.dds",
        "commander_star": "commander/COGauge_Star.dds", "reactor_bar": "ReactorBG.dds",
        "reactor_gauge": "ReactorGauge.dds", "spy_panel": "PanelSpy.dds",
    }
    for key, rel in simple.items():
        save(fit(piece(key), rel), os.path.join(skin_base, rel))
    for key, rel in (("commander_button", "commander/COGauge_Button"), ("reactor_button", "Reactor"),
                     ("commander_button", "commander/COGauge_Button_enemy")):
        a = fit(piece(key), rel + "-A.dds")
        for state, img in states(a).items():
            save(img, os.path.join(skin_base, f"{rel}-{state}.dds"))
    # The enemy commander (spy panel) and the "ability ready" highlight in the same style.
    for key, rel in (("commander_frame", "commander/COGauge_BG_enemy.dds"), ("commander_gauge", "commander/COGauge_Radial_enemy.dds")):
        save(fit(piece(key), rel), os.path.join(skin_base, rel))
    for rel in ("commander/COGauge_BG_HL.dds", "commander/COGauge_BG_HL_enemy.dds"):
        lit = fit(piece("commander_frame"), rel)
        bright = ImageEnhance.Brightness(ImageEnhance.Color(lit).enhance(1.4)).enhance(1.5)
        bright.putalpha(lit.getchannel("A"))
        save(Image.alpha_composite(glyph_glow(lit, (220, 120, 255), 6, 1.0), bright), os.path.join(skin_base, rel))
    # The spy panel's magnifier button.
    spy = clean(Image.open(os.path.join(GEN, "cosmic-purple-spy-button-1024.png")).convert("RGBA"))
    for state, img in states(fit(spy, "HUD-Spy-A.dds")).items():
        save(img, os.path.join(skin_base, f"HUD-Spy-{state}.dds"))


def icons(out):
    sheet = Image.open(os.path.join(GEN, "cosmic-purple-action-icons-sheet.png")).convert("RGBA")
    for name, box in ICONS.items():
        icon = clean(sheet.crop(box)).resize((126, 126), Image.LANCZOS)
        img = Image.new("RGBA", (128, 128))
        img.alpha_composite(icon, (1, 1))
        save(img, os.path.join(out, name + ".dds"))
        if name in ("under_construction", "repair"):  # the disabled look
            grey = ImageEnhance.Brightness(ImageOps.grayscale(img.convert("RGB")).convert("RGB")).enhance(0.75).convert("RGBA")
            grey.putalpha(img.getchannel("A"))
            save(grey, os.path.join(out, name + "_grey.dds"))


def crosshair(out):
    im = clean(Image.open(os.path.join(GEN, "cosmic-purple-crosshair-thick-1024.png")).convert("RGBA"))
    side = max(im.size)
    sq = Image.new("RGBA", (side, side))
    sq.alpha_composite(im, ((side - im.width) // 2, (side - im.height) // 2))
    img = sq.resize((62, 62), Image.LANCZOS)
    tex = Image.new("RGBA", (64, 64))
    tex.alpha_composite(img, (1, 1))
    for name in ("mouse_target", "mouse_target_painted"):
        save(tex, os.path.join(out, name + ".dds"))


def plate(out):
    im = clean(Image.open(os.path.join(GEN, "cosmic-purple-number-plate-1600x400.png")).convert("RGBA"))
    save(im.resize((256, 64), Image.LANCZOS), os.path.join(out, "HUD", "custom", "zors_plate.dds"))


def main(game, out):
    textures = os.path.join(out, "textures")
    game_skin_base = os.path.join(game, "data", "ui", "textures", "HUD", "skin_base")
    backup_skin_base = os.path.join(game, "Forts-Setup-Backup", "data", "ui", "textures", "HUD", "skin_base")
    skin_base = os.path.join(textures, "HUD", "skin_base")
    ORIG_DIRS[:] = [backup_skin_base, game_skin_base]
    buttons(textures, skin_base)
    tabs(skin_base)
    sheet_pieces(skin_base)
    icons(textures)
    crosshair(textures)
    plate(textures)
    print("zors hud written to", out)


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
