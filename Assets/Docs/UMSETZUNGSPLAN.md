# Aegis Protocol – TODO

Stand: 19.09.2026 · Ziel: Android-Spiel im Hochformat fertigstellen.

Grundlage: [Projektanalyse](PROJEKTANALYSE.md).

## TODO

### Features

- [ ] Gegner- und Wellenkonzept aus [GEGNER_WELLENKONZEPT.md](GEGNER_WELLENKONZEPT.md) umsetzen: Rollen, Angriffspläne, Bosszyklen und fortlaufenden Run einführen.
- [x] Pro Gegnerrolle vier klar lesbare Schiffsdesigns zuweisen; Varianten 03 und 04 erscheinen schrittweise ab Welle 10 und 20.

### Balancing

- [ ] U21 – Frühe Wirtschaft, Gegnerrollen und Schwierigkeit auf dem Pixel balancieren.

### Geräte- und Qualitätstests

- [ ] U03 – Vollständigen Run im Editor und auf dem Pixel prüfen; sichtbare Fehler und Console-Meldungen festhalten.
- [ ] U12 – Android-Hintergrundwechsel und App-Neustart auf dem Gerät prüfen.
- [ ] U26 – HUD, Pause und Android-Zurück auf dem Gerät auf Lesbarkeit und Bedienung prüfen.
- [ ] U27 – Kaufoberfläche auf dem Gerät auf Verständlichkeit prüfen.
- [ ] U28 – Touch-Ziele, Safe Area und schmale Displays auf dem Gerät prüfen.
- [ ] U29 – Einführung, visuelle Rückmeldung und Gegnerkontrast auf dem Gerät prüfen.
- [ ] U30 – Musik, Soundeffekte und gespeicherte Audioeinstellungen auf dem Gerät prüfen.
- [ ] U35 – Gesamte Testmatrix vor dem Release-Kandidaten ausführen.
- [ ] U36 – Geräte- und Unterbrechungstests abschließen.

### Performance und Release

- [ ] U31 – Android-Profiling für frühe und späte Wellen durchführen.
- [ ] U32 – Nur die durch U31 belegten Performance-Engpässe beheben.
- [ ] U33 – Bildimporte, SpriteAtlas und ungenutzte Assets nach Referenzprüfung bereinigen.
- [ ] U37 – Store-Screenshots, Beschreibung und Mediennachweise fertigstellen.
- [ ] U38 – Signierten Release-AAB bauen, installieren und vollständig prüfen.

## Umgesetzt

- [x] Upgrade-UI im Editor vorbereitet: 16 Prefab-Varianten unter `Assets/GameContent/Prefabs/UI/Upgrades`, Einzel-Sprites unter `Assets/GameContent/Images/UI/UpgradeS` und alle Buttons als sichtbare Instanzen im `Canvas/UpgradePanel`. Links mittiges Vertical Layout; im Spiel werden nur die Upgrades des ausgewählten Moduls eingeblendet. Kein Erzeugen oder Löschen von Buttons zur Laufzeit. CommandUnit mit Geschütz-Drehsymbol, kompakter Zielanzeige, helleren Preisen und kürzerer Beschreibung; MAX neutral statt rot.
- [x] Einheitliche Textformatierung: Cyan-Titel, hervorgehobene Werte, grüne Verbesserungen, rote Materialdefizite und kleinere Hinweise; verständliche Upgrade-Namen und klarere Statistik-Gruppen.
- [x] UI-Texte gekürzt; Modul- und Upgrade-Infos mit begrenzter Breite und Zeilenumbruch, kompakteres HUD, gekürzte Zahlen auf Kaufbuttons und Statistik ohne breite Tabulator-Abstände.
- [x] Structural Integrity zum Core verschoben; vorhandene HP-Upgrades bleiben beim Laden erhalten.
- [x] CommandUnit behält Drehgeschwindigkeit und bietet Zielpriorisierung: einmalig 40 Material, danach kostenlos zwischen nächstem Gegner, Artillery und stärkstem Gegner wechseln. Auswahl bleibt bei Modulverlust gespeichert.
- [x] Drohnenmodul erzeugt beim Bau eine Startdrohne; insgesamt vier Plätze, keine zusätzliche Startdrohne beim Laden oder Wiederaufbau mit vorhandenen Drohnen.
- [x] Radar gibt beim Bau sofort 3,0 statt 2,5 Reichweite; 16 Upgrades mit je +0,1 bis 4,6. Ältere Upgrade-Level werden beim Laden begrenzt.
- [x] Upgrade-Infos unterscheiden dauerhafte Verbesserungen und Funktionen, die ein aktives Modul benötigen.
- [x] Frühen Runner-Einstieg entschärft: Einzelgegner in Welle 2, Zweiergruppen in Welle 3.
- [x] Wellenbonus eingeführt: 15 Material nach Welle 1, danach +3 pro Welle; direkte Gutschrift und HUD-Rückmeldung.
- [x] Upgrade-Rahmen zeigen die Bezahlbarkeit dauerhaft: rot bei fehlendem Material, weiß bei bezahlbaren Upgrades.
- [x] Feste Angriffspläne für Welle 1–20 hinterlegt; danach läuft der Run weiter.
- [x] U01 – Unity auf `6000.3.15f1` aktualisiert und Android-Start auf dem Pixel geprüft.
- [x] U02 – Gemeinsamen Touch- und Maus-Eingabepfad umgesetzt.
- [x] U04 – Eindeutigen NewRun- und Replay-Ablauf umgesetzt.
- [x] U05 – Upgrade-Vorlagen vom Laufzeitzustand getrennt.
- [x] U06 – Statische Registrierungen und Bereinigung korrigiert.
- [x] U07 – Modulverlust-Regeln festgelegt und zentral angewendet.
- [x] U08 – Modulvorschau vom Kampf ausgeschlossen.
- [x] U09 – Bau, Reparatur und Upgrades als vollständige Aktionen abgesichert.
- [x] U10 – Zeitsteuerung vereinheitlicht.
- [x] U11 – Schildzustand an den Modulzustand gekoppelt.
- [x] U13 – Save-Load-Ablauf ohne Seiteneffekte umgesetzt.
- [x] U14 – Statistik vollständig gespeichert.
- [x] U15 – Dateizugriff mit Validierung, Backup und gebündelten Saves abgesichert.
- [x] U16 – Wellen-Checkpoint umgesetzt.
- [x] U17 – Regressionstests ergänzt.
- [x] U18 – Upgrade-Grenzen und Kosten korrigiert.
- [x] U19 – Balancing-Werkzeuge und Upgrade-Metadaten repariert.
- [x] U20 – Wellenbudget begrenzt und Bosswellen definiert.
- [x] U22 – Ablenkung und Trefferstatistik korrigiert.
- [x] U23 – Manuellen Sammelflug repariert.
- [x] U24 – Zielgültigkeit für Turm und Drohnen korrigiert.
- [x] U25 – Drohnen-Upgrades konsistent umgesetzt.
- [x] U34 – Projektkonfiguration und Build-Dokumentation bereinigt.
- [x] HUD mit Welle, Core-HP und Material ergänzt.
- [x] Pause-Menü mit Replay-Stil, Exit sowie Musik- und Sound-Schaltern ergänzt.
- [x] Wellenwarnung mit Grid-Verzerrung und rotem Ring animiert.
- [x] Android-APK gebaut und in Google Drive bereitgestellt.
