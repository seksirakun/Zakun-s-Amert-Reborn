# Unity

[Kılavuz](index.md) · [Kurulum](install.md) · [Lua](lua.md) · [Bileşenler](components.md) · [Kapılar ve prefab](doors.md) · [Sunucu](server.md)

## Sıra

Schematic kökünde MER'in `Schematic` bileşeni durur. GameObject adı klasör adıdır. Kökü derlemeden önce yeniden adlandır.

1. Meshi MER editöründe kur.
2. ProjectMER ile derle. Dışa aktarma yolu `SchematicManager.Config.ExportPath`. O yol yoksa ZAMERT `Masaüstü\MapEditorReborn_CompiledSchematics` klasörüne düşer.
3. Bu schematic'in çocuklarına ZAMERT bileşenlerini ekle.
4. `SchematicManager → Compile All ZAMERT`.

4. adım, 2. adımın klasörüne yazar. Klasör yoksa Console `could not find the compiled-schematics directory` der ve dosya çıkmaz.

Tek tek menüler (`Compile Lua Modules`, `Compile Map Doors`, …) bir son ek yazar. `Compile All ZAMERT` hepsini yazar. `Decompile All` dosyaları hiyerarşiye geri okur. Eşleşme önce `ObjectNamePath`, sonra `ObjectId`.

![Bir schematic klasöründeki dosyalar](../assets/folder.svg)

## Ne yazılır

Düz bileşenler (Lua Module, Map Door, Prefab Anchor, motorlar, ışıklar, zamanlayıcılar, sayaçlar, schematic anchor, clutter, kameralar, item spawner, damage trigger, loop speaker, custom door) her zaman `{Ad}-{Sonek}.json` yazar.

**Use Script Value** olan bileşenlerin bir de `F` dosyası vardır: `Objects.json` ve `FObjects.json`, `HealthObjects.json` ve `FHealthObjects.json`. Aynısı pickup, collider, toy, player-count trigger ve custom texture için de geçer. Script'li hali dışa aktarmak için **Use Script Value** aç. Kapalıysa düz liste yazılır. İkisini de içeren eski harita ikisini de korur; yükleyici ikisini de okur.

Function Executor ayrıdır. Schematic kökündedir ve `{Ad}-Functions.json` yazar.

Interactable Teleporter, schematic klasörünün içine değil, dışa aktarma köküne `{TargetMapName}-ITeleporters.json` yazar. O dosyadaki `ObjectId`, ışınlanmanın 1'den başlayan index'idir. Dosyayı MER'in Maps klasörüne, haritanın yanına kopyala.

## Kimlik alanları

`ObjectId` ve `ObjectNamePath` elle yazılmaz. Derleme basar.

| Alan | Anlamı |
|---|---|
| `ObjectNamePath` | Schematic kökünden aşağı obje adları, `/` ile. Adın içindeki slash `\/` olarak durur. Sunucunun önce baktığı eşleşme budur. |
| `ObjectId` | Kardeş index'leri. Önce objenin kendi index'i, sonra ebeveyn, boşlukla ayrılmış. İsim yolu tutmazsa buna bakılır. |
| `Code` | Validate/derleme anındaki Unity instance id. Domain reload sonrası yeniden derlersen değişir. Tek bir derlemenin içinde `api.find_by_code` için kullanılır. Kalıcı kimlik değildir. |
| `Script Group` | Bileşene senin yazdığın metin. Kalıcıdır. Lua'da `object.scriptGroup`, ayrıca `api.find_group`. |
| `Active` | Açık başlar. Başka bileşenler ve Lua kapatabilir. |

Objeyi yeniden adlandırmak veya kardeş sırasını değiştirmek yolu değiştirir. Ondan sonra tekrar derle. Sunucu çözemediği satırı atlar ve tutmayan `ObjectId` / `ObjectNamePath` değerini loglar.

## Tuşlar

`InputKeyCode`, Unity `KeyCode` sayısıdır. `E` 101, `F` 102, `None` 0. Lua Module'de 0, bu modülün tuşu olmadığı anlamına gelir.

Sunucu her farklı tuş için Server-Specific Settings'e bir satır ekler: `ZAMERT - Interactable Object - E`. Oyuncu bunu bir kez bind eder. Önerilen tuş senin seçtiğindir; oyuncu değiştirebilir. Config'de `DisableSSS` bu kaydı kapatır. O zaman hiçbir ZAMERT tuşu çalışmaz.

Klavyeden E'ye basmak, o satır yokken ve oyuncu bind etmemişken bir şey yapmaz. İzleyici tetiklemez.

## Collider

Tuş etkileşimi kameradan ray atar. Objede ya da çocuğunda collider olması gerekir. Trigger collider da sayılır. İlk katı çarpışma arkasını keser.

Lua Module'ün yedeği de vardır: ray hiçbir şeye değmezse, o tuşa kayıtlı, `InteractionMaxRange` içinde (varsayılan 5) ve bakışın 35 derece içinde kalan modül yine çalışabilir.

`ListenEnter` / `ListenStay` / `ListenExit` aynı objede trigger collider ister. Collider trigger ise ve rigidbody yoksa, modül kinematic rigidbody ekler; Unity ancak o zaman trigger mesajı yollar. `on_stay` bir collider için saniyede en fazla dört kez çalışır.

## Haritanın yanındaki script dosyası

`ScriptPath`, schematic klasörüne göre bir dosya adı, ya da tam yoldur. `Inline Script` doluysa o kazanır. Örnek: schematic klasöründe `buttons.lua` durur, modülün Script Path alanı `buttons.lua` olur.
