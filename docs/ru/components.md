# Компоненты

[Руководство](index.md) · [Установка](install.md) · [Unity](unity.md) · [Lua](lua.md) · [Двери и префабы](doors.md) · [Сервер](server.md)

Меню: `SchematicManager → Compile All ZAMERT`. В строке компонент, суффикс JSON и файл для schematic с именем `Hall`.

| Компонент | Суффикс | Файл |
|---|---|---|
| Interactable Object | `Objects` и `FObjects` | `Hall-Objects.json` |
| Health Object | `HealthObjects` / `FHealthObjects` | |
| Interactable Pickup | `Pickups` / `FPickups` | |
| Custom Collider | `Colliders` / `FColliders` | |
| Custom Door | `Doors` | дверь-анимация внутри schematic, не дверь комплекса |
| Custom Interactable Toy | `ToyInteractables` / `FToyInteractables` | |
| Loop Speaker | `LoopSpeakers` | |
| Item Spawner | `ItemSpawners` | |
| Damage Trigger | `DamageTriggers` | |
| Player Count Trigger | `PlayerCountTriggers` / `FPlayerCountTriggers` | |
| Prefab Anchor | `PrefabAnchors` | |
| Custom Texture | `CustomTextures` / `FCustomTextures` | |
| Map Door | `MapDoors` | дверь комплекса |
| Clutter | `Clutter` | |
| SCP-079 Camera | `Scp079Cameras` | |
| Spin Motor | `Rotators` | |
| Move Motor | `Movers` | |
| Light Controller | `Lights` | |
| Timer Relay | `Timers` | |
| Schematic Anchor | `SchematicAnchors` | |
| Counter | `Counters` | |
| Lua Module | `LuaModules` | |
| Function Executor | `Functions` | `Hall-Functions.json`, на корне |
| Interactable Teleporter | — | `{ИмяКарты}-ITeleporters.json` в корне экспорта |

Файл `F` пишется, когда включён **Use Script Value**. Там вместо чисел узлы `ScriptValue` (константа, переменная, случайное, стат игрока). Имена свойств внутри те же, что уже лежат в старых картах.

Общие списки действий стоят на interactable, health object, toy и player-count trigger:

| Список | Эффект |
|---|---|
| `MessageModules` | broadcast / hint / сообщение в духе cassie |
| `commandings` | команда серверной консоли |
| `dropItems` | создать предмет |
| `ExplodeModules` | взрыв |
| `effectGivingModules` | эффект состояния |
| `AudioModules` | разовый клип из папки звуков |
| `GroovieNoiseToCall` | запустить Groovy Noise по id или группе. Написание `Groovie` — формат файла. |
| `FunctionToCall` | функция Function Executor по имени |
| `AnimationModules` | проиграть состояние. `AnimatorAdress` — путь, написан так намеренно. |
| `warheadActionType` | действие боеголовки: старт, стоп, замок, подрыв |
| `PrimitiveModifyModules` | сдвинуть, перекрасить, переключить primitive |
| `LoopSpeakerModules` | играть, остановить, громкость |
| `ItemSpawnerModules` | создать, остановить, сбросить |
| `PlayerLinkModules` | привязать действие к игроку, который взаимодействовал |

Списки Deny на Interactable Object и Custom Interactable Toy срабатывают, когда проверка карты не прошла. Пустой `KeycardPermissions` — проверки карты нет. `RequireAllPermissions` требует каждый выбранный флаг.

## Interactable Object

`InputKeyCode`, `InteractionMaxRange` (0 при validate становится 5), `InteractionCooldown` (0 становится 0.2 с и общий для компонентов ZAMERT на этом объекте). `IgnoredRoles` пропускает эти роли. `ActionType` выбирает, какой набор действий выполнить.

Режим берётся из `InteractableObjectDetectionMode`: только луч, луч плюс узкий конус (`InteractableObjectAssistAngleDegrees`, по умолчанию 12) или ближайший в радиусе. Игрушки для части клавиш — отдельный переключатель, `EnableIoToys` и `IoToysKeycodes` (по умолчанию 0 и 101).

## Health Object

`Health`, `ArmorEfficient` (0–100), `DeadType`, `DeadActionDelay`, `ResetHPTo`, `DoNotDestroyAfterDeath`. `whitelistWeapons` ограничивает, какой предмет в руках наносит урон. `AllowedScpDamageTypes` — белый список ударов SCP вблизи; `All` разрешает каждый поддерживаемый удар SCP.

Если сообщение использует подстановки: `{attacker_i}`, `{attacker_name}`, `{a_pos}`, `{a_room}`, `{a_zone}`, `{a_role}`, `{s_pos}`, `{s_room}`, `{s_zone}`, `{a_item}`, `{damage}`.

## Item Spawner

`ItemType` (`None` выключает), `Count`, `LocalPosition`, `AutoStart`, `SpawnDelay` (0 даёт паузу 0.5 с), `RespawnTime` (0 — не создавать снова после того, как предметы забрали).

## Damage Trigger

`DamageAmount`, `DamageInterval`, `MinimumHealth` (0 разрешает убить), `AutoStart`, `TriggerMode` (вход, пребывание, выход или смесь), `KillInstant`, `AffectHumans`, `AffectScps`. На объекте нужен коллайдер, которого игрок может коснуться.

## Player Count Trigger

