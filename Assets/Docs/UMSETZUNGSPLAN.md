# Aegis Protocol – TODO und Umsetzungsplan

Stand: 18.09.2026 · Ziel: Das vorhandene Spiel für Android im Hochformat fertigstellen.

Grundlage: [Projektanalyse](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/Docs/PROJEKTANALYSE.md). Die Kennungen A01–A22 verweisen auf deren Befunde. Die Analyse und isolierten Compilerprüfungen sind bereits erfolgt. Das Projekt wurde anschließend vom Entwickler auf Unity `6000.3.15f1` aktualisiert; der Android-Start wurde vom Entwickler am 18.09.2026 als funktionierend gemeldet.

## Arbeitsregeln

- Die Pakete in der angegebenen Reihenfolge bearbeiten. Einzelne Änderungen klein und überprüfbar halten.
- Erst Fehler und widersprüchliche Spielregeln beheben, anschließend Bedienung und Balancing verbessern.
- Vorhandenen Look, Spielumfang und grundlegende Klassenstruktur erhalten.
- Eine Aufgabe erst abhaken, wenn ihr Abnahmekriterium geprüft wurde. Bei Gerätetests Gerät und Buildversion notieren.
- Keine neuen Spielmodi, keinen PC-Port und keinen vollständigen Architekturumbau einplanen.

## 1. Reproduzierbaren Ausgangspunkt herstellen

Aufwand: etwa 0,5–1 Tag. Voraussetzung für belastbare Laufzeitprüfungen.

- [x] **Unity-Version aktualisiert:** `6000.3.15f1` ist in `ProjectSettings/ProjectVersion.txt` eingetragen. Dieser Teil von U01 ist erledigt.
- [x] **Editor-Start und Android-Werkzeuge geprüft:** Das Projekt ist mit Unity `6000.3.15f1` geöffnet; die Laufzeit- und Editor-Assemblies wurden am 18.09.2026 erzeugt. Im aktuellen Editor-Log stehen keine C#-Compilerfehler. Android Build Support mit SDK, NDK und OpenJDK ist vorhanden.
- [x] **U01 – Android-Start prüfen.** Unter Unity `6000.3.15f1` einen Development-Build erstellen und auf einem Gerät starten. **Abnahme:** Buildschritte und verwendetes Gerät sind dokumentiert; das Spiel startet. **Nachweis vom 18.09.2026:** Der Entwickler hat den Android-Build auf einem Google Pixel gestartet und als funktionierend bestätigt. Vorgehen: Projekt mit Unity `6000.3.15f1` für Android bauen, Build auf dem Gerät installieren und starten. Aktuelle Projektkonfiguration: Version `1.2`, Android-Versioncode `3`. Das genaue Pixel-Modell wurde für diesen Test nicht separat genannt; die Eco-lor-Geräteprotokolle nennen für das nach Aussage des Entwicklers gleiche Gerät ein Pixel 7. Ein APK-Artefakt ist im Repository nicht abgelegt.
- [x] **U02 – Einfachen Editor-Eingabepfad reparieren.** Fehlerhaften Standalone-Zweig in `UIManager` korrigieren und Mausbedienung auch für Ressourcen ermöglichen. Touch und Maus sollen dieselben Aktionen auslösen. **Abnahme:** Station öffnen, Modul kaufen und Material sammeln funktionieren im Editor und auf Android. Bezug: A19.

U02 wurde auf `codex/u02` umgesetzt: gemeinsamer Touch-/Mauspfad für Station und manuelles Sammeln, UI-Klicks werden vor Weltaktionen abgefangen. Unity kompiliert mit Android- und Windows-Standalone-Ziel; die vorhandenen 2 Edit-Mode- und 5 Play-Mode-Tests bestehen. Der Entwickler hat den anschließend angefragten Praxistest im Editor und auf dem Pixel am 18.09.2026 als erfolgreich bestätigt.

- [ ] **U03 – Ausgangsverhalten festhalten.** Kurzen Run mit Bau, Upgrade, Schaden, Laden und Replay durchführen; Fehler und relevante Console-Meldungen notieren. **Abnahme:** Die wichtigsten Analysebefunde sind reproduziert oder ausdrücklich als noch ungeprüft markiert.

