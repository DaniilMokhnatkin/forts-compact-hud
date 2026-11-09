# Forts Setup: installer window for the compact HUD and crosshairs (WPF, macOS Tahoe look).
# -Game <folder> skips auto-detection; -State progress|success|error|cross|backup opens on that
# screen (for screenshots); -NoMotion forces reduced motion.
param([string]$Game, [string]$State, [switch]$NoMotion, [ValidateSet("", "ru", "en")][string]$Lang)
$ErrorActionPreference = "Stop"
$assets = Join-Path $PSScriptRoot "assets"
$script:Lang = $Lang
. (Join-Path $PSScriptRoot "engine.ps1")
if (-not $Lang) { $script:Lang = Get-DefaultLang $(if ($Game) { $Game } else { Find-Forts }) }
. (Join-Path $PSScriptRoot "ui.ps1")
$reduceMotion = $NoMotion -or -not [System.Windows.SystemParameters]::ClientAreaAnimation

$crosshairNames = [ordered]@{
    cross = (L "Крест"); gap_cross = (L "Разрыв"); fine_cross = (L "Тонкий"); bold_cross = (L "Жирный")
    dot = (L "Точка"); dot_ring = (L "Кольцо"); double_ring = (L "Два кольца")
    brackets = (L "Рамка"); square = (L "Квадрат"); diamond = (L "Ромб")
    chevron = (L "Шеврон"); chevron_double = (L "Два шеврона"); x_cross = (L "Икс"); t_cross = (L "Т-образный")
    scope = (L "Оптика"); sniper = (L "Снайпер"); mildot = (L "Мил-дот"); arcade = (L "Аркада"); zors = "Zors.o"
}

$window, $ui = Load-Window (Join-Path $PSScriptRoot "xaml") @{ SPRING = $(if ($reduceMotion) { "0:0:0" } else { "0:0:0.32" }) }
Enable-Glass $window $ui.Tint

# ---------------------------------------------------------------- content
$ui.BeforeImage.Source = Image-From "crosshair_vanilla.png"

# Preview backgrounds: the crosshair has to read on all of them.
$sceneNames = [ordered]@{ scene = (L "Бой"); bg_sky = (L "Небо"); bg_rocks = (L "Скалы"); bg_fire = (L "Огонь") }
foreach ($file in $sceneNames.Keys) {
    $seg = [Windows.Controls.RadioButton]::new()
    $seg.Style = $window.FindResource("SceneSeg")
    $seg.Content = $sceneNames[$file]
    $seg.Tag = Image-From "$file.png"
    [Windows.Automation.AutomationProperties]::SetName($seg, (L "Фон: ") + $sceneNames[$file])
    $seg.Add_Checked({ param($s) $ui.SceneBefore.Source = $s.Tag; $ui.SceneAfter.Source = $s.Tag })
    $ui.Scenes.Children.Add($seg) | Out-Null
}
$ui.Scenes.Children[0].IsChecked = $true

# HUD layouts: compact (ours), Zors.o (ours in purple), Minimalist (nutellafish).
$layoutInfo = @{
    compact    = @{ title = (L "Компактный · наш"); text = (L "Ресурсы «запас / лимит» по центру, кнопки в один ряд, иконки на 20% меньше."); preview = $null }
    zors       = @{ title = (L "Zors.o · фиолетовая тема"); text = (L "Наш компактный HUD в космическом фиолетовом: кнопки, вкладки, командир, реактор, иконки и прицел; фон меню и заставка от Zors.o."); preview = "preview_zors.png" }
    minimalist = @{ title = (L "Minimalist · автор nutellafish"); text = (L "Прозрачный минимальный интерфейс; убранные кнопки нажимаются горячими клавишами."); preview = "preview_minimalist.png" }
}
$layoutLinks = @{
    zors       = @{ text = (L "Профиль Steam"); url = "https://steamcommunity.com/profiles/76561199099877561" }
    minimalist = @{ text = (L "Мастерская Steam"); url = "https://steamcommunity.com/sharedfiles/filedetails/?id=3750502564" }
}
$layoutSegs = @{}
foreach ($key in $Layouts.Keys) {
    $seg = [Windows.Controls.RadioButton]::new()
    $seg.Style = $window.FindResource("LayoutSeg")
    $seg.Content = L $Layouts[$key]
    [Windows.Automation.AutomationProperties]::SetName($seg, "HUD: " + (L $Layouts[$key]))
    $seg.Add_Checked({ param($s) Show-Layout ($layoutSegs.GetEnumerator() | Where-Object { $_.Value -eq $s }).Key })
    $ui.LayoutPicker.Children.Add($seg) | Out-Null
    $layoutSegs[$key] = $seg
}
function Selected-Layout {
    $k = ($layoutSegs.GetEnumerator() | Where-Object { $_.Value.IsChecked } | Select-Object -First 1).Key
    if ($k) { $k } else { "compact" }
}
$script:settingUp = $true  # Refresh picks the installed layout; only a user's pick changes the crosshair default
# The window's look follows the picked HUD: Zors.o turns it purple, the others back.
$themes = @{
    default = @{ Accent = "#0A7AFF"; Group = "#F5F5F7"; Separator = "#E5E5EA"; Tint = "#B8F4F4F7"; Card = "#FFFFFF"; Art = 0.0 }
    zors    = @{ Accent = "#9D4DFF"; Group = "#F1E9FD"; Separator = "#E1D3F5"; Tint = "#A6EDE3FB"; Card = "#FDFBFF"; Art = 0.30 }
}
$ui.ThemeArt.Source = Image-From "theme_zors.jpg"
# Brushes freeze once WPF uses them, so the colours are tweened by a timer that puts a new
# brush in place each frame: Accent, Group and Separator are DynamicResource in the XAML,
# so every template (triggers included) follows; Tint and Card are set directly.
$script:theme = "default"
$script:themeNow = @{}
foreach ($k in "Accent", "Group", "Separator", "Tint", "Card") {
    $script:themeNow[$k] = [Windows.Media.ColorConverter]::ConvertFromString($themes.default[$k])
}
function Set-ThemeColours([hashtable]$colours) {
    foreach ($k in "Accent", "Group", "Separator") { $window.Resources[$k] = [Windows.Media.SolidColorBrush]::new($colours[$k]) }
    $ui.Tint.Background = [Windows.Media.SolidColorBrush]::new($colours.Tint)
    $ui.Card.Background = [Windows.Media.SolidColorBrush]::new($colours.Card)
}
function Set-Theme([string]$name) {
    if ($name -eq $script:theme) { return }
    $script:theme = $name
    $t = $themes[$name]
    $script:themeFrom = $script:themeNow.Clone()
    $script:themeTo = @{}
    foreach ($k in $script:themeFrom.Keys) { $script:themeTo[$k] = [Windows.Media.ColorConverter]::ConvertFromString($t[$k]) }
    $ms = $(if ($reduceMotion) { 0 } else { 700 })
    Animate $ui.ThemeArt "Opacity" $t.Art ($ms * 1.3)
    if ($script:themeTimer) { $script:themeTimer.Stop() }
    if ($ms -eq 0) { $script:themeNow = $script:themeTo; Set-ThemeColours $script:themeTo; return }
    $script:themeStart = [DateTime]::Now
    $script:themeMs = $ms
    $script:themeTimer = [Windows.Threading.DispatcherTimer]::new()
    $script:themeTimer.Interval = [TimeSpan]::FromMilliseconds(16)
    $script:themeTimer.Add_Tick({
        $p = [math]::Min(1, ([DateTime]::Now - $script:themeStart).TotalMilliseconds / $script:themeMs)
        $e = $(if ($p -lt 0.5) { 4 * $p * $p * $p } else { 1 - [math]::Pow(-2 * $p + 2, 3) / 2 })  # ease in-out
        $now = @{}
        foreach ($k in $script:themeTo.Keys) {
            $a = $script:themeFrom[$k]; $b = $script:themeTo[$k]
            $now[$k] = [Windows.Media.Color]::FromArgb([byte]($a.A + ($b.A - $a.A) * $e), [byte]($a.R + ($b.R - $a.R) * $e),
                [byte]($a.G + ($b.G - $a.G) * $e), [byte]($a.B + ($b.B - $a.B) * $e))
        }
        $script:themeNow = $now
        Set-ThemeColours $now
        if ($p -ge 1) { $script:themeTimer.Stop() }
    })
    $script:themeTimer.Start()
}

