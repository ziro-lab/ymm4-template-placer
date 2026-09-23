# Compact Settings Friction / Discoverability Acceptance

Status: **ACCEPTED — RELEASE #654 GREEN / USER HANDS-ON APPROVED**

This acceptance applies to `COMPACT_SETTINGS_FRICTION_DESIGN.md`.

## A1 — First-level sections are visible

With a targeted Set selected, the ordinary sections `セットの管理`, `どのアイテムで使うか`, `どう置く？`, `演出と並び順` and `テンプレートをまとめて追加` are present without opening first-level Expanders.

## A2 — Secondary disclosure remains bounded

`対象の詳細`, `細かく調整`, `選択した演出だけの微調整` and `全体の表示・操作` remain folded by default and retain their existing semantics.

## A3 — Global controls move lower

`全体の表示・操作` is below the ordinary current-Set/source editing flow and remains available without creating a second settings route.

## A4 — Bulk-source outer-scroll escape

The Template source list is narrower than the surrounding section at narrow and normal Tool widths, leaving usable empty space on both sides. The left escape space is wider than the right candidate space.

The initial candidate is approximately 16 DIP left / 6 DIP right. Exact values are hands-on tunable and are not acceptance constants.

## A5 — No horizontal width regression

At a narrow native Settings width, the panel remains within the host width, the outer horizontal scrollbar is not required, and long Template labels wrap rather than forcing content wider.

## A6 — Set picker row coherence

The targeted Set picker ComboBox and `＋` button have comparable native heights and aligned vertical placement without changing global control styles. The duplicate top-row `削除` is absent; the retained Set delete lives under `セットの管理`.

## A7 — Existing advanced visibility regression

Neighbor/fallback/alignment/fixed-duration/boundary/Character and other conditional fields retain the accepted visibility behavior. Opening/closing secondary disclosure never discards draft input.

## A8 — Settings safety

The layout pass performs no Timeline mutation, introduces no new persistence route/schema and retains exact session rollback/automatic persistence behavior.

## A9 — Narrow / normal native evidence

Capture native layout evidence at at least:

- narrow Tool width around the existing 360 px/DIP fixture;
- a normal wider Tool width.

Evidence should make first-level visibility, source-list gutters and Set picker alignment inspectable.

## A10 — User hands-on gate

Before Phase 5 begins, real YMM4 hands-on must check:

1. left-side outer scrolling around the Template list;
2. whether the list became too narrow;
3. section discoverability;
4. Set picker row feel;
5. overall scroll length/friction at narrow and normal widths.

A Native-green candidate is not enough to waive this gate.


## A11 — Set shape is secondary in full Settings

`このセットの形` remains available in full Settings but is located at the bottom of the compact Settings flow rather than in the prominent top area.

The placement panel quick-settings route remains the expected high-frequency route.

## A12 — One destructive Set delete route in compact Settings

The top Set picker row contains Set selection and `＋`, but does not duplicate `削除`.

`セットの管理` retains the Set delete action.

## A13 — Management and cross-Item copy form one coherent area

`セットの管理` and `他のアイテムへコピー` are visually grouped so they do not consume unnecessary vertical space at comfortable widths.

At narrow widths they wrap naturally. No action may be clipped or hidden merely to keep the group on one row.

## A14 — Equivalent list lanes align

The left start of the `演出と並び順` item list aligns with the visible Template-row lane in `テンプレートをまとめて追加`, within normal layout tolerance.

Other indentation should follow a small intentional hierarchy rather than unrelated per-section offsets.

## A15 — Horizontal hiding is not an escape route

At supported narrow widths:

- the outer Settings surface requires no horizontal scrollbar;
- ordinary explanatory text wraps before clipping;
- long source/template labels wrap;
- multi-control rows wrap vertically when needed;
- required actions remain reachable without horizontal panning.

## A16 — White-space wheel continuity

With a genuinely scrollable compact Settings surface, wheel input over ordinary visible white Settings space advances the authoritative outer Settings scroll unless the current live control intentionally owns wheel input.

The native proof must cover at least:

- ordinary content inside the scrolling editor;
- an empty/margin/gutter point;
- a visually continuous fixed/header Settings area if one remains outside the outer ScrollViewer;
- ComboBox ownership;
- an inner source-list ScrollViewer while it can move;
- handoff to the outer scroll when the inner range is exhausted;
- pointer-outside behavior remaining unhandled.

No stale event-source fallback is allowed.

## A17 — Floating/resizable density principle

The corrective pass must not shrink the UI merely to reduce vertical or horizontal footprint.

At normal/wide widths the UI may retain comfortable spacing. At narrow widths it should reflow vertically rather than clip horizontally.

## A18 — Preview remains blocked after first Native-green candidate

Checkpoint #638 / PR #31 proved the first Phase 4 candidate Native-green with 1,800 assertions, but this does not close Phase 4.

Phase 5 may begin only after the A11-A17 corrective pass is Native-green and the user completes a second real YMM4 hands-on pass with no known high-frequency Settings friction that should be fixed first.


## A19 — Final hands-on acceptance / Release

**ACCEPTED.**

The final user hands-on pass reported the Settings surface as good enough to close Phase 4 after the whole-Tool wheel and copy-block alignment corrections.

Release #654 (`35897405826`) at exact source `05285a7bae1eb59b4b588c565aa46e29bdea795c` passed:

- **1,822 Native assertions**;
- `COMPACT_SETTINGS_P1=PASS` through `COMPACT_SETTINGS_P7=PASS`;
- full retained P1-P9 semantic regression;
- exact distribution-DLL smoke;
- verified stable-root `.ymme` / source / provenance packaging.

Distribution DLL SHA256: `648014c22af02f726fc95beb6282b177d94673632e21f108c53e057a80081875`.

Release artifact: `10767621834`, ZIP SHA256 `bf217e5162f31de9ff6fc97d441cc08d3327f033a944ac5c61e35b22b78010d1`.

A10 and A18 gates are satisfied. Phase 5 is no longer blocked by Compact Settings friction.
