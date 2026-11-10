# Zors.o resource placement

Requested by the HUD author: offer bottom-right resources, in the vanilla
metal-over-energy arrangement, alongside the existing centre layout.

The reference is the original `HUD/skin_base/PanelRight.dds` and
`hud-resource-readout` sprite (`PanelRight-Readout.dds`). Its frames and symbols
are baked into the vanilla panel. Reusing that backing would misalign it with
the compact amount/capacity/rate controls, so the corner uses its own ImageGen
art, saved with its prompt in `third_party/zors/generated/`.

- Centre keeps the existing Zors layout and number plates unchanged.
- Corner adds a backing at `(16, -4)`, size `176 × 84`, inside ResourcePanel.
  Live readouts sit at `(46, 0)` and `(46, 40)`; the action row stays at `y = 84`.
  The new frame renders before the controls. Only their old backing sprites
  become transparent; amount, capacity and rate still come from the game.
- ResourcePanel keeps its native right-edge anchoring. The spy panel is raised
  above the corner readouts. All existing engine control paths are retained.
- Zors settings expose only resource position. Card size, opacity, hue, white
  mode and mode indicators are normalized to the original theme settings.
- Both precompiled variants ship in the EXE. The installed profile records the
  choice, and switching themes or restoring removes the extra texture normally.

Validation commands:

```powershell
python -m unittest discover -s tools -p 'test_*layout.py'
.\tools\build_native_setup.ps1 -Test
```

The layout tests check control preservation, frame bounds, action-row clearance,
spy-panel clearance and the unchanged centre layout. Native tests install both
variants into a temporary game folder, check the exact Lua/art bytes and profile,
switch themes, restore originals, and operate the real WPF settings dialog.
These checks do not establish real-match or multiplayer rendering.
