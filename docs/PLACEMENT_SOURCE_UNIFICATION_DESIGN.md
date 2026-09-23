# Placement Source Unification Design

Status: **FROZEN DESIGN CANDIDATE — implementation not started**

This document defines the post-v0.5 design for converging YMM4 Template sources and Tachie Preset sources behind one placement model without pretending that a Tachie Preset is an ItemTemplate.

## Decision

Proceed with convergence.

The product model becomes:

```text
Placement Source
├─ Template Source
│   └─ existing LibraryEntry / TemplateLocator
└─ Tachie Preset Source
    └─ thin persisted preset locator

        ↓ materialize fresh item(s)

Set-owned IntentRelation
        ↓
source-neutral placement geometry
        ↓
PlacementPlan
        ↓
native YMM4 Undo
```

The source decides **what fresh content is created**.

The Set decides **where and for how long it is placed**.

No second placement engine is introduced.

## Why this direction

v0.5.0 already proves both halves independently:

- Template Sets already use `IntentRelation` and the shared PlacementPlan/native Undo path.
- Tachie Preset discovery already resolves a current Character/plugin/configuration, applies one preset to a fresh FaceParameter and produces a fresh TachieFaceItem.

The remaining duplication is concentrated in two places:

1. Tachie Presets are not yet persistent Set/tile sources.
2. Tachie Presets still use independent `ExpressionPreset` geometry.

The product should remove that duplication instead of creating a third expression-placement model.

## Compatibility-first Source ID rule

Existing persisted `IntentEntry.LibraryEntryId` and existing `IntentAssociationTag.Entry` GUIDs must remain byte/meaning compatible for current Template data.

Do **not** rename the serialized field in this phase.

Instead, code gains the semantic concept **SourceId**:

- an existing Template tile's SourceId is its current `LibraryEntry.Id`;
- a new Tachie Preset tile's SourceId is a new `TachiePresetSourceEntry.Id`;
- the two registries share one GUID namespace;
- validation rejects duplicate IDs across Template Library and Tachie Preset Sources.

New code should use a SourceId alias/helper rather than spreading new `LibraryEntryId` assumptions.

This avoids rewriting existing Settings or existing Template association tags.

## Persisted Tachie Preset Source

A registered Tachie Preset Source stores only a thin, re-resolvable locator.

Candidate shape:

```text
TachiePresetSourceEntry
- Id
- DisplayName
- CharacterName
- PluginRuntimeType
- PluginModuleMvid
- CharacterParameterRuntimeType
- FaceParameterRuntimeType
- RouteKind
- PropertyIdentity
- EditorIdentity?
- CandidateIdentity
```

### Persist

Persist stable source-selection information:

- logical Character name;
- plugin type and Module MVID;
- CharacterParameter / FaceParameter runtime types;
- exact public route identity;
- exact preset candidate identity;
- user-facing display name.

### Do not persist

Do not persist:

- a generated TachieFaceItem;
- a FaceParameter body;
- a copy of PSD/animation/preset state;
- `CharacterConfigIdentity`;
- the current `HostIdentity`;
- a transient capability result/cache;
- discovery confidence as source identity;
- Timeline item references.

`CharacterConfigIdentity` remains an operation-current guard, not persistent source identity.

This lets a source survive ordinary session/config object recreation while still failing closed if the plugin surface or named preset can no longer be resolved.

## Character-bound first version

The first persisted Tachie Preset Source is Character-bound.

A source registered for Character A cannot silently materialize for Character B even if the same Tachie plugin is used.

Cross-Character source reuse is deferred until separate Hands-on/native evidence proves that it is both useful and safe.

## Placement Source registry

Add a bounded resolver over the two existing source stores:

```text
SourceId
-> exactly one Template LibraryEntry
   OR
-> exactly one TachiePresetSourceEntry
-> otherwise fail closed
```

Do not merge the existing Template Library storage into a polymorphic JSON body in this phase.

The current Template Library remains authoritative for Template references.

The new Tachie Preset source list is additive.

This minimizes migration risk.

## Source materialization contract

Placement is split into two stages.

### Stage A — source-specific materialization

Materialize a fresh normalized source snapshot.

Template:

