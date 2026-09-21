# UI Micro Polish — corrective implementation status

Status: **C0-C4 RELEASE GREEN / OWNER HANDS-ON ROUND 2 DESIGN FROZEN / IMPLEMENTATION READY**

## Release-green baseline before owner Hands-on Round 2

Exact candidate:

- source `1b8a88255da2e28289996b4d99470a195d6ec6a4`
- Release run #305 `35551459441`
- 1,443 Native assertions PASS / 0 FAIL
- Release/Proof build: 0 warnings / 0 errors
- `HANDS_ON_ROUND4_A/B/C=PASS`
- exact distribution DLL native smoke PASS
- verified stable-root `.ymme` / source / provenance package PASS
- `LICENSE.md` included in distributable and source archive

C0-C4 remain valid accepted implementation/evidence for:

- rejected direct Generic digit route removed;
- wheel success stays quiet;
- Voice row-boundary common-height resize + numeric entry;
- responsive Fixed grid;
- owner-Window deactivation closing quick settings.

PR #20 was intentionally not merged before owner Hands-on.

## Owner Hands-on Round 2 feedback

Owner testing of the Release #305 candidate produced three additional findings:

### F8 — quick settings light-dismiss

The Popup closes on owner Window deactivation, but clicking another place inside YMM4 does not close it.

Wanted:

- internal quick-settings interaction keeps it open;
- click elsewhere in the owning YMM4 Window closes it;
- existing deactivation close remains.

### F9 — targeted Sets should be Item-owned

Current code has a global `ShowAllSets` management mode and permits one `IntentPalette` to target multiple Item types.

Wanted normal model:

```text
Item type -> its own Sets -> entries
```

Set management should never turn into a global all-targeted-Set list.

### F10 — cross-Item reuse by snapshot copy

Reuse between Item types should be explicit copy, not one shared Set.

A copied Set is a one-time snapshot and becomes fully independent.

## Round 2 authority

Frozen documents:

- `UI_MICRO_POLISH_HANDS_ON_ROUND2_FEEDBACK.md`
- `UI_MICRO_POLISH_HANDS_ON_ROUND2_DESIGN.md`
- `UI_MICRO_POLISH_HANDS_ON_ROUND2_ACCEPTANCE.md`
- `UI_MICRO_POLISH_HANDS_ON_ROUND2_WORKPLAN.md`
- `UI_MICRO_POLISH_HANDS_ON_ROUND2_IMPLEMENTATION_PREP.md`

## Frozen implementation direction

### Quick settings

Keep `Popup.StaysOpen=True` and existing owner `Deactivated` handling.

Add one Popup-lifetime-scoped owner `PreviewMouseDown` route:

- internal Popup interaction stays open;
- click elsewhere in owner Window closes;
- outside click is not swallowed;
- no application/global/OS mouse hook.

### Item-owned normal Sets

Normal targeted Set:

```text
Target.ItemTypeKeys.Count == 1
TypeMatch == UniformType
```

Do not add a second serialized owner field.

Remove normal `ShowAllSets` behavior and normal multi-type target creation/editing.

Keep:

- same-type multi-selection count rules;
- optional CharacterName restriction;
- current relation/entry semantics.

### Existing multi-type Sets

Preserve existing serialized multi-type Sets losslessly.

If any exist, expose them only through a bounded `複数種類` compatibility context.

Do not silently split, broaden or assign them to an arbitrary Item parent.

### Cross-Item copy

Inside Set management:

- same-owner `複製` remains;
- add explicit copy-to-another-Item operation;
- new Guid;
- destination gets exactly one Item type / `UniformType`;
- all other Set state is copied as the current snapshot;
- no live link;
- destination-local name collision handling;
- protected Settings persistence only;
- zero Timeline writes.

## Current branch state

Everything after Release baseline `1b8a8825...` in this preparation is documentation-only.

No Round 2 product code has been changed yet.

## Next action

Implement the frozen workplan in PR #20:

1. R2C0 quick-settings light-dismiss;
2. R2C1 remove global targeted-Set management;
3. R2C2 single-owner normal creation/editor;
4. R2C3 owner-local naming/order;
5. R2C4 cross-Item snapshot copy;
6. R2C5 bounded legacy multi-type compatibility;
7. update native proof/docs;
8. full Checkpoint;
9. exact Release;
10. new owner Hands-on;
11. merge PR #20 only after acceptance;
12. then refresh paused Tachie Preset PR #19 from accepted main.
