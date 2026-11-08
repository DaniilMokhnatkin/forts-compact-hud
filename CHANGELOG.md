# Changelog

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