Automatisiert geprüft am 18.09.2026: Bau eines Extractors, Kauf eines Damage-Upgrades, Schaden am Core, Speichern, Szenen-Neustart/Laden und Replay als zusammenhängender Play-Mode-Test. Bestanden unter Unity `6000.3.15f1` mit Android als Build-Ziel. Sichtprüfung und Console-Protokoll eines vollständigen Runs im Editor und auf dem Pixel stehen noch aus; U03 bleibt deshalb offen.

## 2. Run-Zustand und Spielregeln stabilisieren

Gemeinsam mit Paket 3 etwa 3–5 Tage. Vor neuem Balancing abschließen.

- [x] **U04 – Einen eindeutigen NewRun-Ablauf einführen.** GameOver beendet den Run und hält dessen Ergebnis fest. Neustart entfernt Laufzeitobjekte, setzt Anfangswerte in fester Reihenfolge und aktualisiert zuletzt die UI. Verdeckten Welt-Reset aus `DeleteSaveData` entfernen. **Abnahme:** Drei aufeinanderfolgende Replays starten mit identischen Ressourcen, Preisen, Leveln und Statistiken. Bezug: A05. **Nachweis vom 18.09.2026:** Core-Tod, Ergebnis/Bestscore, Löschen des alten Saves, Bereinigung von Gegnern, Drohnen, Projektilen, Material- und Explosionseffekten sowie drei Replays im Play-Mode-Test geprüft. Replay stoppt den alten Kamera-Shake und schreibt einen frischen Anfangs-Save.
- [x] **U05 – Upgrade-Vorlagen und Laufzeitzustand trennen.** Neue Runs aus unveränderten Vorlagen initialisieren; keine bereits veränderten Runtime-Sets erneut als Ausgangskonfiguration verwenden. Widersprüchliche Snapshot-Resets ersetzen. **Abnahme:** Ein ausgebauter Run beeinflusst die Anfangswerte des nächsten Runs nicht. Bezug: A05. **Nachweis vom 18.09.2026:** Runtime-Sets werden nur beim Szenenstart aus den Vorlagen kopiert; Replay setzt die vorhandenen Laufzeitwerte auf ihre Start-Snapshots zurück. Ein Play-Mode-Test verändert alle Upgradelevel, speichert/lädt und prüft die unveränderten Startwerte nach zwei neuen Runs und erneutem Szenenstart. Die Modulkennung wird beim Kopieren mitgenommen. Die falsch zugeordnete Temporal-Modulator-Vorlage bleibt Teil von U19.
- [x] **U06 – Statische Registrierungen und Bereinigung korrigieren.** Modulregistrierungen und GameManager-Flags sauber initialisieren beziehungsweise bereinigen. Entfernen alter Gegner darf keine Kampf-Kills erzeugen. **Abnahme:** Wiederholter Spielstart erzeugt keine doppelten Module, veralteten Referenzen oder künstlichen Kills. Bezug: A20, A21. **Nachweis vom 18.09.2026:** Drei Szenenstarts einschließlich Neustart nach Game Over prüfen eindeutige Modul-, Upgrade- und Set-Registrierungen sowie zurückgesetzte Flags. Der Save-Manager bleibt als einzelnes Root-Objekt erhalten. Der U04-Test prüft, dass beim Entfernen alter Gegner keine Kills entstehen.
- [x] **U07 – Regeln bei Modulverlust festlegen und zentral anwenden.** Für jedes Modul festhalten, welche Grundfunktion erhalten bleibt und welche Upgrades ohne Modul wirken. Effekte aus Vorlage, Level und Modulstatus ableiten. Fremder Wiederaufbau darf andere zerstörte Module nicht reparieren. **Abnahme:** Zerstörung, Wiederaufbau und Laden ergeben dieselben wirksamen Werte; AutoCollecting bleibt ohne Extractor entsprechend der festgelegten Regel aus. Bezug: A04.
- [x] **U08 – Modulvorschau vom Kampf ausschließen.** Vorschau-Collider deaktivieren; Schaden an ungebaute oder bereits zerstörte Module abweisen. **Abnahme:** Ein ausgewähltes, nicht gekauftes Modul fängt keine Treffer ab und verändert keine Verluststatistik. Bezug: A06.
- [x] **U09 – Bau, Reparatur und Upgrades als vollständige Aktionen behandeln.** Voraussetzungen und Preis unmittelbar bei Ausführung prüfen. Nur bei erfolgreicher Zahlung ändern; anschließend Statistik und UI aktualisieren und Speichern anfordern. **Abnahme:** Keine kostenlosen Käufe bei veraltetem Buttonzustand; Zahlung und Ergebnis bleiben zusammen erhalten. Bezug: A07, A17.
- [x] **U10 – Zeitsteuerung vereinheitlichen.** Nur echte Menü-Übergänge verarbeiten; gewünschtes und effektives Tempo eindeutig verwalten. Modulverlust und Neustart setzen alle zugehörigen Werte korrekt. **Abnahme:** Nach Tempoänderung, Tippen ins Leere, Menüwechsel und Replay stimmen Anzeige und tatsächliche Geschwindigkeit überein. Bezug: A11.
- [x] **U11 – Schildzustand an Modulzustand koppeln.** Bei Modulverlust Recharge abbrechen; Aktivierung an ein gebautes Modul binden. **Abnahme:** Ein verlorenes Schildmodul wird nach Ablauf eines alten Countdowns nicht wieder aktiv. Bezug: A12.

