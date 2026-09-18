# Aegis Protocol – Projektanalyse und Weg zur Android-Fertigstellung

Stand: 18.09.2026 · untersuchter Commit: `b0c503d` · Ziel laut Entwickler: vorhandenes Android-Spiel fertigstellen.

Nachtrag: Der Entwickler hat das Projekt nach dieser Untersuchung auf Unity `6000.3.15f1` aktualisiert. Die Version ist in `ProjectSettings/ProjectVersion.txt` bestätigt. Die folgenden Prüfergebnisse beziehen sich auf den ursprünglichen Analysestand; ein erneuter vollständiger Android-Build und Gerätetest sind noch offen.

## 1. Einschätzung

Das Projekt hat einen klaren, erhaltenswerten Kern: Eine kleine Raumstation wächst sichtbar zu einer Verteidigungsanlage, während automatische Waffen und Drohnen die Gegner abwehren. Station, Schild, Ausbau und Gegner sind bereits umgesetzt; auch Musik, Effekte, Statistiken und Veröffentlichungsbilder existieren. Der Umfang ist klein genug für eine gezielte Fertigstellung.

Der größte Engpass ist die Verlässlichkeit des Spielzustands. Laden, Modulzerstörung, Wiederaufbau, Speichern und Neustart behandeln denselben Zustand unterschiedlich. Dazu kommen stark wachsende Wellen und Upgrade-Kosten sowie eine Bedienoberfläche, die viel Vorwissen voraussetzt.

**Empfehlung: Bestehende Systeme reparieren, wenige Verantwortlichkeiten klären und die ersten Spielminuten verbessern. Den visuellen Stil und den vorhandenen Spielumfang beibehalten.** Ein kompletter Rewrite würde hier wenig zusätzlichen Nutzen liefern.

## 2. Umfang und Aussagekraft der Untersuchung

Untersucht wurden alle 30 eigenen Laufzeitskripte mit zusammen 4.913 Zeilen, alle fünf Editor-Skripte mit 1.188 Zeilen, die einzige Szene, sämtliche 17 Prefabs, die Upgrade-Daten, Projekt- und Paketkonfiguration, Asset-Metadaten sowie Referenzen zwischen Assets. Die eigenen PNG-Assets wurden über Bildübersichten gesichtet; die vier vorhandenen Gameplay-Screenshots und zentrale Veröffentlichungsbilder zusätzlich groß betrachtet. Audio wurde hinsichtlich Dateien, Laufzeiten, Import und Verwendung geprüft, nicht durch einen Hörtest.

Die Bilder zeigen gespeicherte Aufnahmen, keinen aktuell ausgeführten Spielstand. Spielgefühl, tatsächliche Bildrate, Touch-Trefferquoten und Audiomischung sind daher noch auf einem Android-Gerät zu prüfen.

Die C#-Prüfung erfolgte isoliert mit lokalem Roslyn und Referenzen aus der vorhandenen Unity-Installation `6000.3.15f1` sowie dem Paketcache. Die damals im Projekt eingetragene Version `6000.0.59f2` fehlte am generierten Installationspfad. Während der Analyse wurde das Projekt nicht mit einer anderen Editorversion geöffnet oder migriert; das spätere Update durch den Entwickler ist im Nachtrag vermerkt.

| Prüfung | Ergebnis |
|---|---|
| Laufzeitcode mit Android-Editor-Symbolen | Kompiliert |
| Laufzeitcode mit Android-Player-Symbolen, ohne `UNITY_EDITOR` | Kompiliert |
| Fünf eigene Editor-Skripte | Kompilieren |
| Laufzeitcode mit Standalone-Symbolen | Vier Compilerfehler in `UIManager.cs` |
| GUID-Prüfung serialisierter Assets gegen Assets und Paketcache | Zwei nicht auflösbare Script-GUIDs in alten Google-Adaptive-Performance-Assets |
| Vollständiger Unity-/IL2CPP-Build, Installation, Playtest, Profiler | Nicht durchgeführt |

Eine erfolgreiche C#-Kompilierung bestätigt weder den Android-Build noch die Laufzeitfunktion. Compilerwarnungen über nicht im Code gesetzte Inspector-Felder wurden nicht als fehlende Szenenreferenzen fehlinterpretiert. Die relevanten Referenzen der eigenen Manager sind in der Szene gesetzt; Nullwerte betreffen hier überwiegend bewusst erst zur Laufzeit gefüllte Felder.

## 3. Bestand und Spielablauf

| Bereich | Vorhandener Stand |
|---|---|
| Engine / Rendering | Unity 6, klassischer 2D-Aufbau mit SpriteRenderern und uGUI; kein URP-Paket im Manifest |
| Szene | `Assets/Scenes/MainScene.unity`, 131 GameObjects, einzige aktivierte Build-Szene |
| Plattform | Android, Hochformat, Legacy Input, IL2CPP, ARMv7 und ARM64 konfiguriert |
| Version | Projektversion `1.2`, Android-Versioncode `3`, Minimum SDK `23`, Target SDK auf automatisch |
| Start | 500 Material, Core gebaut, automatische zentrale Waffe |
| Ausbau | Sieben kaufbare Modularten neben dem Core; zusätzlich ein inaktives ungenutztes Modul `M6-` |
| Upgrades | 15 Upgrade-Arten, acht UpgradeSet-Dateien einschließlich unzugewiesenem Core-Set |
| Gegner | Normal, Fast, Tank, Swarm, Ranged, Boss; zusätzlich Dummy-Prefab |
| Kampf | Zentraler Turm, bis zu vier Drohnen-Slots, Schild, ablenkbare Projektile |
| Wirtschaft | Materialdrops, manuelles Einsammeln, freischaltbare Automatik, Modulbau und Reparaturen |
| Fortschritt | Prozedurale Endloswellen, lokale Spielstände, Highscore, detaillierte Run-Statistik |
| Audio | Drei Musikdateien und sieben Effektdateien; zwei Musikstücke und sechs Effekte über Szenenreferenzen erreichbar |
| Entwicklungshilfen | Modul-Editor, Upgrade-Editor, Kosten-/Wertevorschau und JSON-Presets |
| Builds | APK vom 08.02.2026: rund 43,7 MiB; AAB vom 16.02.2026: rund 45,3 MiB |

