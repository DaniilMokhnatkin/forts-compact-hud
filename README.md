# Компактный HUD и прицел для Forts (1.39.0 r22745)

Без модов: установщик меняет только файлы интерфейса игры и сохраняет оригиналы, вернуть всё можно одной кнопкой.

![Forts Setup](docs/setup-choose.png)

## Скачать

[Releases](https://github.com/DaniilMokhnatkin/forts-compact-hud/releases/latest) → **Forts-Setup.exe**
(один файл) или **Forts-Setup.zip** (папка, запуск через `Forts Setup.cmd`). Закрой Forts перед установкой.
Windows может предупредить о неизвестном издателе: файл не подписан, «Подробнее → Выполнить в любом случае».

- компактный HUD: ресурсы «запас / лимит» по центру, кнопки в один ряд, иконки на 20% меньше;
- 6 прицелов, 5 цветов, размер 50–200%, превью на фоне боя, неба, скал и огня;
- проверка экрана (HUD рассчитан на 16:9), русский и английский, проверка обновлений через GitHub.

*English:* a compact HUD and crosshair pack for Forts without mods. Download **Forts-Setup.exe** from
Releases, close the game, run it. The installer keeps the original files and can restore them.

## Сборка из исходников

Файлы игры в репозитории не лежат (`.gitignore`): скопируй из своей Forts 1.39 `data\ui\screens\hud.lua`,
`data\ui\screens\transaction.lua`, `data\ui\uisprites.lua` в `original\` (а `uisprites.lua` ещё в `reference\`),
`data\ui\textures\mouse_target*.dds` в `textures\vanilla\`. Дальше по шагам из `tools\build_setup.ps1`.

## Что здесь

| Файл | Зачем |
|---|---|
| `hud.lua` | Декомпилированный HUD игры. **Правишь только его.** |
| `original/hud.lua` | Оригинал из игры (байткод), копия на случай отката. |
| `hud_map.txt` | Карта всех 480 элементов: позиция, размер, формулы, текстуры. |
| `reference/uisprites.lua` | Какие файлы текстур стоят за спрайтами вроде `hud-menu` (для перерисовки). |
| `install.ps1` | Проверяет `hud.lua`, компилирует и кладёт в игру. |
| `restore.ps1` | Возвращает оригинал. |
| `tools/forts_ui.py` | Декомпилятор и проверки (`decompile`, `map`, `compare`, `check`, `compile`). |

Декомпиляция без потерь: компилятор игры `LuaPlusC.exe` собирает из `hud.lua` файл, байт в байт
совпадающий с оригиналом (SHA-256 `1E0004F7…893C`). То же проверено для `uisprites.lua` и `chatroom.lua`.

## Компактная раскладка

`py -3 layouts\compact.py` собирает `hud.lua` из `original\hud.lua`: иконки постройки ×0.8,
левая и центральная панели ниже на 14 единиц, кнопки направления оружия подняты обратно в экран.
Правая панель ресурсов остаётся на месте: рамки под цифрами нарисованы в её текстуре, и сдвиг
цифр внутри неё ломает картинку. Скрипт работает
от оригинала, поэтому после патча достаточно обновить `original\` и запустить его снова.
Параметр `ICON` в начале файла задаёт масштаб иконок.

Движок сам растягивает нижнюю панель на всю ширину экрана и прижимает ресурсы к правому краю,
поэтому горизонтальные координаты частей панели в файле почти ничего не значат. Вертикальные
сдвиги и размеры игра учитывает.

## Свои кнопки паузы, play и меню

`py -3 tools\draw_buttons.py` рисует их из `textures\grid_button_dlc3.png` (кнопка «сетка» скина
dlc3, снятая с экрана) в `textures\HUD\custom\*.dds`, 4 состояния. `py -3 layouts\sprites.py` собирает
`uisprites.lua`, где спрайты `hud-pause`, `hud-play`, `hud-menu` смотрят на эти текстуры.
`install.ps1` ставит `hud.lua`, `uisprites.lua` и текстуры; `restore.ps1` всё откатывает.
После смены спрайтов или текстур игру нужно перезапустить.

## Установщик (Forts Setup)

Окно на WPF через PowerShell (ставить ничего не нужно), стекло Windows 11 и вид macOS Tahoe.
- `setup\app\engine.ps1` — логика: поиск игры, резервная копия `Forts-Setup-Backup`, откат по хэшам,
  перекраска прицела при установке, чтение разрешения игры. Проверка: `.\tools\test_setup_engine.ps1`.
- `setup\app\setup.ps1` — окно; `-Game <папка>`, `-Lang ru|en`, `-State cross|backup|progress|success|error`.
  Разметка в `setup\app\xaml\` (части склеиваются по номерам), стекло и анимации в `ui.ps1`,
  английские строки в `strings.ps1`, версия и репозиторий для проверки обновлений в `version.json`.
- Прицелы рисует `py -3 tools\draw_crosshairs.py`; фоны превью — `setup\app\assets\bg_*.png`.
- `.\tools\build_setup.ps1` собирает `dist\Forts-Setup.zip` и `dist\Forts Setup.exe`
  (лаунчер `tools\launcher\Launcher.cs` со встроенным архивом, распаковывается в `%LOCALAPPDATA%\FortsSetup`).
  Перед этим нужен `build\` с HUD: `.\install.ps1 -DryRun`. Тесты окна: `.\tools\test_setup_size_ui.ps1`.

## Порядок работы

1. Правишь `hud.lua` (координаты, размеры, шрифты, текстуры).
2. `.\install.ps1 -DryRun` проверяет синтаксис и показывает, что поменялось, ничего не копируя.
3. `.\install.ps1` ставит HUD в игру. Новый бой; если не видно изменений, перезапусти игру.
4. Что-то сломалось: `.\restore.ps1` (или Steam → Проверить целостность файлов).

`install.ps1` откажется работать, если:
- в `hud.lua` синтаксическая ошибка;
- из файла пропал или переименован элемент. Движок ищет элементы по имени, поэтому ненужное
  прячь (`Visible = false`, сдвиг за экран, прозрачный `Colour`), а не удаляй;
- файл в игре не совпадает ни с оригиналом, ни с твоей прошлой установкой (значит, вышел патч).

## Как устроены координаты

- Виртуальный экран высотой **600**; ширина зависит от пропорций (1067 при 16:9).
- `Position`/`Size` задаются относительно родителя. Формулы `ScriptX/ScriptY/ScriptW/ScriptH` их
  перекрывают. В формулах доступны `ScreenW`, `ScreenH`, `ParentL/R/T/B`, `ParentMiddleX/Y`,
  `PrevSiblingRight`, `PrevSiblingBottom`, `W`, `H`.
- `Anchor`, то есть какой точкой элемент ставится в позицию: 0 левый верх, 1 центр верха,
  2 центр левого края, 3 центр правого края, 4 центр низа, 5 левый низ, 6 правый низ,
  7 правый верх, 8 центр.
- У `Text` число `Size` — это кегль шрифта, а `Control.Size` — рамка текста.

## Где что лежит (основное)

| Что на экране | Элемент (путь в `hud_map.txt`) | Строка в `hud.lua` |
|---|---|---|
| Список игроков с пингом (слева сверху) | `root/ClientList`, строки `ClientName/ClientPing/ClientStatus` | 168 |
| Нижняя панель целиком | `root/HUD/HUDPanel` | 529 |
| Таймер матча | `HUDPanel/PanelCenter/Timer` | 560 |
| Металл и энергия | `HUDPanel/ResourcePanel/ReadoutMetal`, `ReadoutEnergy` (`Metal`, `MetalCap`, `MetalRate`…) | 610 |
| Кнопки меню/паузы/пина/помощника | `ResourcePanel/Menu`, `Pause`, `Pin`, `AssistantSettingsToggle`, `ExpandHUDToggle` | — |
| Стоимость выбранного предмета | `HUDPanel/ItemDetail` (`BuildMetal`, `FireMetal`…) | — |
| Вкладки и иконки постройки | `HUDItems/TabbedItemPanel`; иконка 53×71 в `PrototypeListItem/Button` | — |
| Командир и шкала | `HUDItems/COBackground` (`COGauge`, `COActivate`) | — |
| Полоски реакторов | `HUDItems/ReactorBars` | — |
| Ресурсы врага (шпион) | `HUDItems/ResourcePanelEnemy` | — |
| Полноэкранный режим предметов | `root/HUD/ExpandedHUDItems` | — |
| Вспышки пожарной тревоги по краям | `root/FireAlarmLeft/Right` (или опция `EnableVisualFireAlarm`) | — |

Нижняя панель собрана из кусков шириной 800 единиц от левого края: `PanelLeft` (0–192),
`PanelCenter` (215–585), `ResourcePanel` (608–800), все на y = 485 высотой 192 (уходят за низ
экрана). Свои раскладки удобнее делать, двигая детей внутри `HUDPanel`.

## Не проверено в игре

- Движок может сам двигать `HUDPanel` во время игры (выезд, скрытие, центрирование). Тогда
  координаты самой панели перезапишутся, а сдвиги детей внутри неё останутся.
- Неизвестно, считает ли мультиплеер изменённые UI-файлы расхождением. Первый раз проверь
  в закрытом лобби.

## После обновления игры

Если `install.ps1` пишет «похоже, игра обновилась»:

```powershell
Copy-Item "C:\Program Files (x86)\Steam\steamapps\common\Forts\data\ui\screens\hud.lua" original\hud.new.lua
py -3 tools\forts_ui.py compare original\hud.lua original\hud.new.lua   # что поменяли разработчики
py -3 tools\forts_ui.py decompile original\hud.new.lua -o build\hud.new.lua
```

Перенеси свои правки в `build\hud.new.lua`, затем замени им `hud.lua`, а `original\hud.new.lua`
переименуй в `original\hud.lua`.
# Размер прицела в Forts Setup

Ползунок размера: 50–200%, шаг 5%. Предпросмотр учитывает масштаб картинки и курсора. До 110% масштабируется рисунок в текстуре; выше 110% используются исходные DDS и штатные Input.Mouse.Cursors.Size/Offset, чтобы рисунок не обрезался. Настройка добавляется отдельным блоком в constants_user.lua; остальные настройки сохраняются. При уменьшении до 110% и ниже этот блок убирается. 100% сохраняет исходные DDS без изменений. Для 200% курсор имеет размер 64×64 вместо 32×32, с отступом −32,−32: центр остаётся на точке прицеливания. Изменения constants_user.lua включены в резервную копию и откат.

Готовые размеры создаёт `tools/make_crosshair_sizes.py` при сборке установщика. Пользователю Python не требуется. `tools/build_setup.ps1` принимает `-Python <путь>`; автоматически использует встроенный Python Codex, если он доступен.

Проверки: `tools/test_setup_engine.ps1` (установка размера, смена, определение и откат) и `tools/test_setup_size_ui.ps1` (настоящие WPF-контролы, предпросмотр, переключение вариантов, установка кнопкой и откат на тестовой папке). Проверка в матче после добавления масштаба не выполнялась.