```text
LibraryEntry
-> strict live ItemTemplate resolve
-> TemplateBundle
-> fresh normalized clones
```

Tachie Preset:

```text
TachiePresetSourceEntry
+ current exact Character
-> current TachiePresetProbeTarget
-> validate plugin/type/MVID/route
-> resolve exact candidate identity
-> create fresh TachieFaceItem / FaceParameter
-> apply preset
-> verify retained public state
-> fresh normalized singleton item
```

Preset materialization remains UI-thread-affine and may be asynchronous.

Template materialization may remain synchronous.

### Stage B — source-neutral geometry

Both source kinds then enter the same geometry path:

```text
fresh normalized items
+ source span
+ source Character
+ Set IntentRelation
+ IntentEntry overrides
+ current Context
+ occupancy
-> Frame / Length / Layer
-> PlacementPlan
```

Refactor `BundleLayerPlanner` behind a source-neutral overload that operates on already-normalized fresh items plus a current-source guard.

Keep the current TemplateBundle wrapper so Template regression remains easy to prove.

Do not make all existing callers async merely because Tachie Preset materialization can be async.

## Normal Set/tile behavior

A registered Tachie Preset Source may be used as a normal tile in an applicable targeted Set.

Initial scope:

- Character-bound targeted Sets;
- the selected/current context must resolve one exact matching Character;
- no Generic/time-only Tachie Preset placement in the first slice.

This is sufficient to make Tachie Presets usable outside `表情をまとめて`.

A Set may contain both:

- Template source entries;
- Tachie Preset source entries.

All entries use the Set's same `IntentRelation`.

## Expression Set behavior

An `ExpressionCandidates` Set may also contain both source kinds.

The expression list remains a high-throughput assignment UI.

It no longer owns a separate placement grammar for new registered Tachie Preset Sources.

New registered Preset Sources use the Set's `IntentRelation`, the same as Template entries.

### ExpressionSourceMode

Keep the current Template/TachiePreset mode initially for compatibility/discoverability, but treat it as a **candidate visibility/filter choice**, not as selection of a different placement engine.

Do not require removal of that UI in the first implementation.

A future "all sources" display is separate UX work.

## ExpressionPreset compatibility

`ExpressionPreset` is not deleted in this phase.

It remains the compatibility geometry for:

- existing v0.5 Tachie Preset placements/workflow while the new source path is introduced;
- any legacy expression flow that still explicitly depends on it.

Rules:

- newly registered Set-owned Tachie Preset Sources do not use `ExpressionPreset` geometry;
- no automatic destructive migration of existing `ExpressionPreset` data;
- after the common Set-owned path is native-green and Hands-on accepted, reassess whether the standalone ExpressionPreset UI can be retired or retained only as a compatibility/operation override.

## Association compatibility

### Existing Template association

Keep `IntentAssociationTag` v1 unchanged.

Existing Template-generated expression bundles remain readable and resyncable.

### Existing Tachie Preset association

Keep `TachiePresetAssociationTag` v1 readable.

Existing v0.5 Tachie Preset placements must not become invalid merely because the new common source path exists.

### New Set-owned Tachie Preset association

Add a new Tachie Preset association version for new Set-owned Preset expression placements.

It must identify:

- a unique generated group;
- Palette/Set ID;
- Source ID;
- a canonical hash of the persisted source semantics;
- the retained public FaceParameter state hash.

Tachie Preset v1 materializes exactly one TachieFaceItem, so the new association does not need a generic multi-member bundle format.

The line must remain bounded to the existing Remark safety budget.

Conceptually:

```text
TachiePresetAssociation v2
= group
+ palette
+ source
+ sourceHash
+ stateHash
```

`sourceHash` excludes presentation-only data such as DisplayName.

`stateHash` preserves the current safety rule: if the placed preset face is manually changed after placement, automatic replacement/resync stops instead of guessing.

## Resync

For a new Set-owned Tachie Preset expression:

1. resolve the exact Voice association;
2. require one valid preset member;
3. require current FaceParameter state to match the saved state hash;
4. resolve the exact Palette and SourceId;
5. require the persisted source semantic hash to match the association;
6. re-resolve the source against the current Character/plugin surface;
7. recompute geometry from the Set's current `IntentRelation`;
8. update only Frame / Length / Layer;
9. do not silently reapply preset content during geometry Resync.

