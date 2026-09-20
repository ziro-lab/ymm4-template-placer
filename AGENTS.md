# AGENTS.md

## Current revision and Git authority

Main contains the accepted Round 3 baseline, promoted by owner-approved PR #17 on 2026-09-20. Exact product source: `fdc3f3e5448cdf5ce7c9498776362b1bf598c2b1`; exact tree: `3aa91dd12a8262e60c1909293fb68f93a0a032d1`. Promotion merge: `b8c787b713e2dbfe253a8ab4ca7f9a6920c81f75`.

Read `docs/DEVELOPMENT_WORKFLOW.md` first. It supersedes historical instructions to keep main unchanged or keep the old stacked PRs open. Do not reopen or separately merge incorporated historical candidates. Preserve their branches and evidence.

Active next-round work is main-based Draft PR #16, `work/v0.4.2-hands-on-round4-prep`. Read that branch's Round 4 DESIGN / HOST_EVIDENCE / WORKPLAN / ACCEPTANCE / IMPLEMENTATION_PREP before implementing there. Do not mix unfinished Round 4 source or prep documents into this baseline. Keep an incomplete working PR Draft; merge only after applicable validation and owner acceptance.

For the baseline's behavior and regression contracts read:

1. `docs/V0.4.2_HANDS_ON_ROUND3_DESIGN.md`
2. `docs/V0.4.2_HANDS_ON_ROUND3_HOST_EVIDENCE.md`
3. `docs/V0.4.2_HANDS_ON_ROUND3_ACCEPTANCE.md`
4. Round 2 and earlier design documents for preserved contracts.
5. `docs/NATIVE_VALIDATION_V0.4.md` and current evidence/package workflows.

Do not reimplement completed rounds. Normal placement is context -> Set -> tile. Auto/Fixed layout and position shortcuts are global, never owned by a template. Generic layer targeting is numeric only; no coordinate-derived clicked-layer mode. Normal startup never enters the hidden legacy workspace. Voice freshness is event-driven, excluding association Remark-only changes. Voice navigation uses one root-owned latest-wins coordinator for CurrentFrame, selection, public Preview SeekAsync and independent viewport following. Never use fake input, frame jiggle or FocusService as a Preview-refresh workaround.

## Product boundary

Normal editing is selection -> applicable intent -> optional Palette Set -> one concrete tile click. Decide placement relations during setup, not on every normal action. Palette owns applicability and relation; entries have only small parameter overrides plus non-semantic appearance metadata such as alias/color/order.

Runtime Item type is the primary applicability key. Uniform and mixed multi-selection require explicit matching contracts. CharacterName is the logical grouping key. Distinct host objects with the same logical name do not by themselves suppress candidates; real ambiguous registered Character definitions stop placement.

First expression bootstrap scans live Face-containing Templates. It does not repeatedly re-add user-deleted memberships. New expression import is explicit. Settings organization is separate from normal execution. No arbitrary expression, script, property-path engine, boolean tree, regex rules, DSL or node graph.

The `表情をまとめて` workspace is a specialist high-throughput expression-placement workflow. It may replace or remove only the exact Plugin-managed associated bundle for the row's Voice, after complete strict resolution and preflight. This is not permission to make normal placement destructive or to delete/rebuild manual/unassociated items.

## GUI coordination guardrails

Preserve WPF/MVVM and PlacementPlan; do not rewrite into another architecture merely for terminology compliance.

- Views/child panels render bound state, perform local visual/layout work and forward user intent. They never directly change a sibling View, sibling ViewModel, Timeline, settings or another workflow.
- Child ViewModels own local feature state. Requests affecting global navigation, operation admission, cancellation, visibility or other components go upward to the root coordinator, not sideways through shared mutable flags or ad-hoc callbacks.
- The root is the single arbiter for mutually exclusive/order-sensitive workflow states. Model their finite transitions explicitly. Do not add sleeps, retry loops, duplicated timers or independent show/hide decisions to mask races.
- A user action has one authoritative route from input to decision to effect. Cover repeated input, cancellation, mode/row changes, Tool hide/reopen, DataContext replacement and re-entry during executing operations.
- Keep domain decisions in the existing direction: UI intent -> coordinator/admission -> resolver/plan -> complete preflight -> PlacementPlan commit -> native Undo. These guardrails are not permission to create a new placement engine.

## Safety core

