# Placement Quick Settings Workplan

Status: **P0-P2 COMPLETE / RELEASE #770 GREEN — P3 OWNER HANDS-ON NEXT**

## P0 — freeze the state boundary — COMPLETE

- reuse the existing placement-panel quick Popup;
- map current Set identity to the existing `IntentSettingsSession`;
- expose the exact current `IntentPaletteDraft`;
- extend automatic Settings-commit admission to the bounded quick-edit lifetime;
- keep presentation quick settings independent while sharing the Popup container;
- prove zero-write open/close and clean detach.

## P1 — minimal finite controls — COMPLETE

Implement only the accepted finite first-slice actions.

Suggested order:

1. start / center / end result alignment;
2. target-span / template duration;
3. one-layer up / down;
4. only then evaluate the next-same-character-start compound action.

Reuse `PlacementBehaviorPreview` from the same Draft.

Do not add numeric text boxes in P1.

## P2 — lifecycle and native placement proof — COMPLETE

Prove:

- Popup light-dismiss remains non-consuming;
- Set change / owner deactivation closes safely;
- quick edit commits before the next placement can run;
- actual tile placement uses the edited relation;
- native Undo/Redo remains exact;
- normal Settings and quick surface roundtrip to the same values.

Release #770 / run `36076134931` at exact tested source `9dae643cb9c29780783dde265d41dc4a90f01a62` passed **1,277 ASSERT PASS / 0 FAIL**, all `PLACEMENT_QUICK_SETTINGS_P0/P1/P2` gates, exact distribution-DLL smoke and verified packaging.

The implemented first slice contains:

- shared `IntentSettingsSession / IntentPaletteDraft`;
- same-Draft Behavior Preview;
- start / center / end result alignment;
- target-span / template duration;
- one-layer up / down;
- placement admission blocked while a quick edit is not yet committed;
- synchronous settle on Popup close;
- rebind after presentation-side Settings reconstruction.

No numeric quick inputs or neighbor compound recipe were added.

## P3 — owner hands-on correction — NEXT

Use the real Tool width and normal edit loop.

Remove any action that is slower to understand than opening `どう置く？`.

Only after P3 should additional quick actions be considered.