Der tatsächliche Ablauf ist: Gegner töten → Material sammeln → Module und Upgrades kaufen → größere Wellen überleben → Core verlieren → Punktzahl ansehen → erneut spielen. Es gibt im untersuchten Code kein Siegziel und keine Kampagne. Das ist für ein Survival-Spiel vollkommen ausreichend, muss aber im Spiel verständlich kommuniziert werden.

Die README nennt zum Öffnen einen nicht vorhandenen Szenenordner `Assets/GameContent/Scenes`. Der korrekte Einstieg ist die oben genannte MainScene. Die letzte sichtbare Gameplay-Änderung im Git-Verlauf stammt vom Februar 2026; die September-Commits betreffen die Dokumentation.

## 4. Was bereits gut gelöst ist

- **Visuell sichtbarer Fortschritt:** Neue Module verändern die Station unmittelbar. Das gibt der ansonsten zahlengetriebenen Progression einen erkennbaren Gegenwert.
- **Klarer Schwerpunkt:** Die zentrale Verteidigung passt zum Hochformat und benötigt wenig direkte Steuerung.
- **Sinnvolle Systemgrenzen:** Gegner, Projektile, Ressourcen, Drohnen, Upgrades und UI haben bereits eigene Dateien. Die Grundstruktur ist verständlich.
- **Datengetriebene Upgrades:** ScriptableObjects und bestehende Balancing-Werkzeuge sind eine gute Grundlage. Die Trennung zwischen Vorlagen und Laufzeitzustand muss vervollständigt werden.
- **Ausgearbeitete Details:** Schildaufladung, Ablenkung, Geschwindigkeitssteuerung, Modulauswahl mit Verbindungslinie, Musikwechsel und Statistikansicht sind bereits vorhanden.
- **Beherrschbare Größe:** Es gibt keinen Anlass, ein großes Framework, Dependency Injection, ECS oder eine allgemeine Event-Architektur einzuführen.
- **Repository-Grundlagen:** Buildprodukte, Library und IDE-Dateien werden sinnvoll ignoriert. Die drei lokalen Solution-Dateien sind generierte, ignorierte Dateien und kein dringender Repository-Ballast.

## 5. Probleme, die vor einer Veröffentlichung behoben werden sollten

Priorität P1 bedeutet: vor Release beheben. P2 bedeutet: nach der Zustandsstabilisierung bearbeiten. „Belegt“ meint durch Code, Daten oder Compiler bestätigt; es bedeutet nicht, dass das Verhalten bereits am Gerät reproduziert wurde.

### P1 · A01 – Android-Pause speichert nicht über die vorgesehenen Callbacks

`OnApplicationPause` und `OnApplicationQuit` stehen in der serialisierbaren Datenklasse `SaveGame`, die kein MonoBehaviour ist. Unity ruft diese Methoden dort nicht als Lifecycle-Callbacks auf. Andere Ereignisse speichern häufig, aber Hintergrundwechsel selbst ist nicht zuverlässig abgedeckt. Fundstelle: [SaveGame.cs:56](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/GameContent/Scripts/SaveAndLoading/SaveGame.cs:56).

Die Callbacks gehören in `SaveGameManager`, mit Schutz gegen einen noch nicht initialisierten oder bereits beendeten Run. Diese Zuordnung folgt auch der [Unity-Dokumentation zu OnApplicationPause](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MonoBehaviour.OnApplicationPause.html).

### P1 · A02 – Laden verändert Lebenspunkte und speichert Zwischenzustände

Beim Laden werden Modul-HP gesetzt, bevor die aus Upgrades berechneten maximalen HP angewendet werden. `StructuralIntegrity` interpretiert die gespeicherten HP dann relativ zum noch alten Maximum von drei HP. Beispiel: Bei einem gekauften Integritätslevel ergibt sich gerundet ein Maximum von fünf HP. Gespeicherte drei von fünf HP werden beim Laden als drei von drei verstanden und auf fünf hochgerechnet: Schaden verschwindet.

Außerdem ruft das Wiederherstellen von Drohnen `SpawnDrone()` auf, das sofort `Save()` ausführt. Damit wird die Save-Datei schon während des Ladens mit einem teilweise wiederhergestellten Zustand überschrieben. Anschließend überschreibt `Drone.Start()` die geladenen Drohnen-HP mit dem Maximum. Das Verhalten von `Start` ist in der [Unity-Dokumentation](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/MonoBehaviour.Start.html) beschrieben.

Fundstellen: [SaveGameManager.cs:110](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/GameContent/Scripts/SaveAndLoading/SaveGameManager.cs:110), [UpgradeAttribute.cs:178](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/GameContent/Scripts/Upgrading/UpgradeAttribute.cs:178), [DroneManager.cs:107](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/GameContent/Scripts/Drones/DroneManager.cs:107), [Drone.cs:28](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/GameContent/Scripts/Drones/Drone.cs:28).

Kleinste robuste Lösung: Laden als zusammenhängenden Vorgang behandeln. Erst Module und Upgradelevel lesen, dann abgeleitete Maximalwerte berechnen, danach gespeicherte aktuelle HP begrenzen und setzen. Drohnen explizit initialisieren. Währenddessen nicht speichern und keine Kauf-/Bau-Statistik erzeugen.

### P1 · A03 – Kill-Statistik und ein Teil der Run-Daten gehen beim Laden verloren

`killsByTypeAndCause` wird nicht gespeichert und beim Laden geleert. `GetTotalKills()` verwendet genau dieses Dictionary; damit verschwinden auch die bisherigen Kill-Punkte des laufenden Runs. `resourcesSpawned` und `modulesCost` fehlen ebenfalls in `StatsData`.

Fundstelle: [Stats.cs:177](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/GameContent/Scripts/Stats.cs:177). Die bestehende Datenklasse um serialisierbare Kill-Einträge und die fehlenden Zähler ergänzen. Eine Liste genügt; kein neues Speichersystem nötig.

### P1 · A04 – Zerstörte Module liefern nach Laden oder fremdem Wiederaufbau falsche Effekte

