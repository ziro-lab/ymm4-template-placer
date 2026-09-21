# Expression workspace performance design

Status: **FROZEN FOR IMPLEMENTATION**

Investigation authority: `EXPRESSION_PERFORMANCE_INVESTIGATION.md`.

This pass optimizes `表情をまとめて` and its supporting freshness/candidate/association paths without changing placement semantics.

Tachie Preset / experimental expression source work remains separate in Draft PR #19 and starts only after this pass is accepted.

---

## 1. Primary product requirements

### 1.1 No eager expression workload when the user is doing another task

Opening Template Placer on `配置` or `設定` must not build the full Voice expression row model merely because a Timeline is attached.

Expression rows are initialized lazily when:

- `表情をまとめて` is first entered;
- an expression-specific command explicitly requires them;
- pending same-session expression work must be restored.

### 1.2 Other Tool operations remain usable while expression rows are loading

Expression loading must not hold the UI thread for the complete expensive preparation.

The pipeline is:

```text
UI thread:
  capture the minimum immutable host snapshot

background:
  sort/index/compare/candidate-plan/association-plan

UI thread:
  publish the latest completed result in one bounded update
```

The user must be able to switch tabs while background preparation is running.

Leaving `表情をまとめて` cancels/deactivates nonessential in-flight expression work.

### 1.3 Latest-wins

Every asynchronous expression load/reconcile has a monotonically increasing generation.

Only the latest generation for the current Timeline/settings/task may publish.

Stale results are discarded without mutating:

- Rows;
- Timeline;
- settings;
- pending assignments;
- user-visible error state unrelated to that generation.

---

## 2. Thread-safety boundary

YMM4 Timeline/Item/Template host objects are treated as UI-thread-affine unless proven otherwise.

Background work must not read mutable host properties directly.

### UI-thread capture may read

- current Timeline identity;
- immutable copy of Timeline item references plus required primitive snapshots;
- Voice primitive snapshot: reference identity, CharacterName, Frame, Length, Serif, Layer, Remark;
- non-Voice primitive snapshot required for association validation: reference identity, Group, Remark, marker presence;
- current settings snapshot / immutable copied DTOs;
- resolved candidate metadata that requires host Template access.

### Background work may use only captured immutable data

It may:

- sort Voice snapshots;
- build dictionaries/indexes;
- parse copied Remark strings;
- compare snapshots;
- build candidate choice plans;
- compute summary counts;
- plan row reuse/replacement;
- compute association state from captured descriptors.

### UI-thread publish

Before publication, verify:

- same Timeline identity;
- same load generation;
- same expression task eligibility;
- settings/candidate generation still current.

Actual placement/mutation actions still perform their existing exact live revalidation.

---

## 3. Lazy expression lifecycle

Split current global `Refresh()` responsibilities.

### Timeline attach / scene change

Always refresh only the state needed for normal placement/settings.

Do not perform:

- full VoiceSnapshot capture for the expression list;
- full AssignmentRow construction;
- full association restoration;

unless expression data is actually required.

Mark expression data as dirty/uninitialized.

### Enter expression task

If a valid current cache exists for the same Timeline/settings generation:

- show it immediately;
- perform only the minimum freshness reconciliation required.

Otherwise:

- start the asynchronous expression load pipeline;
- expose a nonmodal loading state.

### Leave expression task

- stop Voice watchers;
- cancel pending freshness/load generation;
- keep the last completed Rows cache for fast return;
- do not destroy user selections/pending work.

---

## 4. Voice freshness scope

Change active monitoring from:

```text
task.Length > 0
```

to expression-task-only behavior.

### While expression task is inactive

Do not subscribe to every Voice.

Mark expression cache potentially stale through the Timeline/task generation boundary.

### When expression task becomes active

- bind current Timeline once;
- capture/reconcile current Voices once;
- subscribe to current Voice objects after the initial capture;
- request no duplicate immediate full scan.

### VoiceItem property change

