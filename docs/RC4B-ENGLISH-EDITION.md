# ChatVox RC.4B - eSpeak-Free English Voice Edition

## Release candidate identity

- Product: ChatVox
- Version: `1.0.0-rc.4`
- Target: Windows x64 self-contained installer
- Scope: American and British English Kokoro voices only

## Included voice set

The installer contains exactly 28 compatible Kokoro English voice assets:

- 11 American Female (`af_*`)
- 9 American Male (`am_*`)
- 4 British Female (`bf_*`)
- 4 British Male (`bm_*`)

No other Kokoro language/locale category is exposed or packaged.

## eSpeak removal and runtime hardening

- The publish target removes `espeak`, `voices-zh`, and every voice asset that
  is not one of the four approved English prefixes.
- ChatVox uses its own English phonemization path and calls Kokoro with
  precomputed tokens. Its Misaki fallback returns an empty safe result rather
  than starting eSpeak.
- A message that produces no supported English phonemes is skipped with a
  diagnostic; it cannot terminate the speech worker.
- Older settings selecting a removed voice are normalized to `af_heart`; other
  settings are retained by the existing settings normalizer. The separate
  DPAPI authorization store and its location are unchanged.

## Validation evidence

On 2026-08-08:

- `dotnet restore .\ChatVox.slnx`: passed.
- `dotnet build .\ChatVox.slnx -c Release --no-restore`: 0 errors, 0 warnings.
- `dotnet test .\ChatVox.slnx -c Release --no-build --no-restore`: 65 passed,
  0 failed.
- The English phonemizer test covers contractions, numbers, punctuation,
  names, accented borrowed words, symbols, Unicode quotes, and a long normal
  chat sentence without an eSpeak fallback.
- The actual published runtime completed synthesis/playback for all 28 retained
  voices using `Hello, ChatVox.` while the published payload contained no
  `espeak` directory.
- Fresh publish inventory: 28 approved voice assets, 0 unsupported voice
  assets, 0 `voices-zh` assets, and 0 eSpeak items.

Automated playback completion proves the runtime path completed. A human must
still listen to the final installer on the intended output device to certify
audible quality for every voice.

## Installer

- Artifact: `READY TO INSTALL\ChatVox-1.0.0-rc.4-Setup.exe`
- SHA-256: `6FA10AF68E2D0B16F0490DF2F21BCDD0AC8F6105566568B2FBDC7E91272586A1`
- Size: 238,339,517 bytes

The installer is built from `build\publish\current`; its Inno Setup rule
recursively packages that current publish directory and excludes PDB files.
The release payload includes `THIRD-PARTY-NOTICES.txt` and
`LICENSES\README.md`.

## Deliberately not done in RC.4B

- No GitHub repository was created.
- No public release was published.
- No ChatVox source license was selected or applied.
- No real Twitch authorization was performed as part of this release pass.
