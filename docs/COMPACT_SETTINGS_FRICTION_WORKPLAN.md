# Compact Settings Friction / Discoverability Workplan

Status: **FIRST CANDIDATE NATIVE GREEN — HANDS-ON CORRECTIVE PASS FROZEN / IMPLEMENTATION NOT YET STARTED**

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


## Current checkpoint — first candidate

PR #31 first hands-on candidate:

- exact candidate head: `1792e55bcb9b4b93e2c9e41265eeb6a4f8240214`;
- Checkpoint run: **#638 / 35864275327**;
- result: **SUCCESS**;
- Native assertions: **1,800 PASS**;
- `COMPACT_SETTINGS_P1=PASS`;
- `COMPACT_SETTINGS_P2=PASS`;
- `COMPACT_SETTINGS_P3=PASS`;
- retained Round 4 and Final wheel regression: GREEN.

This proves the first layout candidate, not Phase 4 completion.

## P6 — hands-on corrective layout pass — NEXT

Do not begin implementation until this frozen handoff is intentionally resumed.

Corrective scope:

1. move `このセットの形` to the bottom of compact Settings;
2. remove the duplicate top-row Set `削除`, retaining management delete;
3. group Set management and cross-Item copy so normal widths can use horizontal space without forced compression;
4. allow the management/copy group to wrap naturally at narrow widths;
5. align the `演出と並び順` list lane with the Template-row lane in `テンプレートをまとめて追加`;
6. preserve useful list margins and breathing room;
7. guarantee no outer horizontal scrolling / hidden required content;
8. wrap text and control groups vertically when width becomes insufficient.

Do not change Settings semantics, commands, persistence, placement rules or Preview.

## P7 — wheel dead-zone correction — NEXT

Treat the user-observed white-space wheel dead zone as a real Phase 4 friction item.

Investigate the live WPF hit-test/admission path before choosing the smallest correction.

Required result:

- ordinary Settings white space scrolls the authoritative outer Settings viewer;
- visually continuous fixed/header Settings areas do not create unexplained wheel dead zones;
- ComboBox / RangeBase ownership remains protected;
- inner ScrollViewer ownership remains direction-aware;
- exhausted inner scrolling hands off to the outer viewer;
- useful visual margins remain; do not solve this by deleting whitespace.

Add focused Native proof for live hit testing at representative white-space/header/gutter points.

## P8 — second integrated candidate

After P6-P7:

- run focused proof;
- run Checkpoint/native full regression;
- capture narrow and normal-width screenshots;
- capture evidence for no horizontal overflow and wheel continuity;
- produce a Hands-on `.ymme`.

Do **not** promote to Release or start Preview yet.

## Second corrective candidate — Native GREEN

Exact implementation/test source:

- `b465bdaf221ba42176af1f84aa9b2906f513505d`
- Checkpoint **#648 / 35880380282**
- **1,816 Native assertions PASS**
- `COMPACT_SETTINGS_P1=PASS` through `COMPACT_SETTINGS_P6=PASS`
- full retained `PASS P1 P2 P3 P4 P5 P6 P7 P8 P9`
- full semantic regression / evidence guards: PASS

New physical wheel evidence includes:

- fixed auto-commit/footer strip;
- far-left white Settings edge;
- white edge outside the right scrollbar.

Layout evidence includes:

- Set management and cross-Item copy heading/action-row alignment;
- list action rows aligned to their corresponding list lanes.

Status: **NATIVE GREEN / USER HANDS-ON NEXT**.

Do not Release, merge, or begin Behavior Preview until P9 is accepted.

## P9 — second real YMM4 hands-on gate

User hands-on specifically checks:

- Set management/copy grouping;
- duplicate delete removal;
- Set-shape placement at the bottom;
- visual column/list alignment;
- normal and narrow floating-window resizing;
- auto-wrap instead of horizontal hiding;
- wheel continuity across ordinary white space, margins, list boundaries and fixed/header areas.

Only when this pass has no remaining high-frequency Settings friction should Phase 4 close and Phase 5 Behavior Preview become NEXT.
