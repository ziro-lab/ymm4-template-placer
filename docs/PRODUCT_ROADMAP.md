# Product roadmap

This document is the sequencing authority for product work after the accepted **v0.5.0** baseline.

It is intentionally separate from:

- `CURRENT_ARCHITECTURE.md`, which describes the current accepted product/architecture;
- `BACKLOG.md`, which collects unimplemented/deferred ideas without assigning implementation order;
- feature-specific DESIGN / ACCEPTANCE / WORKPLAN documents, which become authoritative only when that feature intentionally starts.

The order below may be refined by Hands-on results, but do not silently reorder or combine phases merely because related ideas exist in the Backlog.

## Product principle for Settings UX

Template Placer deliberately moves repeated editing decisions into one-time setup.

That means a wide configuration surface is not automatically a defect. The important UX problem is **discoverability and comprehension**:

- users should be able to tell what can be configured;
- users should be able to predict where to change a behavior;
- current settings should explain the resulting placement behavior;
- advanced capability should not be removed merely to make the Settings surface look smaller.

Prefer reducing **configuration friction**, not hiding configuration capability.

First-level sections should generally expose their existence. Fold only genuinely detailed/exceptional controls when that improves scanning without hiding a capability.

Important actions may have more than one entry point when that materially improves discoverability. In contrast, fully redundant workflows that add no capability should be candidates for removal.

All Settings surfaces must continue to edit the same staged Settings Draft and protected persistence path. Do not create a beginner schema, alternate Settings model, or second persistence route.

## Current baseline — v0.5.0

v0.5.0 is the integrated baseline.

The Tachie Preset expression-source path is accepted as normal product functionality, including persisted Template/TachiePreset source selection, bounded public preset discovery, exact managed replacement and the retained PlacementPlan/native Undo / Settings safety architecture.

Known follow-up work such as built-in Tachie Preset loading performance remains deferred unless measurement shows it should interrupt the sequence below.

## Planned sequence

### 1. Portable Settings storage — COMPLETE

Portable Settings is merged into main.

Authoritative path:

```text
<YMM4>/user/plugin/Ymm4TemplatePlacer/Data/settings-v04.json
```

Accepted behavior:

- if Portable exists, it is authoritative;
- valid legacy-only LocalAppData Settings migrate safely to Portable;
- the legacy file is retained as backup;
- differing/changed legacy data does not override a valid Portable file;
- corrupt Portable never silently falls back to legacy;
- first migration retains validation, atomic replacement, digest/external-change protection and cross-instance locking;
- real `.ymme` update preservation is backed by public Lab/native evidence;
- Release/native regression is GREEN.

### 2. Expression Source / Placement Model unification — COMPLETE

**Accepted product phase. P0-P8 are complete; Release #615 is GREEN at the exact product/test/package source.**

v0.5.0 proved both Template expressions and Tachie Presets as usable content sources, but they still enter placement through different product paths.

Explicitly review whether Tachie Presets can become reusable placement sources rather than remaining primarily an expression-list-only source.

Question to answer:

> Can Template and Tachie Preset be represented as different source kinds that both materialize fresh item(s) and then use the same Set-owned placement rule?

Candidate model:

```text
Placement Source
├─ YMM4 Template
│    └─ strict TemplateLocator
└─ Tachie Preset
     └─ explicit plugin/surface/preset identity

        ↓ materialize fresh item(s)

common Placement Rule
        ↓
PlacementPlan / native Undo
```

Desired product result if the model proves sound:

- Tachie Preset entries can be used from ordinary Sets/tile surfaces, not only from `表情をまとめて`;
- one expression Set may contain both Template and Tachie Preset source entries;
- source identity and placement behavior are separate concepts;
- the expression list remains a high-throughput assignment UI rather than the only place where Tachie Presets can be used;
- common placement behavior can move toward the Set instead of being split between `IntentRelation` and independent `ExpressionPreset` geometry.

Safety boundaries:

- **do not fake Tachie Presets as YMM4 ItemTemplates**;
- do not serialize a generated `TachieFaceItem` / FaceParameter body as a second Template database merely to make the types look uniform;
- persist a thin resolvable preset-source identity/recipe and materialize a fresh current item at execution time;
- preserve strict source re-resolution, current Character/plugin/config validation and exact managed-association guarantees;
- keep unsupported/ambiguous preset sources as local failures;
- begin Character-bound if that is the safest stable identity; cross-Character source reuse requires separate evidence.

