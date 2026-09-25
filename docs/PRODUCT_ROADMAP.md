# Product roadmap

This document is the sequencing authority for product work after the accepted **v0.5.0** baseline.

It is intentionally separate from:

- `CURRENT_ARCHITECTURE.md`, which describes the current accepted product/architecture;
- `BACKLOG.md`, which collects unimplemented/deferred ideas without assigning implementation order;
- feature-specific DESIGN / ACCEPTANCE / WORKPLAN documents, which become authoritative only when that feature intentionally starts.

The order below may be refined by Hands-on results, but do not silently reorder or combine phases merely because related ideas exist in the Backlog.

## Product principle for Settings UX

Template Placer deliberately moves repeated editing decisions into one-time setup.

That means a wide configuration surface is not automatically a defect. The important UX problem is **discoverability and comprehension**:

- users should be able to tell what can be configured;
- users should be able to predict where to change a behavior;
- current settings should explain the resulting placement behavior;
- advanced capability should not be removed merely to make the Settings surface look smaller.

Prefer reducing **configuration friction**, not hiding configuration capability.

First-level sections should generally expose their existence. Fold only genuinely detailed/exceptional controls when that improves scanning without hiding a capability.

Important actions may have more than one entry point when that materially improves discoverability. In contrast, fully redundant workflows that add no capability should be candidates for removal.

All Settings surfaces must continue to edit the same staged Settings Draft and protected persistence path. Do not create a beginner schema, alternate Settings model, or second persistence route.

## Current baseline — v0.5.0

v0.5.0 is the integrated baseline.

The Tachie Preset expression-source path is accepted as normal product functionality, including persisted Template/TachiePreset source selection, bounded public preset discovery, exact managed replacement and the retained PlacementPlan/native Undo / Settings safety architecture.

Known follow-up work such as built-in Tachie Preset loading performance remains deferred unless measurement shows it should interrupt the sequence below.

## Planned sequence

### 1. Portable Settings storage — COMPLETE

Portable Settings is merged into main.

Authoritative path:

```text
<YMM4>/user/plugin/Ymm4TemplatePlacer/Data/settings-v04.json
```

Accepted behavior:

- if Portable exists, it is authoritative;
- valid legacy-only LocalAppData Settings migrate safely to Portable;
- the legacy file is retained as backup;
- differing/changed legacy data does not override a valid Portable file;
- corrupt Portable never silently falls back to legacy;
- first migration retains validation, atomic replacement, digest/external-change protection and cross-instance locking;
- real `.ymme` update preservation is backed by public Lab/native evidence;
- Release/native regression is GREEN.

### 2. Expression Source / Placement Model unification — COMPLETE

**Accepted product phase. P0-P8 are complete; Release #615 is GREEN at the exact product/test/package source.**

v0.5.0 proved both Template expressions and Tachie Presets as usable content sources, but they still enter placement through different product paths.

Explicitly review whether Tachie Presets can become reusable placement sources rather than remaining primarily an expression-list-only source.

Question to answer:

> Can Template and Tachie Preset be represented as different source kinds that both materialize fresh item(s) and then use the same Set-owned placement rule?

Candidate model:

```text
Placement Source
├─ YMM4 Template
│    └─ strict TemplateLocator
└─ Tachie Preset
     └─ explicit plugin/surface/preset identity

        ↓ materialize fresh item(s)

common Placement Rule
        ↓
PlacementPlan / native Undo
```

Desired product result if the model proves sound:

- Tachie Preset entries can be used from ordinary Sets/tile surfaces, not only from `表情をまとめて`;
- one expression Set may contain both Template and Tachie Preset source entries;
- source identity and placement behavior are separate concepts;
- the expression list remains a high-throughput assignment UI rather than the only place where Tachie Presets can be used;
- common placement behavior can move toward the Set instead of being split between `IntentRelation` and independent `ExpressionPreset` geometry.

Safety boundaries:

