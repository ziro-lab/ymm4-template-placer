# Backlog

This is the collection point for **unimplemented or deferred product ideas**.

Being listed here is not implementation approval or priority commitment. Current implementation order is controlled by `docs/PRODUCT_ROADMAP.md`.

Current roadmap state: **Behavior Preview + “what I want” checklist is NEXT.** Do not use this Backlog to silently jump ahead of that phase.

## Performance polish

### Built-in Tachie Preset loading performance

Status: **DEFERRED / MEASURE FIRST**

Built-in/standard Tachie Preset loading works, but Hands-on use can feel heavier than the Template route.

Before changing architecture:

- profile the real built-in discovery/editor-construction/session-cache path;
- keep discovery scoped to distinct current Voice Characters while expression + TachiePreset is active;
- preserve cancellation/latest-wins behavior;
- preserve Template-mode performance;
- change only the measured hot path.

This is performance polish, not a reason to reopen the accepted Placement Source architecture.

## Full Settings Workspace

Status: **PLANNED / AFTER PREVIEW**

The compact Settings surface remains the ordinary quick-edit surface. A later larger workspace should edit the **same Settings Draft, same schema and same protected persistence path**.

Candidate spatial structure:

```text
left navigation        center structure         right inspector
Item type          ->  Sets / tiles         ->  selected settings
```

Goals:

- make current location obvious without help text;
- make Sets/tiles easier to compare and manage at scale;
- keep add/remove actions near the collections they affect;
- reuse the same recognizable tile names/colors/shapes;
- reuse `このセットの動き`, Behavior Preview and the checklist rather than reimplementing them.

Search/filtering may later match Set name, tile alias, source name, Character and broken-reference state, but browsing must remain usable without search.

Safety:

- no second Settings schema;
- no second persistence route;
- no Timeline mutation from the Settings workspace;
- no duplicate placement engine.

## Placement Recipe extensions

Status: **COLLECTING / AFTER PREVIEW UNLESS REPRIORITIZED**

The current start/center/end alignment and Absolute Layer model are already accepted. Additional axes need separate justification.

### Composite Placement Steps

One tile/Recipe may eventually place several independent sources with different finite rules.

Every Step must read the same immutable operation-start Context. Step A output must not become Step B input.

All Steps preflight together; required failure means zero write; success commits once with native Undo.

### Stronger Target conditions

Possible finite conditions include:

- same Character across selected targets;
- explicit count per Item type;
- explicit before/after roles;
- finite mixed-type signatures.

Do not add arbitrary boolean trees, regex predicates or a generic rule language.

### Fan-out

Possible explicit scopes:

- each explicitly selected Item;
- each explicit adjacent boundary.

All results must be preflighted as one operation. Deduplication rules must be defined before implementation.

### Stronger Neighbor selectors

Possible finite additions:

- next/previous specified Item type;
- previous/next any Item;
- next same-Character Voice.

No fuzzy “nearest suitable thing” behavior.

## Action Tile / shortcut candidates

Status: **COLLECTING / RESEARCH BEFORE IMPLEMENTATION**

These ideas were previously parked in Draft PR #33 and are incorporated here so they do not need a long-lived stale PR.

### Highest-priority shortcut candidate: native Undo / Redo

Current hypothesis:

> Native Undo / Redo may be the strongest always-available shortcut for Template Placer because it supports the normal try -> inspect -> revert -> try another tile loop.

UI direction:

- place two compact persistent Undo / Redo controls in the existing top context/Set strip;
- **do not increase the strip height**;
- prefer a fixed right-side position;
- preserve useful width for current context and Set selection;
- disabled state follows actual native Undo/Redo availability.

Boundary:

- use **YMM4 native Undo / Redo**;
- do not create a Template-Placer-only history;
- do not search for “the last Template Placer item” and delete it;
- verify the supported native command route and enabled-state behavior in real YMM4 before implementation.

### Other command candidates

Compare, but do not automatically promote:

- previous / next relevant Item selection;
- repeat last Template Placer action with strict current-context resolution;
- play / pause;
- split;
- delete;
- copy / paste.

