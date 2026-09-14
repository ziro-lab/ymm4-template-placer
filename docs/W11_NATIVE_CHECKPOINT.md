# W11 native checkpoint

Verified source: `3e6729810a4a28f7ec34a34de19c5c150f723476`.
Native Windows YMM4 4.55.1.1 Lite run: `34863878877`.

- 320 native assertions PASS, preserving W1-W10.
- Release/proof builds: 0 warnings / 0 errors with warnings treated as errors.
- P1-P9, Open XML, release DLL identity smoke and packaging PASS.
- Artifact `10356336343`, SHA256 `99fb151b8f4c611359e3b64b0679493ea1bb5de897e1b7bdba49b7e0b9b89b9f`.
- DLL SHA256 `4d01eba6636b0476e9432e99f3dc46c9b62f25b56d7fec073a27ce6df3d590fd`.

Boundary is a thin span strategy over the existing SelectionPlacement, LayerPlanner and PlacementPlan. Exactly two selected Items are ordered by their start Frame. Equal starts are ambiguous. The difference between earlier.End and later.Start must lie within the nonnegative Tolerance. The agreed cut is explicitly earlier.End (exclusive end), not an inferred midpoint. StartOffset and fixed Duration then define the overlay span.

Actual native WPF tolerance editor, Save, Preview and Place pass. Native Undo/Redo returns/restores the one added overlay. Full-span late blockers select a free Layer before mutation. Exact adjacency, allowed small gap and overlap, tolerance boundaries, reversed selection and preservation of source/target/user Remarks are asserted.

Too-large gap/overlap, same start, wrong cardinality, negative result start, overflow, zero duration and exhausted Layer band all reject without partial mutation. A deliberately nearby unselected cut is never substituted. W10 settings migrate to include the default Boundary preset without replacing existing parameters, changing the current preset or writing on load.

Detailed cut semantics and UI behavior: `docs/SELECTION_PLACEMENT.md`. All five v0.4 Profile families are now implemented; W12 final UI/UX, version, documentation and integrated final-package acceptance remain. The intermediate package is still version 0.3.1. Keep PR #6 Draft; do not merge main.