- **do not fake Tachie Presets as YMM4 ItemTemplates**;
- do not serialize a generated `TachieFaceItem` / FaceParameter body as a second Template database merely to make the types look uniform;
- persist a thin resolvable preset-source identity/recipe and materialize a fresh current item at execution time;
- preserve strict source re-resolution, current Character/plugin/config validation and exact managed-association guarantees;
- keep unsupported/ambiguous preset sources as local failures;
- begin Character-bound if that is the safest stable identity; cross-Character source reuse requires separate evidence.

The design review result is **converge behind one explicit Placement Source abstraction** while keeping Template and Tachie Preset identities explicit.

Current authorities:

- `PLACEMENT_SOURCE_UNIFICATION_DESIGN.md`;
- `PLACEMENT_SOURCE_UNIFICATION_ACCEPTANCE.md`;
- `PLACEMENT_SOURCE_UNIFICATION_WORKPLAN.md`.

Frozen direction:

- keep existing Template Library storage and serialized Template Source IDs compatible;
- add thin Character-bound Tachie Preset Source locators rather than fake ItemTemplates;
- materialize source-specific fresh item(s) first, then use one Set-owned `IntentRelation` geometry path;
- keep existing v0.5 Template and TachiePreset association readers compatible;
- use a backward-compatible new preset association version for Set-owned preset expressions;
- keep standalone `ExpressionPreset` only as compatibility while the new common path is proven.

Implemented candidate:

- ordinary Character-bound Sets can place registered Tachie Preset sources beside Template sources;
- ExpressionCandidates Sets can mix registered Template/Preset content;
- registered presets use the owning Set's `IntentRelation` in ordinary placement, expression replacement and geometry Resync;
- a backward-compatible preset association v2 preserves exact Set/Source/state identity while old v0.5 preset tags remain readable;
- detection alone does not persist a Source; `Setへ登録` is explicit;
- registered sources appear in Settings without eager plugin discovery;
- Template/TachiePreset source mode remains a visibility/filter choice;
- the old standalone `ExpressionPreset` path remains only for unregistered direct preset placement compatibility.

Release #615 (`35834323751`) at `7ffe3b2c690a57850c77821f7b4c3eeb6517b3dc` passed **1,763 Native assertions**, P0-P7 source gates, full retained semantic/evidence regression, exact distribution-DLL smoke and verified `.ymme` / source / provenance packaging. Phase 2 is complete.

Exit:

- ordinary Sets can place registered preset sources;
- ExpressionCandidates Sets can mix Template and preset sources;
- new registered preset placements use Set-owned `IntentRelation`;
- existing v0.5 settings/tags stay safe and readable;
- full Native/Release acceptance is GREEN.

### 3. Placement rule inventory / bounded model completion — COMPLETE

Before implementing a visual behavior preview, decide whether the near-term placement vocabulary is complete enough to explain.

Review the current model and the Backlog together.

Inventory result:

- implement the missing **center alignment** by extending the existing `IntentAlignment`; do not add a duplicate general Source Pivot model;
- implement **Absolute Layer** as an additive mode on the existing bounded layer policy;
- current Target contracts are already sufficient before Preview; stronger target grammar is deferred;
- current previous/next same-type / same-Character Neighbor selectors are already sufficient before Preview; arbitrary/typed neighbor grammar is deferred;
- Composite Placement Steps and Fan-out are deferred because they change output multiplicity and deserve a separate design after the one-source Preview model is understood.

Accepted authorities:

- `PLACEMENT_RULE_COMPLETION_DESIGN.md`;
- `PLACEMENT_RULE_COMPLETION_ACCEPTANCE.md`;
- `PLACEMENT_RULE_COMPLETION_WORKPLAN.md`.

Rule:

> Do not implement every collected Placement Recipe idea before Preview. Add only the placement axes that are likely to be part of the near-term normal product vocabulary.

Also use this phase to finish the Expression Source decision from Phase 2. A Preview should not be built against a placement model that is already expected to be reorganized immediately afterward.

Release #629 (`35857198385`) at exact source `5bfbccb5dd81071662ba756ec27591519ff98e19` passed **1,787 Native assertions**, P1-P3 explicit gates, full retained regression, exact distribution-DLL smoke and verified packaging. Phase 3 is complete.

