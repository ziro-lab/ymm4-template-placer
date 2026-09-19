# ROADMAP — v0.4 implementation and native YMM4 proof ladder


## Current Hands-on Round 3 preparation

Round 2 automated candidate is native-green at PR #14 source `a553ae8c32eeb8c1f125b8c005095bbe6fc98ebb`; real-user hands-on found the next UX corrections.

Round 3 is frozen in:

- `V0.4.2_HANDS_ON_ROUND3_DESIGN.md`
- `V0.4.2_HANDS_ON_ROUND3_HOST_EVIDENCE.md`
- `V0.4.2_HANDS_ON_ROUND3_WORKPLAN.md`
- `V0.4.2_HANDS_ON_ROUND3_ACCEPTANCE.md`
- `V0.4.2_HANDS_ON_ROUND3_IMPLEMENTATION_PREP.md`

Primary changes:

1. tile-face color/shape cleanup and Set-wide shape;
2. Auto/Fixed columns plus position-based shortcuts;
3. direct Settings target types and hidden legacy UI;
4. Generic numeric target layer with one-direction collision behavior;
5. automatic Voice freshness instead of top-level シーン更新;
6. expression row navigation synchronized through public Preview `SeekAsync(int)`;
7. semantic Timeline follow using public `ContainFrameInViewport / ScrollFrame`.

Public Lab PR #57 established that clicked Timeline layer must **not** be inferred from screen coordinates, while Preview playback seek and viewport follow have usable public exact-host surfaces.

This stage is implementation preparation only. Historical W/P/R checkpoints below remain preserved and are not instructions to rebuild completed work.

## Completed integrated Candidate

**W1-W12 are complete at the native-verified v0.4.0 checkpoint.** See `docs/W12_NATIVE_CHECKPOINT.md`: verified source `4938217471f183664ba40e5299fcd3bbb6432568`, run `34868820375`, 384 native assertions, all 18 Acceptance items, 0 compiler warnings/errors, release DLL smoke and versioned packaging PASS. PR #6 remains Draft; main is not merged.

The sections below preserve the original implementation order and exit conditions. They are not an instruction to reimplement completed work. Current usage is `docs/USAGE.md`; exact Profile geometry is `docs/SELECTION_PLACEMENT.md`; verification mapping is `docs/V0.4_ACCEPTANCE.md`. Later documentation-only checkpoints or PR runs are identified by their own source/run provenance.

## Baseline

v0.3.0 P0〜P8 is already implemented and verified on native YMM4 4.55.1.1 Lite. `docs/VERIFICATION.md` records the proven baseline. v0.4 must preserve those Golden Paths while changing the product from one Voice→Face replacement flow into Library / Palette / semantic add-only placement.

The v0.4 target design is `docs/DESIGN.md`.

## W1 — Freeze v0.3 regression

Keep existing tests for:

- plugin load
- live Template catalog
- Voice snapshot
- Face clone / placement
- YMM4-hosted assignment UI
- Excel export/import
- error atomicity
- native Undo/Redo

Exit: the old Golden Path remains green before structural changes continue.

## W2 — Add-only Placement Plan foundation

Refactor the old replacement-oriented path into:

```text
request
→ plan Frame / Length
→ plan Layer
→ validate existing + planned occupancy
→ commit add/update
```

Do not delete all plugin-generated Items as part of normal placement.

Exit:

- planned result is deterministic before Timeline mutation
- required-plan failure produces zero partial mutation
- one operation maps to one native Undo unit

## W3 — Plugin Library

Implement the thin Library over live YMM4 Templates.

Required:

- add an existing YMM4 Template by reference
- plugin-local `DisplayName`
- optional Character association
- stable LibraryEntry ID
- broken-reference state
- explicit re-link / unregister
- no copied Template body

Exit: YMM4's long management name and the short plugin display name are independent in the real plugin UI.

## W4 — Palette model and Character context

Separate Library from Palette and allow one LibraryEntry in multiple Palettes.

Implement:

- Character Palette
- Style Palette
- manual Character palette selection
- temporary VoiceItem / TachieFaceItem single-selection context override
- restore prior manual Character palette when the context ends

Exit: selecting a Character Item in actual YMM4 changes only the temporary Character palette context and restores correctly.

## W5 — Quick Drop / Base layer

Palette double-click means exactly:

```text
Frame  = Timeline.CurrentFrame
Length = Template intrinsic Length
Layer  = Base Layer Policy
```

No Target association is created.

Exit:

- actual Palette entry double-click places the expected native Item
- feedback reports Template / Frame / Layer
- Undo 1回で戻る

## W6 — Character Front / Back planning

For Character Palette Quick Drop, add:

