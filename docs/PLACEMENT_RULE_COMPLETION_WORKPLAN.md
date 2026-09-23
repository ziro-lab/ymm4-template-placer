# Placement Rule Completion Workplan

Status: **COMPLETE / RELEASE GREEN — PR #30 MERGE READY**

Authorities:

- `PLACEMENT_RULE_COMPLETION_DESIGN.md`
- `PLACEMENT_RULE_COMPLETION_ACCEPTANCE.md`
- `CURRENT_ARCHITECTURE.md`
- `VALIDATION_STRATEGY.md`

## Current implementation status

P0-P4 are complete on PR #30.

Release #629 (`35857198385`) at exact source `5bfbccb5dd81071662ba756ec27591519ff98e19` passed:

- **1,787 Native assertions**;
- `PLACEMENT_RULE_P1=PASS`, `PLACEMENT_RULE_P2=PASS`, `PLACEMENT_RULE_P3=PASS`;
- retained Placement Source, Hands-on, performance and full semantic/evidence regression gates;
- exact distribution DLL native smoke, SHA256 `02388fd77990650fa5df5af6405e4cfd64a49e00e5b9f665fee771ce0aad0a1c`;
- verified v0.5.0 `.ymme` stable install root / source / provenance packaging;
- Release artifact `10748078586` (`native-yymm4-release`), artifact ZIP SHA256 `e349bff0d59a10d147ef5592d3dae5f341c56285b8459f222705743f5d9fc61b`.

Accepted scope is intentionally narrow: `CenterAtAnchor` and Absolute Layer are complete; stronger Target/Neighbor grammar, Composite Steps, Fan-out and a separate arbitrary Pivot model remain deferred.

## P0 — inventory / scope freeze

Complete in this document set.

Decision:

- keep the existing Target and Neighbor vocabularies;
- add only `CenterAtAnchor` and Absolute Layer before Preview;
- defer arbitrary Source Pivot, stronger Target/Neighbor grammar, Composite Steps and Fan-out.

## P1 — CenterAtAnchor

Implement additively:

- `IntentAlignment.CenterAtAnchor`;
- pure resolver math;
- Settings option / summary wording;
- Native semantic and UI proof.

Do not introduce another persisted pivot object.

Checkpoint:

- old Start/End behavior unchanged;
- even/odd center math;
- Template / preset / multi-item source-neutral behavior;
- `UntilRelated` restriction retained;
- offsets remain post-alignment.

## P2 — Absolute Layer

Implement additively while retaining serialized `RelativeLayerPolicy`:

- `LayerPlacementMode.RelativeToTarget` default;
- `LayerPlacementMode.Absolute`;
- `AbsoluteLayer`;
- shared planner branch;
- Settings mode + relevant-field visibility;
- summary wording;
- Native geometry/UI proof.

Checkpoint:

- relative path exact regression;
- exact absolute baseline;
- multi-item internal layer preservation;
- one-direction collision search and bounds;
- zero-write failure;
- Template / preset common planner.

## P3 — integrated acceptance

Run Checkpoint with both additions enabled.

Confirm managed expression replacement and Resync continue to use the same Set relation and native Undo path.

Do not start Compact Settings polish or Preview in this PR.

## P4 — Release closeout

Run exact Release at the final product/test/package source.

Require:

- all A1-A14 acceptance evidence;
- retained historical regression/evidence guards;
- exact distribution DLL native smoke;
- verified `.ymme` / source / provenance packaging.

After GREEN, update `CURRENT_ARCHITECTURE.md` / `PRODUCT_ROADMAP.md`, mark this phase complete, merge, and only then enter Compact Settings friction/discoverability.