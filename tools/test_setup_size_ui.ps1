# Exercise the actual WPF controls against a disposable game, render without screen capture.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$game = Join-Path $root 'build\size-ui-game'
if (Test-Path -LiteralPath $game) { throw 'Test folder already exists; inspect it before rerunning.' }
New-Item -ItemType Directory -Force "$game\data\ui\screens", "$game\data\ui\textures" | Out-Null
Set-Content "$game\Forts.exe" 'fixture'
Copy-Item "$root\original\hud.lua", "$root\original\transaction.lua" "$game\data\ui\screens\"
Copy-Item "$root\original\uisprites.lua" "$game\data\ui\"
Copy-Item "$root\textures\vanilla\*.dds" "$game\data\ui\textures\"
$harness = Join-Path $root 'setup\app\.size-test.ps1'
$source = [IO.File]::ReadAllText((Join-Path $root 'setup\app\setup.ps1'))
$body = @'
$window.Show()
Flush
$ui.NavCross.IsChecked = $true  # the size slider lives on the crosshair page
Flush
$tiles['scope'].IsChecked = $true
$ui.SizeSlider.Value = 75
Flush
if ($ui.SizeValue.Text -ne '75%' -or $ui.AfterImage.Source.UriSource.LocalPath -notlike '*crosshair_scope_75.png') { throw 'Slider did not update the preview.' }
$tiles['fine_cross'].IsChecked = $true
if ($ui.SizeSlider.Value -ne 75 -or $ui.AfterImage.Source.UriSource.LocalPath -notlike '*crosshair_fine_cross_75.png') { throw 'Switching variants lost the size.' }
$ui.CrossSwitch.IsChecked = $false
Update-InstallButton
if ($ui.SizeControls.IsEnabled) { throw 'Size slider should be disabled.' }
$ui.CrossSwitch.IsChecked = $true
Update-InstallButton
$ui.SizeSlider.Focus() | Out-Null
if (-not $ui.SizeSlider.IsKeyboardFocused) { throw 'Slider cannot receive keyboard focus.' }
foreach ($percent in @(50,75,100,150,200)) {
    $ui.SizeSlider.Value = $percent
    Flush
    $window.UpdateLayout()
    $point = $ui.SizeControls.TranslatePoint([Windows.Point]::new(0, $ui.SizeControls.ActualHeight), $window)
    if ($point.Y -gt ($window.ActualHeight - 100)) { throw 'Slider overlaps footer.' }
    $bitmap = [Windows.Media.Imaging.RenderTargetBitmap]::new([int]$window.ActualWidth,[int]$window.ActualHeight,96,96,[Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($window)
    $encoder = [Windows.Media.Imaging.PngBitmapEncoder]::new()
    $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $out = Join-Path (Split-Path -Parent $PSScriptRoot) "..\docs\setup-size-$percent.png"
    $stream = [IO.File]::Create($out)
    try { $encoder.Save($stream) } finally { $stream.Dispose() }
}
$ui.SizeSlider.Value = 200
$ui.HudSwitch.IsChecked = $false
$ui.InstallBtn.RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Primitives.ButtonBase]::ClickEvent))
$status = Get-PackStatus $script:game
if ($status.Crosshair -ne 'fine_cross' -or $status.CrosshairSize -ne 200) { throw 'Install button did not apply slider value.' }
Show-Choose
Refresh
if ($ui.SizeSlider.Value -ne 200) { throw 'Refresh did not restore installed size.' }
$ui.RestoreBtn.RaiseEvent([Windows.RoutedEventArgs]::new([Windows.Controls.Primitives.ButtonBase]::ClickEvent))
if ((Get-PackStatus $script:game).HasBackup) { throw 'Restore button failed.' }
$window.Close()
'UI SIZE TEST PASSED'
'@
try {
    $source = $source.Replace('$window.ShowDialog() | Out-Null', $body)
    [IO.File]::WriteAllText($harness, $source, [Text.UTF8Encoding]::new($true))
    & powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File $harness -Game $game -NoMotion
    if ($LASTEXITCODE -ne 0) { throw 'UI size test failed.' }
} finally {
    Remove-Item -LiteralPath $harness -ErrorAction SilentlyContinue
    if ((Resolve-Path -LiteralPath $game).Path -eq (Join-Path $root 'build\size-ui-game')) {
        Remove-Item -LiteralPath $game -Recurse -Force
    }
}
