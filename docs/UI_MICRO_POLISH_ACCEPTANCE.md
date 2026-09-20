# UI Micro Polish — acceptance

This acceptance set is intentionally compact. It protects current invariants rather than creating another permanently additive Round ladder.

## A. Generic inline layer editor

- A1 Generic context shows target layer and occupied-layer behavior continuously inside the existing two-line context-header footprint.
- A2 non-Generic context does not show Generic layer controls.
- A3 the old click-to-open Generic layer popup is no longer required for normal editing.
- A4 numeric text edit retains Enter=apply / Esc=restore semantics.
- A5 dirty/invalid Generic layer input blocks Generic tile placement and never falls back to the previously saved layer.
- A6 changing Set/context cannot apply a stale layer draft to the wrong Set.
- A7 Generic planner direction/bounds/collision semantics are unchanged.

## B. Generic layer wheel/direct input

- B1 wheel directly over the numeric layer field changes exactly one numeric step per wheel notch.
- B2 wheel results remain inside the current valid bounds and apply through the same Generic target save path.
- B3 wheel anywhere outside that numeric field keeps normal panel/Settings scrolling behavior.
- B4 modified wheel input is not globally swallowed.
- B5 if direct plain-digit entry is implemented, it is admitted only in active Generic placement with no editor/menu/modal ownership.
- B6 active position shortcut for the same digit wins over optional direct layer entry.
- B7 no second application-level keyboard hook is introduced.

## C. Panel quick settings

- C1 bottom-right control opens local panel quick settings and does not switch to the Settings tab.
- C2 opening/closing the flyout is settings-zero-write and Timeline-zero-write.
- C3 current-Set shape buttons affect all and only tiles in the current Set.
- C4 individual tile shape overrides remain available after Set-wide shape.
- C5 Auto/Fixed remains one global value across Set switches.
- C6 fixed column count remains one global value across Set switches.
- C7 position shortcut enable/assignments remain global and preserve physical-slot semantics.
- C8 quick settings do not expose structural Set applicability/relation configuration.
- C9 an invalid/incomplete full Settings draft prevents conflicting quick-settings persistence instead of being overwritten.
- C10 external settings conflict fails closed.
- C11 Set/context change closes or safely rebinds Set-specific flyout state.

## D. Voice row-height drag

- D1 row-height drag changes one common row height for every Voice row.
- D2 drag preview is bounded to 32-96.
- D3 DragDelta performs zero settings writes.
- D4 DragDelta rebuilds no AssignmentRow objects and mutates no Timeline content.
- D5 successful drag completion performs one protected persistence of the final height.
- D6 persistence failure restores the last saved visual height and reports the failure.
- D7 Serif font size remains unchanged; larger rows still permit wrapped multi-line Serif.
- D8 row virtualization/common-height behavior remains enabled; no per-row height model is added.

## E. Preserved boundaries

- E1 normal placement remains add-only.
- E2 Template identity/fidelity is unchanged.
- E3 PlacementPlan/native Undo path is unchanged.
- E4 Settings atomic/digest conflict protection is unchanged.
- E5 position shortcut exclusion while editing text/ComboBox/DataGrid remains intact.
- E6 UI polish introduces no new persisted schema fields.
- E7 Tachie Preset PR #19 remains separate from this feature.
- E8 documentation-only preparation does not download/build/launch YMM4.

## Human gate

Real hands-on should verify:

- Generic layer target can be changed repeatedly without opening anything;
- wheel on layer number feels predictable and does not steal ordinary scrolling;
- the two-line Generic header still feels compact at the normal narrow Tool width;
- quick settings are more useful than the previous Settings-tab jump;
- Set-wide shape/layout/shortcut edits feel immediate;
- full Settings tab still feels like the obvious place for structural configuration;
- row-height drag feels direct and does not jitter/rebuild rows;
- if direct digit entry is implemented, it never surprises the user while shortcuts or editors are active.
