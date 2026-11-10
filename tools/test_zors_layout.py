"""Zors resource variants keep live engine controls and fit above the action row."""
import copy
import unittest

import forts_ui as ui
from build_customization import ROOT, RES, ITEMS, customize_zors


class ZorsLayoutTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.original, _ = ui.load(str(ROOT / 'build/hud_zors.lua'), ui.DEFAULT_GAME)
        cls.paths = ui.unique_paths(ui.find_root(cls.original))

    def test_center_keeps_the_existing_zors_layout(self):
        g = copy.deepcopy(self.original)
        before = ui.to_source(g, True)
        customize_zors(g, 'center')
        self.assertEqual(ui.to_source(g, True), before)

    def test_corner_preserves_live_controls_and_fits_the_resource_frame(self):
        g = copy.deepcopy(self.original)
        customize_zors(g, 'corner')
        paths = ui.unique_paths(ui.find_root(g))
        self.assertEqual(set(paths), set(self.paths) | {RES + '/ZorsResourceBackdrop'})
        frame = paths[RES + '/ZorsResourceBackdrop'][2]
        fx, fy = frame.get('Position').array()
        fw, fh = frame.get('Size').array()
        self.assertLessEqual(fx + fw, 192)
        for resource in ('Metal', 'Energy'):
            path = RES + '/Readout' + resource
            _, node, box = paths[path]
            x, y = box.get('Position').array()
            w, h = box.get('Size').array()
            self.assertGreaterEqual(x, fx)
            self.assertGreaterEqual(y, fy)
            self.assertLessEqual(x + w, fx + fw)
            self.assertLessEqual(y + h, fy + fh)
            self.assertEqual(node.get('Colour').array()[3], 0)
            for name in (resource, resource + 'Cap', resource + 'Rate'):
                self.assertEqual(paths[path + '/' + name][1].get('Colour').array()[3], 255)
        for name in ('Grid', 'Autodoor', 'AutoCrossBrace', 'Pause', 'Menu'):
            self.assertLessEqual(fy + fh, paths[RES + '/' + name][2].get('Position').array()[1])
        enemy = paths[ITEMS + '/ResourcePanelEnemy'][2]
        self.assertLessEqual(paths[ITEMS][2].get('Position').array()[1] + enemy.get('Position').array()[1] + enemy.get('Size').array()[1], 477)


if __name__ == '__main__':
    unittest.main()
