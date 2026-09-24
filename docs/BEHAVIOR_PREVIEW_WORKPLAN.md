# Behavior Preview Workplan

Status: **P0 COMPLETE — P1 v2 RELEASE GREEN — P2 OWNER HANDS-ON PENDING**

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

Final code/package source `878ab0e3decf0f5ab9cecdb8467f9f2e4e71cd02` passed Release #730 (`36006445462`, attempt 1):

- **1,239 Native assertions PASS / 0 FAIL**;
- `BEHAVIOR_PREVIEW_P0=PASS`, `BEHAVIOR_PREVIEW_V2=PASS`, current `BEHAVIOR_PREVIEW_P1=PASS`;
- 32 differential comparisons with the existing pure resolver, using isolated synthetic host fixtures;
- 22 native WPF captures: 11 cases at 260/360 DIP, including a one-frame bar;
- actual rendered bar x/width/row geometry checked against the diagram model;
- current compact Settings integration and absence of Generic diagram;
- rendering zero-write checks;
- exact distribution-DLL identity smoke and verified `.ymme` / source / provenance packaging.

Artifact: `10810538400`, `native-yymm4-release`.

Artifact SHA256: `5bb92732e01cd1568f30c3b71d28c422008c8a87df2fcde2edfa414a2d828029`.

`.ymme` SHA256: `d667276734149682f290ae51cc368139898a3890d651febc9682a9c7d4c10eac`.

Distribution DLL SHA256: `06decfe40c4fb40d8f97ae6d5278a06516b06bf2b494124bcf5d566a06d43ab1`.

The final package was extracted unchanged from the Release artifact. Its embedded provenance/source identity and DLL digest were cross-checked. Native screenshot review covered alignment, neighbor edges, Up/Down and narrow rendering; final marker clearance, Absolute Layer caption and one-frame label were also inspected.

Docs-only evidence updates after that source do not constitute a newer tested product build.

## P2 — owner hands-on — PENDING

PR #36 remains open/Draft. Main is unchanged. Do not merge or claim first-time-user comprehension from automated PASS alone.

Hands-on questions:

- Are placement start, end and vertical relation recognizable without reading the Summary?
- Are “対象と同じ長さ”, “周辺の開始まで” and “周辺の終了まで” immediately distinguishable?
- Does compact Settings retain enough working space at the owner's normal Tool size?
- Are any remaining hints redundant?

The delivered `.ymme` is the Release #730 candidate, not the superseded card Preview.

## P3 — checklist handoff — AFTER PREVIEW ACCEPTANCE

Use the same Settings Draft and accepted visual vocabulary. A future temporary checklist Draft applies explicitly; Cancel is zero-change. Full Settings Workspace, Undo/Redo UI, composite placement and Item Actions are not part of this implementation.

## Historical v1

#720/#724 remain evidence for the shared projection and earlier read-only integration, not acceptance of the rejected card layout. The v2 design and the owner's hands-on feedback supersede the v1 UX.