- Live `ItemSettings.Default.Templates` is authoritative. Store references, aliases, appearance and relation parameters, never copied template bodies in a second database.
- `ItemTemplate.SceneId` is not a unique ID. Strict TemplateLocator resolves exactly one source. Missing/ambiguous references remain unresolved; only explicit relink changes their meaning.
- Normal placement is add-only. Never move, shorten or delete unrelated/manual items to create space. The specialist expression workspace may atomically replace/remove an exact validated managed bundle only.
- Resolve whole-operation time, full-duration collisions and planned reservations before mutation. Any required-plan failure is zero-write. Fully prepare a replacement before changing the old bundle.
- Clone bundle members independently, normalize minimum source Frame and preserve internal Frame/Layer/Length/content. Do not invent native Group identities.
- `GetClone()` preserves source Character identity in the pinned host. Rebind only planned matching clones to the selected canonical Character, never live templates. Preserve already-cloned TachieFaceParameter/effect objects across the Character setter. Retain TEMPLATE_FIDELITY evidence; exact CharactorMotion 1.1.1 corroboration is not universal third-party PSD rendering proof.
- 上 means smaller Layer numbers; 下 means larger. Escape moves the complete bundle in one direction inside saved bounds, without opposite wrap.
- Shared PlacementPlan and YMM4-native Undo/Redo are authoritative. No custom undo stack. Trial coalescing needs proved native support and must close before unrelated edits.
- Revalidate stale source/context/settings before commit. Never report an empty success after a required validation failure.
- Settings retain staged validation and atomic protected saves. Never overwrite corrupt, future-version or externally modified settings. In-memory migration preserves old meanings. A future approved autosave UX must still use these guards.

## Association and compatibility

Normal tiles are unassociated. Expression placement can associate one Voice with multiple members via weak Remark tags. Preserve user remarks and identify every member. Missing, copied or ambiguous associations are not repaired using proximity, text or order. Refresh/reopen reconstructs choices by exact source identity, not by first match.

Resync is explicit, selection-scoped, uses current saved relations and reports skipped independent bundles truthfully. One native Undo covers successful changes. No continuous scene-wide tracking; legacy Resync must not update only one member of a relative bundle.

Preserve old Library/Palette/Expression/Selection presets and Quick Drop data/code losslessly. Hidden legacy UI is not a migration scheme, and a saved legacy flag must not strand normal startup. Excel is a secondary assignment bridge: validate before changing assignments and never mutate Timeline during import. No Excel COM dependency.

## Host and runtime

Pinned native regression host: YMM4 4.55.1.1 Lite / .NET 10 / WPF / net10.0-windows10.0.19041.0. Production compatibility depends on required capabilities, not a blanket version-number gate. Missing context, Preview or viewport support degrades only that feature.

Use proved public Timeline APIs. No polling or private Timeline ViewModel state for selection. Existing isolated read-only Character-registry compatibility does not authorize arbitrary reflection.

Run heavy native/build/package work in the existing Windows Actions lane, not the chat container. Use small deterministic redistribution-safe fixtures. Check actual commands/state and screenshots. Never infer real PSD visual fidelity from synthetic fixtures.

## Release and change discipline

- Work through a main-based branch/PR. Preserve accepted baseline identity and history. Never force-push or reroute a write rejected by a safety check; report the exact failure and last successful checkpoint.
- Use small auditable changes. Documentation-only commits must not launch native builds; source/project/XAML/tests/fixtures/workflow changes require the Windows lane before promotion.
- Retain P1-P9, W3-W12, WUX1-WUX13, R1-R14, TEMPLATE_FIDELITY, RELATIVE_UIUX, HANDS_ON_UX_POLISH, HANDS_ON_ROUND2, HANDS_ON_ROUND3 and the current acceptance manifests. New stages are additive; keep independent invalid-evidence fixtures.
- Release/Proof builds require zero compiler warnings/errors and the exact distribution DLL must pass native smoke. Package root stays `Ymm4TemplatePlacer/`; record source/checkout/run/attempt/provenance and final hashes.
- Label DONE/PARTIAL/FUTURE/BLOCKED accurately. Native PASS is not human acceptance. Update usage documentation only after the new implemented UI is native-green, not speculatively during partial work.

External research is evidence, not permission to copy assets/code without checking licenses. Prefer the smallest change against proved YMM4 APIs.
