# AGENTS.md

## Project intent

YMM4 Template Placer is a YukkuriMovieMaker4 plugin for organizing registered YMM4 Item Templates into a small plugin-side Library / Palette and placing them through a finite set of semantic Target relationships.

Implemented baseline is **v0.3.1**. It is the v0.3.0 functional baseline plus the native-verified Timeline Tool lifecycle hotfix (`CanSuspend=true`, hide/reopen without Timeline mutation). Current implementation target is **v0.4**. Read `docs/DESIGN.md` first, then `docs/NATIVE_VALIDATION_V0.4.md`, then `docs/ROADMAP.md`. `docs/IMPLEMENTATION.md` and `docs/VERIFICATION.md` describe the already-proven v0.3.0 functional baseline; preserve those behaviors plus the v0.3.1 P9 lifecycle regression while implementing v0.4.

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
- **Selection placement:** apply a finite semantic Profile to selected Timeline Items.

## CURRENT semantic Profiles

v0.4 may implement only these Profile families unless the design is explicitly revised:

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
- Prefer direct state assertions over screenshot-only assertions.
- Preserve v0.3.1 regression tests, including P9 Tool hide/reopen, while adding v0.4 proof steps.

## Implementation order

Follow `docs/ROADMAP.md`. Do not implement all UI surfaces at once. First convert the placement path to add-only planned commits, then implement Library / Character Palette / Quick Drop / Front-Back Layer planning using the public host surfaces already proved in `docs/NATIVE_VALIDATION_V0.4.md`, then semantic Profiles and lightweight Resync.

## External references

Existing YMM4 projects and cross-editor research are implementation/design evidence, not permission to copy blindly. Review licenses before reusing source. Prefer implementing only the required behavior against YMM4 APIs.
