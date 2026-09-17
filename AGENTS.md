# AGENTS.md

## Current revision

Current target: **v0.4.2 Relative Intent Palette**, PR #11. The hands-on UX polish implementation-prep branch is `work/v0.4.2-hands-on-ux-polish`, based exactly on native-verified PR #11 source `eeb465438a75508a936180543ee517bdfbf292a4`. Base: native-verified v0.4.1 Candidate `2710039b9f3d40c54aa8e495cb81a998c3f82e5e`. Historical regression baseline: v0.3.1.

Read `docs/V0.4.2_HANDS_ON_UX_POLISH_WORKPLAN.md` and `docs/V0.4.2_HANDS_ON_UX_POLISH_ACCEPTANCE.md` first on this branch, then `docs/V0.4.2_RELATIVE_PALETTE_DESIGN.md`, `docs/V0.4.2_UIUX_MENTAL_MODEL.md`, `docs/V0.4.2_ROADMAP.md`, `docs/V0.4.2_CANDIDATE.md`, `docs/USAGE.md` and the latest PR/native evidence. The hands-on workplan is authoritative for this polish pass where it adds detail; it does not override the proven safety core. The UI/UX mental-model addendum remains authoritative for the normal editing model: Item -> editing intent -> optional Set -> executable tile; engine parameters must not leak back into the common path. The v0.4.2 roadmap remains authoritative for R numbering.

Do not reimplement completed W/R checkpoints. A build or source edit alone is not completion. Do not merge main. Keep PR #6 and PR #11 Draft until the user accepts the new Candidate in their actual editing environment.

Before implementing expression trial-switch Undo suppression, Tool-menu utility placement, or Item display-name discovery, use the pinned-host P0 evidence required by the hands-on workplan. Do not guess host APIs, add arbitrary reflection, invent a custom undo stack, or silently bypass native Undo to satisfy UX wording.

## Product boundary

Normal editing is selection -> applicable intent -> optional Palette Set -> one concrete tile click. Decide placement relations during setup, not on every normal action. Palette owns applicability and relation; entries have only small parameter overrides plus non-semantic appearance metadata such as alias/color/order.

Runtime Item type is the primary applicability key. Uniform and mixed multi-selection require explicit matching contracts. CharacterName is the logical grouping key. Distinct host objects with the same logical name do not by themselves suppress candidates; real ambiguous registered Character definitions stop placement.

First expression bootstrap scans live Face-containing Templates. It does not repeatedly re-add user-deleted memberships. New expression import is explicit. Settings organization is separate from normal execution. No arbitrary expression, script, property-path engine, boolean tree, regex rules, DSL or node graph.

The `表情をまとめて` workspace is a specialist high-throughput expression-placement workflow. It may replace or remove **only the exact Plugin-managed associated bundle for the row's Voice**, after complete strict resolution and preflight. This is not permission to make normal placement destructive or to delete/rebuild manual/unassociated items.

## GUI coordination guardrails

Preserve the current WPF/MVVM and PlacementPlan architecture; do not rewrite it into MVP, Chain of Responsibility, or another pattern merely for terminology compliance. Apply the following rules when adding or changing GUI behavior so AI-assisted incremental work cannot create hidden cross-screen state coupling.

- Treat Views and child panels as passive surfaces. They may render bound state, perform purely local visual/layout work, and forward user intent. Do not let a View or code-behind directly change a sibling View, sibling ViewModel, Timeline, settings, or another feature's workflow state to make a behavior happen.
- Keep child ViewModels responsible only for their own local feature state. Any request that affects workspace mode, competing operations, global visibility/navigation, operation admission, cancellation, or another component must be raised to the owning parent and ultimately the root coordinator (`PlacerViewModel` or its explicit successor).
- UI events must have one upward ownership path. Prefer commands/events that bubble through the existing owner hierarchy over sibling-to-sibling calls, shared mutable flags, or ad-hoc callbacks. The component that receives an event either handles the part it owns or delegates upward; it must not reach sideways to orchestrate another component.
- The root coordinator is the single arbiter for mutually exclusive UI/workflow state. Do not add sleeps, retry loops, temporary locks, duplicated timers, or competing show/hide decisions in separate components to reconcile races after the fact.
- When a new feature introduces mutually exclusive or order-sensitive application states, model them explicitly as a finite state/transition model (an enum/state object or an equally explicit transition table) instead of encoding the new global state as an unchecked combination of booleans. Keep invalid transitions impossible or rejected at the coordinator boundary. The new expression immediate-apply/replace flow is such a stateful workflow.
- A user action must have one authoritative route from input to decision to effect. Repeated input, cancel/close, mode switch, Tool hide/reopen, row change during immediate apply, and re-entry during an executing operation must be covered by regression tests when the changed feature can interact with those paths.
- Do not move placement/domain decisions into the GUI coordinator. Keep the existing direction: UI intent -> coordinator/admission -> resolver/plan -> complete preflight -> `PlacementPlan` commit -> native Undo. GUI coordination rules are a guardrail around the proven placement core, not a replacement for it.

