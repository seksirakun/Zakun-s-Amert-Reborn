# ZAMERT manual

Version 2.1.0. This is the usage manual for map makers. Field names below are the names written into the JSON.

[Install](install.md) · [Unity](unity.md) · [Lua](lua.md) · [Components](components.md) · [Doors and prefabs](doors.md) · [Server](server.md)

[Türkçe](../tr/index.md) · [English](index.md) · [Русский](../ru/index.md) · [中文](../zh/index.md) · [한국어](../ko/index.md)

ZAMERT adds behaviour to a ProjectMER schematic: keybinds, game doors, network prefabs, health objects, speakers, timers, and Lua. The mesh stays in MER's `{Name}.json`. Behaviour is a set of sibling files next to it.

![Compile path](../assets/pipeline.svg)

Two ways to write logic:

- **Lua Module** on an empty or on a mesh. Real Lua: `if`, `for`, `while`, `api.wait`.
- **Function Executor** on the schematic root. A node list (`If`, `While`, `Wait`, `SendMessage`, …) saved as `{Name}-Functions.json`. Old maps use this. It still runs.

Interactable Object, Health Object and the other original components still compile to the same files they always did. A new component adds a new suffix. It does not change an old file.

Start with [Install](install.md) if the plugin is not on the server yet, or [Lua](lua.md) if the server is already running and you only need scripts.
