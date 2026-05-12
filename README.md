# Zakun's AMERT Reborn (ZAMERT)

A reborn and heavily expanded version of **AdvancedMERTools** for SCP: Secret Laboratory servers.

## Important

You should install **the latest ProjectMER** before using ZAMERT.

ZAMERT is built as an advanced **ProjectMER helper**. It extends schematic objects with runtime logic, Lua events, custom interactions, damage zones, prefab spawning, SDK hooks, and more.

## What is this?

ZAMERT is a LabAPI plugin that adds advanced logic to ProjectMER schematics:

- destructible **HealthObjects**
- keybind and toy based **InteractableObjects**
- **InteractablePickups**
- **CustomColliders**
- **CustomInteractableToys**
- **KillPart / DamageTrigger** style areas
- **PlayerCountTriggers**
- schematic **prefab anchors**
- custom doors, dummy doors, dummy gates
- GroovyNoise macros
- FunctionExecutor scripting
- audio, animation, primitive edits, item spawners, teleporters, and more

## Main Features

- **Lua Event Runtime**
  - Every main ZAMERT object can now carry `LuaEvents`
  - Supports inline Lua and file based Lua scripts
  - Supports events such as `spawned`, `destroyed`, `interacted`, `denied`, `searching`, `picked`, `damaged`, `died`, `entered`, `stayed`, `exited`, `thresholdreached`, `thresholddropped`, and `tick`

- **CustomInteractableToy**
  - New ZAMERT object type for toy based interaction without needing a keybind
  - Supports the same action flow style as InteractableObject, including deny logic and function execution

- **KillPart / DamageTrigger Upgrade**
  - Damage zones now support `OnEnter`, `OnStay`, and `OnExit`
  - Supports instant kill mode
  - Supports human/SCP filtering

- **Prefab Anchor Runtime**
  - Spawn registered network prefabs directly from schematic anchors
  - Can spawn on start, as child, scale matched, and optionally hide or remove the anchor

- **Unity CustomDoor Pipeline**
  - Custom doors can now be authored directly in Unity and exported with the schematic
  - Supports door type, animator target, permissions, require-all permissions, locked/open state, health, damage type, and install position/rotation/scale
  - Decompile now restores CustomDoor data back into the Unity scene as well

- **Expanded Deny Flow**
  - Deny actions support messages, audio, animations, functions, warhead, drop items, commands, explode, effects, primitive edits, loop speakers, and item spawners

- **SDK / API**
  - Other plugins can query ZAMERT objects, damage HealthObjects, trigger Lua events, and spawn registered prefabs

- **SCP vs HealthObject Support**
  - SCP-939, SCP-096, SCP-173, SCP-049, and SCP-049-2 can now damage HealthObjects through their intended interaction flow

- **Unity Tools Fixes**
  - Improved path export/import stability
  - Fixed script value animation target restoration in importer
  - Fixed deny animation target restoration so deny animations keep the exact animator path after decompile/import
  - Added server keybind label preview for InteractableObjects
  - Same-object multi-component toy interactions now fan out correctly instead of only one component firing

## Lua Examples

Attach a `LuaEventBinding` to a supported ZAMERT object and choose the matching event type.

### Denied Example

Event type: `Denied`

```lua
api.log("Access denied on " .. object.name .. " by " .. player.name)
api.hint("Access denied.", 3)
```

### HealthObject Death Example

Event type: `Died`

```lua
api.broadcast("A HealthObject was destroyed: " .. object.name, 5)
api.set_round_var("generator_destroyed", true)
```

### KillPart Tick Example

Event type: `Tick`

```lua
if player.isScp then
    api.log("SCP entered forbidden area: " .. player.name)
end
```

### Spawn Prefab Example

Event type: `Interacted`

```lua
api.spawn_prefab("Simple Boxes Open Connector")
api.call_function("OnSpawnedConnector")
```

## Lua API Summary

Available helpers inside Lua include:

- `api.log(text)`
- `api.command(text)`
- `api.broadcast(text, duration)`
- `api.hint(text, duration)`
- `api.set_active(bool)`
- `api.get_active()`
- `api.destroy_self()`
- `api.cancel()`
- `api.remove_pickup()`
- `api.call_function(name, ...)`
- `api.get_round_var(name)`
- `api.set_round_var(name, value)`
- `api.get_schematic_var(name)`
- `api.set_schematic_var(name, value)`
- `api.get_current_health()`
- `api.set_current_health(value)`
- `api.damage_healthobject(objectIdOrCode, damage)`
- `api.damage_nearest_healthobject(maxDistance, damage)`
- `api.spawn_prefab(prefabName, x, y, z)`

Globals available inside Lua:

- `ctx`
- `player`
- `object`
- `schematic`

## Requirements

- SCP: Secret Laboratory
- LabAPI `>= 1.1.6`
- **Latest ProjectMER**

## Setup

1. Install the latest **ProjectMER** first.
2. Put `ZAMERT.dll` into:
   - `SCPSL/LabAPI/Plugins/Global/`
3. Put every file from the repo `dependencies` folder into:
   - `SCPSL/LabAPI/Dependencies/Global/`
4. Restart the server.

Config is auto-generated on first run.

## Configuration

Config file path:

- `SCPSL/LabAPI/configs/<port>/ZAMERT.yml`

Main keys:

| Key | Default | Description |
|---|---|---|
| `debug` | `false` | Enables debug output and ZMapper tracing |
| `audio_folder_path` | `LabAPI/audio` | Folder for audio files |
| `enable_io_toys` | `false` | Enables InteractableToy support for InteractableObjects |
| `io_toys_keycodes` | `[0, 101]` | Keycodes that should use toy mode |
| `io_toys_no_root` | `false` | Skips root primitive toy spawning |
| `io_toys_debug` | `false` | Spawns visible debug indicators for toy hitboxes |
| `io_toys_both_modes` | `false` | Keeps both toy mode and SSS keybind mode active |
| `disable_sss` | `false` | Disables ZAMERT server-specific keybind registration |
| `custom_spawn_point_enable` | `true` | Enables schematic-defined spawnpoints |

## SDK / Plugin API

ZAMERT exposes helpers for other plugins through `ZAMERTApi`:

- `GetHealthObjects()`
- `GetInteractableObjects()`
- `GetInteractablePickups()`
- `GetCustomInteractableToys()`
- `GetPlayerCountTriggers()`
- `GetPrefabAnchors()`
- `FindNearestHealthObject(...)`
- `TryDamageHealthObject(...)`
- `ExecuteLuaEvent(...)`
- `GetRegisteredPrefabNames()`
- `SpawnRegisteredPrefab(...)`

## Credits

- Original **AdvancedMERTools** idea by **Mujishung**
- Reborn and expanded by **seksirakun48 (zakun)**