Zerstörung verändert `currentValue`, gespeichert wird aber nur `level`. Beim Laden stellt `RecalculateFromLevel()` den vollen Wert wieder her. `wasDestroyed` wird zwar geladen, aber nicht zur Ableitung der Upgrade-Wirkung verwendet. Beim Wiederaufbau eines beliebigen zerstörten Moduls ruft die UI zusätzlich `OnModulRebuildAll()` auf und stellt damit auch die Werte anderer zerstörter Module wieder her.

Besonders eindeutig: Das AutoCollecting-Upgrade aktiviert die Sammelautomatik bei Level eins ohne Prüfung, ob der Extractor gebaut ist. Auch der Kauf eines anderen Moduls wendet alle Effekte erneut an und kann die nach Extractor-Zerstörung abgeschaltete Automatik wieder aktivieren.

Fundstellen: [UpgradeAttribute.cs:191](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/GameContent/Scripts/Upgrading/UpgradeAttribute.cs:191), [UpgradeAttribute.cs:261](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/GameContent/Scripts/Upgrading/UpgradeAttribute.cs:261), [ModulesUI.cs:367](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/GameContent/Scripts/UI/ModulesUI.cs:367).

Zunächst eine Regel festlegen: Welche Grundfunktion bleibt ohne Modul, welche Erweiterung fällt aus? Dann wirksame Werte immer aus Vorlage, Level und Modulstatus berechnen. Den Zustand nicht durch wiederholtes Halbieren und anschließendes Reparieren verändern. Turm-Grundwerte und Modulboni müssen dabei dieselbe Regel benutzen.

### P1 · A05 – Reset und Replay haben widersprüchliche Verantwortlichkeiten

GameOver löscht den Spielstand nicht nur: `DeleteSaveData()` setzt sofort die Welt zurück und schreibt einen neuen Save. Replay setzt dieselben Systeme nochmals zurück. Die Reihenfolge kommt aus einer unsortierten Objektsuche. Einige Resets beeinflussen andere Systeme, etwa Gegnerabbau die Kill-Statistik oder UI-Refresh den Modulzustand.

Zusätzlich klont `UpgradeManager.ResetScript()` die bereits verwendeten Runtime-Sets. Die neuen UpgradeAttribute haben keine gespeicherten Start-Snapshots; das anschließende `UpgradeAttribute.ResetAll()` setzt deshalb deren Snapshot-Felder auf die Standardwerte null beziehungsweise nullwertige Zahlen zurück. Replay berechnet später wieder neu – unterschiedliche Abläufe verwenden also unterschiedliche Zustandsquellen.

Fundstellen: [GameManager.cs:127](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/GameContent/Scripts/GameManager.cs:127), [GameManager.cs:295](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/GameContent/Scripts/GameManager.cs:295), [UpgradeManager.cs:13](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/GameContent/Scripts/Upgrading/UpgradeManager.cs:13), [SaveGameManager.cs:50](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/GameContent/Scripts/SaveAndLoading/SaveGameManager.cs:50).

Empfehlung: GameOver beendet den Run und zeigt dessen Ergebnis. Genau ein expliziter Ablauf startet einen neuen Run aus unveränderten Vorlagen, entfernt alte Objekte ohne Statistik-Nebeneffekte, setzt Werte und aktualisiert zuletzt die UI. `DeleteSaveData` sollte nicht nebenbei die gesamte Welt orchestrieren. Falls stattdessen Szenen-Neuladen gewählt wird, müssen zuvor statische Listen und das persistente SaveGameManager-Objekt sauber behandelt werden; momentan ist das kein gefahrloser Einzeiler.

### P1 · A06 – Ungebaute Modulvorschau nimmt am Kampf teil

Die Modulauswahl aktiviert das echte Modul-GameObject auch dann, wenn `isBuilt` false ist. Die Szene enthält daran aktive Collider. Gegner und Projektile prüfen vor `TakeDamage` nicht, ob das Modul gebaut ist. Eine Vorschau kann dadurch Treffer abfangen, zerstört werden und weitere Zerstörungseffekte auslösen.

Fundstellen: [ModulesUI.cs:143](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/GameContent/Scripts/UI/ModulesUI.cs:143), [StationModule.cs:59](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/GameContent/Scripts/Station/StationModule.cs:59). Für die Vorschau Collider deaktivieren und Schaden an ungebaute/tote Module grundsätzlich abweisen.

### P1 · A07 – Speichern ist häufig, synchron und nicht gegen beschädigte Dateien abgesichert

Jeder eingesammelte Drop, viele Treffer, Ausgaben und Upgrade-Effekte können den gesamten Weltzustand als formatiertes JSON synchron schreiben. Es gibt weder atomaren Austausch noch Backup/Fallback oder Behandlung von Lese-/Schreibfehlern. Das Versionsfeld existiert, wird beim Laden aber nicht ausgewertet.

Bei Reparaturen wird durch `SpendMaterial()` gespeichert, bevor HP aufgefüllt werden; anschließend fehlt ein eigener Save. Wird die App dann beendet, kann Material bezahlt, die Reparatur aber nicht gespeichert sein. Bei anderen Aktionen gibt es ähnliche unnötige Zwischen-Saves.

Fundstellen: [SaveGameManager.cs:24](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/GameContent/Scripts/SaveAndLoading/SaveGameManager.cs:24), [ResourceManager.cs:125](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/GameContent/Scripts/Resources/ResourceManager.cs:125), [ModulesUI.cs:339](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/GameContent/Scripts/UI/ModulesUI.cs:339).

Änderungen erst vollständig durchführen, dann den Save als geändert markieren. Häufige Ereignisse bündeln; an definierten Checkpoints und beim Hintergrundwechsel schreiben. Temporäre Datei, Austausch, Validierung und ein verständlicher Fehlerpfad reichen. Die Menge der aktuell beobachtbaren Ruckler wurde nicht gemessen.

### P1 · A08 – Fortsetzen besitzt keinen klaren Wellen-Checkpoint

Gespeichert wird nur `currentWaveIndex`, nicht der laufende Spawn-Ablauf oder die aktiven Gegner. Der Index erhöht sich bereits nach Ende des Spawnens, während noch Gegner leben können. Nach dem Laden startet die Welt ohne diese Gegner und kann die Welle als abgeschlossen zählen. Während des Spawnens gespeicherte Wellen werden dagegen neu begonnen.

