# Bileşenler

[Kılavuz](index.md) · [Kurulum](install.md) · [Unity](unity.md) · [Lua](lua.md) · [Kapılar ve prefab](doors.md) · [Sunucu](server.md)

Menü: `SchematicManager → Compile All ZAMERT`. Satır, bileşen, JSON son eki ve `Hall` adlı schematic için çıkan dosyadır.

| Bileşen | Sonek | Dosya |
|---|---|---|
| Interactable Object | `Objects` ve `FObjects` | `Hall-Objects.json` |
| Health Object | `HealthObjects` / `FHealthObjects` | |
| Interactable Pickup | `Pickups` / `FPickups` | |
| Custom Collider | `Colliders` / `FColliders` | |
| Custom Door | `Doors` | animasyonlu schematic kapısı, tesis kapısı değil |
| Custom Interactable Toy | `ToyInteractables` / `FToyInteractables` | |
| Loop Speaker | `LoopSpeakers` | |
| Item Spawner | `ItemSpawners` | |
| Damage Trigger | `DamageTriggers` | |
| Player Count Trigger | `PlayerCountTriggers` / `FPlayerCountTriggers` | |
| Prefab Anchor | `PrefabAnchors` | |
| Custom Texture | `CustomTextures` / `FCustomTextures` | |
| Map Door | `MapDoors` | tesis kapısı |
| Clutter | `Clutter` | |
| SCP-079 Camera | `Scp079Cameras` | |
| Spin Motor | `Rotators` | |
| Move Motor | `Movers` | |
| Light Controller | `Lights` | |
| Timer Relay | `Timers` | |
| Schematic Anchor | `SchematicAnchors` | |
| Counter | `Counters` | |
| Lua Module | `LuaModules` | |
| Function Executor | `Functions` | `Hall-Functions.json`, kökte |
| Interactable Teleporter | — | dışa aktarma kökünde `{HaritaAdı}-ITeleporters.json` |

`F` dosyası **Use Script Value** açıkken yazılır. O alanlar ham sayı yerine `ScriptValue` düğümüdür (sabit, değişken, rastgele, oyuncu istatistiği). İçindeki özellik adları eski haritaların kullandığı adlardır.

Ortak aksiyon listeleri interactable, health object, toy ve player-count trigger üzerinde durur:

| Liste | Etki |
|---|---|
| `MessageModules` | broadcast / hint / cassie tarzı mesaj |
| `commandings` | sunucu konsol komutu |
| `dropItems` | eşya doğur |
| `ExplodeModules` | patlama |
| `effectGivingModules` | durum etkisi |
| `AudioModules` | ses klasöründen tek seferlik klip |
| `GroovieNoiseToCall` | id veya grupla Groovy Noise çalıştır. `Groovie` yazımı dosya formatıdır. |
| `FunctionToCall` | ada göre Function Executor fonksiyonu |
| `AnimationModules` | state çal. `AnimatorAdress` yoldur, bilerek böyle yazılır. |
| `warheadActionType` | warhead başlat / durdur / kilitle / patlat türü aksiyon |
| `PrimitiveModifyModules` | primitive taşı, boya, aç-kapa |
| `LoopSpeakerModules` | çal, durdur, ses |
| `ItemSpawnerModules` | doğur, durdur, sıfırla |
| `PlayerLinkModules` | aksiyonu etkileşen oyuncuya bağla |

Interactable Object ve Custom Interactable Toy üzerindeki Deny listeleri kart kontrolü düşünce çalışır. `KeycardPermissions` boşsa kart kontrolü yoktur. `RequireAllPermissions` seçili her bayrağı ister.

## Interactable Object

