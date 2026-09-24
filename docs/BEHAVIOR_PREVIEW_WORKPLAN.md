# Behavior Preview Workplan

Status: **P0 COMPLETE — P1 v2 CHECKPOINT GREEN — RELEASE / USER HANDS-ON PENDING**

## P0 — common meaning projection — COMPLETE

`PlacementBehaviorDescription` and the existing Summary are retained. Incomplete Draft values are not silently normalized. `BEHAVIOR_PREVIEW_P0=PASS` remains required.

Historical evidence: Checkpoint #712, #720 and Release #724.

## P1a / P1b — v2 placement-result diagram — IMPLEMENTED

The v1 label cards have been replaced with Timeline-like blocks:

- common horizontal coordinates preserve start/end coincidence, equal lengths and alignment;
- target-relative Up/Down changes actual block row order;
- neighbor-start and neighbor-end align to different visible boundaries;
- anchor and placement alignment are interpreted as separate settings;
- previous-neighbor examples are not blindly mirrored into a false valid interval;
- target span, fixed/source-owned duration, pair boundary and selection range are supported;
- selected-tile duration precedence and edge deltas are reflected without building/saving the Draft;
- incomplete active numeric settings show a bounded notice rather than a made-up diagram;
- Generic full Preview is removed;
- Absolute Layer labels distinguish the reference band from a real relative layer position.

Production Preview has no Timeline/source/occupancy dependency and performs no mutation. The renderer maps illustrative coordinates to pixels only. Template duration and reference spans are examples, not live project measurements. Collision results, source availability and real-context applicability remain the responsibility of the existing placement path.

### Checkpoint evidence

Exact source `3d087ec00a69104ecc62311e127477377cc71a33` passed Checkpoint #728 (`36004405144`): **1,237 Native assertions PASS / 0 FAIL**.

Coverage includes 32 differential comparisons against the existing pure resolver using isolated synthetic host fixtures, plus actual WPF controls rendered at 260 and 360 DIP widths. `BEHAVIOR_PREVIEW_V2=PASS` and the current `BEHAVIOR_PREVIEW_P1=PASS` are emitted only after v2 checks pass. Old v1 card assertions are superseded, not counted as v2 evidence.

Screenshot review of that checkpoint covered Up/Down, neighbor head/end, center/end, previous neighbor, pair/range and Absolute Layer. The release candidate additionally moves the anchor marker clear of header text, removes a duplicate Absolute Layer note and checks one-frame bars and actual rendered pixel coordinates. These refinements require a fresh Release; #728 is not evidence for later bytes.

## P2 — final package / human hands-on — PENDING

- Run Release against the exact final source and verify the distribution DLL, `.ymme` and provenance.
- Inspect the final native-generated screenshots.
- Deliver the actual `.ymme` for the owner to try; keep PR #36 Draft afterward.
- Do not merge or claim first-time-user comprehension from automated PASS alone.

Hands-on questions:

- Are placement start, end and vertical relation recognizable without reading the Summary?
- Are “対象と同じ長さ”, “周辺の開始まで” and “周辺の終了まで” immediately distinguishable?
- Does the compact Settings surface retain enough working space at the owner's normal Tool size?
- Are any additional hints redundant?

## P3 — checklist handoff — AFTER PREVIEW ACCEPTANCE

Use the same Settings Draft and accepted visual vocabulary. A future temporary checklist Draft applies explicitly; Cancel is zero-change. Full Settings Workspace, Undo/Redo UI, composite placement and Item Actions are not part of this implementation.

## Historical v1

#720/#724 remain evidence for the shared projection and earlier read-only integration, not acceptance of the rejected card layout. The v2 design and the owner's hands-on feedback supersede the v1 UX.
