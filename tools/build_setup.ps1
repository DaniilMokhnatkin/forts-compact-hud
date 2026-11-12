# Assembles setup\payload and setup\app\assets from the built HUD and textures,
# then zips the installer to dist\Forts-Setup.zip.
#   1. py -3 layouts\compact.py; layouts\sprites.py; layouts\transaction.py; .\install.ps1 -DryRun
#   2. py -3 tools\draw_buttons.py; tools\draw_crosshairs.py
#   3. .\tools\build_setup.ps1
param([string]$Python, [string]$Game = "C:\Program Files (x86)\Steam\steamapps\common\Forts")
$ErrorActionPreference = "Stop"
if (-not $Python) {
    $bundled = Join-Path $env:USERPROFILE '.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe'
    if (Test-Path -LiteralPath $bundled) { $Python = $bundled }
    else { $Python = 'python.exe' }
}
$root = Split-Path -Parent $PSScriptRoot
$setup = Join-Path $root "setup"
$payload = Join-Path $setup "payload"
$assets = Join-Path $setup "app\assets"

$payloadPath=[IO.Path]::GetFullPath($payload)
if ($payloadPath -ne (Join-Path $root 'setup\payload')) { throw 'Unexpected payload output path' }
Remove-Item -LiteralPath $payloadPath -Recurse -Force -ErrorAction SilentlyContinue
$hud = Join-Path $payload "hud\data\ui"
New-Item -ItemType Directory -Force "$hud\screens", "$hud\textures\HUD\custom", $assets | Out-Null
Copy-Item (Join-Path $root "build\hud.lua"), (Join-Path $root "build\transaction.lua") "$hud\screens\"
Copy-Item (Join-Path $root "build\uisprites.lua") "$hud\"
Copy-Item (Join-Path $root "textures\HUD\custom\*.dds") "$hud\textures\HUD\custom\"

# Other layouts (Zors.o, Minimalist) and the icon sources for the opacity sliders.
# Needs build\hud_zors.lua (py -3 layouts\compact.py --theme zors, then compile) and the game
# for the vanilla icons.
& $Python (Join-Path $PSScriptRoot "build_zors_hud.py") $Game (Join-Path $root "build\zors_hud")
if ($LASTEXITCODE -ne 0) { throw "zors hud failed" }
& $Python (Join-Path $PSScriptRoot "build_layouts.py") $Game $payload $assets
if ($LASTEXITCODE -ne 0) { throw "layouts failed" }
& $Python (Join-Path $PSScriptRoot 'build_grib_hud.py') $Game
if ($LASTEXITCODE -ne 0) { throw 'Grib HUD failed' }

# Button sets for the other HUD skins (dlc3 is the default set above).
foreach ($dir in Get-ChildItem -LiteralPath (Join-Path $root "textures\HUD\skins") -Directory) {
    $dest = Join-Path $payload "skins\$($dir.Name)"
    New-Item -ItemType Directory -Force $dest | Out-Null
    Copy-Item (Join-Path $dir.FullName "*.dds") $dest
}

& $Python (Join-Path $PSScriptRoot 'draw_crosshair_packs.py')
if ($LASTEXITCODE -ne 0) { throw 'Duo / Neo crosshairs failed' }
$cross = Join-Path $root "textures\crosshairs"
foreach ($dir in Get-ChildItem -LiteralPath $cross -Directory) {
    $dest = Join-Path $payload "crosshairs\$($dir.Name)"
    New-Item -ItemType Directory -Force $dest | Out-Null
    Copy-Item (Join-Path $dir.FullName "*.dds") $dest
}

& $Python (Join-Path $PSScriptRoot "make_crosshair_sizes.py") (Join-Path $payload "crosshairs") $assets
if ($LASTEXITCODE -ne 0) { throw "crosshair sizes failed" }
& $Python (Join-Path $PSScriptRoot "make_setup_assets.py") $root $assets
if ($LASTEXITCODE -ne 0) { throw "assets failed" }

& $Python (Join-Path $PSScriptRoot 'build_tactical_art.py')
if ($LASTEXITCODE -ne 0) { throw 'tactical art failed' }
& $Python (Join-Path $PSScriptRoot 'build_customization.py') $Game
if ($LASTEXITCODE -ne 0) { throw 'HUD customization variants failed' }
# White Tactical HUD: the generated icon buttons (textures\white_hud\sheet.webp) in all four states.
& $Python (Join-Path $PSScriptRoot 'build_white_hud.py') (Join-Path $payload 'customization\white')
if ($LASTEXITCODE -ne 0) { throw 'white HUD buttons failed' }

# Alpha does not disable a Forts control's mouse rectangle. Run after all
# layouts and opacity variants are complete; retain every interactive child.
& $Python (Join-Path $PSScriptRoot 'hud_input.py') $payload $Game
if ($LASTEXITCODE -ne 0) { throw 'HUD click-through layouts failed' }

# The development pipeline may use scripts. The shipped installer is a native
# C# WPF application with embedded assets/payload and no interpreter dependency.
$icon = Join-Path $root "build\forts-setup.ico"
& $Python (Join-Path $PSScriptRoot "launcher\make_icon.py") $icon
if ($LASTEXITCODE -ne 0) { throw "icon failed" }
& (Join-Path $PSScriptRoot 'build_native_setup.ps1')
