# Lua

[Руководство](index.md) · [Установка](install.md) · [Unity](unity.md) · [Компоненты](components.md) · [Двери и префабы](doors.md) · [Сервер](server.md)

Lua Module — компонент. Скрипт работает на том объекте, куда вы его повесили. Общий файл всё равно означает отдельный запуск на каждый объект, поэтому их различает `object.name`.

![Какому объекту принадлежит on_interact](../assets/lua.svg)

## Инспектор

| Поле | Что ставить |
|---|---|
| `Inline Script` | Исходник. Используется, если не пустой. |
| `Script Path` | Файл `.lua` рядом со schematic, если Inline Script пуст. |
| `Input Key Code` | Unity KeyCode. `0` = нет клавиши. `101` = E. |
| `Interaction Max Range` | Метры. `0` считается как 5. |
| `Listen Enter / Stay / Exit` | `on_enter`, `on_stay`, `on_exit`, когда игрок касается trigger на этом объекте. |
| `Tick Interval` | Секунды между `on_tick`. Ниже `0.1` тик выключен. |
| `Script Group` | Ваша метка. Читается как `object.scriptGroup`. |

Сборка: `SchematicManager → Compile Lua Modules` или Compile All. Файл `{Имя}-LuaModules.json`.

## Когда что выполняется

Исходник выполняется один раз, когда schematic появляется. Этот проход объявляет функции и выполняет свободный код на верхнем уровне.

| Функция | Когда |
|---|---|
| код верхнего уровня | появление schematic |
| `on_interact` | игрок смотрит на этот модуль и жмёт его клавишу |
| `on_enter` | игрок входит в trigger |
| `on_stay` | игрок остаётся внутри, не чаще чем раз в 0.25 с |
| `on_exit` | игрок выходит из trigger |
| `on_tick` | каждый Tick Interval, если он не меньше 0.1 с |

Функции должны быть глобальными. `local function on_interact` для вызова события невидима.

```lua
function on_interact()
  api.hint(player.name .. " used " .. object.name, 2)
end
```

Объявляйте функции выше цикла, который ждёт. Пока интерпретатор не дошёл до строки, функции нет. Цикл, который на первом проходе вызывает `api.wait`, не доберётся до функции, написанной ниже.

`player` равен nil при появлении и в `on_tick`, если событие не принесло игрока. Читайте его в `on_interact`, `on_enter`, `on_stay`, `on_exit`.

## `object` и `player`

`object` (этот модуль):

| Поле | |
|---|---|
| `name` | Имя в иерархии. |
| `scriptGroup` | Вписанный Script Group. |
| `code` | Instance id сборки. Меняется при повторной сборке. |
| `objectId` | Путь из индексов. |
| `type` | `"LuaModule"`. |
| `active` | Флаг Active. |
| `position` | Позиция на момент сборки таблицы. `x`, `y`, `z`. |

`player`:

| Поле | |
|---|---|
| `id`, `name`, `userId` | Id игрока, ник, user id. |
| `role`, `team` | Имя роли и имя команды. Сравнение: `== "ClassD"`, `== "ChaosInsurgency"`. |
| `isScp`, `isHuman`, `isAlive` | Булевы значения. |
| `health` | Текущее здоровье. |
| `position` | `x`, `y`, `z`. |
| `room`, `zone` | Имена. |
| `currentItem` | Имя типа предмета или пустая строка. |

`schematic.name` — имя schematic MER. `schematic.directoryPath` — папка, откуда прочитан JSON.

Один скрипт, две кнопки:

```lua
function on_interact()
  if player.role == "ClassD" then
    api.hint("No.", 2)
    return
  end

  if object.name == "RedButton" then
    api.give_item(player, "KeycardGuard")
    api.spawn_door("ez")
  elseif object.name == "BlueButton" then
    api.door_lock(1)
  end
end
```

## `api.wait`

`api.wait(секунды)` ставит этот скрипт на паузу. Сервер продолжает работать. `api.wait(0)` ждёт один кадр.