Fundstellen: [EnemySpawner.cs:70](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/GameContent/Scripts/Enemy/EnemySpawner.cs:70), [SaveGame.cs:17](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/GameContent/Scripts/SaveAndLoading/SaveGame.cs:17).

Für die schnelle Fertigstellung ist ein vollständiger Snapshot an Wellengrenzen am einfachsten: Beim Fortsetzen dieselbe zuletzt begonnene Welle mit ihrem Startzustand wiederholen. Dazu müssen Ressourcen, Käufe und Statistik zum selben Checkpoint gehören. Falls exaktes Fortsetzen mitten im Kampf gewünscht ist, reicht das derzeitige Format nicht.

### P1 · A09 – Upgrade-Daten und Berechnung stimmen nicht überein

`CalculateValue()` verwendet ausschließlich `baseValue + level * upgradeValue`; `maxValue` bleibt unberücksichtigt. Die Kostenformel wächst exponentiell, die UI konvertiert anschließend in einen 32-Bit-Integer.

| Upgrade | Wert auf maximalem Level laut Formel | Konfigurierter Grenzwert | Zusätzlicher Befund |
|---|---:|---:|---|
| Tower Damage | 115 | 100 | Nächster Preis über `int.MaxValue` ab gekauftem Level 23 |
| Drone HP | 115 | 100 | Text verspricht Wirkung auf alle Drohnen, bestehende werden nicht aktualisiert |
| Drone Build Time | −12,5 s | 2,5 s | Runtime begrenzt separat auf 0,05 s; Anzeige und tatsächliche Bauzeit weichen ab |
| Collecting Efficiency | 126 | 100 | Nächster Preis über `int.MaxValue` ab Level 34 |
| Fire Range | 5 | 4,6 | Grenzwert wird überschritten |
| Fire Rate | 11,5 | 15 | Konfiguration widerspricht erreichbarem Endwert |
| Time Multiplier | 3,5 | 4 | Konfiguration widerspricht erreichbarem Endwert |

Fundstelle: [UpgradeAttribute.cs:300](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/GameContent/Scripts/Upgrading/UpgradeAttribute.cs:300). Der exakte Konvertierungseffekt außerhalb des Integer-Bereichs wurde nicht am Zielgerät bestimmt; die unzulässige Zahlenbereichsüberschreitung ist belegt.

Werte einschließlich fallender Bau-/Aufladezeiten begrenzen, realistische Maximallevel definieren und bezahlbare Kostenkurven festlegen. Mehr Stellen durch einen größeren Zahlentyp zu ermöglichen würde das Spielbalance-Problem allein nicht lösen.

### P1 · A10 – Wellen wachsen viel stärker als die vermeintliche Gegnergrenze

`enemyCount` ist die Zahl der Spawn-Anweisungen. Jede Anweisung spawnt zufällig 1 bis `3 + waveIndex` Gruppen, beim Schwarm jeweils mehrere Gegner. Der Deckel von 50 begrenzt daher nicht die Gegnerzahl.

Aus den Codeformeln berechnete Erwartungswerte, ohne Kampfende und bei Spieltempo x1:

| Wellenindex, nullbasiert | Erwartete Gegnerzahl über die gesamte Welle | Ungefähre reine Spawnzeit |
|---|---:|---:|
| 0 | 4 | 4 s |
| 10 | 98 | 66 s |
| 20 | 593 | 199 s |
| 30 | 1.421 | 405 s |
| 40 | 2.585 | 683 s |

Das sind keine gemessenen gleichzeitig aktiven Gegnerzahlen und keine FPS-Messungen. Sie zeigen trotzdem, warum Runs sehr lange und Wellen schwer steuerbar werden können. Bossgegner werden erst ab Index 17 zufällig gewählt und dann ebenfalls gruppenweise gespawnt. Ihre Basiskonfiguration besitzt fünf HP; ein Tank hat zwei HP und sogar höheren Kollisionsschaden. Der Boss hat kein eigenes Verhalten.

Fundstelle: [EnemySpawner.cs:182](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/GameContent/Scripts/Enemy/EnemySpawner.cs:182). Ein festes Gesamtbudget je Welle, klar gesetzte Einführung neuer Gegnertypen und ein expliziter Boss-Termin sind leichter zu balancieren. Dafür genügt eine kleine Tabelle beziehungsweise ein einfaches WaveConfig-Asset.

### P1 · A11 – Tempo kann nach Tippen, Modulverlust und Replay falsch sein

`OnStationUIClose()` stellt stets `storedBeforeStationUI` wieder her. `UIManager` ruft Schließen aber auch beim Tippen ins freie Spielfeld auf, wenn das Menü gar nicht offen ist. Nach einer Änderung auf x2 kann ein solcher Tipp deshalb den älteren gespeicherten Wert x1 herstellen, während `current` beziehungsweise die Anzeige etwas anderes enthalten.

Bei Verlust des Zeitmoduls wird der gespeicherte Rückkehrwert nicht mitkorrigiert. `ResetScript()` aktualisiert lediglich das Panel und setzt die internen Zeitwerte nicht zurück.

Fundstellen: [TimeController.cs:48](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/GameContent/Scripts/TimeController.cs:48), [TimeController.cs:78](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/GameContent/Scripts/TimeController.cs:78), [UIManager.cs:50](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/GameContent/Scripts/UI/UIManager.cs:50). Nur echte Öffnen-/Schließen-Übergänge verarbeiten und eine einzige Stelle das effektive Tempo setzen lassen.

## 6. Weitere konkrete Fehler und technische Risiken

