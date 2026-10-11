# ZAMERT 使用说明

版本 2.1.0。给做地图的人看。下面的字段名就是写进 JSON 的名字。

[安装](install.md) · [Unity](unity.md) · [Lua](lua.md) · [组件](components.md) · [门和预制体](doors.md) · [服务器](server.md)

[Türkçe](../tr/index.md) · [English](../en/index.md) · [Русский](../ru/index.md) · [中文](index.md) · [한국어](../ko/index.md)

ZAMERT 给 ProjectMER 的 schematic 加行为：按键、设施门、网络预制体、血量物体、喇叭、计时器和 Lua。网格留在 MER 的 `{名字}.json` 里。行为写在旁边的文件里。

![编译路径](../assets/pipeline.svg)

逻辑有两种写法：

- **Lua Module**，放在空物体或网格上。就是 Lua：`if`、`for`、`while`、`api.wait`。
- **Function Executor**，放在 schematic 根上。节点有 `If`、`While`、`Wait`、`SendMessage` 等，文件是 `{名字}-Functions.json`。旧地图用的就是这个，现在仍然会跑。

Interactable Object、Health Object 和其他原有组件仍然编译成原来的文件。新组件只多一个后缀，不改旧文件的内容。

插件还没上服务器，从[安装](install.md)看起。服务器已经在跑、只写脚本，直接看 [Lua](lua.md)。
