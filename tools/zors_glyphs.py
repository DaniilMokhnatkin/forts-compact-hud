"""Zors.o's under_construction and upgrade icons have black glyphs; the extinguisher's is a
light see-through one. Make those two glyphs light and see-through the same way:
find the black glyph inside the disc, rebuild the nebula behind it from the pixels around,
then lay the extinguisher's tint over it at half opacity.

    py -3 tools\\zors_glyphs.py <in.dds> <out.dds> [preview.png]
"""
import sys

from PIL import Image, ImageFilter, DdsImagePlugin  # noqa: F401 (registers DDS)

# The extinguisher glyph reads as (90, 97, 74) over the dark violet ground (27, 0, 51):
# that is this tint at 50%.
TINT, ALPHA = (153, 194, 97), 0.5
DISC = 56  # glyph area radius; the dark ring of the disc lies outside it


def glyph_mask(im):
    """The black glyph only: the largest connected area of near-black pixels inside the
    disc (dark patches of the nebula are smaller and not pure black), widened by its
    anti-aliased edge."""
    w, h = im.size
    px = im.load()
    inside = lambda x, y: ((x - w / 2) ** 2 + (y - h / 2) ** 2) ** 0.5 <= DISC
    core = {(x, y) for y in range(h) for x in range(w) if inside(x, y) and max(px[x, y][:3]) < 6}  # the glyph is pure black; dark nebula is (8, 0, 16) and up
    best, seen = set(), set()
    for start in core:
        if start in seen:
            continue
        area, stack = set(), [start]
        while stack:
            p = stack.pop()
            if p in seen or p not in core:
                continue
            seen.add(p)
            area.add(p)
            x, y = p
            stack += [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]
        if len(area) > len(best):
            best = area
    mask = Image.new("L", im.size)
    out = mask.load()
    for x, y in best:
        out[x, y] = 255
    # the edge: dark-ish pixels touching the glyph, by how dark they are
    edge = mask.filter(ImageFilter.MaxFilter(5)).load()
    for y in range(h):
        for x in range(w):
            if edge[x, y] and not out[x, y] and inside(x, y):
                out[x, y] = round(min(max((80 - max(px[x, y][:3])) / 58, 0), 1) * 255)
    return mask


def fill_behind(im, mask, rounds=60):
    """Rebuild the nebula under the glyph: grow the known pixels inwards."""
    w, h = im.size
    src = im.load()
    known = [[mask.getpixel((x, y)) < 128 for x in range(w)] for y in range(h)]
    colour = [[src[x, y][:3] for x in range(w)] for y in range(h)]
    for _ in range(rounds):
        changed = False
        new_known = [row[:] for row in known]
        for y in range(h):
            for x in range(w):
                if known[y][x]:
                    continue
                acc, n = [0, 0, 0], 0
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, -1), (1, -1), (-1, 1)):
                    xx, yy = x + dx, y + dy
                    if 0 <= xx < w and 0 <= yy < h and known[yy][xx]:
                        c = colour[yy][xx]
                        acc = [acc[i] + c[i] for i in range(3)]
                        n += 1
                if n:
                    colour[y][x] = tuple(v // n for v in acc)
                    new_known[y][x] = True
                    changed = True
        known = new_known
        if not changed:
            break
    filled = im.copy()
    fp = filled.load()
    for y in range(h):
        for x in range(w):
            fp[x, y] = colour[y][x] + (src[x, y][3],)
    # soften the rebuilt patch so it does not look like flat streaks
    blurred = filled.filter(ImageFilter.GaussianBlur(2))
    return Image.composite(blurred, filled, mask.filter(ImageFilter.MaxFilter(5)))


def lighten(path_in, path_out):
    im = Image.open(path_in).convert("RGBA")
    mask = glyph_mask(im)
    ground = fill_behind(im, mask)
    tint = Image.new("RGBA", im.size, TINT + (255,))
    over = Image.blend(ground, tint, ALPHA)
    out = Image.composite(over, ground, mask)
    out.putalpha(im.getchannel("A"))
    out.save(path_out, "DDS")
    return im, out


if __name__ == "__main__":
    before, after = lighten(sys.argv[1], sys.argv[2])
    if len(sys.argv) > 3:
        sheet = Image.new("RGBA", (128 * 2 + 10, 128), (60, 60, 60, 255))
        sheet.alpha_composite(before, (0, 0))
        sheet.alpha_composite(after, (138, 0))
        sheet.resize((sheet.width * 3, sheet.height * 3), Image.NEAREST).save(sys.argv[3])
