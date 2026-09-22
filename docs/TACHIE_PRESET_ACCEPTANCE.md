# Tachie Preset source — acceptance

Status: FROZEN

## P0 product-bridge acceptance

Before product source changes:

- P0-1: exact current Character resolves to the intended active tachie plugin/configuration without assembly-wide scanning.
- P0-2: public legacy and modern property-editor binding routes compile and run against YMM4 4.55.1.1; modern ItemProperty[] construction is proved.
- P0-3: built-in Animation and PSD fresh FaceParameters enumerate at least one usable preset and candidate application mutates fresh state.
- P0-4: fresh TachieFaceItem accepts the applied fresh FaceParameter and retains expected state through the intended product construction path.
- P0-5: temporary editor bindings/visuals clear on success, failure and cancellation without persistent UI artifacts.
- P0-6: deterministic state fingerprint is either proved for bounded fixtures or explicitly rejected before product implementation.

## Source-mode acceptance

- A1: every new Tool/session starts Template.
- A2: switching Template <-> TachiePreset alone performs zero Timeline and settings writes.
- A3: Template mode retains accepted v0.4.2 behavior and performance invariants.
- A4: protected pending Template/Excel work blocks entering TachiePreset without discarding it.
- A5: returning Template cancels preset work safely.
- A6: source mode is not persisted in this round.

## Capability acceptance

- B1: discovery is limited to distinct current Voice Characters while expression + TachiePreset is active.
- B2: no preset capability scan occurs on plugin open, placement tab or Settings tab.
- B3: direct-named route requires exact writable Preset string + coherent Character-side definitions + dry-run mutation.
- B4: built-in Animation editor route works.
- B5: built-in PSD editor route works.
- B6: modern property-editor route works.
- B7: unrelated Preset-like noise is rejected.
- B8: broken editor is isolated to its Character/candidate.
- B9: duplicate/ambiguous candidate identity fails closed.
- B10: cache is session-local and fingerprint-invalidated.

## Performance acceptance

- C1: capability scan count scales with distinct Characters, not Voice count.
- C2: 100/500/1000 Voice Template-mode performance invariants remain unchanged.
- C3: no mutable WPF/YMM4/plugin object is touched from background preparation.
- C4: stale/cancelled preset discovery/preparation cannot publish.
- C5: rapid TachiePreset -> other tab cancels nonessential work; the other tab stays usable.
- C6: unchanged fingerprints reuse session cache instead of rescanning every Voice.
- C7: one-Voice Serif/Frame/Length/Layer edits preserve existing incremental behavior when source capability did not change.

## Row/UI acceptance

- D1: one Voice row collection/DataGrid is used in both source modes.
- D2: preset candidates are not represented by fake FaceTemplates/Intent entries.
- D3: valid current association from the other source is shown truthfully, not as corruption.
- D4: disappeared same-source candidate is explicit unavailable state; no first-match healing.
- D5: no-capability Character has a local clear state and does not disable other Characters.
- D6: TachiePreset mode exposes existing ExpressionPreset geometry as 配置ルール.
- D7: Excel UI is unavailable in TachiePreset mode with a clear reason.

## Placement acceptance

- E1: selection re-resolves current Character/plugin and creates a fresh FaceParameter.
- E2: dry-run FaceParameter is never inserted into Timeline.
- E3: stale plugin/fingerprint/candidate fails before mutation.
- E4: a fresh TachieFaceItem is built for current Character and receives the newly applied FaceParameter.
- E5: time placement uses existing ExpressionPreset/CharacterExpressionProfile semantics.
- E5a: the default layer mode inherits the first applicable Voice expression Set's RelativeLayerPolicy; normal up/down offset and same-direction bounded collision search match ordinary Voice-targeted Template placement.
- E5b: an explicit relative override may independently choose Voice-up/Voice-down, offset and bounded search range.
- E5c: an explicit absolute override may target a numeric layer and choose occupied behavior: do not place, search up or search down; retained legacy bounded search remains readable for migrated settings.
- E5d: generated TachieFaceItem constructor Layer is never treated as an implicit destination.
- E5e: untouched historical default settings migrate to Voice-Set inheritance; customized historical numeric layer settings retain absolute-placement meaning.
- E6: complete preflight precedes mutation.
- E7: successful immediate choice participates in the existing one logical native trial Undo.
- E8: Preview/viewport confirmation uses the existing root navigation coordinator.

## Managed association acceptance

- F1: existing Template IntentAssociationTag format is unchanged.
- F2: TachiePreset uses a versioned separate descriptor.
- F3: common managed reader recognizes exactly one source kind and exact members.
- F4: Template -> TachiePreset replacement is atomic.
- F5: TachiePreset -> Template replacement is atomic.
- F6: exact removal of a managed TachiePreset expression works.
- F7: copied/missing/duplicate/mixed source tags fail closed.
- F8: manual/unassociated expressions remain untouched.
- F9: manual edit/state mismatch is detected if P0 fingerprint support is approved.
- F10: Resync truthfully skips unsupported TachiePreset associations in this first round unless explicit preset Resync is separately proved.

## Excel/settings acceptance

- G1: Workbook schema remains unchanged.
- G2: no TachiePreset candidate is exported/imported.
- G3: no new product settings schema is required merely for source mode.
- G4: existing ExpressionPreset persistence/validation/rollback remains protected.

## Regression gates

- Focused for ordinary implementation.
- Checkpoint when capability + source-union association + cross-source replacement are complete.
- Release only when whole feature candidate is ready.
- Final owner Hands-on includes built-in Animation/PSD and at least one available real-world plugin path.
