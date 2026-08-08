# RC.4A — eSpeak NG GPL Redistribution Decision

**Status:** NOT PASS — public-release license gate remains open.

**Scope:** Evidence and controlled packaging experiment only. This pass did not
create a GitHub repository, publish a release, change ChatVox's source license,
or alter the RC.3 installer.

## Current distribution inventory

The RC.3 publish payload contains `espeak\` because
`src\ChatVox\ChatVox.csproj` copies `$(OutDir)espeak\**\*` to the publish
directory after publish. The installer includes all files in
`build\publish\current` except PDB files, so these files are included in the
installer.

| Item | Count / size | Purpose | License |
| --- | ---: | --- | --- |
| eSpeak NG native DLLs | 5 | Platform backends, including Windows x64 | GPL-3.0-or-later |
| eSpeak NG data files | 364 | Dictionaries, phoneme data, voices, configuration | GPL-3.0-or-later |
| eSpeak NG support files | 3 | README, HOW_TO_USE, license | GPL-3.0-or-later notice material |
| Total eSpeak payload | 372 files / 20.01 MiB | KokoroSharp text phonemization backend | GPL-3.0-or-later |

The native DLLs are:

- `espeak-ng-win-amd64.dll` — required backend on the current Windows x64 target
- `espeak-ng-win-arm64.dll`
- `espeak-ng-linux-amd64.dll`
- `espeak-ng-macos-amd64.dll`
- `espeak-ng-macos-arm64.dll`

## Runtime trace

ChatVox calls `KokoroService.SpeakAsync`, which calls
`KokoroTTS.SpeakFast(text, voice, ...)`. KokoroSharp's normal text path calls
`Tokenizer.Tokenize`, then `Tokenizer.Phonemize`.

- American and British English normally use MisakiSharp's native English G2P.
  Its fallback for out-of-dictionary words calls `Phonemize_Internal`, which
  launches eSpeak NG.
- Mandarin Chinese uses a managed Chinese G2P path, but embedded English falls
  back through the English path.
- Spanish, French, Hindi, Italian, Japanese, and Portuguese use
  `Phonemize_Internal` directly in this KokoroSharp version.
- `Phonemize_Internal` launches the bundled platform executable path when it
  exists, otherwise the external command name `espeak-ng`, and sets
  `ESPEAK_DATA_PATH` to the configured eSpeak data directory.

Therefore ChatVox does not call eSpeak directly, but it depends on eSpeak
indirectly through KokoroSharp's normal plaintext phonemization path.

## Controlled eSpeak-free runtime test

Lab payload:

`build\lab\rc4a-espeak-free-runtime`

The lab copied the current RC.3 publish payload file-by-file while excluding
only the `espeak\` directory. It contains the bundled model and all 54 supported
v1 voices, and contains zero `espeak\` payload files. A disposable probe invoked
the actual published `ChatVox.Speech.KokoroService` and waited for Kokoro's real
playback-completion callback.

| Test case | Result without eSpeak | Evidence |
| --- | --- | --- |
| `af_heart`, ordinary English | PASS | Playback completed |
| `af_heart`, accented/less-common English | FAIL | Attempted to launch external `espeak-ng` |
| `af_bella`, English | PASS | Playback completed |
| `am_adam`, English | PASS | Playback completed |
| `bf_alice`, English | PASS | Playback completed |
| `bm_daniel`, English | PASS | Playback completed |
| `zf_xiaobei`, Mandarin Chinese | PASS | Playback completed |
| `ef_dora`, Spanish | FAIL | Attempted to launch external `espeak-ng` |
| `ff_siwis`, French | FAIL | Attempted to launch external `espeak-ng` |
| `hf_alpha`, Hindi | FAIL | Attempted to launch external `espeak-ng` |
| `if_sara`, Italian | FAIL | Attempted to launch external `espeak-ng` |
| `jf_alpha`, Japanese | FAIL | Attempted to launch external `espeak-ng` |
| `pf_dora`, Portuguese | FAIL | Attempted to launch external `espeak-ng` |

Result: 6 passed, 7 failed. The failures are expected runtime dependency
failures, not audio-device or model failures.

## Architecture options

### A — Remove eSpeak entirely

**Not viable for the current advertised 54-voice product.** It removes all
normal phonemization for seven exposed non-English categories and makes English
input unreliable whenever MisakiSharp requires its eSpeak fallback.

### B — External eSpeak installation

**Not ready / not validated.** KokoroSharp does fall back to the command name
`espeak-ng` if the bundled directory is absent, but ChatVox currently has no
consumer setup, discovery, validation, data-path configuration, or support path
for an external installation. Moving the component outside the installer may
change distribution facts, but does not itself resolve GPL obligations; obtain
legal review before treating this as a public-release solution.

### C — Permissively licensed replacement

**Research candidate: `gruut` (MIT), not a drop-in solution.** Its documented
implementation is Python-based, the upstream repository is archived, and its
documented language list does not cover ChatVox's Hindi, Japanese, or Chinese
categories. Its language/model data must also be audited separately. It does
not provide a supported .NET/KokoroSharp integration in the current project.
Replacing eSpeak would require a maintained local bridge or a native .NET
phonemizer, IPA/token compatibility work, per-language quality validation, and
a new dependency/provenance audit. No replacement is approved or implemented
by this pass.

### D — Ship eSpeak and comply with GPLv3

**Technically possible but requires legal review and a deliberate distribution
plan.** At minimum, the distributor must preserve required notices, include the
GPL terms, and make the applicable corresponding source available using a GPLv3
section 6-compliant method. The exact scope of corresponding source and any
combined-work implications for this packaging need qualified legal review; this
document is technical evidence, not legal advice.

## Recommendation

Do not remove eSpeak from the current RC.3 package and do not proceed with a
public release yet. The first viable public route is either a reviewed GPLv3
distribution plan for the shipped eSpeak component or a separately engineered,
fully validated permissive phonemizer replacement.

## ChatVox source license

MIT remains a sensible recommendation for ChatVox-owned source only, subject to
completion of the third-party notices/provenance review. It is **not applied**
by this pass.

## Primary evidence

- `src\ChatVox\ChatVox.csproj`
- `src\ChatVox\Speech\KokoroService.cs`
- KokoroSharp 0.8.0 source commit
  `0e26ae0cdb2f612d3018cbf11b06daad69cc048c`
- `build\lab\rc4a-espeak-free-runtime\Program.cs`
- `build\lab\rc4a-espeak-free-runtime\Rc4aProbe.csproj`

External sources consulted 2026-08-08:

- https://github.com/Lyrcaxis/KokoroSharp
- https://github.com/hexgrad/kokoro
- https://github.com/espeak-ng/espeak-ng
- https://www.gnu.org/licenses/gpl-3.0.html
- https://github.com/rhasspy/gruut