| ID / Priorität | Befund und kleinste Korrektur |
|---|---|
| A12 / P1 | **Schild kehrt ohne Modul zurück:** `deactivateShield()` löscht den laufenden Recharge-Countdown nicht; `RechargeHandling()` aktiviert nach Ablauf ohne Modulprüfung. Nach Schildkollaps das Modul verlieren und prüfen. [Shield.cs:104](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/GameContent/Scripts/Station/Shield.cs:104) |
| A13 / P2 | **Abgelenkte Treffer falsch zugeordnet:** Beim Ablenken wird der Tag geändert, aber `isDeflected` nie true gesetzt. Treffer zählen zum Turm statt zur Ablenkung. [Projectile.cs:72](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/GameContent/Scripts/Projectile/Projectile.cs:72) |
| A14 / P2 | **Manuelle Sammelanimation verschwindet:** `originalScale` wird nur beim automatischen Spawn gesetzt; manuell aktivierte Drops erhalten durch `setOriginScale()` den Standardvektor null. Material kann weiterhin ankommen, aber die Darstellung verschwindet. [CollectEffect.cs:56](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/GameContent/Scripts/Resources/CollectEffect.cs:56) |
| A15 / P2 | **Ziele werden nicht neu bewertet:** Turm und Drohnen suchen erst bei null ein neues Ziel. Der Turm prüft beim Schießen die Reichweite nicht erneut; Drohnen behalten auch ein außer Reichweite geratenes lebendes Ziel und können dadurch untätig bleiben. Zielgültigkeit samt Reichweite regelmäßig prüfen. [Tower.cs:54](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/GameContent/Scripts/Station/Tower.cs:54), [Drone.cs:40](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/GameContent/Scripts/Drones/Drone.cs:40) |
| A16 / P2 | **Drohnen-Upgrades betreffen nur neue Drohnen:** HP und Damage werden nur in `Start()` aus dem Manager gelesen. Beschreibungen versprechen alle Drohnen. Bestehende Werte bewusst aktualisieren oder Beschreibung/Design ändern. [Drone.cs:28](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/GameContent/Scripts/Drones/Drone.cs:28) |
| A17 / P1 | **Kaufprüfung liegt zu stark in der UI:** `BuySelectedModule()` ignoriert den Rückgabewert von `SpendMaterial()`. Sinkt das Material nach Anzeige, kann die Aktion trotzdem bauen. Käufe an einer Stelle prüfen und nur bei erfolgreicher Zahlung anwenden; auch `isBuilt` und nichtnegative Preise prüfen. [ModulesUI.cs:352](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/GameContent/Scripts/UI/ModulesUI.cs:352) |
| A18 / P2 | **Kostenwerkzeug teilweise wirkungslos:** Der Upgrade-Editor sucht `costStep`, das Datenfeld heißt `costMultiplier`. Der entsprechende Multiplikator ändert nichts. Die Levelvorschau zeigt außerdem für Zeile 1 den Basiswert, obwohl Level 1 im Spiel bereits einen Schritt erhöht. [UpgradeSetEditorWindow.cs:259](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/Editor/UpgradeSetEditorWindow.cs:259), [UpgradeProgressPreviewWindow.cs:145](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/Editor/UpgradeProgressPreviewWindow.cs:145) |
| A19 / P2 | **Editor-Maussteuerung unvollständig:** Standalone-Zweig enthält undefinierte Namen und alte `Show`-Aufrufe: drei CS0103 und ein CS1501. Ressourcen verwenden ausschließlich Touch. Für schnelle Android-Entwicklung einen funktionierenden Maus-Testpfad ergänzen; das ist kein Auftrag für einen PC-Port. [UIManager.cs:78](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/GameContent/Scripts/UI/UIManager.cs:78) |
| A20 / P2 | **Statische Zustände ohne sauberes Ende:** `StationModule.allModules` wird nur befüllt, nicht bereinigt. `GameManager.gameOver` und `isInit` haben keinen expliziten Neustart bei erneuter Initialisierung. Relevant für Szenenwechsel und bestimmte Editor-Playmode-Einstellungen. Runtime-Registrierungen beim Lebenszyklus bereinigen. |
| A21 / P2 | **Bereinigungen zählen als Kills:** `RemoveAllEnemies()` läuft über `RemoveEnemy(...None)`, das trotzdem `RegisterKill()` aufruft. Beim Reset kann die unsortierte Reihenfolge beeinflussen, ob diese künstlichen Kills wieder gelöscht werden. Entfernen und Kampf-Kill trennen. [EnemySpawner.cs:146](E:/GitHub/UnityProjects/Aktiv/Aegis-Protocol/Assets/GameContent/Scripts/Enemy/EnemySpawner.cs:146) |
| A22 / P2 | **Zwei verwaiste Google-Provider-Scriptreferenzen:** Google Adaptive Performance ist nicht im Lockfile vorhanden, alte Loader-/Settings-Assets existieren noch. Der tatsächlich eingetragene Loader ist Samsung. Gezielt bereinigen; daraus ist kein bestätigter Gameplay-Absturz abzuleiten. |

Zusätzliche kleine Befunde: `Temporal Modulator.asset` trägt `moduleType: 3` statt 8; die Runtime-Kopie kopiert `moduleType` überhaupt nicht. `RotationSpeed` wird mit Faktor 100 angewandt, aber als Grad pro Sekunde ohne diesen Faktor angezeigt. Die GameOver-Anzeige zeigt den Modulabzug positiv, obwohl er negativ gemeint ist. `UpgradeUI` blockiert die Inspektion nicht bezahlbarer Upgrades durch `interactable = false`. Beim Erreichen von MAX wird der Interactable-Zustand nicht ausdrücklich aktualisiert. Im Modul-Preset-Editor werden inaktive Module bei der Suche ausgelassen.

Noch praktisch zu prüfen: Tippen auf die Station bei aktivem Schild; Welt-Raycasts verwenden keine gezielte LayerMask und treffen möglicherweise den Schild statt das gewünschte Objekt. Der Ressourcen-Eingabepfad prüft außerdem nicht, ob der Finger über UI liegt. Mehrfachtreffer innerhalb desselben Physikschritts sollten durch einen bereits-tot-/bereits-verbraucht-Schutz abgefangen werden.

## 7. Visuelles Feedback und Bedienbarkeit

### Beibehalten

Die graublaue Station mit cyanfarbenen Leuchtelementen wirkt zusammengehörig. Ihre asymmetrischen Anbauten machen Fortschritt sichtbar. Der dunkle Hintergrund schafft Ruhe, das Schild ist sofort erkennbar. Gegner besitzen unterschiedliche Silhouetten; die Modulsymbole und sechseckigen Rahmen passen grundsätzlich zum Thema. Die Verbindungslinie macht die Zuordnung zwischen Auswahl und Station verständlich.

