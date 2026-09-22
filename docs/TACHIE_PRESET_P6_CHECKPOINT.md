# Tachie Preset P6 checkpoint

Status: **P6 NATIVE GREEN / P7 READY**

P6 adds only the common managed-expression seam. It does not enable Tachie-Preset Timeline placement/replacement/removal.

## Exact evidence

- Product source: `0a2710457ff635038f7a6cd83cf36418bd649b35`
- Checkpoint run #429: `35662905394`
- Native job: `106542053072`
- Artifact: `10667807294` (`native-yymm4-checkpoint`)
- Artifact ZIP SHA256: `04cff59ccb2b02dd08a1562877f5622825fdeba0589c4385866a8fa7ed7a7002`
- Host: YMM4 4.55.1.1 Lite / Windows / .NET 10
- Marker: `TACHIE_PRESET_ASSOCIATION_P6=PASS`
- Retained markers: P1/P3/P4/P5, `HANDS_ON_ROUND2_E=PASS`, `EXPRESSION_PERFORMANCE=PASS`
- Full Checkpoint semantic regression/evidence guards: PASS

## Implemented seam

- existing Template `IntentAssociationTag` bytes are unchanged;
- new versioned `TachiePresetAssociationTag` uses reserved `CWT_TPL:T=` lines;
- long immutable capability/candidate identity is represented by deterministic length-framed SHA-256 digests;
- common live reader returns an exact source-kind-discriminated descriptor plus exact members;
- background `ExpressionAssociationIndex` uses the same source union;
- Template mode reports valid TachiePreset content as current-other-source;
- TachiePreset mode re-resolves current managed preset identity by exact capability/candidate hashes;
- missing same-source candidate is explicit unavailable, never same-label healing;
- mixed, duplicate, missing, copied and cross-kind managed groups fail closed;
- manual/unassociated expression content remains outside managed ownership;
- association cleanup strips only complete reserved preset-tag lines;
- the historical Template mutation reader remains a compatibility wrapper and refuses TachiePreset bundles before P7/P8.

## Next boundary

P7 may implement planning only:

1. re-resolve current Character/plugin/config from the immutable candidate;
2. create a fresh FaceParameter;
3. re-apply the exact candidate and verify state;
4. create a fresh TachieFaceItem and attach the applied FaceParameter;
5. apply current `ExpressionPreset` span and `LayerPlanner`;
6. resolve exact current managed association through the P6 union;
7. fully preflight additions/removals/Voice-tag update through `PlacementPlan`.

P7 must remain zero-write until an explicit commit call and must not wire row selection to immediate mutation. P8 owns trial/UI cross-source replacement.
