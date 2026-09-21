# Expression workspace performance implementation prep

Status: **IMPLEMENTATION-READY**

This maps the frozen performance design to current source.

## Baseline source

- PR #20 performance parent: `bb19b8f114194f5a746f9e2e3e73103d3be9ff67`
- last product Release candidate: `0ef2811f79970faf116ef31e8de5ae8b7ee30081`
- Release #338: green
- 1,463 Native assertions PASS / 0 FAIL

Performance work starts from the accepted Round 2 behavior and must preserve it.

## Current hot-path map

### `PlacerViewModel.SetTimelineToolInfo`

Currently calls `Refresh()` on Timeline change.

Refactor target:

- normal Tool/placement refresh;
- expression cache invalidation;
- lazy expression load.

### `PlacerViewModel.Refresh`

Currently performs eager:

- `ExpressionCatalog()`;
- `VoiceSnapshot.Capture()`;
- all `AssignmentRow` construction;
- `SetRows()`.

Split it.

### `TaskNavigationViewModel.SetActiveTask`

Current:

```text
SetVoiceFreshnessActive(task.Length > 0)
if changed && expression -> RequestVoiceFreshnessCheck()
```

Target:

- expression-only freshness lifecycle;
- coordinator enter/exit;
- cancel on leave.

### `ExpressionVoiceFreshness`

Current responsibilities:

- Voice subscriptions;
- Dispatcher freshness coalescing;
- full Voice snapshot;
- full Rows rebuild.

Refactor into:

- UI host capture;
- watcher reconcile;
- dirty Voice tracking;
- coordinator request;
- latest-wins publish.

### `VoiceSnapshot.Capture`

Current implementation scans/sorts full current Timeline.

Retain a pure Voice snapshot shape, but centralize full capture so one Timeline.Items replacement cannot trigger duplicate scans.

### `ManagedIntentExpressionReader.Read`

Current exact live reader remains authoritative for mutation safety.

Add a separate immutable **captured association index** for bulk/list read.

Do not weaken or replace mutation guards.

### `IntentExpressionCatalog.Read / Choices`

Current repeated costs:

- Library `SingleOrDefault` per entry;
- full catalog filter per Voice;
- alias duplicate grouping per row.

Add:

- Library dictionary;
- candidate generation cache;
- per matching-key/Character choice index.

### `AssignmentRow`

May gain cached/read-only fields for:

- effective candidate key;
- captured association match/pending state.

Do not make it own Timeline reads.

### `SetRows`

Replace:

```text
Clear
Add x N
association live restore x N
```

with row reconciliation and one batch publication.

### `Summary / ShowExpressionBatchPlace / UpdateCommands`

Move to maintained aggregates.

No Timeline scan from property getters or CanExecute.

### `PlacerView.xaml`

Keep DataGrid virtualization.

Add only lightweight loading state/surface.

Do not overlay a modal blocker over all Tool tabs.

## Proposed new source units

Names are implementation suggestions; exact filenames may vary while responsibilities remain fixed.

### `ExpressionLoadCoordinator.cs`

Owns:

- generation/cancellation;
- asynchronous preparation lifecycle;
- latest-wins publication.

### `ExpressionHostSnapshot.cs`

Immutable UI-thread-captured DTOs:

- Timeline identity;
- Voice snapshots;
- item association primitives;
- settings/candidate generation identifiers.

### `ExpressionAssociationIndex.cs`

Pure index built from captured DTOs.

### `ExpressionCandidateIndex.cs`

Pure/cacheable effective candidate lookup by Voice matching key.

### `ExpressionRowCollection.cs`

Batch/reconcile-capable collection if needed.

Do not introduce a generic framework beyond the expression use case.

## Proof seam

Under `YMM4_PROOF`, expose structural counters through one compact diagnostics snapshot rather than scattering public test-only properties.

Suggested shape:

```text
ExpressionPerformanceDiagnostics
- HostCaptures
- FullVoiceReconciles
- IncrementalVoiceReconciles
- AssociationIndexBuilds
- AssociationItemsParsed
- CandidateCatalogBuilds
- CandidateKeyBuilds
- FullRowPublishes
- IncrementalRowUpdates
- BatchCollectionPublishes
- CancelledLoads
- StaleResultsDiscarded
```

Reset only in test fixture setup.

## Thread-affinity proof seam

Add a proof helper/guard that records capture thread and worker thread.

Native tests must demonstrate:

- host snapshot capture on Dispatcher thread;
- pure preparation on non-Dispatcher thread;
- publication on Dispatcher thread.

Do not intentionally access a YMM4 host property off-thread merely to prove it fails.

## Performance fixture strategy

Synthetic rows should be generated deterministically.

Suggested sizes:

```text
100 Voice + 200 other items
500 Voice + 1,000 other items
1,000 Voice + 2,000 other items
```

Include:

- several CharacterNames;
- some unassociated Voices;
- valid managed associations;
- malformed/duplicate association fixtures only in separate correctness cases.

For scaling assertions, inspect counters rather than comparing exact wall time.

## Implementation order rationale

Order is fixed:

```text
instrument
-> lazy split
-> expression-only watchers
-> async coordinator
-> association index
-> candidate index
-> row reuse/batch
-> incremental Voice
-> aggregate cache
-> vocabulary defer
-> pending/transient compatibility
-> Checkpoint
-> Release
```

Reason:

- instrumentation makes regressions visible;
- lazy/task scoping removes unnecessary work before concurrency is introduced;
- one coordinator is established before adding caches;
- indexes remove quadratic/repeated work;
- row publication/aggregate work comes after the data pipeline is stable.

Do not reorder into premature micro-optimizations unless a build blocker requires it.
