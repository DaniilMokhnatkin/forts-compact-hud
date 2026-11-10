"""Compact HUD: smaller build icons and a lower bottom panel.

Built from original/hud.lua, so it can be re-run after a game update:
    py -3 layouts\\compact.py               -> writes hud.lua
    py -3 layouts\\compact.py --theme zors  -> writes hud_zors.lua (Zors.o purple)
"""
import fnmatch
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
# Colour themes: plates behind the readouts, the "/" and the three numbers.
THEMES = {
    "grib": {"plate": [255, 255, 255, 255], "plate_texture": "ui/textures/HUD/custom/grib_plate.dds", "tab_text_centred": True,
             "slash": [245, 226, 186, 220], "amount": [255, 246, 222, 255],
             "cap": [225, 213, 185, 255], "rate": [120, 230, 130, 255]},
    "default": {"plate": [0, 0, 0, 150], "slash": [255, 255, 255, 160],
                "amount": [255, 255, 255, 255], "cap": [165, 172, 190, 255], "rate": [120, 230, 130, 255]},
    # Zors.o: the generated nebula plate (installed as HUD/custom/zors_plate.dds), lavender
    # cap and slash; the rate stays green.
    "zors": {"plate": [255, 255, 255, 255], "plate_texture": "ui/textures/HUD/custom/zors_plate.dds", "tab_text_centred": True,
             "slash": [214, 170, 255, 190],
             "amount": [255, 255, 255, 255], "cap": [200, 165, 245, 255], "rate": [120, 230, 130, 255]},
}
THEME_NAME = sys.argv[sys.argv.index("--theme") + 1] if "--theme" in sys.argv else "default"
THEME = THEMES[THEME_NAME]
ROOT = os.path.dirname(HERE)
sys.path.insert(0, os.path.join(ROOT, "tools"))
import forts_ui as ui  # noqa: E402

ICON = 0.8          # build icon scale (vanilla 53x71)
ITEMS_H = 71        # vanilla height of the item strip
DROP = round(ITEMS_H * (1 - ICON))  # how much lower the whole panel sits

globals_, f32 = ui.load(os.path.join(ROOT, "original", "hud.lua"), ui.DEFAULT_GAME)
paths = ui.unique_paths(ui.find_root(globals_))


def matches(path, pattern):
    """Glob per path segment: * never crosses a /."""
    parts, wanted = path.split("/"), pattern.split("/")
    return len(parts) == len(wanted) and all(fnmatch.fnmatchcase(p, w) for p, w in zip(parts, wanted))


def controls(pattern):
    found = [ctl for path, (_, _, ctl) in paths.items() if matches(path, pattern)]
    if not found:
        sys.exit(f"no control matches {pattern}")
    return found


def scale(ctl, sx, sy, keys=("Position", "Size")):
    for key in keys:
        v = ctl.get(key)
        if isinstance(v, ui.Table):
            x, y = v.array()[:2]
            ctl.set(key, [round(x * sx, 2), round(y * sy, 2)])


def nudge(ctl, dx=0, dy=0):
    x, y = ctl.get("Position").array()[:2]
    ctl.set("Position", [x + dx, y + dy])


def height(ctl, h):
    w = ctl.get("Size").array()[0]
    ctl.set("Size", [w, h])


PANEL = "root/HUD/HUDPanel"
ITEMS = PANEL + "/HUDItems"
PAGES = ITEMS + "/TabbedItemPanel/Panel"

# Build icons and everything drawn on them.
ITEM = PAGES + "/*/*ListBox/SlidingPanel/PrototypeListItem"
for button in controls(ITEM + "/Button"):
    scale(button, ICON, ICON)
for part in controls(ITEM + "/Button/*") + controls(ITEM + "/Lock") + controls(ITEM + "/Alert"):
    scale(part, ICON, ICON, keys=("Position",))

# Shorter item strip and pages.
for ctl in controls(PAGES + "/*") + controls(PAGES + "/*/*ListBox") + controls(PAGES + "/*/*ListBox/SlidingPanel"):
    height(ctl, round(ctl.get("Size").array()[1] - DROP, 2))
