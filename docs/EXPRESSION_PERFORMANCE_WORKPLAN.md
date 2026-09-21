# Expression workspace performance workplan

Status: **READY FOR IMPLEMENTATION AFTER FREEZE REVIEW**

Base: `work/ui-micro-polish-prep` at `bb19b8f114194f5a746f9e2e3e73103d3be9ff67`

Performance branch: `work/v0.4.2-expression-performance`

Authority:

1. `EXPRESSION_PERFORMANCE_INVESTIGATION.md`
2. `EXPRESSION_PERFORMANCE_DESIGN.md`
3. `EXPRESSION_PERFORMANCE_ACCEPTANCE.md`
4. this workplan

No product implementation should precede these frozen documents.

## P0 — instrumentation / regression fixture

Add proof-visible counters and large synthetic fixtures first.

Counters:

- host expression captures;
- full Voice reconcile;
- incremental Voice reconcile;
- association index build/items parsed;
- candidate catalog build;
- candidate-key index build;
- full Rows publish;
- incremental row update;
- batch collection publish;
- canceled load generation;
- stale result discard.

Add 100 / 500 / 1,000 Voice fixtures with non-Voice items and managed tags.

Do not optimize yet in P0 except what is necessary to expose counters without changing behavior.

## P1 — split eager global refresh from expression refresh

Refactor `Refresh()` / Timeline attach so placement/settings state can refresh without constructing expression Rows.

Introduce explicit expression cache state:

- Uninitialized;
- Loading;
- Current;
- Dirty;
- StalePending.

Timeline attach marks expression cache dirty/uninitialized unless pending same-session work requires restoration.

Do not change placement or settings semantics.

## P2 — expression-task-only watchers

Change Voice freshness activation to expression task only.

On expression enter:

- bind once;
- capture current host expression snapshot once;
- attach relevant Voice handlers from that capture.

On expression exit:

- detach handlers;
- cancel current freshness/load generation.

Combine watcher reconciliation with the same full capture used by row freshness.

## P3 — current-generation asynchronous coordinator

Introduce one coordinator owning:

- generation id;
- CancellationTokenSource;
- current Timeline identity;
- settings/candidate generation;
- dirty Voice set;
- full-reconcile flag.

Implement:

```text
UI capture
-> Task.Run pure preparation
-> Dispatcher latest-valid publish
```

No background host-object property reads.

Add `IsExpressionLoading` and nonmodal loading copy.

Prove tab switching remains operational while a held worker is running.

## P4 — association snapshot/index

Create immutable captured item/tag DTOs and an association index.

Use it for:

- row initial association restore;
- row read-only match state;
- pending aggregate computation.

Keep `ManagedIntentExpressionReader.Read` for mutation/current guards where exact live validation is required.

Remove full-build per-Voice live reader calls.

## P5 — candidate catalog/index

Optimize `IntentExpressionCatalog.Read()`:

- dictionary Library lookup;
- explicit candidate generation id/cache;
- candidate metadata/hashes precomputed where safe.

Build candidate choices by effective key/Character once.

Rows reference immutable candidate choice plans.

Invalidate cache only on relevant settings/template-source generation changes.

## P6 — row reuse and batch publication

Replace Clear + N Add publication.

Implement row reconciliation keyed by Voice reference identity.

Support:

- unchanged row reuse;
- changed row replacement/update;
- add/remove;
- sort reorder.

Use one bounded collection Reset/range publish for full rebuild.

Preserve pending/unavailable selections exactly.

## P7 — incremental Voice property reconciliation

Use dirty Voice events.

For Serif/Character/Length changes:

- recapture affected Voice primitive snapshot on UI thread;
- update affected row/candidate key/association state only.

For Frame/Layer:

- update affected row and sorted order.

Do not enumerate all Timeline items for a Voice property change.

If an event lacks enough reliable identity/state, fail over to one coalesced full reconcile rather than guessing.

## P8 — cached aggregates / command admission

Add maintained expression aggregate state.

Replace row-wide/live-scan getters and CanExecute paths for:

- Summary;
- selected/no-candidate/unavailable counts;
- pending relative assignment count;
- ShowExpressionBatchPlace;
- non-mutating command admission.

Mutation commands still run exact current validation.

## P9 — vocabulary generation/deferred refresh

Make `RefreshExpressionVocabulary()` generation-aware.

Remove duplicate same-generation refreshes.

When inactive:

- mark candidate dirty only.

When active:

- rebuild catalog/key index once;
- update/reuse Rows;
- reuse current association snapshot if still valid.

Review SceneName notification route and remove redundant vocabulary refresh.

## P10 — pending/transient work compatibility

Revalidate:

- pending Excel rows;
- transient Tool recreation;
- scene switch;
- unavailable selections;
- legacy expression mode retained compatibility.

No pending work may be lost because loading became asynchronous/lazy.

## P11 — performance Checkpoint

Run full performance structural suite plus retained semantic Checkpoint.

Required:

- 100/500/1,000 fixtures pass structural scaling invariants;
- no background thread host access;
- latest-wins/cancel behavior;
- all previous Native semantic gates.

Fix only evidence-backed failures.

## P12 — Release

Move exact candidate to dedicated native-validation branch.

Require:

- full Release;
- exact distribution DLL smoke;
- verified .ymme/source/provenance package;
- performance proof artifact/counters.

Produce owner Hands-on .ymme.

Do not merge performance PR until owner acceptance.

## Stop conditions

Stop and reopen design if:

- background preparation requires reading mutable YMM4 properties off UI thread;
- async publication would weaken pending-assignment safety;
- caching requires persistent serialized state;
- association indexing cannot preserve exact current fail-closed semantics;
- row reuse would require fuzzy Voice identity;
- Tachie Preset functionality starts entering this pass;
- optimization changes expression placement geometry/Undo semantics.
