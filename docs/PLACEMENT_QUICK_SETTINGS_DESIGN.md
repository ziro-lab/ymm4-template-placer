# Placement Quick Settings Design

Status: **ACTIVE DESIGN — NO PRODUCT IMPLEMENTATION YET**

## Goal

Placement Quick Settings exists to shorten one common loop:

> **Change a familiar placement result -> confirm it in Preview -> place the next tile.**

It is not a second Settings screen, not a saved recipe database and not a replacement for the current Set model.

The normal product flow remains:

```text
select target -> choose Set when needed -> choose tile
```

Quick Settings is used only when the current Set's placement relation needs a small correction before continuing.

## Product boundary

Quick Settings must edit the **same current Settings Draft** used by normal Settings.

Authoritative direction:

```text
current runtime Set
        ↓ exact Set identity
IntentSettingsSession
        ↓
IntentPaletteDraft
        ├─ existing Behavior Preview
        ├─ normal Settings controls
        └─ Placement Quick Settings controls
        ↓
existing validation / protected automatic persistence
```

Do not add:

- a `QuickPlacementDraft` that duplicates placement state;
- a second persisted recipe/profile schema;
- a second placement resolver;
- a second validation or persistence path;
- a custom undo model;
- direct Preview manipulation.

A small stateless UI projection or finite command object is acceptable when it only writes documented existing Draft fields.

## Surface

Reuse the existing placement-panel `⚙ 簡易設定` Popup rather than adding another floating window.

The Popup currently owns presentation/interaction quick settings. The placement quick surface should share that container while keeping responsibilities visually separate.

Preferred first candidate:

- **配置** — default for applicable Targeted Sets;
- **表示・操作** — existing Set shape / tile layout / position-shortcut controls.

This may be implemented as tabs or an equivalent bounded switch. Do not stack the full placement controls above all existing presentation controls in one long mini-Settings page.

For Generic Sets, the current direct layer target UI already serves the high-frequency placement case. The new Targeted placement quick surface is not required there; existing presentation quick settings remain available.

## First-slice quick vocabulary

The first slice intentionally exposes only **finite, valid, high-frequency completed choices**. It does not expose arbitrary numeric text.

### A. Time/alignment result

Candidate buttons:

- `開始から`
  - `Anchor = SelectedStart`
  - `Alignment = StartAtAnchor`
- `中央に合わせる`
  - `Anchor = SelectedCenter`
  - `Alignment = CenterAtAnchor`
- `終了に合わせる`
  - `Anchor = SelectedEnd`
  - `Alignment = EndAtAnchor`

These are result-oriented combinations, not new semantics.

Advanced anchors such as selection-range, pair-boundary and related-item anchors remain normal Settings in the first slice. Opening Quick Settings must never normalize an advanced current value.

### B. Duration result

Candidate buttons:

- `対象と同じ長さ`
  - `Duration = TargetSpan`
- `テンプレートの長さ`
  - `Duration = Template`
- `次の同キャラの開始まで` — candidate requiring bounded applicability proof
  - `Duration = UntilRelated`
  - `Neighbor = NextSameCharacter`
  - `NeighborEdge = Start`
  - `Alignment = StartAtAnchor`

The neighbor recipe must not silently rewrite fallback, gap limit, offsets or unrelated fields. If the current context cannot truthfully support the recipe, the control is unavailable rather than guessed.

Fixed-frame duration is deferred from the first quick slice because it requires free numeric input and invalid-draft handling.

### C. Relative layer result

Candidate buttons:

- `1段上`
  - `LayerMode = RelativeToTarget`
  - `Direction = Up`
  - `LayerOffset = 1`
- `1段下`
  - `LayerMode = RelativeToTarget`
  - `Direction = Down`
  - `LayerOffset = 1`

Absolute Layer, custom offsets, search bounds and other detailed collision controls remain normal Settings initially.

## Preview

The accepted `PlacementBehaviorPreview` is reused in the quick placement surface.

A quick action must update the same Draft and therefore the Preview immediately. Preview remains read-only.

The user-facing loop is:

```text
quick action
-> Preview changes
-> protected settings commit
-> next placement uses that saved rule
```

The visual result and the actual next placement must never refer to different relation states.

## Session / persistence rules

Quick placement editing participates in the existing Settings session transaction.

Required behavior:

1. opening the Popup performs zero Settings and Timeline writes;
2. resolve the exact currently displayed Set into the existing `IntentSettingsSession`;
3. quick controls write only existing `IntentPaletteDraft` fields;
4. `IntentSettings.Edited` feeds the existing protected automatic commit path;
5. a valid quick edit is committed before the next placement can use it;
6. if normal Settings already contains an incomplete/dirty conflicting draft, placement quick editing is disabled with a useful notice;
7. external settings conflicts retain the existing fail-closed behavior;
8. the existing session rollback remains authoritative for reverting Settings changes.

Do not bypass `SettingsEditTransaction` with a convenience save.

## Popup lifecycle

Retain the accepted light-dismiss behavior:

- clicking inside the Popup keeps it open;
- owner-window outside click closes it without swallowing the underlying YMM4 click;
- owner-window deactivation closes it;
- Set identity change closes it.

When placement quick editing is active, closing the Popup must synchronously settle any valid queued edit before an outside click is allowed to execute a placement using stale saved settings.

Because the first slice uses finite valid actions only, Quick Settings itself does not need a free-text invalid-input state.

## Placement admission

A placement must never execute using an older persisted relation while Quick Settings visibly shows a newer uncommitted relation.

Therefore either:

- the finite action is fully committed before the next tile command becomes executable; or
- placement remains disabled until the shared Settings commit completes.

Do not execute directly from the quick Draft as a shortcut around persistence.

## What remains in normal Settings

Keep these out of the first Quick Settings slice:

- target Item type/count and Character applicability;
- source/template membership and Set management;
- arbitrary selection-range / pair-boundary / related-anchor grammar;
- fixed duration numeric input;
- arbitrary relative offset and Absolute Layer numeric input;
- neighbor fallback and maximum-gap details;
- collision bounds;
- start/end frame offsets;
- per-tile overrides;
- presentation management beyond the already existing quick surface.

Quick Settings should stay obviously smaller than `どう置く？`, not become another copy of it.

## Exit

Design is ready for implementation when:

- the first finite action set is accepted as small enough;
- every action has an explicit existing-field write set;
- no action changes undocumented fields;
- the same Draft feeds Preview and persistence;
- the quick Popup lifecycle cannot race the next placement;
- Generic behavior remains intentionally separate;
- no second configuration model is introduced.
