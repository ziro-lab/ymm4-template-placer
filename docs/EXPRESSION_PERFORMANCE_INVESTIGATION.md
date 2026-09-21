# Expression workspace performance investigation

Status: **CAPTURED / INPUT TO FROZEN DESIGN**

Baseline:

- source `bb19b8f114194f5a746f9e2e3e73103d3be9ff67`
- product binary lineage: Release #338 / source `0ef2811f79970faf116ef31e8de5ae8b7ee30081`
- owner Hands-on observation: opening Template Placer and repeated switching in/out of `表情をまとめて` becomes slower as Voice count grows.

This document records the current hot paths before optimization. It is not implementation approval by itself.

## 1. Confirmed current behavior

### 1.1 Initial Tool/timeline attachment eagerly builds the expression rows

`SetTimelineToolInfo()` calls `Refresh()` when the Timeline changes.

`Refresh()` currently performs:

```text
ExpressionCatalog()
VoiceSnapshot.Capture(current)
-> Timeline.Items.OfType<VoiceItem>()
-> sort every Voice by Frame / Layer
-> construct AssignmentRow for every Voice
SetRows(...)
-> clear old Rows
-> add every new row one by one
-> restore association state for every row
```

Therefore expression-row preparation can happen even when the user has not opened `表情をまとめて`.

### 1.2 Voice freshness monitoring is active for every non-empty Tool task

`TaskNavigationViewModel.SetActiveTask()` currently uses:

```text
SetVoiceFreshnessActive(task.Length > 0)
```

so Voice monitoring remains active in placement/settings tasks, not only while the expression workspace is visible.

Entering the expression task also explicitly requests a freshness check.

### 1.3 A freshness check captures and compares the complete Voice list

`CheckVoiceFreshness()` currently does:

```text
VoiceSnapshot.Capture(timeline)
-> enumerate Timeline.Items
-> filter every Voice
-> sort every Voice
-> create every VoiceSnapshot

VoiceRowsMatch(...)
-> compare the complete row list
```

If different and no protected pending work exists, it rebuilds the complete expression row set.

### 1.4 Timeline.Items replacement rewires all Voice subscriptions

When `Timeline.Items` changes:

```text
RewireVoiceItems()
-> Timeline.Items.OfType<VoiceItem>()
-> rebuild HashSet
-> diff every watched Voice

RequestVoiceFreshnessCheck()
-> later VoiceSnapshot.Capture(timeline)
```

This means one Items change can currently cause two complete Voice enumerations before any row rebuild work.

### 1.5 Association restoration can repeatedly scan the entire Timeline

`SetRows()` calls `RestoreExpressionChoiceFromTimeline(row)` for each relative-expression row.

That reaches `ManagedIntentExpressionReader.Read(timeline, voice)`.

For an associated Voice, `Read()` may enumerate Timeline items multiple times to:

- confirm target Voice membership;
- find Voices with the same association serial;
- find source items with the serial;
- validate tagged bundle members;
- detect duplicate group IDs.

Doing this once per Voice during a full row rebuild can approach:

```text
Voice count x Timeline item count
```

rather than one Timeline pass plus indexed lookups.

### 1.6 Candidate filtering is repeated per Voice

Each `AssignmentRow` builds candidates through `IntentExpressionCatalog.Choices()` / `TemplateCatalog.ForVoice()`.

The catalog is filtered again for each Voice, even though many Voices share the same CharacterName and therefore the same effective candidate set.

### 1.7 Intent catalog construction has avoidable repeated lookups

`IntentExpressionCatalog.Read()` currently:

- walks expression candidate Sets/entries;
- calls `settings.Library.SingleOrDefault(...)` per entry;
- resolves bundles per candidate.

Library lookup is therefore linear inside another loop.

### 1.8 Candidate refresh revisits every row

`RefreshExpressionVocabulary()`:

- rebuilds the catalog;
- iterates all Rows;
- refreshes candidates for every row;
- restores expression association state for every row.

It can be called by settings changes, workspace attachment/mode changes, and SceneName notifications.

### 1.9 Summary / command admission can re-enumerate rows

`Summary` is computed from multiple `Rows.Count(predicate)` calls.

Expression command admission and `ShowExpressionBatchPlace` can also iterate Rows. In relative-expression mode, pending checks may call live association comparison logic per row.

These costs may be triggered repeatedly by WPF binding/CanExecute refresh, not only by explicit user actions.

### 1.10 Row replacement emits many collection notifications

`SetRows()` currently:

```text
Rows.Clear()
for every row:
    Rows.Add(row)
```

The DataGrid has row/column virtualization enabled, which is good, but the bound collection still receives one notification per row during a rebuild.

## 2. What is already efficient / should be preserved

- DataGrid row virtualization: enabled.
- DataGrid column virtualization: enabled.
- recycling virtualization: enabled.
- Timeline mutation is not performed by freshness checks.
- pending Excel/import work is protected from silent rebuild.
- Voice property changes are already coalesced through one Dispatcher operation.
- `Remark` changes are deliberately excluded from Voice freshness so managed association writes do not rebuild Rows.
- current placement safety / native Undo / exact association semantics are not performance targets.

## 3. Performance problem statement

The goal is not merely to make one benchmark faster.

The goal is:

> Template Placer should do the minimum expression-related work required for the user's current task, and expression loading must not unnecessarily block unrelated Tool operations.

The desired complexity direction is:

```text
tool open while not using expressions:
  ~ no expression-row work

full expression load:
  one bounded UI-thread host snapshot
  + O(Timeline items + Voices + candidate metadata)

single Voice property edit while expression tab is open:
  update/reconcile the affected Voice only where safe

candidate/settings change:
  rebuild candidate index once
  + refresh affected Rows without rescanning Timeline per Voice

tab leave:
  cancel/deactivate nonessential expression work immediately
```

Avoid repeated `Voice x Timeline` scans.

## 4. Evidence that must be added before calling the pass complete

Performance tests must cover at least synthetic current-scene sizes:

- 100 Voices;
- 500 Voices;
- 1,000 Voices.

Use additional non-Voice Timeline items so association indexing is tested against total Timeline size, not only Voice count.

Record structural counters in addition to elapsed time:

- full Timeline/Voice capture count;
- association-index build count;
- association item parse count;
- candidate-catalog build count;
- per-Character candidate-index build count;
- full Rows rebuild count;
- incremental row update count;
- collection Reset/range publish count;
- stale background result discard count;
- canceled expression load count.

Elapsed time may be recorded, but correctness acceptance must not rely only on machine-specific millisecond thresholds.
