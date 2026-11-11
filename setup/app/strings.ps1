# UI language. The window and scripts are written in Russian; for English every
# Russian string is looked up here (XAML attributes by Load-Window, code through L).
if (-not $script:Lang) { $script:Lang = "ru" }

$script:English = @{
    # window
    "Свернуть" = "Minimize"; "Закрыть" = "Close"; "HUD и прицел · Forts 1.39" = "HUD and crosshair · Forts 1.39"
    "Компактный HUD" = "Compact HUD"; "Вкл." = "On"; "Выкл." = "Off"; "Прицел" = "Crosshair"
    "Резервная копия" = "Backup"; "Папка игры" = "Game folder"; "Изменить…" = "Change…"; "Выбрать папку игры" = "Choose the game folder"
    "Есть" = "Saved"; "Язык" = "Language"
    # About
    "О разработчике" = "About the developer"; "Forts Setup делаю я. Установщик бесплатный и без модов." = "I make Forts Setup. The installer is free and mod-free."
    "Даниил · Starkoff" = "Daniil · Starkoff"; "Автор Forts Setup и компактного HUD Starkoff" = "Author of Forts Setup and the Starkoff compact HUD"
    "Ещё для Forts" = "More for Forts"; "Программа для Forts: история матчей и рейтинг 1v1 по реплеям, твоя эффективность в каждом матче, аватарки и ники Steam, радар сильных игроков." = "A Forts app: match history and 1v1 rating from replays, your own performance in every match, Steam avatars and names, a radar for strong players."
    "А ещё у меня есть крутой скриншотер!" = "I also made a great screenshot tool!"; "Скачать Kadr" = "Download Kadr"
    "Очень быстрый скриншотер на Rust: запись экрана, мгновенный перевод текста на экране, блюр важных данных. Помнит 25 последних снимков и 50 скопированных текстов, вызов по Win+V." = "A very fast screenshot tool written in Rust: screen recording, instant translation of on-screen text, blur for sensitive data. Remembers the last 25 screenshots and 50 copied texts, opened with Win+V."
    "Скачать последнюю версию" = "Download the latest version"; "Страница на GitHub" = "GitHub page"; "Скачиваю…" = "Downloading…"; "Скачано в «Загрузки»: " = "Saved to Downloads: "; "Закрой этот установщик и запусти новый." = "Close this installer and run the new one."
    "Не удалось получить последнюю версию. Проверьте подключение." = "Could not get the latest version. Check your connection."
    "Не удалось скачать файл. Проверьте подключение и повторите позже." = "Could not download the file. Check your connection and try later."
    "Контрольная сумма не совпала. Файл удалён, попробуйте ещё раз." = "Checksum mismatch. The file was deleted, please try again."
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
    "Жирный" = "Bold"; "Точка" = "Dot"; "Два кольца" = "Double ring"; "Квадрат" = "Square"; "Ромб" = "Diamond"
    "Шеврон" = "Chevron"; "Два шеврона" = "Double chevron"; "Икс" = "X"; "Т-образный" = "T-shape"
    "Снайпер" = "Sniper"; "Мил-дот" = "Mil-dot"; "Аркада" = "Arcade"
    "Duo Крест" = "Duo Cross"; "Подкова" = "Horseshoe"; "Крылья" = "Wings"; "Фокус" = "Focus"
    "Тренога" = "Tripod"; "Грани" = "Facets"; "Чаша" = "Bowl"; "Диагональ" = "Diagonal"
    "Скобки" = "Parens"; "Апертура" = "Aperture"; "Остриё" = "Point"; "Выемка" = "Notch"
    "Соты" = "Hex"; "Указатели" = "Pointers"; "Дальномер" = "Rangefinder"; "Три луча" = "Three posts"
    "Призма" = "Prism"; "Клык" = "Fang"; "Ореол" = "Halo"; "Камертон" = "Fork"
    "Триада" = "Triad"; "Игла" = "Needle"; "Затмение" = "Eclipse"; "Кайт" = "Kite"
    "Якорь" = "Anchor"; "Маяк" = "Beacon"; "Орбита" = "Orbit"; "Горизонт" = "Horizon"
    "Лепесток" = "Petal"; "Импульс" = "Pulse"; "Врата" = "Gate"; "Звезда" = "Star"
    "Duo · белый прицел, красный центр" = "Duo · white reticle, red centre"
    "Neo · фигурный центр, авторские цвета" = "Neo · shaped centre, original colours"
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
    "{0} · определён по настройкам игры" = "{0} · read from the game's settings"; "По умолчанию" = "Default"; "Луноход" = "Moonshot"; "Классика" = "Classic"; "Склад оружия" = "Armoury"
    "Скин HUD: " = "HUD skin: "; "Фото {0}" = "Photo {0}"; "Неизвестный скин HUD." = "Unknown HUD skin."
    "Неизвестный HUD." = "Unknown HUD."; "Прозрачность иконок: от 10 до 100%." = "Icon opacity: 10 to 100%."
    "Компактный" = "Compact"; "Тушение" = "Extinguish"; "Строительство" = "Construction"; "Ремонт" = "Repair"
    # layouts and icons
    "Выбери вид интерфейса. Без модов: меняются только файлы интерфейса, оригиналы сохраняются." = 'Pick a HUD. No mods: only interface files change, the originals are kept.'
    "Установить HUD" = "Install the HUD"; "Выбранный HUD" = "Chosen HUD"; "Мастерская Steam" = "Steam Workshop"
    "Компактный · наш" = "Compact · ours"; "Zors.o · фиолетовая тема" = "Zors.o · purple theme"; "Minimalist · автор nutellafish" = "Minimalist · by nutellafish"
    "Ресурсы «запас / лимит» по центру, кнопки в один ряд, иконки на 20% меньше." = 'Resources "stock / cap" in the centre, buttons in one row, icons 20% smaller.'
    "Наш компактный HUD в фиолетовом, плюс фон меню, курсоры с прицелом и иконки от Zors.o." = "Our compact HUD in purple, plus the menu background, cursors with a crosshair and icons by Zors.o."
    "Прозрачный минимальный интерфейс; убранные кнопки нажимаются горячими клавишами." = "A transparent minimal interface; removed buttons work through hotkeys."
    "Известные проблемы (от автора)" = "Known issues (from the author)"
    "• В редакторе карт нет вкладок внизу." = "• The map editor has no tabs at the bottom."; "• Скорее всего работает только на 16:9." = "• Probably works only at 16:9."
    "• Обновление игры может сбросить интерфейс — поставь заново." = "• A game update can reset the interface: install again."
    "• Новые значки (в т. ч. рейтинговые) не видны, пока автор не обновит мод." = "• New badges (ranked ones too) do not show until the author updates the mod."
    "• Сейчас в игре скин «{0}». Для этого HUD выбери в игре: Настройки → Игра → Скин HUD → «По умолчанию»." = '• The game now uses the "{0}" skin. For this HUD choose in the game: Options → Game → HUD Skin → "Default".'
    "• Нужен скин HUD «По умолчанию» (Настройки → Игра → Скин HUD)." = '• Needs the "Default" HUD skin (Options → Game → HUD Skin).'
    "У этого HUD свой прицел. Включи, чтобы поставить наш вместо него." = "This HUD has its own crosshair. Turn on to put ours instead."
    "Оригиналы сохраняются, вернуть можно одной кнопкой" = "The originals are kept; restore with one button"
    "Иконки" = "Icons"; "Вернуть всем 100%" = "Set all back to 100%"; "Непрозрачность: " = "Opacity: "; "{0} изм." = "{0} changed"
    "иконки: " = "icons: "; "полупрозрачные иконки" = "see-through icons"
    "Значки тушения, строительства, ремонта и демонтажа над фортом. Сделай их полупрозрачными, чтобы не закрывали постройки." = "Extinguish, construction, repair and demolition icons over your fort. Make them translucent so they do not hide your build."
    "Фон главного меню" = "Main menu background"; "Скины в игре" = "Skins in the game"
    "Наш компактный HUD в космическом фиолетовом: кнопки, вкладки, командир, реактор, иконки и прицел; фон меню и заставка от Zors.o." = "Our compact HUD in cosmic purple: buttons, tabs, commander, reactor, icons and crosshair; menu background and splash by Zors.o."
    "• Сейчас в игре скин меню «{0}», с ним фон Zors.o не виден: DLC-скины берут фон из своих файлов. Выбери в игре: Настройки → Игра → Скин меню → «По умолчанию», «Классика» или «Склад оружия»." = '• The game now uses the "{0}" menu skin, which does not show the Zors.o background: DLC skins use their own files. In the game choose Options → Game → Menu Skin → "Default", "Classic" or "Armoury".'
    "• Фон меню виден со скинами меню «По умолчанию», «Классика» и «Склад оружия»." = '• The menu background shows with the "Default", "Classic" and "Armoury" menu skins.'
    "• Сейчас в игре скин HUD «{0}». Чтобы были видны фиолетовые вкладки, командир и реактор, выбери: Настройки → Игра → Скин HUD → «По умолчанию»." = '• The game now uses the "{0}" HUD skin. For the purple tabs, commander and reactor choose Options → Game → HUD Skin → "Default".'
    "• Скин HUD «По умолчанию» — как нужно: вкладки, командир и реактор будут фиолетовыми." = '• The HUD skin is "Default", as needed: tabs, commander and reactor will be purple.'
    "Сейчас в игре скин меню «{0}», с ним фон Zors.o не виден: DLC-скины берут фон из своих файлов. Выбери в игре: Настройки → Игра → Скин меню → «По умолчанию», «Классика» или «Склад оружия»." = 'The game now uses the "{0}" menu skin, which does not show the Zors.o background: DLC skins use their own files. In the game choose Options → Game → Menu Skin → "Default", "Classic" or "Armoury".'
    "Виден со скинами меню «По умолчанию», «Классика» и «Склад оружия». У «Лунохода», High Seas и Firebird фон свой." = 'Shows with the "Default", "Classic" and "Armoury" menu skins. Moonshot, High Seas and Firebird keep their own.'
    "← → листать · Esc закрыть" = "← → browse · Esc closes"; "⤢  На весь экран" = "⤢  Full screen"; "На весь экран" = "Full screen"
    "Открыть на весь экран (Esc — закрыть)" = "Open full screen (Esc closes)"; "Открыть на весь экран" = "Open full screen"
    "Прицел Zors.o выбран по умолчанию — можно поменять на любой." = "The Zors.o crosshair is picked by default; any other one can be chosen."
    "Профиль Steam" = "Steam profile"; "Иконки и запуск" = "Icons and start-up"; "Запуск игры" = "Game start"
    "Пропускать заставку" = "Skip the intro"; "Логотипы при запуске Forts" = "The logos when Forts starts"
    "без заставки" = "no intro"; "с заставкой" = "with the intro"
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

$script:English['Готово. Запускай Forts.'] = 'Done. Start Forts.'
$script:English['Настроить HUD'] = 'Customize HUD'
$script:English['Кнопки в один ряд. Расположение ресурсов, размер карточек, фон и цвет выбираются в настройках HUD.'] = 'Buttons in one row. Choose resource placement, card size, background and colour in Customize HUD.'
$script:English['Развернуть'] = 'Expand'
$script:English['‹ Назад'] = '‹ Previous'
$script:English['Далее ›'] = 'Next ›'
$script:English['Предыдущий скриншот'] = 'Previous screenshot'
$script:English['Следующий скриншот'] = 'Next screenshot'
$script:English['Тактический'] = 'Tactical'
$script:English['Тактический · по HUD RobberFox'] = 'Tactical · based on RobberFox HUD'
$script:English['Плоские тёмно-синие боковые панели, свободный центр. Расположение ресурсов, фон, масштаб и оттенок настраиваются.'] = 'Flat navy side panels and a clear centre. Customize resource placement, background, scale and hue.'
$script:English['Референс · HUD RobberFox'] = 'Reference · RobberFox HUD'
$script:English['Тема по HUD RobberFox. Превью — исходный референс.'] = 'Theme based on RobberFox HUD. Preview shows the original reference.'
$script:English['Ресурсы справа внизу'] = 'Resources at bottom right'
$script:English['Ресурсы по центру'] = 'Resources in the centre'
$script:English['• Фон меню Zors.o виден только со скинами «По умолчанию», «Классика» и «Склад оружия». Выбери один из них в настройках игры.'] = '• The Zors.o menu background requires Default, Classic or Tons of Guns. Choose one in the game settings.'
$script:English['Не тронул изменившиеся после установки: '] = 'Left files changed since installation untouched: '
$script:English['Демонтаж'] = 'Demolition'
$script:English['Гриб'] = 'Mushroom'
$script:English['Концепт оформления'] = 'Design concept'
$script:English['• Для фона меню Zors.o выбери скин меню «По умолчанию» в настройках Forts.'] = '• For the Zors.o menu background, select the Default menu skin in Forts settings.'
$script:English['Мухоморные вкладки, лесные панели и грибной прицел. 100% гриб. Скилл в комплект не входит.'] = 'Toadstool tabs, forest panels and a mushroom cursor. 100% mushroom. Skill not included.'
$script:English['• Для грибных вкладок и командира выбери скин HUD «По умолчанию» в настройках Forts.'] = '• Choose the Default HUD skin in Forts settings for the mushroom tabs and commander.'
$script:English['Прицел можно выбрать отдельно. В галерее — скриншот из игры.'] = 'Select the cursor independently. The gallery shows a gameplay screenshot.'
$script:English['В игре'] = 'Gameplay'

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
