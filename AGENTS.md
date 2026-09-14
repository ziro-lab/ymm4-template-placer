# AGENTS.md

## Project intent

YMM4 Template Placer is a YukkuriMovieMaker4 plugin for organizing registered YMM4 Item Templates into a small plugin-side Library / Palette and placing them through a finite set of semantic Target relationships.

Current branch is the **integrated v0.4.0 Candidate**. The historical regression baseline is **v0.3.1**: v0.3.0 functionality plus the native-verified Timeline Tool lifecycle hotfix (`CanSuspend=true`, hide/reopen without Timeline mutation). Read `docs/DESIGN.md`, `docs/NATIVE_VALIDATION_V0.4.md`, `docs/ROADMAP.md`, then `docs/V0.4_ACCEPTANCE.md` and the latest PR/checkpoint evidence. Do not reimplement already native-verified W checkpoints. `docs/IMPLEMENTATION.md` and `docs/VERIFICATION.md` are historical v0.3 records; current user behavior is `docs/USAGE.md`, including add-only placement instead of the old replacement path.

Final completion requires the full native ladder and 18-item acceptance manifest, zero compiler warnings/errors, exact release DLL native smoke and versioned package checks. A source edit or successful build alone is not completion. Keep PR #6 Draft and main unchanged unless the user explicitly authorizes a later merge. Small staging file commits may defer CI until their integration commit; every completed W still requires the whole native lane before promotion.

`docs/NATIVE_VALIDATION_V0.4.md` records pre-implementation behavior proved against the real pinned YMM4 host. For covered paths, use the proved public host surface instead of rediscovering private Timeline ViewModel internals unless a later native test demonstrates that the public route is insufficient.

## v0.4 product boundary

The plugin should help the user:

```text
find the relevant YMM4 Template quickly
→ place it through a meaningful relationship
→ optionally re-apply that relationship later
```

It must not become a general rule language or a second timeline engine.

### Source of truth

- YMM4 `ItemSettings.Default.Templates` remains the Template source of truth.
- The plugin Library stores references, plugin display names and optional Character association; it does **not** copy Template bodies into a second template database.
- Library and Palette are separate. One LibraryEntry may appear in multiple Palettes.
- YMM4 `ItemTemplate.SceneId` is **not** a unique Template ID. Native restart validation proved duplicate Name / Path / SceneId entries can persist. Resolve stored source metadata only when exactly one live Template matches; 0 or multiple matches stay unresolved and require explicit relink/removal. Never fuzzy-pick a candidate.

### Main interaction models

- **Expression list:** bulk VoiceItem → Character-compatible Face Template assignment. Excel remains a secondary bridge for this path.
- **Character Palette:** selected VoiceItem / TachieFaceItem temporarily selects the matching Character palette; clearing that context returns to the previously manually selected Character palette. Native proof shows this can subscribe to public `Timeline.PropertyChanged` and read public `SelectedItem` / `SelectedItems`; polling/private Timeline ViewModel reflection is not required on YMM4 4.55.1.1.
- **Style Palette:** manually selected editing vocabulary such as bright / dark / battle effects.
- **Quick Drop:** double-click a Palette entry to place the Template at public `Timeline.CurrentFrame` using the Template intrinsic Length. Quick Drop has no Target association and is not resynced.
- **Selection placement:** apply a finite semantic Profile to selected Timeline Items. Selection Profiles add independent Items; association/Resync is the explicit expression path. See `docs/SELECTION_PLACEMENT.md` for rounding, padding and agreed boundary position.

## CURRENT semantic Profiles

v0.4 implements only these Profile families unless the design is explicitly revised:

```text
Character Expression
Target Companion
Point Emphasis
Selection Range
Boundary
```

Profiles are thin strategies over shared planning primitives. Do not expose arbitrary start/end predicates or build a generic resolver graph.

## Layer rules

Layer placement is important and must be planned before mutation.

Character Quick Drop supports:

