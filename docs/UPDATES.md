# Updates

ChatVox checks the public `Drakcain/ChatVox` GitHub Releases feed at startup
when enabled. It selects the newest valid published ChatVox release, including
release candidates, only when that version is newer than the installed version.
The update path downloads only the exact versioned installer plus its matching
SHA-256 file over HTTPS. The download stream and checksum stream are closed
before the verified `.partial` file is renamed to its final installer name. A
mismatched or malformed checksum is never executable.

Updates are user-approved. Normal upgrades preserve `%LOCALAPPDATA%\ChatVox`
including Twitch authorization, settings, and logs.
