# 컴포넌트

[설명](index.md) · [설치](install.md) · [Unity](unity.md) · [Lua](lua.md) · [문과 프리팹](doors.md) · [서버](server.md)

메뉴: `SchematicManager → Compile All ZAMERT`. 행은 컴포넌트, JSON 접미사, 이름이 `Hall`인 schematic이 만드는 파일입니다.

| 컴포넌트 | 접미사 | 파일 |
|---|---|---|
| Interactable Object | `Objects`와 `FObjects` | `Hall-Objects.json` |
| Health Object | `HealthObjects` / `FHealthObjects` | |
| Interactable Pickup | `Pickups` / `FPickups` | |
| Custom Collider | `Colliders` / `FColliders` | |
| Custom Door | `Doors` | 애니메이션 schematic 문. 시설 문이 아닙니다 |
| Custom Interactable Toy | `ToyInteractables` / `FToyInteractables` | |
| Loop Speaker | `LoopSpeakers` | |
| Item Spawner | `ItemSpawners` | |
| Damage Trigger | `DamageTriggers` | |
| Player Count Trigger | `PlayerCountTriggers` / `FPlayerCountTriggers` | |
| Prefab Anchor | `PrefabAnchors` | |
| Custom Texture | `CustomTextures` / `FCustomTextures` | |
| Map Door | `MapDoors` | 시설 문 |
| Clutter | `Clutter` | |
| SCP-079 Camera | `Scp079Cameras` | |
| Spin Motor | `Rotators` | |
| Move Motor | `Movers` | |
| Light Controller | `Lights` | |
| Timer Relay | `Timers` | |
| Schematic Anchor | `SchematicAnchors` | |
| Counter | `Counters` | |
| Lua Module | `LuaModules` | |
| Function Executor | `Functions` | `Hall-Functions.json`, 루트에 |
| Interactable Teleporter | — | 내보내기 루트의 `{맵이름}-ITeleporters.json` |

`F` 파일은 **Use Script Value**가 켜져 있을 때 씁니다. 그 필드는 맨 숫자 대신 `ScriptValue` 노드입니다(상수, 변수, 무작위, 플레이어 스탯). 안의 속성 이름은 옛 맵이 이미 쓰는 이름입니다.

상호작용, 체력 오브젝트, toy, 인원 트리거에는 같은 동작 목록이 있습니다.

| 목록 | 효과 |
|---|---|
| `MessageModules` | broadcast / hint / cassie 식 메시지 |
| `commandings` | 서버 콘솔 명령 |
| `dropItems` | 아이템 생성 |
| `ExplodeModules` | 폭발 |
| `effectGivingModules` | 상태 효과 |
| `AudioModules` | 오디오 폴더의 단발 클립 |
| `GroovieNoiseToCall` | id나 그룹으로 Groovy Noise 실행. `Groovie` 철자는 파일 형식입니다. |
| `FunctionToCall` | 이름으로 Function Executor 함수 |
| `AnimationModules` | 상태 재생. `AnimatorAdress`는 경로이고, 일부러 그렇게 씁니다. |
| `warheadActionType` | 핵 시작 / 정지 / 잠금 / 기폭 계열 동작 |
| `PrimitiveModifyModules` | primitive 이동, 색, 켜고 끄기 |
| `LoopSpeakerModules` | 재생, 정지, 음량 |
| `ItemSpawnerModules` | 생성, 정지, 리셋 |
| `PlayerLinkModules` | 동작을 상호작용한 플레이어에 묶기 |

Interactable Object와 Custom Interactable Toy의 Deny 목록은 키카드 검사가 실패하면 돕니다. `KeycardPermissions`가 비어 있으면 카드 검사가 없습니다. `RequireAllPermissions`는 고른 플래그를 전부 요구합니다.

## Interactable Object

`InputKeyCode`, `InteractionMaxRange`(validate에서 0은 5가 됨), `InteractionCooldown`(0은 0.2초가 되고, 그 오브젝트의 ZAMERT 컴포넌트가 공유). `IgnoredRoles`는 그 역할을 건너뜁니다. `ActionType`은 어느 동작 묶음이 돌지 고릅니다.

