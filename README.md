# ZAMERT

ProjectMER schematic logic for SCP: Secret Laboratory. LabAPI plugin plus the Unity authoring scripts.

Version 2.1.0. Author seksirakun48. Existing schematic JSON keeps the same file names and the same field names, including the old spellings (`GroovieNoise`, `AnimatorAdress`, `warheadActionType`, `dropItems`).

[Türkçe](docs/tr/index.md) · [English](docs/en/index.md) · [Русский](docs/ru/index.md) · [中文](docs/zh/index.md) · [한국어](docs/ko/index.md)

![Unity compile, then the server reads the sibling json files](docs/assets/pipeline.svg)

## What sits in this folder

| Path | What it is |
|---|---|
| `ZAMERT/` | Plugin. `dotnet build ZAMERT/ZAMERT.csproj -c Release` → `ZAMERT/bin/Release/ZAMERT.dll` |
| `Tools/` | Unity scripts. Copy into the map project's `Assets/ZAMERT`. |
| `docs/` | Usage manual. |

The plugin loads when a ProjectMER schematic spawns. MER owns `{Name}.json`. ZAMERT owns the siblings: `{Name}-Objects.json`, `{Name}-MapDoors.json`, `{Name}-LuaModules.json`, and the rest.

## Server, short version

Needs LabAPI 1.1.6 or newer, and ProjectMER already installed. ZAMERT loads at `LoadPriority.Lowest`, so MER is up first.

Copy `ZAMERT.dll` to:

`%AppData%\SCP Secret Laboratory\LabAPI\plugins\global\`

If the log says MoonSharp, 0Harmony, or Newtonsoft.Json is missing, copy those three from `ZAMERT/bin/Release/` into `LabAPI\dependencies\`. Do not replace the server's ProjectMER with the DLL under `ZAMERT/libs`.

Loop Speaker and one-shot audio need [AudioPlayerApi](https://github.com/Killers0992/AudioPlayerApi). Clips go in `LabAPI\audio\`.

Remote Admin: `zamert`, `zamert prefabs`, `zamert prefabs door`, `zamert audio`.

## Unity, short version

Editor 2021.3.17f1, the same editor ProjectMER's map project uses. Copy `Tools` to `Assets/ZAMERT`.

1. Compile the schematic with ProjectMER so the export folder exists. Default is `Desktop\MapEditorReborn_CompiledSchematics\{SchematicName}\`.
2. `SchematicManager → Compile All ZAMERT`.

That menu writes into the folder MER just created. It does not create the folder itself.

## Lua, one screen

Put **Lua Module** on an object. Give the object a collider if players will look at it. Pick a key (`E` is KeyCode 101). Players bind that key once in Server-Specific Settings; the row is named `ZAMERT - Interactable Object`.

```lua
function on_interact()
  if object.name == "RedButton" then
    api.give_item(player, "KeycardGuard")
    api.spawn_door("ez")
  end
end
```

`object` is the module that was hit. `player` is who pressed the key. `api.wait(1)` pauses a Lua Module script without freezing the server. Loops need that pause. The full list is in the manual.

![The script on a button, and which fields identify it](docs/assets/lua.svg)
