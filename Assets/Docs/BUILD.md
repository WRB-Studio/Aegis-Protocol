# Build und Tests

Für automatisierte signierte APK-/AAB-Builds und Google-Play-Uploads siehe [Unity Android Release Tools](../../scripts/README.md). Die projektbezogenen Werte stehen in `scripts/release.config.json`; Zugangsdaten bleiben außerhalb des Repositorys. Der nächste freie Play-Versioncode wird automatisch ermittelt. Ein Upload erfordert einen passenden Nachweis aus dem aktuellen Build; Production zusätzlich `-ConfirmProduction`.

Tools-Vorlagenstand vom 10.10.2026: `8d9fce859fa9a606a621acd524665e109e7b8e28` aus `unity-android-release-tools/origin/main`. Alle Skripte und beide Editor-Dateien wurden gemeinsam übernommen. Das Menü unter **Tools > Unity Android Release Tools** bietet APK/AAB-Builds, Geräteinstallation und Appstart, Drive/Play sowie Einrichtung und Prüfungen. Menü-Builds nutzen eine dedizierte isolierte Buildkopie, damit der Quell-Editor offen bleiben kann. Builds, Installation und Uploads benötigen jeweils einen eigenen Auftrag.

Projektkonfiguration, SecretsKey, bestehender Secret-Speicherort, Build-Namespace und vorhandene .meta-GUIDs bleiben erhalten. Das neue Menü verwendet denselben angepassten lokalen Speicherort wie die Skripte. Der bestehende DriveDirectory-Altwert bleibt erhalten; Drive-Befehle lesen die lokale drive-export.json. Invoke-AegisAndroidBuild bleibt kompatibel. Anpassungen an den Tooltests: Geräte-Fixture verwendet den projektspezifischen Secret-Speicherort; Compiler-/Menütest übernehmen ausschließlich die zwei Tool-Editor-Dateien statt anderer Spiel-Editor-Scripte.

Prüfungen: Offline-Suite unter Windows PowerShell 5.1/PowerShell 7; beide Editor-Dateien gegen Unity 6000.3.25f1 mit Warnungen als Fehler kompiliert; alle 18 Menüaktionen in einem leeren temporären Unity-Projekt registriert; vorhandene lokale Secrets und Drive-CheckOnly geprüft. Keine Menüaktion, kein echter Android-Build, keine Geräteinstallation und kein Play-/Drive-Upload bei diesem Update. Bedienung und Geräteablauf im Spielprojekt bleiben praktisch zu testen.

Store-Texte und Versionshinweise lassen sich optional über `scripts/Submit-PlayMetadata.ps1` oder zusammen mit einem AAB übertragen; Schema siehe `scripts/examples/play-metadata.json`. Ohne Metadaten bleiben vorhandene Texte erhalten. Release-Entwürfe und verzögerte Einreichung werden unterstützt. Für einen Rückweg liegt vor dem Update eine lokale Sicherung der integrierten Skripte und des Buildhelfers unter `Builds/ReleaseWorkflowBackups/`; die konkrete Sicherung ist in `Builds/release-update-backup.txt` festgehalten. Skripte und Helfer bei einem Rollback gemeinsam zurücksetzen und unabhängige Spieländerungen erhalten.

Unity `6000.3.15f1` mit **Android Build Support**, Android SDK/NDK und OpenJDK verwenden. Die Startszene ist `Assets/Scenes/MainScene.unity` und steht bereits in den Build Settings.

1. Projekt in Unity Hub öffnen und `MainScene` laden.
2. Über **File > Build Profiles** Android auswählen und aktivieren.
3. Für einen Gerätetest **Development Build** aktivieren, ein Pixel per USB-Debugging verbinden und **Build And Run** ausführen.
4. Im **Window > General > Test Runner** sowohl EditMode als auch PlayMode ausführen. Die PlayMode-Tests starten `MainScene` selbst und verwenden ein temporäres Save-Verzeichnis.

Für einen Release-Build müssen Version/Versioncode, Ziel-API, Signierung und Store-Angaben vor dem Export geprüft werden. Keystore und Zugangsdaten gehören nicht ins Repository. Der signierte AAB und der vollständige Gerätetest sind als U35–U38 offen.

Das Spiel speichert lokal. Während einer laufenden Welle wird ein Zustand vor dem ersten Spawn gesichert; nach einem App-Neustart beginnt diese Welle erneut. Nach Abschluss der Welle wird der aktuelle Zustand gespeichert. Android-Hintergrundwechsel und Wiederherstellung sind noch auf dem Gerät zu verifizieren.

Nachweis vom 18.09.2026: Ein isolierter Android-Development-Build mit Unity `6000.3.15f1` war erfolgreich. Das erzeugte APK hat `targetSdkVersion 36`, Version `1.2` und Versioncode `3`; es wurde noch nicht auf einem Gerät installiert. Das finale Entwicklungs-APK liegt außerhalb des Repositorys unter `C:/Temp/Aegis-Protocol-Tests-20260918/AegisDev-final.apk`.
