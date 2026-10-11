# Unity workflow

[Manual](index.md) · [Install](install.md) · [Lua](lua.md) · [Components](components.md) · [Doors and prefabs](doors.md) · [Server](server.md)

## Order

The schematic root has MER's `Schematic` component. Its GameObject name is the folder name. Rename the root before you compile, not after.

1. Build the mesh in the MER editor.
2. Compile it with ProjectMER. The export directory comes from `SchematicManager.Config.ExportPath`. If that path is missing, ZAMERT falls back to `Desktop\MapEditorReborn_CompiledSchematics`.
3. Add ZAMERT components to children of that schematic.
4. `SchematicManager → Compile All ZAMERT`.

Step 4 writes into the folder from step 2. If the folder does not exist, the Console says `could not find the compiled-schematics directory` and nothing is written.

Individual menus (`Compile Lua Modules`, `Compile Map Doors`, …) write one suffix. `Compile All ZAMERT` writes every suffix. `Decompile All` reads them back onto the hierarchy. Decompile matches `ObjectNamePath` first, then `ObjectId`.

![Files in one schematic folder](../assets/folder.svg)

## What gets written

Plain components (Lua Module, Map Door, Prefab Anchor, motors, lights, timers, counters, schematic anchors, clutter, cameras, item spawners, damage triggers, loop speakers, custom doors) always write `{Name}-{Suffix}.json`.

Components that support **Use Script Value** also have an `F` file: `Objects.json` and `FObjects.json`, `HealthObjects.json` and `FHealthObjects.json`, and the same for pickups, colliders, toys, player-count triggers, custom textures. Turn **Use Script Value** on to export the scripted variant. Leave it off to export the plain list. Old maps that already have both files keep both; the loader reads both.

Function Executor is different. It lives on the schematic root and writes `{Name}-Functions.json`.

Interactable Teleporter writes `{TargetMapName}-ITeleporters.json` in the export root, not inside the schematic folder. `ObjectId` in that file is the teleporter's 1-based index. Copy that file into MER's Maps directory next to the map.

## Identity fields

You do not type `ObjectId` or `ObjectNamePath`. Compile stamps them.

| Field | Meaning |
|---|---|
| `ObjectNamePath` | Hierarchy names from the schematic root down, joined with `/`. A slash inside a name is stored as `\/`. This is the match the server tries first. |
| `ObjectId` | Sibling indexes, leaf first, then parent, separated by spaces. Used when the name path misses. |
| `Code` | Unity instance id at the moment of validate/compile. It changes if you recompile after a domain reload. Use it with `api.find_by_code` inside one build. Do not treat it as a permanent id. |
| `Script Group` | Text you type on the component. Stable. `object.scriptGroup` in Lua, and `api.find_group`. |
| `Active` | Starts enabled. Other components and Lua can turn it off. |

Renaming an object, or reordering siblings, changes the path. Compile again after you do that. The server skips a row it cannot resolve and logs the `ObjectId` / `ObjectNamePath` it failed on.

## Keybinds

`InputKeyCode` is Unity's `KeyCode` number. `E` is 101, `F` is 102, `None` is 0. On Lua Module, 0 means the module has no key.

The server registers one Server-Specific Settings row per distinct key: `ZAMERT - Interactable Object - E`. Players bind it once. The suggested key is the one you picked; they can rebind it. `DisableSSS` in the config turns this registration off, and then no ZAMERT key works.

Pressing E on the keyboard does nothing until that settings row exists and the player has it bound. Spectators do not trigger it.

## Colliders

A key interaction raycasts from the camera. The object, or a child, needs a collider. Trigger colliders count. The first solid hit blocks what is behind it.

Lua Module also has a fallback: if the ray hits nothing, a module of that key within `InteractionMaxRange` (default 5) and within 35 degrees of the look direction can still fire.

`ListenEnter` / `ListenStay` / `ListenExit` need a trigger collider on the same object. If the collider is a trigger and there is no rigidbody, the module adds a kinematic rigidbody so Unity sends trigger messages. `on_stay` runs at most four times a second per collider.

## Script files next to the map

`ScriptPath` is a file name relative to the schematic folder, or an absolute path. `Inline Script` wins when it is not empty. Example: schematic folder contains `buttons.lua`, and the module's Script Path is `buttons.lua`.
