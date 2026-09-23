# Placement Rule Completion Design

Status: **ACCEPTED — RELEASE #629 GREEN**

Accepted implementation: PR #30. Exact product/test/package source `5bfbccb5dd81071662ba756ec27591519ff98e19` passed Release #629 (`35857198385`). Any later closeout commit is documentation-only and does not supersede that exact Release source.

Authority:

- `CURRENT_ARCHITECTURE.md`
- `PRODUCT_ROADMAP.md`
- `PLACEMENT_SOURCE_UNIFICATION_DESIGN.md`
- `PLACEMENT_RULE_COMPLETION_ACCEPTANCE.md`
- `PLACEMENT_RULE_COMPLETION_WORKPLAN.md`

## Goal

Freeze the near-term placement vocabulary before Behavior Preview / Checklist is implemented.

The current `IntentRelation` model already covers most normal editing needs. This phase is intentionally **not** a general Recipe expansion.

## Current implemented vocabulary

Already accepted on main:

- target runtime Item type(s), uniform or explicit mixed-type contracts;
- minimum / maximum selection count;
- optional exact logical `CharacterName` restriction;
- anchors: selected start / end / center, selection-range start / end, explicit two-item boundary, related-item start / end;
- duration: source/template span, selected-target span, fixed span, or until a related item;
- start-at-anchor / end-at-anchor alignment;
- Set-level start/end offsets plus bounded per-tile offset/duration overrides;
- related-item selectors: previous/next same type, same Character, or both;
- maximum related-item gap;
- explicit missing-related fallback: current target end, target span, fixed duration, or do not place;
- relative layer placement above/below the selected target band;
- bounded one-direction collision search that never moves, shortens or deletes existing items;
- one source-specific materialization path followed by source-neutral Set-owned geometry.

## Inventory decision

Only two placement gaps are approved before Preview.

### 1. Center alignment at the chosen anchor

The existing `IntentAlignment` already represents the source/output side of timing alignment:

- `StartAtAnchor`;
- `EndAtAnchor`.

Do **not** introduce a second overlapping Source Pivot model in this phase.

Add exactly one missing common alignment:

- `CenterAtAnchor`.

Semantics for a resolved output span of length `L` and anchor `A`:

```text
StartAtAnchor  -> start = A
CenterAtAnchor -> start = A - floor(L / 2)
EndAtAnchor    -> start = A - L
```

Then apply the existing Set/tile start/end offsets exactly as today.

This works for both source kinds because timing is resolved before layer planning and after source materialization. Multi-item sources keep their internal Frame relationships and may center only when their existing duration rules allow the source span unchanged.

`UntilRelated` continues to require `StartAtAnchor`; center/end alignment with that duration is invalid.

Why this is enough now:

- start and end pivot behavior already exist;
- center is the obvious missing high-frequency case;
- rare arbitrary internal-pivot placement can already be expressed with bounded offsets;
- adding an explicit arbitrary pivot field now would duplicate existing axes without a proved normal-editing need.

### 2. Absolute base layer

The current serialized `RelativeLayerPolicy` remains for compatibility. Do not replace it with polymorphic JSON or rename existing serialized fields.

Add additive fields:

```text
LayerPlacementMode
- RelativeToTarget  (default / existing behavior)
- Absolute

RelativeLayerPolicy.Mode
RelativeLayerPolicy.AbsoluteLayer
```

Absolute semantics:

- the normalized source's minimum layer (`0`) is placed first at `AbsoluteLayer`;
- a multi-item source preserves all internal layer offsets from that baseline;
- if occupied, search only in the configured `Direction`;
- `Minimum` / `Maximum` remain the finite search bounds;
- `Offset` is used only for `RelativeToTarget` mode;
- never wrap to the opposite direction;
- never move/shorten/delete existing Timeline items.

Existing settings that do not contain the new fields must deserialize to `RelativeToTarget` with byte/semantic compatibility after load; opening settings alone must not rewrite them.

## Explicitly deferred

### Stronger Target conditions

No new target grammar before Preview.

The current model already has runtime type sets, uniform/exact-mixed matching, selection-count bounds and exact Character restriction. Same-Character-any-name predicates, per-type cardinalities and before/after role grammars remain deferred until a concrete editing case proves they reduce work.

### Stronger Neighbor selectors

No new neighbor grammar before Preview.

The current six previous/next same-type / same-Character / combined selectors already cover the common Voice-next/previous workflows. `next/previous any Item` and arbitrary specified-type neighbors remain deferred.

### Composite Placement Steps

Deferred. One tile continues to materialize one Placement Source and one Set-owned relation. Composite steps change output multiplicity, error reporting, Preview structure and per-step editing semantics.

### Fan-out

Deferred. Per-selected-item or per-boundary fan-out changes one action into N placements and needs separate deduplication/atomicity design.

## Preview boundary after this phase

Once Center alignment and Absolute Layer are native-green, Preview / Checklist may assume the near-term normal placement model is:

```text
Target contract
-> Anchor
-> Alignment (start / center / end)
-> Duration
-> Offsets / related-item fallback
-> Layer reference (target-relative / absolute)
-> bounded one-direction collision search
-> PlacementPlan / native Undo
```

Preview must remain a projection of this model, not a second geometry engine.

## Pause points

Return to design review instead of broadening this phase if implementation would require:

- a second source-specific geometry path;
- changing old persisted field meanings;
- polymorphic settings migration;
- multi-step/fan-out execution;
- fuzzy target/neighbor selection;
- existing-item transformation;
- a second Undo model.