for path in (ITEMS, ITEMS + "/TabbedItemPanel", PAGES):
    ctl = controls(path)[0]
    height(ctl, round(ctl.get("Size").array()[1] - DROP, 2))

# Tabs follow the shorter strip; the rest of the panel moves down.
nudge(controls(ITEMS + "/TabbedItemPanel/Header")[0], dy=-DROP)
# ResourcePanel stays at vanilla height: its readout frames are baked into the
# panel texture, so moving the readouts inside it misaligns them.
for name in ("PanelLeft", "PanelCenter", "PanelCenterL", "PanelCenterR",
             "ItemDetail", "ItemsScrollLeft", "HUDItems"):
    nudge(controls(f"{PANEL}/{name}")[0], dy=DROP)

# Left panel: no backing plate. Keep the original weapon-facing controls and
# their parent paths so the engine can still update and handle them.
plate = [node for path, (_, node, _) in paths.items() if path == PANEL + "/PanelLeft"][0]
for key in ("Colour", "ColourLower"):
    plate.set(key, [255, 255, 255, 0])
DETAIL = PANEL + "/ItemDetail"
nudge(controls(DETAIL)[0], dy=400)

# Resources read as "453 / 900": the cap is as prominent as the amount, so a
# full store is obvious at a glance. The panel plate (with frames baked into
# it) goes transparent; each readout keeps its own box sprite.
RES = PANEL + "/ResourcePanel"


def node_at(path):
    return [node for p, (_, node, _) in paths.items() if p == path][0]


def hide(node):
    for key in ("Colour", "ColourLower"):
        node.set(key, [255, 255, 255, 0])


hide(node_at(RES))
READOUT_W, BOX_H = 142, 36
for res in ("Metal", "Energy"):
    box = f"{RES}/Readout{res}"
    # The game shows at most five digits. Measured at size 11 a digit is ~9 units,
    # at size 13 ~11.5 in game. Amount right-aligned to 65, "/" at 68..72, cap
    # left-aligned from 75; five digits on both sides keep padding. The box is 36
    # high: numbers sit low (centre 20), the rate on its own line at the top.
    controls(box)[0].set("Size", [READOUT_W, BOX_H])
    controls(f"{box}/{res}")[0].set("Position", [65, 20])
    cap = node_at(f"{box}/{res}Cap")
    cap.set("Size", 11)
    cap.get("Control").set("Anchor", 2)  # left-centre: the cap starts right after the slash
    cap.get("Control").set("Position", [75, 20])
    cap.get("Control").set("Size", [46, 11])
    slash = node_at(f"{box}/{res}Cap/{res}MaxLabel")  # reused as the "/" between the numbers
    slash.set("Text", "/")
    slash.set("Size", 11)
    slash.set("Font", "readout")
    slash.set("Colour", THEME["slash"])
    slash.set("ColourLower", THEME["slash"])
    slash.get("Control").set("Position", [-7, -5.5])  # relative to the cap's anchor (75)
    slash.get("Control").set("Size", [6, 11])
    rate = controls(f"{box}/{res}Rate")[0]
    rate.set("Position", [READOUT_W - 4, 6])
    rate.set("Size", [48, 8.5])  # vanilla 29 clipped the "+" of four-digit rates
    node_at(f"{box}/{res}Rate").set("Size", 8.5)  # font, vanilla 7
    hide(node_at(f"{box}/{res}Rate/{res}RateLabel"))

    # Fixed colours: amount bright white, cap muted grey, rate green.
    for name, colour in ((res, THEME["amount"]), (f"{res}Cap", THEME["cap"]), (f"{res}Rate", THEME["rate"])):
        text = node_at(f"{box}/{name}")
        text.set("Colour", colour)
        text.set("ColourLower", colour)

# Readouts move from the corner to the middle, just above the build icons, on a
# dark translucent plate. The engine pins ResourcePanel to the right edge, so the
# position comes from a layout script on screen width (Position is the 16:9 value).
READOUT_Y = 462  # 36-high boxes end at 498, just above the build icons
PLATE = THEME["plate"]  # for the default, tinted plates and corner icons were tried and rejected
for i, res in enumerate(("Metal", "Energy")):
    box = node_at(f"{RES}/Readout{res}")
    box.set("Texture", THEME.get("plate_texture", "ui/textures/FE-PanelTiny.dds"))
    box.set("Colour", PLATE)
    box.set("ColourLower", PLATE)
    # Measured in game at 16:9: the readouts' parent (ResourcePanel) starts at
    # x = 875. Layout scripts work here but ScreenW is in other units, so plain
    # positions are used; other aspect ratios need a new number.
    left = 1066.66 / 2 - (READOUT_W + 4 if i == 0 else -4)
    box.get("Control").set("Position", [round(left - 875, 2), READOUT_Y - 485])