function Show-Layout([string]$key) {
    Set-Theme $(if ($key -eq "zors") { "zors" } else { "default" })
    $info = $layoutInfo[$key]
    $ui.LayoutTitle.Text = $info.title
    $ui.LayoutText.Text = $info.text
    $link = $layoutLinks[$key]
    $ui.LayoutLink.Visibility = $(if ($link) { "Visible" } else { "Collapsed" })
    if ($link) { $ui.LayoutLink.Content = $link.text; $ui.LayoutLink.Tag = $link.url }
    $ui.MiniWarn.Visibility = $(if ($key -eq "minimalist") { "Visible" } else { "Collapsed" })
    $ui.ZorsWarn.Visibility = $(if ($key -eq "zors") { "Visible" } else { "Collapsed" })
    if ($key -eq "zors") {
        # DLC menu skins read their background from the DLC packs; loose files do not show there.
        $ui.ZorsMenuText.Text = $(if ($script:menuSkin -in "dlc1", "dlc2", "dlc3") {
            (L "• Сейчас в игре скин меню «{0}», с ним фон Zors.o не виден: DLC-скины берут фон из своих файлов. Выбери в игре: Настройки → Игра → Скин меню → «По умолчанию», «Классика» или «Склад оружия».") -f (L $MenuSkinNames[$script:menuSkin]) }
            else { L "• Фон меню виден со скинами меню «По умолчанию», «Классика» и «Склад оружия»." })
        # Its tabs, commander and reactor replace the Default skin's; DLC HUD skins use their packs.
        $ui.ZorsHudText.Text = $(if ($script:gameSkin -and $script:gameSkin -ne "base") {
            (L "• Сейчас в игре скин HUD «{0}». Чтобы были видны фиолетовые вкладки, командир и реактор, выбери: Настройки → Игра → Скин HUD → «По умолчанию».") -f (L $HudSkins[$script:gameSkin]) }
            else { L "• Скин HUD «По умолчанию» — как нужно: вкладки, командир и реактор будут фиолетовыми." })
    }
    # Zors.o and Minimalist bring their own buttons, so there is no skin set to pick.
    foreach ($n in "SkinRow", "SkinSep") { $ui[$n].Visibility = $(if ($key -in "minimalist", "zors") { "Collapsed" } else { "Visible" }) }
    Show-Gallery $key
    if ($key -eq "minimalist") {
        $ui.MiniSkin.Text = $(if ($script:gameSkin -and $script:gameSkin -ne "base") {
            (L "• Сейчас в игре скин «{0}». Для этого HUD выбери в игре: Настройки → Игра → Скин HUD → «По умолчанию».") -f (L $HudSkins[$script:gameSkin]) }
            else { L "• Нужен скин HUD «По умолчанию» (Настройки → Игра → Скин HUD)." })
    }
    if (-not $script:settingUp -and $key -ne "compact") {
        if ($key -eq "zors" -and $tiles.ContainsKey("zors")) {
            # Zors.o's crosshair is preselected, but any other one (and any size) can be picked.
            $ui.CrossSwitch.IsChecked = $true
            $tiles["zors"].IsChecked = $true
            $ui.CrossNote.Text = L "Прицел Zors.o выбран по умолчанию — можно поменять на любой."
        } else {
            # Minimalist brings its own crosshair; ours replaces it only if asked.
            $ui.CrossSwitch.IsChecked = $false
            $ui.CrossNote.Text = L "У этого HUD свой прицел. Включи, чтобы поставить наш вместо него."
        }
    }
    if ($script:status) { $ui.HudNote.Text = $(if ($script:status.Layout -eq $key) { L "Уже стоит. Переустановка ничего не сломает" } else { L "Оригиналы сохраняются, вернуть можно одной кнопкой" }) }
    Update-IconArt
    if (Get-Command Update-InstallButton -ErrorAction SilentlyContinue) { Update-InstallButton }
}
$ui.LayoutLink.Add_Click({ if ($ui.LayoutLink.Tag -like "https://steamcommunity.com/*") { Start-Process $ui.LayoutLink.Tag } })

