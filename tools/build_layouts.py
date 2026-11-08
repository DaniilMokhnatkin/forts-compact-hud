"""Payload for the HUD layouts other than the compact one, and the icon sources.

    py -3 tools\\build_layouts.py <game folder> <payload folder> <assets folder>

payload\\layouts\\zors\\        hud.lua (compact in purple, from build\\hud_zors.lua) + textures\\
payload\\layouts\\minimalist\\  hud.lua + textures\\   (nutellafish, third_party\\minimalist)
payload\\icons\\<layout>\\      extinguisher, under_construction(_grey), repair(_grey) as
                              uncompressed DDS: the installer scales their alpha (opacity slider)
assets\\layout_*.png, assets\\icon_<layout>_<name>.png   previews for the window
"""
import os
import shutil
import sys

from PIL import Image, DdsImagePlugin  # noqa: F401 (registers DDS)


ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ICONS = ["extinguisher", "under_construction", "under_construction_grey", "repair", "repair_grey"]


def copy_tree(src, dst):
    for d, _, files in os.walk(src):
        for f in files:
            s = os.path.join(d, f)
            t = os.path.join(dst, os.path.relpath(s, src))
            os.makedirs(os.path.dirname(t), exist_ok=True)
            shutil.copy2(s, t)


def vanilla_texture(game, name):
    """The game's own file: from our backup if the installer already replaced it."""
    for base in (os.path.join(game, "Forts-Setup-Backup"), game):
        p = os.path.join(base, "data", "ui", "textures", name)
        if os.path.exists(p):
            return p
    raise SystemExit(f"missing {name}")


def open_image(path):
    """Pillow, plus uncompressed BGRA in a DX10 header (DXGI 87 / 91), which it lacks."""
    try:
        return Image.open(path).convert("RGBA")
    except NotImplementedError:
        data = open(path, "rb").read()
        height, width = int.from_bytes(data[12:16], "little"), int.from_bytes(data[16:20], "little")
        fmt = int.from_bytes(data[128:132], "little")
        if data[84:88] != b"DX10" or fmt not in (87, 91):
            raise
        return Image.frombuffer("RGBA", (width, height), data[148:148 + width * height * 4], "raw", "BGRA", 0, 1)


def thumb(src, out, size=(320, 140)):
    im = open_image(src).convert("RGB")
    r = max(size[0] / im.width, size[1] / im.height)
    im = im.resize((round(im.width * r), round(im.height * r)), Image.LANCZOS)
    left, top = (im.width - size[0]) // 2, (im.height - size[1]) // 2
    im.crop((left, top, left + size[0], top + size[1])).save(out)


def main(game, payload, assets):
    tp = os.path.join(ROOT, "third_party")
    zors = os.path.join(payload, "layouts", "zors")
    mini = os.path.join(payload, "layouts", "minimalist")
    for d in (zors, mini, os.path.join(payload, "icons")):
        shutil.rmtree(d, ignore_errors=True)
    os.makedirs(zors)
    shutil.copy2(os.path.join(ROOT, "build", "hud_zors.lua"), os.path.join(zors, "hud.lua"))
    # Zors.o's files go where the game reads them: menu backgrounds into every menu
    # skin's folder (screens ask for menu_backgrounds/base, the menu skin redirects it),
    # the splash to data\ui (logo.lua shows ui/earthworkgames), the rest next to the
    # game's own in data\ui\textures.
    for f in os.listdir(os.path.join(tp, "zors", "textures")):
        src = os.path.join(tp, "zors", "textures", f)
        if f.startswith("FE-") and f.endswith("-BG.dds"):  # the installer copies base to every menu skin
            targets = [os.path.join(zors, "textures", "menu_backgrounds", "base", f)]
        elif f == "earthworkgames.png":
            targets = [os.path.join(zors, "ui", f)]
        else:
            targets = [os.path.join(zors, "textures", f)]
        for t in targets:
            os.makedirs(os.path.dirname(t), exist_ok=True)
            shutil.copy2(src, t)
    # The Zors.o HUD made from the generated art (tools/build_zors_hud.py -> build/zors_hud):
    # buttons, tabs, commander, reactor, spy panel, plate, all seven icons and the crosshair.
    # It replaces the archive's icons and cursor.
    copy_tree(os.path.join(ROOT, "build", "zors_hud", "textures"), os.path.join(zors, "textures"))
    os.makedirs(mini)
    shutil.copy2(os.path.join(tp, "minimalist", "screens", "hud.lua"), os.path.join(mini, "hud.lua"))
    copy_tree(os.path.join(tp, "minimalist", "textures"), os.path.join(mini, "textures"))

    sources = {
        "compact": lambda n: vanilla_texture(game, n + ".dds"),
        "zors": lambda n: os.path.join(zors, "textures", n + ".dds"),
        "minimalist": lambda n: os.path.join(mini, "textures", n + ".dds"),
    }
    for layout, find in sources.items():
        out = os.path.join(payload, "icons", layout)
        os.makedirs(out)
        for name in ICONS:
            src = find(name)
            if not os.path.exists(src):  # the theme keeps the game's icon
                src = vanilla_texture(game, name + ".dds")
            im = open_image(src)
            im.save(os.path.join(out, name + ".dds"), "DDS")
            if not name.endswith("_grey"):
                im.save(os.path.join(assets, f"icon_{layout}_{name}.png"))

    # Wide previews for the HUD page frame (the frame is about 6:1).
    thumb(os.path.join(tp, "zors", "textures", "FE-MainMenu-BG.dds"), os.path.join(assets, "preview_zors.png"), (960, 160))
    # (sources stay flat in third_party\zors\textures; see the mapping above)
    mini_shot = Image.open(os.path.join(tp, "minimalist", "preview.jpg")).convert("RGB")
    mini_shot.crop((0, 470, 1280, 720)).resize((960, 188), Image.LANCZOS).save(os.path.join(assets, "preview_minimalist.png"))
    thumb(os.path.join(assets, "hud_after.png"), os.path.join(assets, "layout_compact.png"))
    thumb(os.path.join(tp, "zors", "textures", "FE-MainMenu-BG.dds"), os.path.join(assets, "layout_zors.png"))
    thumb(os.path.join(tp, "minimalist", "preview.jpg"), os.path.join(assets, "layout_minimalist.png"))
    Image.open(os.path.join(tp, "minimalist", "preview.jpg")).convert("RGB").save(os.path.join(assets, "minimalist_preview.jpg"), quality=88)
    print("layouts and icons written")


if __name__ == "__main__":
    main(*sys.argv[1:4])
