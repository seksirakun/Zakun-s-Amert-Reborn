# Unity

[설명](index.md) · [설치](install.md) · [Lua](lua.md) · [컴포넌트](components.md) · [문과 프리팹](doors.md) · [서버](server.md)

## 순서

schematic 루트에는 MER의 `Schematic` 컴포넌트가 있습니다. GameObject 이름이 폴더 이름입니다. 컴파일하기 전에 루트 이름을 바꿉니다.

1. MER 에디터에서 메시를 배치합니다.
2. ProjectMER로 컴파일합니다. 내보내는 경로는 `SchematicManager.Config.ExportPath`입니다. 그 경로가 없으면 ZAMERT는 `바탕화면\MapEditorReborn_CompiledSchematics`로 갑니다.
3. 이 schematic의 자식에 ZAMERT 컴포넌트를 붙입니다.
4. `SchematicManager → Compile All ZAMERT`.

4번은 2번의 폴더에 씁니다. 폴더가 없으면 Console에 `could not find the compiled-schematics directory`가 나오고 파일은 생기지 않습니다.

개별 메뉴(`Compile Lua Modules`, `Compile Map Doors`, …)는 접미사 하나만 씁니다. `Compile All ZAMERT`는 전부 씁니다. `Decompile All`은 계층으로 다시 읽습니다. 일치는 `ObjectNamePath`가 먼저이고, 그다음이 `ObjectId`입니다.

![schematic 폴더 안의 파일](../assets/folder.svg)

## 무엇이 써지는가

일반 컴포넌트(Lua Module, Map Door, Prefab Anchor, 모터, 조명, 타이머, 카운터, schematic anchor, clutter, 카메라, item spawner, damage trigger, loop speaker, custom door)는 항상 `{이름}-{접미사}.json`을 씁니다.

**Use Script Value**가 있는 컴포넌트는 `F` 파일도 있습니다. `Objects.json`과 `FObjects.json`, `HealthObjects.json`과 `FHealthObjects.json`. pickup, collider, toy, player-count trigger, custom texture도 같습니다. 스크립트 값을 내보내려면 **Use Script Value**를 켭니다. 꺼 두면 일반 목록이 나갑니다. 두 파일이 이미 있는 옛 맵은 둘 다 유지되고, 로더는 둘 다 읽습니다.

Function Executor는 다릅니다. schematic 루트에 있고 `{이름}-Functions.json`을 씁니다.

Interactable Teleporter는 schematic 폴더 안이 아니라 내보내기 루트에 `{TargetMapName}-ITeleporters.json`을 씁니다. 그 파일의 `ObjectId`는 1부터 세는 텔레포트 인덱스입니다. 파일을 MER의 Maps 폴더에, 맵 옆에 복사합니다.

## 식별 필드

`ObjectId`와 `ObjectNamePath`는 손으로 넣지 않습니다. 컴파일이 찍습니다.

| 필드 | 의미 |
|---|---|
| `ObjectNamePath` | schematic 루트에서 아래로 이어진 오브젝트 이름. `/`로 잇습니다. 이름 안의 슬래시는 `\/`로 저장됩니다. 서버가 먼저 맞추는 값입니다. |
| `ObjectId` | 형제 인덱스. 오브젝트 자신의 인덱스가 먼저, 그다음 부모. 공백으로 나뉩니다. 이름 경로가 빗나가면 이것을 봅니다. |
| `Code` | validate/컴파일 시점의 Unity instance id. 도메인 리로드 뒤 다시 컴파일하면 바뀝니다. 한 번 컴파일한 결과 안에서 `api.find_by_code`에 씁니다. 영구 id가 아닙니다. |
| `Script Group` | 컴포넌트에 직접 적은 글. 고정입니다. Lua에서는 `object.scriptGroup`, 그리고 `api.find_group`. |
| `Active` | 켜진 채로 시작합니다. 다른 컴포넌트와 Lua가 끌 수 있습니다. |

오브젝트 이름을 바꾸거나 형제 순서를 바꾸면 경로가 바뀝니다. 그 다음 다시 컴파일합니다. 서버는 풀지 못한 행을 건너뛰고, 맞지 않은 `ObjectId` / `ObjectNamePath`를 로그에 남깁니다.

## 키

`InputKeyCode`는 Unity `KeyCode` 숫자입니다. `E`는 101, `F`는 102, `None`은 0입니다. Lua Module에서 0은 이 모듈에 키가 없다는 뜻입니다.

서버는 서로 다른 키마다 Server-Specific Settings에 한 줄을 등록합니다. `ZAMERT - Interactable Object - E`. 플레이어가 한 번 바인드합니다. 제안 키는 고른 그 키이고, 플레이어가 다시 묶을 수 있습니다. 설정의 `DisableSSS`는 이 등록을 끕니다. 그러면 ZAMERT 키는 하나도 동작하지 않습니다.

그 줄이 없고 플레이어가 바인드하지 않았으면 키보드의 E는 아무 일도 하지 않습니다. 관전자는 트리거하지 않습니다.

## 콜라이더

키 상호작용은 카메라에서 레이를 쏩니다. 오브젝트나 자식에 콜라이더가 있어야 합니다. 트리거 콜라이더도 포함됩니다. 첫 고체 히트가 뒤를 가립니다.

Lua Module에는 대비가 있습니다. 레이가 아무것도 맞히지 못하면, 그 키에 등록된 모듈이 `InteractionMaxRange` 안(기본 5)이고 시선 35도 안이면 여전히 실행될 수 있습니다.

`ListenEnter` / `ListenStay` / `ListenExit`는 같은 오브젝트의 트리거 콜라이더가 필요합니다. 콜라이더가 트리거이고 리지드바디가 없으면, 모듈이 kinematic 리지드바디를 붙입니다. Unity는 그때서야 트리거 메시지를 보냅니다. `on_stay`는 콜라이더 하나당 초당 최대 네 번입니다.

## 맵 옆의 스크립트 파일

`ScriptPath`는 schematic 폴더 기준 파일 이름이나 절대 경로입니다. `Inline Script`가 비어 있지 않으면 그것이 이깁니다. 예: schematic 폴더에 `buttons.lua`가 있고, 모듈의 Script Path가 `buttons.lua`입니다.