### Gezielt verbessern

1. **HUD vervollständigen:** Aktuelle Welle, klar erkennbare Core-HP, Ressourcen und Pause anzeigen. Auf den Screenshots fehlt eine dauerhafte Orientierung, wie weit der Run fortgeschritten ist und wie dringend die Gefahr ist.
2. **Material verständlich benennen:** „Material“ und `$` mischen zwei Begriffe. Einen Begriff samt Symbol wählen, beispielsweise Material oder Credits.
3. **Farbhierarchie beruhigen:** Neon-Grün für fast alle Texte konkurriert mit Cyan, gelben Preisen und bunten Effekten. Helle neutrale Texte, Cyan für Auswahl, Grün für positive Zustände und Orange/Rot für Gefahr ergeben eine klarere Hierarchie. Der bestehende Stil lässt sich dabei erhalten.
4. **Kaufhandlung ausdrücklich zeigen:** Derzeit wählt der erste Tap ein Upgrade, der zweite kauft. Ein sichtbarer Kaufknopf mit „aktuell → danach“ und Kosten ist besser zu verstehen. Informationen sollten auch ohne genügend Material lesbar sein.
5. **Touch-Ziele verbessern:** Die manuell zu sammelnden Punkte sind auf den Screenshots sehr klein. Größerer unsichtbarer Sammelradius oder eine einfache Sammelgeste hilft. Erst am echten Display entscheiden, ob die Grafik selbst wachsen muss.
6. **Baugefühl verstärken:** Kurzer Einblendeffekt, klarer Bau-/Upgrade-Sound und kurze Hervorhebung des veränderten Moduls. Bestehende Effekte reichen als Ausgangspunkt.
7. **Kontrast der Gegner erhöhen:** Besonders dunkle rote/braune Schwärme heben sich wenig vom Hintergrund ab. Kleine helle Triebwerke beziehungsweise Konturen helfen mehr als ein vollständiger Austausch der Grafiken.
8. **Menü und Spielfeld entflechten:** Die lange weiße Verbindungslinie und die hohe seitliche Iconleiste überlagern den Kampf. Linie etwas dezenter, Auswahlbereich kompakter, kurze Modulnamen ergänzen. Nicht alle Details müssen gleichzeitig sichtbar sein.
9. **Bildschirmränder absichern:** Canvas-Referenzauflösung ist 1600 × 900 trotz Hochformat-Ziel. Das ist allein noch kein Fehler, verlangt aber Prüfung von Ankern, Skalierung und schmalen Displays. Eine Safe-Area-Behandlung ist im eigenen Code nicht vorhanden.
10. **Kurze Einführung:** Drei kontextbezogene Hinweise reichen zunächst: Station antippen, erstes Modul bauen, Material sammeln. Das Spiel sollte eine erste sinnvolle Entscheidung ermöglichen, bevor der Core ohne Erklärung stirbt.

Die Veröffentlichungsbilder sind deutlich dramatischer und dichter als die Gameplay-Screenshots. Das Logo und die Schildidee sind wiedererkennbar; für die spätere Präsentation sollten aktuelle echte Spielszenen das tatsächliche Tempo und HUD zeigen. Keine neue Grafikserie nötig. Der Schriftzug im komplexen Logo verliert bei kleiner Darstellung Lesbarkeit; das reine Stationssymbol ist ein guter Kandidat für kleine Flächen.

## 8. Balancing und Spielerlebnis

Die Startökonomie erlaubt mehrere günstige Entscheidungen: Ammo Fabricator 35, Command Unit 40, Extractor 60 und Temporal Modulator 200 bei 500 Startmaterial. Radar kostet 350, Shield 800 und Drone 1.500. Die erste Drohne benötigt zusätzlich das DroneCount-Upgrade für 250; der bloße Kauf des Drohnenmoduls erzeugt noch keine Drohne. Das sollte vor dem Kauf erkennbar sein.

AutoCollecting kostet zusätzlich zum Extractor 850. Damit ist die Befreiung vom Tippen anfangs ein großes wirtschaftliches Ziel. Ob manuelles Sammeln bis dahin Spaß macht oder anstrengend wird, ist die wichtigste frühe Playtest-Frage. Ein früher oder günstiger freigeschalteter Komfortgewinn dürfte die ersten Minuten verbessern; den konkreten Preis erst nach einem kurzen Gerätetest festlegen.

Schilde entfernen bei Schiffskollision jeden Gegner für einen Schildpunkt, unabhängig von seinen HP oder seinem Kollisionsschaden. Das gilt auch für Tanks und Bosse. Als Spielregel ist das möglich, schwächt aber ihre Rolle und kann die Schildstrategie übermächtig machen. Vor einer bloßen Preiserhöhung entscheiden, wie schwere Gegner mit dem Schild interagieren sollen.

Normale Gegner und Tanks steuern hauptsächlich geradlinig den Core an. Die meisten Ausbauentscheidungen erhöhen Zahlen; eine überzeugende taktische Entscheidung entsteht erst, wenn etwa schnelle Gegner, Schwärme und Fernkämpfer unterschiedlich auf die vorhandenen Verteidigungen reagieren. Für die erste fertige Version genügen klarer erkennbare Rollen der vorhandenen sechs Typen und ein angekündigter stärkerer Boss. Zusätzliche Gegnersysteme würden den Zeitplan unnötig vergrößern.

Als erste Balancing-Ziele eignen sich: erste verständliche Ausbauentscheidung innerhalb der ersten Minute, erkennbarer neuer Gegnertyp in den ersten Minuten und ein spürbarer Meilenstein nach wenigen Wellen. Ein Endless-Modus kann danach weiterlaufen. Diese Ziele sind Designvorschläge, keine bereits gemessenen Run-Längen.

## 9. Performance, Assets und Audio

### Reihenfolge der Optimierung

