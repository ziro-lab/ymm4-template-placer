# WUX4 — Expression assignment without setup concepts

Finding: the normal expression task spent width on timing columns and height on preset instructions, and every task displayed Expression-only Resync. The user task is to read a line and choose its expression.

Changes: the standard grid has No / Character / Serif / Template. Timing remains in row tooltips. At narrow widths the selected Serif is also readable in a full-width, bounded detail area. Palette aliases omit the repeated Palette badge in the visible selector but keep the same exact source and detailed tooltip. One saved expression preset is represented by its actual placement range, initially `音声と同じ`; its selector appears when multiple presets exist. All saved-preset editing capabilities and dirty guards remain. Excel controls are secondary; the unchanged placement and scoped Resync actions are anchored below the grid. Resync is absent from Palette, Selection and the global header, and requires an associated selected native Voice or Face; disabled guidance distinguishes Timeline selection from row selection.

Costs removed: secondary-column scanning/horizontal exploration, mandatory preset vocabulary, repeated badge reading, unrelated recovery choices, and searching for a truncated Serif. No profile geometry, association, Resync scope, current-preset semantics or Excel schema changed.

Trade-offs: at 360px the table shows a short Serif excerpt; selecting a row provides its full text below, and longer text scrolls in that bounded area. Timing is available on demand, rather than occupying every row. Multiple custom presets retain the fast selector. Excel is one disclosure away and stays open during an editing session.

## Actual native proof

- Source: `ac0c9cf5d58efba7bf260cbde9dc6d975abefde1`
- Tree: `727361d7978cde100e6d3bea98dc8088cc75ef28`
- Run: `34890993402`, attempt 2; job `104135684807`; artifact `10366961821`
- YMM4 4.55.1.1 Lite: **489 assertions PASS**, P1-P9 / W3-W12 / V04 / WUX1-WUX4, 18/18 integrated acceptance.
- Release and proof builds: **0 warnings / 0 errors**. Open XML, exact distribution DLL native smoke and package verification PASS.
- Attempt 1 stopped during YMM4 download on a connection reset, before any build. The same unchanged source was rerun through the supported Actions job rerun operation; attempt 2 passed the pinned download and complete proof.

The new native checks cover actual column widths and Template selector bounds at 360px, the full-width selected Serif, direct missing-candidate recovery, reachable Place/Resync controls, single/multiple preset disclosure with current IDs retained, dirty guards, and real associated versus unrelated Timeline selection. Timeline content is unchanged by UI checks.

Actually inspected: `ux-expression-normal.png`, `ux-expression-narrow.png`, `ux-expression-missing-narrow.png`. All four columns and the primary action are visible without whole-task or horizontal scrolling. Long text is readable in its own bounded area and the missing-candidate action remains visible. Old global success text is still visibly present; task-aware status cleanup is WUX6, not claimed solved here.

Boundary: actual native host with synthetic fixtures and bound WPF controls/commands, not physical pointer injection or a user asset/theme/DPI matrix. Main and the Candidate baseline branch are not yet changed; integration follows the remaining UX units.
