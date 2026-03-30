# Zakun's AMERT Reborn (ZAMERT)

A reborn and improved version of **AdvancedMERTools** for SCP: Secret Laboratory servers.

## What is this?

ZAMERT is a LabAPI plugin that extends **ProjectMER** schematics with advanced scripting capabilities: health objects, interactable objects, colliders, teleporters, custom doors, audio playback, animations, and more.

## Features

- **HealthObjects** -> Destructible schematic objects with custom HP, damage types and death actions
- **InteractableObjects** -> Keybind-triggered objects with full scripting support
- **InteractablePickups** -> Pickups that run scripts on search or pickup
- **CustomColliders** -> Trigger zones that fire on player enter/stay/exit
- **InteractableTeleporters** -> Teleporters embedded directly in schematics
- **GroovyNoise** -> Macro system to enable/disable other objects, now fully delay-free
- **Custom Doors / Dummy Doors / Dummy Gates** -> Animated and permission-based doors inside schematics
- **Audio Playback** -> Spatial and non-spatial audio via AudioPlayer
- **FunctionExecutor** -> Full scripting engine (if/else/while/for, variables, return values, and more)
- **Offset Logging** -> Logs SL version, ProjectMER version and key method offsets on startup for easy diagnostics

## Requirements

- SCP: Secret Laboratory (latest)
- LabAPI `>= 1.1.6`
- ProjectMER (latest)

## Installation

1. Build the project in Visual Studio 2022 targeting **.NET 4.8 / C# 7**
2. Drop the compiled `.dll` into your server's `LabAPI/plugins/global` folder
3. Restart the server config is auto-generated

## Configuration

Config file is generated at `LabAPI/configs/<port>/ZAMERT.yml` on first run.

| Key | Default | Description |
|---|---|---|
| `debug` | `false` | Enables debug console output and ZMapper offset tracing |
| `audio_folder_path` | `LabAPI/audio` | Folder for audio files |
| `enable_io_toys` | `false` | Spawns InteractableToys for IO objects |
| `custom_spawn_point_enable` | `false` | Enables schematic-defined player spawnpoints |

## Credits

- Original **AdvancedMERTools** concept by **Mujishung**
- Reborn & rewritten by **seksirakun48 (zakun)**
