# Expression workspace performance acceptance

Status: **FROZEN**

Authority:

1. `EXPRESSION_PERFORMANCE_INVESTIGATION.md`
2. `EXPRESSION_PERFORMANCE_DESIGN.md`
3. this document

## A. Lazy lifecycle

- A1 opening Template Placer on `配置` performs zero full expression-row builds.
- A2 opening Template Placer on `設定` performs zero full expression-row builds.
- A3 first entering `表情をまとめて` starts exactly one current-generation load.
- A4 returning to `表情をまとめて` with an unchanged valid cache does not rebuild all rows.
- A5 scene/timeline change invalidates the expression cache without forcing an eager full row build while another task is active.

## B. Responsive asynchronous loading

- B1 expensive pure expression preparation runs outside the UI thread.
- B2 YMM4 Timeline/Item/Template mutable properties are captured only on the UI thread.
- B3 the user can switch from expression to another Tool tab while the expression worker is running.
- B4 leaving expression cancels/deactivates the old generation without waiting for completion.
- B5 a canceled worker cannot publish Rows/status/error afterward.
- B6 if two loads overlap, only the newest valid generation may publish.
- B7 loading/reconcile writes zero Timeline items and zero settings bytes.
- B8 loading UI is nonmodal.

## C. Freshness scope

- C1 Voice watchers are inactive while active task is placement/settings/library/empty.
- C2 Voice watchers are active only while expression task requires freshness.
- C3 expression entry binds watchers and performs at most one initial full capture.
- C4 expression exit detaches Voice property watchers.
- C5 one relevant Voice property change is coalesced and handled without a full Timeline item enumeration.
- C6 one Voice Frame/Layer change correctly updates row ordering.
- C7 one Timeline.Items replacement causes one coalesced full host expression capture, not separate rewire + snapshot scans.

## D. Association indexing

- D1 one full expression snapshot parses each captured Timeline item's relevant association metadata at most once per index build.
- D2 full row restoration uses the captured association index.
- D3 full row restoration does not call the complete live `ManagedIntentExpressionReader.Read` once per Voice.
- D4 copied/duplicate/invalid association states produce the same fail-closed row result as before.
- D5 actual expression mutation still performs current live validation before commit.
- D6 existing exact manual/unassociated preservation behavior remains unchanged.

## E. Candidate indexing

- E1 one candidate catalog build creates one LibraryEntryId lookup rather than linear Library search per entry.
- E2 unchanged candidate generation reuses the catalog.
- E3 effective candidates are indexed per Voice matching key/Character rather than filtering the full catalog per row.
- E4 1,000 Voices sharing one Character do not cause 1,000 complete catalog scans.
- E5 relevant settings/template change invalidates candidate generation exactly once.
- E6 candidate disappearance remains explicit unavailable state; no fuzzy replacement occurs.
- E7 mutation-time source/current validation remains exact.

## F. Row reuse/publication

- F1 unchanged Voice rows are reused across incremental freshness reconciliation.
- F2 one changed Voice does not recreate every AssignmentRow.
- F3 removed Voice detaches its row handler.
- F4 added Voice gets one new row with correct sorted position.
- F5 full rebuild publishes the row collection with one bounded Reset/range notification, not one Add notification per Voice.
- F6 DataGrid row/column recycling virtualization remains enabled.
- F7 pending/unavailable selections survive row reuse when still exactly valid.
- F8 currently selected Voice row remains selected when that same Voice survives publication where practical.

## G. Cached aggregate state

- G1 `Summary` does not enumerate the complete Rows collection on every getter read.
- G2 `ShowExpressionBatchPlace` does not trigger live Timeline scans.
- G3 command CanExecute evaluation does not trigger per-Voice full association reads.
- G4 selected/no-candidate/unavailable/pending counters remain correct after full load, one-row change, add/remove and candidate refresh.
- G5 actual Place/Apply still validates exact live state before writing.

## H. Vocabulary refresh

- H1 workspace attachment does not eagerly rebuild expression candidates when expression task is inactive.
- H2 candidate generation change while inactive is recorded as dirty and deferred.
- H3 active vocabulary refresh builds candidate index once then updates rows.
- H4 vocabulary refresh reuses the current association index where valid.
- H5 SceneName/task synchronization does not cause a duplicate same-generation full vocabulary refresh.

## I. Large fixture structural scaling

For 100 / 500 / 1,000 Voice fixtures:

- I1 placement-tab Tool open performs zero expression full-row builds at every size.
- I2 first expression load performs one full host expression capture.
- I3 association-index build count is one per full load.
- I4 association parsed-item count scales with Timeline items, not Voices x Timeline items.
- I5 candidate character-index builds scale with distinct candidate keys/Characters, not Voice count.
- I6 full row publish count is one.
- I7 collection batch publish count is one.
- I8 repeated expression/placement tab switching on unchanged data does not linearly accumulate queued loads.
- I9 no stale generation publishes after rapid switching.

Elapsed timings are recorded in proof output but are diagnostic, not the sole pass/fail criterion.

## J. Pending work / Excel safety

- J1 protected pending assignments still block silent row replacement.
- J2 leaving/reentering expression does not discard pending assignments.
- J3 stale pending state remains explicit.
- J4 Excel import/export format and semantics are unchanged.
- J5 manual refresh confirmation remains when it would discard pending work.

## K. Retained semantic gates

- K1 Item-owned Set Round 2 behavior remains green.
- K2 quick-settings Round 2 behavior remains green.
- K3 expression immediate replacement/removal remains atomic.
- K4 exact managed association ambiguity/copy/missing fail-closed behavior remains green.
- K5 expression trial native Undo remains one logical operation.
- K6 Preview/navigation latest-wins behavior remains green.
- K7 normal placement remains add-only.
- K8 settings persistence safety remains green.
- K9 package/license/provenance gates remain green.
- K10 Tachie Preset PR #19 remains outside this pass.

## Owner Hands-on

Owner acceptance should specifically test:

- large scenes opening Template Placer while staying on `配置`;
- first opening `表情をまとめて`;
- rapidly switching expression <-> placement/settings;
- editing Voices while expression is open;
- returning after edits made while expression was not open;
- responsiveness while the expression list says it is loading;
- 100s of Voice items with real usual expression Sets.
