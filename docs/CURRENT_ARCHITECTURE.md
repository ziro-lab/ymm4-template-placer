# Current architecture

This is the current product/architecture authority for YMM4 Template Placer.

Historical Round/W documents remain evidence of how the product reached this state. They are not instructions to rebuild superseded UI or architecture.

## Product role

YMM4 Template Placer is a **safe placement tool**.

Its normal responsibility is:

```text
read explicit Timeline context
-> evaluate one saved finite placement rule
-> resolve the referenced live YMM4 Template
-> clone new item(s)
-> decide Frame / Length / Layer
-> preflight the complete operation
-> if any required plan fails: write nothing
-> otherwise commit the complete addition with one native Undo unit
```

The core boundary is:

> Use the relation to an existing target/context to place new content safely. Do not turn Template Placer into a general transformer of existing Timeline content.

## Source of truth

Live YMM4 Item Templates are authoritative.

The plugin stores thin references, aliases, finite placement settings and presentation metadata. It does not maintain a second copy of template bodies.

A missing or ambiguous template reference fails closed. Do not fuzzy-match by name, order, text or proximity.

## Normal placement safety

Normal placement is add-only.

It may:

- read selected/current Timeline context;
- clone referenced Template bundles;
- set planned Frame / Length / Layer on clones;
- search a bounded layer direction when that saved rule explicitly permits it;
- add the fully prepared result.

It may not:

- move unrelated existing items;
- shorten unrelated existing items;
- delete unrelated existing items;
- modify effects/properties/volume on existing items;
- invent a target when context is ambiguous.

All required members of a multi-item bundle are planned before mutation. Planned items reserve occupancy against each other before commit.

## Current normal UI model

Normal placement is:

```text
Timeline context
-> applicable Set
-> tile
-> placement
```

The Set owns applicability and placement relation. Tiles primarily identify a referenced Template plus small appearance/parameter overrides.

For **normal targeted Sets**, the Item type is the ownership parent:

```text
one Item type
-> its Sets
-> each Set's tiles
```

A newly created normal targeted Set has exactly one `Target.ItemTypeKey` and `UniformType`. The existing serialized target shape is retained; no second owner field exists.

Cross-Item reuse is explicit snapshot copy. Copying a Set creates a new Guid under the destination Item type and copies the current relation/conditions/entries/appearance. Source and destination are independent afterward.

Existing settings that contain multi-type Sets are preserved losslessly and retain their existing runtime matching semantics. Settings surfaces them only through the bounded `複数種類` compatibility context; normal creation does not create new shared multi-type Sets.

Generic Sets remain a separate existing model.

Current global presentation state is shared across Sets:

- Auto / Fixed layout;
- fixed column count;
- position shortcuts;
- expression viewport-follow behavior;
- common Voice row height.

These are not Set-local settings.

## Single current workspace

The product has one normal executable workspace path.

`LegacyWorkspace` is not a persisted Settings field and there is no product command/session switch that returns the Tool to the old Palette / Selection workspace. Placement, Settings and expression operation use the current Context -> Set -> tile / current Settings / current expression surfaces unconditionally.

Historical JSON may still contain an unknown property named `LegacyWorkspace`; it does not reactivate old behavior and naturally disappears when current Settings are written again.

`LegacyProofShims.cs` is compiled only for the proof build while historical tests are consolidated. It is not part of the distributable runtime contract.

Legacy-looking class/file names do not by themselves mean a second product mode still exists. Some older Palette/Selection/QuickDrop/ExpressionPreset pieces remain where current reuse, secondary behavior or bounded compatibility still gives them a concrete responsibility.

## Generic placement

Generic/time-position placement uses Timeline.CurrentFrame as the time context.

Its current layer rule is an explicit numeric target with one of the finite occupied-layer behaviors:

- do not place;
- search upward (smaller layer numbers);
- search downward (larger layer numbers).

No clicked-screen-coordinate layer inference is used.

## Settings model

Settings use a staged working draft and protected automatic persistence.

A Settings session keeps:

1. immutable opening baseline;
2. current working draft, including temporarily invalid text;
3. latest successfully committed fingerprint.

Valid complete edits are coalesced and persisted through the protected settings store. Invalid/incomplete edits remain visible and are not persisted.

`今回の変更を戻す` restores the exact session-opening settings state through the same protected store.

Settings persistence must keep:

- validation;
- atomic replacement;
- disk digest/external-change protection;
- cross-instance locking;
- no silent rebase/retry over a conflict.

The authoritative product settings file travels with the YMM4 plugin installation:

```text
<YMM4>/user/plugin/Ymm4TemplatePlacer/Data/settings-v04.json
```

