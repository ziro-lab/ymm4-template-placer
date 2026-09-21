# Tachie Preset source — implementation prep

Status: READY AFTER P0

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

## Open implementation decision after P0

State fingerprint:

- if a stable bounded fingerprint is proved, use it in TachiePresetAssociationTag and manual-edit detection;
- otherwise explicitly omit it rather than serializing unstable ToString/object identities.

No other architecture question is expected to block P1.
