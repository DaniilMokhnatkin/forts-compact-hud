# Возвращает в игру оригинальные hud.lua и uisprites.lua и удаляет свои текстуры кнопок.
param(
    [string]$Game = "C:\Program Files (x86)\Steam\steamapps\common\Forts"
)
$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
foreach ($pair in @(@("hud.lua", "data\ui\screens\hud.lua"), @("uisprites.lua", "data\ui\uisprites.lua"),
                    @("transaction.lua", "data\ui\screens\transaction.lua"))) {
    $original = Join-Path $root ("original\" + $pair[0])
    if (-not (Test-Path -LiteralPath $original)) { continue }
    Copy-Item -LiteralPath $original -Destination (Join-Path $Game $pair[1]) -Force
    Remove-Item -LiteralPath (Join-Path $root ("build\" + $pair[0] + ".installed.sha256")) -ErrorAction SilentlyContinue
}
Remove-Item -LiteralPath (Join-Path $root "build\installed.sha256") -ErrorAction SilentlyContinue
Remove-Item -Recurse -LiteralPath (Join-Path $Game "data\ui\textures\HUD\custom") -ErrorAction SilentlyContinue
"Оригинальный HUD возвращён в игру."
