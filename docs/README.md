# ZAMERT wiki

Usage manual for 2.1.0. The pages are the same in five languages. Code samples are identical, so a script copied from one language runs as written.

| | |
|---|---|
| Türkçe | [Kurulum](tr/install.md) · [Unity](tr/unity.md) · [Lua](tr/lua.md) · [Bileşenler](tr/components.md) · [Kapılar](tr/doors.md) · [Sunucu](tr/server.md) |
| English | [Install](en/install.md) · [Unity](en/unity.md) · [Lua](en/lua.md) · [Components](en/components.md) · [Doors](en/doors.md) · [Server](en/server.md) |
| Русский | [Установка](ru/install.md) · [Unity](ru/unity.md) · [Lua](ru/lua.md) · [Компоненты](ru/components.md) · [Двери](ru/doors.md) · [Сервер](ru/server.md) |
| 中文 | [安装](zh/install.md) · [Unity](zh/unity.md) · [Lua](zh/lua.md) · [组件](zh/components.md) · [门](zh/doors.md) · [服务器](zh/server.md) |
| 한국어 | [설치](ko/install.md) · [Unity](ko/unity.md) · [Lua](ko/lua.md) · [컴포넌트](ko/components.md) · [문](ko/doors.md) · [서버](ko/server.md) |

![Compile path](assets/pipeline.svg)

MER writes `{Name}.json`. ZAMERT writes the siblings in that same folder: `{Name}-LuaModules.json`, `{Name}-MapDoors.json`, `{Name}-Objects.json`, and the rest. The server loads them when the schematic spawns.

![Folder layout](assets/folder.svg)

`object.name` is the hierarchy name of the Lua Module that was hit. `player` is who pressed the key. The key is the Server-Specific Settings row `ZAMERT - Interactable Object`, not the raw keyboard key.

![Which object on_interact runs on](assets/lua.svg)

Map Door stores the Unity enum (Entrance is 5). The server turns that into an EZ door. Old `MapDoors.json` files keep their numbers.

![Door values](assets/doors.svg)
