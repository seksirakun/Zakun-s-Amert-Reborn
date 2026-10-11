# 安装

[说明](index.md) · [Unity](unity.md) · [Lua](lua.md) · [组件](components.md) · [门和预制体](doors.md) · [服务器](server.md)

## 先有这些

- SCP: Secret Laboratory 专用服务器
- LabAPI 1.1.6 或更新
- ProjectMER，作为它自己的插件装好

ZAMERT 的加载优先级是 `Lowest`，所以它在 MER 之后挂上。

Loop Speaker、交互音效和 `AudioModules` 需要服务器上有 AudioPlayerApi。没有的话，这些组件会打错误日志，插件其余部分照常加载。

## 编译

在这个仓库目录：

```text
dotnet build ZAMERT\ZAMERT.csproj -c Release
```

输出在 `ZAMERT\bin\Release\`。

| 文件 | 放到 |
|---|---|
| `ZAMERT.dll` | `%AppData%\SCP Secret Laboratory\LabAPI\plugins\global\` |
| `MoonSharp.Interpreter.dll`、`0Harmony.dll`、`Newtonsoft.Json.dll` | 服务器日志说找不到它们时，放进 `%AppData%\SCP Secret Laboratory\LabAPI\dependencies\` |

服务器按端口拆目录时（`plugins\7777\`），放进那个目录也行。一份就够。

`ZAMERT\libs\ProjectMER.dll` 留在原地。它只是编译引用。正在跑的服务器用的是你自己装的 ProjectMER 插件。

## 第一次启动

先把服务器开一次。LabAPI 会在 `LabAPI\configs\` 里写出 ZAMERT 的配置。具体文件名跟 LabAPI 版本走，对应插件名 `ZAMERT`。

音频目录不存在时会建出来：

`%AppData%\SCP Secret Laboratory\LabAPI\audio\`

音频文件放这里。Loop Speaker 的 `AudioName` 就是这个目录里的文件名。

## 确认加载了

Remote Admin：

```text
zamert
```

回复里有版本。这一局里 schematic 已经生成后，还会有当前活着的 ZAMERT 物体数量。

```text
zamert prefabs
zamert prefabs door
```

在 `ZAMERT.dll` 旁边写出 `ZAMERT-prefabs.txt`，并打印前面几个名字。这就是这一版服务端里合法的 `PrefabName` / `api.spawn_prefab` 名字。游戏更新会改名字。从真正跑这张图的那台服务器上拿这个文件。

```text
zamert audio
```

清掉缓存的音频。下一次播放会重新读文件。

## Unity 工具

服务器 DLL 里没有编辑器脚本。把仓库里的 `Tools` 拷进地图工程：

```text
你的地图工程\Assets\ZAMERT\
```

用 Unity 2021.3.17f1。脚本编译完后，菜单栏会出现 `SchematicManager`。菜单没有，说明脚本不在 `Assets` 下，或者 Console 里有编辑器版本对不上的编译错误。

下一页是编译顺序。MER 先把 schematic 导出，ZAMERT 才能写自己的 JSON。