```text
Base  = configured normal Layer policy
Front = greater Layer number than overlapping same-Character related Items
Back  = smaller Layer number than overlapping same-Character related Items
```

YMM4 display priority follows Layer number, so do not rename these modes to ambiguous timeline-screen terms without explanation. Collision checks use the full planned Item duration, not only its first frame.

Native proof confirmed the intended rule: same-Character Layers define the Front/Back baseline, unrelated Characters do not alter that baseline, and any Timeline Item may block a candidate Layer during any part of the proposed duration.

Layer search is deterministic. Existing Items are never moved or shortened to make room. Planned Items in the same batch reserve occupancy before commit.

## Placement / mutation rules

- v0.4 placement is **add-only**. Do not delete all generated Items and rebuild them.
- Deletion is a normal YMM4 user operation.
- Preserve all unrelated/manual Items.
- Build and validate PlacementPlans before mutating Timeline state.
- If a required placement cannot be planned safely, do not partially mutate the operation.
- Use YMM4-native Undo/Redo; do not build a custom undo stack.

## Lightweight association / resync

Persistent connected clips are out of scope. Association is only a weak marker for explicit, user-triggered re-sync.

- Give a target Voice a simple plugin-wide serial only when association is needed.
- Store plugin tags inside Remark without destroying user-authored Remark text.
- Resync lookup: source serial → Character guard → exactly one target.
- 0 matches or multiple matches: skip. Do not infer by Frame, Serif, ordering or similarity.
- Resync is best effort and uses the **current Preset**, never a historical Preset snapshot.
- Quick Drop has no association.
- No continuous event monitoring, automatic target-delete handling, copy/paste ID repair, or whole-scene sync in v0.4.

## Excel

Excel remains a secondary bulk Assignment bridge for Voice Expression only. Do not turn the workbook into a generic Placement Profile editor.

- `.xlsx` generation/read must not require Excel COM automation.
- Import validates first and does not mutate Timeline by itself.
- Placement after import uses the currently selected Character Expression Preset.

## Explicit non-goals

Do not add these to v0.4 unless the design is explicitly changed:

- Series Set as a core concept
- plugin-owned copies of YMM4 Template bodies
- generic Rule Engine / DSL / node editor / arbitrary predicates
- automatic delete-and-rebuild placement
- continuous synchronization or Voice move event tracking
- copy/paste ID repair or fuzzy target recovery
- historical Preset snapshots
- persistent Connected Clip semantics
- multi-item Template generalization
- protected Intro / Outro time remapping
- Parent / Follow / Track Matte engines
- audio/beat analysis, word timing or STT
- direct AI API integration
- multi-scene batch

## Technical target

- YMM4 4.55.1.1 Lite baseline
- .NET 10
- `net10.0-windows10.0.19041.0`
- WPF plugin
- Native Windows GitHub Actions as the primary automated runtime proof

## CI rules

- Heavy native YMM4 work must run only for source/project/XAML/test/fixture/workflow changes plus manual dispatch.
- Documentation-only changes must not download or launch YMM4.
- Keep fixtures tiny, deterministic and redistribution-safe.
- Prefer direct state assertions over screenshot-only assertions; also inspect UI captures for display regressions.
- Preserve v0.3.1 regression tests, including P9 Tool hide/reopen, while maintaining v0.4 proof steps.
- Use small auditable file edits. If a tool safety check rejects a write, do not reroute it: record the exact operation, file and last successful commit.

## Implementation order

Follow `docs/ROADMAP.md` as the historical implementation/proof ladder, and continue from the latest verified checkpoint rather than rebuilding it. Keep the placement core, host surface evidence and finite-profile boundary intact. Changes to a completed W need corresponding regression proof before they are promoted.

## External references

Existing YMM4 projects and cross-editor research are implementation/design evidence, not permission to copy blindly. Review licenses before reusing source. Prefer implementing only the required behavior against YMM4 APIs.