Migration from the historical LocalAppData file is one-way and fail-closed:

- if no portable file exists and the legacy file is valid, copy its validated bytes to the portable location and retain the legacy file;
- once the portable file exists, it is the sole authority and legacy LocalAppData contents no longer override or block it;
- if both exist and differ, load the portable file;
- if the authoritative portable file is corrupt, never silently fall back to the legacy file;
- if the legacy file is corrupt before first migration, reject it and create no portable settings file;
- first migration uses the same cross-instance lock/atomic-write discipline as normal persistence.

The `.ymme` package must never own or overwrite `Data/settings-v04.json`.

`PlacerSettingsStore` is an intentional product boundary, not a temporary substitute for generic plugin-settings helpers. Do not replace it mechanically with host helpers such as `SettingsBase<T>`. The current product contract additionally requires complete-draft/schema validation, the 1 MiB guard, fail-closed handling for corrupt/future settings, digest-based external-change detection, cross-instance locking, atomic replacement and exact Settings-session rollback. Revisit this choice only if a host API is proved to preserve the same conflict-aware/fail-closed contract without weakening those guarantees.

Settings operations do not mutate Timeline or original YMM4 Templates.

## Accepted Compact Settings surface

Compact Settings friction/discoverability is accepted by Release #654 at exact source `05285a7bae1eb59b4b588c565aa46e29bdea795c`.

Current UI contract:

- ordinary Set capabilities are visible at first level; only genuinely detailed/global controls remain folded;
- the top Set picker keeps Set selection and `＋`; destructive Set deletion is centralized under `セットの管理`;
- Set management and cross-Item copy are aligned peer blocks and may reflow vertically at narrow widths;
- equivalent lists and their action rows use consistent horizontal lanes;
- Set shape remains available at the bottom of full Settings, while placement quick settings are the high-frequency route;
- the compact surface preserves useful whitespace and never requires outer horizontal Settings scrolling to reach required content;
- text and control groups reflow vertically when width becomes insufficient;
- the authoritative outer Settings scroller owns ordinary wheel behavior across the white Settings surface and, while the Settings tab is active, ordinary gray Template Placer Tool space;
- ComboBox, RangeBase and independently scrollable inner surfaces retain their own wheel ownership;
- whole-Tool Settings wheel routing is disabled when another task/tab is active.

This is presentation/input routing only. It introduces no second Settings model, persistence path or placement semantics.

## Expression workspace

`表情をまとめて` is a specialist high-throughput workflow, not permission to make normal placement destructive.

Current Template-source behavior may atomically replace/remove only an **exactly identified Plugin-managed expression bundle** for one Voice after complete preflight.

It must never infer or delete a manual/unassociated expression by position, text or order.

Voice row navigation has one authoritative root-owned route:

```text
close prior trial boundary
-> set Timeline.CurrentFrame
-> select Voice
-> public Preview seek when available
-> viewport follow independently when available
```

Rapid navigation is latest-wins.

Excel remains a secondary assignment bridge. Import alone does not mutate Timeline.

## Placement engine boundary

The authoritative mutation path remains:

```text
UI intent
-> root/coordinator admission
-> resolver/planner
-> complete preflight
-> PlacementPlan
-> native YMM4 Undo
```

Do not create a second placement engine or custom Undo stack for a new feature.

## Future Placement Recipe boundary

The following is the accepted **future design boundary**, not a statement that all features below are implemented.

A future Placement Recipe may contain one or more finite Placement Steps.

Every Step must read the same immutable operation-start Context.

```text
freeze Context
-> evaluate Step A
-> evaluate Step B
-> evaluate Step C
-> preflight every result together
-> any required failure = zero write
-> success = one atomic PlacementPlan/native Undo operation
```

A Step must not consume the result of a previous Step.

Allowed future placement axes include finite additions such as:

- a separate arbitrary source pivot only if start/center/end alignment plus offsets prove insufficient;
- Composite Placement Steps;
- stronger finite Target conditions;
- Fan-out over explicit selected targets/boundaries;
- stronger finite Neighbor selectors.

This does **not** authorize:

- arbitrary C# expressions;
- DSL/node graphs;
- loops;
- conditional branching on previous Step output;
- AI/fuzzy target choice;
- existing-item transformation.

## Current post-v0.5 baseline

The accepted product baseline is v0.5.0 plus completed Portable Settings, Placement Source unification, bounded Placement Rule completion, Compact Settings friction cleanup and Baseline Simplification. Placement Source unification was proven by Release #615. The placement vocabulary was promoted at exact product/test/package source `5bfbccb5dd81071662ba756ec27591519ff98e19` by Release #629 (`35857198385`) with **1,787 Native assertions** and verified distribution/package gates. Baseline Simplification was promoted on main at merge `af6a667e401a99311ef58293c750ac51b9145f54`; main Release #708 (`35983059810`) passed **1,198 Native assertions / 0 failures**, exact distribution-DLL identity smoke and verified `.ymme` / source / provenance packaging.

