# UI Micro Polish — corrective implementation status

Status: **OWNER HANDS-ON ROUND 2 IMPLEMENTED / CHECKPOINT PENDING**

## Release-green baseline before owner Hands-on Round 2

Exact accepted automation baseline:

- source `1b8a88255da2e28289996b4d99470a195d6ec6a4`
- Release run #305 `35551459441`
- 1,443 Native assertions PASS / 0 FAIL
- Release/Proof build: 0 warnings / 0 errors
- `HANDS_ON_ROUND4_A/B/C=PASS`
- exact distribution DLL native smoke PASS
- verified stable-root `.ymme` / source / provenance package PASS
- `LICENSE.md` included in distributable and source archive

C0-C4 from the first corrective pass remain preserved.

## Owner Hands-on Round 2 feedback

Release #305 owner testing produced:

- F8: quick settings did not close when clicking elsewhere inside YMM4;
- F9: targeted Sets were too globally visible/shared across Item types;
- F10: cross-Item reuse should be a one-time copy rather than shared ownership.

Frozen authority:

- `UI_MICRO_POLISH_HANDS_ON_ROUND2_FEEDBACK.md`
- `UI_MICRO_POLISH_HANDS_ON_ROUND2_DESIGN.md`
- `UI_MICRO_POLISH_HANDS_ON_ROUND2_ACCEPTANCE.md`
- `UI_MICRO_POLISH_HANDS_ON_ROUND2_WORKPLAN.md`
- `UI_MICRO_POLISH_HANDS_ON_ROUND2_IMPLEMENTATION_PREP.md`

## Implemented Round 2 scope

### R2C0 — quick-settings light-dismiss

Implemented in `IntentPalettePanel`:

- keeps `Popup.StaysOpen=True`;
- existing owner `Window.Deactivated` close remains;
- one Popup-lifetime owner `PreviewMouseDown` route closes on clicks elsewhere in that owning YMM4 Window;
- the outside click is never marked handled;
- Popup-internal interaction remains outside the owner Window route;
- subscription is detached on Popup close / unload / owner replacement;
- no application-global InputManager hook, polling or Win32 global mouse hook.

Native Round4 proof now includes:

- real internal shape-button interaction staying open;
- real internal ComboBox opening staying open;
- real owner-window outside click closing the Popup;
- delivery of that same click to the intended YMM4 control;
- existing owner deactivation / no-reactivation reopen proof.

### R2C1/R2C2 — Item-owned normal targeted Sets

Normal targeted Settings now use:

```text
Item type -> its Sets -> entries
```

Normal creation:

- exactly one `ItemTypeKey`;
- `UniformType`;
- same-runtime-type multi-selection still carries count/Character conditions;
- mixed runtime selection cannot create a new shared multi-type Set.

Removed from the normal Settings surface:

- global `ShowAllSets` behavior;
- runtime Item-type checkbox matrix;
- normal `ExactMixedTypes` choice.

Expanding `セットの管理` no longer broadens the visible Set list.

No new serialized owner field or settings revision was introduced.

### R2C3 — owner-local naming and ordering

Implemented:

- Set names are disambiguated only inside one Item owner;
- two Item types may use the same Set display name;
- duplicate uses the same owner;
- move up/down reorders among owner siblings and preserves other owners' relative order.

### R2C4 — cross-Item snapshot copy

Added to `セットの管理`:

```text
他のアイテムへコピー
[コピー先] [コピー]
```

Copy behavior:

- validates the current Set draft;
- creates a new Guid;
- assigns exactly the destination Item type + `UniformType`;
- copies relation, count/Character conditions, expression-candidate flag, entries/order/appearance/entry overrides;
- destination-local name collision uses the numbered suffix convention;
- source and destination are independent after copying;
- navigates directly to the new destination Set;
- uses the existing protected Settings auto-commit path;
- writes zero Timeline items.

### R2C5 — bounded existing multi-type compatibility

Existing multi-type Sets remain stored and executable under their existing semantics.

Settings:

- adds `複数種類` only when such data exists;
- never assigns old multi-type data to an arbitrary Item owner;
- does not allow normal creation in that context;
- allows snapshot-copying the old Set into one normal Item owner;
- leaves the legacy source target unchanged.

No destructive migration or revision bump.

### Settings navigation cleanup

Tile -> `Setの設定を開く` now resolves:

- normal Set -> exact owning Item parent;
- legacy multi-type Set -> bounded `複数種類` context.

It no longer falls back to a global all-Set mode.

Session rollback no longer stores/restores global Set visibility state.

## Proof/docs/package updates

Updated/extended proof covers:

- Item parent filtering;
- Set-management expansion staying scoped;
- same Set name under different owners;
- cross-Item copy persistence;
- owner-local reorder;
- copied Set independence;
- mixed-selection creation rejection;
- legacy multi-type roundtrip + bounded compatibility parent;
- quick-settings light-dismiss.

Updated:

- `docs/USAGE.md`;
- `docs/CURRENT_ARCHITECTURE.md`;
- `docs/GLOSSARY.md`;
- `tests/PackageVerified.ps1`.

Package gate now requires the Round 2 usage wording and `ItemOwnedSetSettings.cs` in the source archive.

## Validation status

Intermediate run #327 reached:

- checkout: PASS;
- .NET/YMM4 setup: PASS;
- Distribution/Proof build: PASS;

but the Native job was cancelled by later commits under the PR concurrency policy. It is **not** acceptance evidence.

A fresh uninterrupted Checkpoint from the final Round 2 proof HEAD is required next.

## Next action

1. complete the last Round 2 proof assertions;
2. run one uninterrupted full Checkpoint;
3. fix only evidence-backed failures;
4. update this status with exact green HEAD/run;
5. fast-forward `work/v0.4-native-validation` to that exact candidate;
6. run Release;
7. require exact distribution-DLL smoke + verified `.ymme` / source / provenance package;
8. produce a new owner Hands-on `.ymme`;
9. owner Hands-on;
10. merge PR #20 only after acceptance;
11. then refresh paused Tachie Preset PR #19 from accepted main.