# Screenshots per HUD: assets\gallery\<layout>\NN Caption.jpg (made by tools\import_screens.py).
# Without them the old preview strips are used.
function Get-Gallery([string]$key) {
    $dir = Join-Path $assets "gallery\$key"
    $files = @(Get-ChildItem -LiteralPath $dir -File -ErrorAction SilentlyContinue | Where-Object { $_.Extension -in ".jpg", ".png" } | Sort-Object Name)
    if ($files) {
        return @($files | ForEach-Object { @{ path = $_.FullName; caption = ($_.BaseName -replace '^\d+\s*', '') } })
    }
    $fallback = @{ compact = @("hud_after.png", "hud_photo_2.png", "hud_photo_3.png"); zors = @("preview_zors.png"); minimalist = @("minimalist_preview.jpg") }
    @($fallback[$key] | ForEach-Object { @{ path = (Join-Path $assets $_); caption = "" } })
}
function Load-Bitmap([string]$path) {
    $bmp = [Windows.Media.Imaging.BitmapImage]::new()
    $bmp.BeginInit(); $bmp.CacheOption = "OnLoad"; $bmp.UriSource = [Uri]::new($path); $bmp.EndInit(); $bmp.Freeze()
    $bmp
}
$script:gallery = @()
$script:galleryIndex = 0
function Show-Gallery([string]$key) {
    $script:gallery = Get-Gallery $key
    $ui.Photos.Children.Clear()
    for ($i = 0; $i -lt $script:gallery.Count; $i++) {
        $seg = [Windows.Controls.RadioButton]::new()
        $seg.Style = $window.FindResource("SceneSeg")
        $seg.GroupName = "Photo"
        $seg.Content = [string]($i + 1)
        $seg.Tag = $i
        $seg.ToolTip = $script:gallery[$i].caption
        [Windows.Automation.AutomationProperties]::SetName($seg, (L "Фото {0}") -f ($i + 1))
        $seg.Add_Checked({ param($s) Show-Shot $s.Tag })
        $ui.Photos.Children.Add($seg) | Out-Null
    }
    $ui.PhotosBox.Visibility = $(if ($script:gallery.Count -gt 1) { "Visible" } else { "Collapsed" })
    if ($ui.Photos.Children.Count) { $ui.Photos.Children[0].IsChecked = $true } else { Show-Shot 0 }
}
function Show-Shot([int]$i) {
    if (-not $script:gallery.Count) { return }
    $script:galleryIndex = $i
    $shot = $script:gallery[$i]
    $bmp = Load-Bitmap $shot.path
    $brush = [Windows.Media.ImageBrush]::new($bmp)
    $brush.Stretch = "UniformToFill"
    if ($bmp.PixelWidth / $bmp.PixelHeight -lt 3) {
        # a whole screenshot: the frame shows its bottom, where the HUD is
        $brush.Viewbox = [Windows.Rect]::new(0, 0.66, 1, 0.34)
    }
    $ui.HudShot.Background = $brush
    $ui.ShotCaption.Text = $shot.caption
    $ui.ShotCaptionBox.Visibility = $(if ($shot.caption) { "Visible" } else { "Collapsed" })
    Animate $ui.HudShot "Opacity" 1 240 0.4
}

function Open-Viewer {
    <# The chosen HUD's screenshots full screen: arrows or click to go on, Esc to close. #>
    if (-not $script:gallery.Count) { return }
    $v = [Windows.Window]::new()
    $v.WindowStyle = "None"; $v.ResizeMode = "NoResize"; $v.WindowState = "Maximized"; $v.Topmost = $true
    $v.Background = [Windows.Media.Brushes]::Black; $v.Owner = $window; $v.ShowInTaskbar = $false; $v.Title = "Forts Setup"
    $grid = [Windows.Controls.Grid]::new()
    $img = [Windows.Controls.Image]::new(); $img.Stretch = "Uniform"
    [Windows.Media.RenderOptions]::SetBitmapScalingMode($img, "HighQuality")
    $bar = [Windows.Controls.Border]::new()
    $bar.Background = [Windows.Media.BrushConverter]::new().ConvertFrom("#B01C1C1E"); $bar.CornerRadius = [Windows.CornerRadius]::new(14)
    $bar.Padding = [Windows.Thickness]::new(18, 8, 18, 8); $bar.Margin = [Windows.Thickness]::new(24)
    $bar.HorizontalAlignment = "Center"; $bar.VerticalAlignment = "Bottom"
    $text = [Windows.Controls.TextBlock]::new(); $text.Foreground = [Windows.Media.Brushes]::White; $text.FontSize = 15
    $bar.Child = $text
    $grid.Children.Add($img) | Out-Null; $grid.Children.Add($bar) | Out-Null
    $v.Content = $grid
    $script:viewer = @{ window = $v; image = $img; text = $text; i = $script:galleryIndex }
    Step-Viewer 0
    $v.Add_KeyDown({ param($s, $e)
        switch ($e.Key) { "Escape" { $script:viewer.window.Close() } "Left" { Step-Viewer -1 } "Right" { Step-Viewer 1 } "Space" { Step-Viewer 1 } }
    })
    $v.Add_MouseLeftButtonUp({ Step-Viewer 1 })
    $v.Add_MouseRightButtonUp({ $script:viewer.window.Close() })
    $v.ShowDialog() | Out-Null
    if ($ui.Photos.Children.Count) { $ui.Photos.Children[$script:viewer.i].IsChecked = $true }
}
$ui.FullScreenBtn.Add_Click({ Open-Viewer })
$ui.HudShot.Add_MouseLeftButtonUp({ Open-Viewer })