`InputKeyCode`, `InteractionMaxRange` (validate'de 0, 5 olur), `InteractionCooldown` (0, 0.2 sn olur; o objedeki ZAMERT bileşenleri paylaşır). `IgnoredRoles` o rolleri atlar. `ActionType` hangi aksiyon demetinin çalışacağını seçer.

Algılama, config'deki `InteractableObjectDetectionMode`: sadece ray, ray artı küçük nişan konisi (`InteractableObjectAssistAngleDegrees`, varsayılan 12), ya da menzildeki en yakın. Bazı tuşlar için toy ayrı anahtardır: `EnableIoToys` ve `IoToysKeycodes` (varsayılan 0 ve 101).

## Health Object

`Health`, `ArmorEfficient` (0–100), `DeadType`, `DeadActionDelay`, `ResetHPTo`, `DoNotDestroyAfterDeath`. `whitelistWeapons` hangi eldeki eşyanın vuracağını sınırlar. `AllowedScpDamageTypes` SCP yakın dövüş beyaz listesidir; `All` desteklenen her SCP vuruşuna izin verir.

Mesaj şu token'ları kullanıyorsa: `{attacker_i}`, `{attacker_name}`, `{a_pos}`, `{a_room}`, `{a_zone}`, `{a_role}`, `{s_pos}`, `{s_room}`, `{s_zone}`, `{a_item}`, `{damage}`.

## Item Spawner

`ItemType` (`None` kapatır), `Count`, `LocalPosition`, `AutoStart`, `SpawnDelay` (0, 0.5 sn pay bırakır), `RespawnTime` (0, eşyalar alınca yeniden doğmaz).

## Damage Trigger

`DamageAmount`, `DamageInterval`, `MinimumHealth` (0 öldürmeye izin verir), `AutoStart`, `TriggerMode` (giriş, içerde kalma, çıkış veya karışık), `KillInstant`, `AffectHumans`, `AffectScps`. Objede oyuncunun değebileceği collider olmalı.

## Player Count Trigger

`TriggerThreshold`, `TriggerMode` (eşiğe varış veya düşüş), `Cooldown`, `AutoStart`, artı ortak aksiyon listeleri. Lua olayları: `ThresholdReached`, `ThresholdDropped`.

## Loop Speaker

`AudioName`, `LabAPI\audio` içindeki dosyadır. `Volume`, `IsSpatial`, `MinDistance`, `MaxDistance`, `LocalPosition`, `AutoStart`. AudioPlayerApi isteyen bileşen budur.

## Custom Door

Schematic içindeki animasyonlu kapı. LCZ/HCZ kapısı değildir. Buradaki `DoorType`, animator'ın için `LCZ`, `HCZ` veya `EZ` kategorisidir. `OpenAnimation`, `CloseAnimation`, `LockAnimation` varsayılanları `DoorOpen`, `DoorClose`, `DoorLock`. Animator objesi `Animator` alanına hiyerarşi yolu olarak yazılır. Gerçek tesis kapısı için Map Door kullan.

## Custom Texture

Bir schematic'ten yüzlere doku basar. `SchematicName`, `Mode`, `StretchX`, `StretchY`, `Faces`, `ZFightOffset` (varsayılan 0.005), `MaxDuplicates` (varsayılan 256), `ManualSize`, `FlipNormal`, `PivotOffset`. Script'li dosya `FCustomTextures`.

## Spin Motor, Move Motor, Light

| | Alanlar |
|---|---|
| Spin Motor | `AngularSpeed` (saniyede derece, `x`,`y`,`z`, varsayılan y = 45), `UseWorldSpace` |
| Move Motor | `LocalOffset`, `Duration` (varsayılan 2), `StartDelay`, `PingPong`, `Loop`, `AutoStart` |
| Light Controller | Bir ışığın üzerinde. `ApplyOnStart`, `Enabled`, `OverrideColor`, `Color`, `OverrideIntensity`, `Intensity` (0 = kapalı), `OverrideRange`, `Range`, `Pulse`, `PulseSpeed`, `PulseMin`, `PulseMax` |

Lua: `api.rotator_speed`, `api.mover_restart`, `api.light_set`.

## Timer ve Counter

Timer Relay: `Delay`, `Interval` (varsayılan 1), `Repeat`, `RepeatCount` (Repeat açıkken 0 sürer gider), `AutoStart`, sonra tek hedef: `FunctionName`, ya da `GroovyNoiseId`, ya da `GroovyNoiseGroup`.

Counter: `StartValue`, `TargetValue` (varsayılan 1), `Step` (varsayılan 1), `ResetOnReach`, ve değer hedefe değince aynı üç hedef. Lua `api.counter_add` ile ekler.

## Schematic Anchor

Derlenmiş başka bir schematic doğurur. `SchematicName`, `SpawnOnStart`, `Scale`. Derinlik 4 ile sınırlı. Bir schematic kendi anchor'ından kendini doğurmaz. `api.spawn_schematic("Ad")` o adı çağıranın transformunda doğurur, ya da adlı anchor'dan ister.

## Function Executor

Schematic köküne koy. Tek dosya, `{Ad}-Functions.json`. Public fonksiyonlar `FunctionToCall`'dan, Timer/Counter'dan veya `api.call_function("Ad")` ile çağrılır.

Düğümler, dizdiğin sırayla:

`If`, `ElseIf`, `Else`, `While`, `For`, `ForEach`, `SetVariable`, `Return`, `Wait`, `CallFunction`, `CallGroovyNoise`, `PlayAnimation`, `SendMessage`, `SendCommand`, `DropItems`, `Explode`, `GiveEffect`, `PlayAudio`, `FWarhead`, `ChangePlayerValue`, `PlayerAction`, `ChangeEntityValue`.

`Wait` o fonksiyonun gerisini bekletir. `MaxFunctionWaitSeconds` ile sınırlıdır. `Wait` gören `While` / `For` / `ForEach` dönüp durmaz, süre dolunca devam eder. İç içe `If` artı `Wait`, aynı süreden sonra hem ebeveyni hem çocuğu sürdürür. Sıra önemliyse wait'i ebeveynde tut.

`F` bileşenlerinin ve fonksiyonların içindeki değerler (`Integer`, `Float`, oyuncu canı, rol kontrolü, `TrueForAll` ve gerisi) Script Value tipleridir. `$type` adları `AdvancedMERTools` assembly adında kalır; eski dosyalar bu sayede açılır.

## Interactable Teleporter

`TargetMapName` MER harita adı, `TeleporterIndex` o haritadaki 1'den başlayan ışınlanma index'i. Derleme, dışa aktarma köküne `{TargetMapName}-ITeleporters.json` yazar. Onu MER'in Maps klasörüne kopyala. Sunucu, o harita yüklenirken okur. `InvokeType` çarpışma mı, diğer ışınlanma kancası mı seçer. Aksiyon listeleri interactable ile aynı demetlerdir.

## Custom Collider, Groovy Noise, toy

Custom Collider, schematic ile giden oynanış collider'ıdır (`Colliders.json`). Bir şey değince diğer modüllerdeki aksiyon demetlerini kullanır.

Groovy Noise, adlı, yeniden kullanılabilir aksiyon demetidir. Diğer bileşenler onu id (`GroovieNoiseId`) veya `Script Group` ile çağırır. `GroovyNoiseFireOnEnable` (varsayılan açık) bir şey Groovy Noise'un `Active` alanını açınca onu bir kez çalıştırır. Eski çok kademeli kapılar buna bağlıdır.

Custom Interactable Toy, primitive toy'a yönelik interactable'dır: `IncludeRootPrimitive`, `IncludeChildPrimitives`, `ToyScaleMultiplier` (varsayılan 1.3), artı aynı aksiyon ve deny listeleri. `EnableIoToys`'un, listelediğin tuşlar için devreye aldığı şey budur.
