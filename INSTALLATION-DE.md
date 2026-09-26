# RetakesAllocator 2.4.2-jumper.1 – Update für euren Linux-Server

Dies ist eine Reparatur von Yoni Lerners bestehendem **cs2-retakes-allocator v2.4.2**, kein anderer Allocator. Ziel: **CounterStrikeSharp 1.0.375**, **B3none Retakes 3.0.2**, CS2 nach dem Source-2-Update **1.41.8.2**.

## Hochladen

1. Den CS2-Docker-Container stoppen.
2. Den bisherigen RetakesAllocator-Ordner sichern. Auch die bisherige SQLite-Datei **data.db** sichern; deren Ort richtet sich nach eurer bestehenden DatabaseConnectionString und dem Arbeitsverzeichnis des Servers. Bei MySQL die bisherige Datenbank behalten.
3. Das neue ZIP in **game/csgo/** entpacken bzw. dessen **addons/**-Ordner dorthin hochladen. Der Zielordner ist:
   **game/csgo/addons/counterstrikesharp/plugins/RetakesAllocator/**
4. Bestehende **config/config.json**, Datenbanken und eigene Übersetzungen behalten. Das Paket enthält keine Konfiguration und keine Datenbank. Den neuen Allocator wieder aktivieren; nur eine Kopie der DLL laden.
5. In **addons/counterstrikesharp/configs/plugins/RetakesPlugin/RetakesPlugin.json** unter **GameSettings** wieder setzen:
   ~~~json
   "EnableFallbackAllocation": false
   ~~~
   Während RetakesAllocator aktiv ist, muss Retakes' Fallback aus sein, sonst verteilen beide Plugins Waffen.
6. Container vollständig neu starten. Nicht nur die DLL im laufenden Prozess ersetzen. CounterStrikeSharp muss die vollständige Version 375 einschließlich ihres GameData-Ordners enthalten; nur die API-DLL zu aktualisieren reicht nicht.
7. Im Log muss **Connected to Retakes AllocateEvent** erscheinen. Bei aktiviertem Buy-Hook erscheint zusätzlich **CanAcquire hook attached using CounterStrikeSharp gamedata**. Kann dessen Signatur nicht aufgelöst werden, wird nur der optionale Buy-Hook übersprungen; !guns und reguläre Rundenvergabe bleiben verfügbar.

Die DLL verwendet .NET 10, wie CounterStrikeSharp 375. Die notwendigen SQLite- und anderen Plugin-Abhängigkeiten sowie die native Linux-x64-SQLite-Bibliothek sind im Paket enthalten. CounterStrikeSharp.API.dll und RetakesPluginShared.dll stammen aus eurer vorhandenen Installation und werden nicht überschrieben.

## !guns und Speicherung

**!guns** bzw. **/guns** öffnet das vorhandene Chatmenü des Allocators. Dort werden T-Primary, T-Secondary, CT-Primary und CT-Secondary ausgewählt. Die weiteren bestehenden Menüs/Befehle bleiben erhalten.

Die Auswahl wird mit dem vorhandenen Datenbankschema gespeichert. Datenbankänderungen werden nun abgewartet, bevor die Auswahl als gespeichert behandelt wird. Die zusätzliche Einstellung **ApplySelectionsOnNextSpawnOnly** ist standardmäßig **true**, auch beim Einlesen einer alten Konfiguration, in der sie noch nicht vorkommt:

~~~json
"ApplySelectionsOnNextSpawnOnly": true
~~~

Damit vergibt !gun während einer laufenden Runde keine neue Waffe. Änderungen gelten bei der folgenden regulären Retakes-Vergabe. Buy/Rebuy/Autobuy werden außerhalb des Warmups blockiert, damit sie diese Regel nicht umgehen. Mit false ist der frühere sofortige Wechselmodus wieder möglich.

Die ursprünglichen **Pistol-/HalfBuy-/FullBuy-Rundentypen** bleiben erhalten. Eine FullBuy-Primary wird in FullBuy-Runden benutzt; HalfBuy und Pistol verwenden wie bisher die jeweiligen Präferenzen. Wenn ihr **in jeder Runde die ausgewählte FullBuy-Primary und Secondary** erhalten wollt, setzt in der bestehenden Allocator-Konfiguration:

~~~json
"RoundTypeSelection": "Random",
"RoundTypePercentages": {
  "Pistol": 0,
  "HalfBuy": 0,
  "FullBuy": 100
}
~~~

Die normale Spawn-Vergabe verwendet das offizielle **AllocateEvent** von Retakes 3.0.2. Ausgerüstet werden nur verbundene, lebende T-/CT-Spieler mit gültigem Pawn. Spectators werden nicht ausgerüstet. Doppelte AllocateEvents innerhalb derselben Runde werden übersprungen.

## Was die Absturzreparatur ändert

