# Install / restore logic for the Forts compact HUD + crosshair pack. No UI here:
# setup.ps1 drives it, and it can be dot-sourced and tested on its own.
#
# Backup lives in <Forts>\Forts-Setup-Backup: the game's own files as they were
# before the first install, plus state.json with a hash per file. Restore puts a
# file back only if it still holds what we installed, so a Steam update is never
# overwritten.

$script:PackRoot = Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot "strings.ps1")
$script:BackupName = "Forts-Setup-Backup"

# Forts 1.39.0 r22745: original hashes of the files the HUD replaces.
$script:HudFiles = [ordered]@{
    "data\ui\screens\hud.lua"         = "1E0004F7EF8BBA8F8C4CA4BD897D32EB229A0CF1F06905A30BABC3359ED9893C"
    "data\ui\uisprites.lua"           = "45A88437D1A0FD7F18131BAE62A6BB098EDF639B8B331610A7DC1B1CF6FB4E49"
    "data\ui\screens\transaction.lua" = "7D31332AEEE2FB7F0929D3D4E044590013BA8CF0DA28FE225C0FB5B8AC63FBBB"
}
$script:HudTextureDir = "data\ui\textures\HUD\custom"
$script:CrosshairFiles = @("data\ui\textures\mouse_target.dds", "data\ui\textures\mouse_target_painted.dds")
$script:CrosshairSizes = @(50..200 | Where-Object { $_ % 5 -eq 0 })
$script:CursorSizeBlock = '(?s)\r?\n-- BEGIN FORTS SETUP CURSOR SIZE\r?\n.*?-- END FORTS SETUP CURSOR SIZE\r?\n'

function Get-CursorConfigPlan([string]$Game, [int]$Percent) {
    $rel = 'data\db\constants_user.lua'
    $path = Join-Path $Game $rel
    $text = if (Test-Path -LiteralPath $path) { [IO.File]::ReadAllText($path) } else { '' }
    $hadBlock = $text -match $script:CursorSizeBlock
    if ($Percent -le 110 -and -not $hadBlock) { return }
    $text = [regex]::Replace($text, $script:CursorSizeBlock, '')
    if ($Percent -gt 110) {
        $width = (32 * $Percent / 100).ToString('0.###', [Globalization.CultureInfo]::InvariantCulture)
        $offset = (-16 * $Percent / 100).ToString('0.###', [Globalization.CultureInfo]::InvariantCulture)
        $text += @"

-- BEGIN FORTS SETUP CURSOR SIZE
-- Forts Setup size: $Percent
if Input and Input.Mouse and Input.Mouse.Cursors then
    for _, cursor in ipairs(Input.Mouse.Cursors) do
        if cursor.Sprite == "ui/textures/mouse_target.dds" or cursor.Sprite == "ui/textures/mouse_target_painted.dds" then
            cursor.Size = { $width, $width }
            cursor.Offset = { $offset, $offset }
        end
    end
end
-- END FORTS SETUP CURSOR SIZE

"@
    }
    @{ rel = $rel; bytes = [Text.UTF8Encoding]::new($false).GetBytes($text) }
}

# HUD layouts: our compact one, the same in Zors.o's purple theme, and nutellafish's
# Minimalist HUD (Steam Workshop 3750502564, "you can copy anything in this mod").
$script:Layouts = [ordered]@{ compact = "Компактный"; zors = "Zors.o"; minimalist = "Minimalist" }

# World icons whose opacity can be set (the grey ones are their disabled look).
$script:IconNames = [ordered]@{ extinguisher = "Тушение"; under_construction = "Строительство"; repair = "Ремонт" }
$script:IconFiles = @("extinguisher", "under_construction", "under_construction_grey", "repair", "repair_grey") |
    ForEach-Object { "data\ui\textures\$_.dds" }

