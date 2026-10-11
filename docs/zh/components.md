# 组件

[说明](index.md) · [安装](install.md) · [Unity](unity.md) · [Lua](lua.md) · [门和预制体](doors.md) · [服务器](server.md)

菜单：`SchematicManager → Compile All ZAMERT`。每一行是组件、JSON 后缀，以及名为 `Hall` 的 schematic 会写出的文件。

| 组件 | 后缀 | 文件 |
|---|---|---|
| Interactable Object | `Objects` 和 `FObjects` | `Hall-Objects.json` |
| Health Object | `HealthObjects` / `FHealthObjects` | |
| Interactable Pickup | `Pickups` / `FPickups` | |
| Custom Collider | `Colliders` / `FColliders` | |
| Custom Door | `Doors` | schematic 里的动画门，不是设施门 |
| Custom Interactable Toy | `ToyInteractables` / `FToyInteractables` | |
| Loop Speaker | `LoopSpeakers` | |
| Item Spawner | `ItemSpawners` | |
| Damage Trigger | `DamageTriggers` | |
| Player Count Trigger | `PlayerCountTriggers` / `FPlayerCountTriggers` | |
| Prefab Anchor | `PrefabAnchors` | |
| Custom Texture | `CustomTextures` / `FCustomTextures` | |
| Map Door | `MapDoors` | 设施门 |
| Clutter | `Clutter` | |
| SCP-079 Camera | `Scp079Cameras` | |
| Spin Motor | `Rotators` | |
| Move Motor | `Movers` | |
| Light Controller | `Lights` | |
| Timer Relay | `Timers` | |
| Schematic Anchor | `SchematicAnchors` | |
| Counter | `Counters` | |
| Lua Module | `LuaModules` | |
| Function Executor | `Functions` | `Hall-Functions.json`，在根上 |
| Interactable Teleporter | — | 导出根目录里的 `{地图名}-ITeleporters.json` |

**Use Script Value** 打开时才写 `F` 文件。那些字段是 `ScriptValue` 节点（常数、变量、随机、玩家属性），不是裸数字。里面的属性名跟旧地图已经在用的一样。

交互物、血量物体、toy 和人数触发器上有同一套动作列表：

| 列表 | 效果 |
|---|---|
| `MessageModules` | broadcast / hint / cassie 风格的消息 |
| `commandings` | 服务器控制台命令 |
| `dropItems` | 生成物品 |
| `ExplodeModules` | 爆炸 |
| `effectGivingModules` | 状态效果 |
| `AudioModules` | 音频目录里的一次性片段 |
| `GroovieNoiseToCall` | 按 id 或组跑 Groovy Noise。`Groovie` 这个拼写是文件格式。 |
| `FunctionToCall` | 按名字跑 Function Executor 的函数 |
| `AnimationModules` | 播放一个状态。`AnimatorAdress` 是路径，故意这么拼。 |
| `warheadActionType` | 核弹启动 / 停止 / 上锁 / 引爆这一类动作 |
| `PrimitiveModifyModules` | 移动、改色、开关一个 primitive |
| `LoopSpeakerModules` | 播放、停止、音量 |
| `ItemSpawnerModules` | 生成、停止、重置 |
| `PlayerLinkModules` | 把动作绑到按下交互的那个玩家 |

Interactable Object 和 Custom Interactable Toy 上的 Deny 列表在门卡检查失败时跑。`KeycardPermissions` 为空表示不查卡。`RequireAllPermissions` 要求每一个选中的标志都有。

## Interactable Object

`InputKeyCode`，`InteractionMaxRange`（校验时 0 变成 5），`InteractionCooldown`（0 变成 0.2 秒，这个物体上的 ZAMERT 组件共用）。`IgnoredRoles` 跳过这些角色。`ActionType` 选择跑哪一组动作。

检测方式来自配置 `InteractableObjectDetectionMode`：只射线、射线加一个小瞄准锥（`InteractableObjectAssistAngleDegrees`，默认 12），或范围内最近的。部分按键改用 toy 是另一个开关：`EnableIoToys` 加上 `IoToysKeycodes`（默认 0 和 101）。

## Health Object

`Health`，`ArmorEfficient`（0–100），`DeadType`，`DeadActionDelay`，`ResetHPTo`，`DoNotDestroyAfterDeath`。`whitelistWeapons` 限制哪件手持物品能打它。`AllowedScpDamageTypes` 是 SCP 近战白名单；`All` 允许每一种支持的 SCP 攻击。

消息如果用了这些记号：`{attacker_i}`、`{attacker_name}`、`{a_pos}`、`{a_room}`、`{a_zone}`、`{a_role}`、`{s_pos}`、`{s_room}`、`{s_zone}`、`{a_item}`、`{damage}`。

## Item Spawner

`ItemType`（`None` 关闭），`Count`，`LocalPosition`，`AutoStart`，`SpawnDelay`（0 会留 0.5 秒），`RespawnTime`（0 表示物品被拿走后不再生成）。

## Damage Trigger

`DamageAmount`，`DamageInterval`，`MinimumHealth`（0 允许打死），`AutoStart`，`TriggerMode`（进入、停留、离开或混合），`KillInstant`，`AffectHumans`，`AffectScps`。物体上要有玩家能碰到的碰撞体。

