# WUX5 — Select what to place, then where

Finding/task: daily selection placement exposed three equally prominent abstract decisions. The task is choosing a Template and its relationship to the selected native items.

Change: the source chooser comes first; a directly visible purpose control presents only the two valid choices for one item, Range/Boundary for two, or Range alone for three or more. Saved conditions and their editor are secondary. The primary Place button is anchored outside editor scrolling and works without first invoking manual Preview. Dirty conditions block purpose/preset switches rather than losing the draft. All existing profile geometry and current-saved-preset behavior remain.

Costs removed: opening a generic method dropdown, examining unusable methods, finding the primary action after editor scrolling, and treating preview as a required ritual.

Trade-off: when selection cardinality invalidates the remembered current profile, the user explicitly picks a valid purpose; the UI does not silently replace a saved preset. Preview automation and obsolete global feedback are WUX6 work.

## Recovered and verified

The previous WUX5 attempt stopped at a compiler error, not a native regression: SelectionPresetNotice was referenced but absent. Added the missing UI projection without changing placement code.

Verified source: `38e90d33fc8a20d38344816dc956097a24938d2e`.
Checkout tree: `cfebc4aa24c274bdb49fd1d851f20d2ab98dee51`.
Run `34905222358`, job `104180210191`, artifact `10372123704`.

Actual YMM4 4.55.1.1 Lite: **519 assertions PASS**; P1-P9 / W3-W12 / V04 / WUX1-WUX5, **18/18 integrated acceptance**. Release/proof **0 warnings / 0 errors**; Open XML, exact distribution DLL native smoke and package checks PASS.

Actual WPF RadioButton selection and Template -> purpose -> Place are exercised, including native Undo/Redo, one/two/three-item cardinality, draft guards and 360px control bounds. Reviewed both `ux-selection-task-normal.png` and `ux-selection-task-narrow.png`: what/where/Place are readable and accessible. The old preview prompt and old success remain visible in this intermediate capture and are deliberately not declared solved.

Also re-downloaded and independently inspected WUX4 provenance: source `ac0c9cf5d58efba7bf260cbde9dc6d975abefde1`, run `34890993402` attempt 2, artifact `10366961821`, **489 assertions PASS**. The earlier uncertainty about that result is resolved.
