# Tachie Preset source — implementation prep

Status: P0 + P1 + P2 GREEN / READY FOR P3

Base before this refresh:

- accepted docs main 817fdfd86539a28b7e8df5ce87eb54333550f259;
- product Release #399;
- Draft PR #19 history retained.

## Expected new product files

Likely:

- TachiePresetCapability.cs — immutable fingerprint/result/candidate types;
- TachiePresetCapabilityCoordinator.cs — UI-affine discovery/cache/cancellation;
- TachiePresetCandidateApplier.cs — fresh re-resolution/application;
- TachiePresetAssociationTag.cs — versioned source descriptor;
- TachiePresetExpressionPlacement.cs — source-specific planning adapter only if it keeps the current planner clearer.

## Likely touched existing files

- PlacerViewModel.cs — source mode, command gating, Excel gating;
- ExpressionLoadCoordinator.cs — source-aware candidate generation only;
- ExpressionPerformance.cs — immutable preset descriptors, row preparation and diagnostics;
- AssignmentRow.cs — minimal source-aware choice extension;
- IntentExpressionImmediate.cs — source-dispatched immediate mutation;
- common managed expression reader/index — source-union seam while preserving current Template semantics;
- PlacerView.xaml — source switch + placement-rule visibility;
- ExpressionPresetPanel.xaml — user-facing placement-rule wording in TachiePreset mode;
- proof files for new invariants.

## Do not touch unless a blocker is proved

- normal Placement Set ownership;
- Generic placement;
- Settings transaction/store;
- quick settings;
- wheel routing;
- Template addition/library management;
- Workbook schema;
- existing Template association serialization;
- normal placement-engine semantics.

## Suggested commit slices

1. P0 evidence pointer only.
2. source-mode zero-write foundation.
3. immutable capability types.
4. coordinator + cache, no Timeline writes.
5. row-choice projection/performance counters.
6. managed source-union reader/tag.
7. preset planner/application.
8. immediate trial/cross-source replacement.
9. UX/unavailable states.
10. Checkpoint proof fixes only.
11. exact Release prep/docs.

Every product slice keeps Template mode green.

## Threading contract

UI thread only:

- Character/plugin resolution;
- CreateCharacterParameter/CreateFaceParameter;
- property-editor Create/SetBindings/ClearBindings;
- WPF staging visual/layout;
- host-affine TachieFaceItem construction/application;
- final Timeline mutation.

Background only for immutable data:

- candidate grouping/sorting;
- row projection;
- string/fingerprint comparisons that no longer dereference live host/plugin objects.

If implementation wants to cross that line, stop.

## Candidate revalidation contract

A row candidate is a hint, never mutation authority.

At selection time require exact match on:

- current Voice still present;
- current Character/config identity;
- plugin type;
- FaceParameter type;
- module MVID;
- route/property/editor identity;
- candidate identity;
- confidence expectations.

Mismatch = zero-write + explicit refresh guidance.

## Test growth control

Permanent coverage should retain:

- one direct-named route;
- one legacy editor route;
- one modern editor route;
- built-in PSD/Animation bridge where redistribution-safe fixtures allow;
- false-positive rejection;
- broken-editor containment;
- source-switch zero-write;
- cross-source exact replacement;
- performance/cancellation invariants.

Use temporary diagnostics for additional editor shapes and retire them after the invariant is captured.

## P0 implementation decision

State fingerprint is approved for proved bounded candidate routes.

- use the canonical bounded public-state fingerprint in `TachiePresetAssociationTag` and manual-edit detection;
- never substitute unstable `ToString()`, object identity or private state;
- unsupported/untrustworthy third-party state remains local Experimental/None capability rather than weakening the association check.

Pinned proof: Lab PR #62, source `bfe13c6eb304e70401f5d6ba6d467a1ea4d8e1ca`, run `35619789546`, marker `PASS_TACHIE_PRESET_PRODUCT_BRIDGE_P0`.

No architecture question blocks P1.


## P1 implementation result

Source-mode foundation is green on exact YMM4 4.55.1.1 Lite.

Pinned proof:

- source `676491583c54e91e8278d69e1c0b31984feaf7d1`;
- run `35621563372`;
- job `106405620298`;
- artifact `10650011921`;
- artifact SHA256 `c06d6cbc3288dbda10acd02abf92482cc5c66e893bc989a360eac35e95817ca2`;
- marker `TACHIE_PRESET_SOURCE_MODE_P1=PASS`;
- full Checkpoint semantic regression/evidence guards: PASS.

The source mode remains session-local, Template-default and zero-write. Tachie Preset gates Template-only Excel/placement/resync/navigation and cancels stale Template work. Protected imported Template/Excel work blocks source entry without data loss.

P2 should add only immutable capability/fingerprint/route/candidate/result types. Do not move YMM4/plugin/WPF probing into P2.


## P2 implementation result

Immutable capability model is green.

Pinned proof:

- source `7d60808f02bae9450760be8b07aa67ef44a07a95`;
- Focused run `35622493374`;
- job `106408713620`;
- artifact `10649933330`;
- artifact SHA256 `36a8f04a5db1fc3b758968c5108f6c58f9712a8e31cfd35f4e9abda4363f34df`;
- marker `FOCUSED_NATIVE=PASS`.

The P2 model is host-object-free. P3 may populate these descriptors on the UI thread, but must not add live plugin/editor/WPF objects to them or weaken duplicate-identity fail-closed behavior.