```lua
for i = 1, 5 do
  api.broadcast("Count " .. i, 2)
  api.wait(1)
end
```

`while true` без `api.wait` останавливается. По умолчанию: 2500 инструкций Lua на срез, 8 срезов, затем скрипт убивается, а консоль пишет, что он работал слишком долго. Ключи конфига: `LuaAutoYieldInstructions`, `LuaMaxBusySlices`. На одном модуле может быть 8 корутин. За кадр продолжается не больше `LuaMaxProgramsPerFrame` скриптов (по умолчанию 8). `api.wait` ограничен `MaxFunctionWaitSeconds` (по умолчанию 600).

`api.wait` работает в Lua Module, потому что исходник идёт как корутина. Короткие скрипты `LuaEvents` на других компонентах корутинами не являются. Там ставьте `api.delay`.

## Игроки

```lua
api.broadcast("Hello", 5)                 -- текст, секунды, всем
api.hint("Look up", 2)                     -- текущий игрок
api.hint_player(player, "Look up", 2)      -- таблица игрока, id или ник
api.heal_player(player, 25)                -- или api.heal_player(25)
api.damage_player(player, 10, "fence")
api.kill_player(player, "pit")
api.teleport_player(player, 10, 2, 4)      -- или таблица {x=,y=,z=}
api.give_item(player, "KeycardGuard")      -- имя ItemType
api.set_role(player, "ClassD", 0)          -- третий аргумент 1 ещё и переносит на спавн
api.players()                              -- массив, индекс Lua с 1
api.alive_players()
api.humans()
api.scps()
api.player_count("ClassD")                 -- кусок роли или команды, пусто = все
api.find_player("Nick")                    -- id, ник, user id или таблица игрока
api.current_player()
```

`give_item` с одним аргументом отдаёт предмет текущему игроку. `set_role` берёт имя значения enum (`ClassD`, `Scientist`, `NtfSergeant`, `Scp173`, …). Неверное имя возвращает false и игрока не меняет.

## Двери и префабы

```lua
api.spawn_door("ez")              -- на месте этого объекта. ещё lcz, hcz, bulk, gate
api.spawn_door("gate", 4, 1, 8)   -- мировая позиция
api.door_open(1)                  -- ближайшая дверь в 6 м. 0 закрывает
api.door_open(1, 12)              -- то же, 12 м
api.door_lock(1)
api.spawn_prefab("Work Station")
api.spawn_prefab("Work Station", 10, 2, 4)
api.spawn_prefab("Work Station", {x = 10, y = 2, z = 4})
api.spawn_prefab("Work Station", 10, 2, 4, {x = 0, y = 90, z = 0}, {x = 1, y = 1, z = 1})
api.list_prefabs("door")          -- не больше 40 имён
api.prefab_count()
```

`spawn_door` берёт дверные префабы игры (ломаемые двери, bulk, gate) и убирает их вместе со schematic. `spawn_prefab` создаёт имя из `NetworkClient.prefabs` этого сервера. Позиция — три числа или одна таблица `{x,y,z}`. Поворот и масштаб — пятый и шестой аргументы, каждая таблица: имя, x, y, z, поворот, масштаб. Написание берите из `zamert prefabs`. Дверь, которую потом надо открывать и запирать, создавайте через `spawn_door`, не сырым префабом.

## Другие модули из Lua

Цель — code, имя объекта, или пропуск, если вызов разрешает «этот объект».

