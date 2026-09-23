# Compact Settings Friction / Discoverability Acceptance

Status: **FROZEN ACCEPTANCE CANDIDATE**

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

The targeted Set picker ComboBox and `＋` / `削除` buttons have comparable native heights and aligned vertical placement without changing global control styles.

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
