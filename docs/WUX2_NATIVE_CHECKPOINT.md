# WUX2 — Unified Palette task (native verified)

Finding / task: collecting and switching frequently used templates required a Character/Style mode decision before choosing the actual palette. A normal entry also repeated an unnecessary available status.

Changes:
- One Palette picker presents all existing palettes. The persisted kinds, manual selections, IDs, memberships and schema remain unchanged.
- Creation asks for a name and an optional linked Character; the existing kind is derived from the link. New manual palettes do not inherit the last Character input.
- An explicit picker choice wins over the old Timeline context until the next actual Timeline selection. Linked palettes then resume automatic Character context; clearing context restores the last manual linked choice. Unlinked vocabularies remain manual, preserving the existing Style regression semantics.
- Initial/default palette names can be edited without deleting/recreating memberships. This edits an existing model field, not a new setting or rule.
- Normal entry status is empty in Palette and management projections. Missing, ambiguous and Character-mismatched references retain actionable warnings.
- The old registered-Library-to-Palette add editor is no longer exposed. Its internal method remains available to existing behavior tests; the W4 actual-UI path now exercises direct source addition and verifies the same shared stable Library ID.
- Base/Front/Back remain unchanged; shorter labels and a tooltip replace a wide always-read explanation. Primary add/drop controls remain outside editor scrolling.

Costs removed: mode switching, separate palette searches, redundant ready text, remembering where membership addition lives, and recreating a palette just to rename its initial default.

Trade-off: selecting a linked palette enables the existing temporary Character behavior; choosing an unlinked palette means a manual vocabulary. A short context label identifies an active automatic switch. Explicit user selection is never immediately overridden by a stale context event. The old internal split is deliberately retained for settings compatibility.

## Proof

- Source commit: `3a8bb4a5a6fb5709ecba4356717d19889ad36415`
- Native run: `34885922161`; job: `104116431212`; artifact: `10364831955`
- Actual YMM4 4.55.1.1 Lite; **437 native assertions PASS**.
- P1-P9, W3-W12, V04, WUX1 and WUX2 PASS; integrated 18/18 acceptance PASS.
- Release and proof builds: **0 warnings / 0 errors**. Open XML, exact distribution DLL smoke and package verification PASS.
- New proof uses the actual unified picker and creation form, checks persisted kind derivation, automatic context display/manual restoration, explicit-selection precedence, source warnings and unchanged native Timeline signature.

## Screenshot review

Actually inspected `ux-palette-unified-normal.png` and `ux-palette-unified-narrow.png` (360 x 360). The linked palette name, short template label, direct addition, drop control, Base/Front/Back and collapsed secondary editors are readable and reachable without horizontal scrolling. Normal entries now occupy one line instead of a title plus available text. The WUX1 source-picker fixture now scrolls its actual selected source into view.

Remaining scheduled work, not claimed solved here: primary tab order/management demotion (WUX3), expression task and global Resync (WUX4), selection task (WUX5), stale global success text (WUX6), final narrow layout and creation/recovery screenshot matrix (WUX7).

Boundary: real native host with synthetic sources and actual WPF commands/selection binding; not physical mouse injection or a user asset/theme/DPI matrix.