For relevant Voice properties:

- coalesce changes;
- track dirty Voice references;
- update/reconcile only those Voices where safe;
- if Frame/Layer changes affect sort order, reorder cached row data without rescanning all Timeline items.

### Timeline.Items replacement

One capture operation must serve both:

- watcher reconciliation;
- Voice list reconciliation.

Do not perform an immediate `RewireVoiceItems()` full Voice enumeration followed by another independent `VoiceSnapshot.Capture()`.

---

## 5. Association index

Introduce one captured-expression association index per full Timeline expression snapshot.

Conceptual data:

```text
Voice reference -> captured Voice association state
serial -> captured Voices
serial -> captured source items
group Guid -> captured tagged members
item reference -> parsed source/intent tag state
```

Parse each captured item's relevant Remark/tag data once per full snapshot.

Use this index for list-building/read-only expression association restoration.

Do not call `ManagedIntentExpressionReader.Read(timeline, voice)` once per row during a full list build.

### Safety rule

The index is a read optimization only.

Actual mutation/admission paths retain exact live-current validation before commit.

---

## 6. Candidate catalog/index

### 6.1 Build Library lookup once

During `IntentExpressionCatalog.Read()`, build:

```text
LibraryEntryId -> LibraryEntry
```

once instead of `SingleOrDefault` per entry.

### 6.2 Cache immutable catalog per settings/template generation

Do not rebuild the same expression catalog repeatedly when:

- settings candidate membership has not changed;
- Template source generation has not changed.

Invalidate explicitly on relevant settings/template operations.

### 6.3 Build candidate choices per Character

For the current expression source mode, build candidate plans keyed by the effective Voice matching key, initially:

```text
Voice runtime type + CharacterName
```

For normal Voice rows this collapses repeated linear catalog filtering for every row.

Rows sharing the same effective candidate key may share immutable candidate choice data.

### 6.4 Precompute stable candidate descriptor hashes

Where a candidate bundle geometry hash is stable for the captured catalog generation, calculate it once and reuse it during row-association matching.

Actual mutation still recomputes/revalidates current state before write.

---

## 7. Row model and publication

### 7.1 Reuse rows when the same Voice is still present

Key by Voice reference identity.

For unchanged Voices:

- retain the existing `AssignmentRow` when candidate generation and source mode permit;
- do not replace it merely because another Voice changed.

For changed Voice snapshots:

- update/replace only the affected row state.

### 7.2 Bulk publication

A full rebuild must not publish N individual `Rows.Add` collection changes.

Use a collection implementation or publication mechanism that can:

- replace/reorder a batch;
- emit one bounded Reset/range notification.

DataGrid virtualization remains enabled.

### 7.3 Preserve selection/pending state

Row reuse/bulk replacement must preserve:

- exact pending assignment selection when still valid;
- unavailable explicit selection state;
- pending Excel protections;
- currently selected DataGrid row when the same Voice survives.

No fuzzy repair.

---

## 8. Cached aggregate state

Do not recompute expensive row-wide state every time WPF reads a property.

Maintain/update cached aggregates for at least:

- total row count;
- selected count;
- unselected-with-candidates count;
- no-candidate count;
- unavailable selection count;
- pending relative-assignment count.

`Summary`, `ShowExpressionBatchPlace`, and command admission use these aggregates.

### Pending match state

For list/read UI purposes, each row keeps captured association-match state.

Do not call live Timeline association scans from a repeatedly evaluated WPF property/CanExecute path.

Before actual placement/change, existing live guards still run.

---

## 9. Vocabulary refresh

`RefreshExpressionVocabulary()` becomes generation-aware.

If candidate membership/generation did not change:

- skip full row candidate rebuild.

If candidate membership changed while expression task is inactive:

- mark candidate cache dirty;
- defer row refresh until expression task is entered.

If active:

- rebuild candidate index once;
- update rows against the new index;
- use the already captured association index rather than rescanning Timeline per row.

