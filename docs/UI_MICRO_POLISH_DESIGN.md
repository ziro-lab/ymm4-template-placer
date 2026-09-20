# UI Micro Polish — design

## Purpose

This pass removes the remaining high-frequency interaction friction **without changing placement semantics**.

Priority order:

1. Generic layer controls stay visible and become faster to edit.
2. Bottom-right Settings navigation becomes a placement-panel quick-settings flyout.
3. Voice row height becomes directly draggable.
4. Plain-number direct entry for Generic layer is a stretch goal only if it fits the existing bounded palette input route cleanly.

No Tachie Preset implementation belongs in this pass. Draft PR #19 remains paused.

## Non-negotiable boundaries

This is a UI/presentation pass.

Do not change:

- placement Context matching;
- relation/Anchor semantics;
- Generic layer planner meaning;
- collision/preflight rules;
- add-only normal placement;
- exact managed-expression replacement rules;
- settings schema unless absolutely unavoidable;
- PlacementPlan;
- native Undo;
- Template fidelity;
- Excel semantics.

No new generic input hook, scripting layer or alternate settings store.

## A. Generic layer controls are always visible

Current UI uses a `レイヤー N ▾` ToggleButton + Popup.

Replace that extra click with an inline compact editor occupying the **right side of the existing two-line Generic context header**.

Desired composition:

```text
汎用・時間配置                    レイヤー [ 8 ]
再生位置にテンプレートの長さで配置   [上を探す ▼]
```

Exact spacing may respond to width. Semantic layout is fixed:

- title/detail remain left;
- layer target + occupied-layer behavior remain right;
- the controls stay inside the current header footprint;
- no new full-width row above the tiles;
- non-Generic contexts do not show these controls.

The existing `GenericLayerTargetDraft`, apply/reset commands, bounds and planner remain authoritative.

A compact presentation control may be added, but it must not create a second backend.

### A1. Numeric typing

Clicking the numeric field keeps current draft behavior:

- edit text;
- Enter applies;
- Esc restores saved value;
- invalid/incomplete text never becomes placement state;
- Generic tile execution remains blocked while the draft is dirty/invalid.

### A2. Mouse wheel

When the pointer is directly over the Generic numeric layer field:

- wheel up = numeric +1;
- wheel down = numeric -1;
- clamp/reject at the existing valid bounds;
- no modifier-assisted behavior;
- each complete wheel result applies through the existing protected Generic target path;
- only the numeric field consumes the wheel;
- wheel elsewhere keeps normal outer/inner scrolling.

Do not interpret wheel direction as YMM4 semantic “上/下”; it changes the numeric layer value conventionally.

### A3. Direct number entry from the placement panel — stretch goal

Desired interaction:

```text
Generic context active
-> press 1
-> layer editor starts a fresh "1" draft
-> press 2
-> draft is "12"
-> Enter applies
-> Esc restores
```

This is only acceptable if implemented through the **existing bounded palette application-input lifetime**.

Admission rules:

- active task is placement/palette;
- current context is Generic;
- Tool is visible/active under the same host admission as position shortcuts;
- no modal/menu mode;
- no editable TextBox/ComboBox/DataGrid/custom editor owns keyboard input;
- no Ctrl/Alt/Shift modifier;
- current Generic target exists;
- if the exact digit key is already an active position shortcut, the shortcut wins.

Do not add a second independent InputManager/global keyboard hook.

The first admitted digit replaces the current target text rather than appending to the saved number. Subsequent digits go to the focused editor.

If this route requires fragile focus restoration or duplicate keyboard ownership, defer it. Always-visible controls + wheel are the required improvement.

## B. Bottom-right button becomes panel quick settings

Current bottom-right `⚙ 設定` only jumps to the Settings tab, which already has a direct tab.

Replace that duplicate navigation with a local flyout/popup.

Opening the flyout:

- does not switch tabs;
- does not create/open the full Intent Settings session merely for navigation;
- does not mutate Timeline;
- stays local to the placement panel.