Zuerst synchrones Speichern bündeln und die Wellenzahl begrenzen. Anschließend auf einem schwächeren Android-Gerät messen. Danach bei Bedarf Projektile, Explosionen, Materialdrops und kurze Audios wiederverwenden. Aktuell werden sie häufig instanziiert und zerstört; `SoundManager` erstellt für jeden Effekt ein GameObject mit AudioSource. Die Obergrenze von sechs Sounds je Kategorie ist bereits hilfreich.

Alle Physik-Layer kollidieren derzeit miteinander, die geprüften Gegner liegen auf Default. Gezielte Layer für Gegner, Station, Projektile, Sammelobjekte und UI-Raycasts können unnötige Kontakte und Fehlbedienung reduzieren. Bewegungen erfolgen überwiegend über Transform im Update trotz Rigidbody2D. Kollisionszuverlässigkeit bei hohem Spieltempo muss deshalb praktisch geprüft werden; eine pauschale komplette Physik-Neuimplementierung wäre voreilig.

Die Zielsuche durchsucht Gegnerlisten je Waffe/Drohne. Bei höchstens vier Drohnen und begrenzten Wellen ist das voraussichtlich ausreichend. Aufwendige räumliche Suchstrukturen sind erst nach einer Messung gerechtfertigt. `Resources.LoadAll` für Upgrade-Symbole lässt sich dagegen sehr einfach einmalig cachen.

### Bilddaten

Mehrere Stationsmodule und Lichtmasken verwenden jeweils 1024 × 1024 Pixel. Bei Modulen M1–M8 belegt bereits die umschließende sichtbare Fläche nur ungefähr 4–10 Prozent des Gesamtbilds; einige Lichtmasken sind noch kleiner. Ein 2048er Schild und 2048er Gegner-Sheets sind ebenfalls vorhanden.

Viele Importe nutzen Standardkompression ohne spezifischen Android-Override. Ein SpriteAtlas ist nicht vorhanden. Später Texturgröße, Atlas-Packing und tatsächlich geladenen Texturspeicher prüfen. Transparenten Rand nicht blind abschneiden: Gemeinsame Leinwand und Pivotpositionen können Teil der aktuellen Stationsmontage sein. Rohdateigröße ist nicht gleich Laufzeitspeicherbedarf.

Die Referenzanalyse findet Kandidaten zum Archivieren: alte Stations-/Turmvarianten, Dummy-Prefab, einzelne unbenutzte Waffenbilder, ein Musikstück und eine Explosion. Eine fehlende statische Referenz beweist nicht automatisch, dass eine Datei unnötig ist. Assets außerhalb von Resources werden außerdem nicht allein durch ihre Anwesenheit zwangsläufig in den Player aufgenommen. Aufräumen hilft hier zunächst der Übersicht; einen bestimmten Größengewinn behaupte ich nicht.

### Audio

