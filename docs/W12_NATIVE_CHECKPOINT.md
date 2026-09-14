# W12 — completed v0.4.0 Candidate checkpoint

All required W1-W12 work and the 18-item integrated v0.4 acceptance are implemented and native-verified at this checkpoint. Keep PR #6 Draft; this completion record does not authorize merging main.

## Exact evidence

- Verified source: `4938217471f183664ba40e5299fcd3bbb6432568`.
- Checkout tree: `0689ddf2d58dfb678d29d1b2e9e999965211f8bc`.
- Native run: `34868820375`, attempt 1, Windows / real YMM4 4.55.1.1 Lite.
- **384 native assertions PASS**, retaining P1-P9 and W3-W11; W12_UI, W12_SELECTORS, W12 and V04 PASS.
- `v04-acceptance.json`: all 18 requirements PASS; all five Profile families covered.
- Release/proof builds: **0 Warning / 0 Error**, both built with warnings treated as errors.
- Open XML validation, native release DLL identity smoke, source archive and versioned package checks PASS.
- Artifact: `10357769100`, SHA256 `c33246439e84e3dbfc6adb9fc36d7ce5bf83e4b90988c8dea26fb648915b6e2c`.
- Distribution DLL SHA256: `9a6b841e1ea3b0ba1189edaf7ca0b7ba0231982e88fb1e43699d5928f16a408d`.
- `.ymme` SHA256: `2525195356ff979e9264901331e34a010d542aaf2fc16b9c6cfd0f57b44c190e`.
- Source ZIP SHA256: `985f58785955370667d502653a190f2bd3dbeee0e7d537818c8fcb7b4fe2d201`.

The source ZIP is the verified checkout, not an untracked working directory. A later documentation-only checkpoint or PR verification may have a different source HEAD/archive/hash; its provenance identifies that exact run. Do not mix artifacts across runs merely because their version labels match.

## W12 changes and regression proof

Quick Drop numeric saves now normalize equivalent text such as `0100` to `100` and clear the dirty indicator. Selection Preset copies receive distinct names. Library and Template choosers distinguish short name, Character and source; search/empty states give explicit recovery guidance. Long status messages are bounded and scrollable. Selection Preview/Place controls remain outside the scrolling editor, and expression actions use the footer to preserve list space.

Image review found that repeated unchanged settings reloads could leave real WPF Preset selectors blank. Refreshes now retain equal item collections and return the matching selector instance. `NativePresetSelectorProof.cs` verifies visible saved names/IDs through unrelated edits, copy, refresh, actual two-way ComboBox changes and deletion. The underlying placement semantics were not weakened.

The proof capture originally included the host's centering offset for a narrow control. The capture now samples the actual visual-origin rectangle without rearranging the host. Normal and narrow screenshots were inspected after correction; their content is real hosted WPF, not mockups.

## UI review and practical limits

Reviewed four normal-width tab captures, four 360×320 captures, and empty Library/palette/selection states. Normal-width controls, saved Preset names, source labels, status and placement actions are readable. The narrow captures retain navigation and actions, and scrollable editors preserve access to their remaining controls.

**360×320 is a constrained navigation layout, not a recommended editing size.** At this height the expression table or palette can have very little visible row area. Increase the Tool height, preferably to roughly 480 pixels or more, for practical editing. A wide expression table uses horizontal scrolling. The review does not claim every small size, DPI or theme is fully optimized.

## Distribution gate

Assembly/package version is 0.4.0 / 0.4.0.0. `PackageVerified.ps1` requires completed native stages, all 18 manifest IDs/results, clean build logs and the exact distribution DLL's native load marker. It then verifies the eight allowed root payload files and checks that the DLL inside `.ymme` matches the native-smoked DLL.

Payload: Plugin DLL/deps.json, Open XML DLL and Framework DLL, README, third-party notices, provenance and acceptance manifest. YMM4 binaries, test fixture code and third-party character assets are not distributed in this payload. Source ZIP contains the implementation, tests and documentation.

## Scope and remaining user validation

No mandatory v0.4 feature/acceptance remains at this checkpoint. User-asset visual checks and preferred editing layouts remain practical validation outside the synthetic fixture claim. Native WPF command invocation is not a physical mouse/installer test. The `.ymme` structure and DLL identity are verified; arbitrary PSD assets, future YMM4 releases and every DPI are not.

Association/Resync is the explicit Character Expression path. Quick Drop and selection Profiles remain independent, unassociated Items. No continuous sync, fuzzy recovery, historical Preset snapshots, automatic delete/rebuild or copy/paste ID repair was added.

All changes used small auditable file commits. No safety write refusal occurred in this continuation. Main remains unmerged. Earlier W checkpoints and their evidence are preserved in W7-W11 checkpoint documents.
