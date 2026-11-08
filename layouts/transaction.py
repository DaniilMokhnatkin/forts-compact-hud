"""Bigger floating build costs ("-22 / -108" near the cursor after building).

Built from original/transaction.lua (a text layout in the game); writes transaction.lua.

    py -3 layouts\\transaction.py
"""
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
sys.path.insert(0, os.path.join(ROOT, "tools"))
import forts_ui as ui  # noqa: E402

SCALE = 1.0          # vanilla text and icons are 16 units; 1.25 was tried and felt too big
# The engine spawns the popup at the old resource panel (~x 899, y 424 at 16:9,
# measured in game). Shift it to the centre, just above the moved readouts.
OFFSET = (-401, -25)

globals_, f32 = ui.load(os.path.join(ROOT, "original", "transaction.lua"), ui.DEFAULT_GAME)
for path, (_, node, ctl) in ui.unique_paths(ui.find_root(globals_)).items():
    if node.get("Type") == "Text":
        node.set("Size", round(node.get("Size") * SCALE, 2))
    if ctl is not node:
        x, y = ctl.get("Position").array()
        w, h = ctl.get("Size").array()
        ctl.set("Position", [round(x * SCALE + OFFSET[0], 2), round(y * SCALE + OFFSET[1], 2)])
        ctl.set("Size", [round(w * SCALE, 2), round(h * SCALE, 2)])

with open(os.path.join(ROOT, "transaction.lua"), "w", encoding="latin-1", newline="\n") as fh:
    fh.write(ui.to_source(globals_, f32))
print(f"transaction.lua written: floating costs x{SCALE}, offset {OFFSET}")
