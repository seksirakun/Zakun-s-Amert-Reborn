# Unity

[Руководство](index.md) · [Установка](install.md) · [Lua](lua.md) · [Компоненты](components.md) · [Двери и префабы](doors.md) · [Сервер](server.md)

## Порядок

На корне schematic стоит компонент MER `Schematic`. Имя GameObject — имя папки. Переименуйте корень до сборки.

1. Соберите сетку в редакторе MER.
2. Скомпилируйте её ProjectMER. Каталог берётся из `SchematicManager.Config.ExportPath`. Если пути нет, ZAMERT пишет в `Desktop\MapEditorReborn_CompiledSchematics`.
3. Повесьте компоненты ZAMERT на детей этого schematic.
4. `SchematicManager → Compile All ZAMERT`.

Шаг 4 пишет в папку из шага 2. Если папки нет, Console пишет `could not find the compiled-schematics directory`, и файлы не появляются.

Отдельные пункты (`Compile Lua Modules`, `Compile Map Doors`, …) пишут один суффикс. `Compile All ZAMERT` пишет все. `Decompile All` читает их обратно в иерархию. Сначала `ObjectNamePath`, потом `ObjectId`.

![Файлы в папке schematic](../assets/folder.svg)

## Что записывается

Простые компоненты (Lua Module, Map Door, Prefab Anchor, моторы, свет, таймеры, счётчики, schematic anchor, clutter, камеры, item spawner, damage trigger, loop speaker, custom door) всегда пишут `{Имя}-{Суффикс}.json`.

У компонентов с **Use Script Value** есть ещё файл `F`: `Objects.json` и `FObjects.json`, `HealthObjects.json` и `FHealthObjects.json`, то же для pickup, collider, toy, player-count trigger и custom texture. Включите **Use Script Value**, чтобы выгрузить вариант со скриптовыми значениями. Выключенный флаг пишет обычный список. Старая карта, где уже есть оба файла, сохраняет оба; загрузчик читает оба.

Function Executor отдельно. Он на корне schematic и пишет `{Имя}-Functions.json`.

Interactable Teleporter пишет `{TargetMapName}-ITeleporters.json` в корень экспорта, не внутрь папки schematic. `ObjectId` в этом файле — индекс телепорта с единицы. Скопируйте файл в каталог Maps у MER, рядом с картой.

## Поля идентификации

`ObjectId` и `ObjectNamePath` вручную не набирают. Их ставит сборка.

| Поле | Смысл |
|---|---|
| `ObjectNamePath` | Имена от корня schematic вниз, через `/`. Слэш внутри имени хранится как `\/`. Сервер пробует это первым. |
| `ObjectId` | Индексы соседей: сначала сам объект, потом родитель, через пробел. Если путь по имени не совпал. |
| `Code` | Instance id Unity на момент validate/сборки. Меняется, если пересобрать после перезагрузки домена. Годится для `api.find_by_code` внутри одной сборки. Это не постоянный id. |
| `Script Group` | Текст, который вы вписали в компонент. Стабильный. В Lua это `object.scriptGroup`, плюс `api.find_group`. |
| `Active` | Стартует включённым. Другие компоненты и Lua могут выключить. |

Переименование объекта или смена порядка соседей меняет путь. После этого соберите снова. Сервер пропускает строку, которую не смог найти, и пишет в лог несовпавшие `ObjectId` / `ObjectNamePath`.

## Клавиши

`InputKeyCode` — число Unity `KeyCode`. `E` это 101, `F` это 102, `None` это 0. У Lua Module 0 значит, что у модуля нет клавиши.

Сервер регистрирует одну строку Server-Specific Settings на каждую отдельную клавишу: `ZAMERT - Interactable Object - E`. Игрок назначает её один раз. Предложенная клавиша — та, что вы выбрали; игрок может переназначить. `DisableSSS` в конфиге отключает регистрацию, и тогда ни одна клавиша ZAMERT не работает.

Нажатие E на клавиатуре ничего не делает, пока этой строки нет и игрок её не назначил. Наблюдатели не вызывают её.

## Коллайдеры

Взаимодействие по клавише пускает луч из камеры. Нужен коллайдер на объекте или на ребёнке. Trigger тоже считается. Первое твёрдое попадание закрывает то, что за ним.

У Lua Module есть запасной путь: если луч ни во что не попал, модуль этой клавиши в пределах `InteractionMaxRange` (по умолчанию 5) и в пределах 35 градусов от взгляда всё ещё может сработать.

`ListenEnter` / `ListenStay` / `ListenExit` требуют trigger-коллайдер на том же объекте. Если коллайдер trigger и rigidbody нет, модуль добавляет kinematic rigidbody, иначе Unity не шлёт сообщения trigger. `on_stay` для одного коллайдера вызывается не чаще четырёх раз в секунду.

## Файл скрипта рядом с картой

`ScriptPath` — имя файла относительно папки schematic или абсолютный путь. Непустой `Inline Script` важнее. Пример: в папке schematic лежит `buttons.lua`, в поле Script Path модуля написано `buttons.lua`.
