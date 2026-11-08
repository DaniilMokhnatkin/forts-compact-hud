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

Remove-Item -Recurse -Force $payload -ErrorAction SilentlyContinue
$hud = Join-Path $payload "hud\data\ui"
New-Item -ItemType Directory -Force "$hud\screens", "$hud\textures\HUD\custom", $assets | Out-Null
Copy-Item (Join-Path $root "build\hud.lua"), (Join-Path $root "build\transaction.lua") "$hud\screens\"
Copy-Item (Join-Path $root "build\uisprites.lua") "$hud\"
Copy-Item (Join-Path $root "textures\HUD\custom\*.dds") "$hud\textures\HUD\custom\"

# Other layouts (Zors.o, Minimalist) and the icon sources for the opacity sliders.
# Needs build\hud_zors.lua (py -3 layouts\compact.py --theme zors, then compile) and the game
# for the vanilla icons.
& $Python (Join-Path $PSScriptRoot "build_layouts.py") $Game $payload $assets
if ($LASTEXITCODE -ne 0) { throw "layouts failed" }

# Button sets for the other HUD skins (dlc3 is the default set above).
foreach ($dir in Get-ChildItem -LiteralPath (Join-Path $root "textures\HUD\skins") -Directory) {
    $dest = Join-Path $payload "skins\$($dir.Name)"
    New-Item -ItemType Directory -Force $dest | Out-Null
    Copy-Item (Join-Path $dir.FullName "*.dds") $dest
}

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

$zip = Join-Path $root "dist\Forts-Setup.zip"
$stage = Join-Path $root "build\Forts-Setup"
Remove-Item -Recurse -Force $stage -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $stage | Out-Null
Copy-Item (Join-Path $setup "*") $stage -Recurse
Remove-Item $zip -ErrorAction SilentlyContinue
Compress-Archive -Path $stage -DestinationPath $zip
"built $zip ($((Get-Item $zip).Length) bytes)"

# One-file Forts Setup.exe: the staged folder embedded in a small C# launcher.
Add-Type -AssemblyName System.IO.Compression.FileSystem
$appZip = Join-Path $root "build\app.zip"
Remove-Item $appZip -ErrorAction SilentlyContinue
[IO.Compression.ZipFile]::CreateFromDirectory($stage, $appZip)
$icon = Join-Path $root "build\forts-setup.ico"
& $Python (Join-Path $PSScriptRoot "launcher\make_icon.py") $icon
if ($LASTEXITCODE -ne 0) { throw "icon failed" }
$exe = Join-Path $root "dist\Forts Setup.exe"
$csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
& $csc /nologo /target:winexe /optimize+ "/out:$exe" "/win32icon:$icon" "/resource:$appZip,app.zip" `
    /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /r:System.Windows.Forms.dll `
    (Join-Path $PSScriptRoot "launcher\Launcher.cs")
if ($LASTEXITCODE -ne 0) { throw "launcher build failed" }
"built $exe ($((Get-Item -LiteralPath $exe).Length) bytes)"
