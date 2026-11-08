# Headless test of setup\app\engine.ps1 against a fake Forts folder built from original\.
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
. (Join-Path $root "setup\app\engine.ps1")
$fail = 0
function Check([string]$name, [bool]$ok) { if ($ok) { "  ok   $name" } else { "  FAIL $name"; $script:fail++ } }

function New-FakeGame([string]$crosshairSource) {
    $game = Join-Path $root "build\fakegame-setup"
    Remove-Item -Recurse -Force $game -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force "$game\data\ui\screens", "$game\data\ui\textures" | Out-Null
    Set-Content "$game\Forts.exe" "x"
    Copy-Item "$root\original\hud.lua", "$root\original\transaction.lua" "$game\data\ui\screens\"
    Copy-Item "$root\original\uisprites.lua" "$game\data\ui\"
    Copy-Item "$crosshairSource\mouse_target.dds", "$crosshairSource\mouse_target_painted.dds" "$game\data\ui\textures\"
    return $game
}

$vanilla = Join-Path $root "textures\vanilla"
"== fresh vanilla game: HUD + cross"
$game = New-FakeGame $vanilla
$before = @{}; Get-ChildItem -Recurse -File $game | ForEach-Object { $before[$_.FullName] = Get-Sha $_.FullName }
Install-Pack -Game $game -Hud $true -Crosshair "cross"
$st = Get-PackStatus $game
Check "HUD installed" $st.HudInstalled
Check "crosshair = cross" ($st.Crosshair -eq "cross")
Check "28 button textures" ((Get-ChildItem "$game\data\ui\textures\HUD\custom").Count -eq 28)

"== switch crosshair to scope (backup keeps the vanilla one)"
Install-Pack -Game $game -Hud $false -Crosshair "scope"
Check "crosshair = scope" ((Get-PackStatus $game).Crosshair -eq "scope")
Check "backup holds vanilla" ((Get-Sha "$game\Forts-Setup-Backup\data\ui\textures\mouse_target.dds") -eq (Get-Sha "$vanilla\mouse_target.dds"))

"== restore"
$skipped = @(Restore-Pack -Game $game)
Check "nothing skipped" ($skipped.Count -eq 0)
$same = $true; foreach ($k in $before.Keys) { if ((Get-Sha $k) -ne $before[$k]) { $same = $false } }
Check "all files back to original" $same
Check "custom textures removed" (-not (Test-Path "$game\data\ui\textures\HUD\custom"))
Check "backup renamed" (-not (Test-Path "$game\Forts-Setup-Backup") -and (Get-ChildItem $game -Directory -Filter "Forts-Setup-Backup-restored-*").Count -eq 1)

"== restore after a Steam update touched hud.lua"
$game = New-FakeGame $vanilla
Install-Pack -Game $game -Hud $true -Crosshair "dot_ring"
Set-Content "$game\data\ui\screens\hud.lua" "-- new game version"
$skipped = @(Restore-Pack -Game $game)
Check "updated hud.lua skipped" ($skipped -contains "data\ui\screens\hud.lua")
Check "updated hud.lua untouched" ((Get-Content "$game\data\ui\screens\hud.lua") -eq "-- new game version")
Check "crosshair restored" ((Get-Sha "$game\data\ui\textures\mouse_target.dds") -eq (Get-Sha "$vanilla\mouse_target.dds"))

"== other game version: HUD refused, crosshair still allowed"
$game = New-FakeGame $vanilla
Set-Content "$game\data\ui\uisprites.lua" "-- other version"
Check "status: incompatible" (-not (Get-PackStatus $game).HudCompatible)
$refused = $false; try { Install-Pack -Game $game -Hud $true -Crosshair $null } catch { $refused = $true }
Check "HUD install refused" $refused
Check "no backup left by refusal" (-not (Test-Path "$game\Forts-Setup-Backup\state.json"))
Install-Pack -Game $game -Hud $false -Crosshair "brackets"
Check "crosshair installed anyway" ((Get-PackStatus $game).Crosshair -eq "brackets")

