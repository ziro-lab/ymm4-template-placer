# UI Micro Polish — corrective acceptance

Status: **FROZEN**

This replaces the rejected/changed parts of the first candidate acceptance.

## A. Generic layer

- A1 always-visible Generic layer appearance remains unchanged from the accepted Hands-on appearance.
- A2 click/type + Enter/Esc remain supported.
- A3 wheel over the number field changes one numeric step per notch and stays inside saved bounds.
- A4 wheel outside the number field keeps normal scrolling.
- A5 successful wheel stepping does not emit a routine bottom Status message for every step.
- A6 wheel failure/conflict remains visible.
- A7 explicit Enter/apply may emit the normal success confirmation.
- A8 direct application-level number-key entry is absent.
- A9 the palette shortcut router owns only explicit configured position shortcuts; no replacement digit interception route exists.

## B. Voice row height

- B1 all Voice rows continue to share one durable common height.
- B2 hovering a realized row's bottom resize band exposes vertical-resize affordance.
- B3 dragging the boundary of any realized row previews the common height for all realized rows.
- B4 the resize gesture does not also execute normal Voice row navigation.
- B5 preview stays within 32-96.
- B6 DragDelta writes zero settings and zero Timeline state.
- B7 release performs one protected durable save.
- B8 persistence failure restores the previous durable visual height.
- B9 top control shows the current common numeric height.
- B10 valid numeric entry + Enter commits the common height.
- B11 Esc restores the durable value.
- B12 invalid/out-of-range numeric input is never persisted.
- B13 no per-row height storage/model is added.
- B14 row virtualization remains enabled and AssignmentRow objects are not rebuilt.
- B15 Serif font size is unchanged.

## C. Fixed columns

- C1 saved FixedColumns remains the exact number of columns in Fixed mode.
- C2 slot index -> row/column mapping is unchanged by viewport resizing.
- C3 at widths above the minimum grid width, Fixed cells grow uniformly to use the available viewport width.
- C4 Fixed cells remain square so Circle tiles remain visually circular.
- C5 cells never shrink below 104x104; narrower viewports retain horizontal overflow/scrolling.
- C6 Auto mode retains the existing 104x104 / automatic-column behavior.
- C7 position shortcuts still execute the tile currently occupying the same physical slot after resize.

## D. quick-settings lifetime

- D1 quick settings remain usable for multiple internal actions without closing after each same-Set refresh.
- D2 switching to a different application deactivates the owner YMM4 Window and closes the popup.
- D3 the popup does not reopen automatically when YMM4 is activated again.
- D4 Tool hide/unload/DataContext replacement/different logical Set still close it.
- D5 popup close/open is settings-zero-write and Timeline-zero-write.
- D6 no polling/global activation hook is added.

## E. public-readiness cleanup

- E1 awkward `true == suppress ? ...` assignment is removed.
- E2 no-op `EndPanelQuickSettings()` and its call are removed.
- E3 bounded `ExpressionNavigationHost` compatibility catch remains fail-soft with explicit rationale.
- E4 no broad reflection expansion occurs.
- E5 no legacy/current architecture purge occurs in this pass.

## F. preserved safety

- F1 normal placement remains add-only.
- F2 Generic planner/collision semantics are unchanged.
- F3 PlacementPlan/native Undo are unchanged.
- F4 settings atomic/digest conflict protection is unchanged.
- F5 Template fidelity/association boundaries are unchanged.
- F6 no settings schema migration is introduced.
- F7 Tachie Preset PR #19 remains separate.

## Human gate

Owner Hands-on should specifically check:

- wheel feels the same but the bottom log is no longer spammy;
- no unexpected digit interception remains;
- grabbing any row boundary feels natural;
- numeric row-height entry is obvious and useful;
- Fixed columns visually fill wide panel sizes without changing shortcut positions;
- quick settings disappears immediately when switching to another application.
