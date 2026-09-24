# Baseline Simplification Workplan

Status: **P1-P4 COMPLETE — P5 MAIN PROMOTION ACTIVE**

Authorities:

- `BASELINE_SIMPLIFICATION_DESIGN.md`
- `BASELINE_SIMPLIFICATION_ACCEPTANCE.md`
- `CURRENT_ARCHITECTURE.md`
- `VALIDATION_STRATEGY.md`
- `LEGACY_COMPATIBILITY_MAP.md`

Current candidate:

- PR: #32 (`work/v0.5-baseline-simplification`);
- Release candidate source: `800b9bd261180f016ab3c1f26e67df1b55fd1eda`;
- Checkpoint #691: SUCCESS;
- Release #707 (`35982411699`): SUCCESS;
- Native assertions: **1,198 PASS / 0 FAIL**;
- exact distribution-DLL smoke: PASS;
- verified `.ymme` / source / provenance packaging: PASS;
- Release artifact SHA256: `5391a61783b4333b2abd7e419cb895e622b14fb47e997bb222ed16a05a32943b`;
- previous accepted Phase 4 baseline: 1,822 assertions;
- reduction: about **34.2%** while retaining current Set/Settings, association, native Undo, source identity and expression-performance boundaries.

## P0 — scope / cut-line freeze — COMPLETE

Frozen policy:

- old data readability and old executable product paths are separate concerns;
- association readers and settings safety remain protected;
- executable `LegacyWorkspace` is removed rather than kept as a second product mode;
- legacy Selection/Palette/ExpressionPreset families are judged by current reachability and data value, not by filename;
- validation consolidation follows architecture simplification and keeps unique safety/host boundaries.

Baseline before cleanup:

- main merge commit: `5a44e45f7ac1966c51e5bb41125d086f18119c54`;
- main Release #657: SUCCESS;
- Native assertions: 1,822;
- previous validation-strategy review baseline: 1,428 assertions;
- growth: ~27.6%, crossing the documented 25-30% review threshold.

## P1 — remove LegacyWorkspace runtime path — COMPLETE

Production result:

1. serialized `PlacerSettings.LegacyWorkspace` is absent from distribution builds;
2. `UseLegacyWorkspace`, workspace switching commands and mode-change branches are gone from the product runtime;
3. Placement / Settings / expression surfaces are the current surfaces unconditionally;
4. transient expression work no longer carries a legacy-workspace identity or deferred mode switch;
5. Generic placement, position shortcuts, pointer routing and Settings auto-commit no longer branch on legacy mode;
6. old JSON containing an extra `LegacyWorkspace` property cannot reactivate the old UI and is naturally dropped on a later current save.

A bounded `YMM4_PROOF`-only shim remains only so historical proof code can be consolidated without reintroducing the path into the distributable.

## P2 — unreachable legacy UI/code audit — COMPLETE

Audit result:

- the old Palette / Selection workspace is no longer a selectable product mode;
- legacy-looking Selection / Palette / QuickDrop classes are **not** blanket-deleted when current code, secondary workflows or retained compatibility still reuse them;
- standalone `ExpressionPreset` compatibility remains where current unregistered direct-preset behavior still depends on it;
- historical UI/proof source may remain for traceability without becoming a runtime compatibility promise.

Future deletion still requires concrete reachability / serialization / migration evidence. The maintenance goal is one current executable product path, not cosmetic filename cleanup.

## P3 — settings compatibility cut — COMPLETE

Supported pre-publication boundary:

- Portable `Data/settings-v04.json` remains the authoritative current settings file;
- the bounded one-way LocalAppData -> Portable migration remains because it protects real retained user data;
- the removed `LegacyWorkspace` property no longer has a current serialized/runtime meaning;
- association readers, current source identity compatibility and Settings corruption/conflict safety remain;
- no second old UI/runtime is retained merely to read historical settings.

No broader migration-chain rewrite is required for this maintenance cut.

## P4 — validation consolidation — COMPLETE

Result:

- historical WUX8-WUX12 workspace-specific acceptance is no longer a mandatory runtime contract;
- current workflow acceptance is represented by WUX13 plus named current native invariants;
- `RunNative.ps1` validates the workflow evidence schema, required current native stages and PASS results instead of a historical fixed check count;
- current association / Undo / Settings conflict / source identity / expression performance boundaries remain mandatory.

Measured result:

- Phase 4 baseline: 1,822 assertions;
- current Checkpoint #691: 1,198 assertions;
- reduction: ~34.2%;
- assertion failures: 0.

This is validation-history consolidation, not risk-boundary deletion.

## P5 — final baseline promotion — MAIN PROMOTION ACTIVE

Promotion sequence:

1. Checkpoint — **GREEN (#691)**;
2. no extra current-workflow hands-on was required: this cleanup removes a superseded compatibility mode rather than changing the accepted current workflow;
3. Release candidate — **GREEN (#707)** at exact source `800b9bd261180f016ab3c1f26e67df1b55fd1eda`;
4. exact distribution-DLL smoke and verified `.ymme` / source / provenance packaging — **GREEN**;
5. merge PR #32 to main — **NEXT**;
6. main Release — pending;
7. mark Baseline Simplification complete and unblock Behavior Preview.

Do not start Behavior Preview implementation until main promotion is complete.
