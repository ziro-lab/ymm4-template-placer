# AGENTS.md

## Current revision

Current target: **v0.4.2 Relative Intent Palette**, branch `feature/v0.4.2-character-template-bundles`, PR #11. Base: native-verified v0.4.1 Candidate `2710039b9f3d40c54aa8e495cb81a998c3f82e5e`. Historical regression baseline: v0.3.1.

Read `docs/V0.4.2_RELATIVE_PALETTE_DESIGN.md`, `docs/V0.4.2_UIUX_MENTAL_MODEL.md`, `docs/V0.4.2_ROADMAP.md`, `docs/V0.4.2_CANDIDATE.md`, `docs/USAGE.md` and the latest PR/native evidence. The UI/UX mental-model addendum is authoritative for what normal editing must expose: Item -> editing intent -> optional Set -> executable tile; engine parameters must not leak back into the common path. The v0.4.2 roadmap is authoritative for R numbering. `docs/DESIGN.md` and `docs/ROADMAP.md` describe the historical v0.4 implementation; its proven safety core remains mandatory, but its old normal UI and singleton-only scope do not override the explicit v0.4.2 revision.

Do not reimplement completed W/R checkpoints. A build or source edit alone is not completion. Do not merge main. Keep PR #6 Draft until the user accepts the Candidate in their actual editing environment.

## Product boundary

Normal editing is selection -> applicable intent -> optional Palette Set -> one concrete tile click. Decide placement relations during setup, not on every normal action. Palette owns applicability and relation; entries have only small parameter overrides.

Runtime Item type is the primary applicability key. Uniform and mixed multi-selection require explicit matching contracts. CharacterName is the logical grouping key. Distinct host objects with the same logical name do not by themselves suppress candidates; real ambiguous registered Character definitions stop placement.

First expression bootstrap scans live Face-containing Templates. It does not repeatedly re-add user-deleted memberships. New expression import is explicit. Settings organization is separate from normal execution. No arbitrary expression, script, property-path engine, boolean tree, regex rules, DSL or node graph.

## GUI coordination guardrails

Preserve the current WPF/MVVM and PlacementPlan architecture; do not rewrite it into MVP, Chain of Responsibility, or another pattern merely for terminology compliance. Apply the following rules when adding or changing GUI behavior so AI-assisted incremental work cannot create hidden cross-screen state coupling.

- Treat Views and child panels as passive surfaces. They may render bound state, perform purely local visual/layout work, and forward user intent. Do not let a View or code-behind directly change a sibling View, sibling ViewModel, Timeline, settings, or another feature's workflow state to make a behavior happen.
- Keep child ViewModels responsible only for their own local feature state. Any request that affects workspace mode, competing operations, global visibility/navigation, operation admission, cancellation, or another component must be raised to the owning parent and ultimately the root coordinator (`PlacerViewModel` or its explicit successor).
- UI events must have one upward ownership path. Prefer commands/events that bubble through the existing owner hierarchy over sibling-to-sibling calls, shared mutable flags, or ad-hoc callbacks. The component that receives an event either handles the part it owns or delegates upward; it must not reach sideways to orchestrate another component.
- The root coordinator is the single arbiter for mutually exclusive UI/workflow state. Do not add sleeps, retry loops, temporary locks, duplicated timers, or competing show/hide decisions in separate components to reconcile races after the fact.
- When a new feature introduces mutually exclusive or order-sensitive application states, model them explicitly as a finite state/transition model (an enum/state object or an equally explicit transition table) instead of encoding the new global state as an unchecked combination of booleans. Keep invalid transitions impossible or rejected at the coordinator boundary.
- A user action must have one authoritative route from input to decision to effect. Repeated input, cancel/close, mode switch, Tool hide/reopen, and re-entry during an executing operation must be covered by regression tests when the changed feature can interact with those paths.
- Do not move placement/domain decisions into the GUI coordinator. Keep the existing direction: UI intent -> coordinator/admission -> resolver/plan -> complete preflight -> `PlacementPlan` commit -> native Undo. GUI coordination rules are a guardrail around the proven placement core, not a replacement for it.

## Safety core