"== update over an older build of this installer"
$game = New-FakeGame $vanilla
Install-Pack -Game $game -Hud $true -Crosshair $null
Set-Content "$game\data\ui\screens\hud.lua" "-- older pack build"
$state = Read-State $game
$state["data\ui\screens\hud.lua"].installed = Get-Sha "$game\data\ui\screens\hud.lua"
Write-State $game $state
Check "older build: compatible" (Get-PackStatus $game).HudCompatible
Install-Pack -Game $game -Hud $true -Crosshair $null
Check "older build: updated" (Get-PackStatus $game).HudInstalled
Restore-Pack -Game $game | Out-Null
Check "older build: restore gives vanilla hud.lua" ((Get-Sha "$game\data\ui\screens\hud.lua") -eq (Get-Sha "$root\original\hud.lua"))
Set-Content "$game\data\ui\screens\hud.lua" "-- someone else's mod"
Check "foreign hud.lua still refused" (-not (Get-PackStatus $game).HudCompatible)

"== HUD skins"
$game = New-FakeGame $vanilla
New-Item -ItemType Directory -Force "$game\users\1" | Out-Null
Set-Content "$game\users\1\settings.lua" "data = {`n`tCurrentHUDSkin = `"db/skins_hud_elite.lua`",`n}"
Check "skin read from settings: elite" ((Get-GameHudSkin $game) -eq "elite")
Set-Content "$game\users\1\settings.lua" "data = {`n`tCurrentHUDSkin = `"db/skins_hud_dlc2_tester.lua`",`n}"
Check "tester skin maps to dlc2" ((Get-GameHudSkin $game) -eq "dlc2")
Install-Pack -Game $game -Hud $true -Crosshair $null -HudSkin "pro"
$st = Get-PackStatus $game
Check "pro buttons installed and detected" ($st.HudInstalled -and $st.HudSkin -eq "pro")
Check "pro pause texture in place" ((Get-Sha "$game\data\ui\textures\HUD\custom\Pause-A.dds") -eq (Get-Sha "$root\setup\payload\skins\pro\Pause-A.dds"))
Install-Pack -Game $game -Hud $true -Crosshair $null -HudSkin "dlc3"
Check "switch to dlc3" ((Get-PackStatus $game).HudSkin -eq "dlc3")
Restore-Pack -Game $game | Out-Null
Check "skins: restore removes custom textures" (-not (Test-Path "$game\data\ui\textures\HUD\custom"))

"== screen check"
$game = New-FakeGame $vanilla
New-Item -ItemType Directory -Force "$game\users\1" | Out-Null
Set-Content "$game\users\1\settings_hardware.lua" "data = {`n`tAutoResolution = false,`n`tViewHeight = 1080,`n`tViewWidth = 2560,`n}"
$sc = Get-GameScreen $game
Check "2560x1080 is not 16:9" ($sc.Width -eq 2560 -and $sc.Height -eq 1080 -and -not $sc.Wide169)
Set-Content "$game\users\1\settings_hardware.lua" "data = {`n`tAutoResolution = false,`n`tViewHeight = 1440,`n`tViewWidth = 2560,`n}"
Check "2560x1440 is 16:9" (Get-GameScreen $game).Wide169
Remove-Item -Recurse "$game\users"
Check "no settings: null" ($null -eq (Get-GameScreen $game))

"== coloured crosshairs"
$game = New-FakeGame $vanilla
Install-Pack -Game $game -Hud $false -Crosshair "cross" -CrosshairColor "green"
$st = Get-PackStatus $game
Check "green cross detected" ($st.Crosshair -eq "cross" -and $st.CrosshairColor -eq "green" -and $st.CrosshairSize -eq 100)
$px = [IO.File]::ReadAllBytes("$game\data\ui\textures\mouse_target.dds")
$white = 0; $tinted = 0
for ($i = 128; $i -lt $px.Length; $i += 4) {  # B, G, R, A
    if ($px[$i + 3] -lt 200) { continue }
    if ($px[$i] -gt 200 -and $px[$i + 1] -gt 200 -and $px[$i + 2] -gt 200) { $white++ }
    elseif ($px[$i + 1] -gt 200 -and $px[$i + 2] -lt 150) { $tinted++ }
}
Check "pixels are green, none white" ($tinted -gt 20 -and $white -eq 0)
Install-Pack -Game $game -Hud $false -Crosshair "scope" -CrosshairSize 75 -CrosshairColor "yellow"
$st = Get-PackStatus $game
Check "yellow scope 75 detected" ($st.Crosshair -eq "scope" -and $st.CrosshairColor -eq "yellow" -and $st.CrosshairSize -eq 75)
Install-Pack -Game $game -Hud $false -Crosshair "scope" -CrosshairSize 150 -CrosshairColor "red"
Check "red scope 150 detected" ((Get-PackStatus $game).CrosshairSize -eq 150 -and (Get-PackStatus $game).CrosshairColor -eq "red")
Install-Pack -Game $game -Hud $false -Crosshair "dot_ring"
Check "back to white" ((Get-PackStatus $game).CrosshairColor -eq "white" -and (Get-PackStatus $game).Crosshair -eq "dot_ring")
Restore-Pack -Game $game | Out-Null
Check "coloured: restore gives vanilla" ((Get-Sha "$game\data\ui\textures\mouse_target.dds") -eq (Get-Sha "$vanilla\mouse_target.dds"))