function Step-Viewer([int]$d) {
    $n = $script:gallery.Count
    $script:viewer.i = ($script:viewer.i + $d + $n) % $n
    $shot = $script:gallery[$script:viewer.i]
    $script:viewer.image.Source = Load-Bitmap $shot.path
    $parts = @((L $Layouts[(Selected-Layout)]) + " · $($script:viewer.i + 1) / $n")
    if ($shot.caption) { $parts += $shot.caption }
    $parts += L "← → листать · Esc закрыть"
    $script:viewer.text.Text = $parts -join "   ·   "
}

# HUD skin: our buttons are drawn per skin; the game's own choice is preselected.
$skinChips = @{}
foreach ($skin in $HudSkins.Keys) {
    $chip = [Windows.Controls.RadioButton]::new()
    $chip.Style = $window.FindResource("SkinChip")
    $chip.Tag = Image-From "skin_$skin.png"
    $chip.ToolTip = L $HudSkins[$skin]
    [Windows.Automation.AutomationProperties]::SetName($chip, (L "Скин HUD: ") + (L $HudSkins[$skin]))
    $chip.Add_Checked({ param($s) Update-SkinNote })
    $ui.Skins.Children.Add($chip) | Out-Null
    $skinChips[$skin] = $chip
}
# Icon opacity: one slider per icon, previewed over a match.
$ui.IconScene.Source = Image-From "scene.png"
$iconSliders = @{}; $iconImages = @{}; $iconPreviews = @{}; $iconValues = @{}
foreach ($name in $IconNames.Keys) {
    $row = [Windows.Controls.Grid]::new()
    $row.Margin = [Windows.Thickness]::new(16, 6, 16, 6)
    foreach ($w in 40, 120) { $c = [Windows.Controls.ColumnDefinition]::new(); $c.Width = [Windows.GridLength]::new($w); $row.ColumnDefinitions.Add($c) }
    $row.ColumnDefinitions.Add([Windows.Controls.ColumnDefinition]::new())
    $c = [Windows.Controls.ColumnDefinition]::new(); $c.Width = [Windows.GridLength]::new(46); $row.ColumnDefinitions.Add($c)
    $img = [Windows.Controls.Image]::new(); $img.Width = 30; $img.Height = 30
    $label = [Windows.Controls.TextBlock]::new(); $label.Text = L $IconNames[$name]; $label.FontWeight = "Medium"
    $label.VerticalAlignment = "Center"; [Windows.Controls.Grid]::SetColumn($label, 1)
    $slider = [Windows.Controls.Slider]::new()
    $slider.Style = $window.FindResource("SizeRange"); $slider.Height = 36
    $slider.Minimum = 10; $slider.Maximum = 100; $slider.Value = 100; $slider.TickFrequency = 5
    $slider.IsSnapToTickEnabled = $true; $slider.SmallChange = 5; $slider.LargeChange = 10
    $slider.FocusVisualStyle = $window.FindResource("Focus"); $slider.Tag = $name
    [Windows.Automation.AutomationProperties]::SetName($slider, (L "Непрозрачность: ") + (L $IconNames[$name]))
    [Windows.Controls.Grid]::SetColumn($slider, 2)
    $value = [Windows.Controls.TextBlock]::new(); $value.Text = "100%"; $value.TextAlignment = "Right"
    $value.VerticalAlignment = "Center"; $value.Foreground = $window.FindResource("SecondaryText"); [Windows.Controls.Grid]::SetColumn($value, 3)
    foreach ($el in $img, $label, $slider, $value) { $row.Children.Add($el) | Out-Null }
    if ($ui.IconRows.Children.Count) {
        $sep = [Windows.Controls.Border]::new(); $sep.Height = 1; $sep.Margin = [Windows.Thickness]::new(16, 0, 16, 0)
        $sep.Background = $window.FindResource("Separator"); $ui.IconRows.Children.Add($sep) | Out-Null
    }
    $ui.IconRows.Children.Add($row) | Out-Null
    $big = [Windows.Controls.Image]::new(); $big.Width = 64; $big.Height = 64; $big.Margin = [Windows.Thickness]::new(18, 0, 18, 0)
    $ui.IconPreview.Children.Add($big) | Out-Null
    $iconSliders[$name] = $slider; $iconImages[$name] = $img; $iconPreviews[$name] = $big; $iconValues[$name] = $value
    $slider.Add_ValueChanged({ param($s) Update-Icons })
}
function Selected-Icons {
    $h = @{}
    foreach ($name in $IconNames.Keys) { $h[$name] = [int]$iconSliders[$name].Value }
    $h
}
function Update-Icons {
    foreach ($name in $IconNames.Keys) {
        $v = [int]$iconSliders[$name].Value
        $iconValues[$name].Text = "$v%"
        $iconPreviews[$name].Opacity = $v / 100
        $iconImages[$name].Opacity = [math]::Max($v / 100, 0.35)
    }
    $changed = @($IconNames.Keys | Where-Object { [int]$iconSliders[$_].Value -lt 100 })
    $ui.NavIconsValue.Text = $(if ($changed.Count) { (L "{0} изм.") -f $changed.Count } else { "" })
    if (Get-Command Update-Summary -ErrorAction SilentlyContinue) { Update-Summary }
}
function Update-IconArt {
    # The icons look like the chosen layout's (Zors.o and Minimalist redraw them).
    $layout = Selected-Layout
    foreach ($name in $IconNames.Keys) {
        $bmp = Image-From "icon_${layout}_$name.png"
        $iconImages[$name].Source = $bmp; $iconPreviews[$name].Source = $bmp
    }
}
$ui.IconsReset.Add_Click({ foreach ($s in $iconSliders.Values) { $s.Value = 100 } })
$ui.IntroSwitch.Add_Click({ Update-InstallButton })