Modulregel: Der Core beendet bei Verlust den Run. Ohne Extractor bleibt manuelles Sammeln mit Grundeffizienz möglich, AutoCollecting stoppt. Ohne Shield gibt es keinen aktiven Schild und keinen laufenden Recharge; ein Neubau startet mit vollen Schildpunkten. Ohne Drone-Modul verschwinden die Drohnen und es werden keine neuen gebaut. Ohne Radar, Ammo Fabricator oder Command Unit gelten für Reichweite, Feuerrate/Schaden beziehungsweise Rotation/Struktur wieder die Basiswerte; gekaufte Level bleiben für einen späteren Wiederaufbau erhalten. Ohne Temporal Modulator gilt normales Tempo und dessen Steuerung ist verborgen. Effekte werden aus Level und aktuellem Modulstatus abgeleitet, ohne beim Modulverlust Upgrade-Daten zu überschreiben. Die Play-Mode-Tests decken Modulverlust/Laden/Wiederaufbau, Vorschau, Kaufaktionen, Tempo und Schildaufladung ab.

## 3. Speichern und Fortsetzen zuverlässig machen

Auf Paket 2 aufbauen; insbesondere dieselben abgeleiteten Werte und Modulregeln verwenden.

Stand 18.09.2026: Save/Load und Wellen-Checkpoint sind im Play Mode mit Android als Build-Ziel geprüft. Der echte Android-Hintergrundwechsel samt App-Neustart bleibt als Gerätetest offen. Die Regeln bei Modulverlust sind unter U07 festgelegt.

