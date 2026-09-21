# UI Micro Polish — corrective workplan

Status: **READY FOR IMPLEMENTATION**

Base branch: `work/ui-micro-polish-prep`

Hands-on feedback source: `UI_MICRO_POLISH_HANDS_ON_FEEDBACK.md`

Corrective authority:

1. `UI_MICRO_POLISH_CORRECTIVE_DESIGN.md`
2. `UI_MICRO_POLISH_CORRECTIVE_ACCEPTANCE.md`
3. this workplan

## C0 — remove rejected U4 + cleanup trivial code

Implement together:

- remove direct Generic number-key route;
- restore shortcut router to explicit position shortcuts only;
- remove U4-only focus-forwarding/proofs/docs/package wording;
- simplify awkward `e.Handled` assignment;
- remove no-op `EndPanelQuickSettings()`;
- add concise intentional fail-soft comment to bounded navigation compatibility catches.

This step should reduce code/input complexity.

Validation: Focused for source-only cleanup; Checkpoint once proofs are adjusted.

## C1 — quiet wheel success

Keep the existing Generic draft/store/planner.

Add a small success-feedback origin/flag so:

- explicit Enter apply announces success;
- wheel apply is silent on success;
- errors remain visible.

Add/adjust targeted proof that wheel success does not overwrite Status with routine confirmation.

Validation: Focused, then included in next Checkpoint proof update.

## C2 — replace row-height UX

Remove top drag Thumb.

Add:

- editable numeric common-height TextBox;
- lightweight row-bottom resize gesture through DataGridRow events;
- common preview 32-96;
- one durable save on release;
- cancel/failed save restores durable height.

Do not replace the DataGridRow template.

Proof:

- resize band begins gesture;
- ordinary row body still navigates;
- drag preview zero-write;
- one release save;
- numeric Enter/Esc;
- invalid range fail-safe;
- Rows/Timeline/font unchanged.

Validation: Checkpoint because native UI proof changes.

## C3 — responsive Fixed grid

Change only `PaletteTilePanel` geometry.

Auto:

- retain 104x104 cells.

Fixed:

- `cell = max(104, ViewportWidth / FixedColumns)`;
- square cells;
- exact fixed column count;
- same slot indexing.

Proof wide/narrow widths and slot shortcut invariance.

Validation: Focused + targeted native proof.

## C4 — owner-window popup lifetime

In `IntentPalettePanel`:

- resolve owning WPF Window when loaded;
- subscribe to `Deactivated`;
- close quick settings on deactivation;
- unsubscribe on unload/owner replacement.

Keep existing same-Set refresh stability.

No global hook/polling.

Proof owner deactivation closes zero-write and reactivation does not reopen.

Validation: Checkpoint if host activation proof changes; otherwise Focused plus existing native route.

## C5 — corrective checkpoint

When C0-C4 are complete:

1. full Checkpoint;
2. update `USAGE.md` to remove direct-digit docs and describe new row resize/fixed fill;
3. update package verification wording;
4. run Release only when the corrected Hands-on candidate is ready;
5. produce a new Hands-on `.ymme`;
6. do not merge before owner acceptance.

## Stop conditions

Stop and review rather than expanding scope if:

- row-boundary resize requires replacing the full DataGridRow theme/template;
- Fixed fill requires changing saved shortcut semantics or column count;
- popup deactivation needs a global OS hook;
- quiet wheel feedback requires a second Generic save path;
- cleanup reveals a compatibility path whose removal would require migration.
