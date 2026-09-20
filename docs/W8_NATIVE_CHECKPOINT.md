# W8 native checkpoint

Verified source: `9a92376527f22c8a1bb3647bf054e5a878ce0607`.
Native Windows YMM4 4.55.1.1 Lite run: `34856504285`, attempt **2**.

- 211 native assertions PASS; W1-W7 regressions retained.
- Release/proof builds: 0 warnings / 0 errors with warnings treated as errors.
- P1-P9, Open XML, native release DLL identity smoke and packaging PASS.
- Artifact: `10353342681`; SHA256 `ae71fa3e263530925c53c68e2ba716641ed2b537ca9c8d9b79f067499c7d84ec`.
- Distribution DLL SHA256: `3c0effd275bec67ef72c3def2b7730d63979653dd06e4ea1fb4340230e16d7fc`.
- Source ZIP SHA256: `620a538a9f7a364251b37c23fbec950b3f4219db62e375810ec42e16c200f031`.

## Audited acceptance

Actual WPF Place/Resync commands run in real YMM4. Weak Voice/source tags preserve user Remark text; serials are plugin-wide, reconciled with imported/orphan IDs and never consumed by planning alone. Existing associated placement stops rather than replacing or duplicating. Native Undo/Redo covers new items and Voice Remark changes together.

Manual Resync uses the currently saved expression preset, rejects unsaved edits, resolves exactly one target by serial plus actual Character, and never guesses by geometry, text or ordering. Missing/ambiguous/malformed targets and grouped faces are skipped. The successful subset is one native Undo unit. Selection scope, unchanged-item no-op, full-span collision planning, planned occupancy reservations, stale-plan rejection and preservation of unrelated items are asserted. Quick Drop strips copied associations and never allocates a serial or becomes a resync target.

## Execution note

Attempt 1 failed before build: GitHub Release asset download returned HTTP 504 through its bounded retries. Attempt 2 at the identical source SHA downloaded the pinned hash and passed all steps. This is not a product/native assertion failure.

## Boundary

This is an intermediate W8 checkpoint, not final v0.4. Assembly/package version is still 0.3.1. W9-W12 and integrated final acceptance remain. Keep PR #6 Draft; do not merge main.