The design review result is **converge behind one explicit Placement Source abstraction** while keeping Template and Tachie Preset identities explicit.

Current authorities:

- `PLACEMENT_SOURCE_UNIFICATION_DESIGN.md`;
- `PLACEMENT_SOURCE_UNIFICATION_ACCEPTANCE.md`;
- `PLACEMENT_SOURCE_UNIFICATION_WORKPLAN.md`.

Frozen direction:

- keep existing Template Library storage and serialized Template Source IDs compatible;
- add thin Character-bound Tachie Preset Source locators rather than fake ItemTemplates;
- materialize source-specific fresh item(s) first, then use one Set-owned `IntentRelation` geometry path;
- keep existing v0.5 Template and TachiePreset association readers compatible;
- use a backward-compatible new preset association version for Set-owned preset expressions;
- keep standalone `ExpressionPreset` only as compatibility while the new common path is proven.

Implemented candidate:

- ordinary Character-bound Sets can place registered Tachie Preset sources beside Template sources;
- ExpressionCandidates Sets can mix registered Template/Preset content;
- registered presets use the owning Set's `IntentRelation` in ordinary placement, expression replacement and geometry Resync;
- a backward-compatible preset association v2 preserves exact Set/Source/state identity while old v0.5 preset tags remain readable;
- detection alone does not persist a Source; `Setへ登録` is explicit;
- registered sources appear in Settings without eager plugin discovery;
- Template/TachiePreset source mode remains a visibility/filter choice;
- the old standalone `ExpressionPreset` path remains only for unregistered direct preset placement compatibility.

Release #615 (`35834323751`) at `7ffe3b2c690a57850c77821f7b4c3eeb6517b3dc` passed **1,763 Native assertions**, P0-P7 source gates, full retained semantic/evidence regression, exact distribution-DLL smoke and verified `.ymme` / source / provenance packaging. Phase 2 is complete.

Exit:

- ordinary Sets can place registered preset sources;
- ExpressionCandidates Sets can mix Template and preset sources;
- new registered preset placements use Set-owned `IntentRelation`;
- existing v0.5 settings/tags stay safe and readable;
- full Native/Release acceptance is GREEN.

### 3. Placement rule inventory / bounded model completion — ACTIVE

Before implementing a visual behavior preview, decide whether the near-term placement vocabulary is complete enough to explain.

Review the current model and the Backlog together.

Inventory result:

- implement the missing **center alignment** by extending the existing `IntentAlignment`; do not add a duplicate general Source Pivot model;
- implement **Absolute Layer** as an additive mode on the existing bounded layer policy;
- current Target contracts are already sufficient before Preview; stronger target grammar is deferred;
- current previous/next same-type / same-Character Neighbor selectors are already sufficient before Preview; arbitrary/typed neighbor grammar is deferred;
- Composite Placement Steps and Fan-out are deferred because they change output multiplicity and deserve a separate design after the one-source Preview model is understood.

Authorities for this active phase:

- `PLACEMENT_RULE_COMPLETION_DESIGN.md`;
- `PLACEMENT_RULE_COMPLETION_ACCEPTANCE.md`;
- `PLACEMENT_RULE_COMPLETION_WORKPLAN.md`.

Rule:

> Do not implement every collected Placement Recipe idea before Preview. Add only the placement axes that are likely to be part of the near-term normal product vocabulary.

Also use this phase to finish the Expression Source decision from Phase 2. A Preview should not be built against a placement model that is already expected to be reorganized immediately afterward.

Exit:

- the near-term placement vocabulary is explicitly listed as implemented now / implement before Preview / defer;
- source vs. placement responsibilities are frozen for the next UX phases;
- any pre-Preview placement additions are Native GREEN;
- the Preview/Checklist can consume one authoritative placement-description model without source-specific geometry forks.

### 4. Compact Settings friction / discoverability pass

Improve the existing Settings surface after the source/placement model is understood, but before introducing a separate large workspace.

Primary goal:

> Make it obvious **where to go to change a behavior** without reducing the available configuration width.

Direction:

- remove first-level folding where it hides the existence of ordinary Set capabilities;
- keep major destinations visible, including `どのアイテムで使うか`, `どう置く？`, `演出と並び順`, `セットの管理` and `テンプレートをまとめて追加`;
- keep only genuinely detailed controls behind bounded secondary disclosure;
- preserve the current human-readable `このセットの動き` summary;
- improve labels, grouping and spacing based on actual Hands-on use rather than minimizing vertical length;
- retain duplicate entry points when they solve a real discoverability problem.

