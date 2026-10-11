# Lua

[설명](index.md) · [설치](install.md) · [Unity](unity.md) · [컴포넌트](components.md) · [문과 프리팹](doors.md) · [서버](server.md)

Lua Module은 컴포넌트입니다. 스크립트는 붙여 둔 오브젝트에서 돕니다. 파일 하나를 열 오브젝트가 나눠 써도 실행은 오브젝트마다 따로입니다. 구분은 `object.name`이 합니다.

![on_interact가 어느 오브젝트인지](../assets/lua.svg)

## 인스펙터

| 필드 | 넣는 값 |
|---|---|
| `Inline Script` | 소스. 비어 있지 않으면 이것을 씁니다. |
| `Script Path` | Inline Script가 비어 있을 때, schematic 옆의 `.lua` 파일. |
| `Input Key Code` | Unity KeyCode. `0` = 키 없음. `101` = E. |
| `Interaction Max Range` | 미터. `0`은 5로 칩니다. |
| `Listen Enter / Stay / Exit` | 이 오브젝트의 트리거에 닿은 플레이어에 대해 `on_enter`, `on_stay`, `on_exit`. |
| `Tick Interval` | `on_tick` 사이 초. `0.1` 미만이면 tick이 꺼집니다. |
| `Script Group` | 직접 적은 라벨. `object.scriptGroup`으로 읽습니다. |

`SchematicManager → Compile Lua Modules` 또는 Compile All. 파일은 `{이름}-LuaModules.json`입니다.

## 언제 실행되는가

소스는 schematic이 생길 때 한 번 돕니다. 그 실행이 함수를 정의하고, 함수 밖에 있는 코드도 실행합니다.

| 함수 | 언제 |
|---|---|
| 최상위 코드 | schematic 생성 |
| `on_interact` | 플레이어가 이 모듈을 보고 그 키를 누를 때 |
| `on_enter` | 플레이어가 트리거에 들어갈 때 |
| `on_stay` | 안에 있는 동안, 최대 0.25초마다 |
| `on_exit` | 플레이어가 트리거에서 나갈 때 |
| `on_tick` | Tick Interval이 0.1 이상일 때, 그 간격마다 |

함수는 전역이어야 합니다. `local function on_interact`는 이벤트 쪽에서 보이지 않습니다.

```lua
function on_interact()
  api.hint(player.name .. " used " .. object.name, 2)
end
```

기다리는 루프보다 위에 함수를 둡니다. 인터프리터가 그 줄에 닿기 전에는 함수가 없습니다. 첫 바퀴에 `api.wait`를 만나는 루프는 아래에 쓴 함수에 도달하지 않습니다.

생성 때와 `on_tick` 안에서는, 호출에 플레이어가 없으면 `player`가 nil입니다. `on_interact`, `on_enter`, `on_stay`, `on_exit` 안에서 읽습니다.

## `object`와 `player`

`object` (이 모듈):

| 필드 | |
|---|---|
| `name` | 계층 이름. |
| `scriptGroup` | 적어 둔 Script Group. |
| `code` | 컴파일 때의 instance id. 다시 컴파일하면 바뀝니다. |
| `objectId` | 인덱스 경로. |
| `type` | `"LuaModule"`. |
| `active` | Active 플래그. |
| `position` | 테이블을 만들 때의 위치. `x`, `y`, `z`. |

`player`:

| 필드 | |
|---|---|
| `id`, `name`, `userId` | 플레이어 id, 별명, user id. |
| `role`, `team` | 역할 이름과 팀 이름. `== "ClassD"`, `== "ChaosInsurgency"`로 비교합니다. |
| `isScp`, `isHuman`, `isAlive` | 불리언. |
| `health` | 현재 체력. |
| `position` | `x`, `y`, `z`. |
| `room`, `zone` | 이름. |
| `currentItem` | 아이템 타입 이름, 또는 빈 문자열. |

`schematic.name`은 MER schematic 이름입니다. `schematic.directoryPath`는 JSON을 읽은 폴더입니다.

스크립트 하나, 버튼 둘:

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

`api.wait(초)`는 이 스크립트를 멈춥니다. 서버는 계속 돕니다. `api.wait(0)`은 한 프레임을 기다립니다.

