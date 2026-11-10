"""Fit the approved ImageGen mushroom artwork to Forts' native texture bounds."""
from pathlib import Path
import shutil
import subprocess
import sys

from PIL import Image, ImageDraw, ImageEnhance, ImageOps
import forts_ui as ui
import draw_buttons as db
from build_layouts import open_image, vanilla_texture, ICONS as OPACITY_ICONS
from build_zors_hud import fit, clean, save, ORIG_DIRS, extra_glyphs

ROOT = Path(__file__).resolve().parents[1]
GEN = ROOT / 'textures/grib/generated'
CREAM = ((255, 238, 200), (235, 211, 169))


def piece(sheet, x, y, columns=2, rows=2):
    im = Image.open(GEN / sheet).convert('RGBA')
    box = (round(x * im.width / columns), round(y * im.height / rows),
           round((x + 1) * im.width / columns), round((y + 1) * im.height / rows))
    tile = im.crop(box)
    if tile.getchannel('A').point(lambda a: 255 if a >= 24 else 0).getbbox() is None:
        raise ValueError(f'Empty Grib sprite: {sheet} ({x}, {y})')
    return clean(tile)


def brighter(im, factor):
    out = ImageEnhance.Brightness(im).enhance(factor)
    out.putalpha(im.getchannel('A'))
    return out


