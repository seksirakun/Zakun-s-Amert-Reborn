# 설치

[설명](index.md) · [Unity](unity.md) · [Lua](lua.md) · [컴포넌트](components.md) · [문과 프리팹](doors.md) · [서버](server.md)

## 먼저 있어야 하는 것

- SCP: Secret Laboratory 데디케이티드 서버
- LabAPI 1.1.6 이상
- ProjectMER. 별도 플러그인으로 설치되어 있어야 합니다.

ZAMERT 로드 우선순위는 `Lowest`입니다. MER가 올라온 뒤에 붙습니다.

Loop Speaker, 상호작용 소리, `AudioModules`는 서버에 AudioPlayerApi가 있어야 합니다. 없으면 그 컴포넌트는 오류를 남기고, 플러그인 나머지는 로드됩니다.

## 빌드

이 폴더에서:

```text
dotnet build ZAMERT\ZAMERT.csproj -c Release
```

결과: `ZAMERT\bin\Release\`.

| 파일 | 위치 |
|---|---|
| `ZAMERT.dll` | `%AppData%\SCP Secret Laboratory\LabAPI\plugins\global\` |
| `MoonSharp.Interpreter.dll`, `0Harmony.dll`, `Newtonsoft.Json.dll` | 서버 로그가 없다고 하면 `%AppData%\SCP Secret Laboratory\LabAPI\dependencies\` |

서버가 포트 폴더로 나뉘어 있으면 (`plugins\7777\`) 거기 넣어도 됩니다. 한 벌이면 됩니다.

`ZAMERT\libs\ProjectMER.dll`은 그 자리에 둡니다. 컴파일 참조일 뿐입니다. 돌아가는 서버는 직접 설치한 ProjectMER 플러그인을 씁니다.

## 첫 실행

서버를 한 번 켭니다. LabAPI가 `LabAPI\configs\` 아래에 ZAMERT 설정을 씁니다. 파일 이름은 LabAPI 버전에 따르고, 플러그인 이름이 `ZAMERT`인 항목입니다.

오디오 폴더가 없으면 만들어집니다.

`%AppData%\SCP Secret Laboratory\LabAPI\audio\`

클립은 거기에 둡니다. Loop Speaker의 `AudioName`은 그 폴더 안 파일 이름입니다.

## 로드됐는지 확인

Remote Admin:

```text
zamert
```

응답에 버전이 있습니다. 라운드에서 schematic이 생긴 뒤에는 살아있는 ZAMERT 오브젝트 수도 있습니다.

```text
zamert prefabs
zamert prefabs door
```

`ZAMERT.dll` 옆에 `ZAMERT-prefabs.txt`를 쓰고 앞쪽 이름을 출력합니다. 이 목록이 그 서버 빌드에서 쓸 수 있는 `PrefabName` / `api.spawn_prefab` 이름입니다. 게임 업데이트는 이름을 바꿉니다. 맵을 실제로 돌리는 서버에서 파일을 가져옵니다.

```text
zamert audio
```

캐시된 클립을 지웁니다. 다음 재생은 파일을 다시 읽습니다.

## Unity 도구

서버 DLL에는 에디터 스크립트가 없습니다. 저장소의 `Tools` 폴더를 맵 프로젝트로 복사합니다.

```text
맵프로젝트\Assets\ZAMERT\
```

Unity 2021.3.17f1을 씁니다. 스크립트가 컴파일되면 메뉴 막대에 `SchematicManager`가 생깁니다. 메뉴가 없으면 스크립트가 `Assets` 밖에 있거나, Console에 에디터 버전 차이로 생긴 컴파일 오류가 있습니다.

다음 페이지는 컴파일 순서입니다. MER가 schematic을 먼저 내보내야 ZAMERT가 JSON을 쓸 수 있습니다.