- [ ] **U12 – Android-Lifecycle-Speicherung korrigieren.** Pause-/Quit-Callbacks aus der Datenklasse in den SaveGameManager verlegen. Initialisierung und beendete Runs berücksichtigen. **Abnahme:** Hintergrundwechsel speichert einen gültigen Zustand; Rückkehr oder Neustart verliert keinen zuvor bestätigten Fortschritt. Bezug: A01.
- [x] **U13 – Ladeablauf ohne Seiteneffekte implementieren.** Erst Module und Upgradelevel, dann abgeleitete Maximalwerte, danach aktuelle HP wiederherstellen. Drohnen explizit initialisieren. Währenddessen keine Saves oder Bau-Statistiken erzeugen. **Abnahme:** Beschädigte Module und Drohnen besitzen nach Laden exakt dieselben HP und Maximalwerte. Bezug: A02. Im Play Mode mit erhöhtem Struktur-Maximum, beschädigtem Core und beschädigter Drohne geprüft.
- [x] **U14 – Statistik vollständig speichern.** Kill-Einträge serialisierbar ablegen sowie fehlende Ressourcen- und Modulkostenzähler ergänzen. **Abnahme:** Statistik und berechneter Score sind vor und nach Laden identisch. Bezug: A03. Ressourcen-, Modulkosten- und Kill-Zähler sowie Score wurden nach Szenen-Neustart geprüft; für den Vergleich wurde die inzwischen verstrichene Framezeit ausgeklammert.
- [x] **U15 – Dateizugriff absichern und bündeln.** Häufige Änderungen bündeln; vollständigen Zustand über temporäre Datei und atomaren Austausch sichern. JSON, Version und Werte validieren; beschädigte Dateien und Schreibfehler behandeln. **Abnahme:** Defekte Save-Datei blockiert den Start nicht; einzelne Drops und Treffer schreiben nicht mehr jeweils den gesamten Save. Bezug: A07. Materialdrops und nicht tödliche Modultreffer fordern einen verzögerten Save an; Pause/Quit und Käufe schreiben sofort. Temporäre Datei, Austausch und Backup werden verwendet. Ungültiger Spielstand lädt das Backup oder beginnt neu. Editor-Tests bestanden; Android-Dateisystem bleibt unter U36 zu prüfen.
- [x] **U16 – Verbindlichen Wellen-Checkpoint festlegen.** Empfohlener kleiner Umfang: vollständigen Zustand zu Beginn einer Welle sichern und beim Fortsetzen diese Welle wiederholen. Ressourcen, Käufe und Statistik müssen zum selben Checkpoint gehören. **Abnahme:** Kein übersprungener Restkampf und keine doppelte Belohnung durch Laden. Bezug: A08. Der Checkpoint entsteht nach der Wellenpause direkt vor dem ersten Spawn. Während Gegner aktiv sind, wird dieser Zustand gespeichert; nach dem letzten Gegner wird der vollständige neue Zustand gesichert. Laden wiederholt die Welle. Play-Mode-Tests prüfen Ressourcen/Statistik/Wellenindex und die einmalige Wellenzählung beim Fortsetzen.

U12-Teilstand: `OnApplicationPause` und `OnApplicationQuit` schreiben nur bei initialisiertem, laufendem Run. Der Play-Mode-Test prüft, dass Pause einen ausstehenden Save sofort schreibt und Game Over keinen neuen Save erzeugt. Für den Abschluss fehlt der reale Hintergrundwechsel mit App-Neustart auf Android.
- [x] **U17 – Gezielte Regressionstests ergänzen.** Roundtrip-Speicherung, NewRun, Modulverlust/Wiederaufbau und vollständige Kaufaktionen abdecken. **Abnahme:** Die Tests prüfen die Spielregeln und erkennen die zuvor gefundenen Fehler.

Fünfundzwanzig Tests liegen in `Assets/Tests/Editor/SaveGameTests.cs` und `Assets/Tests/PlayMode/GameFlowTests.cs`: Datei-/JSON-Roundtrip und Statistik sowie Laden, Replays, Modulverlust/Wiederaufbau, Kaufaktionen, U03-Kurzrun, Core-Tod/Game Over, Szenenstarts, Modulvorschau, Schild und Tempo. Hinzu kamen Pause-Speicherung, beschädigte Save-Datei mit Backup und zwei Wellen-Checkpoint-Prüfungen. Am 18.09.2026 bestanden zuletzt **3 Edit-Mode- und 22 Play-Mode-Tests** unter Unity `6000.3.15f1` mit Android als Build-Ziel. Play-Mode-Tests nutzen einen temporären Speicherordner. Ausführen über **Window > General > Test Runner**, jeweils **EditMode** und **PlayMode**. Android-Geräteprüfung bleibt Teil von U01/U36.

## 4. Upgrade-Daten, Wellen und Wirtschaft überarbeiten

Aufwand: etwa 2–3 Tage. Erst nach stabilen Zuständen Preise fein abstimmen.

- [x] **U18 – Upgrade-Grenzen und Kosten korrigieren.** Steigende und fallende Werte wirksam begrenzen; Endwerte, Maximallevel und Preise aufeinander abstimmen. Zahlenbereichsüberschreitungen verhindern. **Abnahme:** Alle erlaubten Level haben gültige Preise und Werte; keine negativen Bauzeiten oder Abweichungen zwischen Anzeige und Wirkung. Bezug: A09.
- [x] **U19 – Balancing-Werkzeuge und Metadaten reparieren.** `costStep` durch das tatsächliche Feld ersetzen; Levelvorschau an Laufzeitformeln angleichen. Modulzuordnung des Temporal Modulator und Kopieren von `moduleType` korrigieren; inaktive Module im Editor berücksichtigen. **Abnahme:** Vorschau und Spiel liefern für Stichproben dieselben Werte und Kosten. Bezug: A18 und Zusatzbefunde.

