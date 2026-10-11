# Components

[Manual](index.md) · [Install](install.md) · [Unity](unity.md) · [Lua](lua.md) · [Doors and prefabs](doors.md) · [Server](server.md)

Menu: `SchematicManager → Compile All ZAMERT`. Each row is one component, the JSON suffix, and the file it produces for a schematic named `Hall`.

| Component | Suffix | File |
|---|---|---|
| Interactable Object | `Objects` and `FObjects` | `Hall-Objects.json` |
| Health Object | `HealthObjects` / `FHealthObjects` | |
| Interactable Pickup | `Pickups` / `FPickups` | |
| Custom Collider | `Colliders` / `FColliders` | |
| Custom Door | `Doors` | animated schematic door, not a facility door |
| Custom Interactable Toy | `ToyInteractables` / `FToyInteractables` | |
| Loop Speaker | `LoopSpeakers` | |
| Item Spawner | `ItemSpawners` | |
| Damage Trigger | `DamageTriggers` | |
| Player Count Trigger | `PlayerCountTriggers` / `FPlayerCountTriggers` | |
| Prefab Anchor | `PrefabAnchors` | |
| Custom Texture | `CustomTextures` / `FCustomTextures` | |
| Map Door | `MapDoors` | facility door |
| Clutter | `Clutter` | |
| SCP-079 Camera | `Scp079Cameras` | |
| Spin Motor | `Rotators` | |
| Move Motor | `Movers` | |
| Light Controller | `Lights` | |
| Timer Relay | `Timers` | |
| Schematic Anchor | `SchematicAnchors` | |
| Counter | `Counters` | |
| Lua Module | `LuaModules` | |
| Function Executor | `Functions` | `Hall-Functions.json`, on the root |
| Interactable Teleporter | — | `{MapName}-ITeleporters.json` in the export root |

The `F` file is written when **Use Script Value** is on. Those fields are `ScriptValue` nodes (a constant, a variable, a random, a player stat) instead of raw numbers. The property names inside are the same ones old maps already use.

Shared action lists show up on interactables, health objects, toys and player-count triggers:

| List | Effect |
|---|---|
| `MessageModules` | broadcast / hint / cassie-style messages |
| `commandings` | server console command |
| `dropItems` | spawn an item |
| `ExplodeModules` | explosion |
| `effectGivingModules` | status effect |
| `AudioModules` | one-shot clip from the audio folder |
| `GroovieNoiseToCall` | run a Groovy Noise by id or group. The spelling `Groovie` is the file format. |
| `FunctionToCall` | run a Function Executor function by name |
| `AnimationModules` | play a state. `AnimatorAdress` is the path, spelled that way on purpose. |
| `warheadActionType` | warhead start / stop / lock / detonate style action |
| `PrimitiveModifyModules` | move, recolor, toggle a primitive |
| `LoopSpeakerModules` | play, stop, volume |
| `ItemSpawnerModules` | spawn, stop, reset |
| `PlayerLinkModules` | tie the action to the player who interacted |

Deny lists on Interactable Object and Custom Interactable Toy run when the keycard check fails. `KeycardPermissions` empty means no card check. `RequireAllPermissions` demands every selected flag.

## Interactable Object

`InputKeyCode`, `InteractionMaxRange` (0 becomes 5 on validate), `InteractionCooldown` (0 becomes 0.2 s, shared by ZAMERT components on that object). `IgnoredRoles` skips those roles. `ActionType` selects which action bundle runs.

Detection is the config `InteractableObjectDetectionMode`: ray only, ray plus a small aim cone (`InteractableObjectAssistAngleDegrees`, default 12), or nearest in range. Toys for some keys are a separate switch, `EnableIoToys` plus `IoToysKeycodes` (default 0 and 101).

## Health Object

`Health`, `ArmorEfficient` (0–100), `DeadType`, `DeadActionDelay`, `ResetHPTo`, `DoNotDestroyAfterDeath`. `whitelistWeapons` limits which held items hurt it. `AllowedScpDamageTypes` is the SCP melee whitelist; `All` allows every supported SCP hit.

Message format tokens, if a message uses them: `{attacker_i}`, `{attacker_name}`, `{a_pos}`, `{a_room}`, `{a_zone}`, `{a_role}`, `{s_pos}`, `{s_room}`, `{s_zone}`, `{a_item}`, `{damage}`.

## Item Spawner

`ItemType` (`None` disables it), `Count`, `LocalPosition`, `AutoStart`, `SpawnDelay` (0 uses a 0.5 s grace), `RespawnTime` (0 means do not respawn after the items are taken).

## Damage Trigger

`DamageAmount`, `DamageInterval`, `MinimumHealth` (0 allows a kill), `AutoStart`, `TriggerMode` (enter, stay, exit, or a mix), `KillInstant`, `AffectHumans`, `AffectScps`. The object needs a collider the player can touch.

## Player Count Trigger

