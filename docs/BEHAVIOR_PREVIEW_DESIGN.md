# Behavior Preview Design

Status: **ACTIVE — P0 COMPLETE / P1 v2 REDESIGN ACTIVE**

## Goal

Behavior Preview exists to answer one question inside Settings:

> **With this combination of settings, what will the placed item look like relative to the target and any related item?**

It is not another textual explanation panel and not a Timeline simulator.

The Preview must communicate the **resulting placement relationship by shape and position**, before the user actually places anything.

The phase remains intentionally split:

1. **Behavior Preview** first: read-only visual explanation of the current Settings Draft;
2. **“What I want” checklist** afterward: an editing projection that writes back to the same Draft.

Undo/Redo shortcut work remains after Preview/Checklist; it is not a Preview prerequisite.

## Core architecture

```text
current Settings Draft
        ↓
PlacementBehaviorDescription
        ├─ existing text: 「このセットの動き」
        └─ BehaviorPreviewModel
                  ↓
          simple placement diagram

current Settings / source + live Timeline context
        ↓
existing resolver / geometry / PlacementPlan
        ↓
native Undo
```

`PlacementBehaviorDescription` remains the shared read-only meaning projection.

`BehaviorPreviewModel` is a second read-only projection for **diagram geometry only**. It may choose simple normalized visual positions/sizes, but it must not:

- inspect live Timeline occupancy;
- resolve a real neighbor instance;
- choose the actual destination layer;
- materialize a source;
- calculate real placement Frames;
- mutate Settings or Timeline;
- duplicate `IntentRelationResolver`, `BundleLayerPlanner` or `PlacementPlan`.

The Preview shows the **configured relationship**, not the result of a dry-run placement.

## P0 — common description projection — COMPLETE

The natural-language Summary is already routed through one typed projection shared by Targeted and Generic Settings Drafts.

That projection remains useful and is not reverted.

It carries the current finite vocabulary:

- target Item types / Character restriction;
- anchor;
- start / center / end alignment;
- duration;
- neighbor + edge;
- fallback;
- offsets / bounded numeric details;
- relative vs. absolute layer;
- direction and range.

Incomplete numeric Draft text remains projectable. Parsed values may be nullable; the Preview must never invent a replacement value.

## P1 v1 hands-on finding

The first native-green Preview candidate used labeled cards such as “基準” and “置く範囲”.

That candidate was technically valid but **did not satisfy the UX goal**.

Hands-on finding:

> A Preview that merely restates settings in labeled cards still requires the user to read and mentally reconstruct the placement. The Preview must instead look like the actual placement relationship.

Therefore the v1 card layout is superseded by the v2 diagram specification below.

## P1 v2 — placement-result schematic

### Primary rule

Use simple blocks that visually resemble Timeline items.

The three conceptual blocks are:

- **対象アイテム** — the selected/target Item or selected range;
- **周辺アイテム** — shown only when the configured relation uses a Neighbor;
- **配置アイテム** — the item/result that would be placed.

The Preview is successful when a user can glance at the blocks and understand:

1. where the placed item begins;
2. where it ends / what determines its length;
3. whether it is above or below the target in layer terms;
4. whether a related item determines the result.

### Horizontal axis = time / length relationship

The horizontal position and width of blocks communicate **where placement starts and where it ends**.

The Preview must distinguish at least these configured relationships.

#### Target span

```text
[対象アイテム────────]
[配置アイテム────────]
```

The identical visual span communicates “対象と同じ長さ”.

#### Independent Template / fixed duration

```text
[対象アイテム────────────]
[配置アイテム────]
```

The exact pixel ratio is illustrative, not a real Frame scale.

A small label may distinguish:

- `テンプレート長`;
- `固定 48f`;
- `長さ未確定` while the Draft is incomplete.

#### Until related Item start

```text
[対象アイテム]        [周辺アイテム]
[配置アイテム────────────]
```

The placed block terminates at the **head/start** of the related block.

#### Until related Item end

```text
[対象アイテム]        [周辺アイテム────]
[配置アイテム────────────────]
```

The placed block terminates at the **end** of the related block.

This distinction is mandatory. “周辺アイテムまで” alone is not enough.

### Alignment = placed-block horizontal position

