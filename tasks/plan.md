# ♂ True Grib Version ♂ implementation

Approved concept → production art → playable HUD/cursor payload → installer UI → validation/package.

1. Generate four transparent production images: main atlas, support atlas,
   seven alert icons, and the user's corrected solid mushroom cursor.
2. Build the themed DDS files and two native resource layouts from the existing
   compact controls. Keep original signal gauges and commander character art.
3. Register Grib in the installer, use a red/cream accent, add previews and the
   independent size-adjustable mushroom cursor. Keep recolouring disabled for it.
4. Run existing plus focused tests, inspect WPF screenshots and payload bytes,
   review the diff and produce the versioned EXE/ZIP/checksums.

Key risk: generated atlas placement varies. Crop within documented grid cells,
inspect each fitted texture and fail the builder on missing/empty art. No
generated labels may become baked live resource or category text.

Rollback remains the existing engine's recorded original files. The release
and real-game installation remain separate actions.
