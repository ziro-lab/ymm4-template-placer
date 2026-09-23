# Compact Settings Friction / Discoverability Design

Status: **FROZEN DESIGN CANDIDATE — IMPLEMENTATION MAY PROCEED ONLY WITHIN THIS SCOPE**

Authority:

- `CURRENT_ARCHITECTURE.md`
- `PRODUCT_ROADMAP.md`
- `COMPACT_SETTINGS_FRICTION_ACCEPTANCE.md`
- `COMPACT_SETTINGS_FRICTION_WORKPLAN.md`

## Goal

Reduce friction in the existing compact Settings surface before Behavior Preview / Checklist work begins.

This phase changes presentation and layout only. It does **not** change the settings model, placement semantics, source resolution, persistence, Preview behavior or native Undo.

The user will perform a real YMM4 hands-on pass before Phase 5 starts. Phase 5 is blocked until that pass says the remaining Settings friction is acceptable.

## First-level visibility

The following ordinary Set capabilities must be visible without opening a first-level Expander:

- `セットの管理`;
- `どのアイテムで使うか`;
- `どう置く？`;
- `演出と並び順`;
- `テンプレートをまとめて追加`.

They may use visible section headers and indentation/borders, but they must not require a disclosure click merely to discover that the capability exists.

Keep the current semantic order as much as practical. `セットの管理` stays close to the Set picker because its actions operate on the current Set.

## Secondary disclosure retained

The following are intentionally still folded:

- `対象の詳細`;
- `細かく調整`;
- `選択した演出だけの微調整`;
- `全体の表示・操作`.

These are detailed, lower-frequency or global controls. Opening/closing them must not change persisted values.

`全体の表示・操作` moves lower in the compact Settings flow so global presentation state does not occupy the primary Set-definition area.

## Width and scrolling

The outer Settings ScrollViewer remains the authoritative vertical scroll route.

The bulk Template source list currently consumes nearly the whole content width. Phase 4 must reserve empty outer-scroll escape space on **both** sides of that list, with a somewhat wider strip on the **left** because:

- the right side already contains the list scrollbar and naturally serves direct inner-list scrolling;
- an empty left strip gives the pointer an easy place to wheel the outer Settings surface without interacting with a row/control.

Initial implementation values are intentionally provisional:

- left escape strip: about **16 DIP**;
- right escape strip: about **6 DIP**.

These are not product constants. Narrow/normal-width hands-on may increase or reduce them. The list must not be aggressively narrowed merely to satisfy a numeric target.

Do not change nested-wheel ownership/routing in this phase unless hands-on proves spacing alone is insufficient.

No first-level section may force horizontal scrolling or increase the compact Settings minimum width merely because of long Template names. Long source labels must continue to wrap within the available list width.

## Set picker row

The Set picker ComboBox and adjacent `＋` / `削除` buttons should read as one row:

- comparable visible height;
- aligned vertical position;
- no global Button/ComboBox style change.

Exact height remains a hands-on visual tuning point.

## Preserve existing behavior

Keep unchanged:

- protected automatic Settings persistence / rollback;
- Set creation, duplicate, copy, delete and reorder commands;
- current source bulk-add behavior and filtering;
- current relation summary;
- current advanced-field conditional visibility;
- Placement Source and Placement Rule semantics;
- nested-wheel routing;
- Generic placement behavior.

## Explicit non-goals

Do not add in this phase:

- Behavior Preview;
- checklist / guided configuration;
- Full Settings Workspace;
- direct manipulation;
- new placement rules;
- search/filter workspace features;
- new persistence schema;
- removal of an old workflow unless native proof shows it is fully redundant and the removal is separately accepted.

## Phase exit

Implementation may reach Native-green before hands-on, but Phase 4 is not product-complete until the user checks the real YMM4 surface at narrow and normal Tool widths.

Hands-on specifically decides:

- whether first-level sections are easy to find;
- whether the left scroll escape strip is large enough but not wasteful;
- whether the bulk source list is still wide enough;
- whether Set picker row sizing feels coherent;
- whether the overall vertical length feels acceptable;
- whether any remaining click/scroll friction should be fixed before Preview.

Preview / Checklist work must not begin before this gate.
