# Placement Rule Completion Acceptance

Status: **FROZEN ACCEPTANCE CANDIDATE**

This acceptance applies to `PLACEMENT_RULE_COMPLETION_DESIGN.md`.

## A1 — Existing settings compatibility

Existing accepted settings without the new enum/value/fields load with unchanged placement behavior. Loading alone does not rewrite bytes.

## A2 — Existing relative layer regression

`RelativeToTarget` remains byte/semantic compatible with the current up/down offset and bounded one-direction collision search.

## A3 — Center alignment math

For a resolved span length `L`, `CenterAtAnchor` uses `start = anchor - floor(L/2)` before existing offsets.

Prove even and odd lengths.

## A4 — Center alignment source neutrality

Center alignment behaves through the same `IntentRelationResolver` for:

- Template source;
- registered Tachie Preset source;
- normalized multi-item Template source when source duration is preserved.

No source-kind timing fork.

## A5 — Center alignment restrictions

`UntilRelated + CenterAtAnchor` is rejected by validation. Existing `UntilRelated + StartAtAnchor` behavior remains unchanged.

## A6 — Center settings / summary UX

Settings exposes one clear center option and `このセットの動き` describes center alignment truthfully. No second Source Pivot editor is introduced.

## A7 — Absolute base layer

In Absolute mode, normalized source layer 0 begins at `AbsoluteLayer`; internal layer offsets are preserved.

## A8 — Absolute collision search

Occupied absolute baseline searches only in the configured direction within saved bounds. No opposite-direction wrap.

## A9 — Absolute failure atomicity

Out-of-range baseline, insufficient multi-item width or no free destination fails before Timeline mutation.

## A10 — Absolute source neutrality

Template and registered Tachie Preset sources use the same layer planner.

## A11 — Absolute settings / summary UX

Settings distinguishes target-relative from absolute layer placement, shows only relevant fields, and the human-readable summary matches the saved rule.

## A12 — Association / Resync compatibility

Existing managed expression replacement and Resync continue to derive geometry only from the owning Set relation. A relation change to center/absolute geometry is handled through the existing exact association safety checks.

## A13 — Performance boundary

No new Timeline scan, preset discovery, polling or per-Voice work is introduced merely by adding the two finite placement values.

## A14 — Native regression

Focused development proof must include the new cases. Final Release retains the complete historical semantic/evidence ladder, exact distribution DLL smoke and verified packaging.

## Hands-on

Before merge verify at least:

1. center one short singleton on a Voice center;
2. center a multi-item Template while preserving its internal timing;
3. place one tile at an exact absolute layer;
4. occupy that layer and confirm search proceeds only in the chosen direction;
5. switch back to target-relative mode and confirm the previous behavior is unchanged;
6. confirm the summary sentence changes with the active alignment/layer mode;
7. Undo/Redo remains one native operation.