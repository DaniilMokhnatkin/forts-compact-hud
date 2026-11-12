"""Let construction clicks pass through HUD backings that draw nothing.

Forts treats visual alpha and mouse blocking separately. BlocksMouse belongs
inside Control (the stock enemy commander portrait uses the same property).
Apply this last, after theme/opacity variants are final, before packaging.
"""
from pathlib import Path
import sys
import shutil

import forts_ui as ui

ROOT = Path(__file__).resolve().parents[1]
PANEL = "root/HUD/HUDPanel/"


def draws_nothing(node):
    upper = node.get("Colour")
    if not isinstance(upper, ui.Table) or len(upper.array()) < 4 or upper.array()[3] != 0:
        return False
    lower = node.get("ColourLower")
    return lower is None or (isinstance(lower, ui.Table) and len(lower.array()) >= 4 and lower.array()[3] == 0)


def allow_transparent_backings(globals_):
    changed = 0
    for path, (_, node, control) in ui.unique_paths(ui.find_root(globals_)).items():
        if path.startswith(PANEL) and node.get("Type") == "Static" and draws_nothing(node):
            if control.get("BlocksMouse") is not False:
                control.set("BlocksMouse", False)
                changed += 1
    return changed


def build(payload, game=ui.DEFAULT_GAME):
    staging = ROOT / "build/hud-input"
    files = sorted(p for p in (set(payload.glob("**/hud.lua")) | set((payload / "customization/variants").glob("**/*.lua"))) if 'mouse-blocking' not in p.relative_to(payload).parts)
    total = 0
    for file in files:
        original = payload / 'mouse-blocking' / file.relative_to(payload)
        original.parent.mkdir(parents=True, exist_ok=True)
        if not original.exists():
            shutil.copyfile(file, original)
        globals_, f32 = ui.load(str(file), game)
        changed = allow_transparent_backings(globals_)
        if not changed:
            continue
        source = staging / file.relative_to(payload)
        source.parent.mkdir(parents=True, exist_ok=True)
        source.write_text(ui.to_source(globals_, f32), encoding="latin-1")
        ui.compile_lua(str(source), str(file), game)
        total += changed
    print(f"HUD input: {len(files)} layouts checked, {total} invisible backings made non-blocking")


if __name__ == "__main__":
    build(Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "setup/payload",
          sys.argv[2] if len(sys.argv) > 2 else ui.DEFAULT_GAME)
