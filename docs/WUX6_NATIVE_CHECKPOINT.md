# WUX6 — Automatic preview, task-aware feedback, automatic Character

Finding: an extra manual Preview action interrupted every placement, normal refresh messages consumed space, and unrelated successes survived navigation. Management also asked again for a Character already present in the source.

Change: only the active Selection task schedules a deferred preview after selection, Template, saved condition, or task-entry changes. One dispatcher operation coalesces a burst; no polling or work in CanExecute. It discards the plan; Place still builds and commits a fresh plan. Missing source/invalid plan becomes inline information without erasing global errors or partial Resync results. Leaving/hiding/disposal cancels queued preview work. An explicit refresh remains for source/geometry edits that do not change selection.

Success clears on task navigation; errors remain, and a real partial Resync retains counts/reasons. No notification timer. Empty normal status is no longer replaced by general instructions. Management displays the existing source-derived Character, with manual override available under secondary disclosure; neutral sources and original guards remain.

Costs removed: Preview ritual, repeated confirmation text, stale task interpretation, repeated Character input and a Template picker reset caused by unrelated settings saves. No Profile, Layer, association, Excel schema or storage schema was changed.

Trade-off: item-motion/source-body changes without selection change are not continuously observed. Use 予定を更新 to refresh the displayed numbers; Place always revalidates. Error feedback is intentionally not cleared by an automatic preview. Synthetic performance is not a guarantee for heavy user assets.

## Verified native checkpoint

Source `3762281266c077e34e7d4b3faa2119932fb1166d`; tree `e7be625f10318fa13bbe4ed7ecfe9cdcd174485a`.
Run `34906577203`; job `104184529649`; artifact `10371984252`.
YMM4 4.55.1.1 Lite: **547 assertions PASS**, P1-P9 / W3-W12 / V04 / WUX1-WUX6, 18/18 integrated acceptance. Release/proof **0 warnings / 0 errors**, Open XML / exact release DLL smoke / package PASS.

Native cost: 204 added Voices, 212 total items, 24 iterations, mean 1.5825 ms, p95 1.7816 ms, max 2.0869 ms. The native check requires p95 below 50 ms, unchanged Timeline/settings, exactly one calculation for eight same-turn events, no idle polling and no invisible-task calculations.

The first WUX6 fixture registered a source while already on Selection, then incorrectly expected same-tab navigation to clear the message. The fixture now performs registration in the real management task and then navigates to Selection. The original assertion is retained and passes; no product guard was weakened.

Actually inspected ux-auto-preview-normal.png / ux-auto-preview-narrow.png: what/where/Place and automatic numbers are visible, old normal-success text is absent. The Character capture showed management's list rather than the summary because it was scrolled out of view; WUX7 will explicitly bring that summary into its evidence viewport. This capture was not accepted as visual proof of the Character summary.
