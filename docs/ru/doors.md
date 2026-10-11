# Двери и префабы

[Руководство](index.md) · [Установка](install.md) · [Unity](unity.md) · [Lua](lua.md) · [Компоненты](components.md) · [Сервер](server.md)

Дверью называют три разные вещи:

| Что нужно | Компонент |
|---|---|
| Настоящая дверь LCZ / HCZ / EZ / bulk / gate, с картой и замком | **Map Door** |
| Свой меш, который крутит Animator | **Custom Door** |
| Другой сетевой объект (workstation, шкафчик, мишень, игрушка, дверной префаб по сырому имени) | **Prefab Anchor** или `api.spawn_prefab` |

## Map Door

Поставьте Map Door на пустой объект там, где должна стоять дверь. Поворот и масштаб этого объекта — поворот и масштаб двери. Сборка пишет `{Имя}-MapDoors.json`.

| Поле | |
|---|---|
| `DoorType` | Enum Unity из таблицы ниже. По умолчанию Entrance, в файле это 5, на сервере появляется дверь EZ. |
| `Permissions` | `DoorPermissionFlags`. None — карта не нужна. |
| `Require All Permissions` | Нужен каждый выбранный флаг. |
| `Is Open` | Появиться открытой. |
| `Is Locked` | Появиться запертой специальным замком двери. |

![Значения Map Door](../assets/doors.svg)

Сервер применяет эту таблицу, когда schematic появляется. Собственный enum ProjectMER короче (0–4) и с этими числами не совпадает. ZAMERT переводит. Не перенумеровывайте старые `MapDoors.json`, чтобы их «починить».

Из Lua в рантайме этот enum не используется:

```lua
api.spawn_door("lcz")
api.spawn_door("hcz")
api.spawn_door("ez")       -- entrance
api.spawn_door("bulk")
api.spawn_door("gate")
api.spawn_door("ez", x, y, z)
```

`api.door_open(1)` и `api.door_lock(1)` действуют на ближайшую настоящую дверь в 6 метрах от объекта скрипта, включая двери, которые уже стояли на карте. Второе число меняет радиус. `0` закрывает или снимает замок.

Появление Map Door идёт через настройку дверей MER: префаб этого типа, замок, права, затем сетевой спавн в позе объекта. Если консоль пишет, что дверной префаб не готов, ProjectMER не закончил регистрацию префабов. ZAMERT грузится последним, так что обычно регистрация уже есть; типичная причина — не установлен плагин MER.

## Камера SCP-079

`Scp079Cameras.json`. `CameraType`: `Lcz` 0, `Hcz` 1, `Ez` 2, `EzArm` 3, `Sz` 4. `Label` — имя на экране 079, по умолчанию `Custom Camera`. Эти числа уже совпадают с MER. Камера ставится на transform объекта.

## Clutter

`Clutter.json`. Один из кусков мусора комплекса:

| Значение | Кусок |
|---|---|
| 0 | Simple Boxes |
| 1 | Pipes Short |
| 2 | Boxes Ladder |
| 3 | Tank Supported Shelf |
| 4 | Angled Fences |
| 5 | Huge Orange Pipes |
| 6 | Pipes Long Open |

## Prefab Anchor

Любой сетевой префаб, по имени, которое использует сервер.

| Поле | |
|---|---|
| `Prefab Name` | Точная строка. `Work Station`, не `workstation`. |
| `Spawn On Start` | Создать вместе со schematic. |
| `Spawn As Child` | Сделать ребёнком якоря. |
| `Match Scale` | Скопировать масштаб якоря. |
| `Disable Anchor Renderers` | Спрятать редакторский меш, который стоял как направляющая. |
| `Destroy Anchor After Spawn` | Удалить объект якоря, когда префаб уже есть. |

Имена для той сборки, которую вы запускаете:

```text
zamert prefabs
zamert prefabs door
```

Полный список пишется в `ZAMERT-prefabs.txt` рядом с DLL плагина. `api.list_prefabs("work")` возвращает в скрипт не больше 40 совпадений.

```lua
api.spawn_prefab("Work Station")
api.spawn_prefab("Work Station", 12, 1.2, 8)
```

`api.spawn_prefab` делает сетевой спавн, если net id ещё 0. К schematic экземпляр не привязывается, поэтому после удаления schematic объект остаётся. Prefab Anchor с Spawn As Child привязывает, и тогда объект уходит вместе со schematic. Map Door и `api.spawn_door` числятся на schematic и удаляются вместе с ним. Префаб, которому нужна комната, раскладка шкафчика или права двери, может подняться неполным. Двери комплекса проводите через Map Door или `api.spawn_door`. Workstation, мишени, игрушки и куски clutter — это работа Prefab Anchor.

Обновления игры переименовывают префабы. Когда имя перестаёт работать, выполните `zamert prefabs` на этом сервере и вставьте новое написание в `PrefabName`.