- Live YMM4 `ItemSettings.Default.Templates` is the source of truth. Library/settings store references, aliases and relation parameters, never copied Template bodies.
- `ItemTemplate.SceneId` is not a unique ID. Strict TemplateLocator must resolve exactly one source. Missing/ambiguous references remain unresolved; only explicit relink may change their meaning.
- Normal placement is add-only. Never delete/rebuild or move/shorten existing unrelated/manual items to make room.
- Resolve time, whole-duration collisions and already-planned occupancy for the complete operation before mutation. A required-plan failure leaves zero partial placement.
- Clone bundle members independently, normalize the minimum source Frame and preserve internal Frame/Layer/Length/content. Do not invent native Group identities.
- `GetClone()` preserves the source Character object identity in the pinned host. For a character-bearing clone whose logical `CharacterName` matches the selected target, rebind only the clone to that selected canonical Character after planning; never rewrite the live Template. For `TachieFaceItem`, preserve the already-cloned Face parameter/effect objects across Character setter refresh. Keep `TEMPLATE_FIDELITY=PASS` as a release gate. This synthetic native proof covers built-in/community effect state, not arbitrary third-party plugin fidelity.
- 上 means smaller layer numbers; 下 means larger numbers. Collision escape translates the whole bundle in the same direction only, within the saved bounds. No opposite-side wrap.
- Commit through the shared PlacementPlan and YMM4-native Undo/Redo. No custom undo stack.
- Validate stale source/context/settings again before committing. Do not turn failures into empty-success reports.
- Settings edits are staged and atomically saved. Corrupt/future/external-modified settings must not be overwritten. Load-time migration is in memory and preserves old meanings.

## Association and compatibility

Normal tiles are unassociated. Expression-list placement may associate one Voice with multiple generated members through weak Remark tags. Preserve user remarks. Each member must be identifiable; explicit Resync updates whole bundles or skips the invalid bundle, never partially reconstructs it. A copied/missing member or ambiguous target is not repaired by proximity/text/order.

Resync is user-triggered, selection-scoped, uses current saved relations, and is best effort across independent bundles with truthful skip reporting and one native Undo for the successful updates. No continuous tracking or scene-wide sync. Legacy Resync must not update just one member of a relative bundle.

Keep the explicit compatibility workspace for old Library/Palette/Expression/Selection presets and old Quick Drop. No invented absolute-to-relative migration. Excel remains a secondary Voice assignment bridge; import validates before changing assignments and never mutates Timeline by itself. No Excel COM requirement.

## Host and runtime

Pinned native host: YMM4 4.55.1.1 Lite, .NET 10, WPF, `net10.0-windows10.0.19041.0`. Prefer proved public Timeline APIs documented in `docs/NATIVE_VALIDATION_V0.4.md`; selection is event-driven, not polling or private Timeline ViewModel reflection. The isolated fixed read-only Character registry compatibility adapter is documented separately; do not generalize it into arbitrary reflection.

Run heavy native YMM4/build/package work in the existing Windows GitHub Actions lane, not the chat container. Keep fixtures tiny, deterministic and redistribution-safe. Check actual commands/state plus screenshots. Do not infer user PSD fidelity or third-party effect fidelity from synthetic fixtures.

## Release and git discipline

- Work on the specified branch; preserve main and the accepted baseline.
- Use small auditable changes/checkpoints. If a safety check rejects a write, do not reroute it; record the exact operation and last successful commit.
- Documentation-only changes must not download/build/launch YMM4; source/project/XAML/tests/fixtures/workflow changes require the native lane before promotion.
- Require P1-P9, W3-W12, WUX1-WUX13, R1-R14, `TEMPLATE_FIDELITY=PASS`, `RELATIVE_UIUX=PASS`, current core/Task UX/workflow/relative/relative-UIUX acceptance manifests, zero compiler warnings/errors, exact release DLL native smoke and stable-root archive checks.
- `ValidateRelativeEvidence.ps1` is the independent relative acceptance consumer; keep its negative tests. Do not weaken expected stage/check coverage merely to get a green run.
- `.ymme` root must always be `Ymm4TemplatePlacer/`, never a versioned plugin folder. Record source/checkout/run provenance and all final hashes.
- Separate DONE/PARTIAL/FUTURE/BLOCKED accurately. Native PASS is not hands-on user acceptance.

Existing external projects/research are evidence, not permission to copy assets or code without checking licenses. Prefer the smallest implementation against proved YMM4 APIs.
