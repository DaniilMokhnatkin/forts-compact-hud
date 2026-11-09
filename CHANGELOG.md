# Changelog

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
