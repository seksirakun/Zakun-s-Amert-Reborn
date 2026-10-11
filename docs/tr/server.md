# Sunucu

[Kılavuz](index.md) · [Kurulum](install.md) · [Unity](unity.md) · [Lua](lua.md) · [Bileşenler](components.md) · [Kapılar ve prefab](doors.md)

Config ilk açılışta oluşur. Aşağıdaki değerler 2.1.0 varsayılanlarıdır.

## Remote Admin

| Komut | |
|---|---|
| `zamert` veya `zm` | Sürüm, schematic sayısı, raunt değişkenleri, bileşene göre ayaktaki obje sayısı. |
| `zamert prefabs` | Network prefab listesini tazeler, DLL'in yanına `ZAMERT-prefabs.txt` yazar, ilk 12 adı basar. |
| `zamert prefabs door` | Aynı, filtreli. Filtre alt stringdir. |
| `zamert audio` | Önbellekteki ses kliplerini unutur. |

Prefab dosyası, ad kopyalayacağın dosyadır. Komutu her çalıştırışında, komutu çalıştıran sunucudan yeniden yazılır.

## Config

| Anahtar | Varsayılan | |
|---|---|---|
| `Debug` | false | Ek log. |
| `AudioFolderPath` | `LabAPI/audio` | Klip klasörü. Yoksa açılır. |
| `AudioMasterVolume` | 1 | Sunucu geneli çarpan. 0, ZAMERT sesini kısar. |
| `EnablePerClientAudioVolume` | true | Server-Specific Settings'e `ZAMERT - Audio Volume` kaydırıcısı ekler. Sadece tek seferlik sesler. |
| `EnableIoToys` | false | Tuşu `IoToysKeycodes` içinde olan interactable'lar toy olarak doğar. |
| `IoToysKeycodes` | 0, 101 | None ve E. |
| `IoToysNoRoot` | false | Kök interactable'da toy yapma. |
| `IoToysDebug` | false | Toy interactable'ın durduğu yerde görünen primitive. |
| `IoToysBothModes` | false | Hem toy hem settings tuşu. 200 ms, bir basış iki kez çalışmasın diye. |
| `InteractableObjectDetectionMode` | RaycastWithAssist | `RaycastOnly`, `RaycastWithAssist`, `ProximityOnly`. |
| `InteractableObjectAssistSphereRadius` | 0.35 | Assist yoklama boyu. |
| `InteractableObjectAssistAngleDegrees` | 12 | Assist nişan konisi. |
| `LuaEnabled` | true | Lua Module ve Lua Events ana anahtarı. |
| `LuaCacheFileScripts` | true | `ScriptPath` dosyalarını önbelleğe al, yazılma zamanı değişince yeniden yükle. |
| `LuaAllowServerCommands` | true | `api.command`. |
| `LuaAllowDelayedCallbacks` | true | `api.delay` ve `api.repeat_every`. |
| `LuaMaxRepeatCount` | 100 | `api.repeat_every` tavanı. |
| `LuaLogApiErrors` | true | Bozuk `api.*` çağrısını sessiz nil yerine logla. |
| `AutoRun` | false | MER kapı doğurma kancası. O kancayı bilmiyorsan kapalı bırak. |
| `GroovyNoiseFireOnEnable` | true | Groovy Noise açılınca bir kez çalışır. |
| `DisableSSS` | false | ZAMERT tuşlarını ve ses kaydırıcısını kaydetme. |
| `CustomSpawnPointEnable` | true | Özel spawn noktası desteği. |
| `MaxFunctionWaitSeconds` | 600 | Function Executor `Wait` ve `api.wait` tavanı. 0, tavan yok. |
| `LogSchematicSummary` | true | Schematic başına bir satır: kaç satır yüklendi, kaçı atlandı. |
| `LuaAutoYieldInstructions` | 2500 | Lua Module bir kare vermeden önce işleyeceği komut sayısı. |
| `LuaMaxBusySlices` | 8 | `api.wait` olmadan kaç dilimden sonra script durur. |
| `LuaMaxProgramsPerFrame` | 8 | Bir karede sürdürülen modül coroutine sayısı. |
| `Gates` | örnek kayıt | Harita adına göre dummy gate tanımları. |

`Gates` yer tutucu bir harita adıyla gelir. Değiştir veya bırak; bilinmeyen harita adı bir şey yapmaz.

## Yükleyici ne yapar

Schematic doğunca ZAMERT bildiği her kardeş dosyayı okur. Her satırda transformu şu sırayla çözer: `ObjectNamePath`, tam `ObjectId`, eski daha kısa index yürüyüşü, iki alan da boşsa schematic kökü. Gerisi atlanır ve sayılır. `LogSchematicSummary` toplamları basar.

Raunt değişkenleri (`api.set_round_var`) schematic doğduktan sonra değil, sonraki harita üretilmeye başlayınca silinir. Tuşlar harita üretildikten sonra yeniden kaydedilir. Yok olan objeler kayıtlardan düşer; kayıtlar, harita üretildi diye silinmez. Orada silmek, aynı olay sırasında doğan schematic'leri siliyordu.

## Ses

Dosyalar `AudioFolderPath` içindedir. Dosyayı değiştirdikten sonra `zamert audio` klip önbelleğini temizler. Oyuncu başı ses, settings kaydırıcısıdır, `AudioMasterVolume` ile çarpılır. Kaydırıcı döngüsel hoparlöre gitmez, sadece tek seferlik seslere gider.

## Script ne zaman ölür

`api.wait` çağırmayan Lua Module komut hakkını bitirir ve durur. Log satırı: `Lua on {obje} was stopped because it ran too long without api.wait()`. Döngünün içine `api.wait` koy. O modülde 8 coroutine zaten çalışıyorsa `already has 8 running scripts` çıkar.

Sözdizimi veya çalışma hatası `Lua module failed to start` ya da `Lua failed on {obje}` diye düşer, yanında Lua mesajı vardır. `LuaLogApiErrors`, `api.give_item` ve diğer yardımcılardaki hataları (bilinmeyen eşya adı, nil oyuncu) scriptin tamamını öldürmeden kapsar.

## Eski haritalar

Sonekler, `$type` assembly adı `AdvancedMERTools` ve yanlış yazılmış alanlar durur. 2.1.0'dan önce yapılmış bir haritayı yüklemek yeniden derleme istemez. Yeni bileşen istiyorsan o başka. Lua Module, düzelmiş Map Door, motor veya prefab anchor eklemek, o sonekleri derleyip yeni dosyaları eskilerin yanına koymak demektir.
