# UI Micro Polish — owner Hands-on Round 2 implementation prep

Status: **IMPLEMENTATION-READY**

This document maps the frozen design to the current source at the Release #305 baseline.

## Current baseline

Corrective Release-green source before this documentation-only prep:

- `1b8a88255da2e28289996b4d99470a195d6ec6a4`
- Release run #305 `35551459441`
- 1,443 Native assertions PASS / 0 FAIL
- exact distribution DLL smoke PASS
- verified package PASS

The commits after that baseline in this prep are documentation-only.

## Verified current implementation facts

### Quick settings

`IntentPalettePanel.xaml` currently has:

```xml
<Popup ... StaysOpen="True" ... />
```

`IntentPalettePanel.xaml.cs` already owns:

- owner Window observation;
- `Window.Deactivated` close;
- Popup close -> toggle reset;
- unload/DataContext/visibility close paths.

The missing behavior is same-owner-window outside click dismissal.

### Current targeted Set model

`IntentPalette` owns:

```text
Target.ItemTypeKeys : List<string>
Target.TypeMatch    : UniformType | ExactMixedTypes
```

Normal Settings currently allows:

- multiple type checkboxes;
- `ExactMixedTypes`;
- one Set to match multiple Item types.

### Current global management behavior

`IntentSettingsNavigation.cs` currently has:

```text
ShowAllSets
VisiblePalettes.Filter = ShowAllSets || MatchesContext(...)
```

`IntentSettingsPanel.xaml` binds:

```text
セットの管理 Expander.IsExpanded <-> ShowAllSets
```

Therefore merely expanding management intentionally broadens the picker to all targeted Sets. This exactly explains owner feedback F9.

### Current open-settings fallback

`IntentTileEditing.OpenSettingsForSet()` currently does:

```text
if target Set is not visible:
    session.ShowAllSets = true
```

This must be replaced by owner-context navigation.

### Current proof dependencies

Historical proof explicitly expects the behavior now rejected by Hands-on:

- `NativeHandsOnUxPolishProof.cs` asserts `ShowAllSets=true` exposes both Voice/Text Sets.
- `NativeHandsOnRound3SettingsProof.cs` asserts mixed/multiple target editing remains in collapsed Advanced.
- core/relative proofs still exercise multi-type matching semantics.

The first two assertions must change. Core multi-type execution proof should remain as compatibility coverage unless the implementation removes that runtime support, which this design does not.

## Proposed code shape

### 1. Owner classification helpers

Add small pure helpers rather than spreading list/count checks:

```text
IsNormalSingleOwner(IntentPalette/Draft)
TryGetOwnerTypeKey(...)
IsLegacyMultiType(...)
```

These may live beside Intent settings/navigation or in `IntentPaletteModels.cs` if they are model-level and side-effect free.

Do not add serialized state.

### 2. Settings contexts

Keep existing per-type `IntentSettingsItemContext`.

Add one synthetic compatibility context only when required:

```text
Key   = "multi-type-compat"
Label = "複数種類"
```

It is not Generic and is not a real runtime Item type.

Filtering rules:

- Generic context -> GenericSets only;
- single Item context -> only normal Sets whose sole owner key equals that context;
- compatibility context -> only legacy multi-type Sets;
- current Timeline selection -> normal single-owner Sets for the one selected runtime type, then current count/Character filtering;
- mixed runtime current selection -> no normal single-owner Set list; existing multi-type runtime execution may remain outside normal creation.

### 3. Remove `ShowAllSets`

Delete the normal navigation flag and every persistence/rollback dependency on it.

`SetManagement.IsExpanded` becomes ordinary local presentation state or an unbound Expander.

If preserving the expander open/closed visual state is useful, keep it as view state only; it must not change model filtering.

### 4. Copy destination state

Recommended Settings-session state:

```text
ObservableCollection<IntentSettingsItemContext> CopyDestinations
IntentSettingsItemContext? SelectedCopyDestination
CanCopyToOtherItem
CopySelectedSetToDestination()
```

Destination collection should be derived from the known real single Item contexts.

Exclude:

- Generic;
- current owner;
- synthetic current-selection context;
- multi-type compatibility context.

No destination state needs persistence.

### 5. Snapshot copy algorithm

Conceptual implementation:

```text
source = SelectedPalette.Build()          // validates current draft
owner  = selected destination TypeKey

copy = deep copy(source)
copy.Id = Guid.NewGuid()
copy.Name = UniqueNameWithinOwner(source.Name, owner)
copy.Target.ItemTypeKeys = [owner]
copy.Target.TypeMatch = UniformType

AddDraft(copy)
navigate to owner context
select copy
MarkDirty()
```

