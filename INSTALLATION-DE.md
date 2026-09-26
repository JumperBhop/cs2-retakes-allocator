# Jumper RetakesAllocator 301.1 – Installation

Ziel: Linux x64 / Docker, CounterStrikeSharp **1.0.375 vollständig mit GameData**, B3none Retakes **3.0.2**. Der bestehende Allocator wurde weiterentwickelt; Plugin-Ordner und DLL heißen weiterhin **RetakesAllocator**. Kein anderes Menüpaket nötig.

## Update

1. CS2-Container stoppen. Den bisherigen Allocator-Ordner, seine config/config.json und die bestehende MySQL-Datenbank sichern.
2. ZIP nach **game/csgo/** entpacken: addons/counterstrikesharp/plugins/RetakesAllocator/. Nur eine Allocator-DLL aktiv halten.
3. Bestehende Konfiguration, DatabaseProvider, DatabaseConnectionString und Datenbank behalten. Das ZIP enthält weder Konfiguration noch Datenbank. Bestehende JSON-Dateien werden beim Laden nicht umgeschrieben. Flache Einstellungen und die Gruppen **Config**, **Weapons**, **AWP**, **Database** mit denselben Eigenschaftsnamen werden eingelesen. Gruppen zuerst, explizite Einstellungen auf der obersten Ebene zuletzt.
4. In RetakesPlugin.json unter **GameSettings** setzen: **EnableFallbackAllocation: false**. Der Allocator und die Retakes-Fallback-Verteilung dürfen nicht gleichzeitig aktiv sein.
5. Für manuelles Planten in RetakesPlugin.json unter **Bomb** setzen: **IsAutoPlantEnabled: false**. Bei aktiviertem Autoplant wird die Bombe durch Retakes gelegt und der Countdown startet nicht bzw. wird beendet.
6. Für normale Plant-Dauer **InstaplantPlugin deaktivieren**: dessen Plugin-Ordner außerhalb addons/counterstrikesharp/plugins/ verschieben. B3nones Instaplant hat keine Abschalt-Konfiguration und keinen konkurrierenden Countdown; es setzt beim Plant-Beginn ArmedTime auf 0. Bleibt es aktiv, ist instant erfolgreiches Planten innerhalb des Zeitfensters möglich. Andere Plugins mit Plant-Deadline oder eigenem Runden-Abbruch ebenfalls deaktivieren. Die normale Bombenzeit mp_c4timer wird nicht verändert.
7. Die neuen Schlüssel in der bestehenden **Config**-Gruppe ergänzen; bei einer flachen Konfiguration entsprechend auf oberster Ebene:

~~~json
{
  "PlantTimerEnabled": true,
  "PlantTimeSeconds": 10,
  "ShowPlantCountdown": true,
  "MenuFooter": "by Jumper",
  "ApplySelectionsOnNextSpawnOnly": true,
  "SharedSecondaryPreference": true,
  "SecondaryFallbackWeapons": {
    "Terrorist": "Deagle",
    "CounterTerrorist": "Deagle"
  }
}
~~~

Diese Werte sind bereits die Standardwerte, wenn die Schlüssel fehlen. PlantTimeSeconds muss größer als 0 und höchstens 300 sein. EnableAwp bleibt im bisherigen AWP-Abschnitt; **0 sperrt AWP**. Weapons.UsableWeapons ist zusätzlich die verbindliche Waffenliste. Beide Prüfungen gelten gleichzeitig. Eine konfigurierte Ersatzpistole muss für das Team und in UsableWeapons erlaubt sein; andernfalls wird eine erlaubte Team-Pistole genommen. Gibt es keine, wird keine gesperrte Waffe ausgegeben.

8. Container vollständig neu starten. Nicht nur im laufenden Prozess die DLL austauschen. Zum Einschalten eines beim Start deaktivierten Plant-Timers ist ein Neustart notwendig, damit TerminateRound geprüft wird.
9. Log auf **Connected to Retakes AllocateEvent** prüfen. Der optionale Buy-Hook meldet **CanAcquire hook attached using CounterStrikeSharp gamedata**. Fehlt sein Handle, bleibt er aus; Menü und reguläre Ausrüstung funktionieren weiter. Fehlt TerminateRound, wird der Plant-Timer mit einer eindeutigen Fehlermeldung deaktiviert.
10. Den [Testplan](TESTPLAN-301.md) auf dem Server durchführen.

## Bedienung und Waffen

**!guns**, **/guns** und **css_guns** öffnen das ursprüngliche Advanced Gun Menu im Center-HUD. W/S navigiert, E bestätigt, R geht zurück bzw. schließt das Hauptmenü. A/D wechselt im Primary-Untermenü zwischen CT und T. Gedrückt gehaltene Tasten lösen dieselbe Aktion nicht erneut aus. Die Hauptpunkte sind ausschließlich Primary Weapon und Secondary Weapon. Die Auswahl wird angezeigt, mit E automatisch gespeichert und erst beim nächsten regulären Retakes-AllocateEvent ausgerüstet. In allen Menüseiten steht der konfigurierte Footer, standardmäßig **by Jumper**.

