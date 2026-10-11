# Lua

[说明](index.md) · [安装](install.md) · [Unity](unity.md) · [组件](components.md) · [门和预制体](doors.md) · [服务器](server.md)

Lua Module 是一个组件。脚本跑在你放它的那个物体上。十个物体共用一个文件，也是每个物体各跑一份。用 `object.name` 区分。

![on_interact 属于哪个物体](../assets/lua.svg)

## Inspector

| 字段 | 填什么 |
|---|---|
| `Inline Script` | 源码。非空就用它。 |
| `Script Path` | Inline Script 为空时，用 schematic 旁边的 `.lua` 文件。 |
| `Input Key Code` | Unity KeyCode。`0` = 没有键。`101` = E。 |
| `Interaction Max Range` | 米。`0` 按 5 算。 |
| `Listen Enter / Stay / Exit` | 玩家碰到这个物体上的 trigger 时调用 `on_enter`、`on_stay`、`on_exit`。 |
| `Tick Interval` | `on_tick` 的间隔秒数。低于 `0.1` 则 tick 关闭。 |
| `Script Group` | 你写的标签。读作 `object.scriptGroup`。 |

用 `SchematicManager → Compile Lua Modules` 或 Compile All。文件是 `{名字}-LuaModules.json`。

## 什么时候跑

schematic 生成时，源码跑一遍。这一遍定义函数，也执行顶层那些不在函数里的代码。

| 函数 | 何时 |
|---|---|
| 顶层代码 | schematic 生成 |
| `on_interact` | 玩家看着这个模块并按下它的键 |
| `on_enter` | 玩家进入 trigger |
| `on_stay` | 玩家留在里面，最多每 0.25 秒一次 |
| `on_exit` | 玩家离开 trigger |
| `on_tick` | Tick Interval 至少 0.1 时，按这个间隔 |

函数必须是全局的。`local function on_interact` 事件那边看不见。

```lua
function on_interact()
  api.hint(player.name .. " used " .. object.name, 2)
end
```

会 `api.wait` 的循环，函数要写在循环上面。解释器还没走到那一行，函数就不存在。第一轮就 `api.wait` 的循环，到不了写在下面的函数。

生成时和 `on_tick` 里，如果这次调用没有玩家，`player` 是 nil。在 `on_interact`、`on_enter`、`on_stay`、`on_exit` 里读它。

## `object` 和 `player`

`object`（这个模块）：

| 字段 | |
|---|---|
| `name` | 层级里的名字。 |
| `scriptGroup` | 你填的 Script Group。 |
| `code` | 编译时的 instance id。再次编译会变。 |
| `objectId` | 序号路径。 |
| `type` | `"LuaModule"`。 |
| `active` | Active 标志。 |
| `position` | 建表时的位置。`x`、`y`、`z`。 |

`player`：

| 字段 | |
|---|---|
| `id`、`name`、`userId` | 玩家 id、昵称、user id。 |
| `role`、`team` | 角色名和队伍名。用 `== "ClassD"`、`== "ChaosInsurgency"` 比较。 |
| `isScp`、`isHuman`、`isAlive` | 布尔值。 |
| `health` | 当前血量。 |
| `position` | `x`、`y`、`z`。 |
| `room`、`zone` | 名字。 |
| `currentItem` | 物品类型名，或空字符串。 |

`schematic.name` 是 MER schematic 的名字。`schematic.directoryPath` 是加载 JSON 的文件夹。

同一份脚本，两颗按钮：

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

`api.wait(秒)` 暂停这份脚本。服务器继续跑。`api.wait(0)` 等一帧。

```lua
for i = 1, 5 do
  api.broadcast("Count " .. i, 2)
  api.wait(1)
end
```

没有 `api.wait` 的 `while true` 会被停掉。默认：每片 2500 条 Lua 指令，8 片，然后脚本被杀掉，控制台写它跑得太久。配置项：`LuaAutoYieldInstructions`、`LuaMaxBusySlices`。一个模块同时最多 8 个协程。一帧最多继续 `LuaMaxProgramsPerFrame` 份脚本（默认 8）。`api.wait` 受 `MaxFunctionWaitSeconds` 限制（默认 600）。

`api.wait` 能用，是因为 Lua Module 的源码以协程方式跑。其他组件上的 `LuaEvents` 短脚本不是协程。那些地方用 `api.delay`。

## 玩家

```lua
api.broadcast("Hello", 5)                 -- 文本、秒数、所有人
api.hint("Look up", 2)                     -- 当前玩家
api.hint_player(player, "Look up", 2)      -- 玩家表、id 或昵称
api.heal_player(player, 25)                -- 或 api.heal_player(25)
api.damage_player(player, 10, "fence")
api.kill_player(player, "pit")
api.teleport_player(player, 10, 2, 4)      -- 或 {x=,y=,z=} 表
api.give_item(player, "KeycardGuard")      -- ItemType 名字
api.set_role(player, "ClassD", 0)          -- 第三个参数为 1 时还会送到出生点
api.players()                              -- 数组，Lua 下标从 1
api.alive_players()
api.humans()
api.scps()
api.player_count("ClassD")                 -- 角色或队伍的子串，空则所有人
api.find_player("Nick")                    -- id、昵称、user id 或玩家表
api.current_player()
```

