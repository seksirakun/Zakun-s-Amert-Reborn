# 门和预制体

[说明](index.md) · [安装](install.md) · [Unity](unity.md) · [Lua](lua.md) · [组件](components.md) · [服务器](server.md)

叫做门的有三样不同的东西：

| 你要的 | 组件 |
|---|---|
| 真正的 LCZ / HCZ / EZ / bulk / gate 门，带门卡和锁 | **Map Door** |
| 自己的网格，由 Animator 播放 | **Custom Door** |
| 别的网络物体（工作站、柜子、靶子、toy，或按原始名字来的门预制体） | **Prefab Anchor** 或 `api.spawn_prefab` |

## Map Door

在门该站的位置放一个空物体，挂上 Map Door。这个物体的旋转和缩放就是门的旋转和缩放。编译写出 `{名字}-MapDoors.json`。

| 字段 | |
|---|---|
| `DoorType` | 下面的 Unity 枚举。默认 Entrance，存成 5，生成一扇 EZ 门。 |
| `Permissions` | `DoorPermissionFlags`。None 表示不需要卡。 |
| `Require All Permissions` | 选中的每一个标志都要拿着。 |
| `Is Open` | 生成时是开的。 |
| `Is Locked` | 生成时用特殊门锁锁上。 |

![Map Door 的数值](../assets/doors.svg)

schematic 生成时服务器套用这张表。ProjectMER 自己的枚举更短（0–4），和这些数字对不上。ZAMERT 做转换。不要为了“修正”去改旧 `MapDoors.json` 里的数字。

Lua 在运行时不走这个枚举：

```lua
api.spawn_door("lcz")
api.spawn_door("hcz")
api.spawn_door("ez")       -- entrance
api.spawn_door("bulk")
api.spawn_door("gate")
api.spawn_door("ez", x, y, z)
```

`api.door_open(1)` 和 `api.door_lock(1)` 作用于脚本物体 6 米内最近的一扇真门，地图上本来就有的门也算。第二个数字改范围。`0` 关闭或开锁。

Map Door 的生成走 MER 的门设置：那种类型的预制体、锁、权限，然后在物体的位姿上做网络生成。控制台说门预制体还没准备好，就是 ProjectMER 没注册完预制体。ZAMERT 最后加载，正常情况下注册已经完成；常见原因是没装 MER 插件。

## SCP-079 摄像机

`Scp079Cameras.json`。`CameraType`：`Lcz` 0，`Hcz` 1，`Ez` 2，`EzArm` 3，`Sz` 4。`Label` 是 079 屏幕上的名字，默认 `Custom Camera`。这些数字已经和 MER 一致。摄像机放在物体的 transform 上。

## Clutter

`Clutter.json`。设施杂物里的一块：

| 值 | 块 |
|---|---|
| 0 | Simple Boxes |
| 1 | Pipes Short |
| 2 | Boxes Ladder |
| 3 | Tank Supported Shelf |
| 4 | Angled Fences |
| 5 | Huge Orange Pipes |
| 6 | Pipes Long Open |

## Prefab Anchor

任意网络预制体，用服务器上的名字。

| 字段 | |
|---|---|
| `Prefab Name` | 原样字符串。是 `Work Station`，不是 `workstation`。 |
| `Spawn On Start` | 跟 schematic 一起生成。 |
| `Spawn As Child` | 挂到锚点下面。 |
| `Match Scale` | 复制锚点的缩放。 |
| `Disable Anchor Renderers` | 藏起你当参考用的编辑器网格。 |
| `Destroy Anchor After Spawn` | 预制体出现后删掉锚点物体。 |

你正在跑的这一版的名字：

```text
zamert prefabs
zamert prefabs door
```

完整列表写到插件 DLL 旁边的 `ZAMERT-prefabs.txt`。`api.list_prefabs("work")` 最多把 40 个匹配送进脚本。

```lua
api.spawn_prefab("Work Station")
api.spawn_prefab("Work Station", 12, 1.2, 8)
```

`api.spawn_prefab` 在 net id 仍为 0 时做网络生成。它不把实例挂到 schematic 上，所以 schematic 删掉之后物体还在。Prefab Anchor 打开 Spawn As Child 才会挂上，然后跟 schematic 一起消失。Map Door 和 `api.spawn_door` 记在 schematic 上，一起被删掉。指望房间、柜子布局或门权限的预制体可能生成不完整。设施门走 Map Door 或 `api.spawn_door`。工作站、靶子、toy 和 clutter 块才是 Prefab Anchor 的用途。

游戏更新会改预制体名字。某个名字不再生效时，在那台服务器上再跑一次 `zamert prefabs`，把新拼写贴进 `PrefabName`。
