# Public release checklist

- [ ] VERSION, build, and tests are current.
- [ ] Secret scan is clean.
- [ ] MIT LICENSE, privacy document, and third-party notices are present.
- [ ] Exactly 28 English voices; no eSpeak or `voices-zh` payload.
- [ ] Installer and SHA-256 file match.
- [ ] Correct `Drakcain/ChatVox` tag and prerelease setting.
- [ ] Public installer/SHA download works.
- [ ] Updater discovers the newest valid release (including RC prereleases), validates SHA-256, and finalizes the verified installer.
- [ ] No Stable/Preview channel UI, settings, or active documentation remains.
- [ ] Fresh-profile launch is visible; explicit start-minimized preference is preserved on upgrade.
- [ ] Ignore Emoji and Read usernames behavior is covered by regression tests.
- [ ] Upgrade preservation and SQM-PC acceptance are complete.
