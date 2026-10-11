# 服务器

[说明](index.md) · [安装](install.md) · [Unity](unity.md) · [Lua](lua.md) · [组件](components.md) · [门和预制体](doors.md)

配置在第一次启动时生成。下面是 2.1.0 的默认值。

## Remote Admin

| 命令 | |
|---|---|
| `zamert` 或 `zm` | 版本、schematic 数量、本局变量、按组件统计的存活物体。 |
| `zamert prefabs` | 刷新网络预制体列表，在 DLL 旁边写 `ZAMERT-prefabs.txt`，打印前 12 个名字。 |
| `zamert prefabs door` | 同上，带过滤。过滤是子串。 |
| `zamert audio` | 忘掉缓存的音频片段。 |

预制体文件就是用来抄名字的那份。每次执行命令都会按执行它的那台服务器重写。

## 配置

| 键 | 默认 | |
|---|---|---|
| `Debug` | false | 额外日志。 |
| `AudioFolderPath` | `LabAPI/audio` | 音频目录。没有就创建。 |
| `AudioMasterVolume` | 1 | 全服倍率。0 把 ZAMERT 的声音静音。 |
| `EnablePerClientAudioVolume` | true | 在 Server-Specific Settings 里加 `ZAMERT - Audio Volume` 滑条。只影响一次性声音。 |
| `EnableIoToys` | false | 按键在 `IoToysKeycodes` 里的交互物以 toy 生成。 |
| `IoToysKeycodes` | 0, 101 | None 和 E。 |
| `IoToysNoRoot` | false | 根上的交互物不用 toy。 |
| `IoToysDebug` | false | 在 toy 交互物的位置放一个看得见的 primitive。 |
| `IoToysBothModes` | false | toy 和设置里的按键一起用。200 毫秒，避免一次按下跑两遍。 |
| `InteractableObjectDetectionMode` | RaycastWithAssist | `RaycastOnly`、`RaycastWithAssist`、`ProximityOnly`。 |
| `InteractableObjectAssistSphereRadius` | 0.35 | 辅助探测的大小。 |
| `InteractableObjectAssistAngleDegrees` | 12 | 辅助用的瞄准锥。 |
| `LuaEnabled` | true | Lua Module 和 Lua Events 的总开关。 |
| `LuaCacheFileScripts` | true | 缓存 `ScriptPath` 文件，写入时间变了就重新加载。 |
| `LuaAllowServerCommands` | true | `api.command`。 |
| `LuaAllowDelayedCallbacks` | true | `api.delay` 和 `api.repeat_every`。 |
| `LuaMaxRepeatCount` | 100 | `api.repeat_every` 的上限。 |
| `LuaLogApiErrors` | true | 失败的 `api.*` 调用写日志，而不是安静地返回 nil。 |
| `AutoRun` | false | 挂上 MER 的门生成。不了解这个钩子就保持关闭。 |
| `GroovyNoiseFireOnEnable` | true | Groovy Noise 被启用时跑一次。 |
| `DisableSSS` | false | 不登记 ZAMERT 按键和音量滑条。 |
| `CustomSpawnPointEnable` | true | 自定义出生点。 |
| `MaxFunctionWaitSeconds` | 600 | Function Executor 的 `Wait` 和 `api.wait` 的上限。0 表示不封顶。 |
| `LogSchematicSummary` | true | 每个 schematic 一行：加载了多少行，跳过了多少行。 |
| `LuaAutoYieldInstructions` | 2500 | Lua Module 让出一帧之前能跑的指令数。 |
| `LuaMaxBusySlices` | 8 | 没有 `api.wait` 时，几片之后停掉脚本。 |
| `LuaMaxProgramsPerFrame` | 8 | 一帧里继续执行的模块协程数。 |
| `Gates` | 一条示例 | 按地图名写的 dummy gate。 |

`Gates` 带一个占位地图名。换掉或留着都行；不认识的地图名什么都不做。

## 加载器做什么

schematic 生成时，ZAMERT 读取它认识的每一份旁路文件。每一行按这个顺序找 transform：`ObjectNamePath`，完整 `ObjectId`，旧的更短的序号行走，两个字段都空则用 schematic 根。其余跳过并计数。`LogSchematicSummary` 打印合计。

本局变量（`api.set_round_var`）在下一张地图开始生成时清掉，不是在 schematic 生成之后。按键在地图生成完之后重新登记。被销毁的物体从登记表里拿掉；登记表不会因为地图生成结束就被清空。以前在那里清空，会把同一次事件里刚生成的 schematic 删掉。

## 音频

文件在 `AudioFolderPath`。换过文件之后用 `zamert audio` 清片段缓存。每个玩家的音量是设置里的滑条，再乘 `AudioMasterVolume`。滑条不影响循环喇叭，只影响一次性声音。

## 脚本什么时候被停

从不调用 `api.wait` 的 Lua Module 会把指令额度用完然后停下。日志行是 `Lua on {物体} was stopped because it ran too long without api.wait()`。在循环里放 `api.wait`。这个模块上已经有 8 个协程时，会出现 `already has 8 running scripts`。

语法错误或运行错误是 `Lua module failed to start` 或 `Lua failed on {物体}`，后面带着 Lua 的消息。`LuaLogApiErrors` 覆盖 `api.give_item` 和其他助手里的错误（不认识的物品名、nil 玩家），不会把整份脚本杀掉。

## 旧地图

后缀、`$type` 的程序集名 `AdvancedMERTools`，以及历史上拼错的字段都还在。2.1.0 之前做的地图加载时不用重新编译。要新组件是另一回事：编译那些后缀，把新文件放在旧文件旁边。
