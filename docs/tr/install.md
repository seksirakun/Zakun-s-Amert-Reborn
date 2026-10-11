# Kurulum

[Kılavuz](index.md) · [Unity](unity.md) · [Lua](lua.md) · [Bileşenler](components.md) · [Kapılar ve prefab](doors.md) · [Sunucu](server.md)

## Önceden kurulu olması gerekenler

- SCP: Secret Laboratory dedicated server
- LabAPI 1.1.6 veya daha yeni
- ProjectMER, kendi eklentisi olarak yüklü

ZAMERT'in yükleme önceliği `Lowest`. MER kalktıktan sonra bağlanır.

Loop Speaker, etkileşim sesi ve `AudioModules` için sunucuda AudioPlayerApi gerekir. O yoksa bu bileşenler hata yazar, eklentinin geri kalanı yine yüklenir.

## Derleme

Bu klasörden:

```text
dotnet build ZAMERT\ZAMERT.csproj -c Release
```

Çıktı: `ZAMERT\bin\Release\`.

| Dosya | Nereye |
|---|---|
| `ZAMERT.dll` | `%AppData%\SCP Secret Laboratory\LabAPI\plugins\global\` |
| `MoonSharp.Interpreter.dll`, `0Harmony.dll`, `Newtonsoft.Json.dll` | Sunucu logu bunları bulamadı diyorsa `%AppData%\SCP Secret Laboratory\LabAPI\dependencies\` |

Sunucu port klasörüne bölünmüşse (`plugins\7777\`) oraya da konur. Bir kopya yeter.

`ZAMERT\libs\ProjectMER.dll` yerinde kalsın. O dosya sadece derleme referansı. Çalışan sunucu, senin kurduğun ProjectMER eklentisini kullanır.

## İlk açılış

Sunucuyu bir kez başlat. LabAPI, `LabAPI\configs\` altına ZAMERT config'ini yazar. Dosya adı LabAPI sürümüne göre değişir; eklenti adı `ZAMERT` olan kayıttır.

Ses klasörü yoksa açılır:

`%AppData%\SCP Secret Laboratory\LabAPI\audio\`

Klipleri oraya koy. Loop Speaker'daki `AudioName`, bu klasörün içindeki dosya adıdır.

## Yüklendiğini görmek

Remote Admin:

```text
zamert
```

Cevapta sürüm vardır. Raunt schematic doğurduysa, ayakta olan ZAMERT objelerinin sayısı da vardır.

```text
zamert prefabs
zamert prefabs door
```

`ZAMERT.dll` dosyasının yanına `ZAMERT-prefabs.txt` yazar ve ilk isimleri basar. Bu liste, bu sunucu sürümünde geçerli `PrefabName` / `api.spawn_prefab` adlarıdır. Oyun güncellemesi isimleri değiştirir. Dosyayı, haritayı çalıştırdığın sunucudan al.

```text
zamert audio
```

Önbellekteki klipleri siler. Sonraki çalma dosyayı yeniden okur.

## Unity araçları

Sunucu DLL'i editör scriptlerini içermez. Repodaki `Tools` klasörünü harita projesine kopyala:

```text
HaritaProjen\Assets\ZAMERT\
```

Unity 2021.3.17f1. Scriptler derlenince menü çubuğunda `SchematicManager` görünür. Menü yoksa scriptler `Assets` altında değildir, ya da Console'da editör sürümünden gelen derleme hatası vardır.

Sıradaki sayfa derleme sırası. ZAMERT JSON yazabilsin diye MER'in schematic'i önce dışa aktarması gerekir.