```lua
api.set_active(true)
api.set_active(object.code, false)
api.get_active()
api.set_active_group("doors", false)
api.destroy_self()
api.destroy_by_code(12345)
api.find_by_code(12345)
api.find_group("doors")
api.count_group("doors")
api.call_function("OpenHall")          -- Function Executor этого schematic
api.trigger(object.code, "interacted") -- LuaEvents на том объекте
api.trigger_group("doors", "custom")

api.counter_add("button", 1)           -- Counter по code или имени
api.timer_start("clock")
api.timer_stop("clock")
api.light_set("lamp", 2)               -- яркость, 0 выключает
api.rotator_speed("fan", 0, 90, 0)
api.mover_restart("platform")
api.spawn_schematic("SubRoom")         -- Schematic Anchor по имени, или создать это имя здесь

api.speaker_play("alarm")
api.speaker_stop("alarm")
api.speaker_volume("alarm", 0.4)
api.spawner_spawn("locker")
api.spawner_stop("locker")
api.spawner_reset("locker")
api.damage_trigger_start("pit")
api.damage_trigger_stop("pit")

api.get_current_health("dummy")
api.set_current_health("dummy", 50)
api.damage_healthobject("dummy", 10)
api.heal_healthobject("dummy", 10)
api.nearest_healthobject(8)
api.damage_nearest_healthobject(8, 15)
```

## Переменные

Три области. Ключи — строки. Значения — числа, строки или булевы.

```lua
api.set_self_var("uses", 0)             -- этот объект, пока его не уничтожат
api.inc_self_var("uses", 1)
api.get_self_var("uses")

api.set_schematic_var("power", 1)       -- этот экземпляр schematic
api.get_schematic_var("power")
api.inc_schematic_var("power", -1)

api.set_round_var("opened", true)       -- сбрасывается, когда генерируется следующая карта
api.get_round_var("opened")
```

## Время, шанс, файлы, команды

```lua
api.log("opened")
api.warn("missing clip")
api.error("bad state")
api.time()
api.realtime()
api.chance(0.25)                        -- true в 25% вызовов
api.random_int(1, 6)
api.random_float(0, 1)
api.vec(1, 2, 3)                        -- {x,y,z}
api.distance(a, b)
api.add_vec(a, b)

api.command("cassie 1 Attention")       -- нужен LuaAllowServerCommands
api.include("lib.lua")                  -- выполняется внутри этого скрипта
api.run_file("once.lua")                -- отдельный скрипт, local не общие

api.delay(2, function()
  api.broadcast("later", 3)
end)
api.repeat_every(1, 5, function(ctx, tick)
  api.broadcast("tick " .. tick, 1)
end)
```

`repeat_every` ждёт не меньше 0.05 с. Число повторов обрезается `LuaMaxRepeatCount` (по умолчанию 100). Если `LuaAllowDelayedCallbacks` выключен, `delay` и `repeat_every` ничего не делают.

`include` и `run_file` ищут сначала в папке schematic, потом рядом с DLL плагина. Абсолютный путь используется как есть.

## Lua Events на других компонентах

У каждого компонента ZAMERT есть список `Lua Events`. Это старый крючок: короткий скрипт на Interact, Death, Enter и так далее. Это не Lua Module. `api.wait` там падает; используйте `api.delay`.

| `Event Type` | Когда |
|---|---|
| `Spawned`, `Destroyed` | время жизни объекта |
| `Interacted`, `Denied` | клавиша принята или карта отклонена |
| `Searching`, `Picked` | pickup |
| `Damaged`, `Died` | health object |
| `Entered`, `Stayed`, `Exited` | коллайдер |
| `ThresholdReached`, `ThresholdDropped` | порог числа игроков |
| `Tick` | если компонент тикает |
| `Custom` | `Custom Event Name`, также `api.trigger` |

У строки есть priority, chance (`0`–`1`), cooldown, один раз, выполнять при отмене, остановить при успехе. Inline или файл, те же правила пути.

Внутри скрипта события те же глобалы (`player`, `object`, `api`). `ctx.cancelled`, `ctx.damage`, `ctx.currentHealth` зависят от события. `api.cancel()` / `api.uncancel()` помечают контекст. `api.stop()` обрывает следующие привязки этого события. События pickup могут вызвать `api.remove_pickup()` или `api.keep_pickup()`.

`LuaEnabled` false выключает и Lua Module, и эти привязки.
