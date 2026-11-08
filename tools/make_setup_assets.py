"""Preview images for the installer window.

    py -3 tools\\make_setup_assets.py <project root> <assets dir>

crosshair_<id>.png  128x128, each crosshair on transparent
crosshair_vanilla.png  the game's own round reticle
scene.png  a battlefield crop the crosshair preview is drawn over
hud_after.png  the compact HUD (bottom of a 16:9 screenshot)
"""
import os
import sys

from PIL import Image, DdsImagePlugin  # noqa: F401 (registers DDS)

root, assets = sys.argv[1], sys.argv[2]
os.makedirs(assets, exist_ok=True)
tex = os.path.join(root, "textures")

for name in os.listdir(os.path.join(tex, "crosshairs")):
    folder = os.path.join(tex, "crosshairs", name)
    if os.path.isdir(folder):
        im = Image.open(os.path.join(folder, "mouse_target.dds")).convert("RGBA")
        resample = Image.NEAREST if name == "cross" else Image.LANCZOS
        im.resize((128, 128), resample).save(os.path.join(assets, f"crosshair_{name}.png"))

Image.open(os.path.join(tex, "vanilla", "mouse_target.dds")).convert("RGBA") \
    .resize((128, 128), Image.NEAREST).save(os.path.join(assets, "crosshair_vanilla.png"))

shot = Image.open(os.path.join(root, "textures", "preview_source.png")).convert("RGB")
w, h = shot.size
# HUD: the bottom quarter of the frame, centred on the build bar.
hud = shot.crop((int(w * 0.02), int(h * 0.74), int(w * 0.98), h))
hud.thumbnail((960, 400), Image.LANCZOS)
hud.save(os.path.join(assets, "hud_after.png"))
# Scene behind the crosshair preview: calm dark hills, where a white cursor reads.
scene = shot.crop((int(w * 0.45), int(h * 0.50), int(w * 0.75), int(h * 0.74)))
scene.resize((480, 220), Image.LANCZOS).save(os.path.join(assets, "scene.png"))
print("assets written to", assets)