$script:gameSkin = $null
function Selected-Skin {
    $k = ($skinChips.GetEnumerator() | Where-Object { $_.Value.IsChecked } | Select-Object -First 1).Key
    if ($k) { $k } else { "dlc3" }
}
function Update-SkinNote {
    $k = Selected-Skin
    $ui.SkinNote.Text = $(if ($k -eq $script:gameSkin) { (L "{0} · определён по настройкам игры") -f (L $HudSkins[$k]) }
                          else { L $HudSkins[$k] })
}

$colourNames = [ordered]@{ white = (L "Белый"); green = (L "Зелёный"); yellow = (L "Жёлтый"); red = (L "Красный"); cyan = (L "Бирюзовый") }
$swatches = @{}
foreach ($c in $CrosshairColors.Keys) {
    $sw = [Windows.Controls.RadioButton]::new()
    $sw.Style = $window.FindResource("Swatch")
    $sw.Background = [Windows.Media.BrushConverter]::new().ConvertFrom("#" + $CrosshairColors[$c])
    $sw.ToolTip = $colourNames[$c]
    [Windows.Automation.AutomationProperties]::SetName($sw, (L "Цвет: ") + $colourNames[$c])
    $sw.Add_Checked({ Show-Crosshair })
    $ui.Swatches.Children.Add($sw) | Out-Null
    $swatches[$c] = $sw
}

function Selected-Colour {
    if ((Selected-Crosshair) -eq "zors") { return "white" }  # its own colours, never tinted
    $c = ($swatches.GetEnumerator() | Where-Object { $_.Value.IsChecked } | Select-Object -First 1).Key
    if ($c) { $c } else { "white" }
}

$tiles = @{}
foreach ($id in $crosshairNames.Keys) {
    if (-not (Test-Path -LiteralPath (Join-Path $PackRoot "payload\crosshairs\$id"))) { continue }
    $tile = [Windows.Controls.RadioButton]::new()
    $tile.Style = $window.FindResource("Tile")
    $tile.Content = $crosshairNames[$id]
    $tile.Tag = Image-From "crosshair_$id.png"
    [Windows.Automation.AutomationProperties]::SetName($tile, (L "Прицел: ") + $crosshairNames[$id])
    $ui.Tiles.Children.Add($tile) | Out-Null
    $tiles[$id] = $tile
    $tile.Add_Checked({ param($s)
        Show-Crosshair (($tiles.GetEnumerator() | Where-Object { $_.Value -eq $s }).Key)
        if (Get-Command Update-InstallButton -ErrorAction SilentlyContinue) { Update-InstallButton }
    })
}

function Selected-Crosshair {
    if (-not $ui.CrossSwitch.IsChecked) { return $null }
    ($tiles.GetEnumerator() | Where-Object { $_.Value.IsChecked } | Select-Object -First 1).Key
}

function Update-CrossSize {
    $id = Selected-Crosshair
    $size = [int]$ui.SizeSlider.Value
    $ui.SizeValue.Text = "$size%"
    if ($id) {
        $ui.AfterImage.Width = 128; $ui.AfterImage.Height = 128
        $colour = Selected-Colour
        $ui.AfterImage.Source = Tint-Image (Image-From "crosshair_${id}_${size}.png") $CrosshairColors[$colour]
        $ui.AfterLabel.Text = $crosshairNames[$id] + $(if ($colour -ne "white") { ", " + $colourNames[$colour].ToLower() }) + " · $size%"
    } else {
        $ui.AfterImage.Width = 64; $ui.AfterImage.Height = 64
        $ui.AfterImage.Source = Image-From "crosshair_vanilla.png"
        $ui.AfterLabel.Text = L "Не меняется"
    }
    Update-Summary
}
$ui.SizeSlider.Add_ValueChanged({ Update-CrossSize })

function Show-Crosshair([string]$id) {
    Update-CrossSize
    Animate $ui.AfterImage "Opacity" 1 220 0
    Animate $ui.AfterScale "ScaleX" 1 340 0.82 -Spring
    Animate $ui.AfterScale "ScaleY" 1 340 0.82 -Spring
}

# ---------------------------------------------------------------- navigation
$pages = "HudPage", "CrossPage", "IconsPage", "BackupPage"
function Show-Page([string]$name) {
    foreach ($p in $pages + "WorkPage") { if ($p -ne $name) { $ui[$p].Visibility = "Collapsed" } }
    $page = $ui[$name]
    $page.Visibility = "Visible"
    Animate $page "Opacity" 1 260 0
    if ($page.RenderTransform -is [Windows.Media.TranslateTransform]) { Animate $page.RenderTransform "Y" 0 360 12 }
}
function Update-Footer([string]$name) {
    # Steps: HUD -> crosshair -> icons -> install, so nothing is installed unseen;
    # restore lives on the backup page.
    if ($name -eq "WorkPage") { return }
    $ui.InstallBtn.Content = $(if ($name -in "HudPage", "CrossPage") { L "Далее" } else { L "Установить" })
    $ui.InstallBtn.Visibility = $(if ($name -eq "BackupPage") { "Collapsed" } else { "Visible" })
    $ui.RestoreBtn.Visibility = $(if ($name -eq "BackupPage") { "Visible" } else { "Collapsed" })
    $ui.RestoreBtn.IsDefault = ($name -eq "BackupPage")
    $ui.InstallBtn.IsDefault = ($name -ne "BackupPage")
    $ui.Summary.Opacity = $(if ($name -eq "BackupPage") { 0 } else { 1 })
}
function Current-Page { $pages | Where-Object { $ui[$_].Visibility -eq "Visible" } | Select-Object -First 1 }