The normal product runtime is singular: the superseded `LegacyWorkspace` persisted/session switch is removed from distribution/runtime. Historical legacy-shaped members may remain only in proof builds or in code with a concrete current compatibility/reuse responsibility; they are not a second product mode.

The baseline includes:

- Item-owned targeted Sets, Generic placement, finite collision rules and shared PlacementPlan/native Undo;
- protected automatic Settings persistence and rollback using the portable plugin-local store;
- lazy/cancelable expression loading with 100 / 500 / 1,000 Voice structural performance coverage;
- explicit Placement Sources with existing Template Library entries and thin Character-bound registered Tachie Preset locators kept as separate source kinds;
- one bounded SourceId namespace across Template and registered Tachie Preset sources without rewriting existing serialized Template IDs;
- fresh source-specific materialization followed by one authoritative Set-owned `IntentRelation` geometry path;
- target Anchor timing alignment through additive start / center / end `IntentAlignment`, with existing offsets applied after alignment;
- targeted Set layer reference through existing target-relative placement or explicit Absolute Layer, preserving normalized multi-item offsets and bounded one-direction collision search;
- ordinary applicable targeted Sets able to place registered Tachie Preset tiles beside Template tiles;
- ExpressionCandidates Sets able to mix Template and registered Tachie Preset sources;
- Template/TachiePreset source mode retained as visibility/filter UX only, not as a placement-engine switch;
- explicit `Setへ登録` source persistence; discovery alone never writes a registered source;
- exact managed association and atomic cross-source replacement for plugin-managed expressions only;
- backward-compatible readers for existing Template v1 and TachiePreset v1 associations plus the Set-owned registered-preset association v2;
- registered-preset Resync that recomputes only Frame/Length/Layer from the owning Set and fails closed after source or FaceParameter edits;
- the old standalone `ExpressionPreset` geometry retained only for unregistered direct-preset compatibility;
- Template-only Excel behavior unchanged.

Registered Tachie Preset sources persist only bounded re-resolvable identity metadata. They do not persist generated `TachieFaceItem`, FaceParameter bodies, PSD/preset snapshots, Timeline references or a fake second Template database.

The accepted preset/source performance boundary remains:

- no capability work on plugin open, placement tab or Settings tab;
- no eager all-plugin/all-Character scan;
- registered sources resolve only when placement or an explicit source operation requires them;
- expression discovery scans only distinct current Voice Characters while that task/source is active;
- WPF/property-editor work remains UI-thread-affine and cancelable;
- immutable descriptors feed the existing row/preparation pipeline;
- stale/cancelled results never publish;
- one incompatible plugin/Character/source is a local fail-closed result.

Built-in preset discovery/loading is functionally accepted but has a deferred performance-polish item. Measure the real cost before changing the discovery/cache architecture.

The Placement Source and Placement Rule completion design/acceptance/workplan documents are retained as accepted implementation and compatibility authority, not as future implementation gates.

## Validation

Read `docs/VALIDATION_STRATEGY.md`.

- Focused: ordinary development feedback;
- Checkpoint: complete semantic/history/evidence regression at a feature checkpoint;
- Release: Checkpoint plus exact distribution DLL smoke and verified packaging.

Historical tests are not permanent merely because they belong to an old Round. They may retire only under the lifecycle/retirement rules in the validation strategy.

## Document authority

For current work, prefer documents in this order:

1. this file;
2. `docs/GLOSSARY.md`;
3. `docs/VALIDATION_STRATEGY.md`;
4. current accepted feature authorities, including the Placement Source and Placement Rule completion design/acceptance/workplans, `docs/FINAL_HANDS_ON_POLISH.md` and the expression-performance design/acceptance documents;
5. the next active feature's frozen design/acceptance/workplan documents, once that phase intentionally starts;
6. `docs/BACKLOG.md`;
7. historical Round/W documents when reconstructing rationale.

Historical documents are evidence of how the product reached its current state, not automatic implementation instructions. They may contain superseded decisions; an explicit superseded note or this current architecture wins.

External repositories, community plugins, official samples and API documentation are **references / precedents** unless a current Lab/native proof turns the relevant host claim into version-scoped evidence. See `docs/RESEARCH.md`.

`docs/DESIGN.md`, older ROADMAP/checkpoint files and versioned Round documents are retained historical design/evidence unless a current document explicitly points to them.
