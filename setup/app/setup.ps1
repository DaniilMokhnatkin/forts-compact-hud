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
    cross = (L "Крест"); gap_cross = (L "Разрыв"); fine_cross = (L "Тонкий")
    dot_ring = (L "Кольцо"); brackets = (L "Рамка"); scope = (L "Оптика")
}

$window, $ui = Load-Window (Join-Path $PSScriptRoot "xaml") @{ SPRING = $(if ($reduceMotion) { "0:0:0" } else { "0:0:0.32" }) }
Enable-Glass $window $ui.Tint

# ---------------------------------------------------------------- content
$hudBrush = [Windows.Media.ImageBrush]::new((Image-From "hud_after.png"))
$hudBrush.Stretch = "UniformToFill"
$ui.HudShot.Background = $hudBrush
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
    $tile.Add_Checked({ param($s) Show-Crosshair (($tiles.GetEnumerator() | Where-Object { $_.Value -eq $s }).Key) })
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
$pages = "HudPage", "CrossPage", "BackupPage"
function Show-Page([string]$name) {
    foreach ($p in $pages + "WorkPage") { if ($p -ne $name) { $ui[$p].Visibility = "Collapsed" } }
    $page = $ui[$name]
    $page.Visibility = "Visible"
    Animate $page "Opacity" 1 260 0
    if ($page.RenderTransform -is [Windows.Media.TranslateTransform]) { Animate $page.RenderTransform "Y" 0 360 12 }
}
function Update-Footer([string]$name) {
    # Steps: HUD -> crosshair -> install. Install only after the crosshair page has
    # been seen; restore lives on the backup page.
    if ($name -eq "WorkPage") { return }
    $ui.InstallBtn.Content = $(if ($name -eq "HudPage") { L "Далее" } else { L "Установить" })
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
foreach ($item in $ui.NavHud, $ui.NavCross, $ui.NavBackup) {
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
    if ($ui.HudSwitch.IsChecked) { $parts += Lower-First (L "Компактный HUD") }
    if ($cross) {
        $colour = Selected-Colour
        $parts += ((L "прицел «{0}»") -f $crosshairNames[$cross]) + $(if ($colour -ne "white") { " " + $colourNames[$colour].ToLower() }) + " " + [int]$ui.SizeSlider.Value + "%"
    }
    $ui.Summary.Text = $(if ($parts) { (L "Будет установлено: ") + ($parts -join (L " и ")) } else { L "Ничего не выбрано" })
    $ui.NavHudValue.Text = $(if ($ui.HudSwitch.IsChecked) { L "Вкл." } else { L "Выкл." })
    $ui.NavCrossValue.Text = $(if ($cross) { $crosshairNames[$cross] } else { L "Выкл." })
}

function Update-InstallButton {
    $ui.Tiles.IsEnabled = [bool]$ui.CrossSwitch.IsChecked
    $ui.SizeControls.IsEnabled = [bool]$ui.CrossSwitch.IsChecked
    $ui.ColourControls.IsEnabled = [bool]$ui.CrossSwitch.IsChecked
    $ui.InstallBtn.IsEnabled = ($script:game -and (Test-FortsFolder $script:game) -and
        ($ui.HudSwitch.IsChecked -or (Selected-Crosshair)))
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
    if ((Current-Page) -eq "HudPage") { $ui.NavCross.IsChecked = $true; return }
    $hud = [bool]$ui.HudSwitch.IsChecked
    $cross = Selected-Crosshair
    $size = [int]$ui.SizeSlider.Value
    Show-Work (L "Устанавливаю…") (L "Сохраняю оригинальные файлы игры")
    Flush
    try {
        Install-Pack -Game $script:game -Hud $hud -Crosshair $cross -CrosshairSize $size -CrosshairColor (Selected-Colour) -Progress $progress
        $parts = @()
        if ($hud) { $parts += Lower-First (L "Компактный HUD") }
        if ($cross) { $parts += (L "прицел «{0}»") -f $crosshairNames[$cross] }
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
        "progress" { Show-Work (L "Устанавливаю…") ((L "Копирую {0}") -f "hud.lua"); $ui.BarFill.Width = 240 }
        "success" { Show-Work "" ""; Show-Done (L "Готово") ((L "Поставлено: {0}. Запускай Forts.") -f ((Lower-First (L "Компактный HUD")) + (L " и ") + ((L "прицел «{0}»") -f $crosshairNames["cross"]))) }
        "error" { Show-Work "" ""; Show-Failure (L "Закрой Forts и попробуй ещё раз.") }
    }
    $ui.InstallBtn.Focus() | Out-Null
    Start-UpdateCheck
})

$window.ShowDialog() | Out-Null
