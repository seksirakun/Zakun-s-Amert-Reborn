# Lua

[Kılavuz](index.md) · [Kurulum](install.md) · [Unity](unity.md) · [Bileşenler](components.md) · [Kapılar ve prefab](doors.md) · [Sunucu](server.md)

Lua Module bir bileşendir. Script, koyduğun objede çalışır. Aynı dosyayı on obje paylaşsa da her objenin kendi çalışması vardır. Hangisi olduğunu `object.name` söyler.

![on_interact hangi objeye ait](../assets/lua.svg)

## Inspector

| Alan | Ne yazılır |
|---|---|
| `Inline Script` | Kaynak. Doluysa bu kullanılır. |
| `Script Path` | Inline Script boşsa, schematic'in yanındaki `.lua` dosyası. |
| `Input Key Code` | Unity KeyCode. `0` = tuş yok. `101` = E. |
| `Interaction Max Range` | Metre. `0`, 5 sayılır. |
| `Listen Enter / Stay / Exit` | Bu objenin trigger'ına değen oyuncu için `on_enter`, `on_stay`, `on_exit`. |
| `Tick Interval` | `on_tick` arası saniye. `0.1` altı tick kapalıdır. |
| `Script Group` | Senin etiketin. `object.scriptGroup` diye okunur. |

`SchematicManager → Compile Lua Modules` veya Compile All. Dosya `{Ad}-LuaModules.json`.

## Ne zaman çalışır

Kaynak, schematic doğunca bir kez çalışır. Bu çalışma fonksiyonları tanımlar ve en üstteki serbest kodu da yürütür.

| Fonksiyon | Ne zaman |
|---|---|
| en üstteki kod | schematic doğunca |
| `on_interact` | oyuncu bu modüle bakıp onun tuşuna basınca |
| `on_enter` | oyuncu trigger'a girince |
| `on_stay` | oyuncu içindeyken, en fazla 0.25 sn'de bir |
| `on_exit` | oyuncu trigger'dan çıkınca |
| `on_tick` | Tick Interval en az 0.1 ise, her aralıkta |

Fonksiyonlar global olmalı. `local function on_interact` olay tarafında görünmez.

```lua
function on_interact()
  api.hint(player.name .. " used " .. object.name, 2)
end
```

Bekleyen döngünün üstüne fonksiyonları yaz. Yorumlayıcı o satıra gelmeden fonksiyon yoktur. İlk turda `api.wait` gören bir döngü, altına yazılmış fonksiyona hiç ulaşmaz.

`player`, doğuşta ve `on_tick` içinde yoktur; çağıran olayın oyuncusu yoksa nil kalır. `on_interact`, `on_enter`, `on_stay`, `on_exit` içinde oku.

## `object` ve `player`

`object` (bu modül):

| Alan | |
|---|---|
| `name` | Hiyerarşideki ad. |
| `scriptGroup` | Yazdığın Script Group. |
| `code` | Derlemedeki instance id. Yeniden derlersen değişir. |
| `objectId` | Index yolu. |
| `type` | `"LuaModule"`. |
| `active` | Active bayrağı. |
| `position` | Tablo kurulduğu andaki konum. `x`, `y`, `z`. |

`player`:

| Alan | |
|---|---|
| `id`, `name`, `userId` | Oyuncu id, rumuz, user id. |
| `role`, `team` | Rol adı ve takım adı. `== "ClassD"`, `== "ChaosInsurgency"` diye bak. |
| `isScp`, `isHuman`, `isAlive` | Boolean. |
| `health` | Can. |
| `position` | `x`, `y`, `z`. |
| `room`, `zone` | Adlar. |
| `currentItem` | Eşya tipi adı, ya da boş. |

`schematic.name` MER schematic adıdır. `schematic.directoryPath` JSON'un yüklendiği klasördür.

Aynı script, iki düğme:

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

`api.wait(saniye)` bu scripti duraklatır. Sunucu çalışmaya devam eder. `api.wait(0)` bir kare bekler.

```lua
for i = 1, 5 do
  api.broadcast("Count " .. i, 2)
  api.wait(1)
end
```

`api.wait` olmayan `while true` durdurulur. Varsayılan: dilim başına 2500 Lua komutu, 8 dilim, sonra script ölür ve konsol çok uzun sürdüğünü yazar. Config: `LuaAutoYieldInstructions`, `LuaMaxBusySlices`. Bir modülde 8 coroutine aynı anda durabilir. Bir kare en fazla `LuaMaxProgramsPerFrame` script devam ettirir (varsayılan 8). `api.wait`, `MaxFunctionWaitSeconds` ile sınırlıdır (varsayılan 600).

`api.wait`, Lua Module kaynak coroutine olduğu için çalışır. Diğer bileşenlerdeki Lua event parçaları (`LuaEvents`) coroutine değildir. Oralarda `api.delay` kullan.

## Oyuncular

```lua
api.broadcast("Hello", 5)                 -- metin, saniye, herkese
api.hint("Look up", 2)                     -- şu anki oyuncu
api.hint_player(player, "Look up", 2)      -- oyuncu tablosu, id veya rumuz
api.heal_player(player, 25)                -- ya da api.heal_player(25)
api.damage_player(player, 10, "fence")
api.kill_player(player, "pit")
api.teleport_player(player, 10, 2, 4)      -- ya da {x=,y=,z=} tablosu
api.give_item(player, "KeycardGuard")      -- ItemType adı
api.set_role(player, "ClassD", 0)          -- üçüncü argüman 1 ise spawn noktasına da gider
api.players()                              -- dizi, Lua index 1
api.alive_players()
api.humans()
api.scps()
api.player_count("ClassD")                 -- rol veya takım parçası, boşsa herkes
api.find_player("Nick")                    -- id, rumuz, user id veya oyuncu tablosu
api.current_player()
```

