# ChatVox 1.0.0-rc.7

Release Candidate — updater correctness and first-launch/pipeline hardening.

## Fixed

- Closes the verified update file before finalizing the `.partial` installer,
  preventing the Windows rename `IOException` found during RC.5-to-RC.6 testing.
- Fresh installs open visibly instead of unexpectedly disappearing to the tray.
- Read usernames now speaks the actual Twitch chatter: `<user> said: <message>`.

## Improved

- ChatVox has one update stream and always follows the newest valid published
  ChatVox release. The Stable/Preview selector has been removed.
- Adds a persisted **Ignore Emoji** toggle, enabled by default. Structured Twitch
  emotes remain filtered regardless of this toggle.

## Preserved

- True single-instance behavior, responsive monitor-aware window, 28 English
  Kokoro voices, eSpeak-free runtime, LocalAppData voice cache, Twitch auth
  restoration, EventSub recovery, and mandatory SHA verification.
