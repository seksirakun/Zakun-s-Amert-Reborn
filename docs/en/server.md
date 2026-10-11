# Server

[Manual](index.md) · [Install](install.md) · [Unity](unity.md) · [Lua](lua.md) · [Components](components.md) · [Doors and prefabs](doors.md)

Config is generated on first launch. Values below are the defaults in 2.1.0.

## Remote Admin

| Command | |
|---|---|
| `zamert` or `zm` | Version, schematic count, round variables, live object counts by component. |
| `zamert prefabs` | Refresh the network-prefab list, write `ZAMERT-prefabs.txt` next to the DLL, print the first 12 names. |
| `zamert prefabs door` | Same, filtered. The filter is a substring. |
| `zamert audio` | Forget cached audio clips. |

The prefab file is the one to copy names from. It is rewritten every time you run the command, from the server that executed it.

## Config

| Key | Default | |
|---|---|---|
| `Debug` | false | Extra logs. |
| `AudioFolderPath` | `LabAPI/audio` | Clip directory. Created if missing. |
| `AudioMasterVolume` | 1 | Server-wide scale. 0 mutes ZAMERT audio. |
| `EnablePerClientAudioVolume` | true | Adds a `ZAMERT - Audio Volume` slider to Server-Specific Settings. One-shot sounds only. |
| `EnableIoToys` | false | Interactables whose key is in `IoToysKeycodes` spawn as toys. |
| `IoToysKeycodes` | 0, 101 | None and E. |
| `IoToysNoRoot` | false | Skip toys on a root interactable. |
| `IoToysDebug` | false | Visible primitive where a toy interactable sits. |
| `IoToysBothModes` | false | Toy plus the settings keybind. 200 ms so one press does not run twice. |
| `InteractableObjectDetectionMode` | RaycastWithAssist | `RaycastOnly`, `RaycastWithAssist`, `ProximityOnly`. |
| `InteractableObjectAssistSphereRadius` | 0.35 | Assist probe size. |
| `InteractableObjectAssistAngleDegrees` | 12 | Aim cone for the assist. |
| `LuaEnabled` | true | Master switch for Lua Module and Lua Events. |
| `LuaCacheFileScripts` | true | Cache `ScriptPath` files, reload when the file's write time changes. |
| `LuaAllowServerCommands` | true | `api.command`. |
| `LuaAllowDelayedCallbacks` | true | `api.delay` and `api.repeat_every`. |
| `LuaMaxRepeatCount` | 100 | Cap on `api.repeat_every`. |
| `LuaLogApiErrors` | true | Log a failed `api.*` call instead of returning nil quietly. |
| `AutoRun` | false | Hook MER door spawning. Leave off unless you know that hook. |
| `GroovyNoiseFireOnEnable` | true | Enabling a Groovy Noise runs it once. |
| `DisableSSS` | false | Do not register ZAMERT keybinds or the audio slider. |
| `CustomSpawnPointEnable` | true | Custom spawn-point support. |
| `MaxFunctionWaitSeconds` | 600 | Cap for Function Executor `Wait` and for `api.wait`. 0 means no cap. |
| `LogSchematicSummary` | true | One line per schematic: how many rows loaded, how many skipped. |
| `LuaAutoYieldInstructions` | 2500 | Instructions before a Lua Module yields one frame. |
| `LuaMaxBusySlices` | 8 | Slices without `api.wait` before the script is stopped. |
| `LuaMaxProgramsPerFrame` | 8 | Module coroutines resumed on a single frame. |
| `Gates` | an example entry | Dummy gate definitions by map name. |

`Gates` ships with a placeholder map name. Replace it or leave it; an unknown map name does nothing.

## What the loader does

On schematic spawn, ZAMERT reads every sibling it knows. For each row it resolves the transform in this order: `ObjectNamePath`, full `ObjectId`, a legacy shorter index walk, and if both fields are empty the schematic root. Anything else is skipped and counted. `LogSchematicSummary` prints the totals.

Round variables (`api.set_round_var`) are cleared when the next map starts generating, not after schematic spawn. Keybinds are registered again after the map has generated. Destroyed objects are dropped from the registries; the registries are not wiped just because the map finished generating. Wiping there used to delete schematics that spawned during that same event.

## Audio

Files live in `AudioFolderPath`. `zamert audio` clears the clip cache after you replace a file. Per-player volume is the settings slider, multiplied by `AudioMasterVolume`. The slider does not affect looping speakers, only one-shots.

## When a script is killed

A Lua Module that never calls `api.wait` burns its instruction budget and stops. The log line is `Lua on {object} was stopped because it ran too long without api.wait()`. Put `api.wait` inside the loop. Eight coroutines already running on that module produce `already has 8 running scripts`.

A syntax error or a runtime error is `Lua module failed to start` or `Lua failed on {object}`, plus the Lua message. `LuaLogApiErrors` covers mistakes inside `api.give_item` and the other helpers (unknown item name, nil player) without killing the whole script.

## Old maps

Suffixes, `$type` assembly name `AdvancedMERTools`, and the misspelt fields stay. Loading a map made before 2.1.0 does not require a recompile, unless you want the new components. Adding Lua Module, Map Door fixes, motors, or prefab anchors means compiling those suffixes and shipping the new files next to the old ones.
