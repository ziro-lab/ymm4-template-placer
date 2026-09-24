# Placement Quick Settings Workplan

Status: **DESIGN / IMPLEMENTATION NOT STARTED**

## P0 — freeze the state boundary

- reuse the existing placement-panel quick Popup;
- map current Set identity to the existing `IntentSettingsSession`;
- expose the exact current `IntentPaletteDraft`;
- extend automatic Settings-commit admission to the bounded quick-edit lifetime;
- keep presentation quick settings independent while sharing the Popup container;
- prove zero-write open/close and clean detach.

## P1 — minimal finite controls

Implement only the accepted finite first-slice actions.

Suggested order:

1. start / center / end result alignment;
2. target-span / template duration;
3. one-layer up / down;
4. only then evaluate the next-same-character-start compound action.

Reuse `PlacementBehaviorPreview` from the same Draft.

Do not add numeric text boxes in P1.

## P2 — lifecycle and native placement proof

Prove:

- Popup light-dismiss remains non-consuming;
- Set change / owner deactivation closes safely;
- quick edit commits before the next placement can run;
- actual tile placement uses the edited relation;
- native Undo/Redo remains exact;
- normal Settings and quick surface roundtrip to the same values.

## P3 — hands-on correction

Use the real Tool width and normal edit loop.

Remove any action that is slower to understand than opening `どう置く？`.

Only after P3 should additional quick actions be considered.