`TriggerThreshold`, `TriggerMode` (reach or drop), `Cooldown`, `AutoStart`, plus the shared action lists. Lua events: `ThresholdReached`, `ThresholdDropped`.

## Loop Speaker

`AudioName` is a file in `LabAPI\audio`. `Volume`, `IsSpatial`, `MinDistance`, `MaxDistance`, `LocalPosition`, `AutoStart`. This is the one that needs AudioPlayerApi.

## Custom Door

An animated door inside the schematic, not an LCZ/HCZ door. `DoorType` here is `LCZ`, `HCZ`, or `EZ` as a category for your animator. `OpenAnimation`, `CloseAnimation`, `LockAnimation` default to `DoorOpen`, `DoorClose`, `DoorLock`. The animator object is stored as `Animator` (a hierarchy path). For a real facility door, use Map Door.

## Custom Texture

Projects a texture from a schematic onto faces. `SchematicName`, `Mode`, `StretchX`, `StretchY`, `Faces`, `ZFightOffset` (default 0.005), `MaxDuplicates` (default 256), `ManualSize`, `FlipNormal`, `PivotOffset`. The scripted file is `FCustomTextures`.

## Spin Motor, Move Motor, Light

| | Fields |
|---|---|
| Spin Motor | `AngularSpeed` (`x`,`y`,`z` degrees per second, default y = 45), `UseWorldSpace` |
| Move Motor | `LocalOffset`, `Duration` (default 2), `StartDelay`, `PingPong`, `Loop`, `AutoStart` |
| Light Controller | On a light. `ApplyOnStart`, `Enabled`, `OverrideColor`, `Color`, `OverrideIntensity`, `Intensity` (0 = off), `OverrideRange`, `Range`, `Pulse`, `PulseSpeed`, `PulseMin`, `PulseMax` |

Lua: `api.rotator_speed`, `api.mover_restart`, `api.light_set`.

## Timer and Counter

Timer Relay: `Delay`, `Interval` (default 1), `Repeat`, `RepeatCount` (0 with Repeat keeps going), `AutoStart`, then one target: `FunctionName`, or `GroovyNoiseId`, or `GroovyNoiseGroup`.

Counter: `StartValue`, `TargetValue` (default 1), `Step` (default 1), `ResetOnReach`, and the same three targets when the value hits the target. Lua adds with `api.counter_add`.

## Schematic Anchor

Spawns another compiled schematic. `SchematicName`, `SpawnOnStart`, `Scale`. Depth is capped at 4. A schematic will not spawn itself through its own anchor. `api.spawn_schematic("Name")` spawns that name at the caller's transform, or asks a named anchor to spawn.

## Function Executor

Put it on the schematic root. One asset, `{Name}-Functions.json`. Public functions are called from `FunctionToCall`, from a Timer/Counter, or from `api.call_function("Name")`.

Nodes, in the order you stack them:

`If`, `ElseIf`, `Else`, `While`, `For`, `ForEach`, `SetVariable`, `Return`, `Wait`, `CallFunction`, `CallGroovyNoise`, `PlayAnimation`, `SendMessage`, `SendCommand`, `DropItems`, `Explode`, `GiveEffect`, `PlayAudio`, `FWarhead`, `ChangePlayerValue`, `PlayerAction`, `ChangeEntityValue`.

`Wait` pauses the rest of that function. It is capped by `MaxFunctionWaitSeconds`. A `While` / `For` / `ForEach` that hits `Wait` resumes after the delay instead of spinning. Nested `If` plus `Wait` resumes the parent and the child after the same delay; keep waits in the parent if the order matters.

Values inside `F` components and inside functions (`Integer`, `Float`, player health, role checks, `TrueForAll`, and the rest) are the Script Value types. Their `$type` names stay in the assembly name `AdvancedMERTools` so old files still deserialize.

## Interactable Teleporter

Set `TargetMapName` to the MER map name and `TeleporterIndex` to the 1-based teleport index in that map. Compile writes `{TargetMapName}-ITeleporters.json` into the export root. Copy it into MER's Maps folder. The server reads it when that map loads. `InvokeType` chooses collide versus the other teleport hooks. The action lists are the same bundles as an interactable.

## Custom Collider, Groovy Noise, toys

Custom Collider is a gameplay collider exported with the schematic (`Colliders.json`), with the same action bundles as other modules when something touches it.

Groovy Noise is a named, reusable action bundle. Other components call it by the id (`GroovieNoiseId`) or by `Script Group`. `GroovyNoiseFireOnEnable` (default on) fires a Groovy Noise once when something sets its `Active` on. Old multi-stage doors depend on that.

Custom Interactable Toy is the interactable aimed at primitive toys: `IncludeRootPrimitive`, `IncludeChildPrimitives`, `ToyScaleMultiplier` (default 1.3), plus the same action and deny lists. It is what `EnableIoToys` swaps in for the keycodes you list.
