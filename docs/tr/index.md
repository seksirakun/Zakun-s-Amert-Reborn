# ZAMERT kılavuzu

Sürüm 2.1.0. Harita yapanlar için kullanım kılavuzu. Aşağıdaki alan adları JSON'a yazılan adlardır.

[Kurulum](install.md) · [Unity](unity.md) · [Lua](lua.md) · [Bileşenler](components.md) · [Kapılar ve prefab](doors.md) · [Sunucu](server.md)

[Türkçe](index.md) · [English](../en/index.md) · [Русский](../ru/index.md) · [中文](../zh/index.md) · [한국어](../ko/index.md)

ZAMERT, ProjectMER schematic'ine davranış ekler: tuş, oyun kapısı, network prefab, can objesi, hoparlör, zamanlayıcı ve Lua. Mesh MER'in `{Ad}.json` dosyasında kalır. Davranış onun yanındaki dosyalardadır.

![Derleme yolu](../assets/pipeline.svg)

Mantığı iki türlü yazarsın:

- **Lua Module**, boş objeye veya meshe. Gerçek Lua: `if`, `for`, `while`, `api.wait`.
- **Function Executor**, schematic kökünde. `If`, `While`, `Wait`, `SendMessage` gibi düğümler, `{Ad}-Functions.json`. Eski haritalar bunu kullanır. Çalışmaya devam eder.

Interactable Object, Health Object ve diğer eski bileşenler hâlâ aynı dosyalara derlenir. Yeni bir bileşen yeni bir son ek ekler. Eski dosyanın içini değiştirmez.

Eklenti sunucuda yoksa [Kurulum](install.md). Sunucu ayaktaysa ve sadece script yazacaksan [Lua](lua.md).
