# Jumper RetakesAllocator 301.2

## Patch 301.2: Entity-System beim Serverstart

- Vor dem ersten Entity-Zugriff prüft der Allocator auf dem Server-Thread die öffentliche NativeAPI.GetConcreteEntityListPointer-API. Noch nicht initialisierte Entity-Systeme werden höchstens einmal pro Sekunde erneut geprüft, ohne CSSs EntitySystem-Lazy anzufassen.
- OnTick verarbeitet Menüeingaben, HUD und Spieler erst nach erfolgreicher Prüfung. Beim Map-Ende werden Zugriffe gesperrt; beim Map-Start wird erneut geprüft. Hot-Reload auf einem bereits gestarteten Server bleibt möglich.
- Initiales HUD-Reset, Menü-Reset, Spielerprüfung, GameRules-Abfrage, Chat-Events und Ausrüstung respektieren diese Freigabe. Ein unsichtbares HUD ohne eigene Restart-Flag-Lease sucht keine GameRules.
- Falls ein anderes Plugin den prozessweiten CSS375-Lazy bereits mit dem Startfehler belastet hat, stoppt der Allocator seine Tick-Zugriffe und gibt eine einzelne Fehlermeldung aus. Keine Reflection-Manipulation an CSS, keine neuen Native-Hooks oder Signatures.
- Vollständiger Container-Neustart erforderlich: Plugin-Reload beseitigt einen bereits gecachten Fehler nicht. Das vorgelegte Log enthält denselben Fehler zusätzlich in Ranks.UpdateUserStatsTimer; dieser Patch verändert Ranks nicht.
- Lokal: 74 Tests bestanden, 1 MySQL-Test mangels lokalem Server übersprungen; Plugin-Build ohne Fehler/Warnungen. Vier neue Lebenszyklus-Tests prüfen verzögerten Start, Probe-Begrenzung, Mapwechsel, Hot-Reload und unerwartete Probe-Fehler. Linux/MySQL-Prüfung erfolgt zusätzlich im GitHub-Workflow.
- Die Darstellung aus 301.1 bleibt erhalten: Hauptmenü nur Primary Weapon und Secondary Weapon, größere Schrift, keine Blockzeichen.

# Jumper RetakesAllocator – Änderungen

## Update 301.1 – Menü und HUD