All other fields remain untouched.

Do not copy through serialized source Item bodies; this is copying Set settings/reference metadata only.

### 6. Owner-local move

Current `MovePalette(delta)` uses the index in the global `Palettes` collection.

Replace for normal single-owner context with:

1. obtain visible owner-local sequence;
2. find selected local index;
3. find adjacent local Set in requested direction;
4. swap their positions in the backing `Palettes` collection, or otherwise move selected immediately before/after that adjacent owner Set;
5. preserve relative order of unrelated-owner Sets.

Compatibility context may use the same local-sequence algorithm over legacy multi-type Sets.

### 7. Owner-local naming

Replace global `UniqueName(stem)` use in normal Set creation/copy with:

```text
UniqueName(stem, ownerTypeKey)
```

Same-owner duplicate uses source owner.

Cross-owner copy checks only destination owner.

Legacy compatibility duplicate may retain a compatibility-local suffix rule.

### 8. Mixed current-selection creation

Current `Create(selection)` creates `ExactMixedTypes` when multiple runtime types are selected.

Change admission before constructing the draft:

```text
distinct runtime type count != 1
-> reject with:
   "複数種類を選択中です。作成先のアイテム種類を上で選んでからセットを作成してください。"
```

Same-type multi-selection retains count bounds.

### 9. Editor simplification

In `IntentSettingsPanel.xaml`:

- remove normal `TargetTypeChoices` matrix;
- remove normal `TypeMatchPanel`;
- rename `対象の詳細・複数種類` to a targeted-condition label such as `対象の詳細`;
- keep selection count;
- keep Character restriction.

For the compatibility context, do not reuse the normal editor in a way that lets the user accidentally rewrite legacy multi-type ownership. The simplest acceptable path is read-only ownership summary plus the existing non-owner relation/entry editing only if it can be done without rebuilding the target.

If that requires too much branching, compatibility Set editing may be limited to copy/delete/navigation while exact old data remains preserved.

### 10. Quick-settings outside click

Recommended source route:

```text
Popup opened
-> attach owner Window PreviewMouseDown handledEventsToo=true

owner PreviewMouseDown
-> if source is quick-settings toggle: return
-> PanelQuickSettingsButton.IsChecked = false
-> do not set e.Handled

Popup closed/unloaded/owner change
-> detach owner PreviewMouseDown
```

Keep the existing `Deactivated` handler.

Use one attach/detach helper so repeated open/close cannot duplicate handlers.

## Proof map

### Existing proof to revise

`NativeHandsOnUxPolishProof.cs`

Replace:

```text
ShowAllSets=true -> both Sets visible
```

with:

```text
SetManagement expanded -> still only current owner's Set visible
switch parent -> only destination owner's Set visible
same names across owners allowed
```

`NativeHandsOnRound3SettingsProof.cs`

Replace the normal multi-type-editor expectation with:

```text
normal Settings has one Item owner and no type matrix
legacy multi-type fixture produces compatibility context only
```

### New/extended proof

`NativeHandsOnRound4PresentationProof.cs`

Add real mouse sequence:

1. open quick settings;
2. click/use internal layout control -> still open;
3. reopen if needed;
4. native click a harmless control/area in owner YMM4 Window outside Popup -> closes;
5. assert the outside event/control still receives its normal behavior;
6. retain existing Deactivated proof.

### Settings copy proof

Use a fixture with at least:

- Voice owner Set;
- Text owner Set with same display name;
- one legacy multi-type Set.

Prove:

- scoped picker;
- owner-local duplicate/order;
- Voice -> Text copy;
- exact snapshot;
- new Guid;
- destination-local suffix only if needed;
- source/destination independence after edits;
- legacy multi-type untouched;
- no Timeline mutation.

## Expected docs changes after implementation

- `USAGE.md`: explain Item -> Set ownership and cross-Item copy.
- `CURRENT_ARCHITECTURE.md`: normal targeted Sets are single-owner; legacy multi-type compatibility retained.
- `GLOSSARY.md`: Set owner definition if useful.
- remove docs wording that advertises normal multi-type target editing.

## Validation cadence

1. implement R2C0-R2C5;
2. source-only focused checks where possible;
3. update native proof;
4. full Checkpoint;
5. fix only concrete failures;
6. Release on exact candidate;
7. owner Hands-on;
8. merge PR #20 only after acceptance.

## Scope guard

Do not use this round to:

- remove legacy Preset families;
- merge Generic and targeted Set schemas;
- redesign relation primitives;
- change expression candidate semantics;
- implement Tachie Preset;
- introduce Placement Recipe;
- purge compatibility architecture.
