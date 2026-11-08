"""Put game screenshots into the setup's HUD gallery.

    py -3 tools\\import_screens.py <layout> <screenshot> <caption> [<screenshot> <caption> ...]

layout: compact | zors | minimalist. Replaces setup/app/assets/gallery/<layout>/ with
"01 <caption>.jpg", "02 ...": 1920x1080 JPEG, about 300 KB each instead of 4 MB PNGs.
The setup shows them in this order; the caption appears on the picture.
"""
import os
import shutil
import sys

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LAYOUTS = ("compact", "zors", "minimalist")


def main(layout, pairs):
    if layout not in LAYOUTS:
        raise SystemExit(f"layout must be one of {', '.join(LAYOUTS)}")
    out = os.path.join(ROOT, "setup", "app", "assets", "gallery", layout)
    shutil.rmtree(out, ignore_errors=True)
    os.makedirs(out)
    for n, (path, caption) in enumerate(pairs, 1):
        im = Image.open(path).convert("RGB")
        im.thumbnail((1920, 1080), Image.LANCZOS)
        safe = "".join(c for c in caption if c not in '\\/:*?"<>|').strip()
        target = os.path.join(out, f"{n:02d} {safe}.jpg")
        im.save(target, quality=86, optimize=True)
        print(f"{os.path.basename(target)}  {im.size[0]}x{im.size[1]}  {os.path.getsize(target) // 1024} KB")


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")  # captions are Russian; the Windows console is not UTF-8
    args = sys.argv[2:]
    if not args or len(args) % 2:
        raise SystemExit(__doc__)
    main(sys.argv[1], list(zip(args[0::2], args[1::2])))
