# v0.4 Task UX contract

## Source and scope

Frozen regression source: c3fc36837508b59d09026a74b5799141eae00a7a / native run 34878781993, 384 assertions and 18/18 acceptance. UX development uses feature/v0.4-task-ux / PR #8 into feature/v0.4-integrated-candidate / PR #6. Main remains unchanged. UX changes supersede only the historical DESIGN/ROADMAP UI/setup descriptions, not Profile or safety semantics.

## Findings, recurring cost and validation

| Unit | User task and finding | Cost removed | Trade-off and native evidence |
| --- | --- | --- | --- |
| WUX1 | Collect and place a frequently used Template required Library then membership setup | Two registration paths, initial setup, duplicate names | Exact unique reuse only; frozen destination; errors retain input. Actual add/drop/Undo, ambiguous/no-write tests. |
| WUX2 | Switching a palette first required understanding Character/Style | Mode decision and separate searches, normal ready text | Internal storage split stays. Manual vocabularies stay manual; linked temporary context restores manual choice. Actual picker, persistence and warnings. |
| WUX3 | Missing expression had no local next action; management competed with daily tasks | Remembered tab/setup dependency, repeated Character input, restoring lost assignments | Actual YMM4 Face source must exist. Exact-Character picker, nested Back, preserved assignments and stale-source guards. |
| WUX4 | Timing and preset controls displaced Serif/Template | Scanning secondary columns, preset concept on first use, unrelated Resync | Timing in tooltip; narrow selected Serif below table; multiple saved conditions retain selector. Actual bound commands and viewport checks. |
| WUX5 | Three abstract dropdown decisions concealed what/where task | Opening method menu, invalid-method judgment, mandatory preview ritual | Invalid remembered family requires explicit valid choice; no silent preset rewrite. One/two/three native selections and actual purpose controls. |
| WUX6 | Manual Preview and stale success feedback, redundant Character input | Repeated clicks/interpretation/input; unrelated picker resets | Coalesced active-task preview only; explicit update for selection-unchanged edits, fresh plan on Place. Native 204-Voice cost/no-poll/no-write checks and partial/error feedback. |
| WUX7 | Secondary editors could stack; empty tasks and disabled actions were unclear | Secondary layout recovery, uncertain next action, same-name source identification | Editors scroll within bounded areas without discarding draft. Native normal/narrow galleries, viewport assertions, actual close/reopen, updated guide and package gate. |

## Internal versus task model

LibraryEntry, TemplateLocator, PaletteDefinition.Kind, stored manual choices, Expression/Selection Presets and planner/profile classes remain internal authoritative structures. UI asks for a palette, an optional linked Character, a Template, and a placement purpose. Registration reference creation/reuse, kind derivation, source Character, eligible-purpose filtering, current placement range presentation and automatic preview are derived by the application, never guessed from fuzzy source names.

## Notification and lifecycle

Success clears on task navigation. Errors and genuine partial Resync results remain until a later explicit result replaces them. Automatic preview never claims success or clears errors. No notification timer, no idle polling, no continuous source/target tracking. Hidden/unloaded tool cancels queued preview and detaches its view subscription. The production placement path always re-resolves/preflights at action time.

## Acceptance and boundaries

NativeTaskUxFinalProof writes ux-acceptance.json only after WUX1-WUX7 and V04 stage markers exist, which are emitted after detailed tests pass. The manifest contains the 12 required P0/P1 requirements; tests/PackageVerified.ps1 gates packaging on it as well as the unchanged 18 original requirements, all native stages, clean builds and exact distribution smoke identity. Assertions were adapted to actual task controls, not deleted to conceal failures.

Screenshots require visual inspection in addition to measurable controls. 360px is tested at 320/360px heights; selected long Serif has a bounded full-width area. Editors may need internal scrolling; normal add/place actions must remain accessible. Tooltip guidance is available for disabled actions. Same-name Selection choices show Character/source context.

No physical-pointer/installer automation, beginner participant study, arbitrary real PSD workload, all-DPI/theme matrix or future YMM4 guarantee. UI bulk row assignment (P2) is deliberately not added; existing Excel Bridge remains the low-risk bulk path. No placement engine, Layer algorithm, profile family, saved condition semantics or workbook schema is redesigned.
