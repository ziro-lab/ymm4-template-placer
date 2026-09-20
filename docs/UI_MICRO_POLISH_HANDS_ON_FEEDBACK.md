# UI Micro Polish — Hands-on feedback

Status: **COLLECTING — do not implement yet**

This file records owner hands-on feedback for PR #20 before any corrective implementation is chosen.

Candidate tested:

- source HEAD: `e58d267286399d16c1ffe19bbd7411af58039f55`
- Release run #262: `35521238124`
- verified Hands-on package: `Ymm4TemplatePlacer-v0.4.2-UI-Micro-Polish-HandsOn.ymme`

## F1 — Generic inline appearance

Result: **GOOD**

The always-visible Generic layer presentation itself is acceptable.

No redesign requested for the visual composition at this point.

## F2 — direct Generic numeric typing is not reaching the layer editor

Result: **PROBLEM / feasibility review needed**

Observed hands-on behavior:

- Generic placement is active;
- typing number keys does not begin layer-number input;
- input appears to be consumed by YMM4's character/Serif editing route instead.

Owner direction:

- investigate only after feedback collection is complete;
- if reliably taking precedence over YMM4's existing text/Serif route requires invasive or fragile input ownership, **omit this feature** rather than adding a complicated interception layer;
- always-visible numeric field + mouse wheel already provide the required core improvement.

Do not weaken the existing editor/menu/modal input-exclusion rules merely to preserve U4.

## F3 — Generic layer mouse wheel is useful, but status logging is too noisy

Result: **BEHAVIOR GOOD / FEEDBACK TOO LOUD**

Observed hands-on behavior:

- mouse wheel layer stepping feels good;
- every wheel step writes a bottom status message such as the layer-change confirmation;
- this is excessive for a high-frequency micro-adjustment.

Desired direction:

- keep the wheel behavior;
- suppress or substantially quiet the routine success status for wheel stepping;
- errors/conflicts should remain visible;
- explicit Enter/apply may keep a normal confirmation if useful.

Exact feedback policy is not frozen yet.

## F4 — current Voice row-height grip is hard to use

Result: **CURRENT U3 UX NOT ACCEPTED**

The current top-of-workspace drag grip is not the desired interaction.

Desired user model:

- grab/drag a row boundary or one of the visible Voice rows;
- dragging any such row-height handle changes the **common row height for the entire list**;
- do not create per-row height state.

Additionally, show the current common height as a value and allow direct numeric entry.

Likely desired surface:

```text
row boundary drag -> all Voice rows resize
current height: [ 48 ] px
```

Requirements that remain from U3:

- common height only;
- bounded 32-96;
- live visual preview;
- no settings write on every drag delta;
- persist once after completing a valid edit/drag;
- no AssignmentRow rebuild;
- no Timeline mutation;
- Serif font size unchanged.

The exact row-boundary/resize-handle implementation is not frozen yet.


## F5 — Fixed columns leave unused horizontal space on wide windows

Result: **POLISH CANDIDATE / likely low-cost**

Observed hands-on behavior:

- Fixed column count correctly preserves the number of columns;
- each tile/cell remains approximately 104 DIP wide;
- when the Tool/window becomes wider than `FixedColumns * 104`, the remaining horizontal area is visibly unused.

Implementation inspection confirms the current `PaletteTilePanel` intentionally uses fixed 104-DIP cells in both Auto and Fixed modes.

Desired direction:

- keep the **column count fixed**;
- keep slot-to-row/column mapping fixed;
- when the viewport is wider than the minimum fixed grid width, distribute the available width evenly across the fixed columns;
- when the viewport is narrower than the minimum comfortable tile width, keep the current horizontal-overflow behavior rather than shrinking tiles too far.

Candidate geometry:

```text
minimum cell width = 104
Fixed:
    cell width = max(104, usable viewport width / fixed columns)
Auto:
    current 104-DIP behavior remains
```

This should improve visual fill without changing shortcut slot semantics, tile order, or the saved FixedColumns value.

Exact max-width/capping behavior is not frozen yet.

## F6 — quick-settings Popup can remain visually stranded when YMM4 loses foreground

Result: **BUG / likely low-cost lifetime fix**

Observed hands-on behavior:

- open `⚙ 簡易設定`;
- switch to another application so YMM4 is in the background;
- the quick-settings popup may remain visible by itself even though the YMM4 window is no longer foreground.

Implementation inspection:

- the quick-settings surface is a WPF `Popup`;
- current XAML uses `StaysOpen="True"`;
- current close conditions cover Tool unload/visibility, DataContext changes and logical Set changes;
- losing foreground/window activation is not currently part of the popup lifetime.

Desired direction:

- close quick settings when the owning YMM4/Tool window deactivates;
- do not leave the popup visually floating above another application;
- reopening YMM4 should not automatically reopen the old popup;
- opening/closing remains settings-zero-write and Timeline-zero-write.

Likely implementation direction is to bind popup lifetime to the actual owner Window/Tool activation lifecycle rather than adding polling or global hooks.


## Decision discipline

Do not patch F2-F6 individually yet.

After additional hands-on feedback is collected:

1. group issues by root cause;
2. decide which behaviors are retained/changed/omitted;
3. update the UI design/acceptance in one pass;
4. then implement the smallest coherent correction set.

In particular, do not spend architecture complexity to rescue direct-number typing if YMM4 input ownership makes it disproportionately costly.
