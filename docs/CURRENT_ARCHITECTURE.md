# Current architecture

This is the current product/architecture authority for YMM4 Template Placer.

Historical Round/W documents remain evidence of how the product reached this state. They are not instructions to rebuild superseded UI or architecture.

## Product role

YMM4 Template Placer is a **safe placement tool**.

Its normal responsibility is:

```text
read explicit Timeline context
-> evaluate one saved finite placement rule
-> resolve the referenced live YMM4 Template
-> clone new item(s)
-> decide Frame / Length / Layer
-> preflight the complete operation
-> if any required plan fails: write nothing
-> otherwise commit the complete addition with one native Undo unit
```

The core boundary is:

> Use the relation to an existing target/context to place new content safely. Do not turn Template Placer into a general transformer of existing Timeline content.

## Source of truth

Live YMM4 Item Templates are authoritative.

The plugin stores thin references, aliases, finite placement settings and presentation metadata. It does not maintain a second copy of template bodies.

A missing or ambiguous template reference fails closed. Do not fuzzy-match by name, order, text or proximity.

## Normal placement safety

Normal placement is add-only.

It may:

- read selected/current Timeline context;
- clone referenced Template bundles;
- set planned Frame / Length / Layer on clones;
- search a bounded layer direction when that saved rule explicitly permits it;
- add the fully prepared result.

It may not:

- move unrelated existing items;
- shorten unrelated existing items;
- delete unrelated existing items;
- modify effects/properties/volume on existing items;
- invent a target when context is ambiguous.

All required members of a multi-item bundle are planned before mutation. Planned items reserve occupancy against each other before commit.

## Current normal UI model

Normal placement is:

```text
Timeline context
-> applicable Set
-> tile
-> placement
```

The Set owns applicability and placement relation. Tiles primarily identify a referenced Template plus small appearance/parameter overrides.

For **normal targeted Sets**, the Item type is the ownership parent:

```text
one Item type
-> its Sets
-> each Set's tiles
```

A newly created normal targeted Set has exactly one `Target.ItemTypeKey` and `UniformType`. The existing serialized target shape is retained; no second owner field exists.

Cross-Item reuse is explicit snapshot copy. Copying a Set creates a new Guid under the destination Item type and copies the current relation/conditions/entries/appearance. Source and destination are independent afterward.

Existing settings that contain multi-type Sets are preserved losslessly and retain their existing runtime matching semantics. Settings surfaces them only through the bounded `複数種類` compatibility context; normal creation does not create new shared multi-type Sets.

Generic Sets remain a separate existing model.

Current global presentation state is shared across Sets:

- Auto / Fixed layout;
- fixed column count;
- position shortcuts;
- expression viewport-follow behavior;
- common Voice row height.

These are not Set-local settings.

## Generic placement

Generic/time-position placement uses Timeline.CurrentFrame as the time context.

Its current layer rule is an explicit numeric target with one of the finite occupied-layer behaviors:

- do not place;
- search upward (smaller layer numbers);
- search downward (larger layer numbers).

No clicked-screen-coordinate layer inference is used.

## Settings model

Settings use a staged working draft and protected automatic persistence.

A Settings session keeps:

1. immutable opening baseline;
2. current working draft, including temporarily invalid text;
3. latest successfully committed fingerprint.

Valid complete edits are coalesced and persisted through the protected settings store. Invalid/incomplete edits remain visible and are not persisted.

`今回の変更を戻す` restores the exact session-opening settings state through the same protected store.

Settings persistence must keep:

- validation;
- atomic replacement;
- disk digest/external-change protection;
- cross-instance locking;
- no silent rebase/retry over a conflict.

`PlacerSettingsStore` is an intentional product boundary, not a temporary substitute for generic plugin-settings helpers. Do not replace it mechanically with host helpers such as `SettingsBase<T>`. The current product contract additionally requires complete-draft/schema validation, the 1 MiB guard, fail-closed handling for corrupt/future settings, digest-based external-change detection, cross-instance locking, atomic replacement and exact Settings-session rollback. Revisit this choice only if a host API is proved to preserve the same conflict-aware/fail-closed contract without weakening those guarantees.

Settings operations do not mutate Timeline or original YMM4 Templates.

## Expression workspace

`表情をまとめて` is a specialist high-throughput workflow, not permission to make normal placement destructive.

Current Template-source behavior may atomically replace/remove only an **exactly identified Plugin-managed expression bundle** for one Voice after complete preflight.

It must never infer or delete a manual/unassociated expression by position, text or order.

