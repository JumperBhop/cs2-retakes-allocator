# Server-Testplan – Jumper RetakesAllocator 301

Voraussetzungen: Linux-Testserver/Docker, CSS375 mit vollständiger GameData, Retakes3.0.2, genau ein Allocator. Bestehende MySQL-Datenbank vorher sichern. FallbackAllocation=false, Retakes Bomb.IsAutoPlantEnabled=false. Für normale Plant-Dauer InstaplantPlugin deaktivieren. Kein anderes Plant-Timeout-Plugin. mp_c4timer vor dem Test notieren.

## 1. Start und Lebenszyklus

- Container neu starten. RetakesAllocator muss Version 301.2 anzeigen und sich am AllocateEvent anmelden. Keine Invalid-function-pointer-/GC-Delegate-Meldung.
- EnableCanAcquireHook=false testen: !guns und normale Ausrüstung bleiben funktionsfähig.
- Mehrere Mapwechsel, anschließend geordnetes Plugin-Unload/Reload testen. Keine alten Countdown-/Menüanzeigen oder späteren Timer-Aktionen.
- Einen offenen Menü-Speichervorgang während Reconnect/Mapwechsel prüfen. Alte Antworten dürfen das Menü nicht wieder öffnen.

## 2. Menü und Eingabe

- !guns und /guns als CT und T öffnen: Center-HUD mit genau Primary Weapon und Secondary Weapon sowie Footer by Jumper, kein nummernbasiertes Waffen-Chatmenü.
- W und S jeweils fünf Sekunden halten: genau eine Bewegung je Tastendruck. Taste loslassen und erneut drücken: nächste Bewegung.
- W halten und E separat drücken: genau eine Bestätigung. E halten: keine wiederholten DB-Schreibvorgänge.
- R aus Untermenü: Hauptmenü; R im Hauptmenü: geschlossen. Kein Close-/Loadout-/Pistol-Round-/HalfBuy-/AWP-Kategorieeintrag.
- A/D im Primary-Menü: CT/T wechseln. Erstes/letztes Element und leere Liste prüfen; kein Indexfehler.
- In jedem Untermenü Footer und gespeicherte Auswahl prüfen. Eine Auswahl bestätigen, Menü erneut öffnen: gespeichert. Kein zusätzlicher Save-Button.

## 3. Gemeinsame Pistole und Datenbank

- Desert Eagle einmal auswählen. Danach T, CT und Pistol-Round spielen: dieselbe Pistole.
- Neue Auswahl während einer laufenden Runde: aktuelles Inventar unverändert; erst nächste reguläre Vergabe ändern.
- Für AK/M4-Prüfung FullBuy-Runden erzwingen oder konfigurieren. T-AK und CT-M4 getrennt speichern; Wechsel erhalten.
- Tec-9 wählen und CT spielen: konfigurierte erlaubte CT-Ersatzpistole erhalten. T erhält Tec-9. Gesperrte Ersatzwaffe konfigurieren: erlaubte Alternative oder keine Pistole, niemals gesperrte Waffe.
- Reconnect und Container-Neustart: gemeinsame Pistole und beide Primärwaffen bleiben gespeichert.
- Vorhandene alte CT/T-/Pistol-Präferenzen in MySQL vergleichen: alte JSON-Einträge noch vorhanden; nur None/Secondary wird neu ergänzt. Keine neuen Spalten. Bei abweichenden alten Pistolen gilt die dokumentierte CT-Secondary-Priorität.
- Zwei Spieler gleichzeitig auswählen lassen. Keine DbContext-Concurrency-Fehler oder verlorenen Primärpräferenzen.

## 4. NoAWP und andere Sperren

- In der vorhandenen gruppierten Config AWP.EnableAwp=0 setzen, AWP aus Weapons.UsableWeapons entfernen, Config neu laden.
- AWP erscheint nicht in auswählbaren Menüoptionen. !gun awp und !awp dürfen keine AWP-Präferenz setzen oder ausrüsten.
- Mit vorhandener alter AWP-Präferenz mehrere FullBuy-Runden prüfen: keine AWP, normale erlaubte Primary.
- EnableAwp=1 bei weiterhin fehlender AWP in UsableWeapons: weiterhin gesperrt. Danach AWP in Whitelist bei EnableAwp=0: weiterhin gesperrt.
- Eine andere ausgewählte Waffe aus UsableWeapons entfernen und Config neu laden: Menü filtert sie, Speichern wird abgelehnt, nächste Vergabe nutzt erlaubte Waffen.
- Leere UsableWeapons-Liste prüfen: keine verbotene Waffe und kein Zufallsauswahl-Absturz.

