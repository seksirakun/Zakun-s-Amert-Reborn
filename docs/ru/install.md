# Установка

[Руководство](index.md) · [Unity](unity.md) · [Lua](lua.md) · [Компоненты](components.md) · [Двери и префабы](doors.md) · [Сервер](server.md)

## Что уже должно стоять

- Выделенный сервер SCP: Secret Laboratory
- LabAPI 1.1.6 или новее
- ProjectMER, отдельным плагином

Приоритет загрузки ZAMERT — `Lowest`, он цепляется после MER.

Loop Speaker, звуки взаимодействия и `AudioModules` требуют AudioPlayerApi. Без него эти компоненты пишут ошибку, остальной плагин загружается.

## Сборка

Из этой папки:

```text
dotnet build ZAMERT\ZAMERT.csproj -c Release
```

Результат: `ZAMERT\bin\Release\`.

| Файл | Куда |
|---|---|
| `ZAMERT.dll` | `%AppData%\SCP Secret Laboratory\LabAPI\plugins\global\` |
| `MoonSharp.Interpreter.dll`, `0Harmony.dll`, `Newtonsoft.Json.dll` | В `%AppData%\SCP Secret Laboratory\LabAPI\dependencies\`, если лог сервера говорит, что их нет |

Папка порта (`plugins\7777\`) тоже подходит, если сервер разложен так. Одной копии достаточно.

`ZAMERT\libs\ProjectMER.dll` оставьте на месте. Это только ссылка для компиляции. На сервере работает тот ProjectMER, который вы поставили сами.

## Первый запуск

Запустите сервер один раз. LabAPI запишет конфиг ZAMERT в `LabAPI\configs\`. Точное имя файла зависит от версии LabAPI; это запись плагина `ZAMERT`.

Каталог звуков создаётся, если его нет:

`%AppData%\SCP Secret Laboratory\LabAPI\audio\`

Клипы кладите туда. `AudioName` у Loop Speaker — имя файла в этой папке.

## Проверка

Remote Admin:

```text
zamert
```

В ответе версия. После того как в раунде появились schematic, там же число живых объектов ZAMERT.

```text
zamert prefabs
zamert prefabs door
```

Пишет `ZAMERT-prefabs.txt` рядом с `ZAMERT.dll` и печатает первые имена. Это допустимые `PrefabName` и аргументы `api.spawn_prefab` для этой сборки игры. Обновление игры меняет имена. Берите файл с того сервера, на котором карта реально работает.

```text
zamert audio
```

Сбрасывает кэш клипов. Следующее воспроизведение читает файлы заново.

## Скрипты Unity

В DLL сервера редакторских скриптов нет. Скопируйте папку `Tools` из репозитория в проект карты:

```text
ВашПроектКарты\Assets\ZAMERT\
```

Редактор 2021.3.17f1. Когда скрипты скомпилируются, в меню появится `SchematicManager`. Если пункта нет, скрипты не лежат в `Assets`, либо в Console ошибка из-за другой версии редактора.

Дальше — порядок сборки. MER должен выгрузить schematic до того, как ZAMERT сможет записать свой JSON.