Voice row navigation has one authoritative root-owned route:

```text
close prior trial boundary
-> set Timeline.CurrentFrame
-> select Voice
-> public Preview seek when available
-> viewport follow independently when available
```

Rapid navigation is latest-wins.

Excel remains a secondary assignment bridge. Import alone does not mutate Timeline.

## Placement engine boundary

The authoritative mutation path remains:

```text
UI intent
-> root/coordinator admission
-> resolver/planner
-> complete preflight
-> PlacementPlan
-> native YMM4 Undo
```

Do not create a second placement engine or custom Undo stack for a new feature.

## Future Placement Recipe boundary

The following is the accepted **future design boundary**, not a statement that all features below are implemented.

A future Placement Recipe may contain one or more finite Placement Steps.

Every Step must read the same immutable operation-start Context.

```text
freeze Context
-> evaluate Step A
-> evaluate Step B
-> evaluate Step C
-> preflight every result together
-> any required failure = zero write
-> success = one atomic PlacementPlan/native Undo operation
```

A Step must not consume the result of a previous Step.

Allowed future placement axes include finite additions such as:

- Template Pivot;
- Absolute Layer;
- Composite Placement Steps;
- stronger finite Target conditions;
- Fan-out over explicit selected targets/boundaries;
- stronger finite Neighbor selectors.

This does **not** authorize:

- arbitrary C# expressions;
- DSL/node graphs;
- loops;
- conditional branching on previous Step output;
- AI/fuzzy target choice;
- existing-item transformation.

## Accepted v0.4.2 baseline and next feature

The current accepted main baseline contains PR #20, PR #22 and PR #23.

Accepted Git state:

- main merge commit: `de85c312347ea35371d1c58a92992b50f64cdeb6`;
- final pre-merge product candidate: `f626e7c71385998b22a6d29e43a3fa349cf03f18`;
- Release run #399 `35608381259` / job `106361040827`;
- **1,518 Native assertions PASS / 0 FAIL**;
- exact distribution DLL smoke and verified stable-root package/provenance: PASS.

The accepted baseline includes:

- Item-owned targeted Sets with explicit snapshot copy across Item types and bounded legacy multi-type compatibility;
- quick-settings light-dismiss, common Voice row-height controls and final Hands-on UI polish;
- lazy expression loading, expression-task-only Voice monitoring, latest-wins/cancelable background preparation, linear association/candidate indexing, reusable rows and cached aggregates;
- 100 / 500 / 1,000 Voice structural performance fixtures;
- direct Set deletion beside the Set picker using the existing protected deletion route;
- nested Settings wheel routing based on the current physical cursor position rather than stale event-source state.

Known deferred minor issue:

- continuous wheel rotation while crossing from an inner Settings/list area to outer content can still pause for a few wheel notches before self-recovering;
- no data loss, persistent input lock or explicit recovery requirement is known;
- do not broaden to global/window-level input interception unless the symptom materially worsens or a bounded local fix is proved.

The next prepared feature is **Experimental Tachie Preset** Draft PR #19. It remains separate from the accepted baseline. Before resuming implementation, refresh/rebase its branch onto current main and review its assumptions against this document, the current validation strategy and the accepted expression-performance boundary.

## Validation

Read `docs/VALIDATION_STRATEGY.md`.

- Focused: ordinary development feedback;
- Checkpoint: complete semantic/history/evidence regression at a feature checkpoint;
- Release: Checkpoint plus exact distribution DLL smoke and verified packaging.

Historical tests are not permanent merely because they belong to an old Round. They may retire only under the lifecycle/retirement rules in the validation strategy.

## Document authority

For current work, prefer documents in this order:

1. this file;
2. `docs/GLOSSARY.md`;
3. `docs/VALIDATION_STRATEGY.md`;
4. current accepted feature authorities such as `docs/FINAL_HANDS_ON_POLISH.md` and the expression-performance design/acceptance documents;
5. the active feature's own handoff/spec after it has been refreshed onto current main;
6. `docs/BACKLOG.md`;
7. historical Round/W documents when reconstructing rationale.

Historical documents are evidence of how the product reached its current state, not automatic implementation instructions. They may contain superseded decisions; an explicit superseded note or this current architecture wins.

External repositories, community plugins, official samples and API documentation are **references / precedents** unless a current Lab/native proof turns the relevant host claim into version-scoped evidence. See `docs/RESEARCH.md`.

`docs/DESIGN.md`, older ROADMAP/checkpoint files and versioned Round documents are retained historical design/evidence unless a current document explicitly points to them.
