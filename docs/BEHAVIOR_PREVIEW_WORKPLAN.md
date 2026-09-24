# Behavior Preview Workplan

Status: **P0 COMPLETE — P1 v2 REDESIGN ACTIVE**

## P0 — common meaning projection — COMPLETE

- add `PlacementBehaviorDescription`;
- route existing Summary through it;
- preserve existing visible Summary behavior;
- prove incomplete numeric Draft behavior;
- retain `BEHAVIOR_PREVIEW_P0=PASS`.

Evidence:

- Checkpoint #712: P0 GREEN;
- later #720/#724 also retain P0 GREEN.

## P1a — Preview v2 model

Replace the v1 label/card rendering assumptions with a simple diagram model derived from `PlacementBehaviorDescription`.

The model must express only visual relationships such as:

- target block(s);
- optional related Item block;
- placed block;
- qualitative horizontal start/end;
- qualitative width / end boundary;
- vertical target-relative relation;
- compact layer/fallback hints.

No real Timeline Frame or Layer calculation belongs here.

Required cases:

- target span;
- Template duration;
- fixed duration;
- start / center / end alignment;
- Until-related-start;
- Until-related-end;
- previous vs next related Item;
- target-relative Up / Down;
- Absolute Layer;
- pair boundary / selection range at a bounded first-pass level.

Generic full-preview generation is excluded unless a concrete need is found.

Gate: native proof of model mapping, zero mutation.

## P1b — Preview v2 visual

Replace the current v1 card layout with Timeline-like blocks.

Core examples:

```text
[配置アイテム]
[対象アイテム]
```

```text
[対象アイテム]
[配置アイテム]
```

```text
[対象アイテム]        [周辺アイテム]
[配置アイテム────────────]
```

```text
[対象アイテム]        [周辺アイテム────]
[配置アイテム────────────────]
```

The diagram should communicate the relationship before the user reads supporting text.

Do not implement exact scale, drag interaction or placement simulation.

Gate: Checkpoint GREEN.

## P2 — layout / hands-on

Hands-on should deliberately vary:

- start / center / end;
- TargetSpan / Template / Fixed;
- related Item start vs end;
- previous vs next related Item;
- target-relative Up vs Down;
- Absolute Layer;
- narrow vs normal Tool width.

Evaluate:

- instant comprehension;
- vertical space cost;
- whether Summary + Preview feels redundant;
- which simple cases should suppress the Preview;
- whether Generic should show only a layer hint or nothing.

Gate: user hands-on accepted + Release GREEN.

## P3 — checklist handoff

After Preview acceptance, freeze the visual/description vocabulary and start a separate checklist design:

```text
current Settings Draft
-> temporary checklist draft
-> preview / textual result
-> explicit apply
-> current Settings Draft
```

Checklist Cancel remains zero-change.

Undo/Redo shortcut work remains after Preview/Checklist; it is not a Preview prerequisite.

## Superseded P1 v1

The first card-based Preview is kept only as implementation history.

Its Native GREEN evidence proves read-only integration and shared projection safety, but the card layout itself is superseded because it did not visually communicate the actual placement relationship strongly enough.