Remove redundant vocabulary refresh triggered only because `SceneName` changed when the same refresh generation already covers the scene transition.

---

## 10. Loading UX

Expose:

- `IsExpressionLoading`;
- concise status text such as `表情一覧を読み込み中…`.

Rules:

- no modal dialog;
- other tabs remain selectable;
- placement/settings task interaction remains enabled;
- previous completed expression rows may remain visible until the new generation publishes, but stale rows cannot execute unsafe expression changes;
- first-ever load may show an empty/loading surface;
- canceled/stale load does not flash an error.

Optional progress counts may be shown only if they are essentially free to compute. Do not add a chatty progress-update loop.

---

## 11. Cancellation / coalescing

Use one expression load/freshness coordinator.

It owns:

- current generation;
- CancellationTokenSource;
- dirty Voice set;
- pending full-reconcile flag;
- candidate generation;
- association snapshot generation.

Coalesce bursts.

Do not queue an unbounded chain of Dispatcher/background jobs when the user rapidly switches tabs or edits several Voices.

---

## 12. Performance instrumentation

Add debug/native-proof counters, not persistent telemetry.

At minimum expose proof-visible counters for:

- host snapshot captures;
- Timeline item snapshot count;
- full Voice reconciles;
- incremental Voice reconciles;
- association-index builds;
- association parsed-item count;
- candidate catalog builds;
- candidate character-index builds;
- row full publishes;
- row incremental updates;
- collection batch Reset/range publishes;
- canceled generations;
- stale result discards.

Use `Stopwatch` timings in proof artifacts for diagnosis only.

No user data leaves the machine.

---

## 13. Synthetic performance fixtures

Native/performance proof should exercise:

- 100 Voices;
- 500 Voices;
- 1,000 Voices;

plus non-Voice items and managed association tags.

Scenarios:

1. open Template Placer on placement tab;
2. first enter expression tab;
3. expression -> placement -> expression repeated switching;
4. leave expression while load is intentionally held/in progress;
5. change one Voice Serif;
6. change one Voice Frame;
7. add/remove one Voice via Timeline.Items replacement;
8. candidate/settings generation change;
9. protected pending Excel/assignment state;
10. scene switch.

Prefer structural scaling assertions over fragile absolute time limits.

---

## 14. Hard performance invariants

The accepted implementation must ensure:

- opening Tool on placement/settings performs zero full expression row builds;
- expression watchers are inactive outside expression task;
- leaving expression cancels/deactivates in-flight nonessential work;
- a stale background generation never publishes;
- a full expression load parses each Timeline item's association metadata at most once per association-index build;
- a full row build does not invoke a complete live Timeline association read once per Voice;
- candidate filtering is not linear over the complete catalog once per Voice;
- one relevant Voice property change does not enumerate all Timeline items;
- one Timeline.Items replacement causes at most one full host expression snapshot for that coalesced change;
- full row publication does not emit one collection-add notification per Voice;
- WPF summary/CanExecute reads do not trigger Timeline scans;
- load/reconcile itself writes zero Timeline/settings state.

---

## 15. Out of scope

Do not change:

- expression placement geometry;
- association tag format;
- placement relation semantics;
- native Undo behavior;
- Excel file format;
- Set ownership model;
- quick-settings behavior;
- Tachie Preset source implementation;
- normal placement engine;
- settings schema.

Do not:

- access YMM4 mutable host properties from a background thread;
- add a persistent cache database;
- paginate away Voices or hide rows to fake speed;
- drop exact association validation for speed;
- use timers/polling when event/generation invalidation is sufficient.

---

## 16. Completion boundary

This performance pass is complete only when:

1. all structural acceptance gates pass;
2. retained semantic Checkpoint passes;
3. Release passes exact DLL smoke/package verification;
4. owner Hands-on confirms large-Voice navigation/tabs no longer produce the observed interaction stall.

Only then should Tachie Preset PR #19 be rebased/refreshed on the accepted result.
