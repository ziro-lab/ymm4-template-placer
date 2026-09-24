# Behavior Preview Workplan

Status: **COMPLETE — P0/P1 RELEASE GREEN / P2 OWNER HANDS-ON ACCEPTED**

This workplan records current implementation state. The DESIGN and ACCEPTANCE v2 documents define the required UX, not a claim that the owner has accepted it.

## P0 — common meaning projection — COMPLETE

`PlacementBehaviorDescription` and the existing Summary are retained. Incomplete Draft values are not silently normalized. `BEHAVIOR_PREVIEW_P0=PASS` remains required.

Historical evidence: Checkpoint #712, #720 and Release #724.

## P1a / P1b — v2 placement-result diagram — IMPLEMENTED / NATIVE GREEN

The v1 label cards have been replaced with Timeline-like blocks:

- shared horizontal coordinates preserve start/end coincidence, equal lengths and alignment;
- target-relative Up/Down changes actual block row order;
- neighbor-start and neighbor-end align to different visible boundaries;
- anchor and placement alignment remain separate settings;
- previous-neighbor examples are not blindly mirrored into a false valid interval;
- target span, fixed/source-owned duration, pair boundary and selection range are supported;
- selected-tile duration precedence and edge deltas update without building/saving the Draft;
- incomplete active numeric settings show a notice, not made-up geometry;
- Generic full Preview is removed;
- Absolute Layer labels separate the reference band from an actual relative layer position.

Production Preview has no Timeline/source/occupancy dependency and performs no mutation. The renderer maps illustrative coordinates to pixels only. Template duration and reference spans are examples, not live project measurements. Collision results, source availability and real-context applicability remain the responsibility of the existing placement path.

### Validation evidence

Initial v2 source `3d087ec00a69104ecc62311e127477377cc71a33` passed Checkpoint #728 (`36004405144`): **1,237 Native assertions PASS / 0 FAIL**.

Release #730 established the first full v2 package baseline. Subsequent owner-driven compactness and clarity corrections were completed on exact tested source `6ed136abcb9b6a3346e32d1b0661c5aed98d69b3`.

Final Release #747 is GREEN:

- **1,254 Native assertions PASS / 0 FAIL**;
- `BEHAVIOR_PREVIEW_P0=PASS`, `BEHAVIOR_PREVIEW_V2=PASS`, current `BEHAVIOR_PREVIEW_P1=PASS`;
- PairBoundary separator is conveyed by the guide line without duplicate separator text;
- Settings owns one shared 8 DIP right gutter while retaining the wider left outer-scroll escape;
- live Anchor / Alignment / Direction ComboBox changes retain the Preview crash guard;
- rendering remains zero-write;
- exact distribution-DLL smoke and verified `.ymme` / source / provenance packaging are GREEN.

Artifact: `10817701121`, `native-yymm4-release`.

Artifact SHA256: `d6406350f3c66eda36b181c131421ec5cb3d08145a38bf3a6cdbeed21bea3d66`.

`.ymme` SHA256: `8b3429f7d9059ba9e9322a26c9e7c93eeaf365df770b1430b6e5e9cb534ad4c1`.

Distribution DLL SHA256: `1888e12534f319bbd30387170e47f653a0280693790f865c21772fe70a455b55`.

Docs-only acceptance/roadmap updates after `6ed136ab...` do not constitute a newer tested product build.

## P2 — owner hands-on — ACCEPTED

Owner hands-on accepted the final v2 Preview after the #747 corrections. The Preview phase may be promoted; automated PASS is no longer standing in for owner acceptance.

Acceptance is specifically for the current owner workflow and v2 visual vocabulary. It is not a universal first-time-user comprehension claim.

## P3 — product handoff — COMPLETE

Behavior Preview closes here as a read-only Settings aid.

The next product work is intentionally reprioritized:

1. prove and expose compact **YMM4-native Undo / Redo** in the existing top context strip;
2. investigate **placement quick settings** that edit the same current Settings Draft while the accepted Preview provides immediate feedback;
3. re-evaluate the “what I want” checklist only if Preview + direct Settings + quick settings still leave a concrete comprehension/input problem;
4. keep Full Settings Workspace deferred until real Set/tile scale creates a navigation/management problem.

No custom Undo stack, alternate Settings schema, second placement engine or Preview direct-manipulation path is introduced by this handoff.

## Historical v1

#720/#724 remain evidence for the shared projection and earlier read-only integration, not acceptance of the rejected card layout. The v2 design and the owner's hands-on feedback supersede the v1 UX.
