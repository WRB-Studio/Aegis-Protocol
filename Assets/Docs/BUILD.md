# Build und Tests

Für automatisierte signierte APK-/AAB-Builds und Google-Play-Uploads siehe [Unity Android Release](../../scripts/README.md). Die projektbezogenen Werte stehen in `scripts/release.config.json`; Zugangsdaten bleiben außerhalb des Repositorys. Der nächste freie Play-Versioncode wird automatisch ermittelt. Ein Upload erfordert einen passenden Nachweis aus dem aktuellen Build; Production zusätzlich `-ConfirmProduction`.

Uploader-Vorlagenstand vom 02.10.2026: `df31626`. Store-Texte und Versionshinweise lassen sich optional über `scripts/Submit-PlayMetadata.ps1` oder zusammen mit einem AAB übertragen; Schema siehe `scripts/examples/play-metadata.json`. Ohne Metadaten bleiben vorhandene Texte erhalten. Release-Entwürfe und verzögerte Einreichung werden unterstützt. Lokale Release-/Metadatenprüfungen bestanden; die Play-Zugangsprüfung ergab Versioncode 5 als höchsten vorhandenen Code und 6 als nächsten freien Code. Bei dieser Aktualisierung wurde kein Build oder Release hochgeladen.

Unity `6000.3.15f1` mit **Android Build Support**, Android SDK/NDK und OpenJDK verwenden. Die Startszene ist `Assets/Scenes/MainScene.unity` und steht bereits in den Build Settings.

1. Projekt in Unity Hub öffnen und `MainScene` laden.
2. Über **File > Build Profiles** Android auswählen und aktivieren.
3. Für einen Gerätetest **Development Build** aktivieren, ein Pixel per USB-Debugging verbinden und **Build And Run** ausführen.
4. Im **Window > General > Test Runner** sowohl EditMode als auch PlayMode ausführen. Die PlayMode-Tests starten `MainScene` selbst und verwenden ein temporäres Save-Verzeichnis.

Für einen Release-Build müssen Version/Versioncode, Ziel-API, Signierung und Store-Angaben vor dem Export geprüft werden. Keystore und Zugangsdaten gehören nicht ins Repository. Der signierte AAB und der vollständige Gerätetest sind als U35–U38 offen.

Das Spiel speichert lokal. Während einer laufenden Welle wird ein Zustand vor dem ersten Spawn gesichert; nach einem App-Neustart beginnt diese Welle erneut. Nach Abschluss der Welle wird der aktuelle Zustand gespeichert. Android-Hintergrundwechsel und Wiederherstellung sind noch auf dem Gerät zu verifizieren.

Nachweis vom 18.09.2026: Ein isolierter Android-Development-Build mit Unity `6000.3.15f1` war erfolgreich. Das erzeugte APK hat `targetSdkVersion 36`, Version `1.2` und Versioncode `3`; es wurde noch nicht auf einem Gerät installiert. Das finale Entwicklungs-APK liegt außerhalb des Repositorys unter `C:/Temp/Aegis-Protocol-Tests-20260918/AegisDev-final.apk`.
