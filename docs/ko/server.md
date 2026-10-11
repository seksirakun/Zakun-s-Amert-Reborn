# 서버

[설명](index.md) · [설치](install.md) · [Unity](unity.md) · [Lua](lua.md) · [컴포넌트](components.md) · [문과 프리팹](doors.md)

설정은 첫 실행에 생깁니다. 아래 값은 2.1.0 기본값입니다.

## Remote Admin

| 명령 | |
|---|---|
| `zamert` 또는 `zm` | 버전, schematic 수, 라운드 변수, 컴포넌트별 살아있는 오브젝트 수. |
| `zamert prefabs` | 네트워크 프리팹 목록을 새로 고치고, DLL 옆에 `ZAMERT-prefabs.txt`를 쓰고, 앞 12개 이름을 출력합니다. |
| `zamert prefabs door` | 같음, 필터. 필터는 부분 문자열입니다. |
| `zamert audio` | 캐시된 오디오 클립을 잊습니다. |

프리팹 파일이 이름을 베낄 파일입니다. 명령을 실행할 때마다, 그 명령을 실행한 서버 기준으로 다시 써집니다.

## 설정

| 키 | 기본 | |
|---|---|---|
| `Debug` | false | 추가 로그. |
| `AudioFolderPath` | `LabAPI/audio` | 클립 폴더. 없으면 만듭니다. |
| `AudioMasterVolume` | 1 | 서버 전체 배율. 0은 ZAMERT 소리를 끕니다. |
| `EnablePerClientAudioVolume` | true | Server-Specific Settings에 `ZAMERT - Audio Volume` 슬라이더를 넣습니다. 단발음만. |
| `EnableIoToys` | false | 키가 `IoToysKeycodes`에 있는 interactable은 toy로 생깁니다. |
| `IoToysKeycodes` | 0, 101 | None과 E. |
| `IoToysNoRoot` | false | 루트 interactable에는 toy를 만들지 않습니다. |
| `IoToysDebug` | false | toy interactable 자리에 보이는 primitive. |
| `IoToysBothModes` | false | toy와 설정 키를 같이. 200ms, 한 번 누름이 두 번 돌지 않게. |
| `InteractableObjectDetectionMode` | RaycastWithAssist | `RaycastOnly`, `RaycastWithAssist`, `ProximityOnly`. |
| `InteractableObjectAssistSphereRadius` | 0.35 | 보조 탐지 크기. |
| `InteractableObjectAssistAngleDegrees` | 12 | 보조 조준 원뿔. |
| `LuaEnabled` | true | Lua Module과 Lua Events의 스위치. |
| `LuaCacheFileScripts` | true | `ScriptPath` 파일을 캐시하고, 쓴 시각이 바뀌면 다시 읽습니다. |
| `LuaAllowServerCommands` | true | `api.command`. |
| `LuaAllowDelayedCallbacks` | true | `api.delay`와 `api.repeat_every`. |
| `LuaMaxRepeatCount` | 100 | `api.repeat_every` 상한. |
| `LuaLogApiErrors` | true | 실패한 `api.*` 호출을 조용한 nil 대신 로그합니다. |
| `AutoRun` | false | MER 문 생성 고리. 그 고리를 모르면 꺼 둡니다. |
| `GroovyNoiseFireOnEnable` | true | Groovy Noise가 켜지면 한 번 실행합니다. |
| `DisableSSS` | false | ZAMERT 키와 음량 슬라이더를 등록하지 않습니다. |
| `CustomSpawnPointEnable` | true | 커스텀 스폰 지점. |
| `MaxFunctionWaitSeconds` | 600 | Function Executor `Wait`와 `api.wait` 상한. 0은 상한 없음. |
| `LogSchematicSummary` | true | schematic마다 한 줄: 몇 행을 읽었고 몇 행을 건너뛰었는지. |
| `LuaAutoYieldInstructions` | 2500 | Lua Module이 한 프레임을 내주기 전에 처리할 명령 수. |
| `LuaMaxBusySlices` | 8 | `api.wait` 없이 몇 조각 뒤에 스크립트를 멈출지. |
| `LuaMaxProgramsPerFrame` | 8 | 한 프레임에 재개하는 모듈 코루틴 수. |
| `Gates` | 예시 항목 | 맵 이름별 dummy gate. |

`Gates`는 자리표시 맵 이름과 함께 옵니다. 바꾸거나 둡니다. 모르는 맵 이름은 아무 일도 하지 않습니다.

## 로더가 하는 일

schematic이 생기면 ZAMERT는 아는 옆 파일을 모두 읽습니다. 각 행의 transform은 이 순서로 풉니다. `ObjectNamePath`, 전체 `ObjectId`, 예전 방식의 더 짧은 인덱스 걷기, 두 필드가 비어 있으면 schematic 루트. 나머지는 건너뛰고 셉니다. `LogSchematicSummary`가 합계를 출력합니다.

라운드 변수(`api.set_round_var`)는 schematic이 생긴 뒤가 아니라, 다음 맵이 생성되기 시작할 때 지워집니다. 키는 맵 생성이 끝난 뒤 다시 등록됩니다. 파괴된 오브젝트는 목록에서 빠집니다. 맵 생성이 끝났다고 목록을 비우지는 않습니다. 거기서 비우면, 같은 이벤트 동안 생긴 schematic이 지워졌습니다.

## 오디오

파일은 `AudioFolderPath`에 있습니다. 파일을 바꾼 뒤 `zamert audio`가 클립 캐시를 지웁니다. 플레이어별 음량은 설정 슬라이더이고, `AudioMasterVolume`을 곱합니다. 슬라이더는 반복 스피커가 아니라 단발음에만 갑니다.

## 스크립트가 멈추는 때

`api.wait`를 한 번도 부르지 않는 Lua Module은 명령 한도를 다 쓰고 멈춥니다. 로그 줄: `Lua on {오브젝트} was stopped because it ran too long without api.wait()`. 루프 안에 `api.wait`를 넣습니다. 그 모듈에 코루틴이 이미 8개면 `already has 8 running scripts`가 나옵니다.

문법 오류나 실행 오류는 `Lua module failed to start` 또는 `Lua failed on {오브젝트}`이고, Lua 메시지가 붙습니다. `LuaLogApiErrors`는 `api.give_item`과 다른 도우미의 실수(모르는 아이템 이름, nil 플레이어)를 스크립트 전체를 죽이지 않고 감쌉니다.

## 옛 맵

접미사, `$type` 어셈블리 이름 `AdvancedMERTools`, 잘못 적힌 필드는 그대로입니다. 2.1.0 전에 만든 맵을 로드하는 데 다시 컴파일할 필요는 없습니다. 새 컴포넌트를 원하면 별개입니다. 그 접미사를 컴파일하고 새 파일을 옛 파일 옆에 둡니다.
