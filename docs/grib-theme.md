# ♂ True Grib Version ♂

## Accepted direction
The user approved the mushroom concept on 2026-11-11 and requested implementation.
The final requested display name is **♂ True Grib Version ♂**; `grib` remains
the internal layout/cursor id. Demolition opacity was added to the same task
at the user's explicit request; it applies to all HUDs, not just Grib.
Their correction takes precedence over the earlier crosshair proposal: the cursor
is one complete mushroom, with no target ring, cross, ticks or central hole.

## Objective and scope
Add a separate `grib` HUD and independently selectable `grib` cursor to Forts
Setup. Red spotted caps, ivory stem frames, dark forest wells and small moss
accents follow the approved concept. Keep live amounts, capacities, rates,
functional glyphs, commander characters and warning/charge colours readable.
The meme copy in the installer is “100% гриб. Скилл в комплект не входит.”

Resources support centre and bottom-right placement. Other theme styling is
fixed, like Zors. Four button states and disabled construction/repair states
must be included. Cursor size follows the existing 50–200% selector; its red
cap and cream stem retain their original colours. Selecting the theme suggests
its cursor; players can pick a different cursor independently.

The theme targets the game's Default HUD skin, using the existing skin notice.
No menu backgrounds, splash, world structures, node/weapon textures, gameplay
rules, multiplayer data or existing user settings are changed. The concept
board is labelled as a concept, never presented as a tested gameplay screenshot.

## Implementation structure
- `textures/grib/generated/`: original ImageGen atlases, cursor and prompts.
- `tools/build_grib_hud.py`: deterministic crop/fit, DDS conversion, four states,
  complete payload and installer previews. Reuse existing glyph/fit helpers.
- `layouts/compact.py`: Grib readout colour/texture theme on the proven layout.
- `tools/build_customization.py`: two precompiled resource placements, retaining
  all existing game control paths; one optional backing control in corner mode.
- `setup/native/`: layout/cursor selection, red/cream installer accent, settings
  preview, installation/profile/restore through the existing engine.
- `tools/native/` and `tools/test_grib_layout.py`: regression coverage.

## Commands and conventions
Use the bundled Python when `python` is unavailable:
`C:/Users/r4pto/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe`.

```powershell
python tools/build_grib_hud.py 'C:\Program Files (x86)\Steam\steamapps\common\Forts'
python tools/build_customization.py
python -m unittest discover -s tools -p 'test_*layout.py'
.\tools\build_native_setup.ps1 -Test
```

Follow the existing `Path`/Pillow builders and C# WPF conventions. For example:
`save(texture, out / 'HUD/custom/grib_plate.dds')`. Generated textures are
fitted to the original alpha bounds, not guessed canvas dimensions. Read
vanilla sources from the installer backup before the live game directory.

## Validation and boundaries
Always preserve native control names and number callbacks, verify alpha and
payload texture references, run layout and native tests before commits, and
inspect the actual WPF rendering. Installer tests use a temporary game fixture.
Never claim a concept image or fixture test demonstrates real-match rendering.
No dependency additions are needed. Do not install into the real game or publish
a new release as part of this implementation without the user's instruction.

Success: both positions install and persist, the cursor is a solid mushroom at
every supported size, button and alert states are complete, the theme is selectable
in Russian/English, switching layouts restores foreign theme files, and the
existing installer/rollback tests pass. Deliver a local versioned EXE and hashes.

## Verification performed
- 20 layout tests, including transparent borders, the solid mushroom silhouette,
  spy-well alignment and resource-rate gutters.
- 49 native engine checks, including both Grib variants, cursor independence,
  fixed cursor colours, real demolition DDS alpha at 25%, reset and restoration.
- 62 WPF checks, including the exact requested name, RU/EN copy, theme switching,
  resource settings, demolition slider and mandatory skin warnings; rendered screens inspected.
- 7 downloader checks. Full payload-to-EXE build completed; embedded artwork,
  cursor, demolition source and both Lua variants match their source bytes.
- Real-game and multiplayer rendering remain unverified; no real installation
  or new GitHub release was performed.

## 1.10.1 game-feedback correction
The user's in-game screenshots exposed the spy text retaining vanilla y=40/64
while the generated texture places its numeric wells around y=59/79. The header
also sat on the red cap, and own-resource rates reached the ivory right bezel.
`tools/grib_geometry.py` aligns these live fields to the existing artwork without
changing the textures or engine control names. Tests sample the actual DDS
wells and gutters, rather than checking only the outer panel bounds.

The user briefly requested removing the two commander mushrooms, then explicitly
retracted that request. Both mushroom emblems and all their texture states remain
unchanged. The running game has not been modified or closed during this fix.

## 1.10.2 refinement
The user's next game screenshot confirmed the resource layout correction and
showed the commander emblems above the circles' centres. Frame silhouettes have
asymmetric caps; their texture-canvas centre is not the centre of the transparent
round opening. Read the opening from alpha and project its centre through the
player/enemy control sizes, retaining the enemy's mirrored horizontal geometry.
Align the emblem, radial charge and ready effect to the same point.
The background and radial sprites crop their DDS with UVs `.128/.14/.859/.855`;
button sprites and the ready effect use the full texture. Projection must include
these UVs, and the regression test reads them independently from `uisprites.lua`.

The cursor now uses a 70% base sprite on the same centred 64×64 canvas. Every
build starts from the original large mushroom PNG. Red ordinary buttons become
muted gold in their selected state; world action caps are red for extinguish,
blue for repair and ochre for construction. Original and edited ImageGen sheets
and their exact prompts remain in `textures/grib/generated/`.

After successful HUD installation, a modal amber warning instructs players to
select Settings → Game → HUD Skin → Default and restart Forts for Grib, Zors,
Minimalist and Tactical. The user's clarification also requires a separate
Starkoff warning: the game's skin and the selected installer skin must match;
the chosen name is displayed. No warning is shown for restoring files or
changing only cursor/icons, and instructions remain on the completion page.
For Zors.o the user additionally requires the menu skin/background to be Default;
both setting paths appear in its post-install warning and its page hint.