For independent-duration placement, start/center/end alignment must be visible through the placed block position.

Start:

```text
[対象アイテム────────]
[配置アイテム────]
```

Center:

```text
[対象アイテム────────]
    [配置アイテム────]
```

End:

```text
[対象アイテム────────]
        [配置アイテム────]
```

The diagram need not be mathematically to scale. It must preserve the qualitative relation.

### Related Item before the target

Previous-neighbor configurations may mirror the visual order.

```text
[周辺アイテム]        [対象アイテム]
        [配置アイテム────────]
```

The purpose is to show which related Item supplies the configured boundary.

### Pair boundary / selection range

These may use the same compact diagram vocabulary.

Pair boundary example:

```text
[対象1]│[対象2]
       [配置アイテム────]
```

Selection-range example:

```text
[対象1][対象2]
[ 選択範囲   ]
[配置アイテム────]
```

The first implementation only needs enough distinction to communicate the configured anchor. It does not need a full Timeline renderer.

## Vertical axis = layer relationship

Vertical ordering communicates relative layer placement.

Target-relative up:

```text
[配置アイテム]
[対象アイテム]
```

Target-relative down:

```text
[対象アイテム]
[配置アイテム]
```

When a related Item is visible, keep its time-axis position while preserving the placed-vs-target vertical relation.

A small hint may supplement the diagram:

- `対象より1レイヤー上`;
- `対象より2レイヤー下`;
- `Layer 42`;
- `塞がっていれば上へ`.

The hint is secondary. The block position is primary.

### Absolute layer

Absolute Layer cannot always be expressed relative to the target without implying a false vertical relation.

For absolute placement, a compact label such as `Layer 42` is sufficient; do not fabricate a target-relative vertical distance.

## Neighbor fallback

Fallback is supplementary and appears only when Neighbor behavior is relevant.

Examples:

- `周辺がない場合: 配置しない`
- `周辺がない場合: 対象の終了まで`
- `周辺がない場合: 対象と同じ長さ`
- `周辺がない場合: 固定 48f`

Fallback should not dominate the diagram.

## Generic Sets

Generic placement is **not a primary Behavior Preview target**.

Reason:

- Generic time placement always begins at the current playhead;
- its duration is fundamentally source/template-owned rather than a configurable placement-length relationship;
- forcing a diagram adds visual noise without answering a confusing Settings-combination question.

Default direction:

- do not show the full Targeted-style Preview for Generic Sets;
- where useful, show only a small placement-layer result such as:
  - `Layer 8`;
  - `塞がっていれば上を探す`.

Generic may gain a richer diagram only if later hands-on evidence shows a concrete comprehension problem.

## What the Preview must NOT show

Do not add:

- exact real Timeline Frame numbers;
- scale-accurate timing simulation;
- current live occupancy;
- the actual layer found after collision search;
- Template thumbnails;
- separate geometry diagrams for Template vs TachiePreset;
- Preview drag/edit gestures;
- placement execution;
- Undo/Redo controls;
- decorative information that does not help predict placement.

## Source behavior

Template and registered Tachie Preset sources share the same Set-owned Preview when their placement semantics are shared.

Source kind is not a reason to fork the diagram.

Unregistered direct Tachie Preset compatibility may remain outside this common Preview until that compatibility path is intentionally redesigned.

## Draft behavior

Preview reads the **currently edited Settings Draft**, including uncommitted valid edits.

Changes to anchor, duration, neighbor edge, alignment or layer relation should update the diagram immediately.

For incomplete/invalid numeric text:

- keep the editor text intact;
- do not persist or normalize it;
- show a bounded `未確定` / visually neutral unknown state where needed;
- never silently substitute the previous persisted number.

## Product role

Behavior Preview is not valuable because it is always visible.

It is valuable when a combination of Settings is difficult to imagine.

Therefore:

> **Do not force a diagram where the configured result is already obvious. Show only the visual information that materially helps the user predict the actual placement.**

## Current evidence

P0 is still accepted.

The v1 Preview implementation was Native GREEN through Checkpoint #720 and Release #724, proving the shared projection and read-only integration are sound. The **visual layout itself is superseded by this v2 design based on hands-on feedback**.
