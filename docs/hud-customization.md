# HUD customization

User choices apply to the installed game HUD, not only to the installer preview:
resources in the right corner or above the centre strip; build cards 65/80/100%;
panel backgrounds 0/50/100%; interface hue -180..180 degrees; corner mode buttons.
Default is resources in the corner, smaller cards and transparent backing. Existing
Compact, Zors.o and Minimalist layouts remain available.

Tactical recreates the flat navy side-panel arrangement from the user reference.
HUD reference/credit: **RobberFox**, https://www.youtube.com/@robber_fox/videos .
The reference screenshot is displayed as a reference, not claimed as an in-game
screenshot of this implementation. New panel artwork is generated with ImageGen.

Build-time Python produces 144 compiled Lua variants. Runtime C# selects a validated
variant, rotates UI texture hues while preserving alpha/white glyphs, and records
the profile/hash in the compatible backup. No runtime compiler, PowerShell or Python.
Game control names/paths are preserved, including grid/cross-brace controls.

The settings dialog has a live layout diagram explicitly labelled as a diagram.
The base HUD layout's gallery/reference remains separate from this schematic preview.

Settings follow the existing Tahoe design vocabulary (Claude apple-design and
ui-review-tahoe skills), adapted to WPF: shared capsule buttons, grouped surfaces,
rounded selectors, subtle glass and named colours instead of technical degree values.
Screenshot controls sit outside the image on an opaque toolbar, with one set of
previous/counter/next controls; the full viewer supports arrow keys and Escape.

Verification: 144 compiled variants passed control-path and layout-clearance assertions;
21 C# engine checks and 25 WPF UI checks passed. The real game was not modified during
verification. Combat, multiple-reactor layouts and multiplayer still need real-game
validation; the RobberFox screenshot is an attributed reference, not proof of a game run.
