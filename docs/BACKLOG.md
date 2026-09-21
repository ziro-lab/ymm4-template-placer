# Backlog

This is the single collection point for **unimplemented or deferred product ideas**.

Being listed here is not implementation approval or priority commitment.

## Active

### Experimental Tachie Preset source

Status: **ACTIVE PREPARATION — Draft PR #19**

Use `docs/EXPERIMENTAL_PRESET_HANDOFF.md`.

Goal:

- Template/Tachie-Preset source switch in `表情をまとめて`;
- structural detection through current Character/plugin and fresh FaceParameter;
- exact managed preset association;
- reuse current placement geometry/PlacementPlan/native Undo.

Do not repeat completed generic-preset research.

## Accepted UI polish / known residual

The former UI Micro Polish items are implemented in the accepted v0.4.2 baseline (PR #20 / #23) and are no longer backlog implementation work.

Known deferred minor issue:

- continuous wheel rotation while crossing from an inner Settings/list area to outer content can still pause for a few wheel notches before self-recovering;
- revisit only if it becomes persistent, requires explicit recovery, affects normal controls, or a bounded local fix is proved.

## Placement Recipe extensions — collect before implementation

Status: **COLLECTING**

Architecture boundary is defined in `docs/CURRENT_ARCHITECTURE.md`.

### 1. Template Pivot

Priority: **very high**

Align Template/Bundle start / center / end / explicit internal offset to the chosen target Anchor.

High value because current target Anchors are already rich, while the source-side alignment point is comparatively limited.

### 2. Absolute Layer

Priority: **very high**

Add an explicit Layer N base policy.

Reuse existing finite collision behavior where possible:

- place only if free;
- search one direction;
- never move/shorten/delete existing items.

### 3. Composite Placement Steps

Priority: **high**

One tile/Recipe may place several independent sources with different finite rules.

Example:

```text
強調

Frame:
  target span
  target-relative layer

Flash:
  target center
  Template Pivot = center
  different target-relative layer

SE:
  target center - 3f
  Absolute Layer = 50
```

Every Step must read the same immutable initial Context. Step A output cannot become Step B input.

All Steps preflight together; any required failure means zero write; success commits once with native Undo.

### 4. Stronger Target conditions

Priority: **medium**

Possible finite conditions:

- same Character across selected targets;
- explicit count per Item type;
- explicit before/after roles;
- finite mixed-type signatures.

Do not add arbitrary boolean trees, regex predicates or a generic rule language.

### 5. Fan-out

Priority: **medium**

Possible explicit scopes:

- each selected item;
- each explicit adjacent boundary.

All fan-out results must be one complete preflighted operation.

Deduplication of identical generated anchors must be specified before implementation.

### 6. Stronger Neighbor selectors

Priority: **medium**

Finite additions may include:

- next specified Item type;
- previous specified Item type;
- previous/next any Item;
- next same-Character Voice.

No fuzzy “nearest suitable thing” behavior.

## Deferred / out of scope

### Existing-item transformation

Keep outside Template Placer normal placement:

- move existing item;
- change existing Length;
- delete existing item;
- change Effect/property values;
- change volume;
- arbitrary retiming.

If this becomes a product, it belongs in a separate Transformer/Automation responsibility.

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

Potentially useful, but lower frequency than the placement and panel ideas above. Keep separate until hands-on usage justifies it.

## Backlog discipline

When adding an idea:

1. state the user problem first;
2. state whether it changes UI, Context, planning axes, output multiplicity, or product boundary;
3. state the safety boundary;
4. do not create implementation tasks until related ideas have been collected/reviewed;
5. move an idea into a feature handoff/spec only when implementation is intentionally starting.

Do not use this file as a reason to broaden the current active PR automatically.
