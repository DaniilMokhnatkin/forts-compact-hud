# Changelog

## 1.3.0 — 2026-11-09

- Windows installer is now a single C# WPF executable; it no longer launches PowerShell.
- Startup selects Compact with the neutral theme, even when Zors is installed.
- Theme changes use interruptible native WPF colour animation instead of a per-frame script timer.
- Gallery images are cached; file installation and hashing no longer block the UI thread.
- Existing backups remain compatible. Restore verifies every required backup before writing,
  preserves externally changed game files and rolls back failed operations.
