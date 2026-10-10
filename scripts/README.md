# Unity Android Release Tools

[unity-android-release-tools](https://github.com/WRB-Studio/unity-android-release-tools) baut signierte Unity-APKs/AABs, übermittelt Releases an Google Play und exportiert Builds in lokale Google-Drive-Ordner.

Wiederverwendbarer Windows-Workflow für signierte Unity-APKs/AABs, Google-Play-Uploads und den Export in einen lokal synchronisierten Google-Drive-Ordner. Beide Ziele nutzen dieselbe Buildfunktion und denselben Unity-Editor-Helfer. Unity-Version und sichtbare App-Version werden aus dem Projekt gelesen. Kein Upload allein durch Git-Push.

Voraussetzungen: Windows PowerShell 5.1 oder PowerShell 7, Unity mit Android Build Support inklusive SDK/NDK/OpenJDK und ein passender Signierungs-Keystore. Ruby, Fastlane und Google-Service-Account werden **nur für Google Play** benötigt. Für den lokalen Drive-Export genügt ein vorhandener Zielordner; die Synchronisierung übernimmt Google Drive for desktop.

Updates bestehender Unity-Integrationen: [UPDATING.md](https://github.com/WRB-Studio/unity-android-release-tools/blob/8d9fce859fa9a606a621acd524665e109e7b8e28/UPDATING.md). Änderungen und Migrationen: [CHANGELOG.md](https://github.com/WRB-Studio/unity-android-release-tools/blob/8d9fce859fa9a606a621acd524665e109e7b8e28/CHANGELOG.md).

Für Installation und Updates ist der aktuelle Stand von **`origin/main`** dieses Tool-Repositories der Standard. Ein bestimmter Commit oder Tag muss nicht ausgewählt werden. Der Agent dokumentiert die tatsächlich übernommene Commit-ID zur Nachvollziehbarkeit und prüft die Integration.

## Stand dieser Projektintegration

Quell-Commit: `8d9fce859fa9a606a621acd524665e109e7b8e28` (Update am 10.10.2026).

Projektkonfiguration, SecretsKey, vorhandene Zugangsdaten und bestehende .meta-GUIDs bleiben erhalten. Die Integration verwendet weiterhin `%LOCALAPPDATA%/WRBStudio/<SecretsKey>/` und den Namespace `WRBStudio.UnityRelease.Editor`; das neue Unity-Menü verwendet denselben lokalen Speicherort wie die Skripte. `Invoke-AegisAndroidBuild` bleibt als Kompatibilitätsaufruf erhalten.

Der bestehende DriveDirectory-Altwert bleibt auf ausdrücklichen Wunsch in release.config.json erhalten. Neue Drive-Befehle verwenden die bereits eingerichtete lokale drive-export.json oder einen CLI-Override. Toolupdates führen keine Geräteinstallation, Builds oder Veröffentlichungen aus.

## Nutzung mit einem Coding-Agenten

In der Session des Zielprojekts genügt der Link zu diesem Repository mit dem Auftrag **„Integriere Unity Android Release Tools in das aktuelle Unity-Projekt.“** Die [AGENTS.md](https://github.com/WRB-Studio/unity-android-release-tools/blob/8d9fce859fa9a606a621acd524665e109e7b8e28/AGENTS.md) und die [Integrationsanleitung](https://github.com/WRB-Studio/unity-android-release-tools/blob/8d9fce859fa9a606a621acd524665e109e7b8e28/INTEGRATION.md) beschreiben das vollständige Vorgehen. Der Agent ermittelt die Projektwerte selbst, integriert die Dateien, prüft vorhandene Zugangsdaten und testet den Workflow.

Für eine Veröffentlichung ergänzen: **„Baue und lade anschließend in Google Play Production hoch.“** Ein schon erteilter Upload-Auftrag benötigt keine erneute Bestätigung. Rückfragen bleiben nur bei fehlenden Zugangsdaten, unklaren App-Entscheidungen oder notwendigen Aktionen im Unity-Editor.

## Anderes Projekt einrichten

Für ein bereits eingerichtetes Projekt genügt der Auftrag: **„Aktualisiere Unity Android Release Tools aus origin/main und prüfe die Integration.“** Projektkonfiguration, Zugangsdaten und bestehende `.meta`-GUIDs bleiben erhalten. Ein Updateauftrag startet keine Veröffentlichung.

### Google-Einrichtung: einmal insgesamt und einmal pro Spiel

| Oberfläche | Einrichtung | Wiederverwendung |
| --- | --- | --- |
| Google Cloud Console | Cloud-Projekt, Google Play Developer API und Service-Account | Einmal einrichten; für mehrere Spiele verwendbar |
| Google Play Console | Eigene App, Paketname, passende Signierung, Store-/Pflichtangaben und Zugriff für den Service-Account | Für jedes Spiel prüfen und fehlende Teile ergänzen |

Ein vorhandener Service-Account kann mehrere Apps hochladen. Bei passenden kontoweiten Rechten ist keine zusätzliche App-Freigabe nötig; sonst die neue App unter **Nutzer und Berechtigungen → Service-Account → App-Berechtigungen** hinzufügen. Ein neuer API-Schlüssel pro Spiel ist nicht erforderlich. [Google-API-Einrichtung](https://developers.google.com/android-publisher/getting_started?hl=de), [Play-Berechtigungen](https://support.google.com/googleplay/android-developer/answer/9844686?hl=de).

**Der Coding-Agent soll diese Einrichtung selbst prüfen und begleiten.** Er verwendet vorhandene Zugänge und Ressourcen, erledigt fehlende Schritte mit verfügbaren APIs oder Browser-Werkzeugen und fragt nur nach tatsächlich fehlenden Angaben, Zugängen oder notwendigen Nutzeraktionen. Bei manuellen Schritten führt er durch den konkreten nächsten Schritt, prüft das Ergebnis und arbeitet weiter. Der vollständige Ablauf steht in [INTEGRATION.md](https://github.com/WRB-Studio/unity-android-release-tools/blob/8d9fce859fa9a606a621acd524665e109e7b8e28/INTEGRATION.md#5-google-cloud-und-play-console-selbst-prüfen-und-einrichten-nur-play-ziel). Rechtliche und inhaltliche Store-Angaben werden nicht erfunden oder ungeprüft bestätigt.

### Lokale Dateien und Zugangsdaten

**Standard ist der vorhandene gemeinsame Service-Account für mehrere Spiele.** Ein neues Spiel oder eine fehlende lokale Schlüsseldatei ist kein Auftrag, einen neuen Account mit neuer E-Mail-Adresse anzulegen. Ist die gewünschte gemeinsame Identität bereits eindeutig festgelegt, verwendet der Agent sie wieder. Andernfalls klärt er vor Änderungen: **„Welches vorhandene Cloud-Projekt und welchen gemeinsamen Service-Account soll dieses Spiel verwenden? Ist eine zugehörige Schlüsseldatei bereits lokal vorhanden?“** Benötigt werden nur die Zuordnung oder der lokale Dateipfad, niemals der Schlüsselinhalt.

Bei mehreren Accounts darf der Agent nicht allein nach dem Namen oder erfolgreichem Zugriff auswählen. Bei fehlendem Zugriff prüft er die Rechte des ausgewählten Accounts. Bei fehlender Schlüsseldatei fragt er zuerst nach einer vorhandenen Datei. Einen neuen Service-Account, ein neues Cloud-Projekt oder einen zusätzlichen Schlüssel legt er erst nach ausdrücklichem Auftrag an; ein allgemeiner Integrations- oder Uploadauftrag genügt dafür nicht. Bereits erteilte konkrete Aufträge gelten weiter. Ein neuer Schlüssel desselben Accounts ist kein neuer Account und ändert dessen E-Mail-Adresse nicht. Der genaue Auswahlablauf steht in [INTEGRATION.md](https://github.com/WRB-Studio/unity-android-release-tools/blob/8d9fce859fa9a606a621acd524665e109e7b8e28/INTEGRATION.md#gemeinsamen-service-account-auswählen).

Für einen gemeinsamen Service-Account einen projektneutralen Namen wählen, beispielsweise `unity-play-uploader`, mit Anzeigename „Unity Play Uploader“. Die Service-Account-ID in der E-Mail-Adresse lässt sich nach der Erstellung nicht umbenennen; Anzeigename und Beschreibung lassen sich ändern. Ein funktionierender Account muss deshalb nicht allein wegen seines Namens ersetzt werden. [Google-Dokumentation zur Erstellung](https://cloud.google.com/iam/docs/service-accounts-create).

Mehrere Spiele dürfen dieselbe Service-Account-Schlüsseldatei verwenden. Jedes Spiel behält seine eigene `release.config.json`, seinen eindeutigen `SecretsKey` und seine passende Signierung. Den gemeinsamen Schlüssel außerhalb der Repositories speichern und in jeder lokalen Secret-Konfiguration über `ServiceAccountJsonPath` referenzieren. Bei einem späteren Schlüsselwechsel alle betroffenen Konfigurationen umstellen und mit `-CheckOnly` prüfen, bevor der alte Zugang stillgelegt wird. Der Ablauf einschließlich Downloadprüfung und Bereinigung steht in der [Integrationsanleitung](https://github.com/WRB-Studio/unity-android-release-tools/blob/8d9fce859fa9a606a621acd524665e109e7b8e28/INTEGRATION.md#service-account-oder-schlüssel-umstellen-und-alte-zugänge-bereinigen).

1. Den Ordner `scripts` und beide Dateien `Assets/Editor/UnityAndroidBuild.cs` sowie `Assets/Editor/UnityAndroidReleaseMenu.cs` ins Unity-Projekt kopieren.
2. `scripts/release.config.json` anpassen:

```json
{
  "PackageName": "com.example.game",
  "ArtifactName": "Game",
  "SecretsKey": "Game"
}
```

`PackageName` muss mit dem Android Application Identifier in Unity und bei Play-Nutzung mit der App in Google Play übereinstimmen. `ArtifactName` benennt Build-Dateien. `SecretsKey` muss pro App eindeutig sein und ordnet lokale Signierungs- und Drive-Einstellungen zu. Persönliche Pfade gehören nicht in diese Datei.

3. Für Google Play zusätzlich Ruby mit Fastlane installieren (`gem install fastlane`). Die App muss bereits in der Play Console angelegt sein; der Service-Account benötigt Zugriff auf diese App und den gewünschten Track. Für vorhandene Apps den passenden Upload-Keystore verwenden. Bei reinem Drive-Export entfällt die Google-Cloud-/Play-Einrichtung.
4. Zugangsdaten einmalig hinterlegen:

```powershell
./scripts/Set-ReleaseSecrets.ps1 -KeystorePath 'D:/Keys/game.keystore' -KeyAlias 'game' -ServiceAccountJsonPath 'D:/Keys/play-service-account.json'
```

Passwörter werden verdeckt abgefragt und verschlüsselt unter `%LOCALAPPDATA%/WRBStudio/<SecretsKey>/release-secrets.xml` gespeichert, gebunden an das Windows-Benutzerkonto. Keystore, Service-Account-JSON und Secret-Datei gehören nicht ins Repository.

Für Builds und Drive-Export `-ServiceAccountJsonPath` weglassen. Beim erneuten Einrichten der Signierung ohne diesen Parameter bleibt ein bereits gespeicherter Play-Schlüsselpfad erhalten.

## Drive-Export: APK oder AAB lokal kopieren

Einmal pro Projekt das lokale Ziel setzen, beispielsweise mit einem neutralen Beispielpfad:

```powershell
./scripts/Set-DriveExportDirectory.ps1 -DriveDirectory 'G:/My Drive/Builds/Game'

# Nur Zielkonfiguration und Version des Unity-Buildhelfers prüfen:
./scripts/Build-AndroidAndExportToDrive.ps1 -CheckOnly

# Signiertes APK bauen und lokal exportieren (Standardformat):
./scripts/Build-AndroidAndExportToDrive.ps1

# Signiertes AAB bauen und lokal exportieren:
./scripts/Build-AndroidAndExportToDrive.ps1 -Format aab -VersionCode 42

# Ziel einmalig überschreiben, ohne die lokale Konfiguration zu ändern:
./scripts/Build-AndroidAndExportToDrive.ps1 -Format apk -DriveDirectory 'G:/My Drive/Tests'
```

Das Ziel muss bereits existieren und ein absoluter Dateisystempfad sein. Die Einrichtung speichert ausschließlich `%LOCALAPPDATA%/WRBStudio/<SecretsKey>/drive-export.json`; keine Änderung an `release.config.json`, keine Kopie und kein Upload. Ein explizites `-DriveDirectory` hat Vorrang. Kopien desselben Projekts mit identischem `SecretsKey` teilen diese lokale Einstellung; unterschiedliche Spiele benötigen unterschiedliche Schlüssel. Alte `DriveDirectory`-Einträge in `release.config.json` werden nicht mehr gelesen: einmal lokal konfigurieren und den persönlichen Pfad aus Git entfernen.

Der Dateiname lautet `<ArtifactName>-<Unity-Version>-<tatsächlicher-Versioncode>.apk` beziehungsweise `.aab`. Der Drive-Befehl übernimmt ohne `-VersionCode` den Projekt-Versioncode; er fragt Google Play nicht ab und erhöht den Code nicht automatisch. Vorhandene Exporte werden nur mit `-Force` ersetzt. Der Build verbleibt zusätzlich unter `Builds/Android`. `-UnityPath` kann wie beim Play-Befehl angegeben werden.

**„Local copy verified“ bestätigt nur die lokale Kopie samt SHA-256-Vergleich.** Der Ergebnisdatensatz enthält `LocalCopySucceeded = true` und `CloudUploadConfirmed = false`. Das Tool ruft keine Drive-API auf und kann weder erkennen, ob der Ordner synchronisiert wird, noch ob die Datei in der Cloud angekommen ist. Google Drive for desktop kann nach dem Kopieren selbstständig hochladen. Den Cloud-Status anschließend im Drive-Client oder in Drive prüfen. Wenn keine Übertragung stattfinden darf, nur `-CheckOnly` oder die lokalen Tests ausführen; nicht in einen synchronisierten Ordner exportieren.

`-CheckOnly` startet weder Unity noch Kopiervorgänge oder Netzwerkzugriffe. Es prüft keine Signierung, Schreibrechte oder Cloud-Verfügbarkeit. `Build-ApkAndUploadToDrive.ps1` bleibt als APK-Weiterleitung mit den bisherigen Parametern sowie `-Force` und `-CheckOnly` erhalten und weist auf die Bedeutung „lokaler Export“ hin.

## Verwendung

```powershell
# Nächsten freien Versioncode anzeigen; kein Build und kein Upload:
./scripts/Build-AabAndSubmitToPlay.ps1 -CheckOnly

# Signiertes AAB bauen und auf den internen Test-Track hochladen:
./scripts/Build-AabAndSubmitToPlay.ps1 -Track internal

# Signiertes AAB bauen und als Production-Release übermitteln:
./scripts/Build-AabAndSubmitToPlay.ps1 -ConfirmProduction

# Nur signiertes APK bauen:
./scripts/Build-Apk.ps1

# Lokale Skriptprüfungen ohne Build oder Netzwerk:
./scripts/Test-ReleaseWorkflow.ps1

# Optional: C# gegen eine installierte Unity-Version kompilieren, ohne Editorstart:
./scripts/Test-UnityHelperCompilation.ps1 -UnityPath 'C:/Program Files/Unity/Hub/Editor/<Version>/Editor/Unity.exe'

# Optional: Menüregistrierung im leeren Unity-Testprojekt prüfen (Lizenz nötig):
./scripts/Test-UnityMenuRegistration.ps1 -UnityPath 'C:/Program Files/Unity/Hub/Editor/<Version>/Editor/Unity.exe'
```

Bei direkten Release-CLI-Aufrufen ohne `-BuildRoot` Unity vor dem Build speichern und schließen; Menü-Builds verwenden eine isolierte Kopie und lassen den Quell-Editor geöffnet. Falls Unity nicht automatisch gefunden wird, `-UnityPath` angeben. Builds und Logs liegen unter `Builds/Android`. Ein erfolgreicher Build schreibt einen Nachweis mit Paketname, Format, Schnittstellenversion und tatsächlichem Versioncode; ohne passenden Nachweis startet weder Play-Upload noch Drive-Kopie. Projektversion, Signierungswerte und die temporär deaktivierte Gradle-Projektexport-Einstellung werden nach dem Build wiederhergestellt. Builds desselben Projekts nacheinander ausführen.

Der nächste Versioncode ist das Maximum aus lokalem Projekt-Versioncode und höchstem Play-Versioncode + 1. Dafür werden alle Tracks sowie vorhandene APKs/AABs abgefragt. `-VersionCode` bleibt als explizite Vorgabe möglich; bereits verwendete Codes werden abgelehnt. Releases für dieselbe App nacheinander ausführen, da parallele Uploads denselben nächsten Code wählen könnten.

Ohne `-MetadataFile` bleiben Store-Texte und Versionshinweise unverändert. Bilder und Screenshots werden immer übersprungen. Production verwendet standardmäßig `release_status completed`; öffentliche Verfügbarkeit richtet sich nach Google-Prüfung und Veröffentlichungseinstellungen. Der Workflow registriert keine neue App und beantwortet keine rechtlichen Store-Fragebögen.

## Store-Texte und Versionshinweise per API

[examples/play-metadata.json](examples/play-metadata.json) ins Zielprojekt beispielsweise nach `release/play-metadata.json` kopieren. `packageName` an das Projekt anpassen und die Texte für alle gewünschten Sprachen freigeben lassen. Pro Sprache sind `title`, `shortDescription`, `fullDescription` und `releaseNotes` optional; mindestens ein Text muss vorhanden sein. Sobald Versionshinweise geändert werden, muss jede aufgeführte Sprache `releaseNotes` enthalten; gemischte Dateien mit fehlenden Versionshinweisen werden abgewiesen, damit Fastlane keine leeren Texte sendet. Fehlende Beschreibungsfelder bleiben erhalten. Leere Werte werden abgewiesen und sind kein Löschbefehl. Unbekannte Felder und doppelte Sprachen werden ebenfalls abgewiesen.

Grenzen: Titel 30, Kurzbeschreibung 80, vollständige Beschreibung 4000 und Versionshinweise 500 Zeichen je Sprache. Die Datei enthält ausschließlich öffentliche Texte und Paketname, niemals Zugangsdaten. Sie darf im jeweiligen Spiel-Repository versioniert werden. In diesem wiederverwendbaren Repository bleibt nur das neutrale Beispiel.

```powershell
# Texte lokal prüfen und anzeigen; kein Netzwerk, Build oder Upload:
./scripts/Submit-PlayMetadata.ps1 -MetadataFile release/play-metadata.json -VersionCode 42 -CheckOnly

# Nächsten freien Code online ermitteln und Texte anzeigen; kein Build/Upload:
./scripts/Build-AabAndSubmitToPlay.ps1 -MetadataFile release/play-metadata.json -CheckOnly

# Bauen und AAB samt bereits freigegebenen Texten in einem Play-Edit übermitteln:
./scripts/Build-AabAndSubmitToPlay.ps1 -MetadataFile release/play-metadata.json -ConfirmMetadata -ConfirmProduction

# Dasselbe für einen internen Test:
./scripts/Build-AabAndSubmitToPlay.ps1 -Track internal -MetadataFile release/play-metadata.json -ConfirmMetadata

# Texte/Versionshinweise für einen EXISTIERENDEN Release ändern; kein neuer Build:
./scripts/Submit-PlayMetadata.ps1 -MetadataFile release/play-metadata.json -VersionCode 42 -ConfirmMetadata -ConfirmProduction

# Als Entwurf hochladen und nicht zur Überprüfung senden:
./scripts/Build-AabAndSubmitToPlay.ps1 -ReleaseStatus draft -ChangesNotSentForReview -ConfirmProduction
```

`-ConfirmMetadata` bestätigt die vorab geprüften Texte; ein fehlender Schalter stoppt vor Netzwerk- oder Buildzugriff. Für Production bleibt zusätzlich `-ConfirmProduction` nötig. `Submit-PlayMetadata.ps1` verlangt den vorhandenen Versionscode im angegebenen Track (`-Track`, Standard `production`), auch bei reinen Beschreibungsänderungen. Der Befehl verändert keinen Roll-out-Status und lädt keine AAB/APK hoch.

Versionshinweise werden für den tatsächlichen neuen beziehungsweise angegebenen Versionscode erzeugt, ohne einen unspezifischen `default.txt`-Fallback. **Bei mitgelieferten Versionshinweisen ersetzt Fastlane die Versionshinweise des Zielrelease durch die gelieferten Sprachen. Daher alle gewünschten Übersetzungen dieses Release in die Datei aufnehmen.** Neue Store-Sprachen benötigen vollständige Pflichtfelder; das Skript richtet fehlende Store-Pflichtangaben nicht ein.

Für jeden Lauf wird ein neuer UTF-8-Metadatenordner unter `Builds/Android/PlayMetadata/` erstellt; alte Texte können nicht versehentlich erneut mitgeschickt werden. Fastlane lädt Build und Texte im selben Edit und übernimmt sie beim Commit. API-Fehler werden nicht automatisch mit anderen Veröffentlichungseinstellungen erneut versucht. Nach einem unklaren Ausgang zuerst den Play-Zustand prüfen und erst dann erneut hochladen.

Der Service-Account benötigt Zugriff auf die Ziel-App, die Release-Berechtigung für den Track und bei Store-Beschreibungen zusätzlich die Berechtigung zum Verwalten der Store-Präsenz. Production benötigt die separate Production-Release-Berechtigung. Keine kontoweiten Administratorrechte auf Verdacht vergeben. `-CheckOnly` beim Buildbefehl belegt Lesezugriff und Versioncodes, aber keine vollständigen Schreibrechte; beim reinen Textbefehl ist die Prüfung ausschließlich lokal.

`-ChangesNotSentForReview` speichert Änderungen ohne Einreichung zur Überprüfung; anschließend in der Console einreichen. `-ReleaseStatus draft` erstellt einen Release-Entwurf. Auch ohne diese Schalter bleibt Googles Prüfung erforderlich. Bei aktivierter verwalteter Veröffentlichung muss nach Freigabe manuell veröffentlicht werden. Dieses Skript schaltet die verwaltete Veröffentlichung nicht um und meldet einen API-Commit nicht als öffentlich verfügbar.

Referenzen: [Fastlane supply](https://docs.fastlane.tools/actions/supply/), [Google Play Edits](https://developers.google.com/android-publisher/api-ref/rest/v3/edits), [Play-Berechtigungen](https://support.google.com/googleplay/android-developer/answer/9844686).
## Welche Dateien dürfen nach GitHub?

| Inhalt | In Git aufnehmen? |
| --- | --- |
| Build-/Upload-Skripte und Unity-Buildhelfer | Ja |
| Paketname, Build-Dateiname und `SecretsKey` | Ja; `SecretsKey` ist nur ein lokaler Ordnername, kein Zugangsschlüssel |
| Keystore, private Schlüssel und Google-Service-Account-JSON | Nein |
| Verschlüsselte `release-secrets.xml`, Passwörter und Tokens | Nein |
| Lokale Drive-Konfiguration, Keystore-Pfade und persönliche Aliasnamen | Nein; außerhalb des Repositories speichern |
| Builds, Build-Nachweise und Logs | Lokal behalten |

Die eigentlichen Zugangsdaten liegen außerhalb des Unity-Projekts. Ein normaler Git-Push nimmt sie deshalb nicht automatisch mit. Kopierst du sie ins Projekt, musst du sie dort zusätzlich ausschließen. Ein privates Repository ersetzt diesen Schutz nicht.

Die `release.config.json` enthält nur Projektwerte. Das Drive-Ziel mit `Set-DriveExportDirectory.ps1` lokal speichern oder einmalig mit `-DriveDirectory` angeben. Keine persönlichen Pfade, Passwörter, Tokens oder Schlüssel in diese JSON-Datei schreiben.

## Beispiel für die .gitignore im Spielprojekt

Diese Regeln in die vorhandene `.gitignore` des Unity-Projekts **ergänzen**; die bestehenden Unity-Regeln beibehalten:

```gitignore
# Lokale Zugangsdaten; unabhängig vom Dateinamen
[Ss]ecrets/

# Signierung und private Schlüssel
*.keystore
*.jks
*.p12
*.pfx
*.pem
*.key

# Verschlüsselte lokale Konfiguration und Umgebungsdateien
release-secrets.xml
drive-export.json
.env
.env.*
!.env.example

# Beispielnamen für Google-Service-Account-Dateien
*service-account*.json
*service_account*.json

# Build-Ausgaben, Nachweise und Logs
/[Bb]uilds/
/[Ll]ogs/
*.apk
*.aab
```

Eine beliebig benannte Google-Schlüsseldatei wird von den Namensmustern nicht automatisch erkannt. Am besten außerhalb des Projekts oder unter `Secrets/` speichern. Andernfalls ihren **genauen relativen Dateinamen** zusätzlich in `.gitignore` eintragen.

**Nicht pauschal `*.json` im Unity-Spielprojekt ignorieren:** beispielsweise `Packages/manifest.json` und `Packages/packages-lock.json` gehören zum Projekt. Das eigenständige Uploader-Repository ignoriert dagegen standardmäßig alle JSON-Dateien außer seiner Beispielkonfiguration und dem neutralen Metadatenbeispiel; diese Regel nicht ungeprüft in ein Spielprojekt übernehmen.

In Unity können Keystore-Pfad und Alias in `ProjectSettings/ProjectSettings.asset` stehen. Die Datei vor einem Commit prüfen und persönliche Signierungsangaben in den Unity-Einstellungen entfernen. Der Uploader setzt sie beim Build vorübergehend aus den lokalen Zugangsdaten und stellt anschließend die ursprünglichen Werte wieder her. Die gesamte `ProjectSettings.asset` deshalb nicht ignorieren: sie enthält wichtige Spieleinstellungen.

## Vor einem Push prüfen

```powershell
# Prüfen, welche Regel eine lokale Schlüsseldatei ausschließt:
git check-ignore -v -- Secrets/play-service-account.json

# Änderungen und tatsächlich vorgemerkte Dateien prüfen:
git status --short
git diff --cached --name-only
git diff --cached

# Bereits erfasste Schlüsseldateien suchen:
git ls-files -- '*.keystore' '*.jks' '*service-account*.json' 'release-secrets.xml'
```

Zusätzlich nach eigenen Dateinamen, privaten Pfaden, Passwörtern und Tokens suchen. Dateinamensregeln erkennen keine Zugangsdaten, die direkt in Skripten, Dokumentation oder anderen Dateien stehen.

`.gitignore` schützt beim normalen Hinzufügen mit Git. Bereits erfasste Dateien bleiben erfasst; `git add -f`, ein ZIP des ganzen Projektordners oder manuelle Datei-Uploads über die GitHub-Webseite können die Regeln umgehen.

## Wenn eine sensible Datei schon in Git ist

```powershell
# Beispiel: nur aus der aktuellen Git-Version entfernen; lokale Datei bleibt erhalten:
git rm --cached -- Secrets/play-service-account.json
```

Danach die Ignore-Regel und die Entfernung committen. **Das entfernt die Datei nicht aus älteren Commits.** Die gesamte betroffene Git-Historie muss separat bereinigt werden. Bereits veröffentlichte Zugangsschlüssel widerrufen beziehungsweise ersetzen; nur Löschen im neuesten Commit oder nachträgliches Umschalten auf „privat“ reicht dafür nicht aus.

## APK direkt auf ein Android-Gerät installieren

Nach Integration in ein Spielprojekt:

```powershell
./scripts/Build-ApkAndInstall.ps1 -CheckOnly
./scripts/Build-ApkAndInstall.ps1 -Development
```

Ein USB-Gerät anschließen und USB-Debugging auf dem Handy bestätigen. Bei mehreren Geräten explizit `-DeviceSerial` verwenden. Unity kommt aus Projektversion/Unity-Hub-Konfiguration oder `-UnityPath` beziehungsweise `UNITY_EDITOR_PATH`; ADB standardmäßig aus dessen Android SDK, alternativ `-AdbPath` oder `ANDROID_ADB_PATH`. Gerätekennungen werden weder als Konfiguration noch im Installationsnachweis gespeichert.

Der Befehl baut die **normale Paketkennung aus release.config.json**, ARM64/IL2CPP, standardmäßig mit Entwicklungssignatur. `-Development` aktiviert Unity-Entwicklungsdiagnosen, `-ReleaseSigning` verwendet die vorhandene lokale Signierung. Ohne ReleaseSigning werden keine Release-/Play-Zugangsdaten benötigt. Keine separate Test-App, kein Play-/Drive-Upload und kein automatischer zweiter Build. Sichtbare Spielversion unverändert; Android-Code ist größer als Projekt und installierte App; er gilt ausschließlich für diesen Gerätebuild. Die Quell-Projekteinstellungen bleiben unverändert.

Der gespeicherte Projektstand wird in einen separaten Cache gespiegelt. Ein offener Quell-Editor kann bleiben; vorher ungespeicherte Szenen/Assets speichern. Standardcache lokal unter `%LOCALAPPDATA%/WRBStudio/<SecretsKey>/BuildCache`; `-BuildRoot` darf nur ein eigens dafür bestimmter Ordner außerhalb des Quellprojekts sein. Seine Assets/Packages/ProjectSettings werden durch die Projektkopie ersetzt. Keine Quell-/Eltern-/Laufwerkswurzel oder verlinkten Cacheverzeichnisse erlaubt. Unity wartet höchstens 30 Minuten (`-TimeoutSeconds`), bei Timeout wird nur der eigene Prozessbaum beendet. Ergebnisse liegen ausschließlich lokal im ignorierten `Builds/Android/Device/<Versuch>`: APK, Unity-Log, frischer Build-Nachweis und Installation mit SHA-256, Paket/Version und Build-ID. Fehlgeschlagene Installation erhält die APK.

Vor der Installation werden frischer Build-Nachweis und tatsächliches APK-Manifest mit SDK-aapt abgeglichen; danach installierte Paketversion mit dumpsys bestätigt. `adb install -r` erhält App-Daten. Bei anderer Signatur stoppt der Befehl. **Nur `-ReplaceExistingApp` erlaubt dann das Entfernen genau der Ziel-App samt lokalen Daten und anschließende Installation.** Keine pauschale Deinstallation, kein Downgrade. Dieser Geräteablauf ersetzt keine Gameplay-Abnahme oder Prüfung einer Store-Signatur.

Skripte und Unity-Helfer gemeinsam aktualisieren. Der bestehende Release-Vertrag bleibt bei Protokoll 1; die optionale Geräte-Buildmethode wird zusätzlich separat geprüft. `Test-ReleaseWorkflow.ps1` enthält geräteneutrale Prüfungen ohne ADB/Build. Praktische APK-/USB-Abnahme getrennt beauftragen.

Optional einmal `Set-AndroidDeviceBuildSettings.ps1 -UnityPath <Editor/Unity.exe> -BuildRoot <dedizierter Cache>` ausführen. Unity-/ADB-/Cachepfade werden lokal unter `%LOCALAPPDATA%/WRBStudio/<SecretsKey>/device-build.json` gespeichert und beim nächsten Gerätebuild verwendet. CLI-Argumente haben Vorrang. Keine Gerätekennung wird gespeichert; diese Datei niemals ins Repository kopieren.

## Unity-Menü

Nach Integration beider Editor-Dateien erscheint unter **Tools → Unity Android Release Tools**:

```text
Unity Android Release Tools
├─ Build
│  ├─ APK bauen
│  └─ AAB bauen
├─ Handy
│  ├─ Bauen, installieren und starten
│  ├─ Mit Release-Signierung installieren und starten
│  └─ Verbundene Geräte anzeigen
├─ Google Drive
│  ├─ APK bauen und lokal exportieren
│  ├─ AAB bauen und lokal exportieren
│  └─ Ziel prüfen
├─ Google Play
│  ├─ Versioncode prüfen (online)
│  ├─ AAB bauen und intern hochladen
│  └─ AAB bauen und Production übermitteln...
├─ Einrichtung
│  ├─ Projektkonfiguration öffnen
│  ├─ Signierung einrichten...
│  ├─ Lokalen Drive-Ordner auswählen...
│  └─ Buildcache und ADB einstellen...
├─ Prüfungen
│  └─ Lokale Tests ausführen
├─ Dateien
│  └─ Builds und Logs öffnen
└─ Hilfe
   └─ Dokumentation öffnen
```

Alle Menü-Builds verwenden dieselben Skripte und eine isolierte Projektkopie. Der Quell-Editor bleibt geöffnet; veränderte Szenen werden vorab zum Speichern angeboten, Assets gespeichert. APK/AAB für Play oder Drive verwenden die vorhandene Release-Signierung. Der erste Geräte-Eintrag verwendet Development-Diagnosen und Debug-Signierung, der zweite die lokale Release-Signierung. Beide installieren und starten das Spiel automatisch. Bei genau einem autorisierten Gerät wird es verwendet, bei mehreren erscheint eine Auswahl mit Status; `unauthorized` und `offline` sind nicht auswählbar. Das Spiel muss danach auf dem Handy selbst getestet werden.

Das abbrechbare Fortschrittsfenster zeigt Arbeitsschritte und Laufzeit. Die Leiste ist keine Zeitschätzung. Bei Abbruch, Fenster-Schließen, Editorende oder Assembly-Reload wird nur der eigene PowerShell-/Build-Prozessbaum beendet. Aktive Vorgänge sperren weitere Menüaktionen; Play Mode und Kompilierung sperren die Ausführungsbefehle ebenfalls. Detailprotokolle und Menü-Anfragen bleiben unter dem ignorierten `Builds/Android/menu-*`. Die Anfragen enthalten keine Passwörter oder Schlüssel; sie werden über einen eingeschränkten Skript-Dispatcher ausgeführt.

**Production** benötigt eine ausdrückliche Bestätigung im Editor. Die normale Einrichtung erzeugt keine Google-Service-Accounts. „Signierung einrichten“ öffnet ein interaktives PowerShell-Fenster für die vorhandenen lokalen Signierungsdaten; Play-Schlüsselzuordnung weiterhin gezielt über `Set-ReleaseSecrets.ps1 -ServiceAccountJsonPath` vornehmen. „Ziel prüfen“ bei Drive ist lokal; „Versioncode prüfen“ bei Play verwendet die Play-API. Drive-Menüeinträge bestätigen ausschließlich die lokale Kopie, keinen Cloud-Upload. Bestehende Drive-Dateien werden ohne `-Force` nicht ersetzt; der Menüaufruf setzt diesen Schalter nicht.

### Buildcache und Installation

Menü und Skripte lesen zuerst die lokale `Builds/Android/device-build.json`, danach `%LOCALAPPDATA%/WRBStudio/<SecretsKey>/device-build.json`. Einstellungen im Menü oder mit `Set-AndroidDeviceBuildSettings.ps1` speichern. Gerätekennungen werden nicht dauerhaft konfiguriert. Alle Menüs verwenden den dort gewählten Cache; ohne Angabe gilt der Standardcache unter LocalAppData. Mindestens 25 GB freier Platz wird vor Builds geprüft (`-MinimumFreeGB` beim Gerätebefehl konfigurierbar).

Nur ein **neuer leerer Ordner** oder ein vom Tool bereits für dieses Projekt markierter Cache wird gespiegelt. Andere bestehende Ordner, fremde Tool-Caches, Quell-/Eltern-/Laufwerkswurzeln und verlinkte Cachepfade werden abgewiesen. Parallele Nutzung desselben Caches wird gesperrt. Ältere Branch-Caches ohne Besitzmarkierung werden nicht automatisch übernommen: einen neuen leeren Ordner wählen. Die gespiegelten Assets/Packages/ProjectSettings dürfen vom Tool ersetzt werden. Relative lokale Paketverweise werden nur in der Cache-Manifestdatei angepasst, damit externe lokale Pakete am ursprünglichen Ort erreichbar bleiben; das Quellmanifest bleibt erhalten.

Bei erfolgreicher Installation wird die installierte Version geprüft und anschließend die Launcher-Activity aus dem geprüften APK mit Androids Activity Manager gestartet. `-NoLaunch` bleibt für reine CLI-Installation verfügbar. Ein Startfehler wird im Installationsnachweis separat erfasst und als Fehler beendet; die bereits erfolgreiche Installation bleibt erhalten. Es erfolgt weder ein neuer Build noch eine Deinstallation wegen eines Startfehlers. Einen Signaturwechsel samt Datenlöschung erlaubt weiterhin ausschließlich der explizite CLI-Schalter `-ReplaceExistingApp`; kein Menüeintrag setzt ihn.

Die Release-CLI bleibt verwendbar. Mit optionalem `-BuildRoot <dedizierter Cache>` unterstützen auch `Build-Apk.ps1`, `Build-Aab.ps1`, Drive und Play einen offenen Quell-Editor. Ohne diesen Parameter bleibt für diese Befehle das Schließen des Editors erforderlich.

### Prüfstand dieser Erweiterung

Die Geräte- und Fortschrittsfunktionen aus `codex/build-and-install-android` und `codex/android-device-progress` wurden übernommen und für das gesamte Toolmenü erweitert. PowerShell-Prüfungen verwenden temporäre Projektkopien und simulierte Unity-/ADB-/aapt-Programme, einschließlich Installations-/Startfehlern und Schutz der Quell-Einstellungen. Beide Editor-Dateien kompilieren gegen Unity 2019.3.15f1 und 6000.3.25f1; alle 18 Menüaktionen wurden außerdem in einem leeren Unity-6.3-Batch-Testprojekt als registriert bestätigt (`Test-UnityMenuRegistration.ps1`). Ein echter Android-Build, eine Geräteinstallation und die Anzeige/Klickbedienung im geöffneten Zielprojekt müssen weiterhin separat geprüft werden. Es erfolgt keine automatische Übernahme in Spielprojekte.
