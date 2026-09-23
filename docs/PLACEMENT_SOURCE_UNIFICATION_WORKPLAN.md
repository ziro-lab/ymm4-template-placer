# Placement Source Unification Workplan

Status: **IMPLEMENTATION PREP CANDIDATE**

Authority:

- `PLACEMENT_SOURCE_UNIFICATION_DESIGN.md`
- `PLACEMENT_SOURCE_UNIFICATION_ACCEPTANCE.md`
- `CURRENT_ARCHITECTURE.md`
- `VALIDATION_STRATEGY.md`

Do not redesign the feature while implementing these slices. If a required host behavior contradicts the frozen design, stop that slice and return the evidence to design review.

## P0 — persisted source model / compatibility seam

Implement:

- `TachiePresetSourceEntry`;
- additive `PlacerSettings.TachiePresetSources`;
- validation bounds;
- SourceId alias for existing `IntentEntry.LibraryEntryId`;
- source registry that resolves exactly one Template Library entry or preset source;
- cross-registry GUID uniqueness.

Proof:

- v0.5 settings roundtrip unchanged;
- existing Template SourceIds resolve;
- preset SourceIds resolve;
- duplicates/ambiguity reject;
- no FaceParameter/item body appears in serialized settings.

Exit: A1-A3, A21 relevant settings guarantees.

## P1 — source-neutral materialization/geometry seam

Implement a runtime materialized-source contract with:

- fresh normalized Items;
- Span;
- logical Character;
- SourceId/kind;
- current-source guard;
- source semantic hash where required.

Refactor layer planning so geometry can operate on normalized fresh items without requiring a TemplateBundle concrete type.

Keep the existing Template wrapper.

Proof:

- all current Template geometry tests remain green;
- singleton materialized test uses identical timing/layer math;
- multi-item Template behavior unchanged.

Exit: A8-A9.

## P2 — registered Tachie Preset source materialization

Implement:

- explicit source creation from one discovered preset candidate;
- persisted thin locator;
- exact current Character/plugin surface re-resolution;
- direct-route and PropertyEditor-route application to fresh item;
- no full rediscovery requirement where saved route/candidate is enough.

Initial ordinary placement scope:

- targeted Character-bound Sets only;
- no Generic/time-only preset source placement.

Proof:

- direct source;
- editor source;
- unavailable candidate;
- wrong Character;
- plugin/type/MVID mismatch;
- cancellation/current-config change;
- fresh object/state verification.

Exit: A4-A7.

## P3 — normal Set/tile integration

Extend Set source editing/tiles so one Set may reference either source kind.

Do not auto-persist discovered candidates.

Add an explicit "add/register preset source" operation.

Normal tile execution:

```text
context
-> resolve SourceId
-> materialize source
-> common IntentRelation geometry
-> PlacementPlan
-> native Undo
```

Proof:

- ordinary preset tile placement outside expression list;
- mixed Template/preset Set;
- same relation changes both behaviors;
- settings rollback/source removal has no Timeline mutation.

Exit: A10, A19-A22 relevant parts.

## P4 — expression catalog unification

Make `ExpressionCandidates` Sets enumerate both registered source kinds.

Current source mode remains a filter/visibility decision initially.

New registered preset choices use Set-owned `IntentRelation`.

Do not remove legacy v0.5 preset choices/geometry yet.

Proof:

- mixed Set candidate order is deterministic;
- filter changes candidate visibility only;
- no Timeline writes on filter switch;
- Template expression path regression green;
- new preset choice follows Set relation.

Exit: A11-A12.

## P5 — association v2 / cross-source mutation

Extend Tachie Preset association with a backward-compatible v2 for Set-owned sources.

Persist in tag:

- group;
- Palette ID;
- SourceId;
- source semantic hash;
- state hash.

Keep v1 reader.

Update ManagedExpressionReader/Mutation so exact managed replacement supports both new source kinds.

Proof:

- v1 Template tags unchanged;
- v1 preset tags still readable;
- v2 preset exact identity/state;
- all four cross-source replacement directions;
- copied/duplicate/mixed/malformed tags reject;
- manual preset state edit rejects replacement.

Exit: A13-A18 except Resync.

## P6 — preset source Resync

Add v2 preset geometry Resync through the owning Set relation.

Do not reapply content.

Proof:

- Set relation change -> Frame/Length/Layer update;
- source semantic change -> fail closed;
- face state change -> fail closed;
- missing Set/source -> local skip/failure;
- native Undo;
- unrelated items untouched.

Exit: A17 plus remaining safety cases.

## P7 — Settings/UX closeout

Expose registered preset sources without eager scanning.

Hands-on decide:

- exact add action location;
- source label/detail presentation;
- relink/remove language;
- whether current source filter stays as-is;
- whether standalone ExpressionPreset controls become compatibility-only.

Do not add Full Settings Workspace or Preview in this feature.

Exit: A19-A21 plus Hands-on items.

## P8 — Release closeout

Run:

- Focused during slices;
- Checkpoint after P3 and P6;
- Release at final exact source.

Require A1-A24 evidence.

Do not merge solely on synthetic/unit tests.

## Explicit pause points

Pause and return to design review if any of these become necessary:

- serializing generated FaceParameter/item bodies;
- replacing existing Template Library with polymorphic JSON;
- changing old IntentAssociationTag wire format;
- fuzzy preset identity recovery;
- cross-Character preset sharing;
- Generic preset placement requiring global Character guessing;
- a second placement/Undo engine;
- eager source discovery on Settings/plugin open.
