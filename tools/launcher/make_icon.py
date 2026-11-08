"""App icon for Forts Setup.exe: dark rounded square with the crosshair mark.

    py -3 tools\\launcher\\make_icon.py <out.ico>
"""
import sys

from PIL import Image, ImageDraw

S = 256


def icon():
    img = Image.new("RGBA", (S, S))
    bg = Image.new("RGBA", (S, S))
    d = ImageDraw.Draw(bg)
    for y in range(S):  # top-to-bottom gradient, as in the window's app header
        t = y / (S - 1)
        c = tuple(round(a + (b - a) * t) for a, b in zip((58, 58, 64), (20, 20, 22)))
        d.line([(0, y), (S, y)], fill=c + (255,))
    mask = Image.new("L", (S, S))
    ImageDraw.Draw(mask).rounded_rectangle([8, 8, S - 8, S - 8], radius=56, fill=255)
    img.paste(bg, (0, 0), mask)
    d = ImageDraw.Draw(img)
    c, r, w = S / 2, 62, 18
    d.ellipse([c - r, c - r, c + r, c + r], outline="white", width=w)
    for x0, y0, x1, y1 in ((c, 36, c, 92), (c, S - 92, c, S - 36), (36, c, 92, c), (S - 92, c, S - 36, c)):
        d.line([x0, y0, x1, y1], fill="white", width=w)
    d.ellipse([c - 14, c - 14, c + 14, c + 14], fill=(255, 90, 78))
    return img


if __name__ == "__main__":
    icon().save(sys.argv[1], sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
