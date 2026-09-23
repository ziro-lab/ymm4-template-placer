# Compact Settings Friction / Discoverability Design

Status: **ACCEPTED — RELEASE #654 GREEN / USER HANDS-ON APPROVED**

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

The Set picker ComboBox and adjacent `＋` button should read as one row:

- comparable visible height;
- aligned vertical position;
- the duplicate top-row Set delete is intentionally absent; deletion lives under `セットの管理`;
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


## Hands-on Round 1 — frozen corrective direction

The first real YMM4 hands-on pass on PR #31 / Checkpoint #638 found the overall Phase 4 direction good enough to keep, but identified a bounded corrective pass that must finish before Behavior Preview begins.

### Density / floating-window principle

Template Placer is comfortable when floated near the active editing area and resized for the current task.

Therefore:

- do **not** optimize the compact Settings surface for minimum width or maximum information density;
- keep useful breathing room and pointer-safe empty space;
- allow the surface to use a wider working area when available;
- when the Tool is narrowed, move content **downward by wrapping** rather than hiding it horizontally;
- ordinary text should wrap automatically before it is clipped;
- control groups may wrap to the next row rather than forcing a horizontal scrollbar;
- the outer Settings surface must not require horizontal scrolling.

The goal is **alignment and predictable resizing**, not compression.

### Set shape moves to the bottom

`このセットの形` is duplicated by the placement panel's quick settings and is normally more convenient there.

Keep the full-Settings route as a secondary/fallback route, but move it to the bottom of the compact Settings flow so it no longer consumes prominent top space.

Do not remove the capability.

### Set management / copy grouping

The current top Set picker and Set-management block contain redundant and vertically expensive actions.

Freeze the following direction:

- the top Set picker row keeps Set selection and `＋`;
- remove the duplicate top-row `削除`;
- keep the destructive `削除` action in `セットの管理`;
- place ordinary Set management actions and `他のアイテムへコピー` in one coherent management area;
- at comfortable widths, management and copy controls may sit beside each other;
- at narrow widths, they should naturally wrap to another row instead of being compressed or clipped.

Do not reduce spacing merely to force a one-row layout.

### Column / list alignment

Hands-on found inconsistent left starts visually noisy.

Minimum alignment requirement:

- the content/list start of `演出と並び順` must align with the visible Template rows in `テンプレートをまとめて追加`.

Use a small number of intentional horizontal levels rather than making every control share one absolute X coordinate.

Preferred mental model:

- section header;
- normal section content;
- list/content lane;
- secondary-detail indentation.

Equivalent controls at the same hierarchy should share the same lane.

### White-space wheel continuity

Hands-on found a likely source of the long-standing "wheel catches / stops" feeling: visible white Settings areas exist where the outer Settings scroll does not respond even though the pointer is not intentionally operating an inner control.

This is now an accepted Phase 4 problem, not something to solve only by narrowing lists.

Required behavior:

- ordinary non-interactive white space inside the Settings surface should scroll the authoritative outer `SettingsScroll`;
- fixed/header areas that visually belong to the same Settings surface should not become unexplained wheel dead zones;
- empty margins/gutters may remain for comfort and should also be usable as outer-scroll space where practical;
- ComboBox, RangeBase and an inner ScrollViewer that can still move retain their intentional wheel ownership;
- when an inner scroll range is exhausted, the existing direction-aware handoff to the outer Settings scroll remains valid;
- do not synthesize key input or create a second scrolling model.

The exact implementation may broaden wheel admission above `SettingsScroll`, make ordinary white-space hit testing explicit, or use another bounded WPF route. Acceptance is based on observed behavior, not one prescribed mechanism.

### Existing source-list margins remain intentional

The left/right margins around the Template list are no longer justified only as a workaround for wheel routing.

Keep useful margins because they:

- reduce visual crowding;
- provide an easy pointer resting/scrolling lane;
- suit the floating/resizable Tool workflow.

The current approximately 16 DIP left / 6 DIP right values remain tunable hands-on values, not hard product constants.

### Preview gate after corrective pass

Checkpoint #638 is Native GREEN for the first Phase 4 candidate, but Phase 4 is **not** complete.

Behavior Preview / Checklist work remains blocked until:

1. the corrective layout/wheel pass above is implemented;
2. Native regression is GREEN again;
3. the user performs another real YMM4 hands-on pass;
4. no known high-frequency Settings friction remains that should reasonably be fixed before Preview.


## Final accepted hands-on / Release baseline

Final user hands-on accepted the compact Settings surface after the corrective passes.

Exact implementation/test/package source:

- `05285a7bae1eb59b4b588c565aa46e29bdea795c`
- Checkpoint #653 / `35884196081`: **1,822 Native assertions PASS**
- Release #654 / `35897405826`: **1,822 Native assertions PASS**
- `COMPACT_SETTINGS_P1=PASS` through `COMPACT_SETTINGS_P7=PASS`
- retained `PASS P1 P2 P3 P4 P5 P6 P7 P8 P9`
- exact distribution DLL SHA256: `648014c22af02f726fc95beb6282b177d94673632e21f108c53e057a80081875`
- verified v0.5.0 `.ymme` stable-root / source / provenance packaging: PASS
- Release artifact `10767621834` (`native-yymm4-release`)
- artifact ZIP SHA256: `bf217e5162f31de9ff6fc97d441cc08d3327f033a944ac5c61e35b22b78010d1`

Accepted product behavior includes:

- ordinary first-level Set capabilities remain visible;
- low-frequency/detail controls remain bounded disclosures;
- Set shape is secondary at the bottom of full compact Settings;
- duplicate top Set delete is removed;
- Set management and cross-Item copy form aligned peer blocks;
- the destination ComboBox is intentionally wide and equal-height with the copy action;
- equivalent lists and their action rows share consistent visual lanes;
- narrow widths reflow vertically instead of requiring horizontal Settings scrolling;
- useful whitespace remains for the floating/resizable Tool workflow;
- white Settings margins, footer and edges participate in outer scrolling;
- while the Settings tab is active, the surrounding gray Template Placer Tool surface also routes ordinary wheel input to the Settings scroller;
- ComboBox / RangeBase / independently scrollable inner surfaces retain their own intentional wheel ownership;
- whole-Tool Settings wheel routing is disabled outside the Settings tab.

Phase 4 is complete. Behavior Preview / Checklist may now begin against this accepted Settings surface.
