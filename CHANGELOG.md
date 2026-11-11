# Changelog

## 1.11.0 — 2026-11-12

- Added 32 crosshairs in two packs: Duo uses white shapes with a red aiming
  point; Neo uses cyan, amber, mint and violet accents, hollow centres and
  chevrons. All have dark outlines, both cursor states and 50–200% sizing.
- The new crosshairs keep their designed colours when switching from a tinted
  classic cursor. Their previews show the same colours and size as installation.
- Crosshair cards now fill the picker in six equal columns with balanced
  spacing, removing the empty strip at the right edge.

## 1.10.3 — 2026-11-11

- Replaced Grib's installer concept preview with the user's gameplay screenshot;
  the old preview is removed from the shipped gallery.
- Fixed Grib charge gauges using their complete circular mask: their angular
  alpha ramp had been mistaken for ordinary opacity. The discs now fit inside
  the commander frames and use uniform blue RGB to remove the vertical seam,
  while preserving every alpha value used by the live charge indicator.
- Extended all five reactor gauges to the end of their mushroom buttons,
  removing the empty vanilla spacer while retaining the native live controls.

## 1.10.2 — 2026-11-11

- Centred Grib's commander mushrooms, charge gauges and ready effect on the
  actual openings in the generated player/enemy frames, including mirrored art.
- Reduced the mushroom cursor's base size by 30%. Size presets and previews use
  the smaller source; rebuilding and reinstalling never shrink it repeatedly.
- Active Grib buttons and the selected build tab now use muted gold caps;
  inactive buttons keep red caps. Extinguish is red, repair is blue and
  construction is yellow; disabled appearances and opacity settings still work.
- After every HUD installation, a prominent warning opens automatically. Zors,
  Minimalist, Tactical and Grib require Default; Starkoff instructs players to
  match the game's skin with the installer selection and shows that selection.
  Zors also explicitly requires the Default menu skin/background.
  Instructions remain on the completion screen. Cursor-only changes and
  restoration do not show this warning.

## 1.10.1 — 2026-11-11

- Aligned Grib's spy-panel header, metal and energy values with their actual
  dark wells. Numeric columns have space between them and the spy button sits
  beside the device name inside the header.
- Moved resource income away from the ivory frame, widened its text field and
  adjusted line spacing in both resource layouts. Commander mushroom emblems
  and charge gauges remain unchanged.

## 1.10.0 — 2026-11-11

- Added **♂ True Grib Version ♂**: hand-painted mushroom caps, ivory frames,
  forest-green panels, matching action icons and a solid mushroom cursor.
  Resources can be placed in the centre or bottom right. The cursor is also
  selectable independently, supports 50–200% size and keeps its original colours.
- Added the missing **Demolition** opacity slider for every HUD. Like the other
  action icons it supports 10–100%, is saved with the installation and can be reset.
- The Grib installer uses a red/cream palette and clearly labels its concept
  artwork. Theme textures target the Default HUD skin in Forts.

## 1.9.0 — 2026-11-10

- Zors.o now offers resource placement in Customize HUD: bottom right, like the
  original game, or above the centre build strip. The corner option has a new
  two-row purple nebula panel with metal and energy symbols, matching the theme.
- Zors.o keeps its original colours, card size and action buttons. Resource
  placement is saved with the installed profile; Tactical-only settings do not
  carry over. The settings diagram shows the new corner artwork.

## 1.8.4 — 2026-11-09

- Downloads include the GitHub release version in their filename. Existing
  installers are preserved, including running/locked files; repeated downloads
  use a numbered suffix. Each download uses its own temporary file.
- After a successful self-update download and checksum verification, the new
  installer is highlighted in Downloads and the old installer window closes.
  Failed downloads keep the old window open and show the error.

## 1.8.3 — 2026-11-09

- Replaced all seven Zors.o action icons with the new dark horizontal purple
  nebula backgrounds and light translucent symbols. Updated construction/repair
  disabled states and the installer's icon previews from the same artwork.
- Synchronized Windows executable version metadata with the release version.

## 1.8.2 — 2026-11-09

- Tactical HUD: the panel behind the bottom-right icons now fits the icons
  exactly (no empty slot on the left, no extra margin below). With resources in
  the bottom-right corner the icon strip is as wide as the resource panel, never
  shorter; with centred resources it hugs the icons.
- White and hue colours no longer touch the pause menu, victory/defeat screens,
  achievements, fire alarms or the replay controls; only the HUD is recoloured.

## 1.8.1 — 2026-11-09

- Reworked the white Tactical HUD, which looked grey and lost its icons in game.
  Icon buttons (grid, doors, cross bracing, spy, gun facing, play, pause, menu)
  are now white plates with dark icons in all four states: idle, hover,
  selected and disabled. Panels, tabs and the commander circles stay graphite
  with a white border, the reactor panel is recoloured too, and the gauge keeps
  its colours.

## 1.8.0 — 2026-11-09