# Crosshair colours: white art times the colour, so the dark outline stays dark.
$script:CrosshairColors = [ordered]@{
    white = "FFFFFF"; green = "5CFF6E"; yellow = "FFE04A"; red = "FF5A4E"; cyan = "46E6FF"
}
Add-Type @"
public static class FortsTint {
  // Uncompressed 32-bit DDS (B,G,R,A after the 128-byte header) multiplied by a colour.
  public static byte[] Apply(byte[] dds, int r, int g, int b) {
    if (dds.Length < 132 || (System.BitConverter.ToInt32(dds, 80) & 0x40) == 0 || System.BitConverter.ToInt32(dds, 88) != 32)
      throw new System.ArgumentException("not an uncompressed 32-bit DDS");
    var o = (byte[])dds.Clone();
    MultiplyBgra(o, 128, r, g, b);
    return o;
  }
  // Same DDS with its alpha scaled to percent (icon opacity).
  public static byte[] ScaleAlpha(byte[] dds, int percent) {
    var o = Apply(dds, 255, 255, 255);
    for (int i = 131; i < o.Length; i += 4) o[i] = (byte)(o[i] * percent / 100);
    return o;
  }
  public static void MultiplyBgra(byte[] px, int start, int r, int g, int b) {
    for (int i = start; i + 3 < px.Length; i += 4) {
      px[i] = (byte)(px[i] * b / 255); px[i + 1] = (byte)(px[i + 1] * g / 255); px[i + 2] = (byte)(px[i + 2] * r / 255);
    }
  }
}
"@

function Get-TintedCrosshair([string]$Dir, [string]$Color) {
    # Compressed art (the earlier pack's cross) ships an uncompressed tint.dds next to it.
    $source = Join-Path $Dir "tint.dds"
    if (-not (Test-Path -LiteralPath $source)) { $source = Join-Path $Dir "mouse_target.dds" }
    $hex = $script:CrosshairColors[$Color]
    [FortsTint]::Apply([IO.File]::ReadAllBytes($source), [Convert]::ToInt32($hex.Substring(0, 2), 16),
        [Convert]::ToInt32($hex.Substring(2, 2), 16), [Convert]::ToInt32($hex.Substring(4, 2), 16))
}

function Get-Sha([string]$Path) {
    if (Test-Path -LiteralPath $Path -PathType Leaf) { (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash } else { $null }
}

function Find-Forts {
    $libraries = @()
    foreach ($key in "HKCU:\Software\Valve\Steam", "HKLM:\SOFTWARE\WOW6432Node\Valve\Steam") {
        $reg = Get-ItemProperty $key -ErrorAction SilentlyContinue
        if ($reg.SteamPath) { $libraries += $reg.SteamPath }
        if ($reg.InstallPath) { $libraries += $reg.InstallPath }
    }
    $libraries += "${env:ProgramFiles(x86)}\Steam"
    foreach ($lib in @($libraries)) {
        $vdf = Join-Path $lib "steamapps\libraryfolders.vdf"
        if (Test-Path -LiteralPath $vdf) {
            foreach ($m in [regex]::Matches((Get-Content -LiteralPath $vdf -Raw), '"path"\s+"([^"]+)"')) {
                $libraries += $m.Groups[1].Value.Replace('\\', '\')
            }
        }
    }
    foreach ($lib in $libraries | Select-Object -Unique) {
        $candidate = Join-Path $lib "steamapps\common\Forts"
        if (Test-Path -LiteralPath (Join-Path $candidate "Forts.exe")) { return (Resolve-Path -LiteralPath $candidate).Path }
    }
    return $null
}

function Test-FortsFolder([string]$Game) {
    $Game -and (Test-Path -LiteralPath (Join-Path $Game "Forts.exe") -PathType Leaf) -and
        (Test-Path -LiteralPath (Join-Path $Game "data\ui\screens") -PathType Container)
}

function Test-FortsRunning([string]$Game) {
    $exe = (Resolve-Path -LiteralPath (Join-Path $Game "Forts.exe")).Path
    [bool](Get-Process Forts -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $exe })
}

# Our buttons (pause, menu, weapon facing) come in one set per HUD skin; dlc3 is the
# set in payload\hud, the rest in payload\skins\<skin>.
$script:HudSkins = [ordered]@{ dlc3 = "Firebird"; base = "Стандартный"; dlc1 = "High Seas"; dlc2 = "Moonshot"; pro = "PRO"; elite = "Elite" }

function Get-GameHudSkin([string]$Game) {
    <# The HUD skin chosen in the game (CurrentHUDSkin in users\<id>\settings.lua). #>
    $file = Get-ChildItem -LiteralPath (Join-Path $Game "users") -Filter "settings.lua" -Recurse -Depth 1 -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $file) { return $null }
    $m = [regex]::Match([IO.File]::ReadAllText($file.FullName), 'CurrentHUDSkin\s*=\s*"db/skins_hud_?([a-z0-9]*)')
    if (-not $m.Success) { return $null }
    $key = ($m.Groups[1].Value -replace '_tester$', '')
    if ($script:HudSkins.Contains($key)) { $key } else { "base" }
}

