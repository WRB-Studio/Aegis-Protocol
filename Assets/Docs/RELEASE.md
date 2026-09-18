# Release-Stand (18.09.2026)

## Store-Textentwurf (Englisch)

**Kurzbeschreibung:** Build a space station, defend it, and survive escalating enemy waves.

**Beschreibung:**

Protect a vulnerable space station against incoming fleets. Start with a basic tower, collect materials from defeated enemies, and choose which modules to build next.

Expand your defenses with a shield, radar, drones and faster ammunition. Upgrade their capabilities while each new wave brings tougher enemy types. Manage repairs, collect resources and aim for a higher score across repeatable runs.

Aegis Protocol is a portrait sci-fi defense game with local progress and replayable waves.

## Vorhandene Medien

`Assets/GameContent/Publishing/Featuregraphic.png` ist 1024 × 500 px, RGB, und passt zum aktuellen Format der Google-Play-Feature-Grafik. `Logo_withText_512.png` ist 512 × 512 px, aber RGB; das Store-Icon benötigt ein 32-Bit-PNG mit Alpha. Die vier vorhandenen `Screenshoot*.png` sind rund 770 × 1550 px und zeigen einen älteren Spielstand. Mindestens zwei neue, echte Screenshots der aktuellen Android-Version aufnehmen; für Spiele empfiehlt Google mindestens drei Hochformatbilder mit 1080 × 1920 px. Keine vorhandenen Bilder hochskalieren, um Gameplay-Änderungen vorzutäuschen.

Die Herkunft und Nutzungsrechte der Bild- und Audioelemente sind hier nicht belegt und müssen vom Entwickler ergänzt werden. Den bestehenden Store-Eintrag samt Text, Altersfreigabe, Datenschutzangaben und Medien erst vor Veröffentlichung in der Play Console abgleichen.

Quellen: [Google Play: Preview Assets](https://support.google.com/googleplay/android-developer/answer/9866151?hl=en-GB), [Google Play: Target API](https://support.google.com/googleplay/android-developer/answer/11926878?hl=en).

## Abnahme auf Android

- Development-Build auf dem Pixel installieren: Start, Pause/Zurück, Musik/Effekte, schmaler Bildschirm und Aussparung prüfen.
- Ersten Run mit Bau, manueller und automatischer Sammlung, Schild und erster Drohne spielen. Kosten und Schwierigkeit der ersten zehn Wellen notieren.
- Eine Welle mit Käufen und Schaden unterbrechen, App aus dem Hintergrund neu starten und den Wellen-Checkpoint prüfen.
- Drei Replays, zerstörte Module, Wiederaufbau, Upgradegrenzen und einen längeren Run bis zur Bosswelle prüfen.
- Frametimes, Speicher und GC auf einem schwächeren Gerät messen; erst danach über Pooling, Texturen und Audioimport entscheiden.

Für neue Apps und Updates verlangt Google Play seit 31.08.2026 API 36. Dieses Projekt verwendet aktuell die automatische Ziel-API; die installierte Unity-Android-SDK enthält API 36. Der erfolgreiche Development-Build meldet `targetSdkVersion 36`; die Manifest-Zielversion des finalen AAB erneut prüfen. Ein signierter AAB sowie der Store-Testweg sind noch offen.

Signierung: Ein Keystore und Alias sind lokal konfiguriert, die Passwörter sind im Projekt nicht hinterlegt. Für U38 müssen sie im Unity-Editor eingegeben werden; niemals in Git oder diese Dokumentation schreiben. Erst danach Version/Versioncode festlegen, AAB signieren und auf dem vorgesehenen Testweg installieren.