hide(node_at(RES + "/Label"))

# Centre: no red frame and no purple item backdrop, only icons and tabs.
for name in ("PanelCenter", "PanelCenterL", "PanelCenterR"):
    hide(node_at(f"{PANEL}/{name}"))
for path in paths:
    if matches(path, PAGES + "/TabPanel*"):
        hide(node_at(path))

# Without the backing the strip sat too low: tabs touched the screen edge and the
# cost text under the icons was clipped. Lift it and give the lists 4 more units.
nudge(controls(ITEMS)[0], dy=-4)
for ctl in controls(PAGES + "/*/*ListBox") + controls(PAGES + "/*/*ListBox/SlidingPanel"):
    height(ctl, ctl.get("Size").array()[1] + 4)

# Scroll arrows floated far from the centred icons.
nudge(controls(PANEL + "/ItemsScrollLeft")[0], dy=400)
nudge(controls(RES + "/ItemsScrollRight")[0], dy=400)

# Bottom-right row: Grid, Autodoor, AutoCrossBrace (taken out of the pop-up
# assistant panel so they are always visible), Pause, Menu. Expand, Pin and the
# assistant toggle go off screen.
def children(ctl):
    return ctl.get("Children")


def move_child(src, dst, name):
    src_list, dst_list = children(src), children(dst)
    entry = next(e for e in src_list.entries if e[3] and e[2].get("Name") == name)
    src_list.entries.remove(entry)
    for i, (start, _, value, is_array) in enumerate(sorted(src_list.entries, key=lambda e: e[0])):
        src_list.entries[src_list.entries.index((start, _, value, is_array))] = (start, i + 1, value, True)
    dst_list.entries.append((float("inf"), len(dst_list.array()) + 1, entry[2], True))
    return entry[2]


resource_ctl = controls(RES)[0]
assistant_ctl = controls(RES + "/AssistantSettingsPanel")[0]
# One level row, lifted clear of the screen edge. (In the dlc3 skin the Pause and
# Menu art has a dark top rim; that is the texture, not clipping.)
ROW_Y, ROW_X0, STEP = 84, 39, 30
facing = move_child(controls(DETAIL)[0], assistant_ctl, "WeaponLeft")
facing_ctl = ui.control_part(facing)
ui.control_part(move_child(facing_ctl, controls(DETAIL)[0], "Help")).set("Position", [0, 0])
# Open in a row above the toggle. On the same row, left of it, the tabs covered
# them: with the DLC ammo tab the centred tab row reaches x ~842 (at 16:9), the
# buttons started at 794.
assistant_ctl.set("Position", [ROW_X0 - STEP, ROW_Y - STEP])
assistant_ctl.set("Size", [84, 24])
hide(node_at(RES + "/AssistantSettingsPanel"))
controls(RES + "/AssistantSettingsToggle")[0].set("Position", [ROW_X0 - STEP, ROW_Y])
facing_ctl.set("Position", [0, 0])
for i, name in enumerate(("Grid", "Autodoor", "AutoCrossBrace")):
    button = move_child(assistant_ctl, resource_ctl, name)
    ui.control_part(button).set("Position", [ROW_X0 + STEP * i, ROW_Y])
controls(RES + "/Pause")[0].set("Position", [ROW_X0 + STEP * 3, ROW_Y])
controls(RES + "/Menu")[0].set("Position", [ROW_X0 + STEP * 4, ROW_Y])

# Tab captions: the game lifts a released tab's caption by 4 units to suit its tab art
# (raised face, pressed face lower). A theme with symmetric tabs keeps them centred.
if THEME.get("tab_text_centred"):
    for path, (_, _, ctl) in paths.items():
        if path.endswith("/NewText") and "ParentPressed" in str(ctl.get("ScriptY")):
            ctl.set("ScriptY", "ParentMiddleY")