Ordinary one-command operations should remain YMM4 command/action candidates rather than bespoke transformation engines.

### Interaction-surface candidates

Potential surfaces that should reuse the existing tile/action execution route:

- **Quick Palette** near the pointer/editing location;
- **tile/action search** over currently exposed Template Placer actions;
- visual grouping/separators;
- finite **Action Tiles**.

Do not turn Template Placer into a general launcher for external apps, arbitrary scripts or macros.

## Item Action / Transformer candidates

Status: **COLLECTING / SEPARATE EXECUTION RESPONSIBILITY**

These ideas were previously parked in Draft PR #29 and are incorporated here.

Use an Item Action only when one ordinary YMM4 command cannot directly express the intended edit.

### Proportional split

- split the selected Item at 1/2 of its current span;
- consider finite 1/3 or 2/3 variants only if hands-on use justifies them;
- define deterministic odd-frame rounding;
- prefer YMM4's native split route after deriving the exact point.

### Effect Template apply

Candidates:

- append one registered Video Effect Template;
- append one registered Audio Effect Template;
- replace the selected Item's complete relevant VideoEffects/AudioEffects chain;
- possibly clear the relevant chain if later justified.

Boundary:

- Effect application is **not** a Placement Source;
- keep YMM4 as owner of Effect Template bodies;
- store thin live references rather than a second effect-body database;
- use a separate bounded Item Action / Effect executor behind the shared tile surface.

### Fit playback speed to an exact target span

- derive playback rate from an explicit target duration/reference;
- use only supported compatible media Item types;
- prefer host-native timing/property semantics;
- do not infer a vague “good” duration.

### Extend or trim to an exact boundary

Candidates:

- set selected Item end to the next exact Item/Voice start;
- set start/end to another explicitly selected target boundary.

No fuzzy target selection. Distinguish destructive trim from safe extension.

### Match Item duration

- match one selected Item to one explicit reference Item;
- multi-selection variants such as shortest/longest remain optional future policies.

Do not mix this with playback-speed fitting: equal timeline Length and content retiming are different actions.

### Fixed-gap multi-Item arrangement

- operate only on an explicit selected Item set;
- use one finite gap;
- preflight every resulting Frame;
- commit as one native Undo operation.

Keep this lower priority than single-Item actions until the Item Action boundary is proven.

### Shared Item Action safety

Any future Item Action should keep:

- explicit target(s);
- finite action kinds;
- no general property editor or scripting layer;
- complete preflight before multi-write mutation;
- one native Undo unit;
- failure before commit = zero mutation;
- host-native operations where they preserve semantics.

Normal Placement remains add-only even if Placement and Item Actions share a tile surface.

## Deferred / out of scope

### General automation language

Do not add:

- arbitrary C# expressions;
- scripting DSL;
- node graph;
- loops;
- previous-Step conditional branching;
- AI-selected ambiguous targets.

### Item-type custom ordering

Status: **DEFERRED**

Potentially useful, but lower frequency than the current roadmap work.

## Completed references

Completed work belongs in its accepted authority/history rather than occupying the active Backlog:

- Portable Settings -> `CURRENT_ARCHITECTURE.md` / `PRODUCT_ROADMAP.md`;
- Placement Source unification -> `PLACEMENT_SOURCE_UNIFICATION_*.md`;
- bounded Placement Rule completion -> `PLACEMENT_RULE_COMPLETION_*.md`;
- Compact Settings friction pass -> `COMPACT_SETTINGS_FRICTION_*.md`;
- Baseline Simplification -> `BASELINE_SIMPLIFICATION_*.md`.

Historical PRs/runs remain evidence; this file only needs the still-open product questions.

## Backlog discipline

When adding an idea:

1. state the user problem first;
2. state whether it changes UI, Context, planning axes, output multiplicity, or product boundary;
3. state the safety boundary;
4. do not create implementation tasks until related ideas have been collected/reviewed;
5. move an idea into a feature DESIGN / ACCEPTANCE / WORKPLAN only when implementation intentionally starts.

Do not use this file as a reason to broaden the current active phase automatically.
