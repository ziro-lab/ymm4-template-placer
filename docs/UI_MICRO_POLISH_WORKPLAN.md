# UI Micro Polish — workplan

Status: **U1-U4 DONE / U5 HANDS-ON PREPARATION**

Base: accepted main after PR #16.

`7dc30dcb887a0d57f3079d72ffe89dc62bedd9fa`

No Tachie Preset product code is included.

## U0 — preparation

Documentation only:

- current architecture authority;
- UI design;
- compact acceptance set;
- implementation prep;
- backlog priority.

No YMM4 run is required for docs-only prep.

## U1 — Generic inline layer controls — DONE

Primary files:

- `IntentPalettePanel.xaml[.cs]`
- `GenericLayerTargetPanel.xaml[.cs]` or a compact presentation-only sibling
- existing Generic layer draft/commands

Implement:

1. remove normal dependency on `GenericLayerButton + Popup`;
2. place target + occupied behavior in the right side of the existing context header;
3. preserve title/detail on the left;
4. keep current Enter/Esc/draft admission;
5. add field-local wheel ±1 with immediate existing-path apply;
6. preserve outer scrolling everywhere else.

Do not change Generic planner/backend.

Validation: Focused.

## U2 — panel quick settings flyout — DONE

Primary files:

- `IntentPalettePanel.xaml[.cs]`
- new small `PanelQuickSettingsPanel.xaml[.cs]` if useful
- `IntentSetAppearance.cs`
- `PalettePresentationSettings.cs`
- `PalettePresentationDraft.cs`
- position-shortcut UI/commands

Implement:

1. change bottom-right Settings navigation into a local ToggleButton/flyout;
2. current-Set shape controls;
3. global Auto/Fixed;
4. fixed columns;
5. shortcut enable;
6. shortcut assignment editing;
7. block/conflict cleanly when full Settings draft is invalid/pending;
8. close/rebind on stale Set/context.

Do not clone the full Settings tab into this flyout.

Prefer reuse of `PalettePresentationDraft`/validation rather than a second presentation model.

Validation: Focused.

## U3 — global Voice row-height drag — DONE

Primary files:

- `PlacerView.xaml[.cs]`
- `CompactPresentation.cs`
- possibly one tiny resize-state helper

Implement:

1. replace/supplement discrete row-height picker with a global resize grip;
2. transient preview height while dragging;
3. clamp 32-96;
4. update DataGrid visual height live;
5. zero settings writes during DragDelta;
6. persist once on completion using the existing row-height setting/store;
7. restore saved visual height on persistence failure.

Do not add per-row sizing.

Validation: Focused.

## U4 — direct numeric layer typing — DONE

Only after U1-U3 are green.

Reuse the existing palette application-input lifetime/admission.

Do not add a second InputManager hook.

Priority:

1. active position shortcut exact match;
2. otherwise unmodified digit may begin Generic layer entry when no editor/menu/modal owns input.

If focus restoration or event routing requires brittle special cases, mark FUTURE and stop. Always-visible editor + wheel already satisfies the required UX improvement.

Validation: Focused if implemented.

## U5 — completion — HANDS-ON PENDING

When U1-U3 (and optional U4 if retained) are green:

1. run Checkpoint;
2. update `docs/USAGE.md` only after the actual UI is native-green;
3. prepare a hands-on package only if useful for the owner pass;
4. keep this UI PR independent from Tachie Preset PR #19;
5. after hands-on acceptance, merge UI to main;
6. rebase/refresh PR #19 from the new accepted main before Tachie Preset implementation.

No Release/package gate is needed before hands-on unless the owner requests a distributable. Final promotion should use Release.