```lua
for i = 1, 5 do
  api.broadcast("Count " .. i, 2)
  api.wait(1)
end
```

`api.wait`가 없는 `while true`는 중단됩니다. 기본값: 조각당 Lua 명령 2500개, 8조각, 그 다음 스크립트가 죽고 콘솔에 너무 오래 돌았다고 나옵니다. 설정: `LuaAutoYieldInstructions`, `LuaMaxBusySlices`. 모듈 하나에서 코루틴은 동시에 8개까지입니다. 한 프레임에 이어 가는 스크립트는 최대 `LuaMaxProgramsPerFrame`(기본 8)입니다. `api.wait`는 `MaxFunctionWaitSeconds`로 잘립니다(기본 600).

`api.wait`는 Lua Module 소스가 코루틴으로 돌기 때문에 됩니다. 다른 컴포넌트의 `LuaEvents` 조각은 코루틴이 아닙니다. 거기서는 `api.delay`를 씁니다.

## 플레이어

```lua
api.broadcast("Hello", 5)                 -- 글, 초, 모두
api.hint("Look up", 2)                     -- 현재 플레이어
api.hint_player(player, "Look up", 2)      -- 플레이어 테이블, id, 별명
api.heal_player(player, 25)                -- 또는 api.heal_player(25)
api.damage_player(player, 10, "fence")
api.kill_player(player, "pit")
api.teleport_player(player, 10, 2, 4)      -- 또는 {x=,y=,z=} 테이블
api.give_item(player, "KeycardGuard")      -- ItemType 이름
api.set_role(player, "ClassD", 0)          -- 세 번째 인자가 1이면 스폰 지점으로도 보냅니다
api.players()                              -- 배열, Lua 인덱스 1
api.alive_players()
api.humans()
api.scps()
api.player_count("ClassD")                 -- 역할이나 팀의 일부, 비우면 모두
api.find_player("Nick")                    -- id, 별명, user id, 플레이어 테이블
api.current_player()
```

`give_item`에 인자가 하나면 현재 플레이어에게 줍니다. `set_role`은 역할 enum 이름을 씁니다(`ClassD`, `Scientist`, `NtfSergeant`, `Scp173`, …). 틀린 이름은 false를 돌려주고 플레이어를 바꾸지 않습니다.

## 문과 프리팹

```lua
api.spawn_door("ez")              -- 이 오브젝트 위치. lcz, hcz, bulk, gate도 됩니다
api.spawn_door("gate", 4, 1, 8)   -- 월드 좌표
api.door_open(1)                  -- 6m 안의 가장 가까운 문. 0은 닫기
api.door_open(1, 12)              -- 같음, 12m
api.door_lock(1)
api.spawn_prefab("Work Station")
api.spawn_prefab("Work Station", 10, 2, 4)
api.spawn_prefab("Work Station", {x = 10, y = 2, z = 4})
api.spawn_prefab("Work Station", 10, 2, 4, {x = 0, y = 90, z = 0}, {x = 1, y = 1, z = 1})
api.list_prefabs("door")          -- 최대 40개 이름
api.prefab_count()
```

`spawn_door`는 게임 문 프리팹(부서지는 문, bulk, gate)을 쓰고 schematic과 함께 사라집니다. `spawn_prefab`은 이 서버의 `NetworkClient.prefabs` 이름으로 만듭니다. 위치는 숫자 세 개이거나 `{x,y,z}` 테이블 하나입니다. 회전과 스케일은 다섯 번째, 여섯 번째 인자이고 각각 테이블입니다. 이름, x, y, z, 회전, 스케일. 철자는 `zamert prefabs`에서 가져옵니다. 나중에 열고 잠글 문은 `spawn_door`로 만들고, 날것의 프리팹으로 만들지 않습니다.

## Lua에서 다른 모듈

대상은 code, 오브젝트 이름, 또는 호출이 허용하면 비워서 "이 오브젝트"입니다.

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
api.call_function("OpenHall")          -- 이 schematic의 Function Executor
api.trigger(object.code, "interacted") -- 그 오브젝트의 LuaEvents
api.trigger_group("doors", "custom")