Teilstand: Das Kopieren von `moduleType` in Runtime-Sets ist mit U05 korrigiert. Die Temporal-Modulator-Vorlage ist jetzt korrekt zugeordnet; Editorfenster verwenden die Laufzeitformeln und zeigen inaktive Module.
- [x] **U20 – Wellen auf ein Gesamtbudget begrenzen.** Tatsächliche Gegnerzahl, Schwarmgrößen und Spawnzeit berücksichtigen. Neue Gegnertypen nachvollziehbar einführen; Bosswellen ausdrücklich definieren. **Abnahme:** Späte Wellen bleiben im vorgesehenen Budget; die vermeintliche 50er-Grenze erzeugt keine Tausende Gegner mehr. Bezug: A10.
- [ ] **U21 – Frühe Wirtschaft und Gegnerrollen abstimmen.** Startkäufe, Sammelautomatik, Schild und erste Drohne inklusive Slotkosten testen. Schildinteraktion mit Tanks/Bossen bewusst festlegen. **Abnahme:** Die ersten Minuten bieten verständliche Entscheidungen; Modulkäufe vermitteln ihren tatsächlichen Nutzen und ihre Gesamtkosten.

## 5. Kampf- und Darstellungsfehler schließen

Zusammen mit Paket 6 etwa 2–3 Tage; Umfang nach Gerätetest begrenzen.

- [x] **U22 – Ablenkung und Trefferstatistik korrigieren.** `isDeflected` beim Ablenken setzen; Schutz gegen mehrfach verarbeitete Treffer prüfen. **Abnahme:** Abgelenkte Treffer und Kills landen in der richtigen Kategorie und zählen nur einmal. Bezug: A13.
- [x] **U23 – Manuellen Sammelflug reparieren.** Ursprungsskalierung unabhängig von AutoCollecting initialisieren. **Abnahme:** Manuell eingesammelte Drops fliegen sichtbar zur Station und werden einmal gutgeschrieben. Bezug: A14.
- [x] **U24 – Zielgültigkeit für Turm und Drohnen prüfen.** Tote, verschwundene und außer Reichweite geratene Ziele freigeben; erneut suchen. **Abnahme:** Keine Schüsse außerhalb der erlaubten Reichweite und keine untätigen Drohnen wegen eines veralteten Ziels. Bezug: A15.
- [x] **U25 – Drohnen-Upgrades konsistent anwenden.** Festlegen, ob vorhandene Drohnen profitieren; bevorzugt entsprechend der bestehenden Beschreibung alle aktiven Drohnen aktualisieren. HP-Anpassung eindeutig definieren. **Abnahme:** Beschreibung, bestehende Drohnen und neu gebaute Drohnen entsprechen derselben Regel. Bezug: A16.

Stand 18.09.2026: U18–U20 und U22–U25 sind umgesetzt. Werte werden zwischen Basis- und Endwert begrenzt, Preise bleiben im gültigen `int`-Bereich; ungenutzte Level nach Erreichen des Endwerts wurden entfernt. Die Wellenzahl zählt nun tatsächliche Gegner (maximal 50), mit genau einem Boss in Welle 20, 30 usw. Deflected-Treffer werden korrekt markiert und jedes Projektil nur einmal verarbeitet. Manuelles Sammeln behält die Ursprungsskalierung. Turm und Drohnen verwerfen ungültige Ziele; aktive Drohnen erhalten HP- und Schadens-Upgrades, wobei bereits erlittener Schaden erhalten bleibt. 3 Edit-Mode- und 21 Play-Mode-Tests bestanden unter Android als Build-Ziel.

U21-Teilstand: AutoCollecting und der erste Drohnenslot kosten je 100 statt 850/250 Material; Schild und Drohnenmodul kosten 300/500 statt 800/1500. Die Drohnenbeschreibung nennt den nötigen Slotkauf. Schwere Gegner verbrauchen Schildpunkte entsprechend ihrer HP; Bosswellen sind planbar. Frühe Spielzeit, Verständlichkeit und Schwierigkeit brauchen weiterhin einen echten Playtest auf dem Pixel; U21 bleibt dafür offen.

## 6. Android-Bedienung und Feedback vervollständigen