Changing expression content remains an explicit source-selection/replacement action.

## Registration UX boundary

Preset discovery remains read-only.

Do not persist every discovered preset merely because it was scanned.

Persistence happens only through an explicit user action such as adding a detected preset to a Set/source collection.

Registered sources can be shown in Settings without rescanning all plugins.

Creating/using one registered preset source may resolve only that relevant Character/plugin surface.

Do not add an eager all-plugin or all-Character scan on:

- plugin open;
- placement tab open;
- Settings open.

## Failure rules

A registered Tachie Preset Source fails locally if:

- Character name does not match;
- plugin type/MVID changed;
- parameter runtime types no longer match;
- saved route cannot be uniquely re-resolved;
- saved preset candidate no longer exists uniquely;
- preset application cannot be verified;
- current source/config changes during the operation.

Never:

- fuzzy-match another preset by label similarity;
- pick another Character automatically;
- copy a stale FaceParameter snapshot;
- fall back to a Template with the same display name.

## Performance boundary

Registered preset placement should be cheaper than full discovery where possible.

Because the persistent source already stores route + candidate identity, execution should:

- resolve only the current source;
- open only the required editor route when needed;
- select only the named candidate;
- avoid enumerating unrelated Characters/plugins;
- retain cancellation/latest-wins rules.

Do not optimize by weakening exact identity checks.

## Settings compatibility

Preferred additive settings change:

```text
PlacerSettings
+ TachiePresetSources = []
```

Existing settings deserialize with an empty list.

No Template Library rewrite is required.

Validate:

- bounded preset source count;
- each preset source body;
- SourceId uniqueness within preset sources;
- SourceId uniqueness across Template Library and preset sources;
- every IntentEntry SourceId resolves to exactly one source when the operation requires it.

A schema bump is not required solely to add the empty-default list if native roundtrip proof confirms old settings remain lossless.

## Explicit non-goals

This phase does not add:

- fake YMM4 ItemTemplates for Tachie Presets;
- copied FaceParameter/template bodies;
- cross-Character preset sharing;
- Generic/time-only preset placement;
- arbitrary source plugins;
- a source DSL;
- preview/checklist UI;
- Composite Placement Steps or Fan-out;
- automatic cleanup/deletion of legacy v0.5 tags/settings.

## Implementation slices

### P0 — source model / compatibility seam

- add TachiePresetSourceEntry;
- add SourceId semantics/registry;
- keep existing serialized `LibraryEntryId`;
- cross-registry uniqueness and roundtrip proof.

### P1 — source-neutral geometry seam

- introduce materialized-source snapshot;
- generalize normalized bundle/layer planning;
- retain current Template wrapper and exact regression.

### P2 — registered preset normal placement

- explicit source registration from a discovered candidate;
- source-specific preset materialization;
- normal targeted Set/tile placement through IntentRelation;
- no expression-list migration yet.

### P3 — expression catalog unification

- ExpressionCandidates Sets may expose both source kinds;
- current source mode becomes candidate filter/visibility only;
- new preset selections use the Set's IntentRelation.

### P4 — common expression mutation safety

- add Tachie Preset association v2;
- exact cross-source replacement;
- state/source guards;
- geometry Resync through Set relation;
- preserve v1 preset and v1 Template tag readers.

### P5 — UX/compatibility closeout

- decide how registered preset sources are added/removed/relinked in Settings;
- keep or simplify source filter based on Hands-on;
- decide whether standalone ExpressionPreset remains visible or compatibility-only;
- Release + Hands-on.

## Exit condition

Phase 2 is complete when:

- Template and registered Tachie Preset content are explicit Placement Source kinds;
- ordinary applicable Sets can place registered Tachie Preset tiles;
- ExpressionCandidates Sets can mix both source kinds;
- new preset placements use the same Set-owned IntentRelation as Template placements;
- existing Template settings/tags remain compatible;
- existing v0.5 Tachie Preset tags remain readable/safe;
- no FaceParameter/item body is persisted as a fake template;
- no second placement engine or Undo path exists;
- full Native/Release regression is GREEN.
