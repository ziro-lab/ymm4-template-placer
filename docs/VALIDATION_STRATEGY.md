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

Use on push to main, push to the dedicated `work/v0.4-native-validation` release-candidate branch, or explicit Release workflow dispatch.

Release runs everything Checkpoint runs, then also:

- installs and launches the exact distributable DLL in native YMM4;
- verifies DLL identity/hash;
- builds the verified .ymme, source archive and provenance;
- enforces the stable Ymm4TemplatePlacer/ install root and package hash equality.

This is authoritative promotion/release evidence.

## Automatic selection

- Draft PR ordinary src change -> Focused.
- PR change touching tests/, fixtures/ or the native workflow -> Checkpoint.
- PR ready_for_review -> Release. Marking a Draft PR Ready is the explicit promotion gate for exact distribution-DLL smoke and verified packaging.
- push to `work/v0.4-native-validation` -> Release candidate validation.
- other non-main validation-branch push -> Checkpoint.
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


## Rollout measurement

The first successful tier comparison used the same Round 4 C product state plus validation-tier changes.

- Checkpoint run #209 (`35506188787`): 1,428 native assertions, 0 failures, 5m04s wall-clock, artifact 4,205,014 bytes.
- Focused run #210 (`35506440656`): 473 native assertions, 0 failures, 2m57s wall-clock, artifact 2,833,603 bytes.
- Native assertion volume fell by about 67%.
- End-to-end Actions time fell by about 42%; YMM4 download and both warning-as-error builds remain fixed overhead.
- Focused skipped release DLL smoke, final packaging and historical evidence-negative validators as designed.

Do not optimize for a smaller assertion count by itself. Further reductions need a concrete runtime/maintenance benefit and must preserve the stable-core risk boundary.


## Test lifecycle and retirement

Round/feature-specific tests are allowed while behavior is new or the host boundary is still being learned. They are not automatically permanent Checkpoint/Release requirements.

A historical test may be consolidated into a current invariant proof, or retired from normal execution, only when **all** of the following are true:

1. a current invariant test covers the same failure class;
2. the historical test has no unique host/API/UI/compatibility boundary that would otherwise be lost;
3. the invariant test has an equal-or-stronger mutation/safety boundary (for example Timeline zero-write, exact association, native Undo, or settings conflict rejection);
4. Checkpoint validation passes before and after the consolidation with no lost required behavior;
5. at least one Release validation passes after the consolidation.

Retirement does not require deleting history. Prefer one of these outcomes:

- remove the historical test from Checkpoint/Release execution but keep the source/evidence for traceability;
- replace several round-labeled tests with one stable invariant proof;
- delete a genuinely obsolete test only when its behavior and evidence are fully represented elsewhere and no compatibility value remains.

Do **not** keep a test mandatory merely because it belongs to an older Round. A test stays mandatory because it protects a current product invariant or a unique compatibility boundary.

Examples of consolidation candidates:

- several configuration/navigation operations that independently prove zero Timeline mutation;
- repeated Set-switch tests that protect the same global-presentation invariant;
- repeated UI-entry tests whose only remaining contract is one authoritative root action.

Examples that should usually remain distinct:

- a host-specific public API capability boundary;
- exact native Undo/Redo behavior;
- Template fidelity;
- exact managed-association replacement/removal;
- settings external-change/digest conflict protection;
- a compatibility migration that can regress independently.

## Growth-control thresholds

Track at least these values at meaningful checkpoints:

- Focused wall-clock time;
- Focused native assertion count;
- Checkpoint wall-clock time;
- Checkpoint native assertion count.

These are observation metrics, not score targets.

Review validation growth when either of these becomes true:

- Focused exceeds roughly **4-5 minutes** on the current pinned runner/host;
- Focused or Checkpoint grows by roughly **25-30%** from the last reviewed baseline without a comparable increase in product risk/coverage.

At that point, first look for invariant consolidation and obsolete historical duplication. Do not immediately add file-to-test routing logic.

## Deferred optimization triggers

Do not introduce change-area-specific Focused selection while the current Focused lane remains around the present cost. File-to-test routing becomes its own maintenance and validation system.

Do not split the proof harness out of the product build merely to save a small amount of build time. Reconsider a separate proof harness only when the second proof build is a demonstrated dominant bottleneck (for example, total Focused time has grown beyond the agreed review threshold and proof compilation is a substantial share).

The goal is long-term bounded complexity, not minimizing every second of CI time.