Primary enthält die erlaubten FullBuy-/HalfBuy-Waffen und gegebenenfalls erlaubte Preferred-Waffen. Die ursprünglichen Pistol-/HalfBuy-/FullBuy-Rundentypen bleiben erhalten. Für die ausgewählte FullBuy-Primary in jeder Runde setzt wie bisher RoundTypeSelection auf Random und RoundTypePercentages auf Pistol=0, HalfBuy=0, FullBuy=100. Halbkäufe verwenden die gespeicherte HalfBuy-Primary.

Die neue gemeinsame Pistolenpräferenz wird für CT, T und Pistol-Rounds benutzt. Teamgebundene Pistolen verwenden auf der anderen Seite die sichere Ersatzwaffe. Es gibt kein separates Pistol-Round-Menü.

Es wurden **keine Tabellen/Spalten/Migrationen geändert**. Die gemeinsame Pistole wird im vorhandenen WeaponPreferences-JSON unter **None -> Secondary** gespeichert. Alte Team- und Rundentyp-Präferenzen bleiben unverändert vorhanden. Ohne neuen gemeinsamen Eintrag gilt deterministisch: CT Secondary, dann T Secondary, dann CT PistolRound, dann T PistolRound. Bei unterschiedlichen alten Werten entscheidet diese Reihenfolge; es wird beim Upgrade kein Wert gelöscht. Nach einer neuen Auswahl ersetzt der gemeinsame Eintrag die wirksame Pistole auf beiden Teams. SharedSecondaryPreference=false reaktiviert für die Vergabe/Befehle das alte getrennte Verhalten; die neue Menü-Pistolenauswahl ist für den gemeinsamen Modus vorgesehen.

## Timer, HUD und Kompatibilität

Der Plant-Timer startet nach **round_freeze_end**, ausschließlich in einer durch das Retakes-AllocateEvent aktivierten Runde mit lebenden CTs und Ts, außerhalb des Warmups. Er endet beim **erfolgreichen** bomb_planted, nicht schon bei bomb_beginplant. Nach 10 Sekunden ohne erfolgreichen Plant wird über **CCSGameRules.TerminateRound(0.1f, CTsWin)** beendet. Plant-Timer, Countdown und Bombsite-HUD werden bei Rundenende, Mapwechsel und Unload beendet. Countdown und geöffnetes Waffenmenü teilen sich eine einzige HUD-Nachricht; die Bombsite-Anzeige wird dabei unterdrückt. Eingabeänderungen werden maximal viermal pro Sekunde angezeigt. Gleiche Inhalte werden nur alle zwei Sekunden bei fünf Sekunden Anzeigedauer erneuert. Die größere Anzeige enthält drei Waffen pro sichtbarem Ausschnitt. Die bekannte CS2-HTML-Flicker-Korrektur hält GameRestart nur während unserer HUD-Anzeige, solange RestartRoundTime bereits vergangen ist. RestartRoundTime wird niemals verändert; ein neu geplanter Neustart und ein fremder bereits gesetzter Flag bleiben unangetastet. Unsere Flag-Änderung wird beim Schließen bzw. Cleanup zurückgenommen. Zum Beenden wird einmal geleert.

Die Native-Hook-Reparatur aus dem vorherigen Fork bleibt enthalten: kanonische CSS375-Signaturen, Null-Handle-Prüfung und explizite Callback-Referenz bis zum erfolgreichen Unhook. AutoUpdateSignatures und CapabilityWeaponPaints bleiben ohne Wirkung; alte Plugin-GameData wird ignoriert. Der ungeprüfte GiveNamedItem2-Pfad bleibt entfernt. Besondere Skin-Übernahme für teamfremde Waffen wird nicht garantiert.

Der Build verwendet .NET 10, CounterStrikeSharp.API 1.0.375 und RetakesPluginShared 2.0.0. Host-/Shared-DLLs werden nicht mitgeliefert. Das Paket enthält die eigenen Abhängigkeiten und Linux-x64-SQLite, auch wenn auf dem Server MySQL verwendet wird.

## Prüfung und Grenzen

Release-Build und automatisierte Regressionstests werden lokal und im Linux-CI ausgeführt. Linux-CI testet zusätzlich gegen MySQL 8 eine vorhandene UserSettings-Tabelle mit alten Präferenzen und die gemeinsame Auswahl ohne Schemaänderung. Das ist kein Starttest in einem echten CS2-Prozess. Menüdarstellung, Native-Hooks, Grenzfälle des Plant-Events und die Netzkanal-Stabilität sind auf eurem Server anhand des Testplans zu bestätigen. Es wurden keine Änderungen auf eurem Server vorgenommen.

Quellen: [CSS375](https://github.com/roflmuffin/CounterStrikeSharp/releases/tag/v1.0.375), [Retakes 3.0.2](https://github.com/B3none/cs2-retakes/tree/3.0.2), [Instaplant-Quellcode](https://github.com/B3none/cs2-instaplant/blob/master/InstaplantPlugin.cs).

Beim Upgrade auf 301.1 die neue RetakesAllocator.dll und RetakesAllocatorCore.dll vollständig ersetzen und Container neu starten. Im Plugin-Log muss 301.1 stehen. Alte T-/CT-Loadout-/Pistol-/HalfBuy-Sprachschlüssel und Menü-GIFs werden nicht mehr benutzt und sind aus den mitgelieferten Sprachdateien entfernt. Werden diese Menüs noch angezeigt, prüfen, ob ein alter Allocator oder ein anderes Waffenmenü parallel geladen wird.
