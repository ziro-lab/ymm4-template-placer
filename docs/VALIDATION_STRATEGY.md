# Validation strategy

Effective after the Round 4 C checkpoint.

This changes when existing tests run. It does not delete historical tests or weaken final release acceptance.

## Why

By Round 4 C the repository had 72 native/test scripts and 1,428 native assertions. The coverage is useful, but requiring the complete historical native ladder, evidence-negative fixtures, release-DLL smoke and packaging after every small Draft-PR edit made validation cost grow permanently with each round.

The correction is execution tiers, not test deletion.

## Focused

Use for ordinary product-source edits on a Draft PR.

Focused always:

- builds both distributable and proof variants with warnings as errors;
- starts the exact pinned YMM4 host;
- retains P1-P9 host/golden-path checks;
- runs a compact stable core: template/library identity, Quick Drop and placement, exact associations, safety intent, intent/relative core, Settings safety, Template fidelity, Preview/navigation and the current Round 4 A/B/C checks.

Focused intentionally does not:

- rerun every superseded UI acceptance stage;
- execute historical evidence-negative fixture suites;
- run exact distribution-DLL release smoke;
- build the final .ymme/source/provenance package.

A Focused PASS is development feedback, not release evidence.

## Checkpoint

Use when a feature/checkpoint is complete, when tests/fixtures/workflow change, when a PR is marked Ready, or by explicit workflow dispatch.

Checkpoint runs the complete historical semantic native ladder and current checkpoint evidence, including the independent negative-evidence guards.

It intentionally skips release-DLL smoke and final packaging.

## Release

Use on push to main or explicit Release workflow dispatch.

Release runs everything Checkpoint runs, then also:

- installs and launches the exact distributable DLL in native YMM4;
- verifies DLL identity/hash;
- builds the verified .ymme, source archive and provenance;
- enforces the stable Ymm4TemplatePlacer/ install root and package hash equality.

This is authoritative promotion/release evidence.

## Automatic selection

- Draft PR ordinary src change -> Focused.
- PR change touching tests/, fixtures/ or the native workflow -> Checkpoint.
- PR ready_for_review -> Checkpoint.
- non-main validation-branch push -> Checkpoint.
- main push -> Release.
- workflow_dispatch -> explicit Focused / Checkpoint / Release choice.
- documentation-only synchronize events still avoid YMM4 entirely.

## Historical tests

Historical Round 2/3 checks remain source-controlled and still execute at Checkpoint/Release. They should not be copied into new tests merely because a new round exists.

New acceptance should prefer current invariants over repeating old round labels. Reuse an existing test when it already protects the same invariant.

## Stable-core rule

Keep Focused small. Add a check to Focused only when a failure could plausibly cause data loss, incorrect placement, broken Undo/association, unsafe Settings persistence, loss of Template fidelity, or a primary workflow regression.

Visual polish, superseded UI routes, provenance formatting and evidence-parser adversarial cases belong at Checkpoint/Release unless the current change directly edits them.

## Preset work

The experimental preset feature starts from this tiered model:

- ordinary implementation iterations use Focused;
- completion of capability/placement checkpoints uses Checkpoint;
- package/release proof waits until the whole candidate is ready.

This avoids turning the upcoming D/E acceptance set into a permanent 40+ check tax on every edit.