- Tactical HUD can now be white: a white swatch next to the hue slider turns
  the buttons white with dark icons, keeps panels graphite and gives them a
  white border. Moving the hue slider switches back to a colour.
- "Download" in the update banner now downloads the new installer inside Forts
  Setup, with a progress bar and into Downloads, instead of opening GitHub.
  Releases carry a SHA256SUMS.txt that the installer checks.
- The About page shows the new Matchroom Next icon.

## 1.7.0 — 2026-11-09

- 12 new crosshairs, 19 in total: an upside-down V (Chevron and Double chevron),
  CS-style Bold, Dot, Double ring, Square, Diamond, X and T-shape, and tank-style
  Sniper, Mil-dot and Arcade. Colour and size work for all of them.
- The crosshair list scrolls inside its card, with a thin macOS-style scrollbar
  that is now used across the whole window.
- The About page shows the author's avatar and the original Matchroom Next and
  Kadr icons. Matchroom's description now mentions that you can check your own
  performance in every match.

## 1.6.1 — 2026-11-09

- New "About the developer" page with the author and Matchroom Next. The
  download button fetches the latest Matchroom release from GitHub directly
  into Downloads and verifies its SHA-256 checksum; no browser is opened.
  The page also offers the Kadr screenshot tool the same way.
- Fixed "Could not check for updates": the installer now enables TLS 1.2 for
  GitHub and the feedback relay, which .NET Framework did not use by default.

## 1.6.0 — 2026-11-09

- The compact HUD is now called **Starkoff**. It only offers the resource
  position (centre or bottom-right) and keeps its original panels and colours.
- Colour, card size, panel background and indicator options are Tactical-only.
- Bug-report and HUD settings windows redesigned in the Tahoe style
  (shared sheet, grouped cards, rounded fields, capsule buttons).
- The HUD page title shows the selected HUD name.
- Added a bottom-right gameplay screenshot to the Starkoff gallery.

## 1.5.1 — 2026-11-09

- Quote multipart field names and filenames from the .NET Framework client so
  Cloudflare accepts report forms and attachments instead of invalid_form.
- Simplified the feedback explanation and removed the service hostname from the form.

## 1.5.0 — 2026-11-09

- Minimalist and Zors.o use their ready-made layouts and textures, ignoring
  inherited HUD customization. Compact and Tactical remain customizable.
- Tactical gallery now includes two real gameplay screenshots supplied by the user.
- Added a manual update button with a progress indicator and explicit result.
- Removed duplicate native caption buttons behind the custom window controls.
- Added a feedback form with an optional screenshot and reply contact, plus a
  Cloudflare-to-Telegram relay and interactive Windows configuration wizard.
  Delivery requires running the wizard; no Telegram credentials ship in the app.

## 1.4.3 — 2026-11-09

- Moved Tactical's carousel input container together with its cards, so the
  visible list remains inside its parent's mouse region at all three scales.
  Commander, reactor, spy panel and tooltip positions are preserved.

## 1.4.2 — 2026-11-09

- Tactical's centred resources now keep their selected background opacity and
  sit directly above the build cards at the bottom of the screen.

## 1.4.1 — 2026-11-09

- Reworked Tactical around the RobberFox reference: direction controls on the
  left, hidden category tabs with keyboard switching retained, and build cards
  at the bottom edge. Added new ImageGen panel and chevron reactor-health art.
- Kept the live spy readout inside the screen and aligned its resource columns.
- Restored the normal localized font for the spy's last-building name; the
  numeric readout font is used only for resource values.
- Custom backgrounds no longer reveal the vanilla HUD's baked screens and frames.
- Hue and icon opacity now support Minimalist's uncompressed DX10 BGRA DDS files,
  preserving their headers and alpha channels.

## 1.4.0 — 2026-11-09

- Added Customize HUD: choose corner/centre resource placement, build-card size,
  background opacity, UI hue and persistent grid/doors/cross-brace indicators.
- Settings are applied to the actual installed Lua/textures and recorded in the backup profile.
- Added Tactical, a flat navy recreation inspired by **RobberFox**'s HUD
  (https://www.youtube.com/@robber_fox/videos), with new ImageGen panel artwork.
- Resource boxes, build cards, enemy resources and the tactical reactor strip keep separate vertical space.
- Added a live settings diagram; the Tactical gallery clearly identifies its original reference.
- Settings share the installer's Tahoe-style controls; hue uses a colour strip, swatch and colour name.
- Screenshot navigation is a single Previous / counter / Next control on an opaque toolbar.
  Previews and the keyboard-accessible large viewer display the complete screenshot without cropping.

## 1.3.0 — 2026-11-09

- Windows installer is now a single C# WPF executable; it no longer launches PowerShell.
- Startup selects Compact with the neutral theme, even when Zors is installed.
- Theme changes use interruptible native WPF colour animation instead of a per-frame script timer.
- Gallery images are cached; file installation and hashing no longer block the UI thread.
- Existing backups remain compatible. Restore verifies every required backup before writing,
  preserves externally changed game files and rolls back failed operations.