function Get-HudSkinTextures([string]$Skin) {
    if ($Skin -eq "dlc3") { Join-Path $script:PackRoot "payload\hud\$script:HudTextureDir" }
    else { Join-Path $script:PackRoot "payload\skins\$Skin" }
}

function Get-GameScreen([string]$Game) {
    <# The game's resolution from the newest users\<id>\settings_hardware.lua; the
       compact HUD is laid out for 16:9. Returns $null when it cannot be read. #>
    $file = Get-ChildItem -LiteralPath (Join-Path $Game "users") -Filter "settings_hardware.lua" -Recurse -Depth 1 -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $file) { return $null }
    $text = [IO.File]::ReadAllText($file.FullName)
    $w = [regex]::Match($text, 'ViewWidth\s*=\s*(\d+)'); $h = [regex]::Match($text, 'ViewHeight\s*=\s*(\d+)')
    if ($text -match 'AutoResolution\s*=\s*true' -or -not $w.Success -or -not $h.Success) {
        Add-Type -AssemblyName System.Windows.Forms
        $b = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
        $width, $height = $b.Width, $b.Height
    } else {
        $width, $height = [int]$w.Groups[1].Value, [int]$h.Groups[1].Value
    }
    if ($height -le 0) { return $null }
    [pscustomobject]@{ Width = $width; Height = $height; Wide169 = [math]::Abs($width / $height - 16 / 9) -lt 0.02 }
}

function Get-Crosshairs {
    Get-ChildItem -LiteralPath (Join-Path $script:PackRoot "payload\crosshairs") -Directory | Select-Object -ExpandProperty Name
}

function Read-State([string]$Game) {
    $path = Join-Path $Game "$script:BackupName\state.json"
    if (-not (Test-Path -LiteralPath $path)) { return @{} }
    $state = @{}
    foreach ($entry in (Get-Content -LiteralPath $path -Raw | ConvertFrom-Json)) {
        $owner = $entry.owner
        if (-not $owner) { $owner = Get-DefaultOwner $entry.path }  # state from before owners were recorded
        $state[$entry.path] = @{ path = $entry.path; existed = [bool]$entry.existed; original = $entry.original
            installed = $entry.installed; owner = $owner }
    }
    return $state
}

function Get-DefaultOwner([string]$Rel) {
    if ($Rel -in $script:CrosshairFiles -or $Rel -eq 'data\db\constants_user.lua') { "crosshair" }
    elseif ($Rel -in $script:IconFiles) { "icons" }
    else { "hud" }
}

function Write-State([string]$Game, [hashtable]$State) {
    $path = Join-Path $Game "$script:BackupName\state.json"
    @($State.Values | Sort-Object { $_.path } | ForEach-Object { [pscustomobject]$_ }) |
        ConvertTo-Json -Depth 3 | Set-Content -LiteralPath $path -Encoding UTF8
}

