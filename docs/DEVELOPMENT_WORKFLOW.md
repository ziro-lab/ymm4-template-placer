# Main-based development workflow

Effective 2026-09-20, explicitly approved by the repository owner.

## Current baseline

The completed Round 3 source `fdc3f3e5448cdf5ce7c9498776362b1bf598c2b1` was promoted to main by PR #17, merge `b8c787b713e2dbfe253a8ab4ca7f9a6920c81f75`. The promoted tree is exactly `3aa91dd12a8262e60c1909293fb68f93a0a032d1`.

Promotion was checked by the fresh Windows/YMM4 native run `35490064220`, job `106023365142`, including full historical regression, exact distribution DLL smoke and verified packaging. The original Round 3 evidence remains in PR #15 / run `35462490239` (1,327 assertions, `HANDS_ON_ROUND3=PASS`). A new main merge identity does not rewrite the original source/run evidence.

This is an accepted development baseline, not a claim of no known UX issues or compatibility with every third-party renderer. The subsequent hands-on changes belong to Round 4.

## Superseded temporary Git rules

Historical documents and PR descriptions said to keep main unchanged and PR #6/#11/#13/#14/#15 Draft/open/unmerged. Those were temporary rules for the stacked-candidate stage and are superseded by this owner-approved transition. They remain in historical evidence as descriptions of that time, not as instructions for current work.

- PR #17 incorporated the cumulative Round 3 work with a normal merge commit, retaining ancestry.
- GitHub automatically recognized main-targeting ancestor PR #6 as merged.
- PR #11/#13/#14/#15 were closed as incorporated/superseded, not merged into their historical feature bases.
- Historical branches, commits, comments and evidence are retained. No force-push or branch deletion was needed.
- Round 4 is Draft PR #16, now based on main. Its preparation and unfinished implementation do not belong to the stable baseline.

## Ongoing development

Use `main -> focused work branch -> Draft PR -> validation and hands-on acceptance -> approved merge`.

Round numbers are work checkpoints, not a requirement to keep stacking unmerged PRs or hold a version number forever. Choose and verify the next package version when a release candidate is ready. Do not change the pinned host version merely because Git workflow has changed.

Keep the current working PR Draft while incomplete. Promotion of this baseline is not advance authorization to merge untested future work. Do not force-push, delete history, bypass a rejected write or weaken a check to achieve a merge.

## Unchanged quality gates

Preserve the WPF/MVVM root ownership and the shared PlacementPlan/native Undo architecture. Preserve strict Template identity/fidelity, complete preflight, exact managed-expression association, add-only normal placement and settings atomic-write/digest protection.

Heavy builds, native YMM4 proofs, release-DLL smoke and packaging run in the existing Windows Actions lane. Documentation-only commits must not download/build/launch YMM4. Source/XAML/test/workflow changes need actual native results before promotion.

Keep all historical semantic regression gates and independent evidence-negative fixtures. A superseded UI entry may be tested through its approved replacement, but its underlying safety/behavior coverage must not be removed. Add new checkpoint evidence without claiming the entire round passed prematurely.

The `.ymme` root remains `Ymm4TemplatePlacer/`. Record exact source, checkout tree, run/attempt, artifact and package/DLL/source hashes. Native PASS and human acceptance are different claims.


## Tiered validation

Validation is tiered; see `docs/VALIDATION_STRATEGY.md`.

Ordinary Draft-PR product edits use Focused native validation. Changes to tests/fixtures/workflow, PR Ready transitions and explicit checkpoint runs use the full semantic Checkpoint lane. Main pushes use Release, which adds exact distribution-DLL smoke and verified packaging.

Historical gates remain required at Checkpoint/Release. They are no longer required after every small Focused edit. This is an execution-cost change, not permission to delete safety coverage or weaken final promotion evidence.