Exit:

- the near-term placement vocabulary is explicitly listed as implemented now / implement before Preview / defer;
- source vs. placement responsibilities are frozen for the next UX phases;
- any pre-Preview placement additions are Native GREEN;
- the Preview/Checklist can consume one authoritative placement-description model without source-specific geometry forks.

### 4. Compact Settings friction / discoverability pass — COMPLETE

Improve the existing Settings surface after the source/placement model is understood, but before introducing a separate large workspace.

Primary goal:

> Make it obvious **where to go to change a behavior** without reducing the available configuration width.

Active authorities:

- `COMPACT_SETTINGS_FRICTION_DESIGN.md`;
- `COMPACT_SETTINGS_FRICTION_ACCEPTANCE.md`;
- `COMPACT_SETTINGS_FRICTION_WORKPLAN.md`.

Hands-on Round 1 status:

- PR #31 first candidate / Checkpoint #638 is Native GREEN with 1,800 assertions;
- the overall visible-first-level direction is accepted for continued refinement;
- a bounded corrective pass remains before Phase 4 can close;
- move Set shape to the bottom because placement quick settings are the primary route;
- remove the duplicate top Set delete and keep deletion in Set management;
- group Set management and cross-Item copy without forcing dense one-row compression;
- align equivalent list lanes, at minimum `演出と並び順` with the Template list lane;
- preserve comfortable margins for the floating/resizable Tool workflow;
- never use horizontal scrolling/hiding as the narrow-width solution: wrap text and control groups vertically;
- fix ordinary white-space/header wheel dead zones while retaining intentional inner-control wheel ownership.

Phase 5 Preview / Checklist remains blocked until this corrective pass is Native GREEN and a second real YMM4 hands-on pass accepts the remaining Settings friction.

Direction:

- remove first-level folding where it hides the existence of ordinary Set capabilities;
- keep major destinations visible, including `どのアイテムで使うか`, `どう置く？`, `演出と並び順`, `セットの管理` and `テンプレートをまとめて追加`;
- keep only genuinely detailed controls behind bounded secondary disclosure;
- preserve the current human-readable `このセットの動き` summary;
- improve labels, grouping and spacing based on actual Hands-on use rather than minimizing vertical length;
- retain duplicate entry points when they solve a real discoverability problem.

Also review truly redundant workflows. Current candidate:

- remove the single-Template registration path if `テンプレートをまとめて追加` completely subsumes it, including the one-item case.

Do not remove independent placement axes merely to shorten the screen.

Exit:

- ordinary Set capabilities are discoverable without opening multiple first-level containers;
- the common source/placement model from Phases 2-3 is understandable in Compact Settings;
- bounded Hands-on at narrow and normal Tool widths identifies no major navigation ambiguity;
- no second Settings model or persistence route is introduced.

Final Phase 4 result:

- user Hands-on accepted the final compact Settings candidate;
- exact accepted implementation/test/package source: `05285a7bae1eb59b4b588c565aa46e29bdea795c`;
- Release #654 (`35897405826`) passed **1,822 Native assertions**, `COMPACT_SETTINGS_P1` through `P7`, full retained regression, exact distribution-DLL smoke and verified `.ymme` / source / provenance packaging;
- whole-Tool gray-surface wheel routing is active only while Settings is selected, while intentional inner control ownership remains protected.

Phase 4 is complete. Phase 5 is the next product phase.

### 4.5. Baseline simplification / compatibility cleanup — COMPLETE

Before adding Behavior Preview, simplify the pre-publication baseline while there is only one known user and no announced compatibility commitment.

Primary goals:

- remove executable legacy product paths, starting with `LegacyWorkspace`;
- keep old-data migration separate from old-UI/runtime preservation;
- retain association/source/settings safety compatibility;
- audit legacy Selection/Palette/ExpressionPreset families for current reachability;
- consolidate historical Native tests now that assertion growth crossed the validation strategy's review threshold.

Authorities:

- `BASELINE_SIMPLIFICATION_DESIGN.md`;
- `BASELINE_SIMPLIFICATION_ACCEPTANCE.md`;
- `BASELINE_SIMPLIFICATION_WORKPLAN.md`.