## Safety core

- Live YMM4 `ItemSettings.Default.Templates` is the source of truth. Library/settings store references, aliases, appearance metadata and relation parameters, never copied Template bodies.
- `ItemTemplate.SceneId` is not a unique ID. Strict TemplateLocator must resolve exactly one source. Missing/ambiguous references remain unresolved; only explicit relink may change their meaning.
- Normal placement is add-only. Never delete/rebuild or move/shorten existing unrelated/manual items to make room. The only planned UX-polish exception is atomic replacement/removal of a validated exact associated expression bundle in the specialist expression workspace.
- Resolve time, whole-duration collisions and already-planned occupancy for the complete operation before mutation. A required-plan failure leaves zero partial placement. Expression replacement must fully prepare the new result before the old associated result is changed.
- Clone bundle members independently, normalize the minimum source Frame and preserve internal Frame/Layer/Length/content. Do not invent native Group identities.
- `GetClone()` preserves the source Character object identity in the pinned host. For a character-bearing clone whose logical `CharacterName` matches the selected target, rebind only the clone to that selected canonical Character after planning; never rewrite the live Template. For `TachieFaceItem`, preserve the already-cloned Face parameter/effect objects across Character setter refresh. Keep `TEMPLATE_FIDELITY=PASS` as a release gate. Exact CharactorMotion 1.1.1 `ZoomCorrection=5` / disabled state has separate lab corroboration; real PsdTachie content still requires hands-on acceptance.
- 上 means smaller layer numbers; 下 means larger numbers. Collision escape translates the whole bundle in the same direction only, within the saved bounds. No opposite-side wrap.
- Commit through the shared PlacementPlan and YMM4-native Undo/Redo. No custom undo stack. Trial-switch history coalescing is gated by proved host support.
- Validate stale source/context/settings again before committing. Do not turn failures into empty-success reports.
- Settings edits are staged and atomically saved. Corrupt/future/external-modified settings must not be overwritten. Load-time migration is in memory and preserves old meanings.

## Association and compatibility

Normal tiles are unassociated. Expression-list placement may associate one Voice with multiple generated members through weak Remark tags. Preserve user remarks. Each member must be identifiable; explicit Resync updates whole bundles or skips the invalid bundle, never partially reconstructs it. A copied/missing member or ambiguous target is not repaired by proximity/text/order.

The hands-on UX polish may add immediate expression replacement/removal, but only by exact association identity and whole-bundle atomic preflight. Opening/refreshing the list must reconstruct current choices from exact association identity, never by visual position, text similarity or first match.

Resync is user-triggered, selection-scoped, uses current saved relations, and is best effort across independent bundles with truthful skip reporting and one native Undo for the successful updates. No continuous scene-wide tracking. Legacy Resync must not update just one member of a relative bundle.

Keep the explicit compatibility workspace for old Library/Palette/Expression/Selection presets and old Quick Drop. No invented absolute-to-relative migration. Excel remains a secondary Voice assignment bridge; import validates before changing assignments and never mutates Timeline by itself. No Excel COM requirement.

## Host and runtime

Pinned native host: YMM4 4.55.1.1 Lite, .NET 10, WPF, `net10.0-windows10.0.19041.0`. Prefer proved public Timeline APIs documented in `docs/NATIVE_VALIDATION_V0.4.md`; selection is event-driven, not polling or private Timeline ViewModel reflection. The isolated fixed read-only Character registry compatibility adapter is documented separately; do not generalize it into arbitrary reflection.

Run heavy native YMM4/build/package work in the existing Windows GitHub Actions lane, not the chat container. Keep fixtures tiny, deterministic and redistribution-safe. Check actual commands/state plus screenshots. Do not infer real PSD rendering fidelity from synthetic fixtures.

## Release and git discipline

- Work on the specified branch; preserve main, PR #6, PR #11 and the accepted baseline.
- Use small auditable changes/checkpoints. If a safety check rejects a write, do not reroute it; record the exact operation and last successful commit.
- Documentation-only changes must not download/build/launch YMM4; source/project/XAML/tests/fixtures/workflow changes require the native lane before promotion.
- Require P1-P9, W3-W12, WUX1-WUX13, R1-R14, `TEMPLATE_FIDELITY=PASS`, `RELATIVE_UIUX=PASS`, and after implementation `HANDS_ON_UX_POLISH=PASS`, plus current acceptance manifests, zero compiler warnings/errors, exact release DLL native smoke and stable-root archive checks.
- `ValidateRelativeEvidence.ps1` is the independent relative acceptance consumer; keep its negative tests and extend rather than weaken it for the new stage.
- `.ymme` root must always be `Ymm4TemplatePlacer/`, never a versioned plugin folder. Record source/checkout/run provenance and all final hashes.
- Separate DONE/PARTIAL/FUTURE/BLOCKED accurately. Native PASS is not hands-on user acceptance.

Existing external projects/research are evidence, not permission to copy assets or code without checking licenses. Prefer the smallest implementation against proved YMM4 APIs.