function Get-PackStatus([string]$Game) {
    <# What is installed now and whether the HUD fits this game version. #>
    $state = Read-State $Game
    $hudOk = $true
    foreach ($rel in $script:HudFiles.Keys) {
        $current = Get-Sha (Join-Path $Game $rel)
        $ours = Get-Sha (Join-Path $script:PackRoot "payload\hud\$rel")
        $previousHud = $rel -eq 'data\ui\screens\hud.lua' -and
            $current -in @('93BD9A12D9EF1D53F454DEF18D3611FB7BA0E9A5E30D9C7E1CCAF733789D743D', '08ED3730AB95598A279FB85F594E45C29232704A20B5F8986010260A92B0653E')
        $previousHud = $previousHud -or ($rel -eq 'data\ui\screens\hud.lua' -and $current -eq 'EC8BF7E1BCB5923343A3C265DB7B0769A297A814472F482E1CD4F281F802105B') -or ($rel -eq 'data\ui\uisprites.lua' -and $current -eq '324F43C990EC0757B921A641670FF66A20CDE22BE9E5EBD1DBA8A61BB13F6FC4')
        # Any older build of this installer: the file still holds what it wrote.
        $previousHud = $previousHud -or ($state.ContainsKey($rel) -and $current -and $current -eq $state[$rel].installed)
        # One of our other layouts (also when put in by hand, e.g. Minimalist from the Workshop).
        $previousHud = $previousHud -or ($rel -eq 'data\ui\screens\hud.lua' -and $current -and $current -in @(
            "zors", "minimalist" | ForEach-Object { Get-Sha (Join-Path $script:PackRoot "payload\layouts\$_\hud.lua") }))
        if ($current -ne $script:HudFiles[$rel] -and $current -ne $ours -and -not $previousHud) { $hudOk = $false }
    }
    # Which layout is in place, by its hud.lua.
    $hudNow = Get-Sha (Join-Path $Game 'data\ui\screens\hud.lua')
    $layout = $null
    foreach ($key in $script:Layouts.Keys) {
        $file = $(if ($key -eq "compact") { "payload\hud\data\ui\screens\hud.lua" } else { "payload\layouts\$key\hud.lua" })
        if ($hudNow -and $hudNow -eq (Get-Sha (Join-Path $script:PackRoot $file))) { $layout = $key }
    }
    $hudInstalled = [bool]$layout
    if ($layout -and $layout -ne "minimalist") {
        foreach ($rel in @($script:HudFiles.Keys | Select-Object -Skip 1)) {
            if ((Get-Sha (Join-Path $Game $rel)) -ne (Get-Sha (Join-Path $script:PackRoot "payload\hud\$rel"))) { $hudInstalled = $false }
        }
    }
    # Icon opacity: what the last install recorded, while those files are still ours.
    $icons = @{}
    foreach ($name in $script:IconNames.Keys) { $icons[$name] = 100 }
    $iconsFile = Join-Path $Game "$script:BackupName\icons.json"
    if (Test-Path -LiteralPath $iconsFile) {
        $saved = Get-Content -LiteralPath $iconsFile -Raw | ConvertFrom-Json
        foreach ($name in $script:IconNames.Keys) {
            $rel = "data\ui\textures\$name.dds"
            if ($saved.$name -and $state.ContainsKey($rel) -and $state[$rel].owner -eq "icons" -and
                (Get-Sha (Join-Path $Game $rel)) -eq $state[$rel].installed) { $icons[$name] = [int]$saved.$name }
        }
    }
    # Which skin's button set is installed: compare one of our textures.
    $hudSkin = $null
    $pause = Get-Sha (Join-Path $Game "$script:HudTextureDir\Pause-A.dds")
    foreach ($skin in $script:HudSkins.Keys) {
        if ($pause -and $pause -eq (Get-Sha (Join-Path (Get-HudSkinTextures $skin) "Pause-A.dds"))) { $hudSkin = $skin }
    }
    $crosshair = $null
    $crosshairSize = 100
    $current = Get-Sha (Join-Path $Game $script:CrosshairFiles[0])
    foreach ($id in Get-Crosshairs) {
        foreach ($size in ($script:CrosshairSizes | Where-Object { $_ -le 110 })) {
            $dir = Join-Path $script:PackRoot "payload\crosshairs\$id"
            if ($size -ne 100) { $dir = Join-Path $dir "sizes\$size" }
            if ($current -and $current -eq (Get-Sha (Join-Path $dir "mouse_target.dds")) -and
                (Get-Sha (Join-Path $Game $script:CrosshairFiles[1])) -eq (Get-Sha (Join-Path $dir "mouse_target_painted.dds"))) {
                $crosshair = $id; $crosshairSize = $size
            }
        }
    }
    $crosshairColor = "white"
    $recorded = Join-Path $Game "$script:BackupName\crosshair.json"
    if (-not $crosshair -and $current -and (Test-Path -LiteralPath $recorded)) {
        # A tinted crosshair matches no payload file; the last install recorded it.
        $info = Get-Content -LiteralPath $recorded -Raw | ConvertFrom-Json
        if ($info.hash -eq $current -and $info.id -in @(Get-Crosshairs) -and $script:CrosshairColors.Contains([string]$info.color)) {
            $crosshair = $info.id; $crosshairSize = [int]$info.size; $crosshairColor = $info.color
        }
    }
    $config = Join-Path $Game 'data\db\constants_user.lua'
    if ($crosshair -and $crosshairSize -eq 100 -and (Test-Path -LiteralPath $config)) {
        $block = [regex]::Match([IO.File]::ReadAllText($config), $script:CursorSizeBlock).Value
        $match = [regex]::Match($block, '-- Forts Setup size: (\d+)')
        if ($match.Success -and [int]$match.Groups[1].Value -in $script:CrosshairSizes -and [int]$match.Groups[1].Value -gt 110) {
            $crosshairSize = [int]$match.Groups[1].Value
        }
    }
    [pscustomobject]@{
        HudCompatible = $hudOk
        HudInstalled  = $hudInstalled
        Layout        = $layout
        Icons         = $icons
        HudSkin       = $hudSkin
        Crosshair     = $crosshair
        CrosshairSize = $crosshairSize
        CrosshairColor = $crosshairColor
        HasBackup    = $state.Count -gt 0
    }
}