감지는 설정의 `InteractableObjectDetectionMode`입니다. 레이만, 레이와 좁은 조준 원뿔(`InteractableObjectAssistAngleDegrees`, 기본 12), 또는 범위 안 가장 가까운 것. 일부 키를 toy로 바꾸는 스위치는 따로입니다. `EnableIoToys`와 `IoToysKeycodes`(기본 0과 101).

## Health Object

`Health`, `ArmorEfficient`(0–100), `DeadType`, `DeadActionDelay`, `ResetHPTo`, `DoNotDestroyAfterDeath`. `whitelistWeapons`는 손에 든 어느 아이템이 피해를 주는지 제한합니다. `AllowedScpDamageTypes`는 SCP 근접 화이트리스트입니다. `All`은 지원하는 SCP 타격을 모두 허용합니다.

메시지가 이 토큰을 쓰면: `{attacker_i}`, `{attacker_name}`, `{a_pos}`, `{a_room}`, `{a_zone}`, `{a_role}`, `{s_pos}`, `{s_room}`, `{s_zone}`, `{a_item}`, `{damage}`.

## Item Spawner

`ItemType`(`None`이면 끔), `Count`, `LocalPosition`, `AutoStart`, `SpawnDelay`(0은 0.5초를 둠), `RespawnTime`(0이면 아이템을 가져간 뒤 다시 만들지 않음).

## Damage Trigger

`DamageAmount`, `DamageInterval`, `MinimumHealth`(0이면 처치 허용), `AutoStart`, `TriggerMode`(입장, 머무름, 퇴장, 또는 섞음), `KillInstant`, `AffectHumans`, `AffectScps`. 플레이어가 닿을 수 있는 콜라이더가 있어야 합니다.

## Player Count Trigger

`TriggerThreshold`, `TriggerMode`(도달 또는 하락), `Cooldown`, `AutoStart`, 그리고 공통 동작 목록. Lua 이벤트: `ThresholdReached`, `ThresholdDropped`.

## Loop Speaker

`AudioName`은 `LabAPI\audio` 안의 파일입니다. `Volume`, `IsSpatial`, `MinDistance`, `MaxDistance`, `LocalPosition`, `AutoStart`. AudioPlayerApi가 필요한 것이 이것입니다.

## Custom Door

schematic 안의 애니메이션 문입니다. LCZ/HCZ 문이 아닙니다. 여기 `DoorType`은 애니메이터용 분류로 `LCZ`, `HCZ`, `EZ`입니다. `OpenAnimation`, `CloseAnimation`, `LockAnimation` 기본값은 `DoorOpen`, `DoorClose`, `DoorLock`입니다. 애니메이터 오브젝트는 `Animator`에 계층 경로로 들어갑니다. 진짜 시설 문은 Map Door를 씁니다.

## Custom Texture

schematic의 텍스처를 면에 얹습니다. `SchematicName`, `Mode`, `StretchX`, `StretchY`, `Faces`, `ZFightOffset`(기본 0.005), `MaxDuplicates`(기본 256), `ManualSize`, `FlipNormal`, `PivotOffset`. 스크립트 파일은 `FCustomTextures`입니다.

## Spin Motor, Move Motor, Light

| | 필드 |
|---|---|
| Spin Motor | `AngularSpeed`(초당 도, `x`,`y`,`z`, 기본 y = 45), `UseWorldSpace` |
| Move Motor | `LocalOffset`, `Duration`(기본 2), `StartDelay`, `PingPong`, `Loop`, `AutoStart` |
| Light Controller | 조명 위. `ApplyOnStart`, `Enabled`, `OverrideColor`, `Color`, `OverrideIntensity`, `Intensity`(0 = 끔), `OverrideRange`, `Range`, `Pulse`, `PulseSpeed`, `PulseMin`, `PulseMax` |