Es sind vollständige Musik- und Effektdateien vorhanden. `Cosmic Siege` dauert etwa 245 Sekunden und ist als Decompress On Load eingestellt. Für lange Musik ist Streaming ein sinnvoller Prüfpunkt; kurze Effekte benötigen andere Einstellungen. Die Abwägung zwischen Speicher und Laufzeitaufwand beschreibt die [Unity-Dokumentation zu AudioClips](https://docs.unity3d.com/6000.0/Documentation/Manual/class-AudioClip.html).

Der Sound-Lebenszyklus benutzt `WaitForSeconds`, dessen Zeitbasis mit dem Spieltempo verändert wird. Dadurch können Audioobjekte bei hoher Geschwindigkeit vor dem tatsächlichen Ende eines Clips entfernt werden. Für die Bereinigung Echtzeit oder den Wiedergabestatus verwenden. Musik- und Effektlautstärke sollten getrennt einstellbar und die Einstellung dauerhaft gespeichert sein; derzeit gibt es lediglich eine nicht angebundene `ToggleMusic`-Methode.

Eine Übersicht der Herkunft und Nutzungsnachweise der verwendeten Bilder, Fonts und Audios wäre vor der Veröffentlichung nützlich. Im Repository liegen Lizenzinformationen für TMP/Font und die allgemeine Projektlizenz; eine vollständige Zuordnung der übrigen Medien wurde nicht gefunden. Daraus folgt keine Aussage über vorhandene oder fehlende Rechte außerhalb des Repositories.

## 10. Einfacher Refactoring-Zuschnitt

**Die bestehenden Klassen grundsätzlich behalten.** Entscheidend sind wenige klare Zuständigkeiten:

| Verantwortung | Kleinstmöglicher Zuschnitt |
|---|---|
| Run-Lebenszyklus | GameManager koordiniert explizit Start, Laden, GameOver und Neustart in fester Reihenfolge |
| Konfiguration | UpgradeSet-Assets bleiben unveränderte Vorlagen; Runtime-Level und wirksame Werte werden getrennt gehalten |
| Käufe / Bau / Reparatur | Bestehender UpgradeManager beziehungsweise StationModule prüfen Voraussetzungen und führen die vollständige Aktion aus |
| Anzeige | ModulesUI und UpgradeUI zeigen Zustand und lösen Aktionen aus; sie verändern nicht nebenbei Kampfregeln |
| Speicherzugriff | SaveGameManager liest/schreibt Daten und bündelt Saves; Laden erzeugt keine neuen Spielereignisse |
| Zeit | TimeController besitzt die eine verbindliche Berechnung des effektiven Spieltempos |
| Wellen | EnemySpawner bekommt eine begrenzte und nachvollziehbare Wellenkonfiguration |

Globale `Instance`-Referenzen sind für diese Projektgröße zunächst tragbar. Die problematischen Seiteneffekte zu entfernen bringt mehr als sämtliche Singletons auszutauschen. Ebenso kann der zentrale Update-Aufruf bleiben. Die manuellen Start-Snapshots sollten dort entfallen, wo sich ein sauberer Anfangszustand direkt aus Vorlagen ableiten lässt.

Neue Namespaces, flächendeckende Umbenennungen, ein eigener Service-Layer, Netzwerkfunktionen, Werbung, zusätzliche Progressionssysteme und ein großes Content-Update sind für diese Fertigstellung nicht erforderlich. Den Input-System-Wechsel ebenfalls nicht mit den Zustandsreparaturen vermischen; ein kleiner gemeinsamer Touch-/Mauspfad reicht zunächst.

## 11. Reihenfolge zur Fertigstellung

Die folgenden Größenordnungen sind eine Planungsschätzung für konzentrierte Arbeit, keine Zusage. Unbekannte Geräteprobleme, Store-Abhängigkeiten und gewünschte Designänderungen können den Umfang erhöhen.

| Paket | Inhalt | Grober Aufwand | Fertig, wenn … |
|---|---|---:|---|
| 1. Reproduzierbarer Ausgangspunkt | Passende Unity-Installation/Buildumgebung, Android-Development-Build, einfacher Editor-Eingabepfad, Fehlerliste am Gerät bestätigen | 0,5–1 Tag | Start und kurzer Test-Run reproduzierbar funktionieren |
| 2. Zustand stabilisieren | A01–A08, A11, A12, A17; kontrollierter Neustart und Roundtrip-Laden | 3–5 Tage | Bauen, Schaden, Speichern, Laden und drei Replays dieselben Regeln verwenden |
| 3. Wellen und Ökonomie | A09–A10, defektes Balancing-Werkzeug, erste Wellen und Boss, nachvollziehbare Drohnenkosten | 2–3 Tage | Keine negativen Zeiten/überlaufenden Preise; erste Spielminuten bieten sinnvolle Entscheidungen |
| 4. Android-Bedienung und Abschluss | HUD, Pause, kurze Einführung, Kaufanzeige, Sammeln, Safe Area, Audioeinstellungen, kleinere A13–A16-Fehler | 2–3 Tage | Ein neuer Spieler ohne Erklärung einen Run beginnen und ausbauen kann |
| 5. Abnahme und Release-Kandidat | Gerätetests, Profile, nötige Optimierung, Buildprüfung, aktuelle Screenshots und Beschreibung | 2–4 Tage | Ein getesteter signierter Release-Kandidat vorliegt |

Insgesamt ungefähr **10–16 konzentrierte Arbeitstage** bei unverändertem Spielumfang. Eine grundlegende Neugestaltung oder zusätzliche Spielmodi sind darin nicht enthalten. Der erste konkrete Arbeitsblock sollte die Speicher-/Ladefehler und einen eindeutigen NewRun-Ablauf behandeln.

## 12. Abnahmefälle mit hohem Nutzen

Für die empfindlichen Regeln lohnen wenige gezielte automatisierte Tests; die Bedienung benötigt zusätzlich echte Gerätetests. Ein eigenes großes Testframework ist nicht nötig, das Unity Test Framework ist bereits im Projekt.

| Test | Erwartetes Ergebnis |
|---|---|
| Frischer Start ohne Save | 500 Material, nur Core gebaut, korrekte Grundwerte, erstes Menü bedienbar |
| Beschädigtes Modul mit Integritätsupgrade speichern/laden | Gleiche aktuellen und maximalen HP wie vor dem Laden |
| Beschädigte Drohnen speichern/laden | Gleiche Anzahl und HP; kein Save während der Wiederherstellung |
| Mehrere Kills, Ressourcen und Käufe speichern/laden | Alle Zähler und daraus berechneter Score identisch |
| Extractor zerstören; anderes Modul kaufen; App neu starten | Automatik bleibt entsprechend der festgelegten Modulregel aus |
| Zwei Module zerstören, nur eines wiederaufbauen | Nur dessen Funktion wird wiederhergestellt |
| Schild erschöpfen und Schildmodul während Recharge verlieren | Kein automatisches Reaktivieren ohne Modul |
| Ungebautes Modul während eines Angriffs auswählen | Vorschau fängt keine Treffer ab und erzeugt keine Verluststatistik |
| Upgrade kaufen, anschließend sofort Hintergrundwechsel | Ressourcen, Upgradelevel und Wirkung bilden einen vollständigen Zustand |
| Reparatur, anschließend sofort Hintergrundwechsel | Bezahlung und reparierte HP sind gemeinsam gespeichert |
| Save abschneiden oder ungültiges JSON einsetzen | Kontrollierter Fallback; kein unbedienbarer Start |
| x2 wählen, ins Leere tippen, Menü öffnen/schließen, Zeitmodul verlieren | Tatsächliches Tempo und Anzeige stimmen jederzeit überein |
| GameOver und drei aufeinanderfolgende Replays | Keine alten Gegner/Drohnen/Level/Kills, korrekte Preise und UI |
| Speichern während beziehungsweise nach Spawn einer Welle | Definierter Checkpoint; kein übersprungener Restkampf |
| Alle Upgrade-Grenzwerte prüfen | Keine negativen Zeiten, ungültigen Preise oder falschen MAX-Anzeigen |
| Manueller Drop und abgelenktes Projektil | Sichtbarer Sammelflug und korrekte Trefferstatistik |
| 16:9-, 19,5:9- und schmales Display mit Aussparung | Alle Buttons erreichbar, nichts unter Systemrändern |
| Hintergrund, Rückkehr, Android-Zurück, längerer Run | Verständlicher Pausenzustand, keine Datenverluste und keine anhaltenden Fehler |
| Späte Welle auf schwächerem Gerät | Dokumentierte Frametimes, Speicher und Temperatur; anschließend gezielte Optimierung |

## 13. Offene Punkte und erhaltene Projektdateien

Noch offen sind ein aktueller vollständiger Android-Build, Gerätetests, Audiobeurteilung und gemessene Performance. Die alten APK-/AAB-Dateien belegen frühere Builds, nicht die Fehlerfreiheit dieses Quellstands. Konkrete aktuelle Play-Store-Vorgaben und der Status eines vorhandenen Store-Eintrags waren nicht Gegenstand dieser Quellcodeprüfung; vor Einreichung separat verifizieren.

Gameplay-Code, Szenen, vorhandene Assets, Paketversionen und persönliche Spielstände wurden bei der Analyse nicht verändert. Die Analyse und die daraus abgeleitete Umsetzungsliste liegen in `Assets/Docs`. Die bereits zu Beginn unversionierte Datei `Assets/TODOs.txt.meta` wurde unverändert belassen. Temporäre Kompilate und Bildübersichten liegen außerhalb des Projekts unter `C:/Temp/aegis-audit-20260918`.
