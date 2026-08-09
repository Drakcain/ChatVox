# Changelog

## 1.0.0-rc.11 - Release Candidate

### Simplified

- Removed the manual Username Pronunciation Overrides feature from the UI,
  settings, and runtime pipeline. ChatVox now uses its automatic conservative
  username pronunciation for every chatter, with no per-streamer setup.

## 1.0.0-rc.10 - Release Candidate

### Improved

- Made the Username Pronunciation Overrides panel readable in dark mode.
- Added clear instructions, an example, and an explicit reminder that the
  field is optional and only needed when automatic username speech is wrong.

## 1.0.0-rc.9 - Release Candidate

### Added / improved

- Added safe, high-confidence Twitch username pronunciation for common wrappers,
  recognized word fragments, and familiar leetspeak forms.
- Added a local username-pronunciation override setting for cases where a
  streamer wants a specific pronunciation.
- Added a 600-handle safety corpus and gold-reference pronunciation tests.
- Clarified the test controls: **Test Message** speaks normal chat text as
  entered, while **Test Username** uses the same username normalization as live
  Twitch chat.

### Preserved

- Username normalization is speech-only; Twitch identity, filtering, and
  EventSub behavior continue to use the original Twitch handle.
- No eSpeak runtime or new pronunciation dependency is included.

## 1.0.0-rc.8 - Release Candidate

- Fixed startup visibility so normal launches do not hide unless Start minimized is explicitly enabled.
- Added explicit Normal, Windows startup, post-install, and post-update launch reasons.
- Post-install and post-update launches always open visibly.
- Windows startup invokes ChatVox with its explicit launch context.

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