function Install-Pack {
    <# Installs the HUD and/or a crosshair. $Progress gets (step, total, text). #>
    param([string]$Game, [bool]$Hud, [string]$Crosshair, [scriptblock]$Progress = {}, [int]$CrosshairSize = 100,
          [string]$CrosshairColor = "white", [string]$HudSkin = "dlc3", [string]$Layout = "compact",
          [hashtable]$Icons = $null)  # $Icons: extinguisher/under_construction/repair -> opacity %, $null = leave

    if (-not (Test-FortsFolder $Game)) { throw (L "В этой папке нет Forts.exe.") }
    if (Test-FortsRunning $Game) { throw (L "Закрой Forts и попробуй ещё раз.") }
    if (-not $Hud -and -not $Crosshair -and -not $Icons) { throw (L "Ничего не выбрано.") }

    $plan = @()  # @{ rel; source | bytes; owner }
    if ($Hud) {
        $status = Get-PackStatus $Game
        if (-not $status.HudCompatible) {
            throw (L "HUD сделан для Forts 1.39.0 r22745, а файлы интерфейса у тебя другие (другая версия игры или чужой мод).")
        }
        if (-not $script:Layouts.Contains($Layout)) { throw (L "Неизвестный HUD.") }
        $layoutDir = Join-Path $script:PackRoot "payload\layouts\$Layout"
        if ($Layout -eq "minimalist") {
            # nutellafish's own hud.lua and textures; the game's uisprites and transaction stay.
            $plan += @{ rel = 'data\ui\screens\hud.lua'; source = (Join-Path $layoutDir "hud.lua"); owner = "hud" }
        } else {
            foreach ($rel in $script:HudFiles.Keys) { $plan += @{ rel = $rel; source = (Join-Path $script:PackRoot "payload\hud\$rel"); owner = "hud" } }
            if ($Layout -eq "zors") { $plan[0].source = Join-Path $layoutDir "hud.lua" }  # same layout, purple colours
            if (-not $script:HudSkins.Contains($HudSkin)) { throw (L "Неизвестный скин HUD.") }
            foreach ($tex in Get-ChildItem -LiteralPath (Get-HudSkinTextures $HudSkin) -File) {
                $plan += @{ rel = (Join-Path $script:HudTextureDir $tex.Name); source = $tex.FullName; owner = "hud" }
            }
        }
        $textures = Join-Path $layoutDir "textures"
        if (Test-Path -LiteralPath $textures) {
            foreach ($tex in Get-ChildItem -LiteralPath $textures -File -Recurse) {
                $plan += @{ rel = (Join-Path "data\ui\textures" $tex.FullName.Substring($textures.Length + 1)); source = $tex.FullName; owner = "hud" }
            }
        }
    }
    if ($Icons) {
        # Opacity is applied to the icons of the layout being installed (or the one in place).
        $iconLayout = $(if ($Hud) { $Layout } else { $s = (Get-PackStatus $Game).Layout; if ($s) { $s } else { "compact" } })
        foreach ($name in $script:IconNames.Keys) {
            if (-not $Icons.ContainsKey($name)) { continue }
            $percent = [int]$Icons[$name]
            if ($percent -ge 100) {
                # Fully opaque: the layout's own icon if it has one (Zors.o, Minimalist),
                # otherwise the game's, which the revert below puts back.
                foreach ($file in @($name, "${name}_grey")) {
                    $own = Join-Path $script:PackRoot "payload\layouts\$iconLayout\textures\$file.dds"
                    if (Test-Path -LiteralPath $own) { $plan += @{ rel = "data\ui\textures\$file.dds"; source = $own; owner = "hud" } }
                }
                continue
            }
            if ($percent -lt 10) { throw (L "Прозрачность иконок: от 10 до 100%.") }
            foreach ($file in @($name, "${name}_grey")) {
                $src = Join-Path $script:PackRoot "payload\icons\$iconLayout\$file.dds"
                if (-not (Test-Path -LiteralPath $src)) { continue }
                $plan += @{ rel = "data\ui\textures\$file.dds"; bytes = [FortsTint]::ScaleAlpha([IO.File]::ReadAllBytes($src), $percent); owner = "icons" }
            }
        }
    }
    if ($Crosshair) {
        if ($CrosshairSize -notin $script:CrosshairSizes) { throw (L "Размер прицела должен быть от 50 до 200%, с шагом 5%.") }
        if ($Crosshair -notin @(Get-Crosshairs)) { throw (L "Неизвестный прицел.") }
        $dir = Join-Path $script:PackRoot "payload\crosshairs\$Crosshair"
        if ($CrosshairSize -ne 100 -and $CrosshairSize -le 110) { $dir = Join-Path $dir "sizes\$CrosshairSize" }
        if (-not (Test-Path -LiteralPath $dir)) { throw (L "Неизвестный прицел.") }
        foreach ($rel in $script:CrosshairFiles) {
            if (-not (Test-Path -LiteralPath (Join-Path $dir (Split-Path $rel -Leaf)) -PathType Leaf)) { throw (L "Не хватает текстуры прицела.") }
        }
        if (-not $script:CrosshairColors.Contains($CrosshairColor)) { throw (L "Неизвестный цвет прицела.") }
        if ($CrosshairColor -eq "white") {
            foreach ($rel in $script:CrosshairFiles) { $plan += @{ rel = $rel; source = (Join-Path $dir (Split-Path $rel -Leaf)); owner = "crosshair" } }
        } else {
            $tinted = Get-TintedCrosshair $dir $CrosshairColor  # normal and painted art are the same
            foreach ($rel in $script:CrosshairFiles) { $plan += @{ rel = $rel; bytes = $tinted; owner = "crosshair" } }
        }
        $configPlan = Get-CursorConfigPlan $Game $CrosshairSize
        if ($configPlan) { $configPlan.owner = "crosshair"; $plan += $configPlan }
    }

    $backup = Join-Path $Game $script:BackupName
    New-Item -ItemType Directory -Force -Path $backup | Out-Null
    $state = Read-State $Game
    # Files a previous layout (or icon setting) put in that this install does not:
    # back to the game's own, so switching layouts leaves nothing of the old one.
    $replacing = @()
    if ($Hud) { $replacing += "hud" }
    if ($Icons) { $replacing += "icons" }
    $planned = @($plan | ForEach-Object { $_.rel })
    $revert = @($state.Values | Where-Object { $_.owner -in $replacing -and $_.path -notin $planned })
    $total = $revert.Count + $plan.Count + 1
    $step = 0
    $written = @()
    try {
        foreach ($entry in $revert) {
            $step++
            & $Progress $step $total ((L "Возвращаю {0}") -f (Split-Path $entry.path -Leaf))
            $target = Join-Path $Game $entry.path
            if ((Get-Sha $target) -eq $entry.installed) {  # untouched since: put the game's file back
                $written += @{ rel = $entry.path; before = [IO.File]::ReadAllBytes($target) }
                if ($entry.existed) { Copy-Item -LiteralPath (Join-Path $backup $entry.path) -Destination $target -Force }
                else { Remove-Item -LiteralPath $target }
            }
            $state.Remove($entry.path)
        }
        foreach ($item in $plan) {
            $step++
            & $Progress $step $total ((L "Копирую {0}") -f (Split-Path $item.rel -Leaf))
            $target = Join-Path $Game $item.rel
            if (-not $state.ContainsKey($item.rel)) {
                # First time we touch this file: keep the game's own copy.
                $existed = Test-Path -LiteralPath $target -PathType Leaf
                if ($existed) {
                    $saved = Join-Path $backup $item.rel
                    New-Item -ItemType Directory -Force -Path (Split-Path $saved) | Out-Null
                    Copy-Item -LiteralPath $target -Destination $saved -Force
                }
                $state[$item.rel] = @{ path = $item.rel; existed = $existed; original = (Get-Sha $target); installed = $null; owner = $null }
            }
            $state[$item.rel].owner = $item.owner
            $written += @{ rel = $item.rel; before = $(if (Test-Path -LiteralPath $target) { [IO.File]::ReadAllBytes($target) } else { $null }) }
            New-Item -ItemType Directory -Force -Path (Split-Path $target) | Out-Null
            if ($item.ContainsKey('bytes')) {
                [IO.File]::WriteAllBytes($target, $item.bytes)
            } else {
                Copy-Item -LiteralPath $item.source -Destination $target -Force
            }
            $state[$item.rel].installed = Get-Sha $target
            if ($item.ContainsKey('bytes')) {
                $sha = [Security.Cryptography.SHA256]::Create()
                try { $expectedHash = [BitConverter]::ToString($sha.ComputeHash($item.bytes)).Replace('-', '') } finally { $sha.Dispose() }
            } else { $expectedHash = Get-Sha $item.source }
            if ($state[$item.rel].installed -ne $expectedHash) { throw ((L "Не удалось записать {0}.") -f $item.rel) }
        }
        $step++
        & $Progress $step $total (L "Сохраняю резервную копию")
        Write-State $Game $state
        if ($Crosshair) {
            [pscustomobject]@{ id = $Crosshair; size = $CrosshairSize; color = $CrosshairColor
                hash = (Get-Sha (Join-Path $Game $script:CrosshairFiles[0])) } |
                ConvertTo-Json | Set-Content -LiteralPath (Join-Path $backup "crosshair.json") -Encoding UTF8
        }
        if ($Icons) {
            [pscustomobject]$Icons | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $backup "icons.json") -Encoding UTF8
        }
    } catch {
        # Roll back this run's writes, newest first, so the game is never left half-installed.
        [array]::Reverse($written)
        foreach ($w in $written) {
            $target = Join-Path $Game $w.rel
            if ($null -ne $w.before) { [IO.File]::WriteAllBytes($target, $w.before) } elseif (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target }
        }
        throw
    }
}

