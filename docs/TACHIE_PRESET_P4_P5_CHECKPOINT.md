# Tachie Preset P4/P5 checkpoint

Status: **P4 + P5 NATIVE GREEN / P6 READY**

This checkpoint records the existing-row integration and source-aware choice model on exact YMM4 4.55.1.1 Lite. It does not authorize Tachie-Preset Timeline mutation yet; P6-P8 remain the mutation boundary.

## Exact evidence

- Product source: `7cdc3355858851635a8dca85ba349326c3237427`
- Checkout merge commit: `793bf2d4effa8cff843469038f464582e240668e`
- Checkpoint run #423: `35661119036`
- Native job: `106536681752`
- Artifact: `10667463789` (`native-yymm4-checkpoint`)
- Artifact ZIP SHA256: `c3c04b7768ffa5564fda2fccc2664b15e7e44cab08a39926f2f9b71d6780e311`
- Host: YMM4 4.55.1.1 Lite / Windows / .NET 10
- Markers:
  - `TACHIE_PRESET_ROWS_P4=PASS`
  - `TACHIE_PRESET_CHOICE_MODEL_P5=PASS`
  - retained `TACHIE_PRESET_SOURCE_MODE_P1=PASS`
  - retained `TACHIE_PRESET_CAPABILITY_P3=PASS`
  - retained `TACHIE_PRESET_GUARDS_P3=PASS`
  - retained `HANDS_ON_ROUND2_E=PASS`
  - retained `EXPRESSION_PERFORMANCE=PASS`
  - `PASS P1 P2 P3 P4 P5 P6 P7 P8 P9`
  - full Checkpoint semantic regression/evidence guards: PASS

Distribution and proof builds completed with zero errors. This is not Release or owner Hands-on acceptance.

## P4 implemented and proved

P4 connects immutable Tachie-Preset capability results to the existing expression capture/preparation/publication pipeline.

The proof establishes:

- one existing `Rows` collection and one DataGrid serve Template and TachiePreset source modes;
- 1,000 Voices sharing one Character require one Character discovery and no Template catalog scan in TachiePreset mode;
- immutable preset descriptors use the existing off-thread row preparation and UI-thread publication boundary;
- Tachie-Preset candidates are never represented by fake `FaceTemplate` or Intent entries;
- a valid current Template association is shown as current-other-source rather than corrupt/unselected;
- candidate inspection, source switching and Template return are Timeline/settings zero-write;
- Excel remains Template-only, including the direct Workbook API;
- Serif/Frame incremental edits preserve row/candidate identity without collection Reset or preset re-scan;
- unchanged re-entry reuses the session cache;
- Character/config changes invalidate capability identity and never silently heal an old candidate descriptor;
- one broken Character remains a local unavailable state;
- leaving the Tool root while an editor is bound cancels and clears it;
- obsolete source preparation cannot publish after a newer source wins.

## P5 implemented and proved

The historical row-choice model is minimally source-aware. There is still one row model.

The native proof explicitly covers all required states:

1. none / unselected;
2. Template candidate;
3. TachiePreset candidate;
4. valid current other-source association;
5. unavailable same-source choice;
6. invalid managed association.

Workbook remains Template-only.

## Regression fixed while closing P4/P5

The first P4 Checkpoint exposed one retained Round2-E regression: choosing `配置しない` during a Template candidate refresh was blocked before the immediate Template mutation path ran.

The final fix keeps Template immediate choices active while the existing row projection refresh is in flight, while TachiePreset inspection remains non-mutating and load-gated. A permanent Round2-E assertion now requires removal to start from authoritative Template rows even while candidate refresh is active.

Final Checkpoint #423 proves `HANDS_ON_ROUND2_E=PASS` together with P4/P5 and the full semantic regression suite.

## Next boundary

P6 may now add the common managed-expression seam only:

- keep existing `IntentAssociationTag` bytes unchanged;
- add a separate versioned `TachiePresetAssociationTag`;
- add a discriminated managed source descriptor;
- make the live reader and background association index recognize exactly one source kind;
- copied/missing/duplicate/mixed source tags must fail closed;
- preserve current Template behavior before any Tachie-Preset mutation is enabled.

P6 must not place, replace or remove Tachie-Preset Timeline items. Those mutations begin in P7/P8 after the source-union seam is proved.