Lua: `api.rotator_speed`, `api.mover_restart`, `api.light_set`.

## Timer와 Counter

Timer Relay: `Delay`, `Interval`(기본 1), `Repeat`, `RepeatCount`(Repeat가 켜져 있고 0이면 계속), `AutoStart`, 그다음 대상 하나: `FunctionName`, 또는 `GroovyNoiseId`, 또는 `GroovyNoiseGroup`.

Counter: `StartValue`, `TargetValue`(기본 1), `Step`(기본 1), `ResetOnReach`, 값이 목표에 닿으면 같은 세 대상. Lua는 `api.counter_add`로 더합니다.

## Schematic Anchor

이미 컴파일된 다른 schematic을 만듭니다. `SchematicName`, `SpawnOnStart`, `Scale`. 깊이는 4까지입니다. schematic은 자기 anchor로 자기를 만들지 않습니다. `api.spawn_schematic("이름")`은 그 이름을 호출자의 transform에 만들거나, 그 이름의 anchor에게 만들라고 합니다.

## Function Executor

schematic 루트에 둡니다. 파일 하나, `{이름}-Functions.json`. 공개 함수는 `FunctionToCall`, Timer/Counter, 또는 `api.call_function("이름")`으로 부릅니다.

쌓은 순서의 노드:

`If`, `ElseIf`, `Else`, `While`, `For`, `ForEach`, `SetVariable`, `Return`, `Wait`, `CallFunction`, `CallGroovyNoise`, `PlayAnimation`, `SendMessage`, `SendCommand`, `DropItems`, `Explode`, `GiveEffect`, `PlayAudio`, `FWarhead`, `ChangePlayerValue`, `PlayerAction`, `ChangeEntityValue`.

`Wait`는 그 함수의 나머지를 붙잡습니다. 한도는 `MaxFunctionWaitSeconds`입니다. `Wait`를 만난 `While` / `For` / `ForEach`는 빈 회전을 하지 않고, 시간이 지나면 이어갑니다. 중첩된 `If`와 `Wait`는 같은 지연 뒤에 부모와 자식을 함께 재개합니다. 순서가 중요하면 wait를 부모에 둡니다.

`F` 컴포넌트와 함수 안의 값(`Integer`, `Float`, 플레이어 체력, 역할 검사, `TrueForAll`과 나머지)은 Script Value 타입입니다. `$type` 이름은 어셈블리 `AdvancedMERTools`에 남습니다. 옛 파일이 그 때문에 열립니다.

## Interactable Teleporter

`TargetMapName`은 MER 맵 이름, `TeleporterIndex`는 그 맵에서 1부터 세는 텔레포트 인덱스입니다. 컴파일은 내보내기 루트에 `{TargetMapName}-ITeleporters.json`을 씁니다. MER의 Maps 폴더로 복사합니다. 서버는 그 맵이 로드될 때 읽습니다. `InvokeType`은 충돌인지, 다른 텔레포트 고리인지 고릅니다. 동작 목록은 interactable과 같은 묶음입니다.

## Custom Collider, Groovy Noise, toy

Custom Collider는 schematic과 함께 나가는 플레이 콜라이더입니다(`Colliders.json`). 무언가 닿으면 다른 모듈과 같은 동작 묶음을 씁니다.

Groovy Noise는 이름을 가진, 다시 부를 수 있는 동작 묶음입니다. 다른 컴포넌트는 id(`GroovieNoiseId`)나 `Script Group`으로 부릅니다. `GroovyNoiseFireOnEnable`(기본 켜짐)은 무언가가 Groovy Noise의 `Active`를 켜면 한 번 실행합니다. 예전 다단계 문이 여기에 기대어 있습니다.

Custom Interactable Toy는 primitive toy를 향한 interactable입니다. `IncludeRootPrimitive`, `IncludeChildPrimitives`, `ToyScaleMultiplier`(기본 1.3), 그리고 같은 동작·거부 목록. `EnableIoToys`가 적어 둔 키에 바꿔 끼우는 것이 이것입니다.