# Costs on the build cards: the bolder resource font, slightly smaller than the
# vanilla size 8 so four digits still fit the narrower card.
for cost in [node_at(p) for p in paths if matches(p, ITEM + "/Button/*Cost")]:
    cost.set("Size", 7)
    cost.set("Font", "readout")
for name in ("ExpandHUDToggle", "Pin"):
    nudge(controls(f"{RES}/{name}")[0], dy=400)

# Timer, "Paused" and the resume button sat on the centre panel's top edge, now
# behind the readouts: lift them 40 units, above the readouts.
LIFT = 40
for name in ("Timer", "PauseText", "ResumeButton"):
    ctl = controls(f"{PANEL}/PanelCenter/{name}")[0]
    nudge(ctl, dy=-LIFT)
    script = ctl.get("ScriptY")  # e.g. "ParentT - 10"
    offset = float(script.split("-")[1])
    ctl.set("ScriptY", f"ParentT - {offset + LIFT:g}")

# Tooltip box (commander ability, item descriptions) pinned to the left edge,
# just above the commander portrait.
tips = controls(ITEMS + "/ToolTips")[0]
tips.set("Position", [8 - 205, tips.get("Position").array()[1] - 35])  # clears tall commander art

# Match timer to the top-right corner. The engine decides when it is shown (it
# never appeared in sandbox). PanelCenter is stretched by the engine, its right
# edge ~ResourcePanel's left (875 at 16:9); ParentR/ParentT layout scripts work.
timer = controls(f"{PANEL}/PanelCenter/Timer")[0]
timer.set("ScriptX", "ParentR + 165")
timer.set("ScriptY", "ParentT - 478")
timer.set("Position", [875 + 165 - 215, -478])
# "Paused" was placed right of the timer (PrevSiblingRight); keep it centred
# above the readouts instead. The resume button still follows it.
controls(f"{PANEL}/PanelCenter/PauseText")[0].set("ScriptX", "ParentMiddleX - 30")

# Spy panel (enemy resources, also shown with the Phantom commander) down to just
# above the weapon-facing row, which opens above the bottom-right buttons; the
# enemy commander gauge is its child and moves with it. At +56 its bottom edge
# sat 7 units above the buttons and covered the facing row (measured at 1440p).
nudge(controls(ITEMS + "/ResourcePanelEnemy")[0], dy=56 - STEP)

# Hidden HUD (observer, loading, replays): the engine slides the HUD down by
# ScreenH minus the top of the panel plates (measured: 101 with plates at 499).
# The readouts sit above the plates, so they stayed on screen. The plates are
# invisible now, so raise them to 455: the slide becomes 145 and takes the
# readouts off screen. PanelCenter's children keep their place on screen.
PLATE_TOP = 455
for name in ("PanelLeft", "PanelCenter", "PanelCenterL", "PanelCenterR"):
    ctl = controls(f"{PANEL}/{name}")[0]
    shift = ctl.get("Position").array()[1] - PLATE_TOP
    ctl.set("Position", [ctl.get("Position").array()[0], PLATE_TOP])
for name in ("Timer", "PauseText", "ResumeButton"):
    ctl = controls(f"{PANEL}/PanelCenter/{name}")[0]
    nudge(ctl, dy=shift)
    offset = float(ctl.get("ScriptY").split("-")[1]) - shift
    ctl.set("ScriptY", f"ParentT - {offset:g}" if offset >= 0 else f"ParentT + {-offset:g}")

# Commander and reactor bars sat on the left panel; drop them into the corner.
for name in ("COBackground", "ReactorBars"):
    nudge(controls(f"{ITEMS}/{name}")[0], dy=88)

OUT_NAME = "hud.lua" if THEME_NAME == "default" else f"hud_{THEME_NAME}.lua"
with open(os.path.join(ROOT, OUT_NAME), "w", encoding="latin-1", newline="\n") as fh:
    fh.write(ui.to_source(globals_, f32))
print(f"{OUT_NAME} written ({THEME_NAME}): icons x{ICON}, panel lowered by {DROP}")