- Hauptmenü enthält ausschließlich Primary Weapon und Secondary Weapon. R schließt bzw. geht zurück; E speichert wie bisher automatisch.
- Größere Schrift, deutlich hervorgehobene Auswahl, drei große Waffenzeilen pro Untermenü. Footer by Jumper bleibt sichtbar. Keine Loadout-/Pistol-/HalfBuy-/AWP-Kategorien.
- Alte dekorierte Sprachschlüssel sowie Menü-GIFs in allen Sprachdateien entfernt.
- Identische HUD-Nachrichten werden nur alle zwei Sekunden erneuert, mit fünf Sekunden Anzeigedauer. Geänderte Inhalte bleiben auf vier Aktualisierungen pro Sekunde begrenzt.
- Bekannte HTML-HUD-Flicker-Korrektur nach Poggu/girlglock direkt eingebaut: temporär GameRestart während unserer Anzeige, nur bei vergangenem RestartRoundTime. Tatsächliche Neustart-Timer werden nicht verändert. Fremde Flag-Änderungen/neue Restart-Deadlines werden respektiert; eigene Änderungen beim Schließen, Map-/Round-Cleanup und Unload zurückgenommen.
- Neue Regressionstests für genau zwei Hauptpunkte, große HTML-Zeilen, Escaping, Nachrichten-Cache und die Flag-Lebensdauer bei echten geplanten Neustarts.
- Kein neues Menüpaket, keine neue Datenbankmigration und kein zusätzlicher Native-Hook.
- Betroffene neue Dateien: GunMenuView.cs, HudContentCache.cs, HtmlHudRestartLease.cs, HtmlHudStability.cs und MenuHudRegressionTests.cs. Weitere Änderungen in AdvancedGunMenu.cs, RetakesAllocator.cs, lang/*.json, Versions-/Paketdateien und Dokumentation.

# Änderungen – Version 301

Basis: Yoni Lerners RetakesAllocator v2.4.2 plus Jumper-CSS375-Absturzreparatur. Version im Plugin: **301**; Assembly: **301.0.0**. Linux-Paket: cs2-retakes-allocator-301-linux-x64.zip.

## Waffenmenü und Nachrichtenüberlauf

Die ursprüngliche Klasse AdvancedGunMenu bleibt der Einstieg für das Center-HUD-Menü. Die lange wiederholte Seitenlogik wurde zu drei Seiten mit gemeinsamem Eingabe-/Renderpfad zusammengeführt; keine externe oder zusätzliche Menü-Bibliothek.

Die alte Implementierung baute und sendete PrintToCenterHtml bei jedem Tick für alle lebenden Spieler, auch wenn kein Menü aktiv war. Menü und Bombsite-HUD konnten denselben Kanal im selben Tick beschreiben. Bei 64 Ticks/sek waren entsprechend viele GameEvent-Nachrichten möglich. Zudem verglich sie das gesamte Button-Bitfeld mit einzelnen Tasten: kombinierte Eingaben konnten nicht sauber erkannt werden. Das ist ein im Code nachgewiesener Fehlpfad, der den gemeldeten Netchan-Überlauf erklären kann; der genaue Server-Disconnect wurde hier nicht live reproduziert.

Jetzt: ein registrierter Tick-Listener; rising-edge-Erkennung pro Menüsession; W/S, E, R und A/D für den Primary-Teamwechsel; kein Client-Sound-Befehl pro Navigation; maximal vier HUD-Updates pro Sekunde/Spieler; keine leeren Tick-Nachrichten für Spieler ohne Anzeige. Countdown und Menü werden kombiniert. Alte asynchrone Speicherantworten öffnen keine geschlossenen oder ersetzten Menüs. Datenbankarbeit des Menüs erfolgt außerhalb des Server-Ticks mit eigenen DbContexts und serialisierten Schreiboperationen.

## Präferenzen und NoAWP

Gemeinsame Pistole in der vorhandenen JSON-Spalte, ohne Tabellenänderung. Alte Präferenzen bleiben erhalten. Teamrestriktionen werden vor der Vergabe geprüft und durch erlaubte konfigurierbare Ersatzpistolen aufgefangen. Gesperrte Preferred-Waffen belegen keinen AWP-Queue-Platz. Leere Waffenlisten führen nicht mehr zu einer ungültigen Zufallsauswahl.

Die bisher nur flach deserialisierte Konfiguration akzeptiert jetzt auch Config/Weapons/AWP/Database-Gruppen. Dadurch wird AWP.EnableAwp=0 tatsächlich gelesen. UsableWeapons und EnableAwp werden in Menülisten, Speichern/Befehlen und unmittelbar vor GiveNamedItem geprüft. Bestehende Config-Dateien einschließlich unbekannter Schlüssel werden nicht umgeschrieben.

## Plant-Timer

Eigenständige Deadline nach Freeze-Ende, standardmäßig 10 Sekunden. Erfolgreicher Plant stoppt sie. Ablauf beendet die Runde mit CTsWin über die unterstützte CSS375-Methode. Keine Änderung von mp_roundtime_defuse oder mp_c4timer. Genau ein Timer wird gehalten, mit Cleanup für Runde, Map und Unload. TerminateRound wird vor Aktivierung auf einen gültigen kanonischen Funktionshandle geprüft. Bombsite-Ankündigungen kommen jetzt direkt aus dem offiziellen AnnounceBombsiteEvent; die verschachtelten verzögerten Bombzone-Timer sind entfernt.

## Geänderte Dateien

- RetakesAllocator/RetakesAllocator.cs: Menüanbindung, gemeinsame HUD-Ausgabe, Retakes-Events und Spawn-Prüfung.
- RetakesAllocator/Menus/AdvancedGunMenu.cs: bestehendes Advanced-Menü, Eingaben, Footer, Auswahl und asynchrones Speichern.
- RetakesAllocator/Menus/GunsMenu.cs: auch der unbenutzte alte Chat-Menüpfad überspringt gesperrte AWP-Optionen.
- RetakesAllocator/PlantTimerController.cs: neuer Plant-Timer mit CSS375-Rundenabschluss.
- RetakesAllocator/Helpers.cs: vorhandener GameRules-Zugriff für Timer freigegeben.
- RetakesAllocatorCore/Config/Configs.cs: gruppierte Config, neue Einstellungen, Erhalt vorhandener Dateien.
- RetakesAllocatorCore/Db/UserSetting.cs und Queries.cs: gemeinsamer JSON-Eintrag, sichere Schreibreihenfolge, eigene Kontexte.
- RetakesAllocatorCore/WeaponHelpers.cs, OnWeaponCommandHelper.cs und OnRoundPostStartHelper.cs: gemeinsame Pistole, Ersatzwaffen und Sperren.
- RetakesAllocatorCore/MenuInput.cs, HudRefreshGate.cs und PlantDeadline.cs: testbare Eingabe-, Rate- und Deadline-Logik.
- PluginInfo.cs und beide Plugin-Projektdateien: Version 301.
- RetakesAllocatorTest/Version301Tests.cs, MySqlCompatibilityTests.cs und GlobalSetup.cs: neue Regressionen, MySQL-CI und Erhalt der bisherigen Verhaltenstests.
- .github/workflows/build.yml und scripts/package.ps1: Linux-/MySQL-Prüfung, Paket 301 mit Anleitungen.
- README.md, INSTALLATION-DE.md, CHANGELOG-301.md und TESTPLAN-301.md: Bedienung, Upgrade und konkrete Serverprüfungen.

Die vorhandenen Migrationen und der reparierte Native-Hook-Code wurden für v301 nicht ersetzt.
