# Changelog

## 1.0.0-rc.7 - Release Candidate

### Fixed

- Verified updater finalization now closes the checksum input before renaming
  the verified installer.
- Fresh installs open their main window visibly by default.
- Read usernames now speaks the actual Twitch chatter as `<user> said: <message>`.

### Added / improved

- Persisted **Ignore Emoji** Chat Reader setting, enabled by default.
- One release stream: ChatVox now discovers the newest valid published release;
  Stable/Preview selection has been removed.

### Preserved

- Single-instance/tray behavior, responsive monitor-aware UI, local voice cache,
  Twitch recovery, words-only structured Twitch-emote filtering, and the 28-voice
  eSpeak-free English runtime.

## 1.0.0-rc.5 - Release Candidate

### Added

- Public GitHub Releases distribution and update discovery.
- Public GitHub Release discovery.
- Verified installer download using a matching SHA-256 asset.
- Public privacy, licensing, and release documentation.

### Preserved

- eSpeak-free local speech and 28 American/British English Kokoro voices.
- Twitch saved-auth restoration, words-only chat speech, tray operation,
  Windows startup, and start-minimized behavior.
