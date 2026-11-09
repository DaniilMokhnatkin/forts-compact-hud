"""Regression tests against the oversized/misaligned 1.4.0 screenshots."""
import unittest
import copy
import forts_ui as ui
from build_customization import customize, PANEL, RES, ITEMS, ROOT


class TacticalLayoutTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.original,_=ui.load(str(ROOT/'build/hud.lua'),ui.DEFAULT_GAME)

    def make(self,tactical=True,placement='corner',background=100):
        g=copy.deepcopy(self.original);customize(g,placement,80,background,True,tactical)
        return ui.unique_paths(ui.find_root(g))

    def test_left_backing_matches_compact_detail_area(self):
        p=self.make();c=p[PANEL+'/PanelLeft'][2]
        self.assertLessEqual(c.get('Size').array()[0],160)
        self.assertGreater(c.get('Position').array()[0],0)
        self.assertEqual(sum((c.get('Position').array()[1],c.get('Size').array()[1])),600)

    def test_corner_uses_dense_three_column_readouts(self):
        p=self.make()
        for res in ('Metal','Energy'):
            n=p[RES+'/Readout'+res][1];c=p[RES+'/Readout'+res][2]
            self.assertLessEqual(c.get('Size').array()[0],132)
            self.assertLessEqual(c.get('Size').array()[1],31)
            self.assertEqual(p[RES+'/Readout'+res+'/'+res+'Cap/'+res+'MaxLabel'][1].get('Text'),'$Maximum')

    def test_background_does_not_reactivate_baked_vanilla_frames(self):
        p=self.make()
        for key in ('PanelCenter','PanelCenterL','PanelCenterR','ResourcePanel'):
            self.assertEqual(p[PANEL+'/'+key][1].get('Colour').array()[3],0)

    def test_starkoff_keeps_original_panel_art(self):
        original=ui.unique_paths(ui.find_root(self.original))
        for placement in ('corner','center'):
            g=copy.deepcopy(self.original);customize(g,placement,80,0,True,False,True)
            p=ui.unique_paths(ui.find_root(g))
            for name in ('PanelLeft','PanelCenter','PanelCenterL','PanelCenterR','ResourcePanel'):
                path=PANEL+'/'+name
                for key in ('Texture','Colour','ColourLower'):
                    before=original[path][1].get(key);after=p[path][1].get(key)
                    self.assertEqual(after.array() if isinstance(after,ui.Table) else after,before.array() if isinstance(before,ui.Table) else before)

    def test_enemy_uses_matching_tactical_art(self):
        p=self.make()
        self.assertEqual(p[ITEMS+'/ResourcePanelEnemy'][1].get('Texture'),'ui/textures/HUD/custom/tactical_spy.dds')
        self.assertEqual(p[RES+'/ReadoutMetal'][1].get('Colour').array()[:3],[255,255,255])

    def test_tactical_cards_end_at_screen_bottom_for_each_scale(self):
        for scale in (65,80,100):
            g=copy.deepcopy(self.original);customize(g,'corner',scale,100,True,True)
            p=ui.unique_paths(ui.find_root(g))
            strip=p[ITEMS][2];tabs=p[ITEMS+'/TabbedItemPanel'][2]
            bottom=strip.get('Position').array()[1]+tabs.get('Position').array()[1]+strip.get('Size').array()[1]
            self.assertEqual(bottom,600)
            self.assertIn(PANEL+'/ItemDetail/WeaponLeft',p)
            self.assertGreater(p[ITEMS+'/TabbedItemPanel/Header'][2].get('Position').array()[1],600)

    def test_spy_building_name_keeps_localized_text_font(self):
        for tactical in (False,True):
            p=self.make(tactical=tactical)
            title=p[ITEMS+'/ResourcePanelEnemy/LastDeviceBox/LastDevice'][1]
            self.assertIsNone(title.get('Font'),'Localized building names must use the normal game font, not the numeric readout atlas')

    def test_centered_tactical_readouts_keep_the_selected_background(self):
        for opacity in (0,50,100):
            p=self.make(placement='center',background=opacity)
            for res in ('Metal','Energy'):
                self.assertEqual(p[RES+'/Readout'+res][1].get('Colour').array()[3],round(255*opacity/100))

    def test_centered_tactical_readouts_follow_cards_to_bottom(self):
        for scale in (65,80,100):
            g=copy.deepcopy(self.original);customize(g,'center',scale,100,True,True)
            p=ui.unique_paths(ui.find_root(g))
            top=p[ITEMS][2].get('Position').array()[1]+p[ITEMS+'/TabbedItemPanel'][2].get('Position').array()[1]
            for res in ('Metal','Energy'):
                box=p[RES+'/Readout'+res][2]
                bottom=p[RES][2].get('Position').array()[1]+box.get('Position').array()[1]+box.get('Size').array()[1]
                self.assertEqual(top-bottom,4)

    def test_visible_carousel_stays_inside_its_mouse_input_parent(self):
        for scale in (65,80,100):
            for placement in ('corner','center'):
                g=copy.deepcopy(self.original);customize(g,placement,scale,100,True,True)
                p=ui.unique_paths(ui.find_root(g))
                parent=p[ITEMS][2];child=p[ITEMS+'/TabbedItemPanel'][2]
                top=child.get('Position').array()[1]
                self.assertGreaterEqual(top,0)
                self.assertLessEqual(top+child.get('Size').array()[1],parent.get('Size').array()[1])


if __name__=='__main__':unittest.main()