Behavior Preview remains sequenced after this maintenance phase so new UI does not build on top of known legacy runtime branches.

Current maintenance result:

- executable `LegacyWorkspace` persistence/switching is removed from distribution/runtime;
- current placement, Settings and expression surfaces are unconditional;
- compatibility retained is bounded to current data/Timeline/source/Settings responsibilities rather than a second product mode;
- historical workflow validation is consolidated behind current named invariants;
- Checkpoint #691, Release candidate #707 at exact source `800b9bd261180f016ab3c1f26e67df1b55fd1eda`, and main Release #708 at merge `af6a667e401a99311ef58293c750ac51b9145f54` are GREEN with **1,198 Native assertions / 0 failures**;
- exact distribution-DLL smoke and verified `.ymme` / source / provenance packaging are GREEN;
- validation volume is down from 1,822 at the accepted Phase 4 baseline by about **34.2%**;
- Phase 4.5 is complete.

### 5A. Behavior preview — COMPLETE

Behavior Preview v2 is implemented, Release GREEN and owner hands-on accepted.

Accepted product result:

- one shared `PlacementBehaviorDescription` feeds the existing `このセットの動き` text and the read-only visual projection;
- applicable Targeted Sets show a compact Timeline-like relationship diagram;
- block position/length communicates target span, independent duration, related-start/end boundaries and start/center/end alignment;
- target-relative Up/Down is visible through vertical block order;
- Absolute Layer is shown truthfully as an explicit layer hint rather than a fabricated target-relative distance;
- Generic Sets do not force a redundant full diagram;
- Preview reads the staged Draft and performs zero Timeline/Settings mutation;
- PairBoundary and Settings spacing were simplified through the final hands-on correction pass.

Final tested code/package source: `6ed136abcb9b6a3346e32d1b0661c5aed98d69b3`.

Release #747: **1,254 Native assertions PASS / 0 FAIL**, exact distribution-DLL smoke and verified packaging GREEN.

Authorities:

- `BEHAVIOR_PREVIEW_DESIGN.md`;
- `BEHAVIOR_PREVIEW_ACCEPTANCE.md`;
- `BEHAVIOR_PREVIEW_WORKPLAN.md`.

The Preview materially reduces the need for a second “explain the settings” workflow. Therefore the old automatic sequence **Preview -> checklist -> Full Settings Workspace** is retired.

### 5B. Native Undo / Redo surface — COMPLETE

The placement surface now exposes compact persistent **YMM4-native Undo / Redo** controls.

Implemented result:

- two 30x26 controls live in the existing placement context header without adding a new header row;
- commands resolve through `CommandSettings.Default[CommandType.Undo/Redo]` and execute against the YMM4 main window;
- enabled state follows the real routed-command availability;
- `TimelineToolInfo.UndoRedoManager.Recorded / Undoed / Redoed` refreshes the product command state;
- there is no Template-Placer-only history, synthetic Ctrl+Z/Ctrl+Y input or “delete the last placement” behavior;
- PlacementPlan/native Undo remains the only placement history authority.

Canonical host evidence already existed in Lab PR #139 for the YMM4 standard-command route on 4.55.1.1 Lite and 4.56.1.0 Lite. Product Release #760 then verified the integrated surface at exact tested source `d12b4057e8d4cd1b79f009804cd280d96c38c148`.

Release #760 / run `36068432875`:

- **1,263 ASSERT PASS / 0 FAIL**;
- `NATIVE_UNDO_REDO_P0=PASS`;
- `NATIVE_UNDO_REDO_P1=PASS`;
- real placement -> header Undo -> header Redo -> header Undo roundtrip preserves the exact expected Timeline states;
- distribution build and proof build: **0 warnings / 0 errors**;
- exact distribution-DLL smoke PASS;
- PackageVerified PASS;
- Release artifact `10836644101`, uploaded artifact SHA256 `97543721d72828330e459a78015ec8a712087e0e32596a32dae1509e2fb5a1a7`;
- distribution DLL SHA256 `185ff470679328526fec84355b66174c4d4c0d6e9736ea3a7cb3be7405cc0fa7`.