- Die alten Plugin-Signaturen unterscheiden sich von CounterStrikeSharp 375. Plugin-lokale GameData und automatische Downloads vom archivierten Upstream werden nicht mehr eingelesen.
- CanAcquire wird mit der ABI aus CounterStrikeSharp 375 aufgelöst: ItemServices, EconItemView, AcquireMethod, IntPtr, AcquireResult.
- Vor dem Hooken wird sowohl CanAcquire als auch GetCSWeaponDataFromKey auf einen aufgelösten Handle geprüft. Ein Null-Handle gelangt niemals zu HookFunction.
- Der Callback wird explizit über eine permanente CounterStrikeSharp FunctionReference gehalten. Beim Entladen wird zuerst der native Hook entfernt und erst danach die Callback-Referenz freigegeben.
- Schlägt das Unhook fehl, bleibt die Callback-Referenz absichtlich erhalten und der Callback wird inaktiv. Das Log fordert dann einen Serverneustart. Eine möglicherweise noch native referenzierte Delegate wird nicht freigegeben.
- Kein Hintergrund-Download kann einen Hook nach Unload erneut registrieren.
- Der ungeprüfte GiveNamedItem2-Pfad wurde entfernt. Waffen werden über CounterStrikeSharps GiveNamedItem vergeben. Besondere Skin-Übernahme bei teamfremden Waffen wird dadurch nicht garantiert.
- Waffen und Defuse-Kits werden innerhalb des offiziellen AllocateEvent vergeben, statt über spätere Spawn-Timer.
- Menü-Timer und aktive Menüs werden bei Mapwechsel/Unload aufgeräumt.
- PlayerConnectedState ist an die API 375 angepasst.
- Das Datenbankschema und die bestehenden EF-Migrationen wurden nicht verändert.

Die alten Konfigurationsschlüssel **AutoUpdateSignatures** und **CapabilityWeaponPaints** bleiben einlesbar, sind in diesem Fork aber ohne Wirkung. Auch vorhandene Dateien unter **RetakesAllocator/gamedata/** werden ignoriert; sie müssen für dieses Update nicht gelöscht werden.

**EnableCanAcquireHook** bleibt eine optionale Einstellung. Bei false wird kein eigener Native-Hook eingerichtet; das Menü und die reguläre Rundenvergabe funktionieren weiterhin. Die Änderung sollte beim Serverstart erfolgen. Ein Wechsel von false auf true per Config-Reload aktiviert keinen neuen Hook im laufenden Prozess.

## Nachweis und Grenzen

Release-Build gegen CounterStrikeSharp.API **1.0.375** und RetakesPluginShared **2.0.0**: erfolgreich, keine Build-Warnungen oder Fehler.

**50 Tests**: ursprüngliche Allocator-Tests plus neue Tests für Null-Handles, genau einmaliges Attach/Detach, teilweise fehlgeschlagene Registrierung, fehlgeschlagenes Unhook ohne Callback-Freigabe sowie gespeicherte CT/T-Auswahl nach erneutem Datenbanköffnen.

Die Tests ersetzen keinen CS2-Prozess: Die tatsächliche Hook-Installation, das Menü im Spiel und das Verhalten bei Server-/Map-Neustarts müssen noch auf eurem Dedicated Server bestätigt werden. Der Fehler „Invalid function pointer“ ist durch den ungeprüften Null-Handle-Pfad im alten Code erklärt. Der genaue Auslöser des späteren GC-Delegate-Absturzes lässt sich ohne vollständigen Crash-Dump nicht endgültig bestimmen; die problematischen Lebenszykluspfade werden hier abgesichert.

Kurzer Test nach dem Neustart:
1. Server startet ohne Native-Hook-/Delegate-Fehler.
2. !guns öffnen und CT/T-Waffen speichern.
3. Laufende Runde: Waffen bleiben unverändert; nächste FullBuy-Runde: gespeicherte Auswahl.
4. Spectator bleibt ohne Ausrüstung.
5. Reconnect und Container-Neustart: Auswahl bleibt erhalten.
6. Mapwechsel und Allocator-Unload/Reload ohne verwaiste Callbacks.

Der konfigurierbare Bomb-Plant-Timer (Standard 10 Sekunden) ist **noch nicht Bestandteil dieses Updates**.

## Quellen

- [Allocator v2.4.2](https://github.com/yonilerner/cs2-retakes-allocator/tree/v2.4.2)
- [CounterStrikeSharp 375](https://github.com/roflmuffin/CounterStrikeSharp/releases/tag/v1.0.375)
- [CSSharp-Anpassungen an CS2 1.41.8.2](https://github.com/roflmuffin/CounterStrikeSharp/pull/1433)
- [Retakes 3.0.2: Ausrüstungsereignis](https://github.com/B3none/cs2-retakes/blob/3.0.2/RetakesPlugin/Events/RoundEventHandlers.cs)
- [Valve: CS2-Update vom 22. September 2026](https://store.steampowered.com/news/app/730/view/1844751498216924)
