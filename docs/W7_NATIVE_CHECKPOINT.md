# W7 native checkpoint — Character Expression presets

W7 is implemented and native-verified. W8–W12 are not complete. Do not merge PR #6 or label this checkpoint as the final v0.4 distribution.

- Verified source: `c44db20ec53782cd2658a9aef082c14ef7f942fe`
- Native run: https://github.com/ziro-lab/ymm4-template-placer/actions/runs/34852878500
- Artifact ID: `10350194983`
- Artifact SHA256: `ceed1aea7ffd04fb215aa1b90c9cefae9c581b8e8f378653615ea2e5b752b992`
- Distribution DLL SHA256: `fa7a929cb64f39395b6178304dce3b28c4ac456432a9ccdae3b1b0273b05cee1`
- Actual pinned YMM4 4.55.1.1 Lite, 158 native assertions PASS (previous 119 preserved).
- Release/proof builds: 0 compiler warnings, 0 errors, warnings as errors.
- P1–P9, Open XML regression, native release DLL identity smoke and packaging PASS.

## Behavior added

A bounded, persisted Character Expression Preset holds Voice span / Next Same Character, MaxGap, Start/End offsets and the existing LayerPolicy. Multiple presets use the same thin profile over PlacementPlan and LayerPlanner. Default parameters preserve the prior expression placement geometry.

The current saved preset is selected in the actual expression-list UI. The editor is a separate draft: invalid or unsaved parameters do not drive placement. Save, duplicate, delete and revert are explicit. Last-preset deletion is rejected. Semantically identical numeric saves normalize the editor.

Character Palette references prioritize compatible assignment choices and expose the short Library name; the existing compatible choices and workbook assignment identity remain available. Excel schema and import validation remain unchanged. Import changes assignments only. The subsequent placement reads the current preset, including changes made after export.

## Native tests added

`tests/NativeExpressionPresetProof.cs` verifies the real WPF preset surface and Save/Place controls, independent settings reload, next-Character resolution past another Character, overlap preservation, inclusive MaxGap, large-gap/last-Voice fallback, both offsets, negative/overflow rejection, complete preflight without mutation, existing and already-planned occupancy, later-plan layer exhaustion without partial placement, native one-step Undo/Redo, valid Open XML export and current-preset use after Excel import.

Fixtures are synthetic and restored after proof. Product code does not delete generated or unrelated Items. Visual PSD-asset fidelity and the remaining v0.4 profiles are not claimed by this checkpoint.

## Execution continuity

W7 was written in individual file commits on `work/v0.4-native-validation` so the PR's previously verified HEAD remained intact during implementation. The previously refused `PresetDraft.cs` and `PresetViewModel.cs` writes both succeeded as individual file operations. No refused operation was retried through an alternative route in this session. The validation workflow's push branch allowlist includes that isolated work branch; the docs-only scope guard remains intact.

Package/assembly version deliberately remains 0.3.1 until W12, so checkpoint packages are not final v0.4 Candidates.
