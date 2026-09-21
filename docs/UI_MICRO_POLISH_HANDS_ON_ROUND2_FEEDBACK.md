# UI Micro Polish — owner Hands-on Round 2 feedback

Status: **CAPTURED**

Source: owner Hands-on of the corrective Release candidate from run #305 / source `1b8a88255da2e28289996b4d99470a195d6ec6a4`.

This document records the product feedback before any second-round corrective implementation.

## F8 — quick settings should light-dismiss inside YMM4

Observed:

- `⚙ 簡易設定` stays open when another part of YMM4 is clicked.
- It closes only when the quick-settings toggle is pressed again or when the owning Window deactivates.

Wanted:

- interacting inside quick settings keeps it open;
- clicking elsewhere in the owning YMM4 Window closes it;
- switching to another application still closes it;
- reactivating YMM4 does not reopen it.

The existing owner-`Window.Deactivated` C4 behavior remains valid but is not sufficient by itself.

## F9 — Sets should belong to one Item type

Observed:

- the current Settings model can expose all non-Generic Sets together through `ShowAllSets`;
- a current `IntentPalette` may target multiple Item types through `Target.ItemTypeKeys` / `ExactMixedTypes`.

Wanted mental model:

```text
Item type
  -> this Item type's Sets
      -> tiles/templates
```

Examples:

```text
ボイス
  -> 表情
  -> リアクション

テキスト
  -> 字幕強調
  -> 注意書き

図形
  -> 枠
  -> 強調
```

The normal Settings surface should never need a global "all targeted Sets" list as the primary management view.

## F10 — reuse by snapshot copy, not shared ownership

Wanted:

- a Set belongs to one Item type;
- if the same setup is useful for another Item type, use an explicit "copy to another Item type" operation;
- the copy is a snapshot at that moment;
- after copying, source and destination Sets are independent;
- later edits to either Set never propagate to the other.

This replaces implicit cross-Item sharing with explicit duplication.

## Clarification of terminology

Owner feedback used the word "preset" conversationally.

The current normal product entity is `Set` / `IntentPalette`. This corrective pass changes that current Set model only.

Legacy `SelectionPreset` and `ExpressionPreset` compatibility structures are not the target of F9/F10 and must not be broadly refactored in this pass.

## Product direction exposed by this Hands-on

The desired normal model is now:

```text
Generic Sets: separate existing Generic model

Targeted Sets:
Item type owns Set
-> Set owns relation/conditions/entries
-> explicit copy creates a new independent Set under another Item type
```

The core placement safety boundary is unchanged.
