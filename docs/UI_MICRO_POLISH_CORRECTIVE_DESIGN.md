# UI Micro Polish — corrective design

Status: **FROZEN FOR IMPLEMENTATION**

This is the corrective design after the first real-user Hands-on pass of PR #20.

Source feedback: `docs/UI_MICRO_POLISH_HANDS_ON_FEEDBACK.md`.

The initial candidate remains useful evidence, but where this document conflicts with the first `UI_MICRO_POLISH_DESIGN.md`, this corrective design wins.

## Scope decisions

| Feedback | Decision |
| --- | --- |
| F1 Generic inline appearance | **KEEP** |
| F2 direct Generic digit entry | **REMOVE** |
| F3 wheel success logging | **KEEP wheel / silence routine success** |
| F4 Voice row-height UX | **REPLACE top grip with row-boundary drag + numeric entry** |
| F5 Fixed columns unused width | **RESPONSIVE FIXED GRID** |
| F6 stranded quick-settings Popup | **CLOSE ON OWNER WINDOW DEACTIVATE** |
| F7 public-readiness audit | **SMALL CLEANUP ONLY** |

No Tachie Preset or Placement Recipe work belongs in this corrective pass.

---

## 1. F2 — remove direct Generic digit entry

The direct number-key route is not worth additional input interception.

Real Hands-on showed ordinary number keys being consumed by YMM4's existing Serif/text route before the intended Template Placer behavior was useful.

The accepted Generic interaction becomes:

- layer field is always visible;
- click/type into the field;
- mouse wheel over the field changes ±1;
- Enter applies;
- Esc restores.

Remove the global/direct digit feature completely.

### Required removals

Remove product code whose only responsibility is direct number-key entry:

- Generic direct-digit arbitration from the palette shortcut route;
- `TryBeginGenericLayerNumberInput`;
- `IsPositionShortcutReserved` if no longer used;
- focus-forwarding used only by U4;
- U4-only proof and package/usage wording.

Restore the shortcut router to one job:

> execute explicitly configured position shortcuts.

Do not add a stronger key hook to rescue F2.

This is a deliberate product simplification, not a regression.

---

## 2. F3 — wheel is silent on routine success

Mouse wheel layer stepping remains accepted.

The problem is only high-frequency success noise in the bottom status area.

### Behavior

Wheel success:

- persist through the same protected Generic layer save path;
- update the visible numeric field;
- do **not** write the routine success message to bottom Status.

Explicit Enter/apply:

- may keep the existing success message.

Failure/conflict:

- remains visible through the existing error/Guard path.

### Implementation boundary

Keep one Generic apply implementation.

A small apply-origin/success-feedback parameter is acceptable.

Example conceptual shape:

```text
ApplyGenericLayerTarget(announceSuccess: true)   // Enter/explicit apply
ApplyGenericLayerTarget(announceSuccess: false)  // wheel
```

Do not duplicate planner/store logic.

---

## 3. F4 — row-boundary drag controls one common row height

The top standalone drag grip is removed.

The invariant remains:

> every Voice row uses the same global `ExpressionRowHeight`.

### 3.1 Direct manipulation

Any realized Voice row can act as the handle.

When the pointer is within a small bottom-edge band of a row:

- cursor becomes vertical resize;
- left press begins common-height resize;
- dragging vertically previews the same new height on **all rows**;
- release persists the final common height once;
- the gesture consumes the row click so it does not also navigate/select as an ordinary row click.

Recommended hit band: about 4 DIP at the row bottom.

Do not customize the complete DataGridRow ControlTemplate merely to add a visual Thumb.

Prefer lightweight row pointer events so the native/default theme, virtualization and cell controls remain intact.

### 3.2 Numeric entry

Keep a compact top readout, but make it editable:

```text
行の高さ [ 48 ] px
```

Rules:

- integer only;
- range 32-96;
- Enter commits;
- Esc restores the durable value;
- invalid/incomplete input is retained or rejected locally without persisting an invalid setting;
- successful numeric commit uses the same durable `ExpressionRowHeight` path as row-boundary drag.

The old discrete preset list and old top drag grip are removed.

### 3.3 Persistence

During boundary drag:

- visual preview only;
- zero settings writes;
- zero Timeline writes;
- no AssignmentRow rebuild.

On release:

- one protected save.

If persistence fails:

- restore the last durable common height;
- show the existing settings error.

Per-row heights remain explicitly out of scope.

---

## 4. F5 — Fixed mode fills width while keeping fixed slot geometry

Current Fixed mode uses 104x104 cells regardless of viewport width.

Correct it without changing column count or slot mapping.

### Fixed geometry

Let:

```text
columns = saved FixedColumns
minimumCell = 104 DIP
viewport = finite usable Palette ScrollViewer viewport width
cell = max(minimumCell, viewport / columns)
```

In **Fixed** mode:

- use exactly `columns` columns;
- every cell is square: `cell x cell`;
- when viewport is wider, tiles grow uniformly to fill the row width;
- when viewport is narrower than `columns * 104`, keep 104x104 minimum cells and horizontal overflow/scrolling;
- row/column slot index never changes merely because the window is resized.

Square scaling is deliberate so `丸` remains visually round instead of becoming a pill.

In **Auto** mode:

- retain current 104x104 behavior and automatic column-count changes.

No new saved setting is required.

### Shortcut invariant

Physical-slot position shortcuts retain exactly the current semantics.

Resizing changes cell size only, never slot ordering.

---

## 5. F6 — quick settings belongs to the owning active YMM4 window

The quick-settings Popup must never remain visibly stranded over another application.

Keep `StaysOpen=True` if needed for stable internal interaction, but explicitly bind its lifetime to the owner window.

### Close conditions

Close the popup when:

- owning YMM4 Window deactivates;
- Tool unloads/hides;
- DataContext is replaced;
- logical Set changes to a different Set.

Do not reopen automatically when YMM4 activates again.

Opening/closing remains:

- settings-zero-write;
- Timeline-zero-write.

### Implementation route

Use the actual WPF owner Window lifecycle from the placement panel:

- subscribe to owner `Window.Deactivated` while loaded;
- unsubscribe on unload/owner replacement.

No polling, OS hooks or application-global activation monitor.

---

## 6. F7 — public-readiness cleanup

This is deliberately small.

### 6.1 Simplify awkward boolean assignment

Replace:

```csharp
e.Handled = true == suppress ? true : e.Handled;
```

with the straightforward form:

```csharp
if (suppress) e.Handled = true;
```

### 6.2 Remove no-op lifecycle method

`EndPanelQuickSettings()` currently has no state transition.

Remove the method and its call.

Do not keep an empty method for speculative symmetry.

### 6.3 Keep bounded catch-all failure containment, document why

Do not broadly rewrite `ExpressionNavigationHost`.

Its outer compatibility probe is allowed to fail-soft because YMM4 host visual/API differences must not crash the plugin.

Add/retain a concise boundary comment explaining that the catch-all is intentional compatibility containment.

The same principle applies to exact public-method binding.

Do not add user-visible noise for optional Preview/viewport discovery failures.

### 6.4 No legacy purge

Do not remove old Library/Palette/Selection/QuickDrop/Preset compatibility families in this pass.

`CURRENT_ARCHITECTURE.md` and `LEGACY_COMPATIBILITY_MAP.md` remain the maintenance boundary.

---

## 7. Preserved architecture

This corrective pass must not change:

- settings schema;
- placement Context semantics;
- Template identity/fidelity;
- PlacementPlan;
- native Undo;
- add-only normal placement;
- exact managed-expression replacement;
- Excel behavior;
- Tachie Preset scope.

No new application-level input hook is introduced.

In fact, removing U4 reduces input-route complexity.

---

## 8. Validation strategy

Ordinary corrective implementation uses Focused where only product source changes.

Changes to native proofs/tests trigger Checkpoint under the current tier policy.

Final corrective candidate must prove:

- U1 Generic core still works;
- direct digit U4 is absent;
- wheel success is quiet while failures remain visible;
- row-boundary drag + numeric row height are common/global and one-save;
- Fixed resizing preserves slot mapping and fills wide viewport;
- Popup closes on real owner deactivation;
- cleanup changes do not alter behavior;
- all historical Checkpoint evidence remains green.

Real owner Hands-on is still required before merge.