## 5. Plant-Deadline

- Freeze-Time testweise 5 Sekunden: während Freeze kein Countdown und kein Timeout. Nach Freeze-Ende Anzeige Plant the bomb: 10s.
- Ohne Plant: nach zehn Sekunden aktiver Spielzeit CTsWin und Meldung. Zeit über Demo/Server-Log nach Freeze-Ende kontrollieren; serverseitige Timer laufen in Server-Ticks.
- Plant vor Ablauf erfolgreich abschließen: Countdown verschwindet, keine spätere Deadline-Aktion. mp_c4timer unverändert, normale Detonation/Defuse möglich.
- Plant kurz vor Ablauf beginnen, erst nach Ablauf abschließen: Beginn allein stoppt den Timer nicht; CTs gewinnen.
- Plant bei ungefähr 9,9/10,0 Sekunden mehrfach prüfen. Bei Ablauf zählt der zu diesem Server-Tick erfolgreich registrierte Plant; kein doppelter Rundenabschluss.
- Warmup, leere Teams, bereits automatisch gepflanzte Bombe: keine unpassende Deadline.
- Rundenende durch Kill/Defuse vor Timeout, Mapwechsel, Unload: keine alte CT-Sieg-Aktion in der nächsten Runde.
- PlantTimeSeconds=7: sieben Sekunden; ShowPlantCountdown=false: keine Anzeige, Deadline trotzdem aktiv; PlantTimerEnabled=false nach Neustart: kein Timer.

## 6. HUD und Stabilität

- Waffenmenü während Plant-Countdown öffnen: beide in einer Anzeige, Footer sichtbar, kein gegenseitiges Flackern. Bombsite-HUD darf diese Anzeige nicht überschreiben.
- Bombsite-Center aktivieren und alle Spieler gleichzeitig navigieren lassen. Mindestens 20 Runden, mehrere Reconnects und zwei Mapwechsel.
- Keine Netchan-High-Water-Mark-/excessive-CPU-/Überlauffehler. Menü-HUD maximal vier Updates pro Sekunde/Spieler, keine wiederholten Anzeigen für geschlossene Menüs.
- Spectators erhalten niemals Spawn-Ausrüstung. Doppelte AllocateEvents dürfen nicht zweimal Waffen verteilen.
- Server-Logs, CSS-Version, Pluginliste und Testfälle mit Abweichungen aufbewahren. Dieser Ingame-Test ist nach automatisierten Build-/Datenbanktests weiterhin erforderlich.

## Zusatzprüfung 301.1
- Menü 30 Sekunden ohne Eingabe offen lassen: kein sekündliches Blinken, große Schrift und Footer sichtbar. Danach schnell mit W/S navigieren; gehaltene Tasten bleiben entprellt.
- Hauptmenü enthält exakt zwei Einträge. Untermenü zeigt jeweils drei große Waffenzeilen, Auswahl scrollt durch alle erlaubten Waffen.
- Während geöffnetem Menü einen echten mp_restartgame-Neustart durchführen: Neustart funktioniert. Danach Menü schließen und mehrere normale Retake-Runden spielen; keine falschen Neustarts oder hängenbleibenden HUD-Flags.
- Round-/Map-Cleanup und Unload mit offenem Menü bzw. aktivem Countdown prüfen. Keine alte Anzeige und keine zurückgelassene eigene GameRestart-Änderung.

## 301.2: Serverstart und Entity-System

- Vollständigen Container-Kaltstart und mehrere Mapwechsel prüfen: keine wiederholten Entity system yet is not initialized-Meldungen aus dem Allocator; !guns und zukünftige Ausrüstung funktionieren nach Mapstart.
- Nach vollständig gestartetem Server Plugin-Unload/Hot-Reload testen; !guns ohne zusätzlichen Mapwechsel verfügbar.
- Im Log den Ursprung verbleibender Fehler prüfen: Ranks.UpdateUserStatsTimer gehört zum Ranks-Plugin. Bei Wiederholung Ranks gesondert aktualisieren/deaktivieren und danach den Container neu starten.
