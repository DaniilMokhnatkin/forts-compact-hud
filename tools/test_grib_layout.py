"""Grib keeps live controls and exposes only a solid mushroom cursor."""
import copy
import unittest
from PIL import Image

import forts_ui as ui
from build_customization import ROOT, RES, ITEMS, customize_themed


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
        self.assertGreaterEqual(box[0], 2); self.assertGreaterEqual(box[1], 2)
        self.assertLessEqual(box[2], 62); self.assertLessEqual(box[3], 62)
        self.assertLess(abs((box[0] + box[2]) / 2 - 32), 1)
        pixels = [im.getpixel((x, y)) for y in range(64) for x in range(64)]
        red = sum(1 for r, g, b, a in pixels if a > 200 and r > g * 1.5 and r > b * 1.5)
        cream = sum(1 for r, g, b, a in pixels if a > 200 and r > 180 and g > 150 and b > 90)
        self.assertGreater(red, 300); self.assertGreater(cream, 300)

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


if __name__ == '__main__':
    unittest.main()