function Restore-Pack {
    <# Puts back every file we changed that still holds our version. Returns skipped paths. #>
    param([string]$Game, [scriptblock]$Progress = {})

    if (-not (Test-FortsFolder $Game)) { throw (L "В этой папке нет Forts.exe.") }
    if (Test-FortsRunning $Game) { throw (L "Закрой Forts и попробуй ещё раз.") }
    $state = Read-State $Game
    if ($state.Count -eq 0) { throw (L "Резервной копии нет: пак сюда не ставился. Оригинал можно вернуть через Steam (Проверить целостность файлов).") }

    $backup = Join-Path $Game $script:BackupName
    $skipped = @()
    $total = $state.Count + 1
    $step = 0
    foreach ($entry in @($state.Values)) {
        $step++
        & $Progress $step $total ((L "Возвращаю {0}") -f (Split-Path $entry.path -Leaf))
        $target = Join-Path $Game $entry.path
        if ((Get-Sha $target) -ne $entry.installed) { $skipped += $entry.path; continue }  # changed since: leave it
        if ($entry.existed) {
            $saved = Join-Path $backup $entry.path
            if ((Get-Sha $saved) -ne $entry.original) { throw ((L "Копия {0} повреждена, остановился.") -f $entry.path) }
            Copy-Item -LiteralPath $saved -Destination $target -Force
        } else {
            Remove-Item -LiteralPath $target
        }
    }
    $step++
    & $Progress $step $total (L "Убираю резервную копию")
    $customDir = Join-Path $Game $script:HudTextureDir
    if ((Test-Path -LiteralPath $customDir) -and -not (Get-ChildItem -LiteralPath $customDir -Force)) { Remove-Item -LiteralPath $customDir }
    Rename-Item -LiteralPath $backup -NewName ("$script:BackupName-restored-" + (Get-Date -Format "yyyyMMdd-HHmmss"))
    return $skipped
}