`give_item` tek argümanla eşyayı şu anki oyuncuya verir. `set_role` rol enum adını kullanır (`ClassD`, `Scientist`, `NtfSergeant`, `Scp173`, …). Yanlış ad false döner, oyuncuyu değiştirmez.

## Kapı ve prefab

```lua
api.spawn_door("ez")              -- bu objenin yerinde. ayrıca lcz, hcz, bulk, gate
api.spawn_door("gate", 4, 1, 8)   -- dünya konumu
api.door_open(1)                  -- 6 m içindeki en yakın kapı. 0 kapatır
api.door_open(1, 12)              -- aynı, 12 m
api.door_lock(1)
api.spawn_prefab("Work Station")
api.spawn_prefab("Work Station", 10, 2, 4)
api.spawn_prefab("Work Station", {x = 10, y = 2, z = 4})
api.spawn_prefab("Work Station", 10, 2, 4, {x = 0, y = 90, z = 0}, {x = 1, y = 1, z = 1})
api.list_prefabs("door")          -- en fazla 40 isim
api.prefab_count()
```

`spawn_door` oyun kapısı prefablarını kullanır (kırılır kapı, bulk, gate) ve schematic ile birlikte silinirler. `spawn_prefab`, bu sunucudaki `NetworkClient.prefabs` adını doğurur. Konum üç sayı ya da tek `{x,y,z}` tablosudur. Dönüş ve ölçek beşinci ve altıncı argümandır, ikisi de tablo: ad, x, y, z, dönüş, ölçek. Yazımı `zamert prefabs` çıktısından al. Sonradan açıp kilitleyeceğin kapı `spawn_door` olsun, ham prefab olmasın.

## Lua'dan diğer modüller

Hedef bir code, bir obje adı, ya da çağrı izin veriyorsa boş bırakınca "bu obje".

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
api.call_function("OpenHall")          -- bu schematic'teki Function Executor
api.trigger(object.code, "interacted") -- o objedeki LuaEvents
api.trigger_group("doors", "custom")

api.counter_add("button", 1)           -- code veya ad ile Counter
api.timer_start("clock")
api.timer_stop("clock")
api.light_set("lamp", 2)               -- şiddet, 0 kapatır
api.rotator_speed("fan", 0, 90, 0)
api.mover_restart("platform")
api.spawn_schematic("SubRoom")         -- isimle Schematic Anchor, ya da bu ismi burada doğur

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

## Değişkenler

Üç kapsam. Anahtar string. Değer sayı, string veya boolean.

```lua
api.set_self_var("uses", 0)             -- bu obje, yok olana kadar
api.inc_self_var("uses", 1)
api.get_self_var("uses")

api.set_schematic_var("power", 1)       -- bu schematic örneği
api.get_schematic_var("power")
api.inc_schematic_var("power", -1)

api.set_round_var("opened", true)       -- sonraki harita üretilirken silinir
api.get_round_var("opened")
```

## Zaman, ihtimal, dosya, komut

```lua
api.log("opened")
api.warn("missing clip")
api.error("bad state")
api.time()
api.realtime()
api.chance(0.25)                        -- çağrıların %25'inde true
api.random_int(1, 6)
api.random_float(0, 1)
api.vec(1, 2, 3)                        -- {x,y,z}
api.distance(a, b)
api.add_vec(a, b)

api.command("cassie 1 Attention")       -- LuaAllowServerCommands açık olmalı
api.include("lib.lua")                  -- bu scriptin içinde çalışır
api.run_file("once.lua")                -- ayrı script, local'ları paylaşmaz

api.delay(2, function()
  api.broadcast("later", 3)
end)
api.repeat_every(1, 5, function(ctx, tick)
  api.broadcast("tick " .. tick, 1)
end)
```

`repeat_every` en az 0.05 sn bekler. Sayı `LuaMaxRepeatCount` ile kesilir (varsayılan 100). `LuaAllowDelayedCallbacks` kapalıysa `delay` ve `repeat_every` hiçbir şey yapmaz.

`include` ve `run_file` önce schematic klasörüne, sonra eklenti DLL'inin yanına bakar. Tam yol verirsen oraya bakar.

## Diğer bileşenlerdeki Lua Events

Her ZAMERT bileşeninde `Lua Events` listesi vardır. Bu, eski kanca: Interact, Death, Enter üzerine kısa script. Lua Module değildir. Orada `api.wait` hata verir; `api.delay` kullan.

| `Event Type` | Ne zaman |
|---|---|
| `Spawned`, `Destroyed` | objenin ömrü |
| `Interacted`, `Denied` | tuş kabul, ya da kart reddi |
| `Searching`, `Picked` | pickup |
| `Damaged`, `Died` | health object |
| `Entered`, `Stayed`, `Exited` | collider |
| `ThresholdReached`, `ThresholdDropped` | oyuncu sayısı eşiği |
| `Tick` | bileşen tick atıyorsa |
| `Custom` | `Custom Event Name`, bir de `api.trigger` |

Satırda priority, chance (`0`–`1`), cooldown, bir kez çalış, iptal edilmişken de çalış, başarıda dur vardır. Inline ya da dosya, yol kuralları aynı.

Event scriptinin içinde globaller aynıdır (`player`, `object`, `api`). `ctx.cancelled`, `ctx.damage`, `ctx.currentHealth` olaya bağlıdır. `api.cancel()` / `api.uncancel()` context'i işaretler. `api.stop()` bu olayın sonraki binding'lerini keser. Pickup olayında `api.remove_pickup()` veya `api.keep_pickup()`.

`LuaEnabled` kapalıysa hem Lua Module hem bu binding'ler susar.
