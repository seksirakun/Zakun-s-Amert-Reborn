# 문과 프리팹

[설명](index.md) · [설치](install.md) · [Unity](unity.md) · [Lua](lua.md) · [컴포넌트](components.md) · [서버](server.md)

문이라고 부르는 것은 세 가지입니다.

| 원하는 것 | 컴포넌트 |
|---|---|
| 키카드와 잠금이 있는 실제 LCZ / HCZ / EZ / bulk / gate 문 | **Map Door** |
| Animator가 재생하는 자기 메시 | **Custom Door** |
| 그 밖의 네트워크 오브젝트(워크스테이션, 사물함, 표적, toy, 날것 이름의 문 프리팹) | **Prefab Anchor** 또는 `api.spawn_prefab` |

## Map Door

문이 설 자리의 빈 오브젝트에 Map Door를 붙입니다. 그 오브젝트의 회전과 스케일이 문의 회전과 스케일입니다. 컴파일은 `{이름}-MapDoors.json`을 씁니다.

| 필드 | |
|---|---|
| `DoorType` | 아래 Unity enum. 기본은 Entrance이고, 5로 저장되며, EZ 문을 만듭니다. |
| `Permissions` | `DoorPermissionFlags`. None이면 카드가 필요 없습니다. |
| `Require All Permissions` | 고른 플래그를 모두 들고 있어야 합니다. |
| `Is Open` | 열린 채로 생깁니다. |
| `Is Locked` | 특수 문 잠금으로 잠긴 채 생깁니다. |

![Map Door 값](../assets/doors.svg)

서버는 schematic이 생길 때 이 표를 적용합니다. ProjectMER 자신의 enum은 더 짧고(0–4) 이 숫자와 맞지 않습니다. ZAMERT가 바꿉니다. 옛 `MapDoors.json`의 숫자를 "고친다"고 다시 매기지 않습니다.

Lua에서는 실행 중에 이 enum을 거치지 않습니다.

```lua
api.spawn_door("lcz")
api.spawn_door("hcz")
api.spawn_door("ez")       -- entrance
api.spawn_door("bulk")
api.spawn_door("gate")
api.spawn_door("ez", x, y, z)
```

`api.door_open(1)`과 `api.door_lock(1)`은 스크립트 오브젝트 6미터 안의 가장 가까운 실제 문에 갑니다. 맵에 원래 있던 문도 포함됩니다. 범위를 바꾸려면 두 번째 숫자를 줍니다. `0`은 닫거나 잠금을 풉니다.

Map Door 생성은 MER의 문 설정을 씁니다. 그 타입의 프리팹, 잠금, 권한, 그다음 오브젝트 자세에서 네트워크 스폰. 콘솔이 문 프리팹이 준비되지 않았다고 하면 ProjectMER가 프리팹 등록을 끝내지 않은 것입니다. ZAMERT는 마지막에 로드되므로 보통은 끝나 있습니다. 흔한 원인은 MER 플러그인이 없는 경우입니다.

## SCP-079 카메라

`Scp079Cameras.json`. `CameraType`: `Lcz` 0, `Hcz` 1, `Ez` 2, `EzArm` 3, `Sz` 4. `Label`은 079 화면의 이름이고 기본은 `Custom Camera`입니다. 이 숫자는 이미 MER와 맞습니다. 카메라는 오브젝트 transform에 놓입니다.

## Clutter

`Clutter.json`. 시설 클러터 조각 중 하나:

| 값 | 조각 |
|---|---|
| 0 | Simple Boxes |
| 1 | Pipes Short |
| 2 | Boxes Ladder |
| 3 | Tank Supported Shelf |
| 4 | Angled Fences |
| 5 | Huge Orange Pipes |
| 6 | Pipes Long Open |

## Prefab Anchor

서버가 쓰는 이름으로, 아무 네트워크 프리팹.

| 필드 | |
|---|---|
| `Prefab Name` | 그대로인 문자열. `workstation`이 아니라 `Work Station`. |
| `Spawn On Start` | schematic과 함께 생깁니다. |
| `Spawn As Child` | 앵커의 자식이 됩니다. |
| `Match Scale` | 앵커 스케일을 복사합니다. |
| `Disable Anchor Renderers` | 안내로 둔 에디터 메시를 숨깁니다. |
| `Destroy Anchor After Spawn` | 프리팹이 생긴 뒤 앵커 오브젝트를 지웁니다. |

지금 돌리는 빌드의 이름:

```text
zamert prefabs
zamert prefabs door
```

전체 목록은 플러그인 DLL 옆 `ZAMERT-prefabs.txt`에 써집니다. `api.list_prefabs("work")`는 스크립트에 최대 40개 일치를 돌려줍니다.

```lua
api.spawn_prefab("Work Station")
api.spawn_prefab("Work Station", 12, 1.2, 8)
```

`api.spawn_prefab`은 net id가 아직 0이면 네트워크 스폰합니다. 인스턴스를 schematic에 붙이지 않으므로 schematic이 사라져도 오브젝트는 남습니다. Prefab Anchor에서 Spawn As Child를 켜면 붙고, 그러면 schematic과 함께 사라집니다. Map Door와 `api.spawn_door`는 schematic에 기록되고 함께 지워집니다. 방, 사물함 배치, 문 권한을 기대하는 프리팹은 불완전하게 뜰 수 있습니다. 시설 문은 Map Door나 `api.spawn_door`로 갑니다. 워크스테이션, 표적, toy, clutter 조각이 Prefab Anchor의 자리입니다.

게임 업데이트는 프리팹 이름을 바꿉니다. 이름이 멈추면 그 서버에서 `zamert prefabs`를 다시 실행하고 새 철자를 `PrefabName`에 붙입니다.