api.counter_add("button", 1)           -- code 또는 이름으로 Counter
api.timer_start("clock")
api.timer_stop("clock")
api.light_set("lamp", 2)               -- 세기, 0은 끄기
api.rotator_speed("fan", 0, 90, 0)
api.mover_restart("platform")
api.spawn_schematic("SubRoom")         -- 이름으로 Schematic Anchor, 또는 이 이름을 여기서 생성

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

## 변수

범위는 세 개입니다. 키는 문자열입니다. 값은 숫자, 문자열, 불리언입니다.

```lua
api.set_self_var("uses", 0)             -- 이 오브젝트, 파괴될 때까지
api.inc_self_var("uses", 1)
api.get_self_var("uses")

api.set_schematic_var("power", 1)       -- 이 schematic 인스턴스
api.get_schematic_var("power")
api.inc_schematic_var("power", -1)

api.set_round_var("opened", true)       -- 다음 맵이 생성되기 시작할 때 지워집니다
api.get_round_var("opened")
```

## 시간, 확률, 파일, 명령

```lua
api.log("opened")
api.warn("missing clip")
api.error("bad state")
api.time()
api.realtime()
api.chance(0.25)                        -- 호출의 25%에서 true
api.random_int(1, 6)
api.random_float(0, 1)
api.vec(1, 2, 3)                        -- {x,y,z}
api.distance(a, b)
api.add_vec(a, b)

api.command("cassie 1 Attention")       -- LuaAllowServerCommands가 켜져 있어야 합니다
api.include("lib.lua")                  -- 이 스크립트 안에서 실행
api.run_file("once.lua")                -- 별도 스크립트, local을 공유하지 않습니다

api.delay(2, function()
  api.broadcast("later", 3)
end)
api.repeat_every(1, 5, function(ctx, tick)
  api.broadcast("tick " .. tick, 1)
end)
```

`repeat_every`는 최소 0.05초를 기다립니다. 횟수는 `LuaMaxRepeatCount`로 잘립니다(기본 100). `LuaAllowDelayedCallbacks`가 꺼져 있으면 `delay`와 `repeat_every`는 아무것도 하지 않습니다.

`include`와 `run_file`은 schematic 폴더를 먼저 보고, 그다음 플러그인 DLL 옆을 봅니다. 절대 경로를 주면 그 경로를 씁니다.

## 다른 컴포넌트의 Lua Events

모든 ZAMERT 컴포넌트에 `Lua Events` 목록이 있습니다. 예전 고리입니다. Interact, Death, Enter 위에 짧은 스크립트. Lua Module이 아닙니다. 거기서는 `api.wait`가 오류를 내고, `api.delay`를 씁니다.

| `Event Type` | 언제 |
|---|---|
| `Spawned`, `Destroyed` | 오브젝트 수명 |
| `Interacted`, `Denied` | 키 허용, 또는 키카드 거부 |
| `Searching`, `Picked` | 픽업 |
| `Damaged`, `Died` | 체력 오브젝트 |
| `Entered`, `Stayed`, `Exited` | 콜라이더 |
| `ThresholdReached`, `ThresholdDropped` | 인원 임계 |
| `Tick` | 컴포넌트가 tick할 때 |
| `Custom` | `Custom Event Name`, 그리고 `api.trigger` |

행에는 priority, chance(`0`–`1`), cooldown, 한 번만, 취소된 상태에서도 실행, 성공하면 중단이 있습니다. 인라인이나 파일, 경로 규칙은 같습니다.

이벤트 스크립트 안의 전역은 같습니다(`player`, `object`, `api`). `ctx.cancelled`, `ctx.damage`, `ctx.currentHealth`는 이벤트에 달렸습니다. `api.cancel()` / `api.uncancel()`은 컨텍스트를 표시합니다. `api.stop()`은 이 이벤트의 다음 바인딩을 끊습니다. 픽업 이벤트는 `api.remove_pickup()` 또는 `api.keep_pickup()`을 쓸 수 있습니다.

`LuaEnabled`가 꺼지면 Lua Module과 이 바인딩이 함께 멈춥니다.
