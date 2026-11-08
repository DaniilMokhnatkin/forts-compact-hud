# Native Forts Setup

Replace the PowerShell-hosted UI and installer engine with a single C# WPF executable.
The existing artwork, controls, layout choices and backup format remain compatible.
PowerShell/Python may still be used by development build scripts; the shipped app must
neither contain runtime scripts nor launch a shell/interpreter.

Acceptance:
- Startup selects Compact with the neutral theme, including when Zors is installed.
- Selecting Zors animates colours through WPF animation clocks; no per-frame resource replacement.
- Installation and hashing run off the UI thread, with real progress and no artificial sleeps.
- Preserve HUD layouts, skin sets, crosshair style/size/colour, icon opacity, skip-intro,
  RU/EN UI, gallery, Steam detection, compatibility checks and restoration.
- Read old backup state.json; verify backups before restore; preserve externally changed files.
- Validate relative paths; roll back failed writes, including metadata.
- Tests use disposable fake-game directories. No installation into the real game during verification.

Implementation order: tested C# engine -> WPF controller using existing XAML ->
embedded-resource executable -> fake-game regression tests and live UI Automation smoke test.

Why the old window misbehaved: Refresh selected status.Layout automatically, and its
DispatcherTimer replaced five brushes every 16 ms. Gallery images were decoded on the UI
thread. The native controller keeps explicit user selection, caches previews and uses
ColorAnimation with interruptible transitions.

Validation (2026-11-09): 16 engine checks and 14 WPF UI checks passed. The final
packaged EXE was launched and driven through Microsoft winapp CLI 0.8.0 UI Automation:
Compact was initially selected, Zors/Compact selection succeeded, and no child shell
or interpreter process was created. Its embedded pack contains 875 resource files,
with no PS1/CMD/BAT files or nested executable. Installation/restoration were exercised
only against disposable fake-game folders, not the real Forts installation.