`give_item` 只给一个参数时，把物品给当前玩家。`set_role` 用角色枚举名（`ClassD`、`Scientist`、`NtfSergeant`、`Scp173` 等）。名字不对就返回 false，不改玩家。

## 门和预制体

```lua
api.spawn_door("ez")              -- 在这个物体的位置。还有 lcz、hcz、bulk、gate
api.spawn_door("gate", 4, 1, 8)   -- 世界坐标
api.door_open(1)                  -- 6 米内最近的门。0 关闭
api.door_open(1, 12)              -- 同上，12 米
api.door_lock(1)
api.spawn_prefab("Work Station")
api.spawn_prefab("Work Station", 10, 2, 4)
api.spawn_prefab("Work Station", {x = 10, y = 2, z = 4})
api.spawn_prefab("Work Station", 10, 2, 4, {x = 0, y = 90, z = 0}, {x = 1, y = 1, z = 1})
api.list_prefabs("door")          -- 最多 40 个名字
api.prefab_count()
```

`spawn_door` 用游戏的门预制体（可破坏门、bulk、gate），并跟 schematic 一起销毁。`spawn_prefab` 按这台服务器 `NetworkClient.prefabs` 里的名字生成。位置是三个数，或一张 `{x,y,z}` 表。旋转和缩放是第五、第六个参数，各自一张表：名字、x、y、z、旋转、缩放。拼写从 `zamert prefabs` 拿。之后还要开关和上锁的门，用 `spawn_door`，不要用原始预制体。

## 从 Lua 调其他模块

目标是 code、物体名，或者调用允许省略时表示“这个物体”。

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
api.call_function("OpenHall")          -- 这个 schematic 上的 Function Executor
api.trigger(object.code, "interacted") -- 那个物体上的 LuaEvents
api.trigger_group("doors", "custom")

api.counter_add("button", 1)           -- 按 code 或名字找 Counter
api.timer_start("clock")
api.timer_stop("clock")
api.light_set("lamp", 2)               -- 强度，0 关掉
api.rotator_speed("fan", 0, 90, 0)
api.mover_restart("platform")
api.spawn_schematic("SubRoom")         -- 按名字找 Schematic Anchor，或在这里生成这个名字

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

## 变量

三个范围。键是字符串。值是数字、字符串或布尔。

```lua
api.set_self_var("uses", 0)             -- 这个物体，直到它被销毁
api.inc_self_var("uses", 1)
api.get_self_var("uses")

api.set_schematic_var("power", 1)       -- 这一份 schematic 实例
api.get_schematic_var("power")
api.inc_schematic_var("power", -1)

api.set_round_var("opened", true)       -- 下一张地图开始生成时清掉
api.get_round_var("opened")
```

## 时间、概率、文件、命令

```lua
api.log("opened")
api.warn("missing clip")
api.error("bad state")
api.time()
api.realtime()
api.chance(0.25)                        -- 25% 的调用返回 true
api.random_int(1, 6)
api.random_float(0, 1)
api.vec(1, 2, 3)                        -- {x,y,z}
api.distance(a, b)
api.add_vec(a, b)

api.command("cassie 1 Attention")       -- 需要 LuaAllowServerCommands
api.include("lib.lua")                  -- 在这份脚本里执行
api.run_file("once.lua")                -- 另一份脚本，不共享 local

api.delay(2, function()
  api.broadcast("later", 3)
end)
api.repeat_every(1, 5, function(ctx, tick)
  api.broadcast("tick " .. tick, 1)
end)
```

`repeat_every` 至少等 0.05 秒。次数被 `LuaMaxRepeatCount` 卡住（默认 100）。`LuaAllowDelayedCallbacks` 为 false 时，`delay` 和 `repeat_every` 什么都不做。

`include` 和 `run_file` 先在 schematic 文件夹里找，再到插件 DLL 旁边找。传入绝对路径就用那个路径。

## 其他组件上的 Lua Events

每个 ZAMERT 组件都有 `Lua Events` 列表。这是旧的钩子：挂在 Interact、Death、Enter 上的短脚本。它不是 Lua Module。那里的 `api.wait` 会报错；用 `api.delay`。

| `Event Type` | 何时 |
|---|---|
| `Spawned`、`Destroyed` | 物体的生命周期 |
| `Interacted`、`Denied` | 按键通过，或门卡被拒 |
| `Searching`、`Picked` | 拾取物 |
| `Damaged`、`Died` | 血量物体 |
| `Entered`、`Stayed`、`Exited` | 碰撞体 |
| `ThresholdReached`、`ThresholdDropped` | 人数阈值 |
| `Tick` | 组件会 tick 的时候 |
| `Custom` | `Custom Event Name`，也可以 `api.trigger` |

每一行有 priority、chance（`0`–`1`）、cooldown、只跑一次、取消时也跑、成功后停止。内联或文件，路径规则相同。

事件脚本里的全局量一样（`player`、`object`、`api`）。`ctx.cancelled`、`ctx.damage`、`ctx.currentHealth` 看事件。`api.cancel()` / `api.uncancel()` 标记上下文。`api.stop()` 停掉这个事件后面的绑定。拾取事件可以 `api.remove_pickup()` 或 `api.keep_pickup()`。

`LuaEnabled` 为 false 时，Lua Module 和这些绑定一起关掉。