- [ ] **U26 – HUD und Pause ergänzen.** Welle, Core-HP, Ressourcen und Pause klar anzeigen; Android-Zurück sinnvoll behandeln. **Abnahme:** Fortschritt und akute Gefahr sind ohne Öffnen des Modulmenüs erkennbar.
- [ ] **U27 – Kaufoberfläche verständlich machen.** Informationen auch bei Geldmangel zugänglich lassen; Kaufaktion und „aktuell → danach“ zeigen. MAX-Zustand, Rotationseinheit und negatives Vorzeichen beim Modulabzug korrigieren. Material-/Währungsbegriff vereinheitlichen. **Abnahme:** Ein neuer Spieler erkennt Wirkung und Kosten vor dem Kauf.
- [ ] **U28 – Touch, UI-Abgrenzung und Safe Area prüfen.** Sammelradius verbessern, UI-Touches nicht an die Welt weiterreichen und zielgerichtete Raycasts verwenden. Bedienung bei aktivem Schild und auf schmalen Displays testen. **Abnahme:** Keine unbeabsichtigten Weltaktionen, verdeckten Buttons oder unzugänglichen Stationselemente.
- [ ] **U29 – Kurze Einführung und visuelle Rückmeldung ergänzen.** Station antippen, bauen und sammeln kontextbezogen erklären. Textfarben beruhigen, dunkle Gegner besser abheben und Bau/Upgrade kurz hervorheben. **Abnahme:** Ein Erstspieler kann ohne mündliche Anleitung einen Run beginnen und ausbauen; der bestehende Look bleibt erhalten.
- [ ] **U30 – Audioeinstellungen und Audiolaufzeit korrigieren.** Musik und Effekte getrennt regeln und Einstellungen speichern. Audioobjekte unabhängig vom Spieltempo erst nach Wiedergabe entfernen. Hörtest durchführen. **Abnahme:** Keine abgeschnittenen Sounds bei hohem Tempo; Einstellungen bleiben nach Neustart erhalten.

Stand 18.09.2026 zu U26–U30: Eine dauerhafte Anzeige nennt Welle, Core-HP und Material; Pause/Android-Zurück sowie getrennte, gespeicherte Musik-/Effekt-Schalter sind implementiert. Upgrade-Infos bleiben auch ohne Material zugänglich und zeigen aktuelle und nächste Werte samt Preis. Material ist die einheitliche Währung; der Modulabzug im Score trägt ein Minuszeichen. Welt-Taps wählen gezielt Material oder Station. Zwei kontextbezogene Hinweise erklären den ersten Bau und das Sammeln. Soundeffekte bleiben unabhängig vom Spieltempo bis zum Ende der Wiedergabe bestehen. Ein Play-Mode-Test prüft Pause/Tempo. **U26–U30 bleiben bis zur visuellen Bedien- und Hörprüfung auf Android offen**; insbesondere Safe Area, Touch-Ziele, Lesbarkeit und Gegnerkontrast.

## 7. Gemessen optimieren und gezielt aufräumen

Teil der abschließenden 2–4 Tage. Erst messen, dann die nachgewiesenen Engpässe bearbeiten.

- [ ] **U31 – Android-Profil erstellen.** Frühe und späte Wellen auf einem schwächeren Gerät untersuchen: Frametimes, Speicher, Garbage Collection, Save-Zugriffe und längere Belastung. Zielwerte festlegen. **Abnahme:** Gerät, Szene/Welle, Tempo, Messwerte und relevante Engpässe sind dokumentiert.
- [ ] **U32 – Nötige Optimierungen umsetzen.** Je nach Messung Projektile/Effekte/Audio wiederverwenden, Physik-Layer eingrenzen und Musikimport anpassen. Upgrade-Symbole einmalig laden. **Abnahme:** Erneute Messung bestätigt die Verbesserung; Kampfverhalten bleibt korrekt.
- [ ] **U33 – Bildimporte und ungenutzte Assets prüfen.** Android-Texturgrößen und gegebenenfalls SpriteAtlas prüfen. Pivots bei transparenten Rändern erhalten. Alte Grafiken, Dummy und unbenutzte Dateien erst nach Referenzprüfung archivieren. **Abnahme:** Keine fehlenden Referenzen oder veränderten Stationspositionen.
- [x] **U34 – Projektkonfiguration und Dokumentation bereinigen.** Verwaiste Google-Adaptive-Performance-Assets behandeln; unnötige Pakete nur nach Prüfung entfernen. README-Szenenpfad korrigieren und Buildanleitung aktualisieren. **Abnahme:** Projekt und Build funktionieren mit dokumentiertem Setup. Bezug: A22.