`TriggerThreshold`, `TriggerMode` (достижение или падение), `Cooldown`, `AutoStart` и общие списки действий. События Lua: `ThresholdReached`, `ThresholdDropped`.

## Loop Speaker

`AudioName` — файл в `LabAPI\audio`. `Volume`, `IsSpatial`, `MinDistance`, `MaxDistance`, `LocalPosition`, `AutoStart`. Именно этому компоненту нужен AudioPlayerApi.

## Custom Door

Анимированная дверь внутри schematic, не дверь LCZ/HCZ. `DoorType` здесь — категория для вашего аниматора: `LCZ`, `HCZ` или `EZ`. `OpenAnimation`, `CloseAnimation`, `LockAnimation` по умолчанию `DoorOpen`, `DoorClose`, `DoorLock`. Объект аниматора пишется в `Animator` как путь по иерархии. Настоящая дверь комплекса — это Map Door.

## Custom Texture

Кладёт текстуру schematic на грани. `SchematicName`, `Mode`, `StretchX`, `StretchY`, `Faces`, `ZFightOffset` (по умолчанию 0.005), `MaxDuplicates` (по умолчанию 256), `ManualSize`, `FlipNormal`, `PivotOffset`. Скриптовый файл — `FCustomTextures`.

## Spin Motor, Move Motor, Light

| | Поля |
|---|---|
| Spin Motor | `AngularSpeed` (градусы в секунду, `x`,`y`,`z`, по умолчанию y = 45), `UseWorldSpace` |
| Move Motor | `LocalOffset`, `Duration` (по умолчанию 2), `StartDelay`, `PingPong`, `Loop`, `AutoStart` |
| Light Controller | На источнике света. `ApplyOnStart`, `Enabled`, `OverrideColor`, `Color`, `OverrideIntensity`, `Intensity` (0 = выкл), `OverrideRange`, `Range`, `Pulse`, `PulseSpeed`, `PulseMin`, `PulseMax` |

Lua: `api.rotator_speed`, `api.mover_restart`, `api.light_set`.

## Timer и Counter

Timer Relay: `Delay`, `Interval` (по умолчанию 1), `Repeat`, `RepeatCount` (0 при включённом Repeat не останавливается), `AutoStart`, затем одна цель: `FunctionName`, или `GroovyNoiseId`, или `GroovyNoiseGroup`.

Counter: `StartValue`, `TargetValue` (по умолчанию 1), `Step` (по умолчанию 1), `ResetOnReach` и те же три цели, когда значение доходит до цели. Lua прибавляет через `api.counter_add`.

## Schematic Anchor

Создаёт другой уже собранный schematic. `SchematicName`, `SpawnOnStart`, `Scale`. Глубина не больше 4. Schematic не создаёт сам себя через собственный anchor. `api.spawn_schematic("Имя")` создаёт это имя на transform вызывающего или просит именованный anchor.

## Function Executor

Ставится на корень schematic. Один файл, `{Имя}-Functions.json`. Публичные функции вызываются из `FunctionToCall`, из Timer/Counter или через `api.call_function("Имя")`.

Узлы в том порядке, в каком вы их сложили:

`If`, `ElseIf`, `Else`, `While`, `For`, `ForEach`, `SetVariable`, `Return`, `Wait`, `CallFunction`, `CallGroovyNoise`, `PlayAnimation`, `SendMessage`, `SendCommand`, `DropItems`, `Explode`, `GiveEffect`, `PlayAudio`, `FWarhead`, `ChangePlayerValue`, `PlayerAction`, `ChangeEntityValue`.

`Wait` удерживает остаток этой функции. Потолок — `MaxFunctionWaitSeconds`. `While` / `For` / `ForEach`, которые доходят до `Wait`, продолжаются после паузы, а не крутятся вхолостую. Вложенный `If` вместе с `Wait` продолжает и родителя, и ребёнка после одной и той же паузы. Если порядок важен, держите wait в родителе.

Значения внутри компонентов `F` и внутри функций (`Integer`, `Float`, здоровье игрока, проверка роли, `TrueForAll` и остальные) — типы Script Value. Имена `$type` остаются в сборке `AdvancedMERTools`, поэтому старые файлы по-прежнему читаются.

## Interactable Teleporter

`TargetMapName` — имя карты MER, `TeleporterIndex` — индекс телепорта в этой карте, с единицы. Сборка пишет `{TargetMapName}-ITeleporters.json` в корень экспорта. Скопируйте его в папку Maps у MER. Сервер читает файл, когда эта карта загружается. `InvokeType` выбирает столкновение или другой крючок телепорта. Списки действий те же, что у interactable.

## Custom Collider, Groovy Noise, игрушки

Custom Collider — игровой коллайдер, который уходит вместе со schematic (`Colliders.json`). При касании использует те же наборы действий.

Groovy Noise — именованный набор действий, который можно вызвать снова. Другие компоненты зовут его по id (`GroovieNoiseId`) или по `Script Group`. `GroovyNoiseFireOnEnable` (по умолчанию включён) запускает Groovy Noise один раз, когда кто-то включает его `Active`. Старые многоступенчатые двери на этом держатся.

Custom Interactable Toy — interactable для primitive-игрушек: `IncludeRootPrimitive`, `IncludeChildPrimitives`, `ToyScaleMultiplier` (по умолчанию 1.3) и те же списки действий и отказов. Именно его подставляет `EnableIoToys` для перечисленных клавиш.
