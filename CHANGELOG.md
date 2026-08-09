# Changelog

## Release visibility

RC.1 through RC.4 were internal, pre-public release candidates. Their local
installer records and backup snapshots are retained, but they were not published
as GitHub Releases and are not being recreated retroactively. Public GitHub
prerelease distribution began with RC.5.

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

## 1.0.0-rc.6 - Release Candidate

### Added / improved

- Added true single-instance handling: a second launch restores the existing
  ChatVox window instead of creating another tray, Twitch, or TTS instance.
- Made the window resizable and monitor-work-area aware, including persisted
  placement, off-screen recovery, and bounded scrolling for long diagnostics.
- Moved managed Kokoro voice runtime assets to the per-user cache and made cache
  repair atomic, while keeping the Program Files installation read-only.

### Preserved

- Public GitHub prerelease update discovery, eSpeak-free 28-voice local speech,
  Twitch authorization restoration, EventSub recovery, queue controls, tray
  operation, and Windows startup support.

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

## 1.0.0-rc.4 - Internal Release Candidate

### English packaging and licensing hardening

- Finalized the eSpeak-free English edition with 28 supported American and
  British Kokoro voices.
- Removed eSpeak, `voices-zh`, and unsupported voice assets from the installer
  payload.
- Added managed English phonemization with a safe skip path when usable English
  phonemes are unavailable, so unsupported input cannot terminate the speech
  worker.
- Preserved saved Twitch authorization and safely fell back to `af_heart` when a
  legacy selected voice was no longer part of the supported package.

### Internal validation

- Release build completed with 0 errors and 0 warnings; the internal test suite
  reported 65 passing tests.
- A local RC.4 installer was produced and archived, but no public GitHub release
  or public-source publication was made for this candidate.

## 1.0.0-rc.3 - Internal Release Candidate

### UX and runtime hardening

- Added tray-first operation: closing or minimizing hides ChatVox, while the tray
  menu can reopen the window, pause/resume speech, or perform a full exit.
- Added independent per-user **Start ChatVox with Windows** and **Start minimized
  to tray** preferences.
- Kept installed files under `C:\Program Files\ChatVox` and settings,
  authorization, logs, and update working files under `%LOCALAPPDATA%\ChatVox`.
- Improved words-only chat speech by removing Twitch emotes and Unicode emoji;
  emote-only messages are ignored.
- Added bounded operational diagnostics and the safe, non-blocking groundwork for
  a future update UI.

## 1.0.0-rc.2 - Internal Release Candidate

### Twitch authorization reliability

- Classified token validation results as success, confirmed unauthorized, or
  transient failure instead of treating every failure as lost authorization.
- Refreshed only after a confirmed 401 response and preserved saved authorization
  during transient DNS, socket, timeout, HTTP 429, and HTTP 5xx failures.
- Added atomic DPAPI authorization updates, bounded retry/backoff, EventSub
  reconnect and subscription recovery, and bounded safe diagnostics that omit
  tokens, device codes, authorization headers, DPAPI blobs, and chat bodies.
- Increased automated coverage from 32 to 50 passing tests.

## 1.0.0-rc.1 - Internal Release Candidate

### Baseline

- First internal Windows candidate integrating local Kokoro speech with Twitch
  device authorization, saved local authorization, EventSub chat intake, filters,
  and a bounded speech queue.

### Known issue discovered after this candidate

- The initial validation monitor could mistake transient Twitch/network failures
  for invalid authorization and unnecessarily return the app to **Authorization
  Required**. RC.2 corrected that behavior.
