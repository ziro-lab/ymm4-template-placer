# Compact Settings Friction / Discoverability Workplan

Status: **P0 DESIGN FROZEN — P1 IMPLEMENTATION NEXT**

Authorities:

- `COMPACT_SETTINGS_FRICTION_DESIGN.md`
- `COMPACT_SETTINGS_FRICTION_ACCEPTANCE.md`
- `CURRENT_ARCHITECTURE.md`
- `VALIDATION_STRATEGY.md`

## P0 — scope freeze

Complete in this document set.

Freeze the phase as layout/discoverability only. Preview, Full Settings, placement-model changes and wheel-routing redesign are excluded.

## P1 — first-level visibility

Replace only the five ordinary first-level disclosure containers with always-visible sections.

Retain the existing nested advanced Expanders.

Move `全体の表示・操作` lower while keeping it folded.

Focused proof:

- all five ordinary section headers/content visible;
- advanced sections remain folded;
- no command/binding route changes.

## P2 — width / scrolling polish

Make the Template source list modestly narrower with outer-scroll escape space on both sides, initially left about 16 DIP and right about 6 DIP.

Keep label wrapping and current nested-wheel routing.

Focused proof at narrow width:

- source list width < enclosing section width;
- non-zero left/right gutters and left > right;
- panel does not exceed host width;
- no horizontal-scroll requirement introduced.

## P3 — Set picker row alignment

Apply local-only sizing/margins so Set ComboBox / `＋` / `削除` read as one row.

Do not change implicit global styles.

## P4 — Native integrated candidate

Run Focused/Checkpoint as required by `VALIDATION_STRATEGY.md`.

Capture narrow and normal native Settings screenshots/evidence.

Do not start Preview in this PR.

## P5 — real YMM4 hands-on gate

Hand the candidate to the user before Phase 5.

Treat width values and small spacing as tunable:

- adjust if the left scroll strip is too small;
- reduce if it wastes too much width;
- widen the Template list if labels feel cramped;
- fix any remaining first-look/click/scroll friction.

Only after hands-on acceptance should closeout/Release promote Phase 4 and Roadmap Phase 5 become NEXT.
