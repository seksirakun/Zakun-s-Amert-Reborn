# Сервер

[Руководство](index.md) · [Установка](install.md) · [Unity](unity.md) · [Lua](lua.md) · [Компоненты](components.md) · [Двери и префабы](doors.md)

Конфиг появляется при первом запуске. Значения ниже — умолчания 2.1.0.

## Remote Admin

| Команда | |
|---|---|
| `zamert` или `zm` | Версия, число schematic, переменные раунда, живые объекты по компонентам. |
| `zamert prefabs` | Обновить список сетевых префабов, записать `ZAMERT-prefabs.txt` рядом с DLL, напечатать первые 12 имён. |
| `zamert prefabs door` | То же с фильтром. Фильтр — подстрока. |
| `zamert audio` | Забыть закэшированные клипы. |

Файл префабов — тот, из которого копируют имена. Он переписывается при каждом запуске команды, с того сервера, который команду выполнил.

## Конфиг

| Ключ | По умолчанию | |
|---|---|---|
| `Debug` | false | Дополнительные логи. |
| `AudioFolderPath` | `LabAPI/audio` | Каталог клипов. Создаётся, если его нет. |
| `AudioMasterVolume` | 1 | Общий множитель сервера. 0 глушит звук ZAMERT. |
| `EnablePerClientAudioVolume` | true | Добавляет в Server-Specific Settings ползунок `ZAMERT - Audio Volume`. Только разовые звуки. |
| `EnableIoToys` | false | Interactable, чья клавиша есть в `IoToysKeycodes`, появляются как игрушки. |
| `IoToysKeycodes` | 0, 101 | None и E. |
| `IoToysNoRoot` | false | Не делать игрушку на корневом interactable. |
| `IoToysDebug` | false | Видимый primitive там, где стоит toy-interactable. |
| `IoToysBothModes` | false | И игрушка, и клавиша в настройках. 200 мс, чтобы одно нажатие не сработало дважды. |
| `InteractableObjectDetectionMode` | RaycastWithAssist | `RaycastOnly`, `RaycastWithAssist`, `ProximityOnly`. |
| `InteractableObjectAssistSphereRadius` | 0.35 | Размер пробы assist. |
| `InteractableObjectAssistAngleDegrees` | 12 | Конус прицеливания для assist. |
| `LuaEnabled` | true | Общий выключатель Lua Module и Lua Events. |
| `LuaCacheFileScripts` | true | Кэш файлов `ScriptPath`, перечитать, когда меняется время записи. |
| `LuaAllowServerCommands` | true | `api.command`. |
| `LuaAllowDelayedCallbacks` | true | `api.delay` и `api.repeat_every`. |
| `LuaMaxRepeatCount` | 100 | Потолок `api.repeat_every`. |
| `LuaLogApiErrors` | true | Писать в лог сбойный вызов `api.*`, а не тихо возвращать nil. |
| `AutoRun` | false | Крючок появления дверей MER. Оставьте выключенным, если этот крючок вам не нужен. |
| `GroovyNoiseFireOnEnable` | true | Включение Groovy Noise запускает его один раз. |
| `DisableSSS` | false | Не регистрировать клавиши ZAMERT и ползунок громкости. |
| `CustomSpawnPointEnable` | true | Поддержка своих точек появления. |
| `MaxFunctionWaitSeconds` | 600 | Потолок `Wait` у Function Executor и `api.wait`. 0 — без потолка. |
| `LogSchematicSummary` | true | Одна строка на schematic: сколько строк загружено, сколько пропущено. |
| `LuaAutoYieldInstructions` | 2500 | Сколько инструкций Lua Module выполняет до отдачи кадра. |
| `LuaMaxBusySlices` | 8 | Сколько срезов без `api.wait`, после чего скрипт останавливается. |
| `LuaMaxProgramsPerFrame` | 8 | Сколько корутин модулей продолжается за один кадр. |
| `Gates` | пример | Описания dummy gate по имени карты. |

`Gates` поставляется с именем-заглушкой. Замените его или оставьте: неизвестное имя карты ничего не делает.

## Что делает загрузчик

Когда schematic появляется, ZAMERT читает каждый известный соседний файл. Для каждой строки transform ищется так: `ObjectNamePath`, полный `ObjectId`, старый более короткий обход индексов, и если оба поля пусты — корень schematic. Остальное пропускается и считается. `LogSchematicSummary` печатает итог.

Переменные раунда (`api.set_round_var`) сбрасываются, когда начинается генерация следующей карты, а не после появления schematic. Клавиши регистрируются снова после генерации карты. Уничтоженные объекты вычёркиваются из реестров; реестры не очищаются только потому, что карта догенерировалась. Очистка в этот момент раньше удаляла schematic, которые появлялись в том же событии.

## Звук

Файлы лежат в `AudioFolderPath`. `zamert audio` чистит кэш клипов после замены файла. Громкость игрока — ползунок в настройках, умноженный на `AudioMasterVolume`. Ползунок не трогает циклические динамики, только разовые звуки.

## Когда скрипт останавливают

Lua Module, который ни разу не вызвал `api.wait`, сжигает бюджет инструкций и останавливается. Строка лога: `Lua on {объект} was stopped because it ran too long without api.wait()`. Поставьте `api.wait` внутрь цикла. Если на модуле уже работают 8 корутин, будет `already has 8 running scripts`.

Ошибка синтаксиса или выполнения — это `Lua module failed to start` или `Lua failed on {объект}` плюс сообщение Lua. `LuaLogApiErrors` покрывает ошибки внутри `api.give_item` и других помощников (неизвестное имя предмета, nil-игрок) и не убивает весь скрипт.

## Старые карты

Суффиксы, имя сборки в `$type` (`AdvancedMERTools`) и поля с историческими опечатками остаются. Карта, собранная до 2.1.0, грузится без пересборки. Новые компоненты — отдельный шаг: скомпилировать эти суффиксы и положить новые файлы рядом со старыми.
