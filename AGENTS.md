# AGENTS.md

## Current revision and Git authority

Main contains the accepted Round 4 A/B/C candidate merged by PR #16.

Accepted baseline:

- merge commit `7dc30dcb887a0d57f3079d72ffe89dc62bedd9fa`
- main Release run #227 `35508468265`: full native regression, exact distribution DLL smoke and verified package succeeded.

Current priority is **UI Micro Polish** on branch `work/ui-micro-polish-prep`.

Tachie Preset Draft PR #19 remains paused until the UI pass is accepted and merged.

Read in this order:

1. `docs/CURRENT_ARCHITECTURE.md`
2. `docs/GLOSSARY.md`
3. `docs/UI_MICRO_POLISH_CORRECTIVE_DESIGN.md`
4. `docs/UI_MICRO_POLISH_CORRECTIVE_ACCEPTANCE.md`
5. `docs/UI_MICRO_POLISH_CORRECTIVE_WORKPLAN.md`
6. `docs/UI_MICRO_POLISH_CORRECTIVE_STATUS.md`
7. `docs/UI_MICRO_POLISH_HANDS_ON_FEEDBACK.md`
8. `docs/VALIDATION_STRATEGY.md`
9. `docs/BACKLOG.md` only for scope context
10. `docs/LEGACY_COMPATIBILITY_MAP.md` before deleting/refactoring old-looking code

This pass is UI-only. Do not add Tachie Preset or Placement Recipe product code.

Do not reimplement completed Round 4 A/B/C work. Preserve its Settings transaction model, PlacementPlan/native Undo architecture, exact managed-expression safety and validation tiers.

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
- Settings use staged validation plus protected automatic persistence. Never overwrite corrupt, future-version or externally modified settings. Invalid/incomplete drafts remain local; session rollback and auto-commit both retain the existing atomic/digest guards.

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
- Retain P1-P9, W3-W12, WUX1-WUX13, R1-R14, TEMPLATE_FIDELITY, RELATIVE_UIUX, HANDS_ON_UX_POLISH, HANDS_ON_ROUND2, HANDS_ON_ROUND3 and current acceptance assets. Follow `docs/VALIDATION_STRATEGY.md`: Focused runs a compact stable core for ordinary Draft edits; Checkpoint/Release retain the complete historical semantic ladder and evidence-negative fixtures.
- Release/Proof builds require zero compiler warnings/errors. Exact distribution-DLL smoke, final package/provenance and stable `Ymm4TemplatePlacer/` root are Release gates rather than a tax on every Focused edit. Record source/checkout/run/attempt/provenance and final hashes at Release.
- Label DONE/PARTIAL/FUTURE/BLOCKED accurately. Native PASS is not human acceptance. Update usage documentation only after the new implemented UI is native-green, not speculatively during partial work.

External research is evidence, not permission to copy assets/code without checking licenses. Prefer the smallest change against proved YMM4 APIs.
