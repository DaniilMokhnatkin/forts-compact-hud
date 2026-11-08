"""Point the Pause / Play / Menu HUD sprites at the redrawn buttons.

Built from original/uisprites.lua; writes uisprites.lua. Textures come from
tools/draw_buttons.py (textures/HUD/custom), installed to data/ui/textures/HUD/custom.

    py -3 layouts\\sprites.py
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
sys.path.insert(0, os.path.join(ROOT, "tools"))
import forts_ui as ui  # noqa: E402

REDRAWN = {"hud-pause": "HUD/custom/Pause", "hud-play": "HUD/custom/Play", "hud-menu": "HUD/custom/Menu"}

globals_, f32 = ui.load(os.path.join(ROOT, "original", "uisprites.lua"), ui.DEFAULT_GAME)
sprites = next(value for name, value in globals_ if name == "Sprites")
done = set()
for _, _, call, _ in sprites.entries:
    if isinstance(call, ui.Call) and call.func == "ButtonSprite" and call.args[0] in REDRAWN:
        call.args[1] = REDRAWN[call.args[0]]
        done.add(call.args[0])
facing = {
    "hud-weapon-left": "FacingLeft",
    "hud-weapon-auto": "FacingAuto",
    "hud-weapon-right": "FacingRight",
    "hud-assistant": "FacingMenu",
}
for index, (start, key, value, array) in enumerate(sprites.entries):
    name = value.get("Name") if isinstance(value, ui.Table) else (
        value.args[0] if isinstance(value, ui.Call) and value.func == "ButtonSprite" else None
    )
    if name in facing:
        sprites.entries[index] = (start, key, ui.Call("ButtonSprite", [name, "HUD/custom/" + facing[name]]), array)
        done.add(name)
missing = (set(REDRAWN) | set(facing)) - done
if missing:
    sys.exit(f"sprites not found: {', '.join(sorted(missing))}")

with open(os.path.join(ROOT, "uisprites.lua"), "w", encoding="latin-1", newline="\n") as fh:
    fh.write(ui.to_source(globals_, f32))
print("uisprites.lua written: " + ", ".join(f"{k} -> {v}" for k, v in REDRAWN.items()))