## Player Count Trigger

`TriggerThreshold`，`TriggerMode`（达到或掉下去），`Cooldown`，`AutoStart`，再加上那套动作列表。Lua 事件：`ThresholdReached`、`ThresholdDropped`。

## Loop Speaker

`AudioName` 是 `LabAPI\audio` 里的文件。`Volume`，`IsSpatial`，`MinDistance`，`MaxDistance`，`LocalPosition`，`AutoStart`。需要 AudioPlayerApi 的就是这个。

## Custom Door

schematic 里面的动画门，不是 LCZ/HCZ 门。这里的 `DoorType` 是给你的 Animator 用的分类：`LCZ`、`HCZ` 或 `EZ`。`OpenAnimation`、`CloseAnimation`、`LockAnimation` 默认是 `DoorOpen`、`DoorClose`、`DoorLock`。Animator 物体以层级路径写进 `Animator`。真正的设施门用 Map Door。

## Custom Texture

把一张 schematic 的贴图投到面上。`SchematicName`，`Mode`，`StretchX`，`StretchY`，`Faces`，`ZFightOffset`（默认 0.005），`MaxDuplicates`（默认 256），`ManualSize`，`FlipNormal`，`PivotOffset`。脚本版文件是 `FCustomTextures`。

## Spin Motor、Move Motor、Light

| | 字段 |
|---|---|
| Spin Motor | `AngularSpeed`（度/秒，`x`、`y`、`z`，默认 y = 45），`UseWorldSpace` |
| Move Motor | `LocalOffset`，`Duration`（默认 2），`StartDelay`，`PingPong`，`Loop`，`AutoStart` |
| Light Controller | 放在灯上。`ApplyOnStart`，`Enabled`，`OverrideColor`，`Color`，`OverrideIntensity`，`Intensity`（0 = 关），`OverrideRange`，`Range`，`Pulse`，`PulseSpeed`，`PulseMin`，`PulseMax` |

Lua：`api.rotator_speed`、`api.mover_restart`、`api.light_set`。

## Timer 和 Counter

Timer Relay：`Delay`，`Interval`（默认 1），`Repeat`，`RepeatCount`（Repeat 开着且为 0 时一直重复），`AutoStart`，然后一个目标：`FunctionName`，或 `GroovyNoiseId`，或 `GroovyNoiseGroup`。

Counter：`StartValue`，`TargetValue`（默认 1），`Step`（默认 1），`ResetOnReach`，数值碰到目标时用同样的三个目标。Lua 用 `api.counter_add` 加。

## Schematic Anchor

生成另一份已经编译好的 schematic。`SchematicName`，`SpawnOnStart`，`Scale`。深度上限 4。一份 schematic 不会通过自己的 anchor 生成自己。`api.spawn_schematic("名字")` 在调用者的 transform 上生成这个名字，或让同名 anchor 去生成。

## Function Executor

放在 schematic 根上。一个文件，`{名字}-Functions.json`。公开函数从 `FunctionToCall`、Timer/Counter，或 `api.call_function("名字")` 调用。

节点按你堆的顺序：

`If`、`ElseIf`、`Else`、`While`、`For`、`ForEach`、`SetVariable`、`Return`、`Wait`、`CallFunction`、`CallGroovyNoise`、`PlayAnimation`、`SendMessage`、`SendCommand`、`DropItems`、`Explode`、`GiveEffect`、`PlayAudio`、`FWarhead`、`ChangePlayerValue`、`PlayerAction`、`ChangeEntityValue`。

`Wait` 把这个函数剩下的部分停住。上限是 `MaxFunctionWaitSeconds`。碰到 `Wait` 的 `While` / `For` / `ForEach` 会在延迟之后继续，不会空转。嵌套的 `If` 加上 `Wait`，同一段延迟之后父级和子级都会继续。顺序要紧时，把 wait 放在父级。

`F` 组件和函数里的值（`Integer`、`Float`、玩家血量、角色检查、`TrueForAll` 以及其余）是 Script Value 类型。`$type` 名字留在程序集 `AdvancedMERTools` 上，旧文件才能反序列化。

## Interactable Teleporter

`TargetMapName` 填 MER 地图名，`TeleporterIndex` 填那张地图里从 1 开始的传送序号。编译把 `{TargetMapName}-ITeleporters.json` 写到导出根目录。拷进 MER 的 Maps 文件夹。服务器在那张地图加载时读它。`InvokeType` 选择碰撞，或其他传送钩子。动作列表和交互物是同一套。

## Custom Collider、Groovy Noise、toy

Custom Collider 是跟 schematic 一起导出的玩法碰撞体（`Colliders.json`）。有东西碰到时，用和其他模块一样的动作组。

Groovy Noise 是一份可以反复调用的命名动作组。其他组件用 id（`GroovieNoiseId`）或 `Script Group` 调用它。`GroovyNoiseFireOnEnable`（默认开）在有人把它的 `Active` 打开时跑一次。旧的多段门靠这个。

Custom Interactable Toy 是对着 primitive toy 的交互物：`IncludeRootPrimitive`、`IncludeChildPrimitives`、`ToyScaleMultiplier`（默认 1.3），加上同一套动作和拒绝列表。`EnableIoToys` 对你列出的按键换上的就是它。
