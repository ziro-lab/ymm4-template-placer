# WUX3 — Contextual recovery and secondary management

Finding: a missing expression candidate required remembering another primary tab and the Library/Palette setup dependency. Management also competed with three daily tasks.

Change: primary navigation is now Palette / Expression / Selection. Template Management is a same-tool secondary view, opened from Palette or the add task, with Back returning to the original task and preserving its source/name/destination draft. A missing-expression row opens the exact Character's Face-template add task directly. Adding refreshes compatible candidates without discarding other assignments; externally changed assigned-source snapshots are deliberately not healed. An already-registered recovery member needs no extra save. A native source that does not exist still has to be created in YMM4; the empty picker explains that boundary and provides list refresh.

Costs removed: management-tab search, remembered setup dependency, repeated Character input, recovery navigation, reassignment after a refresh, and restoration of drafts after a management visit. No placement engine, profile, association or Excel schema changes.

Trade-off: a filtered source picker intentionally excludes neutral/non-Face/other-Character sources during expression recovery. Ambiguous Character identity or source registration remains a blocking error. Source creation itself is not fabricated by this reference-only plugin.

## Native evidence and visual correction

Initial WUX3 source `6f0bfd5b0c52f371f524427517d5173c2a3cbf20`, run `34888608732`, job `104125367098`, artifact `10366380031`: 461 assertions PASS, full original ladder and 18/18 acceptance, release/proof 0 warnings and 0 errors, exact release DLL smoke / Open XML / packaging PASS.

Actual screenshot review found a visible issue despite that green run: the unified Palette name could become blank after another task saved a preset. The settings model's membership list made deep-copied records unsuitable as stable WPF selector values. A small UI-only PaletteChoice projection now retains the picker collection when identity/labels have not changed. Names still update on a real rename. Storage and selection semantics are unchanged.

Verified correction: `0041b598f687d1a7037de4500e69f8830b2a750a`, tree `0fb8f85a1686e2c7614efaa86732e618da18b57a`.
- Run `34889775587`; job `104129238189`; artifact `10365988179`.
- Actual YMM4 4.55.1.1 Lite: **469 assertions PASS**; P1-P9, W3-W12, V04, WUX1-WUX3 including WUX3_PICKER; 18/18 integrated acceptance PASS.
- Release and proof: **0 warnings / 0 errors**. Open XML, exact distribution DLL native smoke and package verification PASS.
- New native test checks actual selected ComboBox value and active binding after unrelated preset copy/save/delete/refresh, no collection reset, actual rename, navigation return, and unchanged Timeline signature.

Screens actually reviewed: `ux-expression-missing-normal.png`, `ux-expression-recovery-empty-narrow.png`, `ux-palette-refresh-normal.png`, `ux-palette-refresh-narrow.png`, and the late-suite `ui-palette-narrow.png`. The recovery task fits 360px and provides a direct next action. Both picker follow-up images and the late-suite gallery now show the current Palette name correctly. The expression table, global Resync, and stale success text are still the scheduled WUX4/WUX6 work, not declared solved here.

Boundary: actual native host and WPF controls/commands with synthetic sources. Not a physical-pointer, user-assets, theme/DPI or installer test matrix. PR #8 remains the UX work branch into Candidate PR #6; main is unchanged.
