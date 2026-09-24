# Behavior Preview Acceptance

Status: **P0 ACCEPTED / P1 v2 REDESIGN ACTIVE**

## P0 — Description Projection — ACCEPTED

P0 remains accepted when:

1. Targeted Settings expose one typed `PlacementBehaviorDescription` covering the current finite placement vocabulary.
2. Generic Settings expose the same projection family where needed.
3. `このセットの動き` is generated from that projection rather than a separate meaning implementation.
4. Existing Summary wording/meaning remains unchanged for accepted current cases.
5. Incomplete numeric Draft text remains visible/projectable and is never replaced by an invented value.
6. The projection performs zero Timeline mutation and no host/source resolution.
7. Native validation retains `BEHAVIOR_PREVIEW_P0=PASS`.

## P1 v2 — Placement-result schematic

P1 v2 is accepted only when the Preview communicates the **configured placement result by block position and block length**, not merely by labels.

### Required visual meaning

For applicable Targeted Sets:

1. target Item / target range is visually identifiable;
2. placed Item is visually identifiable;
3. related/Neighbor Item appears only when the relation needs it;
4. placed start position communicates start / center / end alignment;
5. placed width communicates the configured length relationship;
6. `TargetSpan` visibly matches the target span;
7. Template/fixed duration is visually independent from target span and may carry a small label;
8. Until-related-start visibly terminates at the related Item **start**;
9. Until-related-end visibly terminates at the related Item **end**;
10. previous-neighbor configuration can visually place the related Item before the target;
11. target-relative Up places the result visually above the target;
12. target-relative Down places the result visually below the target;
13. Absolute Layer uses a truthful explicit layer hint rather than fabricating a false relative distance.

### Supplementary meaning

14. fallback appears only when relevant;
15. collision-search direction may be a small secondary hint;
16. incomplete numeric Draft input is shown as incomplete/unknown rather than normalized;
17. Template vs registered TachiePreset does not fork the placement diagram.

### Generic policy

18. Generic Settings do **not** require the full placement diagram;
19. Generic may show only a compact layer result/hint when that adds useful information;
20. no generic diagram may imply a configurable duration relationship that does not exist.

### Safety / architecture

21. Preview reads the current staged Draft;
22. opening/viewing/updating Preview performs zero Settings/Timeline write;
23. Preview performs no live neighbor resolution, source materialization, placement planning or occupancy search;
24. Preview has no drag/edit gestures;
25. real placement remains owned by the existing resolver / geometry / PlacementPlan path.

## P2 — Hands-on acceptance

Before moving to the checklist, real YMM4 hands-on must answer:

- Can the user tell **how the item will actually be placed** without reading documentation?
- Can the user distinguish “対象と同じ長さ”, “周辺の頭まで”, and “周辺の終了まで” at a glance?
- Can the user distinguish target-relative Up vs Down at a glance?
- Do start / center / end alignment changes visibly move the placed block in the expected way?
- Is the Preview compact enough that it does not crowd ordinary Settings work?
- Does the text Summary remain useful without duplicating so much information that the UI becomes noisy?
- Are there configurations where the Preview adds no value and should simply be hidden/minimized?

The checklist starts only after this placement-result visual vocabulary is accepted.

## Historical v1 evidence

PR #36 Checkpoint #720 and Release #724 proved that the shared projection and a read-only WPF Preview can run safely.

That evidence remains architecture/safety evidence only. The v1 card layout is **not** accepted as the final Preview UX.
