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

### 1. Portable Settings storage

**Next implementation priority.**

Move Template Placer Settings from the current LocalAppData location to a YMM4-portable location while preserving the existing protected store guarantees.

Use the detailed requirements and Lab evidence in `BACKLOG.md`.

Key boundaries:

- preserve schema validation, atomic replacement, digest/external-change protection, cross-instance locking and fail-closed behavior;
- migrate existing valid Settings safely;
- never silently choose between divergent valid old/new Settings files;
- do not mix placement or Settings-UX redesign into this work.

### 2. Compact Settings friction / discoverability pass

Improve the existing Settings surface before introducing a separate large workspace.

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

### 3. Behavior preview + “what I want” checklist

Add a reusable Settings-assistance surface that can be opened from both Compact Settings and the later Full Settings Workspace.

#### Behavior preview

Show the meaning of the current Settings as a small schematic timeline/placement diagram.

Initial scope should be a **read-only projection**, not another placement engine and not a direct-manipulation editor.

Examples of useful visible meaning:

- selected/Voice target and anchor;
- start/end/center relationship;
- duration relationship;
- relative layer direction and distance;
- bounded collision-search direction;
- next-related-Voice behavior when applicable.

The existing textual `このセットの動き` summary and the visual preview should describe the same Settings Draft.

Do not duplicate placement semantics inside the preview. Derive a bounded Preview Model from the authoritative Settings/relation model.

#### “What I want” checklist

Provide an alternate way to construct the same Settings by describing the intended outcome, for example:

- what is the target/context;
- where should placement start/end;
- what duration should be used;
- above/below and how far;
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

### 4. Full Settings Workspace (“Settings mode”)

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
- existing Set/tile editors and validation semantics where practical.

Compact Settings remains useful for ordinary quick edits. Both entry points must converge on the same authoritative Settings state.

## Design review gate — expression placement Settings

During the Settings-friction work, explicitly re-evaluate whether the current independent ExpressionPreset placement Settings still earn their complexity.

Question to answer:

> Can expression content source (Template / Tachie Preset) be separated from placement so that an expression Set owns the common placement rule?

Potential direction:

```text
Expression Set
├─ applicability / placement rule
└─ expression source entries
   ├─ Template
   └─ Tachie Preset
```

This is **not yet an approved migration**.

Do not fake Tachie Presets as YMM4 ItemTemplates. If this direction is pursued, keep source identity explicit and preserve exact managed-association / compatibility guarantees.

Retain a separate per-session or per-operation override only if Hands-on use proves that users genuinely need to switch duration/placement behavior independently of the Set.

## Items that do not automatically interrupt this sequence

The Backlog still contains useful extensions such as Placement Recipe axes, Action Tiles and performance polish.

Their presence is not implementation priority by itself.

Unless a bug, regression or measured performance issue requires otherwise, prefer completing the Settings/storage sequence above before broadening the product with additional placement capability.