function Move-NavPill($item) {
    $window.UpdateLayout()
    $y = $item.TranslatePoint([Windows.Point]::new(0, 0), $ui.Nav).Y
    Animate $ui.NavPillY "Y" $y 380 -Spring
}
foreach ($item in $ui.NavHud, $ui.NavCross, $ui.NavIcons, $ui.NavBackup) {
    $item.Add_Checked({ param($s) Show-Page $s.Tag; Update-Footer $s.Tag; Move-NavPill $s })
}

# ---------------------------------------------------------------- state
$script:game = $Game
$script:status = $null

function Set-Status([string]$text) {
    $ui.Status.Text = $text
    $ui.Status.Visibility = $(if ($text) { "Visible" } else { "Collapsed" })
    $ui.Summary.Visibility = $(if ($text) { "Collapsed" } else { "Visible" })
}

function Update-Summary {
    $cross = Selected-Crosshair
    $parts = @()
    if ($ui.HudSwitch.IsChecked) { $parts += "HUD «" + (L $Layouts[(Selected-Layout)]) + "»" }
    if ($cross) {
        $colour = Selected-Colour
        $parts += ((L "прицел «{0}»") -f $crosshairNames[$cross]) + $(if ($colour -ne "white") { " " + $colourNames[$colour].ToLower() }) + " " + [int]$ui.SizeSlider.Value + "%"
    }
    $icons = Selected-Icons
    $dim = @($IconNames.Keys | Where-Object { $icons[$_] -lt 100 } | ForEach-Object { (Lower-First (L $IconNames[$_])) + " " + $icons[$_] + "%" })
    if ($dim) { $parts += (L "иконки: ") + ($dim -join ", ") }
    if ($script:status -and [bool]$ui.IntroSwitch.IsChecked -ne $script:status.SkipIntro) {
        $parts += $(if ($ui.IntroSwitch.IsChecked) { L "без заставки" } else { L "с заставкой" })
    }
    $ui.Summary.Text = $(if ($parts) { (L "Будет установлено: ") + ($parts -join "; ") } else { L "Ничего не выбрано" })
    $ui.NavHudValue.Text = $(if ($ui.HudSwitch.IsChecked) { L $Layouts[(Selected-Layout)] } else { L "Выкл." })
    $ui.NavCrossValue.Text = $(if ($cross) { $crosshairNames[$cross] } else { L "Выкл." })
}

function Update-InstallButton {
    $ui.Tiles.IsEnabled = [bool]$ui.CrossSwitch.IsChecked
    $ui.SizeControls.IsEnabled = [bool]$ui.CrossSwitch.IsChecked
    # Zors.o's crosshair has its own colours; tinting would spoil them.
    $ui.ColourControls.IsEnabled = [bool]$ui.CrossSwitch.IsChecked -and (Selected-Crosshair) -ne "zors"
    $ui.Skins.IsEnabled = [bool]$ui.HudSwitch.IsChecked
    $icons = Selected-Icons
    $iconsChanged = @($IconNames.Keys | Where-Object { $icons[$_] -ne $(if ($script:status) { $script:status.Icons[$_] } else { 100 }) }).Count -gt 0
    $iconsChanged = $iconsChanged -or ($script:status -and [bool]$ui.IntroSwitch.IsChecked -ne $script:status.SkipIntro)
    $ui.InstallBtn.IsEnabled = ($script:game -and (Test-FortsFolder $script:game) -and
        ($ui.HudSwitch.IsChecked -or (Selected-Crosshair) -or $iconsChanged -or (Current-Page) -ne "IconsPage"))
    Update-Summary
}
$ui.HudSwitch.Add_Click({ Update-InstallButton })
$ui.CrossSwitch.Add_Click({ Update-InstallButton; Show-Crosshair (Selected-Crosshair) })

function Refresh {
    Set-Status ""
    if (-not $script:game -or -not (Test-FortsFolder $script:game)) {
        $ui.GamePath.Text = L "Forts не найден"
        Set-Status (L "Нажми «Изменить…» слева и выбери папку, где лежит Forts.exe.")
        $ui.InstallBtn.IsEnabled = $false
        $ui.RestoreBtn.IsEnabled = $false
        $ui.BackupState.Text = "—"; $ui.BackupPath.Text = "—"; $ui.NavBackupValue.Text = ""
        return
    }
    $ui.GamePath.Text = $script:game
    $ui.GamePath.ToolTip = $script:game
    $script:status = Get-PackStatus $script:game
    $script:gameSkin = Get-GameHudSkin $script:game
    $script:menuSkin = Get-GameMenuSkin $script:game
    $pick = $(if ($script:gameSkin) { $script:gameSkin } elseif ($script:status.HudSkin) { $script:status.HudSkin } else { "dlc3" })
    $skinChips[$pick].IsChecked = $true
    Update-SkinNote
    $script:settingUp = $true
    $layout = $(if ($script:status.Layout) { $script:status.Layout } else { Selected-Layout })
    if ($layoutSegs[$layout].IsChecked) { Show-Layout $layout } else { $layoutSegs[$layout].IsChecked = $true }
    foreach ($name in $IconNames.Keys) { $iconSliders[$name].Value = $script:status.Icons[$name] }
    $ui.IntroSwitch.IsChecked = $script:status.SkipIntro
    $script:settingUp = $false
    $ui.HudSwitch.IsEnabled = $script:status.HudCompatible
    if (-not $script:status.HudCompatible) {
        $ui.HudSwitch.IsChecked = $false
        $ui.HudNote.Text = L "Не подходит к этой версии игры"
    } elseif ($script:status.HudInstalled) {
        $ui.HudNote.Text = L "Уже стоит. Переустановка ничего не сломает"
    }
    if ($script:status.Crosshair -and $tiles.ContainsKey($script:status.Crosshair)) {
        $tiles[$script:status.Crosshair].IsChecked = $true
        $ui.SizeSlider.Value = $script:status.CrosshairSize
        if ($swatches.ContainsKey($script:status.CrosshairColor)) { $swatches[$script:status.CrosshairColor].IsChecked = $true }
        $ui.CrossNote.Text = (L "Сейчас стоит: {0}, {1}%") -f $crosshairNames[$script:status.Crosshair], $script:status.CrosshairSize
    }
    $screen = Get-GameScreen $script:game
    $brushes = [Windows.Media.BrushConverter]::new()
    if (-not $screen) {
        $ui.ScreenText.Text = L "не удалось узнать — HUD рассчитан на 16:9"
        $ui.ScreenDot.Fill = $brushes.ConvertFrom("#C7C7CC")
    } elseif ($screen.Wide169) {
        $ui.ScreenText.Text = (L "{0}×{1} · 16:9, подходит") -f $screen.Width, $screen.Height
        $ui.ScreenDot.Fill = $brushes.ConvertFrom("#34C759")
    } else {
        $ui.ScreenText.Text = (L "{0}×{1} — не 16:9, ресурсы сместятся") -f $screen.Width, $screen.Height
        $ui.ScreenDot.Fill = $brushes.ConvertFrom("#FF9F0A")
    }
    $ui.BackupState.Text = $(if ($script:status.HasBackup) { L "есть, файлы игры сохранены" } else { L "нет — пак сюда ещё не ставился" })
    $ui.NavBackupValue.Text = $(if ($script:status.HasBackup) { L "Есть" } else { "" })
    $ui.BackupPath.Text = Join-Path $script:game $BackupName
    $ui.RestoreBtn.IsEnabled = $script:status.HasBackup
    Update-InstallButton
}