"== game with the earlier crosshair pack (like yours)"
$game = New-FakeGame (Join-Path $root "textures\crosshairs\cross")
Check "detected as cross" ((Get-PackStatus $game).Crosshair -eq "cross")
Install-Pack -Game $game -Hud $true -Crosshair "gap_cross"
Restore-Pack -Game $game | Out-Null
Check "restore returns the earlier pack's cross" ((Get-PackStatus $game).Crosshair -eq "cross")

"== resized crosshair: both textures, detection, switching and restore"
$game = New-FakeGame $vanilla
Install-Pack -Game $game -Hud $false -Crosshair "scope" -CrosshairSize 75
$st = Get-PackStatus $game
Check "size detected as 75" ($st.Crosshair -eq "scope" -and $st.CrosshairSize -eq 75)
foreach ($name in "mouse_target.dds", "mouse_target_painted.dds") {
    Check "sized $name installed" ((Get-Sha "$game\data\ui\textures\$name") -eq (Get-Sha "$root\setup\payload\crosshairs\scope\sizes\75\$name"))
}
Install-Pack -Game $game -Hud $false -Crosshair "fine_cross" -CrosshairSize 200
Check "size switch detected" ((Get-PackStatus $game).CrosshairSize -eq 200)
$config = "$game\data\db\constants_user.lua"
Check "200% geometry keeps cursor centred" ((Get-Content $config -Raw) -match 'Size = \{ 64, 64 \}' -and (Get-Content $config -Raw) -match 'Offset = \{ -32, -32 \}')
$status = Get-PackStatus $game
Check "200% preview matches full texture" ($status.Crosshair -eq 'fine_cross')
$compiler = 'C:\Program Files (x86)\Steam\steamapps\common\Forts\LuaPlusC.exe'
if (Test-Path -LiteralPath $compiler) {
    & $compiler -p $config
    Check 'generated cursor config passes LuaPlusC' ($LASTEXITCODE -eq 0)
}
$hashBefore = Get-Sha "$game\data\ui\textures\mouse_target.dds"
$refused = $false
try { Install-Pack -Game $game -Hud $false -Crosshair "scope" -CrosshairSize 123 } catch { $refused = $true }
Check "invalid size refused without writes" ($refused -and $hashBefore -eq (Get-Sha "$game\data\ui\textures\mouse_target.dds"))
Restore-Pack -Game $game | Out-Null
Check "resized crosshair restores vanilla" ((Get-Sha "$game\data\ui\textures\mouse_target.dds") -eq (Get-Sha "$vanilla\mouse_target.dds"))
Check "new user config removed on restore" (-not (Test-Path $config))

"== preserve user settings through large/small size changes"
$game = New-FakeGame $vanilla
New-Item -ItemType Directory -Force "$game\data\db" | Out-Null
$config = "$game\data\db\constants_user.lua"
Set-Content $config 'Game = { SkipIntro = true }; View = { TargetIcon = { Size = 40 } }' -Encoding UTF8
$originalConfig = Get-Sha $config
foreach ($size in @(200,150,50)) {
    Install-Pack -Game $game -Hud $false -Crosshair 'scope' -CrosshairSize $size
    Check "detect scope at $size percent" ((Get-PackStatus $game).CrosshairSize -eq $size)
    Check "preserve unrelated config at $size percent" ((Get-Content $config -Raw) -match 'SkipIntro = true')
}
Check 'large-size override removed at 50 percent' ((Get-Content $config -Raw) -notmatch 'BEGIN FORTS SETUP CURSOR SIZE')
Restore-Pack -Game $game | Out-Null
Check 'user config restored byte for byte' ((Get-Sha $config) -eq $originalConfig)

Remove-Item -Recurse -Force (Join-Path $root "build\fakegame-setup")
if ($fail) { "FAILED: $fail"; exit 1 } else { "ALL PASSED" }