```text
Base
Front = greater Layer number side
Back  = smaller Layer number side
```

Use overlapping same-Character Voice / Face / safely readable Tachie Items as the ordering context.

Rules:

- check the entire planned duration
- deterministic one-direction search
- no wraparound to the opposite side
- if no same-Character context exists, fall back to Base
- existing Items are never moved or shortened

Exit: PSD-style multiple Face Items can coexist on different Layers and Front / Back ordering is deterministic.

## W7 — Character Expression Presets

Migrate the v0.3 expression path to the new Profile/Preset model.

Implement at least:

```text
Voice span
Next Same Character + MaxGap
Start / End offset
Layer Policy / Band / Preferred
```

Next Same Character must never shorten a Face below the current Voice span merely because the next Voice overlaps.

Exit: a multi-Voice native fixture proves semantic next-Character resolution, MaxGap and planned Layer reservations.

## W8 — Lightweight Voice association + manual Resync

Add weak plugin tags to Remark only when Target association is required.

Concept:

```text
Voice:     CWT_TPL:V=<serial>
Generated: CWT_TPL:S=<serial>;P=<profile>
```

Do not destroy user Remark content.

Resync lookup:

```text
serial match
→ Character guard
→ exactly one Target = use
→ otherwise skip
```

No fuzzy recovery.

Exit:

- moving a Voice then explicitly resyncing recomputes related Item geometry using the current Preset
- missing / ambiguous Target is skipped without mutation
- successful subset is one Undo operation
- Quick Drop remains unassociated

## W9 — Target Companion + Point Emphasis

Add single-Target Profiles without a new generic rule language.

Target Companion:

```text
Target span + optional head/tail padding
```

Point Emphasis:

```text
Anchor = Start / 25 / 50 / 75 / End
+ Offset
+ Fixed Duration
```

Exit: both are implemented through the existing Planner / LayerPlanner without special Timeline mutation code.

## W10 — Selection Range

Add the multi-target scope:

```text
Start = min(selected.Start)
End   = max(selected.End)
```

with optional padding and normal Layer planning.

Exit: multiple selected native Items produce exactly one planned overlay Item and preserve unrelated Items.

## W11 — Boundary

Add Adjacent Pair as a thin Profile.

```text
Exactly 2 selected Items
abs(A.End - B.Start) <= Tolerance
```

Do not search for a nearby cut when the condition is not satisfied.

Exit: Boundary is added without redesigning Core planning or mutation. If it requires a new engine layer, stop and re-evaluate the architecture.

## W12 — Style Palette and UX polish

Finish practical UI:

- Style Palette manual switching
- Library search / add / re-link UX
- Profile/Preset selection without exposing low-level resolver primitives
- placement preview where useful
- clear empty states
- clear failure guidance
- Quick Drop feedback

Do not add advanced favorites, icon systems, automatic name classification or general tagging unless concrete use proves it necessary.

---

# Native proof requirements

v0.4 is not complete on model/unit tests alone. Final proof must run in native Windows YMM4.

At minimum verify:

1. v0.3 regression Golden Paths.
2. Library DisplayName differs from the YMM4 Template name.
3. one LibraryEntry can appear in multiple Palettes.
4. broken Template references are not silently rebound.
5. Voice / Face single selection temporarily changes Character Palette and restores manual selection afterwards.
6. Quick Drop uses CurrentFrame and intrinsic Length.
7. Quick Drop creates no association tag.
8. Front / Back uses Character-related Layer ordering and the whole placement interval.
9. multiple Face Items can be intentionally stacked on distinct Layers.
10. Next Same Character + MaxGap behaves as designed.
11. Layer Band accounts for existing Items and already planned Items.
12. associated Voice placement can be explicitly resynced.
13. missing / ambiguous resync Targets are skipped, not guessed.
14. resync uses the current Preset.
15. successful placement/resync operations use native Undo as a single operation.
16. normal placement never automatically deletes existing Timeline Items.

---

# CI policy

Native Windows YMM4 proof remains the main validation lane. YMM4 4.55.1.1 Lite stays the fixed compatibility baseline until explicitly revised.

Heavy native work is relevant for changes to:

- `src/**/*.cs`, `src/**/*.csproj`, `src/**/*.xaml`
- `tests/**/*.cs`, `tests/**/*.csproj`, `tests/**/*.ps1`
- fixture data
- native workflow
- explicit `workflow_dispatch`

**Documentation-only changes must not download or launch YMM4.** Keep the existing docs-only guard / scope self-tests.

Successful implementation artifacts should continue to include the normal distributable, source, native proof, provenance and deterministic assertions. Do not package YMM4 itself or proof-only fixture code into the user distribution.