$ui.ChangeFolder.Add_Click({
    $dialog = [System.Windows.Forms.FolderBrowserDialog]::new()
    $dialog.Description = L "Папка Forts (где лежит Forts.exe)"
    if ($script:game) { $dialog.SelectedPath = $script:game }
    if ($dialog.ShowDialog() -eq "OK") { $script:game = $dialog.SelectedPath; Refresh }
})

# ---------------------------------------------------------------- work states
$script:lastPage = "HudPage"
function Show-Work([string]$title, [string]$text) {
    $script:lastPage = Current-Page
    if (-not $script:lastPage) { $script:lastPage = "HudPage" }
    Show-Page "WorkPage"
    $ui.WorkTitle.Text = $title
    $ui.WorkText.Text = $text
    $ui.BarFill.Width = 0
    $ui.Bar.Visibility = "Visible"
    foreach ($b in "RestoreBtn", "InstallBtn") { $ui[$b].Visibility = "Collapsed" }
    $ui.Nav.IsEnabled = $false
    $ui.ChangeFolder.IsEnabled = $false
    $ui.Summary.Text = ""
}

function Set-Progress([int]$step, [int]$total, [string]$text) {
    $ui.WorkText.Text = $text
    Animate $ui.BarFill "Width" (400 * $step / $total) 200
    Flush
    if (-not $reduceMotion) { Start-Sleep -Milliseconds 70 }
}

function Show-Done([string]$title, [string]$text) {
    $ui.WorkTitle.Text = $title
    $ui.WorkText.Text = $text
    $ui.Bar.Visibility = "Collapsed"
    Animate $ui.DoneCircle "Opacity" 1 200 0
    Animate $ui.BadgeScale "ScaleX" 1 460 0.6 -Spring
    Animate $ui.BadgeScale "ScaleY" 1 460 0.6 -Spring
    Animate $ui.DoneCheck "StrokeDashOffset" 0 480 11
    $ui.DoneBtn.Visibility = "Visible"
    $ui.DoneBtn.Focus() | Out-Null
}

function Show-Failure([string]$text) {
    $ui.WorkTitle.Text = L "Не получилось"
    $ui.WorkText.Text = $text
    $ui.Bar.Visibility = "Collapsed"
    $ui.ErrorCircle.Visibility = "Visible"
    $ui.ErrorMark.Visibility = "Visible"
    Animate $ui.BadgeScale "ScaleX" 1 420 0.6 -Spring
    Animate $ui.BadgeScale "ScaleY" 1 420 0.6 -Spring
    $ui.BackBtn.Visibility = "Visible"
    $ui.DoneBtn.Visibility = "Visible"
    $ui.BackBtn.Focus() | Out-Null
}

function Show-Choose {
    foreach ($b in "BackBtn", "DoneBtn") { $ui[$b].Visibility = "Collapsed" }
    $ui.ErrorCircle.Visibility = "Collapsed"
    $ui.ErrorMark.Visibility = "Collapsed"
    $ui.DoneCircle.BeginAnimation([Windows.UIElement]::OpacityProperty, $null)
    $ui.DoneCheck.BeginAnimation([Windows.Shapes.Shape]::StrokeDashOffsetProperty, $null)
    $ui.DoneCircle.Opacity = 0
    $ui.DoneCheck.StrokeDashOffset = 11
    $ui.Nav.IsEnabled = $true
    $ui.ChangeFolder.IsEnabled = $true
    Show-Page $script:lastPage
    Update-Footer $script:lastPage
    Refresh
}
$ui.BackBtn.Add_Click({ Show-Choose })
$ui.DoneBtn.Add_Click({ $window.Close() })

$progress = { param($step, $total, $text) Set-Progress $step $total $text }

