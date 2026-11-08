# UI language. The window and scripts are written in Russian; for English every
# Russian string is looked up here (XAML attributes by Load-Window, code through L).
if (-not $script:Lang) { $script:Lang = "ru" }

$script:English = @{
    # window
    "Свернуть" = "Minimize"; "Закрыть" = "Close"; "HUD и прицел · Forts 1.39" = "HUD and crosshair · Forts 1.39"
    "Компактный HUD" = "Compact HUD"; "Вкл." = "On"; "Выкл." = "Off"; "Прицел" = "Crosshair"
    "Резервная копия" = "Backup"; "Папка игры" = "Game folder"; "Изменить…" = "Change…"; "Выбрать папку игры" = "Choose the game folder"
    "Есть" = "Saved"; "Язык" = "Language"
    # HUD page
    "Меньше места внизу экрана, ресурсы всегда перед глазами. Без модов: меняются только файлы интерфейса." =
        "Less space at the bottom of the screen, resources always in sight. No mods: only interface files change."
    "Компактный HUD в бою" = "Compact HUD in a match"; "Установить компактный HUD" = "Install the compact HUD"
    "Иконки на 20% меньше, панель ниже" = "Icons 20% smaller, panel lower"; "Экран игры" = "Game screen"
    "Ресурсы" = "Resources"; "«запас / лимит» по центру, над иконками" = '“stock / cap” in the centre, above the icons'
    "Кнопки" = "Buttons"; "сетка, двери, раскосы, пауза и меню в один ряд" = "grid, doors, bracing, pause and menu in one row"
    "Направление оружия" = "Weapon facing"; "открывается над рядом кнопок" = "opens above the button row"
    "Командир и реактор" = "Commander and reactor"; "в левом нижнем углу" = "in the bottom-left corner"
    "Не подходит к этой версии игры" = "Does not fit this game version"
    "Уже стоит. Переустановка ничего не сломает" = "Already installed. Reinstalling is safe"
    "не удалось узнать — HUD рассчитан на 16:9" = "unknown — the HUD is made for 16:9"
    "{0}×{1} · 16:9, подходит" = "{0}×{1} · 16:9, fits"
    "{0}×{1} — не 16:9, ресурсы сместятся" = "{0}×{1} — not 16:9, resources will be off"
    # crosshair page
    "Выбери вариант, цвет и размер. Слева — стандартный прицел игры." = "Pick a style, colour and size. On the left is the game's default crosshair."
    "Стандартный прицел" = "Default crosshair"; "Было" = "Before"; "Новый прицел" = "New crosshair"; "Станет" = "After"
    "Заменить прицел" = "Replace the crosshair"; "Вместо большого круглого" = "Instead of the big round one"; "Менять прицел" = "Replace the crosshair"
    "Цвет" = "Colour"; "Размер" = "Size"; "Размер прицела в процентах" = "Crosshair size in percent"
    "От 50 до 200%. Стрелки: шаг 5%." = "From 50 to 200%. Arrow keys: 5% steps."
    "Крест" = "Cross"; "Разрыв" = "Gap"; "Тонкий" = "Fine"; "Кольцо" = "Ring"; "Рамка" = "Brackets"; "Оптика" = "Scope"
    "Бой" = "Match"; "Небо" = "Sky"; "Скалы" = "Rocks"; "Огонь" = "Fire"; "Фон: " = "Background: "
    "Белый" = "White"; "Зелёный" = "Green"; "Жёлтый" = "Yellow"; "Красный" = "Red"; "Бирюзовый" = "Cyan"; "Цвет: " = "Colour: "
    "Прицел: " = "Crosshair: "; "Не меняется" = "Unchanged"; "Сейчас стоит: {0}, {1}%" = "Installed now: {0}, {1}%"
    # backup page
    "Перед первой установкой файлы игры сохраняются. Кнопка «Вернуть оригинал» кладёт их обратно." =
        'Before the first install the game''s files are saved. “Restore original” puts them back.'
    "Состояние" = "Status"; "Где лежит" = "Location"; "Если игра обновилась" = "If the game updated"; "новые файлы не трогаются" = "new files are left alone"
    "Полностью чистый интерфейс вернёт и Steam: Свойства → Установленные файлы → Проверить целостность." =
        "Steam can also restore a clean interface: Properties → Installed Files → Verify integrity."
    "есть, файлы игры сохранены" = "saved, the game's files are kept"; "нет — пак сюда ещё не ставился" = "none — the pack was never installed here"
    # footer and work states
    "Вернуть оригинал" = "Restore original"; "Установить" = "Install"; "Далее" = "Next"; "Назад" = "Back"
    "Будет установлено: " = "Will install: "; "Ничего не выбрано" = "Nothing selected"; " и " = " and "
    "прицел «{0}»" = 'the “{0}” crosshair'
    "Forts не найден" = "Forts not found"; "Нажми «Изменить…» слева и выбери папку, где лежит Forts.exe." = 'Click “Change…” on the left and choose the folder with Forts.exe.'
    "Папка Forts (где лежит Forts.exe)" = "The Forts folder (with Forts.exe)"
    "Устанавливаю…" = "Installing…"; "Сохраняю оригинальные файлы игры" = "Saving the game's original files"
    "Готово" = "Done"; "Поставлено: {0}. Запускай Forts." = "Installed: {0}. Start Forts."
    "Не получилось" = "That didn't work"; "Возвращаю оригинал…" = "Restoring the original…"; "Проверяю резервную копию" = "Checking the backup"
    "Файлы игры снова такие, какими были до установки." = "The game's files are back as they were before the install."
    "Не тронул изменившиеся после установки: {0}." = "Left alone files changed after the install: {0}."
    "Оригинал на месте" = "Original restored"
    "Скин HUD в игре" = "HUD skin in the game"; "Кнопки рисуются под выбранный скин" = "Our buttons are drawn for the chosen skin"
    "{0} · определён по настройкам игры" = "{0} · read from the game's settings"; "Стандартный" = "Default"
    "Скин HUD: " = "HUD skin: "; "Фото {0}" = "Photo {0}"; "Неизвестный скин HUD." = "Unknown HUD skin."
    # update check
    "Доступна версия {0}" = "Version {0} is available"; "Скачать" = "Download"
    # engine
    "В этой папке нет Forts.exe." = "There is no Forts.exe in this folder."; "Закрой Forts и попробуй ещё раз." = "Close Forts and try again."
    "HUD сделан для Forts 1.39.0 r22745, а файлы интерфейса у тебя другие (другая версия игры или чужой мод)." =
        "The HUD is made for Forts 1.39.0 r22745, but your interface files differ (another game version or someone else's mod)."
    "Размер прицела должен быть от 50 до 200%, с шагом 5%." = "Crosshair size must be 50 to 200% in 5% steps."
    "Неизвестный прицел." = "Unknown crosshair."; "Не хватает текстуры прицела." = "A crosshair texture is missing."
    "Неизвестный цвет прицела." = "Unknown crosshair colour."; "Копирую {0}" = "Copying {0}"; "Не удалось записать {0}." = "Could not write {0}."
    "Сохраняю резервную копию" = "Saving the backup"; "Возвращаю {0}" = "Restoring {0}"; "Убираю резервную копию" = "Removing the backup"
    "Резервной копии нет: пак сюда не ставился. Оригинал можно вернуть через Steam (Проверить целостность файлов)." =
        "There is no backup: the pack was never installed here. Steam can restore the original (Verify integrity of game files)."
    "Копия {0} повреждена, остановился." = "The backup of {0} is damaged; stopped."
}

function Lower-First([string]$s) { $s.Substring(0, 1).ToLower() + $s.Substring(1) }

function L([string]$s) {
    if ($script:Lang -eq "en" -and $script:English.ContainsKey($s)) { $script:English[$s] } else { $s }
}

function Get-DefaultLang([string]$Game) {
    # The game's own language first, then Windows.
    if ($Game) {
        $file = Get-ChildItem -LiteralPath (Join-Path $Game "users") -Filter "settings.lua" -Recurse -Depth 1 -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($file) {
            $m = [regex]::Match([IO.File]::ReadAllText($file.FullName), 'Language2\s*=\s*"([^"]+)"')
            if ($m.Success) { return $(if ($m.Groups[1].Value -in "Russian", "Ukrainian") { "ru" } else { "en" }) }
        }
    }
    if ((Get-UICulture).TwoLetterISOLanguageName -in "ru", "uk", "be", "kk") { "ru" } else { "en" }
}