Stand 18.09.2026 zu U31–U34: Die Upgrade-Symbole werden nur einmal aus `Resources` geladen. 64 PNG-Dateien unter `GameContent` wurden inventarisiert; die größten Spielgrafiken sind 2048 × 2048 px. Ohne Profiling werden weder Pooling noch Sprite-Importe breit umgebaut oder Assets gelöscht. Die verwaisten Google-Adaptive-Performance-Assets und ihre Build-Settings-Referenz sind entfernt; Samsung bleibt der aktive Loader. README-Szenenpfad und `BUILD.md` sind aktualisiert. Ein vollständiger Android-Development-Build aus der isolierten Projektkopie erzeugte ein APK mit `targetSdkVersion 36` und Version `1.2`/Code `3`. U31–U33 warten auf Gerätemessung und Referenzprüfung.

## 8. Release-Kandidat abnehmen

- [ ] **U35 – Gesamte Testmatrix ausführen.** Abnahmefälle der Projektanalyse prüfen: insbesondere Speichern/Laden, drei Replays, Modulverlust, Schildaufladung, Tempo, Upgradegrenzen und beschädigte Saves. **Abnahme:** Keine offenen P1-Fehler; verbleibende Einschränkungen sind dokumentiert.
- [ ] **U36 – Geräte- und Unterbrechungstests abschließen.** Unterschiedliche Seitenverhältnisse, Aussparungen, Hintergrundwechsel, App-Neustart, Android-Zurück und längeren Run testen. **Abnahme:** Bedienung bleibt erreichbar; Fortsetzen folgt der dokumentierten Checkpoint-Regel.
- [ ] **U37 – Veröffentlichungsmaterial vervollständigen.** Aktuelle Gameplay-Screenshots, kurze Beschreibung und Medienherkunft/Nutzungsnachweise zusammenstellen. Aktuelle Store-Vorgaben und vorhandenen Eintrag prüfen. **Abnahme:** Präsentation entspricht dem tatsächlichen Spiel und alle erforderlichen Angaben liegen vor.
- [ ] **U38 – Signierten Release-Kandidaten erstellen und prüfen.** Versionsnummer/Versioncode festlegen, Release-AAB bauen und über den vorgesehenen Testweg installieren. **Abnahme:** Der tatsächliche Release-Build besteht Start, Ausbau, Speichern/Fortsetzen und Replay.

Release-Teilstand: Die automatisierte Android-Ziel-Konfiguration besteht 22 Play-Mode- und 3 Edit-Mode-Tests. Der Android-Development-Build ist erfolgreich (APK, API 36). `RELEASE.md` enthält einen Store-Textentwurf, eine Medieninventur, aktuelle Google-Quellen und die konkrete Geräte-Testmatrix. U35–U38 bleiben bis zu Pixel-Abnahme, neuen Screenshots, Medienrechten, Signierung und Store-Testlauf offen.

## Abschlusskriterien

- [ ] Alle P1-Befunde aus der Analyse sind behoben und überprüft.
- [ ] Ein neuer Spieler versteht Start, Ausbau, Sammeln und Run-Ende ohne zusätzliche Erklärung.
- [ ] Spielstände, Score und Modulwirkungen bleiben über Unterbrechungen konsistent.
- [ ] Balancing und Darstellung enthalten keine ungültigen Werte oder ungebremsten Wellenmengen.
- [ ] Performance und Bedienung wurden auf echten Android-Geräten geprüft.
- [ ] Ein getesteter signierter Release-Kandidat samt aktueller Dokumentation liegt vor.

Planungsrahmen: ungefähr **10–16 konzentrierte Arbeitstage** bei unverändertem Umfang. Geräteprobleme oder zusätzliche Designwünsche können den Aufwand erhöhen. Erster Umsetzungsschritt: **U01**, danach **U02–U03** und der zusammenhängende Zustands-/Speicherblock **U04–U17**.