$ui.InstallBtn.Add_Click({
    switch (Current-Page) {
        "HudPage" { $ui.NavCross.IsChecked = $true; return }
        "CrossPage" { $ui.NavIcons.IsChecked = $true; return }
    }
    $hud = [bool]$ui.HudSwitch.IsChecked
    $cross = Selected-Crosshair
    $size = [int]$ui.SizeSlider.Value
    $layout = Selected-Layout
    Show-Work (L "Устанавливаю…") (L "Сохраняю оригинальные файлы игры")
    Flush
    try {
        Install-Pack -Game $script:game -Hud $hud -Crosshair $cross -CrosshairSize $size -CrosshairColor (Selected-Colour) `
            -HudSkin (Selected-Skin) -Layout $layout -Icons (Selected-Icons) -SkipIntro ([bool]$ui.IntroSwitch.IsChecked) -Progress $progress
        $parts = @()
        if ($hud) { $parts += "HUD «" + (L $Layouts[$layout]) + "»" }
        if ($cross) { $parts += (L "прицел «{0}»") -f $crosshairNames[$cross] }
        if (@((Selected-Icons).Values | Where-Object { $_ -lt 100 }).Count) { $parts += L "полупрозрачные иконки" }
        Show-Done (L "Готово") ((L "Поставлено: {0}. Запускай Forts.") -f ($parts -join (L " и ")))
    } catch {
        Show-Failure $_.Exception.Message
    }
})

$ui.RestoreBtn.Add_Click({
    Show-Work (L "Возвращаю оригинал…") (L "Проверяю резервную копию")
    Flush
    try {
        $skipped = @(Restore-Pack -Game $script:game -Progress $progress)
        $text = L "Файлы игры снова такие, какими были до установки."
        if ($skipped.Count) { $text += " " + ((L "Не тронул изменившиеся после установки: {0}.") -f ($skipped -join ", ")) }
        Show-Done (L "Оригинал на месте") $text
    } catch {
        Show-Failure $_.Exception.Message
    }
})

# ---------------------------------------------------------------- language
$(if ($script:Lang -eq "en") { $ui.LangEn } else { $ui.LangRu }).IsChecked = $true
function Switch-Language([string]$to) {
    if ($to -eq $script:Lang) { return }
    # The window is built in one language; reopen it in the other.
    $argv = @("-NoProfile", "-STA", "-ExecutionPolicy", "Bypass", "-WindowStyle", "Hidden",
        "-File", "`"$PSCommandPath`"", "-Lang", $to)
    if ($script:game) { $argv += @("-Game", "`"$script:game`"") }
    if ($NoMotion) { $argv += "-NoMotion" }
    Start-Process powershell.exe -ArgumentList $argv
    $window.Close()
}
$ui.LangRu.Add_Click({ Switch-Language "ru" })
$ui.LangEn.Add_Click({ Switch-Language "en" })

# ---------------------------------------------------------------- update check
# Asks GitHub for the latest release once per start; nothing is sent but the request.
$about = Get-Content -LiteralPath (Join-Path $PSScriptRoot "version.json") -Raw | ConvertFrom-Json
function Start-UpdateCheck {
    if (-not $about.repo) { return }
    try {
        Add-Type -AssemblyName System.Net.Http
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        $client = [Net.Http.HttpClient]::new()
        $client.Timeout = [TimeSpan]::FromSeconds(6)
        $client.DefaultRequestHeaders.UserAgent.ParseAdd("FortsSetup/" + $about.version)
        $script:updateTask = $client.GetStringAsync("https://api.github.com/repos/$($about.repo)/releases/latest")
    } catch { return }
    $script:updateTimer = [Windows.Threading.DispatcherTimer]::new()
    $script:updateTimer.Interval = [TimeSpan]::FromMilliseconds(300)
    $script:updateTimer.Add_Tick({
        if (-not $script:updateTask.IsCompleted) { return }
        $script:updateTimer.Stop()
        if ($script:updateTask.Status -ne "RanToCompletion") { return }
        try {
            $release = $script:updateTask.Result | ConvertFrom-Json
            $latest = [version]($release.tag_name.TrimStart("v"))
            if ($latest -gt [version]$about.version -and $release.html_url -like "https://github.com/*") {
                $script:updateUrl = $release.html_url
                $ui.UpdateText.Text = (L "Доступна версия {0}") -f $latest
                $ui.Update.Visibility = "Visible"
                Animate $ui.Update "Opacity" 1 300 0
            }
        } catch {}
    })
    $script:updateTimer.Start()
}
$ui.UpdateLink.Add_Click({ if ($script:updateUrl) { Start-Process $script:updateUrl } })

# ---------------------------------------------------------------- window
$ui.CloseLight.Add_Click({ $window.Close() })
$ui.MinLight.Add_Click({ $window.WindowState = "Minimized" })
$window.Add_KeyDown({ param($s, $e) if ($e.Key -eq "Escape") { $window.Close() } })

$window.Add_Loaded({
    Animate $ui.Shell "Opacity" 1 300 0
    $swatches["white"].IsChecked = $true
    if ($tiles.ContainsKey("cross")) { $tiles["cross"].IsChecked = $true }
    if (-not $script:game) { $script:game = Find-Forts }
    $ui.NavHud.IsChecked = $true
    Refresh
    switch ($State) {
        "cross" { $ui.NavCross.IsChecked = $true }
        "backup" { $ui.NavBackup.IsChecked = $true }
        "icons" { $ui.NavIcons.IsChecked = $true }
        "minimalist" { $layoutSegs["minimalist"].IsChecked = $true }
        "zors" { $layoutSegs["zors"].IsChecked = $true }
        "progress" { Show-Work (L "Устанавливаю…") ((L "Копирую {0}") -f "hud.lua"); $ui.BarFill.Width = 240 }
        "success" { Show-Work "" ""; Show-Done (L "Готово") ((L "Поставлено: {0}. Запускай Forts.") -f ((Lower-First (L "Компактный HUD")) + (L " и ") + ((L "прицел «{0}»") -f $crosshairNames["cross"]))) }
        "error" { Show-Work "" ""; Show-Failure (L "Закрой Forts и попробуй ещё раз.") }
    }
    $ui.InstallBtn.Focus() | Out-Null
    Start-UpdateCheck
})

$window.ShowDialog() | Out-Null
