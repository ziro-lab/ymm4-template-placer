# Current / compatibility / historical map

Purpose: prevent future work from mistaking old compatibility code or historical documents for the current primary workflow.

This is a navigation map, not a deletion list.

## Current primary product paths

These areas are part of the accepted normal product behavior.

### Placement core

- `PlacementPlan.cs`
- `PlacementEngine.cs`
- `LayerPlanner.cs`
- `BundleLayerPlanner.cs`
- `TemplateBundle.cs`
- `TemplateCatalog.cs`
- `TemplateLibrary.cs`

Contract: complete preflight before mutation, add-only normal placement, one native Undo unit.

### Current context / Set placement

Primary families include:

- `IntentSelectionContext.cs`
- `IntentPaletteModels.cs`
- `IntentWorkspaceViewModel.cs`
- `IntentPalettePanel.xaml[.cs]`
- `IntentRelationResolver.cs`
- `IntentPlacementGeometry.cs`
- `IntentExecutionPlan.cs`
- `GenericLayerTarget*`
- `GenericSetSettings*`
- `PlacementSetPresentation.cs`

“Intent” is an internal/historical code-family name. The current user-facing model is Context -> Set -> tile.

### Current presentation / shortcuts

- `PalettePresentationSettings.cs`
- `PalettePresentationDraft.cs`
- `PalettePresentationPanel.xaml[.cs]`
- `PalettePositionShortcuts.cs`
- `CompactPresentation.cs`

These settings are global, not Set-local.

### Current Settings safety

- `IntentSettingsDraft.cs`
- `IntentSettingsNavigation.cs`
- `IntentSettingsViewModel.cs`
- `IntentSettingsPanel.xaml[.cs]`
- `SettingsEditTransaction.cs`
- `SettingsSessionIntegration.cs`
- `SettingsAutomaticCommit.cs`
- `PlacerSettingsStore.cs`

Current behavior is protected auto-commit plus session rollback, not manual Save/Discard.

### Current expression workflow

- `AssignmentRow.cs`
- `IntentExpressionCatalog.cs`
- `IntentExpressionPlacement.cs`
- `IntentExpressionImmediate.cs`
- `ExpressionRowNavigation.cs`
- `ExpressionNavigationHost.cs`
- `ExpressionVoiceFreshness.cs`
- `IntentAssociationTag.cs`
- `IntentAssociationResync.cs`
- `ExpressionResync.cs`

The expression workflow is allowed exact managed replacement/removal. This exception does not broaden normal placement.

## Compatibility / secondary paths

These areas still have value for compatibility, migrations, secondary workflows or retained regression behavior. Do not remove them merely because they are not the current normal UI.

Families include:

- legacy/library/palette/Quick Drop models and views such as `Palette*`, `Library*`, `QuickDrop*`;
- Selection-placement families such as `Selection*`;
- older saved Preset/Expression models such as `PresetDraft.cs`, `PresetViewModel.cs`, `ExpressionPreset.cs`;
- Workbook/Excel bridge `Workbook*`;
- explicit Resync compatibility paths;
- bounded LocalAppData -> Portable settings migration and other still-supported serialized data compatibility.

Some files in these families may still be reused by current code. Classification is by responsibility, not filename.

Rules:

- preserve serialized data compatibility;
- preserve current migrations;
- preserve secondary workflows still exposed/used;
- do not delete or rewrite a family without proving reachability and migration consequences;
- a hidden legacy UI is not permission to discard its data model.

## Removed executable compatibility

`LegacyWorkspace` is no longer a product runtime or persisted Settings contract.

The old Palette / Selection workspace is not a selectable Tool mode. Historical JSON containing a `LegacyWorkspace` property cannot enable it.

A proof-only shim may expose legacy-shaped members under `YMM4_PROOF` so retained historical tests can be consolidated safely. That shim is excluded from the distribution build and must not be treated as product compatibility.

Legacy Palette / Selection / QuickDrop / ExpressionPreset families that remain in source are classified by their **current concrete responsibility** (reuse, secondary workflow, migration/association compatibility, or trace evidence), not as evidence that the old workspace still exists.

## Proof / test-only architecture

Production source excludes the large native proof suite unless built with the proof configuration.

Validation responsibilities live under `tests/` and are governed by `docs/VALIDATION_STRATEGY.md`.

Old Round-labeled tests may be consolidated/retired under the explicit retirement rules; their age alone is not a deletion reason.

## Historical design/evidence documents

The repository intentionally retains many versioned/Round/checkpoint documents.

Treat these as historical evidence unless a current authority document explicitly refers to them for an active contract:

- `V0.4.1_*`;
- `V0.4.2_HANDS_ON_ROUND2_*`;
- `V0.4.2_HANDS_ON_ROUND3_*`;
- `V0.4.2_HANDS_ON_ROUND4_*`;
- `W*_NATIVE_CHECKPOINT.md`;
- `WUX*_NATIVE_CHECKPOINT.md`;
- old candidate/work-log documents.

Do not update every historical document when current behavior changes. That would rewrite history and recreate documentation bloat.

Current authority lives in:

- `CURRENT_ARCHITECTURE.md`;
- `GLOSSARY.md`;
- `VALIDATION_STRATEGY.md`;
- active feature handoff/spec;
- `BACKLOG.md`;
- `USAGE.md` for actual user operation.

## Refactor rule

Before removing or consolidating “legacy-looking” code:

1. identify current call sites and serialization use;
2. identify migration/load compatibility;
3. identify native tests whose fixture depends on it;
4. confirm the current primary path has equivalent behavior where required;
5. run Checkpoint;
6. if release/package/runtime behavior is affected, run Release.

Do not perform a broad cleanup only to make names or folders look newer.
