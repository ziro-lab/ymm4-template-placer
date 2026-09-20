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

## Current active feature

UI Micro Polish is implemented and native-green on Draft PR #20; real-user Hands-on acceptance is the remaining gate.

It removes high-frequency interaction friction without changing placement semantics, settings schema, or the PlacementPlan/native Undo architecture:

- always-visible Generic layer controls with field-local wheel;
- placement-panel quick settings;
- direct common Voice row-height drag;
- direct Generic digit entry through the existing bounded input router.

Experimental Tachie Preset support remains prepared separately on Draft PR #19 and resumes after the UI pass is accepted and merged.

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
4. feature-specific active handoff/spec (currently `UI_MICRO_POLISH_DESIGN.md` / `UI_MICRO_POLISH_WORKPLAN.md`);
5. `docs/BACKLOG.md`;
6. historical Round/W documents as evidence.

`docs/DESIGN.md`, older ROADMAP/checkpoint files and versioned Round documents are retained historical design/evidence unless a current document explicitly points to them.
