# Match Reporter

Jede gestartete Spielsession zeichnet automatisch lokal auf; an der Szene muss nichts eingerichtet werden. Es werden keine Daten hochgeladen. Vorhandene Reports bleiben erhalten.

Im Unity-Editor: **Aegis > Match Reports > Open Latest Analysis** oeffnet die neueste Analyse. **Open Reports Folder** oeffnet alle Aufzeichnungen. Der genaue Ordner erscheint beim Start auch in der Console als `Match report: ...`.

Speicherort: `Application.persistentDataPath/MatchReports/<UTC-Zeit>-<ID>/`. Unter Windows normalerweise `%USERPROFILE%/AppData/LocalLow/WRB.Studio/Aegis Protocol/MatchReports`. Android verwendet den lokalen persistentDataPath der App. PlayMode-Tests verwenden das bestehende isolierte Save-Verzeichnis.

## Dateien

- `analysis.md`: lesbarer Bericht mit Sessionkennzahlen, Trefferquoten, Gegnern nach Typ/Entfernungursache, Wellentabelle, kritischen Ereignissen und Analysehinweisen.
- `summary.json`: strukturierter Bericht (Schema-Version 1) mit Anfangs-/Endzustand, Wellen und Entscheidungsverlauf.
- `events.jsonl`: ein vollstaendiges JSON-Objekt pro Zeile. Fuer eine Untersuchung diese Datei zusammen mit dem Bericht bereitstellen.

Ereignisse enthalten Sequenznummer, UTC-Zeit, skalierte Spielzeit, Wellennummer, Art, Akteur, Ziel, Wert, Restwert, Position und Details. Gegner und Projektile erhalten ihre Unity-Instance-ID fuer die Zuordnung innerhalb einer Session. Gegner-Spawns dokumentieren HP, Geschwindigkeit, Schaden, Feuerrate und Reichweite. Aufgezeichnet werden Spawns, Gegner-Schaden und Entfernungursache, Schuesse, Treffer, Reflexionen, Modul-/Drohnenverluste, Schildschaden und Ausfaelle, Modulbau, Reparaturen, Upgrades mit Kosten/Level/Wert, Materialeinnahmen/-ausgaben, Wellenboni, Zeitmodulation, App-Pausen, Selbstzerstoerung sowie Unity-Warnungen/Fehler inklusive Stacktrace.

Snapshots alle fuenf Spielsekunden dokumentieren Statistiken, Material, aktive Gegner/Drohnen, Modul-HP und Bauzustand, Upgradelevel/-werte/-kosten, Schildkapazitaet/-zustand/-Recharge und Zeitfaktor. Schuesse und Treffer sind ueber die Projektil-ID verknuepft; `enemy_fired` verbindet Gegner und Projektil. Bei Modulbau/Reparatur ist `value` der Preis, `remaining` die HP; bei Upgrades ist `remaining` das neue Level. Bei Schaden ist `remaining` die verbleibende HP/Kapazitaet. Bei Gegner-Spawns ist `value` die maximale HP und `remaining` die Geschwindigkeit.

## Auswertung und Grenzen

Berichte werden beim Start, nach abgeschlossenen Wellen, beim Hintergrundwechsel und beim Matchende geschrieben. Die Ereignisdatei wird alle fuenf Echtzeitsekunden und an Checkpoints geleert. Bei einem harten Prozessabbruch kann der letzte Puffer fehlen; die letzte Zusammenfassung kann aelter als das Ereignisprotokoll sein. Schreibfehler werden gemeldet und blockieren das Spiel nicht. Ereignisse werden fortlaufend geschrieben, statt alle Kampfereignisse im RAM zu sammeln. Alte Reports werden nicht automatisch geloescht und koennen im Report-Ordner manuell entfernt werden.

Ein geladenes Save startet eine **neue, als Fortsetzung markierte Session**. Die vorhandene Save-Logik startet eine unterbrochene Welle erneut; deshalb werden separate Sessions nicht zu einem vermeintlich lueckenlosen Match zusammengefuegt. Analysekennzahlen sind Differenzen gegen den geladenen Anfangszustand. Bei Replay entsteht ein neuer Ordner. `game_over`, `self_destruct`, `restarted`, `session_closed` und `app_quit` unterscheiden die Endgruende; `in_progress` ist ein Zwischenbericht.

Stationskollisionen sind Durchbrueche, obwohl die bestehende Spielstatistik sie unter Kills zaehlt. Modul-/Schildschaden folgt den bestehenden Countern und enthaelt angeforderten Schaden inklusive Overkill; `enemy_damage.value` erfasst tatsaechlich abgezogene HP. Trefferquoten koennen durch noch fliegende Projektile verzerrt sein. Der Bericht weist auf beobachtete Staerken, schadensreiche Wellen und ungenutztes Material hin; er behauptet keine bewiesene Ursache fuer eine Niederlage. Fuer die Ursachenanalyse die betreffenden Schadensereignisse mit Modulzustaenden, Kaufzeitpunkten und Kosten vergleichen.

EditMode-Tests: `MatchReporterTests` prueft Dateiformat und Chronologie, finale Kollisionsstatistiken, Sessiondifferenzen bei Fortsetzung, Wellen-Checkpoints, Warnungen, Replay-Trennung und mehrfachen Abschluss.
