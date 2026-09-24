# Behavior Preview Workplan

Status: **P0 ACTIVE**

## P0 — common meaning projection

- add `PlacementBehaviorDescription` for Targeted + Generic Drafts;
- move existing Summary generation behind the projection;
- preserve existing visible Summary behavior;
- prove incomplete numeric Draft behavior;
- add `BEHAVIOR_PREVIEW_P0=PASS` to all validation tiers.

Gate: Checkpoint GREEN.

## P1 — Preview model + schematic

- define the minimal visual tokens consumed from the description;
- add a read-only Preview control;
- bind it to the current staged Draft;
- cover Targeted start/center/end, duration, relative/absolute layer and neighbor/fallback;
- cover Generic current-frame/layer behavior;
- no mutation / no live placement planning.

Gate: Checkpoint GREEN.

## P2 — layout / hands-on

- validate narrow and normal Tool widths;
- decide Preview placement relative to `このセットの動き`;
- verify ordinary Settings edits stay fast and readable;
- verify Template/TachiePreset source kinds do not fork placement semantics.

Gate: user hands-on accepted + Release GREEN.

## P3 — checklist handoff

After Preview acceptance, freeze the visual/description vocabulary and start a separate checklist design:

```text
current Settings Draft
-> temporary checklist draft
-> preview / textual result
-> explicit apply
-> current Settings Draft
```

Checklist Cancel remains zero-change.

Undo/Redo shortcut work may be reconsidered after P2/P3; it is not a Preview prerequisite.