Also review truly redundant workflows. Current candidate:

- remove the single-Template registration path if `テンプレートをまとめて追加` completely subsumes it, including the one-item case.

Do not remove independent placement axes merely to shorten the screen.

Exit:

- ordinary Set capabilities are discoverable without opening multiple first-level containers;
- the common source/placement model from Phases 2-3 is understandable in Compact Settings;
- bounded Hands-on at narrow and normal Tool widths identifies no major navigation ambiguity;
- no second Settings model or persistence route is introduced.

### 5. Behavior preview + “what I want” checklist

Add a reusable Settings-assistance surface that can be opened from both Compact Settings and the later Full Settings Workspace.

This phase starts only after the source/placement responsibilities and near-term placement vocabulary are frozen enough that the preview is not expected to be immediately rewritten.

#### Behavior preview

Show the meaning of the current Settings as a small schematic timeline/placement diagram.

Initial scope should be a **read-only projection**, not another placement engine and not a direct-manipulation editor.

Examples of useful visible meaning:

- selected/Voice target and anchor;
- source pivot/alignment when applicable;
- start/end/center relationship;
- duration relationship;
- relative or absolute layer behavior;
- bounded collision-search direction;
- next-related-Voice behavior when applicable;
- source kind only where it materially affects availability, not placement geometry.

The existing textual `このセットの動き` summary and the visual preview should describe the same Settings Draft.

Do not duplicate placement semantics inside the preview. Derive a bounded Preview Model from the authoritative source/placement Settings model.

#### “What I want” checklist

Provide an alternate way to construct the same Settings by describing the intended outcome, for example:

- what is the target/context;
- what source/content should be placed;
- where should placement start/end;
- what duration should be used;
- above/below/absolute layer and how far;
- what should happen when the destination is occupied or a required neighbor is missing.

The checklist is an **editing projection of the existing Settings Draft**, not a second configuration model.

Safe interaction model:

```text
current Settings Draft
-> temporary checklist draft
-> preview / textual result
-> explicit apply
-> current Settings Draft
```

Cancel must leave the current Settings Draft unchanged.

Start from the currently edited Draft, not from only the last persisted Settings.

Initial preview should remain read-only. Do not add preview drag/edit gestures in the first implementation.

The exact layout (text above preview vs. beside it, vertical vs. horizontal composition) should be decided by Hands-on use rather than frozen prematurely.

Exit:

- textual summary and visual preview are projections of the same authoritative Draft;
- Checklist Apply produces the same Draft shape as direct Settings editing;
- Checklist Cancel is zero-change;
- Template and TachiePreset sources use the same placement-description path where Phase 2 decided they should;
- no placement semantics are reimplemented in the preview layer.

### 6. Full Settings Workspace (“Settings mode”)

After the compact surface and reusable assistance components are understood, create a larger settings-only workspace for managing growing Sets and tiles.

This is not a second Settings system.

It must edit the same model, same staged Draft and same protected persistence route as Compact Settings.

Current candidate spatial structure:

```text
left navigation        center structure         right inspector
Item type          ->  Sets / tiles         ->  selected settings
```

Primary purpose:

- make Settings location spatially predictable;
- make Sets/tiles easier to compare and manage at scale;
- provide enough space to expose capabilities without relying on first-level hiding.

Reuse rather than recreate:

- `このセットの動き` summary;
- behavior preview;
- “what I want” checklist;
- existing Set/tile/source editors and validation semantics where practical.

Compact Settings remains useful for ordinary quick edits. Both entry points must converge on the same authoritative Settings state.

Exit:

- Compact and Full Settings edit the same authoritative Draft;
- both surfaces observe the same validation/auto-commit/conflict/rollback semantics;
- Preview/Checklist are reused rather than reimplemented;
- no second schema, source model, placement engine or persistence route exists.

## Items that do not automatically interrupt this sequence

The Backlog still contains useful extensions such as Placement Recipe axes, Action Tiles and performance polish.

Their presence is not implementation priority by itself.

Unless a bug, regression or measured performance issue requires otherwise, prefer completing the Settings/storage sequence above before broadening the product with additional placement capability.
