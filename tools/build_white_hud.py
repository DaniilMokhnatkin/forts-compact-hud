"""White Tactical HUD buttons from the generated sheet (textures/white_hud/sheet.webp).

The sheet holds the idle state of 11 icon buttons on a 4x3 grid (see build/white-hud-icons-prompt.txt).
This cuts them out by their transparent gaps and writes the four game states as 64x64 DDS:
A idle, R hover (brighter), S selected (dimmer) and D disabled (dark plate, grey icon).

    py -3 tools\\build_white_hud.py [payload\\customization\\white]

Output: textures/white_hud/HUD/<custom|skin_base>/<Name>-<A|D|R|S>.dds, copied to the payload folder when given.
"""
import shutil
import sys
from pathlib import Path

import numpy as np
from PIL import Image, DdsImagePlugin  # noqa: F401 (registers DDS)

ROOT = Path(__file__).resolve().parents[1]
SHEET = next((p for p in (ROOT / "textures" / "white_hud" / "sheet.png", ROOT / "textures" / "white_hud" / "sheet.webp") if p.exists()),
             ROOT / "textures" / "white_hud" / "sheet.webp")
OUT = ROOT / "textures" / "white_hud" / "HUD"
SIZE = 64
# Row-major order of the sheet: (folder, name). The last cell of the third row is empty.
CELLS = [
    ("skin_base", "Grid"), ("skin_base", "AutoDoors"), ("skin_base", "CrossBrace"), ("skin_base", "HUD-Spy"),
    ("custom", "FacingLeft"), ("custom", "FacingAuto"), ("custom", "FacingRight"), ("custom", "FacingMenu"),
    ("custom", "Play"), ("custom", "Pause"), ("custom", "Menu"),
]


def runs(mask):
    """Index ranges of consecutive True values."""
    found, start = [], None
    for i, on in enumerate(mask):
        if on and start is None:
            start = i
        elif not on and start is not None:
            found.append((start, i))
            start = None
    if start is not None:
        found.append((start, len(mask)))
    return found


def cut(sheet):
    alpha = np.asarray(sheet.getchannel("A")) > 16
    boxes = []
    for y0, y1 in runs(alpha.any(axis=1)):
        for x0, x1 in runs(alpha[y0:y1].any(axis=0)):
            sub = alpha[y0:y1, x0:x1]
            ys = np.where(sub.any(axis=1))[0]
            boxes.append((x0, y0 + ys[0], x1, y0 + ys[-1] + 1))
    if len(boxes) != len(CELLS):
        raise SystemExit(f"expected {len(CELLS)} buttons on the sheet, found {len(boxes)}")
    return boxes


def levels(image, scale=1.0, lift=0.0):
    arr = np.asarray(image).astype(np.float32)
    arr[..., :3] = np.clip(arr[..., :3] * scale + lift * 255, 0, 255)
    return Image.fromarray(arr.astype(np.uint8), "RGBA")


def neutral(image):
    """The generated art carries faint colour casts and fringes; the white theme must be pure grey."""
    arr = np.asarray(image).astype(np.float32)
    gray = np.clip(0.299 * arr[..., 0] + 0.587 * arr[..., 1] + 0.114 * arr[..., 2], 0, 255).astype(np.uint8)
    return Image.fromarray(np.dstack([gray, gray, gray, arr[..., 3].astype(np.uint8)]), "RGBA")


def states(idle):
    """Hover, selected and disabled looks derived from the idle button."""
    hover = levels(idle, 1.05, 0.04)
    selected = levels(idle, 0.80)
    arr = np.asarray(idle).astype(np.float32)
    lum = (0.299 * arr[..., 0] + 0.587 * arr[..., 1] + 0.114 * arr[..., 2]) / 255
    # Plate becomes dark graphite, the dark icon becomes a mid grey: same hierarchy as the original disabled look.
    disabled_lum = np.clip(0.60 - lum * 0.42, 0, 1)
    gray = (disabled_lum * 255).astype(np.uint8)
    disabled = np.dstack([gray, gray, gray, arr[..., 3].astype(np.uint8)])
    return {"A": idle, "R": hover, "S": selected, "D": Image.fromarray(disabled, "RGBA")}


def main():
    sheet = Image.open(SHEET).convert("RGBA")
    boxes = cut(sheet)
    if OUT.exists():
        shutil.rmtree(OUT)
    preview = Image.new("RGBA", (4 * (SIZE + 8), len(CELLS) * (SIZE + 8)), (70, 74, 60, 255))
    for index, ((folder, name), box) in enumerate(zip(CELLS, boxes)):
        idle = neutral(sheet.crop(box).resize((SIZE, SIZE), Image.LANCZOS))
        target = OUT / folder
        target.mkdir(parents=True, exist_ok=True)
        for state, image in states(idle).items():
            image.save(target / f"{name}-{state}.dds", "DDS")
            col = "ARSD".index(state)
            preview.alpha_composite(image, (col * (SIZE + 8) + 4, index * (SIZE + 8) + 4))
    preview.save(ROOT / "build" / "white-hud-states.png")
    if len(sys.argv) > 1:
        dest = Path(sys.argv[1])
        if dest.exists():
            shutil.rmtree(dest)
        shutil.copytree(OUT, dest / "HUD")
    print(f"wrote {len(CELLS) * 4} textures to {OUT}")


if __name__ == "__main__":
    main()
