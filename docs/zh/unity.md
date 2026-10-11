# Unity

[说明](index.md) · [安装](install.md) · [Lua](lua.md) · [组件](components.md) · [门和预制体](doors.md) · [服务器](server.md)

## 顺序

Schematic 根物体上有 MER 的 `Schematic` 组件。GameObject 的名字就是文件夹名。编译前改根物体的名字。

1. 在 MER 编辑器里摆网格。
2. 用 ProjectMER 编译。导出目录来自 `SchematicManager.Config.ExportPath`。这条路径不存在时，ZAMERT 退回到 `桌面\MapEditorReborn_CompiledSchematics`。
3. 在这个 schematic 的子物体上加 ZAMERT 组件。
4. `SchematicManager → Compile All ZAMERT`。

第 4 步写进第 2 步的文件夹。文件夹不存在时，Console 会写 `could not find the compiled-schematics directory`，一个文件都不会出。

单独的菜单（`Compile Lua Modules`、`Compile Map Doors` 等）只写一个后缀。`Compile All ZAMERT` 全部都写。`Decompile All` 把它们读回层级。先对 `ObjectNamePath`，再对 `ObjectId`。

![一个 schematic 文件夹里的文件](../assets/folder.svg)

## 写出什么

普通组件（Lua Module、Map Door、Prefab Anchor、马达、灯、计时器、计数器、schematic anchor、clutter、摄像机、item spawner、damage trigger、loop speaker、custom door）一律写 `{名字}-{后缀}.json`。

带 **Use Script Value** 的组件还有一份 `F` 文件：`Objects.json` 和 `FObjects.json`，`HealthObjects.json` 和 `FHealthObjects.json`。pickup、collider、toy、player-count trigger、custom texture 同样如此。要导出脚本值就打开 **Use Script Value**。关掉就写普通列表。旧地图里两份都有的，两份都留着；加载器两份都读。

Function Executor 不一样。它在 schematic 根上，写 `{名字}-Functions.json`。

Interactable Teleporter 写在导出根目录，不在 schematic 文件夹里：`{TargetMapName}-ITeleporters.json`。文件里的 `ObjectId` 是传送点的序号，从 1 开始。把这个文件拷到 MER 的 Maps 目录，跟地图放在一起。

## 身份字段

`ObjectId` 和 `ObjectNamePath` 不用手填。编译时盖上去。

| 字段 | 含义 |
|---|---|
| `ObjectNamePath` | 从 schematic 根往下的物体名，用 `/` 接起来。名字里的斜杠存成 `\/`。服务器先试这个。 |
| `ObjectId` | 兄弟序号。先是物体自己的序号，再是父级，用空格分开。名字路径对不上时用这个。 |
| `Code` | 校验/编译那一刻的 Unity instance id。域重载后再编译会变。同一份编译结果里可以拿来 `api.find_by_code`。它不是永久 id。 |
| `Script Group` | 你在组件上填的字。稳定。Lua 里是 `object.scriptGroup`，还有 `api.find_group`。 |
| `Active` | 开始时是开的。别的组件和 Lua 可以关掉。 |

改名，或者调整兄弟顺序，路径就变了。改完再编译一次。服务器解不开的行会跳过，并把对不上的 `ObjectId` / `ObjectNamePath` 打进日志。

## 按键

`InputKeyCode` 是 Unity `KeyCode` 的数字。`E` 是 101，`F` 是 102，`None` 是 0。Lua Module 上 0 表示这个模块没有按键。

服务器给每个不同的键在 Server-Specific Settings 里登记一行：`ZAMERT - Interactable Object - E`。玩家绑定一次。建议键是你选的那个；玩家可以改绑。配置里的 `DisableSSS` 关掉这次登记，之后所有 ZAMERT 按键都不工作。

设置里没有这一行、玩家也没绑的时候，键盘上按 E 没有效果。观察者不会触发。

## 碰撞体

按键交互从摄像机打射线。物体自己或子物体上要有碰撞体。Trigger 也算。第一下实心命中会挡住后面的东西。

Lua Module 还有一条退路：射线什么都没打到时，这个键对应的模块如果在 `InteractionMaxRange` 里（默认 5）并且在视线 35 度以内，仍然可以触发。

`ListenEnter` / `ListenStay` / `ListenExit` 需要同一个物体上的 trigger 碰撞体。碰撞体是 trigger 且没有刚体时，模块会加一个 kinematic 刚体，Unity 才会发 trigger 消息。`on_stay` 对同一个碰撞体每秒最多四次。

## 放在地图旁边的脚本文件

`ScriptPath` 是相对 schematic 文件夹的文件名，或绝对路径。`Inline Script` 非空时用它。例如 schematic 文件夹里有 `buttons.lua`，模块的 Script Path 就填 `buttons.lua`。