def cursor_texture(mushroom):
    cursor = Image.new('RGBA', (64, 64))
    small = mushroom.copy()
    small.thumbnail((round(58 * .7), round(58 * .7)), Image.Resampling.LANCZOS)
    cursor.alpha_composite(small, ((64 - small.width) // 2, (64 - small.height) // 2))
    return cursor


def main(game=ui.DEFAULT_GAME):
    payload = ROOT / 'setup/payload'
    layout = payload / 'layouts/grib'
    out = layout / 'textures'
    skin = out / 'HUD/skin_base'
    custom = out / 'HUD/custom'
    assets = ROOT / 'setup/app/assets'
    ORIG_DIRS[:] = [str(Path(game) / 'Forts-Setup-Backup/data/ui/textures/HUD/skin_base'),
                   str(Path(game) / 'data/ui/textures/HUD/skin_base')]
    main_art = lambda x, y: piece('atlas-main.png', x, y)
    support = lambda x, y: piece('atlas-support.png', x, y)
    button = main_art(0, 0)
    normal = Image.new('RGBA', (64, 64))
    normal.alpha_composite(button.resize((58, 58), Image.Resampling.LANCZOS), (3, 3))
    selected = Image.new('RGBA', (64, 64))
    selected.alpha_composite(piece('atlas-selected.png', 0, 0).resize((58, 58), Image.Resampling.LANCZOS), (3, 3))
    glyph_sets = [(custom, db.glyphs()), (skin, extra_glyphs())]
    for folder, glyphs in glyph_sets:
        for name, shapes in glyphs.items():
            glyph = db.skin_glyph_layer(shapes, (32, 34), CREAM, 0.95)
            face = Image.alpha_composite(normal, glyph)
            for state, im in db.states(face, Image.alpha_composite(selected, glyph)).items():
                save(im, str(folder / f'{name}-{state}.dds'))
    tab = fit(main_art(1, 0), 'tabs/Button-Long-A.dds')
    for state, im in db.states(tab, fit(piece('atlas-selected.png', 1, 0), 'tabs/Button-Long-A.dds')).items():
        save(im, str(skin / 'tabs' / f'Button-Long-{state}.dds'))
    frame = main_art(1, 1)
    mushroom = clean(Image.open(GEN / 'mushroom-cursor.png').convert('RGBA'))
    for rel in ('commander/COGauge_BG.dds', 'commander/COGauge_BG_enemy.dds'):
        save(fit(frame, rel), str(skin / rel))
    for rel in ('commander/COGauge_BG_HL.dds', 'commander/COGauge_BG_HL_enemy.dds'):
        save(brighter(fit(frame, rel), 1.35), str(skin / rel))
    for rel in ('commander/COGauge_Button', 'commander/COGauge_Button_enemy'):
        face = fit(mushroom, rel + '-A.dds')
        for state, im in db.states(face).items():
            save(im, str(skin / f'{rel}-{state}.dds'))
    save(fit(mushroom, 'commander/COGauge_Star.dds'), str(skin / 'commander/COGauge_Star.dds'))
    save(fit(support(0, 1), 'ReactorBG.dds'), str(skin / 'ReactorBG.dds'))
    face = fit(support(1, 1), 'Reactor-A.dds')
    for state, im in db.states(face).items():
        save(im, str(skin / f'Reactor-{state}.dds'))
    # Keep native charge and signal colours, including their original alpha masks.
    for rel in ('ReactorGauge.dds', 'commander/COGauge_Radial.dds', 'commander/COGauge_Radial_enemy.dds'):
        save(open_image(vanilla_texture(game, 'HUD/skin_base/' + rel)), str(skin / rel))
    save(fit(support(1, 0), 'PanelSpy.dds'), str(skin / 'PanelSpy.dds'))
    # The magnifier is a vector functional glyph on the same generated button face.
    glyph = Image.new('RGBA', (256, 256)); draw = ImageDraw.Draw(glyph)
    draw.ellipse((76, 64, 140, 128), outline=CREAM[0] + (255,), width=12)
    draw.line((132, 120, 176, 164), fill=CREAM[0] + (255,), width=14)
    spy = Image.alpha_composite(normal, glyph.resize((64, 64), Image.Resampling.LANCZOS))
    for state, im in db.states(fit(spy, 'HUD-Spy-A.dds')).items():
        save(im, str(skin / f'HUD-Spy-{state}.dds'))
    save(main_art(0, 1).resize((512, 128), Image.Resampling.LANCZOS), str(custom / 'grib_plate.dds'))
    corner = support(0, 0)
    save(corner.resize((512, 256), Image.Resampling.LANCZOS), str(custom / 'grib_corner.dds'))
    corner.save(assets / 'grib_resource_corner.png')
    for name, (x, y) in {'extinguisher': (0, 0), 'under_construction': (1, 0),
                         'upgrade': (2, 0), 'repair': (3, 0), 'reload': (0, 1),
                         'reload_banked_ammo': (1, 1), 'scrap': (2, 1)}.items():
        icon = Image.new('RGBA', (128, 128))
        icon.alpha_composite(piece('action-icons-colours.png', x, y, 4, 2).resize((126, 126), Image.Resampling.LANCZOS), (1, 1))
        save(icon, str(out / f'{name}.dds'))
        if name in ('under_construction', 'repair'):
            grey = ImageOps.grayscale(icon).convert('RGBA'); grey.putalpha(icon.getchannel('A'))
            save(brighter(grey, 0.75), str(out / f'{name}_grey.dds'))
    cursor = cursor_texture(mushroom)
    cursor_dir = payload / 'crosshairs/grib'
    for name in ('mouse_target', 'mouse_target_painted'):
        save(cursor, str(cursor_dir / f'{name}.dds'))
    # Build scripts also use this source when rebuilding the complete payload.
    source_dir = ROOT / 'textures/crosshairs/grib'; source_dir.mkdir(parents=True, exist_ok=True)
    for name in ('mouse_target.dds', 'mouse_target_painted.dds'):
        shutil.copy2(cursor_dir / name, source_dir / name)
    mushroom.save(assets / 'theme_grib.png')
    gallery = assets / 'gallery/grib'; gallery.mkdir(parents=True, exist_ok=True)
    shutil.copy2(ROOT / 'docs/concepts/true-version-grib/concept.png', gallery / '01 Концепт оформления.png')
    icons = payload / 'icons/grib'; icons.mkdir(parents=True, exist_ok=True)
    for name in OPACITY_ICONS:
        shutil.copy2(out / f'{name}.dds', icons / f'{name}.dds')
        if not name.endswith('_grey'):
            open_image(str(out / f'{name}.dds')).save(assets / f'icon_grib_{name}.png')
    subprocess.run([sys.executable, str(ROOT / 'layouts/compact.py'), '--theme', 'grib'], cwd=ROOT, check=True)
    ui.compile_lua(str(ROOT / 'hud_grib.lua'), str(ROOT / 'build/hud_grib.lua'), game)
    # The default plain layout is replaced by the compiled corner variant below.
    shutil.copy2(ROOT / 'build/hud_grib.lua', layout / 'hud.lua')
    print('Grib: complete HUD textures, seven alerts, solid mushroom cursor and previews built')


if __name__ == '__main__':
    main(sys.argv[1] if len(sys.argv) > 1 else ui.DEFAULT_GAME)
