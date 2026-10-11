# Doors and prefabs

[Manual](index.md) · [Install](install.md) · [Unity](unity.md) · [Lua](lua.md) · [Components](components.md) · [Server](server.md)

Three different things get called a door:

| You want | Component |
|---|---|
| A real LCZ / HCZ / EZ / bulk / gate door, with keycard and lock | **Map Door** |
| Your own mesh, played by an Animator | **Custom Door** |
| Some other network object (workstation, locker, target, toy, a door prefab by its raw name) | **Prefab Anchor** or `api.spawn_prefab` |

## Map Door

Put Map Door on an empty at the place the door should stand. Rotation and scale of that object are the door's rotation and scale. Compile writes `{Name}-MapDoors.json`.

| Field | |
|---|---|
| `DoorType` | The Unity enum below. Default is Entrance, stored as 5, which spawns an EZ door. |
| `Permissions` | `DoorPermissionFlags`. None means no card. |
| `Require All Permissions` | Every selected flag must be held. |
| `Is Open` | Spawn open. |
| `Is Locked` | Spawn locked with the special-door lock. |

![Map Door values](../assets/doors.svg)

The server applies that table when the schematic spawns. ProjectMER's own enum is smaller (0–4) and does not match these numbers. ZAMERT translates. Do not renumber old `MapDoors.json` files to "fix" them.

From Lua, at runtime, you do not go through this enum:

```lua
api.spawn_door("lcz")
api.spawn_door("hcz")
api.spawn_door("ez")       -- entrance
api.spawn_door("bulk")
api.spawn_door("gate")
api.spawn_door("ez", x, y, z)
```

`api.door_open(1)` and `api.door_lock(1)` affect the nearest real door within 6 metres of the script object, including doors the map already had. Pass a second number to change the range. `0` closes or unlocks.

Map Door spawn uses MER's door setup: the prefab for that type, lock, permissions, then a network spawn at the object's pose. If the console says the door prefab is not ready, ProjectMER did not finish registering prefabs. ZAMERT loads last so this is normally done; a missing MER plugin is the usual cause.

## SCP-079 camera

`Scp079Cameras.json`. `CameraType`: `Lcz` 0, `Hcz` 1, `Ez` 2, `EzArm` 3, `Sz` 4. `Label` is the name on the 079 screen, default `Custom Camera`. These numbers already match MER. The camera is placed on the object's transform.

## Clutter

`Clutter.json`. One of the facility clutter pieces:

| Value | Piece |
|---|---|
| 0 | Simple Boxes |
| 1 | Pipes Short |
| 2 | Boxes Ladder |
| 3 | Tank Supported Shelf |
| 4 | Angled Fences |
| 5 | Huge Orange Pipes |
| 6 | Pipes Long Open |

## Prefab Anchor

Any network prefab, by the name the server uses.

| Field | |
|---|---|
| `Prefab Name` | Exact string. `Work Station`, not `workstation`. |
| `Spawn On Start` | Spawn with the schematic. |
| `Spawn As Child` | Parent under the anchor. |
| `Match Scale` | Copy the anchor's scale. |
| `Disable Anchor Renderers` | Hide the editor mesh you used as a guide. |
| `Destroy Anchor After Spawn` | Remove the anchor object after the prefab exists. |

Names for the build you run:

```text
zamert prefabs
zamert prefabs door
```

The full list is written to `ZAMERT-prefabs.txt` beside the plugin DLL. `api.list_prefabs("work")` returns at most 40 matches into the script.

```lua
api.spawn_prefab("Work Station")
api.spawn_prefab("Work Station", 12, 1.2, 8)
```

`api.spawn_prefab` network-spawns the instance when its net id is still 0. It does not parent the instance to the schematic, so the object stays after the schematic is removed. Prefab Anchor with Spawn As Child does parent it, and then it goes away with the schematic. Map Door and `api.spawn_door` are tracked on the schematic and removed with it. A prefab that expects a room, a locker layout, or door permissions can come up incomplete. Facility doors should go through Map Door or `api.spawn_door`. Workstations, targets, toys, and the clutter pieces are what Prefab Anchor is for.

Game updates rename prefabs. When a name stops working, run `zamert prefabs` again on that server and paste the new spelling into `PrefabName`.
