"""Regression checks for invisible HUD panels consuming construction clicks."""
import copy
import unittest
from pathlib import Path

import forts_ui as ui
from build_customization import customize
from hud_input import allow_transparent_backings

ROOT = Path(__file__).resolve().parents[1]
PANEL = "root/HUD/HUDPanel"


class HudInputTests(unittest.TestCase):
    def load(self, relative):
        return ui.load(str(ROOT / relative), ui.DEFAULT_GAME)

    def test_invisible_original_backings_release_the_mouse(self):
        globals_, _ = self.load("build/hud.lua")
        before = copy.deepcopy(globals_)
        allow_transparent_backings(globals_)
        paths = ui.unique_paths(ui.find_root(globals_))
        for name in ("PanelLeft", "PanelCenter", "PanelCenterL", "PanelCenterR", "ResourcePanel", "HUDItems"):
            with self.subTest(panel=name):
                self.assertIs(paths[PANEL + "/" + name][2].get("BlocksMouse"), False)
        # The repair affects input only: no geometry, sprites, names or control
        # paths change, and actual buttons keep their existing input settings.
        old = ui.unique_paths(ui.find_root(before))
        self.assertEqual(set(paths), set(old))
        for path, (_, node, control) in paths.items():
            previous = old[path][2]
            if node.get("Type") != "Static":
                self.assertEqual(control.get("BlocksMouse"), previous.get("BlocksMouse"), path)
            for key in ("Position", "Size", "Anchor", "ScriptX", "ScriptY", "Visible", "Enabled"):
                a, b = control.get(key), previous.get(key)
                self.assertEqual(a.array() if isinstance(a, ui.Table) else a,
                                 b.array() if isinstance(b, ui.Table) else b, path)

    def test_opaque_panels_and_live_buttons_keep_their_mouse_behaviour(self):
        globals_, _ = self.load("build/hud.lua")
        customize(globals_, "corner", 80, 100, True, tactical=True)
        before = ui.unique_paths(ui.find_root(copy.deepcopy(globals_)))
        allow_transparent_backings(globals_)
        after = ui.unique_paths(ui.find_root(globals_))
        for name in ("PanelLeft", "ResourcePanel/CustomizationBackdrop", "ResourcePanel/CustomizationStrip"):
            path = PANEL + "/" + name
            self.assertEqual(after[path][2].get("BlocksMouse"), before[path][2].get("BlocksMouse"), path)
        for path, (_, node, control) in after.items():
            if node.get("Type") in ("Button", "CheckBox", "TabbedPanel", "ListBox"):
                self.assertEqual(control.get("BlocksMouse"), before[path][2].get("BlocksMouse"), path)

    def test_zero_background_includes_the_added_tactical_frames(self):
        globals_, _ = self.load("build/hud.lua")
        customize(globals_, "corner", 80, 0, True, tactical=True)
        allow_transparent_backings(globals_)
        paths = ui.unique_paths(ui.find_root(globals_))
        for name in ("CustomizationBackdrop", "CustomizationStrip"):
            self.assertIs(paths[PANEL + "/ResourcePanel/" + name][2].get("BlocksMouse"), False)

    def test_gradient_and_transparent_buttons_are_not_mistaken_for_empty_backings(self):
        globals_, _ = self.load("build/hud.lua")
        paths = ui.unique_paths(ui.find_root(globals_))
        panel = paths[PANEL + "/PanelLeft"][1]
        panel.set("ColourLower", [255, 255, 255, 255])
        empty_texture = paths[PANEL + "/PanelCenterL"][1]
        empty_texture.set("Texture", "")
        empty_texture.set("Colour", [255, 255, 255, 255])
        empty_texture.set("ColourLower", [255, 255, 255, 255])
        button = paths[PANEL + "/PanelCenter"][1]
        button.set("Type", "Button")
        allow_transparent_backings(globals_)
        self.assertIsNone(paths[PANEL + "/PanelLeft"][2].get("BlocksMouse"))
        self.assertIsNone(paths[PANEL + "/PanelCenterL"][2].get("BlocksMouse"))
        self.assertIsNone(paths[PANEL + "/PanelCenter"][2].get("BlocksMouse"))


if __name__ == "__main__":
    unittest.main()
