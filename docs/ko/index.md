# ZAMERT 사용 설명

버전 2.1.0. 맵을 만드는 사람을 위한 설명입니다. 아래 필드 이름은 JSON에 기록되는 이름입니다.

[설치](install.md) · [Unity](unity.md) · [Lua](lua.md) · [컴포넌트](components.md) · [문과 프리팹](doors.md) · [서버](server.md)

[Türkçe](../tr/index.md) · [English](../en/index.md) · [Русский](../ru/index.md) · [中文](../zh/index.md) · [한국어](index.md)

ZAMERT는 ProjectMER schematic에 동작을 붙입니다. 키, 시설 문, 네트워크 프리팹, 체력 오브젝트, 스피커, 타이머, Lua입니다. 메시는 MER의 `{이름}.json`에 남습니다. 동작은 그 옆 파일에 있습니다.

![컴파일 경로](../assets/pipeline.svg)

로직은 두 가지로 씁니다.

- **Lua Module**. 빈 오브젝트나 메시에 둡니다. Lua 그대로입니다. `if`, `for`, `while`, `api.wait`.
- **Function Executor**. schematic 루트에 둡니다. `If`, `While`, `Wait`, `SendMessage` 같은 노드이고, 파일은 `{이름}-Functions.json`입니다. 예전 맵이 이것을 씁니다. 그대로 실행됩니다.

Interactable Object, Health Object와 그 밖의 기존 컴포넌트는 늘 쓰던 파일로 컴파일됩니다. 새 컴포넌트는 새 접미사를 더할 뿐, 옛 파일 내용을 바꾸지 않습니다.

플러그인이 서버에 없으면 [설치](install.md)부터 봅니다. 서버가 이미 떠 있고 스크립트만 쓰면 [Lua](lua.md)로 갑니다.
