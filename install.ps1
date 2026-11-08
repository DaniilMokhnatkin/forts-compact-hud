# Устанавливает отредактированные hud.lua и uisprites.lua и свои текстуры кнопок в Forts.
# -DryRun: только проверить и собрать build\, в игру ничего не копировать.
param(
    [string]$Game = "C:\Program Files (x86)\Steam\steamapps\common\Forts",
    [switch]$DryRun
)
$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$tool = Join-Path $root "tools\forts_ui.py"
$build = Join-Path $root "build"
$files = @(
    @{ Name = "hud.lua"; Target = "data\ui\screens\hud.lua"; Check = $true },
    @{ Name = "uisprites.lua"; Target = "data\ui\uisprites.lua"; Check = $false },
    @{ Name = "transaction.lua"; Target = "data\ui\screens\transaction.lua"; Check = $false }
)
$textures = Join-Path $root "textures\HUD\custom"
$texturesTarget = Join-Path $Game "data\ui\textures\HUD\custom"

function Get-Sha([string]$path) { (Get-FileHash -Algorithm SHA256 -LiteralPath $path).Hash }
New-Item -ItemType Directory -Force $build | Out-Null

foreach ($f in $files) {
    $source = Join-Path $root $f.Name
    if (-not (Test-Path -LiteralPath $source)) { continue }
    $original = Join-Path $root ("original\" + $f.Name)
    $target = Join-Path $Game $f.Target
    $stamp = Join-Path $build ($f.Name + ".installed.sha256")
    if (-not (Test-Path -LiteralPath $original)) { throw "Нет original\$($f.Name): без копии оригинала устанавливать нельзя." }

    # В игре должен лежать либо оригинал, либо наша прошлая сборка; иначе игра обновилась.
    $known = @(Get-Sha $original)
    if (Test-Path -LiteralPath $stamp) { $known += (Get-Content -LiteralPath $stamp).Trim() }
    if ($known -notcontains (Get-Sha $target)) {
        throw "$($f.Name) в игре не совпадает ни с оригиналом, ни с последней установкой. Похоже, игра обновилась: ничего не копирую."
    }
    if ($f.Check) {
        & py -3 $tool --game $Game check $original $source
        if ($LASTEXITCODE -ne 0) { throw "Проверка $($f.Name) не пройдена, игра не тронута." }
    }
    $out = Join-Path $build $f.Name
    & py -3 $tool --game $Game compile $source $out
    if ($LASTEXITCODE -ne 0) { throw "Компиляция $($f.Name) не удалась, игра не тронута." }
    $f.Build = $out; $f.Stamp = $stamp; $f.Dest = $target
}

if ($DryRun) { "DryRun: собрано в $build, в игру ничего не скопировано."; return }

foreach ($f in $files) {
    if (-not $f.Build) { continue }
    Copy-Item -LiteralPath $f.Build -Destination $f.Dest -Force
    Get-Sha $f.Build | Set-Content -LiteralPath $f.Stamp
}
if (Test-Path -LiteralPath $textures) {
    New-Item -ItemType Directory -Force $texturesTarget | Out-Null
    Copy-Item -Path (Join-Path $textures "*") -Destination $texturesTarget -Force
}
"Установлено. Перезапусти игру, чтобы подхватились спрайты и текстуры."
