# Install

[Manual](index.md) · [Unity](unity.md) · [Lua](lua.md) · [Components](components.md) · [Doors and prefabs](doors.md) · [Server](server.md)

## What has to be there already

- SCP: Secret Laboratory dedicated server
- LabAPI 1.1.6 or newer
- ProjectMER, loaded as its own plugin

ZAMERT's load priority is `Lowest`, so it attaches after MER.

Loop Speaker, interaction sounds and `AudioModules` need AudioPlayerApi on the server. Without it those components log an error and the rest of the plugin still loads.

## Build

From this repo:

```text
dotnet build ZAMERT\ZAMERT.csproj -c Release
```

Output: `ZAMERT\bin\Release\`.

| File | Where it goes |
|---|---|
| `ZAMERT.dll` | `%AppData%\SCP Secret Laboratory\LabAPI\plugins\global\` |
| `MoonSharp.Interpreter.dll`, `0Harmony.dll`, `Newtonsoft.Json.dll` | `%AppData%\SCP Secret Laboratory\LabAPI\dependencies\` if the server log says they are missing |

A port-specific folder (`plugins\7777\`) also works if that is how the server is split. One copy is enough.

Leave `ZAMERT\libs\ProjectMER.dll` where it is. That file is only a compile reference. The running server uses the ProjectMER plugin you installed yourself.

## First boot

Start the server once. LabAPI writes `LabAPI\configs\ZAMERT\ZAMERT.yml` (the exact filename follows your LabAPI version; it is the config for plugin `ZAMERT`).

The audio directory is created if it is missing:

`%AppData%\SCP Secret Laboratory\LabAPI\audio\`

Put clips there. Loop Speaker's `AudioName` is the file name inside that folder.

## Check that it loaded

Remote Admin:

```text
zamert
```

The reply includes the version and, once a round has spawned schematics, how many live ZAMERT objects exist.

```text
zamert prefabs
zamert prefabs door
```

Writes `ZAMERT-prefabs.txt` next to `ZAMERT.dll` and prints the first names. That list is the legal `PrefabName` / `api.spawn_prefab` set for this server build. Names change between game updates. Read the file from the server you actually run.

```text
zamert audio
```

Drops cached clips so the next playback reads the files again.

## Unity tools

The server DLL does not contain the editor scripts. Copy the repo's `Tools` folder into the map project:

```text
YourMapProject\Assets\ZAMERT\
```

Use Unity 2021.3.17f1. After the scripts compile, the menu bar has `SchematicManager`. If the menu is missing, the scripts are not under `Assets`, or the Console has compile errors from an editor mismatch.

The next page is the compile order. MER has to export the schematic before ZAMERT can write its JSON.
