"""Grib keeps live controls and exposes only a solid mushroom cursor."""
import copy
import unittest
import colorsys
from PIL import Image, ImageDraw

import forts_ui as ui
from build_customization import ROOT, RES, ITEMS, customize_themed
from build_layouts import open_image, vanilla_texture


class GribLayoutTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.original, _ = ui.load(str(ROOT / 'build/hud_grib.lua'), ui.DEFAULT_GAME)
        cls.paths = ui.unique_paths(ui.find_root(cls.original))

    def test_resource_variants_keep_all_native_paths_and_live_values(self):
        for placement in ('corner', 'center'):
            g = copy.deepcopy(self.original)
            customize_themed(g, placement, 'grib')
            p = ui.unique_paths(ui.find_root(g))
            extra = {RES + '/GribResourceBackdrop'} if placement == 'corner' else {RES + '/GribMetalPlate', RES + '/GribEnergyPlate'}
            self.assertEqual(set(p), set(self.paths) | extra)
            for res in ('Metal', 'Energy'):
                self.assertEqual(p[RES + '/Readout' + res][1].get('Colour').array()[3], 0)
                for name in (res, res + 'Cap', res + 'Rate'):
                    node = p[RES + '/Readout' + res + '/' + name][1]
                    self.assertEqual(node.get('Text'), self.paths[RES + '/Readout' + res + '/' + name][1].get('Text'))
                    self.assertEqual(node.get('Colour').array()[3], 255)
            if placement == 'corner':
                frame = p[RES + '/GribResourceBackdrop'][2]
                x, y = frame.get('Position').array(); w, h = frame.get('Size').array()
                self.assertLessEqual(x + w, 192)
                for name in ('Grid', 'Autodoor', 'AutoCrossBrace', 'Pause', 'Menu'):
                    self.assertLessEqual(y + h, p[RES + '/' + name][2].get('Position').array()[1])
                enemy = p[ITEMS + '/ResourcePanelEnemy'][2]
                self.assertLessEqual(p[ITEMS][2].get('Position').array()[1] + enemy.get('Position').array()[1] + enemy.get('Size').array()[1], 485 + y - 8)

    def test_cursor_is_a_solid_centred_mushroom_with_clear_border(self):
        im = Image.open(ROOT / 'setup/payload/crosshairs/grib/mouse_target.dds').convert('RGBA')
        self.assertEqual(im.size, (64, 64))
        self.assertGreater(im.getpixel((32, 32))[3], 200)
        self.assertEqual(im.getpixel((0, 0))[3], 0)
        box = im.getchannel('A').point(lambda a: 255 if a >= 128 else 0).getbbox()
        self.assertLessEqual(abs((box[2] - box[0]) - round(58 * .7)), 1)
        self.assertLessEqual(abs((box[3] - box[1]) - round(58 * .7)), 1)
        self.assertLess(abs((box[0] + box[2]) / 2 - 32), 1)
        pixels = [im.getpixel((x, y)) for y in range(64) for x in range(64)]
        red = sum(1 for r, g, b, a in pixels if a > 200 and r > g * 1.5 and r > b * 1.5)
        cream = sum(1 for r, g, b, a in pixels if a > 200 and r > 180 and g > 150 and b > 90)
        opaque = sum(1 for r, g, b, a in pixels if a > 200)
        self.assertGreater(red / opaque, .2); self.assertGreater(cream / opaque, .2)

    def test_commander_emblems_and_gauges_are_centred_in_the_frame_holes(self):
        g = copy.deepcopy(self.original); customize_themed(g, 'corner', 'grib')
        p = ui.unique_paths(ui.find_root(g))
        folder = ROOT / 'setup/payload/layouts/grib/textures/HUD/skin_base/commander'
        sprite_globals, _ = ui.load(str(ROOT / 'reference/uisprites.lua'), ui.DEFAULT_GAME)
        sprites = next(value for name, value in sprite_globals if name == 'Sprites')
        sprite_uv = {}
        for value in sprites.array():
            if isinstance(value, ui.Table) and value.get('Name') in ('CO-panel-background', 'CO-panel-background-enemy', 'CO-panel-gauge', 'CO-panel-gauge-enemy'):
                frame = value.get('States').get('Normal').get('Frames').array()[0]
                sprite_uv[value.get('Name')] = tuple(frame.get(k) for k in ('left', 'top', 'right', 'bottom'))
        for parent, frame_name, children in (
            (ITEMS + '/COBackground', 'COGauge_BG.dds', [('COActivate', 'COGauge_Button-A.dds'), ('COGauge', 'COGauge_Radial.dds'), ('COStar', 'COGauge_Star.dds')]),
            (ITEMS + '/ResourcePanelEnemy/COBackground_Enemy', 'COGauge_BG_enemy.dds', [('COActivate_Enemy', 'COGauge_Button_enemy-A.dds'), ('COGauge_Enemy', 'COGauge_Radial_enemy.dds')])):
            frame = Image.open(folder / frame_name).convert('RGBA')
            holes = frame.getchannel('A').point(lambda a: 255 if a >= 24 else 0)
            ImageDraw.floodfill(holes, (frame.width // 2, frame.height // 2), 128)
            bounds = holes.point(lambda a: 255 if a == 128 else 0).getbbox()
            fw, fh = p[parent][2].get('Size').array()
            left, top, right, bottom = sprite_uv[p[parent][1].get('Texture')]
            target = (fw * ((bounds[0] + bounds[2]) / 2 / frame.width - left) / (right - left),
                      -fh + fh * ((bounds[1] + bounds[3]) / 2 / frame.height - top) / (bottom - top))
            for child, texture in children:
                image = Image.open(folder / texture).convert('RGBA')
                # Radial alpha stores the charge ramp; even the low-alpha half
                # belongs to the circle and must contribute to its centre.
                bounds = image.getchannel('A').point(lambda a: 255 if a >= (1 if child.startswith('COGauge') else 128) else 0).getbbox()
                c = p[parent + '/' + child][2]; x, y = c.get('Position').array(); w, h = c.get('Size').array()
                uv = sprite_uv.get(p[parent + '/' + child][1].get('Texture'), (0, 0, 1, 1))
                left, top, right, bottom = uv
                actual = (x + w * ((bounds[0] + bounds[2]) / 2 / image.width - left) / (right - left),
                          y + h * ((bounds[1] + bounds[3]) / 2 / image.height - top) / (bottom - top))
                self.assertAlmostEqual(actual[0], target[0], delta=.05, msg=child + ' horizontal centre')
                self.assertAlmostEqual(actual[1], target[1], delta=.05, msg=child + ' vertical centre')

    def test_commander_charge_disc_fits_the_opening_without_a_colour_seam(self):
        folder = ROOT / 'setup/payload/layouts/grib/textures/HUD/skin_base/commander'
        for placement in ('center', 'corner'):
            g = copy.deepcopy(self.original); customize_themed(g, placement, 'grib')
            p = ui.unique_paths(ui.find_root(g))
            for parent, suffix in ((ITEMS + '/COBackground', ''), (ITEMS + '/ResourcePanelEnemy/COBackground_Enemy', '_enemy')):
                frame = Image.open(folder / ('COGauge_BG' + suffix + '.dds')).convert('RGBA')
                hole = frame.getchannel('A').point(lambda a: 255 if a >= 24 else 0)
                ImageDraw.floodfill(hole, (frame.width // 2, frame.height // 2), 128)
                bounds = hole.point(lambda a: 255 if a == 128 else 0).getbbox()
                gauge = Image.open(folder / ('COGauge_Radial' + suffix + '.dds')).convert('RGBA')
                circle = gauge.getchannel('A').point(lambda a: 255 if a else 0).getbbox()
                control = p[parent + '/COGauge' + ('_Enemy' if suffix else '')][2]
                x, y = control.get('Position').array(); w, h = control.get('Size').array()
                fw, fh = p[parent][2].get('Size').array()
                left, top, right, bottom = (.128, .14, .859, .855)
                opening_x = sorted(fw * (v / frame.width - left) / (right - left) for v in (bounds[0], bounds[2]))
                opening_y = [-fh + fh * (v / frame.height - top) / (bottom - top) for v in (bounds[1], bounds[3])]
                fill_x = sorted(x + w * (v / gauge.width - left) / (right - left) for v in (circle[0], circle[2]))
                fill_y = [y + h * (v / gauge.height - top) / (bottom - top) for v in (circle[1], circle[3])]
                self.assertGreaterEqual(fill_x[0], opening_x[0] + .3)
                self.assertLessEqual(fill_x[1], opening_x[1] - .3)
                self.assertGreaterEqual(fill_y[0], opening_y[0] + .3)
                self.assertLessEqual(fill_y[1], opening_y[1] - .3)
                # Both sides of twelve o'clock need the same blue: the old
                # angular RGB discontinuity drew the thin vertical stripe.
                cx = round((circle[0] + circle[2]) / 2); cy = round((circle[1] + circle[3]) / 2)
                a = gauge.getpixel((cx - 3, cy - round((circle[3] - circle[1]) * .3)))[:3]
                b = gauge.getpixel((cx + 3, cy - round((circle[3] - circle[1]) * .3)))[:3]
                self.assertLessEqual(max(abs(u - v) for u, v in zip(a, b)), 5)
                original = open_image(vanilla_texture(ui.DEFAULT_GAME, 'HUD/skin_base/commander/COGauge_Radial' + suffix + '.dds'))
                self.assertEqual(gauge.getchannel('A').tobytes(), original.getchannel('A').tobytes(), 'The charge ramp must stay intact')

    def test_reactor_gauges_fill_the_track_beside_the_button(self):
        for placement in ('center', 'corner'):
            g = copy.deepcopy(self.original); customize_themed(g, placement, 'grib')
            p = ui.unique_paths(ui.find_root(g))
            for index in range(5):
                bar = ITEMS + '/ReactorBars/ReactorBG' + str(index)
                button = p[bar + '/ReactorButton' + str(index)][2]
                gauge = p[bar + '/ReactorGauge' + str(index)][2]
                bx, by = button.get('Position').array(); bw, bh = button.get('Size').array()
                x, y = gauge.get('Position').array(); w, h = gauge.get('Size').array()
                self.assertGreaterEqual(x, bx + bw + 2)
                self.assertLessEqual(x - bx - bw, 4, 'No empty vanilla spacer before the live gauge')
                self.assertGreaterEqual(x + w, 157)
                self.assertLessEqual(x + w, 159)
                self.assertGreaterEqual(y, by)
                self.assertLessEqual(y + h, by + bh)

    def test_texture_states_and_demolition_source_are_complete(self):
        textures = ROOT / 'setup/payload/layouts/grib/textures'
        for folder, names in [('HUD/custom', ('Pause', 'Play', 'Menu', 'FacingLeft', 'FacingAuto', 'FacingRight', 'FacingMenu')),
                              ('HUD/skin_base', ('Grid', 'AutoDoors', 'CrossBrace')),
                              ('HUD/skin_base/tabs', ('Button-Long',))]:
            for name in names:
                for state in ('A', 'R', 'S', 'D'):
                    self.assertTrue((textures / folder / f'{name}-{state}.dds').is_file())
        self.assertTrue((ROOT / 'setup/payload/icons/grib/scrap.dds').is_file())
        self.assertFalse((textures / 'mouse_target.dds').exists(), 'HUD selection alone must not force a cursor')

    def test_cursor_build_is_repeatable_from_the_original_art(self):
        from build_grib_hud import cursor_texture, clean, GEN
        source = clean(Image.open(GEN / 'mushroom-cursor.png').convert('RGBA'))
        before = source.tobytes()
        first = cursor_texture(source); second = cursor_texture(source)
        self.assertEqual(first.tobytes(), second.tobytes())
        self.assertEqual(before, source.tobytes())

    def test_grib_colours_distinguish_active_buttons_and_world_actions(self):
        folder = ROOT / 'setup/payload/layouts/grib/textures'
        def cap_hue(name):
            im = Image.open(folder / name).convert('RGBA')
            values = []
            for y in range(round(im.height * .4)):
                for x in range(im.width):
                    r, g, b, a = im.getpixel((x, y)); h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
                    if a > 200 and s > .4 and v > .25: values.append(h)
            self.assertGreater(len(values), 5)
            return sorted(values)[len(values) // 2]
        red = cap_hue('HUD/skin_base/Grid-A.dds')
        self.assertTrue(red < .08 or red > .94)
        self.assertTrue(.08 < cap_hue('HUD/skin_base/Grid-S.dds') < .18)
        red = cap_hue('extinguisher.dds')
        self.assertTrue(red < .08 or red > .94)
        self.assertTrue(.08 < cap_hue('under_construction.dds') < .18)
        self.assertTrue(.5 < cap_hue('repair.dds') < .67)

    def test_spy_values_use_the_numeric_wells_not_header_or_dividers(self):
        image = open_image(str(ROOT / 'setup/payload/layouts/grib/textures/HUD/skin_base/PanelSpy.dds'))
        bands = []
        start = None
        for y in range(image.height):
            r, g, b, a = image.getpixel((round(image.width * .55), y))
            inside = a > 200 and g > r * 1.1 and r < 65 and b < 65
            if inside and start is None: start = y
            if not inside and start is not None:
                if y - start > 12: bands.append((start, y))
                start = None
        self.assertEqual(len(bands), 3)
        for placement in ('corner', 'center'):
            g = copy.deepcopy(self.original); customize_themed(g, placement, 'grib')
            p = ui.unique_paths(ui.find_root(g)); spy = ITEMS + '/ResourcePanelEnemy'
            scale = p[spy][2].get('Size').array()[1] / image.height
            for resource, band in zip(('Metal', 'Energy'), bands[1:]):
                top, bottom = (v * scale for v in band)
                for suffix in ('', 'Cap', 'Rate'):
                    c = p[spy + '/' + resource + suffix][2]
                    x, y = c.get('Position').array(); w, h = c.get('Size').array()
                    self.assertGreaterEqual(y - h / 2, top + 2)
                    self.assertLessEqual(y + h / 2, bottom - 2)
                    ix = round((x + 3) / p[spy][2].get('Size').array()[0] * image.width)
                    iy = round(y / scale)
                    r, green, b, a = image.getpixel((ix, iy))
                    self.assertTrue(a > 200 and green > r * 1.1 and r < 65 and b < 65, 'Spy values need a dark gutter')
            label = p[spy + '/LastDeviceBox'][2]
            child = p[spy + '/LastDeviceBox/LastDevice'][2]
            label_y = label.get('Position').array()[1] + child.get('Position').array()[1]
            self.assertGreaterEqual(label_y, bands[0][0] * scale + 2)
            self.assertLessEqual(label_y + child.get('Size').array()[1], bands[0][1] * scale - 2)
            columns = [p[spy + '/Metal' + suffix][2] for suffix in ('', 'Cap', 'Rate')]
            for left, right in zip(columns, columns[1:]):
                self.assertGreaterEqual(right.get('Position').array()[0] - right.get('Size').array()[0] - left.get('Position').array()[0], 3)

    def test_own_rates_leave_a_dark_gutter_before_the_frame(self):
        for placement in ('corner', 'center'):
            g = copy.deepcopy(self.original); customize_themed(g, placement, 'grib')
            p = ui.unique_paths(ui.find_root(g))
            for resource in ('Metal', 'Energy'):
                box = p[RES + '/Readout' + resource][2]
                c = p[RES + '/Readout' + resource + '/' + resource + 'Rate'][2]
                bx, by = box.get('Position').array(); x, y = c.get('Position').array(); w, h = c.get('Size').array()
                backing = p[RES + ('/GribResourceBackdrop' if placement == 'corner' else '/Grib' + resource + 'Plate')]
                fx, fy = backing[2].get('Position').array(); fw, fh = backing[2].get('Size').array()
                image = open_image(str(ROOT / 'setup/payload/layouts/grib/textures' / backing[1].get('Texture').removeprefix('ui/textures/')))
                for px, py in ((bx + x + 3, by + y - h / 2), (bx + x + 3, by + y + h / 2)):
                    ix, iy = round((px - fx) / fw * image.width), round((py - fy) / fh * image.height)
                    self.assertTrue(0 <= ix < image.width and 0 <= iy < image.height)
                    r, green, b, a = image.getpixel((ix, iy))
                    self.assertTrue(a > 200 and green > r * 1.1 and r < 65 and b < 65, f'{placement} {resource} rate reaches the frame')
                self.assertGreaterEqual(w, 48, 'Long positive rates need a full-width field')


if __name__ == '__main__':
    unittest.main()
