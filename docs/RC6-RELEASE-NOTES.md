# ChatVox 1.0.0-rc.6

Release Candidate — responsive-window, single-instance, and voice-cache hardening.

## Added and improved

- ChatVox is now a true single-instance application for the current Windows session.
  Launching the desktop shortcut, Start Menu shortcut, or executable again restores
  the existing window instead of creating another tray/Twitch/TTS instance.
- The window is resizable and monitor-work-area aware, with a practical ultrawide
  first-launch width cap, vertical content scrolling, bounded diagnostics scrolling,
  persisted normal bounds, maximized-state restoration, and off-screen recovery.
- Kokoro voice runtime assets now initialize from
  `%LOCALAPPDATA%\ChatVox\speech\voices`, keeping installed Program Files assets
  read-only. Missing, altered, or obsolete managed voice-cache files are repaired
  atomically from the bundled approved assets.

## Preserved

- 28 American/British English Kokoro voices; no eSpeak and no `voices-zh` payload.
- Twitch authorization restore, EventSub recovery, words-only speech, queue controls,
  system tray, Windows startup support, and verified GitHub Release updates.

## Release status

This is a prerelease. RC.5-to-RC.6 updater acceptance, physical display validation,
and SQM-PC live validation remain release gates before 1.0.0.