### B1. Quick-settings content

Keep this intentionally small.

#### Current Set

Set-wide tile shape:

- 角丸
- 四角
- 丸

This means **all tiles in the currently visible Set**, not every Set in the plugin.

Reuse the existing Set-wide shape behavior and protected save path.

#### Global placement-panel presentation

Expose:

- Auto / Fixed;
- Fixed column count;
- position shortcuts enabled/disabled;
- position shortcut assignments.

These values stay global across Sets.

Do not move structural Set semantics into the flyout.

### B2. Not in quick settings

Keep in the full Settings tab:

- Set creation/deletion/duplication/order;
- applicability/type/count/Character conditions;
- placement Anchor/relation;
- Neighbor/fallback semantics;
- detailed layer bounds;
- template membership management;
- expression-candidate membership;
- expression viewport-follow setting;
- other low-frequency structural configuration.

The distinction is:

```text
placement-panel quick settings = how this panel looks/operates
full Settings tab              = what a Set means / how placement is defined
```

### B3. Save/conflict behavior

Quick settings must use the existing protected settings model.

If the full Settings session currently has an invalid/incomplete/pending draft, quick settings must not silently overwrite it.

Prefer reuse of current presentation draft/validation primitives where possible. Do not duplicate the entire IntentSettingsSession.

Opening/closing the flyout must never itself write settings.

If Set/context changes while a current-Set shape action is being shown, close/rebind the flyout rather than applying to a stale Set.

## C. Voice row height becomes directly draggable

Current UI uses a discrete row-height ComboBox.

The product invariant remains:

> all Voice rows share one bounded global height.

Do not introduce per-row height.

Replace or supplement the coarse selector with a compact **global resize grip** near the Voice grid.

Desired user model:

```text
grab row-height grip
-> drag vertically
-> all realized Voice rows resize live
-> release
-> final common height is saved
```

Range remains:

- minimum 32
- maximum 96
- integer pixels

### C1. Persistence discipline

Do not persist on every DragDelta.

During drag:

- keep a transient preview height in view/root state;
- update DataGrid RowHeight only;
- do not rebuild `Rows`;
- do not mutate Timeline;
- do not write settings.

On successful drag completion:

- save the final bounded height once through the existing protected settings store;
- publish that value as the durable global row height.

If persistence fails:

- report the existing settings error;
- return the visual height to the last saved value.

A click/keyboard-accessible numeric readout may remain, but arbitrary per-row resize handles are not required.

## D. Shared UI rules

### One authoritative backend

Visual shortcuts may multiply; semantic save/placement routes may not.

- Generic inline editor -> existing Generic draft/commands.
- Set shape -> existing protected Set-wide shape path.
- layout/columns/shortcuts -> existing Presentation settings model.
- row height -> existing Presentation.ExpressionRowHeight setting.

### Popup lifetime

The quick-settings flyout is presentation-local.

Close it on:

- DataContext replacement;
- Tool unload;
- current Set/context identity change when Set-specific controls would become stale.

Do not use popup state as product/domain state.

### Narrow width

Pinned minimum Tool width remains 360.

At narrow width:

- context title/detail may trim;
- layer editor remains usable;
- quick flyout may scroll internally;
- no horizontal growth should make the Tool unusable.

## E. Validation discipline

This pass must not recreate additive test bloat.

Use targeted UI/native proofs for the new interactions, then consolidate into stable invariants where possible.

Ordinary implementation iterations use Focused validation.

Run Checkpoint after the UI pass is feature-complete or if tests/workflow/fixtures change materially.

Release only after real hands-on acceptance.

## F. Explicit non-goals

This pass does not implement:

- Tachie Presets;
- Template Pivot / Absolute Layer / Composite Steps;
- new placement Context/conditions;
- new settings schema;
- new Tile appearance metadata;
- global all-Set shape inheritance;
- per-row Voice heights;
- new global keyboard hooks;
- mouse-coordinate layer inference;
- a redesign of Settings tab.
