# Placement Rule Completion Workplan

Status: **P0 INVENTORY FROZEN — P1/P2 IMPLEMENTATION NEXT**

Authorities:

- `PLACEMENT_RULE_COMPLETION_DESIGN.md`
- `PLACEMENT_RULE_COMPLETION_ACCEPTANCE.md`
- `CURRENT_ARCHITECTURE.md`
- `VALIDATION_STRATEGY.md`

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