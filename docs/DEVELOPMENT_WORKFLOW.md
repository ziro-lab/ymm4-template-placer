# Main-based development workflow

Effective 2026-09-24. This is the current Git / promotion workflow.

## Current baseline

The accepted product/test/package baseline is the Baseline Simplification main promotion:

- product merge: `af6a667e401a99311ef58293c750ac51b9145f54`;
- main Release #708: `35983059810`;
- Native: **1,198 PASS / 0 FAIL**;
- exact distribution-DLL identity smoke: PASS;
- verified `.ymme` / source / provenance packaging: PASS.

Documentation-only authority commits may follow that product merge without changing the accepted runtime/package bytes.

Current architecture and sequencing authorities are:

1. `docs/CURRENT_ARCHITECTURE.md`;
2. `docs/PRODUCT_ROADMAP.md`;
3. the active phase's DESIGN / ACCEPTANCE / WORKPLAN documents;
4. `docs/BACKLOG.md` only for collected/deferred ideas.

Baseline Simplification is complete. Behavior Preview + checklist is the next product phase.

## Normal development

Use:

```text
main
-> focused work branch
-> Draft PR
-> Focused / Checkpoint feedback as appropriate
-> mark Ready when the candidate is actually promotable
-> Release validation
-> approved merge
-> main Release
```

Keep a working PR Draft while implementation or acceptance is incomplete.

Do not force-push, bypass rejected writes, weaken validation, or merge merely to make CI green. Historical branches and PRs may remain as evidence; they are not current execution authority.

## Validation routing

Validation is tiered; `docs/VALIDATION_STRATEGY.md` is authoritative.

- ordinary Draft-PR product-source edits -> **Focused**;
- changes under `tests/`, `fixtures/`, or the native workflow -> **Checkpoint** while the PR remains Draft;
- marking a PR **Ready for review** -> **Release**;
- push to `main` -> **Release**;
- explicit `workflow_dispatch` -> requested Focused / Checkpoint / Release;
- documentation-only PR changes -> no YMM4 native run.

Ready-for-review is therefore a promotion signal, not just a request for a larger Checkpoint run.

Dedicated versioned native-validation branches are no longer part of the normal promotion route. Old validation branches may remain as historical refs, but they have no special Release semantics.

## Quality gates

Preserve the current architecture and safety boundaries:

- WPF/MVVM root ownership;
- shared PlacementPlan / native Undo;
- strict Template/source identity and fidelity;
- complete preflight before mutation;
- add-only normal placement;
- exact managed-expression replacement/removal only;
- protected Settings validation / atomic replace / digest conflict / cross-instance lock;
- bounded current compatibility rather than reintroducing superseded runtime modes.

Historical tests are not permanent because of their age or Round label. They may remain as trace evidence, but Checkpoint/Release requirements should be current named invariants plus unique safety/host boundaries as defined by `docs/VALIDATION_STRATEGY.md`.

The `.ymme` install root remains:

```text
Ymm4TemplatePlacer/
```

Release evidence records exact source/checkout identity, native result, distribution DLL hash and package/provenance output. Native PASS and human hands-on acceptance are separate claims.
