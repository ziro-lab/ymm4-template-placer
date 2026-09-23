# Placement Source Unification Acceptance

Status: **ACCEPTED — RELEASE #615 GREEN**

This acceptance applies to the design in `PLACEMENT_SOURCE_UNIFICATION_DESIGN.md`.

Release #615 (`35834323751`) at exact product/test/package source `7ffe3b2c690a57850c77821f7b4c3eeb6517b3dc` is the accepted closeout gate for this phase. It passed **1,763 Native assertions**, the retained Hands-on/UI evidence chain, exact distribution-DLL smoke and verified `.ymme` / source / provenance packaging.

The requirements below remain the compatibility/acceptance contract for future changes.

## A1 — Existing settings compatibility

Given an accepted v0.5.0 settings file with only Template Library entries:

- load succeeds without rewriting Template Library identities;
- existing `IntentEntry.LibraryEntryId` values are preserved;
- existing Palette/Set behavior is unchanged;
- saving/reloading is lossless apart from explicitly documented additive defaults.

## A2 — Shared SourceId namespace

Template Library IDs and Tachie Preset Source IDs share one bounded namespace.

Reject:

- duplicate IDs inside Tachie Preset Sources;
- one ID present in both Template Library and Tachie Preset Sources;
- ambiguous source resolution.

Existing Template IDs remain valid SourceIds.

## A3 — Thin preset persistence

Registering one Tachie Preset persists only bounded locator metadata.

The persisted source must not contain:

- TachieFaceItem serialization;
- FaceParameter bodies;
- preset/PSD content snapshots;
- Timeline references;
- current Character configuration hash.

## A4 — Character-bound exact resolution

A registered preset source resolves only for its exact logical Character and compatible plugin surface.

A same-named preset on a different Character is not substituted automatically.

## A5 — Current plugin-surface guard

Before materialization, require the registered source's:

- plugin runtime type;
- plugin Module MVID;
- CharacterParameter type;
- FaceParameter type;
- route identity;
- candidate identity

to resolve exactly against the current Character.

Missing/ambiguous/incompatible sources fail with zero Timeline writes.

## A6 — Fresh preset materialization

Each placement creates independent fresh content.

Prove:

- fresh TachieFaceItem/FaceParameter identity;
- selected preset is applied to the fresh object;
- retained public state is verifiable;
- no source object/body is reused as the placed item.

## A7 — No eager discovery regression

Opening the plugin, ordinary placement UI, or Settings must not scan all Tachie plugins/Characters.

A registered preset source may resolve only when its placement/explicit source operation requires it.

Existing expression-performance gates remain green.

## A8 — Source-neutral geometry

Template and registered Tachie Preset sources both feed the same authoritative placement relation path after materialization.

For equivalent singleton source span/context/relation:

- Frame calculation is common;
- Length calculation is common;
- Layer/collision-search behavior is common.

No duplicate preset-only geometry implementation is accepted for the new registered-source path.

## A9 — Template regression

Existing Template placement remains byte/semantic compatible where the feature does not intentionally add source-kind handling.

Retain:

- TemplateBundle strict resolution;
- normalized bundle behavior;
- multi-item atomic planning;
- current character guards;
- PlacementPlan/native Undo.

## A10 — Normal Set preset tile

A Character-bound targeted Set can contain a registered Tachie Preset source tile.

Using the tile outside `表情をまとめて`:

- materializes the preset;
- uses that Set's `IntentRelation`;
- respects bounded collision search;
- commits through one PlacementPlan/native Undo unit.

## A11 — Mixed expression Set

One `ExpressionCandidates` Set may contain both:

- Template source entries;
- Tachie Preset source entries.

Both are candidate content under the same Set-owned placement relation.

## A12 — Expression source mode is not an engine switch

The current Template/TachiePreset source selection may remain as a visibility/filter control.

Changing it alone must not:

- alter Set placement semantics;
- mutate Timeline content;
- select a separate geometry engine.

## A13 — New preset expression association

A newly placed Set-owned Tachie Preset expression has an exact association that identifies:

- generated group;
- Palette/Set;
- SourceId;
- persisted-source semantic hash;
- retained preset state hash.

The format remains bounded for Remark storage.

## A14 — Existing association compatibility

Existing accepted tags remain readable:

- IntentAssociationTag v1 Template bundles;
- TachiePresetAssociationTag v1 legacy v0.5 placements.

Do not reinterpret an old tag as a new source kind.

## A15 — Manual preset-state edit protection

If a managed Tachie Preset expression's FaceParameter state changes after placement:

- automatic replacement/resync stops;
- no source is guessed;
- no Timeline item is deleted/replaced.

## A16 — Set/source edit protection

If the Set relation or registered preset source semantics change between plan and commit:

- commit fails;
- Timeline remains unchanged.

## A17 — New preset geometry Resync

For a valid new Set-owned preset association:

- resolve the exact Voice;
- resolve the exact Set and SourceId;
- validate source hash and current face state;
- recompute geometry from the Set's current `IntentRelation`;
- update only Frame/Length/Layer;
- use native Undo;
- do not silently reapply preset content.

## A18 — Cross-source replacement

For one managed Voice, replacement among:

- Template -> Template;
- Template -> registered Tachie Preset;
- registered Tachie Preset -> Template;
- registered Tachie Preset -> registered Tachie Preset

must:

- preflight completely;
- remove only the exact plugin-managed prior bundle;
- add only the selected source;
- preserve unrelated/manual expressions;
- be one owned Undo operation.

Legacy v0.5 TachiePreset v1 replacement safety must remain supported during migration.

## A19 — Explicit registration only

Discovery alone does not persist preset sources.

A source is stored only after an explicit user source-registration/add action.

Cancelling that action performs zero settings writes.

## A20 — Source removal safety

Removing/unregistering a preset source:

- does not delete Timeline items automatically;
- leaves existing association data intact;
- causes future source resolution/resync to fail locally and explicitly rather than guessing another source.

## A21 — Portable settings integration

Preset source registration persists through the current authoritative portable settings store.

Retain:

- full settings validation;
- atomic replacement;
- digest/external-change protection;
- cross-instance lock;
- session rollback semantics.

## A22 — Failure atomicity

For every required source/geometry failure:

- zero partial Timeline mutation;
- zero unrelated settings mutation;
- no fallback to another source by label/name similarity.

## A23 — Performance boundary

Native proof retains the current 100/500/1000 Voice expression-performance acceptance.

Registered preset source use must not introduce an eager all-source scan.

## A24 — Distribution proof

Final Release must prove:

- warning/error-clean release/proof builds;
- full semantic regression/evidence guards;
- exact distribution DLL native smoke;
- verified stable-root `.ymme` packaging;
- source/provenance identity;
- no user Data/settings payload in `.ymme`.

## Hands-on acceptance

Before final merge of the implementation feature, verify at least:

1. add one detected Tachie Preset to an existing Character Set;
2. place it from the ordinary tile surface;
3. mix Template + Tachie Preset tiles in the same Set;
4. change the Set's placement rule and confirm both source kinds follow it;
5. use the same Set through `表情をまとめて`;
6. switch Template/TachiePreset visibility filter without Timeline writes;
7. restart YMM4 and confirm registered preset sources survive portable settings;
8. make one source unavailable and confirm clear local failure;
9. Undo/Redo one preset tile placement and one expression replacement;
10. confirm manual/unmanaged expression items remain untouched.