Docs-only closeout commits after the tested source do not constitute a newer tested product build.

### 5C. Placement quick settings — FIRST SLICE RELEASE GREEN / OWNER HANDS-ON NEXT

Primary question:

> Which small subset of placement settings is changed often enough during editing that it deserves a fast path beside the normal tile workflow?

Active authorities:

- `PLACEMENT_QUICK_SETTINGS_DESIGN.md`;
- `PLACEMENT_QUICK_SETTINGS_ACCEPTANCE.md`;
- `PLACEMENT_QUICK_SETTINGS_WORKPLAN.md`.

The first design candidate reuses the existing `⚙ 簡易設定` Popup, edits the same `IntentSettingsSession / IntentPaletteDraft`, and limits the first slice to finite valid result-oriented actions plus the accepted read-only Preview.

This is **not** a second Settings mode.

Direction:

- edit the same staged Settings Draft used by Compact Settings;
- reuse the accepted Behavior Preview for immediate read-only feedback;
- expose only high-frequency placement axes proven useful by hands-on use;
- candidate axes may include anchor/alignment, duration relation and layer relation/direction, but the final subset is intentionally not frozen yet;
- keep detailed applicability, source management, bounds/fallback and Set management in normal Settings unless use proves they belong in the quick surface;
- avoid duplicating validation, persistence or placement semantics.

The design should optimize “small correction before the next placement”, not reproduce the full Settings screen in miniature.

First-slice implementation is Release GREEN at exact tested source `9dae643cb9c29780783dde265d41dc4a90f01a62` by Release #770 / run `36076134931`: **1,277 ASSERT PASS / 0 FAIL**, `PLACEMENT_QUICK_SETTINGS_P0/P1/P2=PASS`, exact distribution-DLL smoke and PackageVerified PASS.

The implemented slice is intentionally bounded to start/center/end alignment, target-span/template duration and one-layer up/down on Targeted Sets. P3 owner hands-on is next; do not add neighbor compound recipes or numeric quick inputs before that review.

Exit:

- a bounded quick-setting vocabulary is chosen from actual repeated edits;
- every quick control maps directly to an existing Draft field/finite option;
- Preview updates from the same Draft;
- Compact Settings remains the complete ordinary configuration surface.

### 5D. “What I want” checklist — DEFERRED / RE-EVALUATE

The checklist is no longer the automatic next phase.

Reason:

- Behavior Preview now explains placement combinations much better than the original pre-Preview plan assumed;
- placement quick settings may solve the remaining frequent-edit problem with much less UI and state;
- a second outcome-oriented editing workflow would add meaningful interaction/state complexity.

Keep the prior safe concept only as a fallback:

```text
current Settings Draft
-> temporary checklist draft
-> preview / textual result
-> explicit apply
-> current Settings Draft
```

If later hands-on evidence shows a real unresolved comprehension/input problem, the checklist may return as an editing projection of the same Draft. Cancel must remain zero-change and no second schema/persistence route may appear.

### 6. Full Settings Workspace (“Settings mode”) — DEFERRED / NEED-DRIVEN

Do not build a separate large Settings workspace merely because it was on the old sequence.

Re-evaluate only when real usage shows that many Sets/tiles make Compact Settings plus existing filtering/search materially difficult to manage.

If that threshold is reached:

- edit the same model, staged Draft and protected persistence route as Compact Settings;
- reuse Behavior Preview and any accepted quick-setting vocabulary;
- solve large-scale navigation/comparison/management rather than recreating normal Settings;
- do not introduce a second schema, source model, placement engine or persistence route.

Until then, Compact Settings remains the complete configuration surface and the larger workspace is parked.

## Items that do not automatically interrupt this sequence

The Backlog still contains useful extensions such as Placement Recipe axes, Action Tiles and performance polish.

Their presence is not implementation priority by itself.

Unless a bug, regression or measured performance issue requires otherwise, prefer completing the Settings/storage sequence above before broadening the product with additional placement capability.
