# Baseline Simplification Workplan

Status: **P0 COMPLETE — P1 LEGACY WORKSPACE REMOVAL NEXT**

Authorities:

- `BASELINE_SIMPLIFICATION_DESIGN.md`
- `BASELINE_SIMPLIFICATION_ACCEPTANCE.md`
- `CURRENT_ARCHITECTURE.md`
- `VALIDATION_STRATEGY.md`
- `LEGACY_COMPATIBILITY_MAP.md`

## P0 — scope / cut-line freeze — COMPLETE

Frozen policy:

- old data readability and old executable product paths are separate concerns;
- association readers and settings safety remain protected;
- `LegacyWorkspace` is the first executable compatibility path to remove;
- P2 audits legacy UI families after P1 is Native GREEN;
- validation consolidation follows architecture simplification rather than guessing which tests are obsolete first.

Baseline:

- main merge commit: `5a44e45f7ac1966c51e5bb41125d086f18119c54`;
- main Release #657: SUCCESS;
- Native assertions: 1,822;
- previous validation-strategy review baseline: 1,428 assertions;
- growth: ~27.6%, crossing the documented 25-30% review threshold.

## P1 — remove LegacyWorkspace runtime path — NEXT

Production changes:

1. remove serialized `PlacerSettings.LegacyWorkspace`;
2. remove `UseLegacyWorkspace`, `SetLegacyWorkspace`, legacy open/close commands and mode-change branching;
3. make current placement/settings/expression surfaces unconditional;
4. remove transient-work mode identity and deferred-expression mode comparison;
5. simplify Generic placement, position shortcuts, pointer routing and Settings auto-commit admission by removing legacy checks;
6. keep current Tachie Preset source-mode behavior intact.

Validation changes:

- replace legacy-switch tests with a current invariant: old JSON cannot reactivate legacy UI;
- preserve current input, Settings, expression and placement regression;
- retire tests whose only contract is switching in/out of LegacyWorkspace when an equal-or-stronger invariant exists.

Gate:

- Checkpoint GREEN;
- then Release GREEN before P2.

## P2 — unreachable legacy UI/code audit

Audit current reachability after P1.

Candidate families:

- `SelectionPanel`, `SelectionViewModel`, SelectionPreset editor/runtime;
- legacy `PalettePanel` and legacy-only Palette editor/runtime;
- QuickDrop UI/ViewModel pieces, while retaining reusable current planners/models;
- old ExpressionPreset editor/runtime portions not used by current Tachie Preset compatibility.

For every removal candidate record:

- production call sites;
- serialized fields;
- migration use;
- current reuse;
- unique Native safety boundary.

## P3 — settings compatibility cut

Choose a pre-publication supported settings baseline.

Prefer:

- current settings only;
- plus one bounded migration for the current user's realistically retained previous file/backups if needed.

Do not keep incremental revisions indefinitely.

## P4 — validation consolidation

Use `VALIDATION_STRATEGY.md` retirement rules.

Targets:

- LegacyWorkspace switching tests;
- superseded Round-labeled UI structure proofs;
- duplicate no-Timeline-write tests;
- duplicate navigation/visibility proofs.

Retain strong unique boundaries such as association identity, Undo, Template fidelity and settings conflict safety.

## P5 — final baseline promotion

Run:

1. Checkpoint;
2. user hands-on of current workflows if UI reachability changed;
3. Release;
4. main merge;
5. main Release.

Then mark Behavior Preview as NEXT again.
