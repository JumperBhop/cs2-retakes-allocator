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
