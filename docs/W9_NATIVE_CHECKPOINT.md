# W9 native checkpoint

Verified source: `84cc5b42c0f05722b62dff2b80e0319e3d9928ad`.
Native run: `34861421743`, pinned YMM4 4.55.1.1 Lite on Windows.

- 253 native assertions PASS, retaining W1-W8.
- Release/proof builds: 0 warnings / 0 errors with warnings treated as errors.
- P1-P9, Open XML, native release DLL smoke and packaging PASS.
- Artifact `10355101754`, SHA256 `9c657a98bb3c862878072221524d1ca64b157c32fb5c4ba7b43ec767614a6ba5`.
- Distribution DLL SHA256 `a4504fd851b0dfea907319651c34a051893fdb4cdb5b859e6c9d776066e5db3a`.

Actual WPF Profile/Preset/Library selection, Save, Preview and Place are exercised. Target Companion uses target span and signed offsets; Point Emphasis uses exactly five anchors with floor rounding and fixed duration. The existing LayerPlanner/PlacementPlan performs all placement and native Undo/Redo. A real singleton non-Face TachieItem Template is cloned; source geometry, grouping and Remark are preserved. Detached clone grouping and inherited associations are removed without removing user prose/newlines.

Native assertions cover independent settings reload, multiple presets, dirty draft rejection, numeric normalization, preview without mutation, full-span late blockers, layer exhaustion, invalid cardinality, Character mismatch, missing template, negative/overflow geometry, invalid anchor/duration and stale target geometry. Every unrelated original item survives fixture cleanup.

Initial proof exposed a fixture assumption: native TryAddItems changes selection. The empty-context assertion now explicitly clears public SelectedItems. Review also corrected the expected retained Remark newline; product behavior was not weakened or rewritten. W1-W8 remained green.

Usage and intentional boundary: `docs/SELECTION_PLACEMENT.md`. Selection placement is independent add-only placement; manual association/resync remains expression-only. W10-W12 remain; version is still the intermediate 0.3.1. Keep PR #6 Draft and main unchanged.
