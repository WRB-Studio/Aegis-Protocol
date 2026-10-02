# Unity Android Release

Wiederverwendbarer Windows-Workflow für signierte APKs/AABs und Google-Play-Uploads. Unity-Version und sichtbare App-Version werden aus dem Projekt gelesen. Kein Upload allein durch Git-Push.

Integrierter Vorlagenstand: [unity-google-play-uploader](https://github.com/WRB-Studio/unity-google-play-uploader), Commit `df31626` (02.10.2026). Die bestehende Aegis-Konfiguration, Credential-Ablage und Unity-Buildklasse bleiben erhalten. Weitere Einrichtungshinweise stehen in der [Integrationsanleitung](https://github.com/WRB-Studio/unity-google-play-uploader/blob/main/INTEGRATION.md).

## Anderes Projekt einrichten

1. Den Ordner `scripts` und `Assets/Editor/UnityAndroidBuild.cs` samt `.meta` ins Unity-Projekt kopieren. Das Vorlagen-ZIP enthält diese Dateien ohne Zugangsdaten.
2. `scripts/release.config.json` anpassen:

```json
{
  "PackageName": "com.YourStudio.YourGame",
  "ArtifactName": "YourGame",
  "SecretsKey": "YourGame"
}
```

`PackageName` muss mit dem Android Application Identifier in Unity und der App in Google Play übereinstimmen. `ArtifactName` benennt Build-Dateien. `SecretsKey` muss pro App eindeutig sein. Optional kann `DriveDirectory` ergänzt werden.

3. Ruby mit Fastlane installieren (`gem install fastlane`). Unity benötigt Android Build Support, SDK/NDK und OpenJDK. Die App muss bereits in der Play Console angelegt sein; der Service-Account benötigt Zugriff auf diese App und den gewünschten Track. Für vorhandene Apps den passenden Upload-Keystore verwenden.
4. Zugangsdaten einmalig hinterlegen:

```powershell
./scripts/Set-ReleaseSecrets.ps1 -KeystorePath 'D:/Keys/game.keystore' -KeyAlias 'game' -ServiceAccountJsonPath 'D:/Keys/play-service-account.json'
```

Passwörter werden verdeckt abgefragt und verschlüsselt unter `%LOCALAPPDATA%/WRBStudio/<SecretsKey>/release-secrets.xml` gespeichert, gebunden an das Windows-Benutzerkonto. Keystore, Service-Account-JSON und Secret-Datei gehören nicht ins Repository oder Vorlagen-ZIP. Aegis verwendet seine bestehende Secret-Datei unverändert.

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
```

Unity vor dem automatisierten Build speichern und schließen. Falls Unity nicht automatisch gefunden wird, `-UnityPath` angeben. Builds und Logs liegen unter `Builds/Android`. Ein erfolgreicher Build schreibt einen Nachweis mit Paketname und tatsächlichem Versioncode; ohne passenden Nachweis startet kein Upload. Projektversion und Signierungswerte werden nach dem Build wiederhergestellt.

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
| Lokale Drive-/Keystore-Pfade und persönliche Aliasnamen | Für eine neutrale Vorlage weglassen |
| Builds, Build-Nachweise und Logs | Lokal behalten |

Die eigentlichen Zugangsdaten liegen außerhalb des Unity-Projekts. Ein normaler Git-Push nimmt sie deshalb nicht automatisch mit. Kopierst du sie ins Projekt, musst du sie dort zusätzlich ausschließen. Ein privates Repository ersetzt diesen Schutz nicht.

Die `release.config.json` enthält nur Projektwerte. `DriveDirectory` weglassen, wenn darin ein persönlicher Pfad stehen würde. Für den Drive-Export stattdessen bei Bedarf `-DriveDirectory` lokal angeben. Keine Passwörter, Tokens oder Schlüssel in diese JSON-Datei schreiben.

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

**Nicht pauschal `*.json` im Unity-Spielprojekt ignorieren:** beispielsweise `Packages/manifest.json` und `Packages/packages-lock.json` gehören zum Projekt. Das eigenständige Uploader-Repository ignoriert dagegen standardmäßig alle JSON-Dateien außer seiner Beispielkonfiguration; diese Regel nicht ungeprüft in ein Spielprojekt übernehmen.

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
