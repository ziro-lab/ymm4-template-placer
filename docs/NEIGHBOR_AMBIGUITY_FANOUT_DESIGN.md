# Neighbor Ambiguity Fan-out Design

Status: **IMPLEMENTED / RELEASE GREEN — OWNER HANDS-ON PENDING**

## Goal

Reduce avoidable placement failures when several equally-ranked Neighbor candidates exist.

Current behavior rejects the whole operation when the nearest candidate start frame is shared by multiple matching Items. That is too conservative for two common cases:

1. several candidate Items produce the **same final placement result**;
2. several candidate Items produce different results, but placing all results is still more useful than forcing a fully manual Template workflow.

The new rule is result-oriented:

> Resolve possible placement results, not one guessed Neighbor Item.

## Result resolution

For a Neighbor relation:

1. collect the nearest matching Neighbor group using the existing previous/next and same-type / same-Character rules;
2. apply the existing saved maximum-gap rule to that nearest group;
3. calculate the final `ResolvedIntentTime` for every remaining candidate;
4. deduplicate identical `Frame / Length / Skip` results.

No candidate is selected by layer, arbitrary enumeration order or fuzzy similarity.

### One distinct result

Place immediately.

Example:

- candidate A: start 260 / end 280
- candidate B: start 260 / end 340
- relation: until Neighbor **start**

Both resolve to the same end frame, therefore there is one placement result and no confirmation is needed.

### Multiple distinct results

The first execution performs **zero Timeline writes** and arms one bounded confirmation.

User-facing meaning:

> 配置候補が複数あります。もう一度同じタイルを押すと、候補ごとの結果をすべて配置します。不要な方は削除するか、YMM4の「元に戻す」1回で戻せます。

The second execution confirms only when all relevant state is still the same:

- current Set;
- current tile / Placement Source;
- complete current settings snapshot;
- selected Items;
- captured scene / Neighbor state;
- distinct result signature.

There is no double-click timeout.

If any relevant state changed, the old confirmation is invalid and the current execution becomes a new first execution.

## Fan-out commit

On confirmed second execution:

1. materialize one fresh Source;
2. fork independent fresh normalized Source content for each distinct result;
3. plan results in deterministic result order;
4. each later result sees existing Timeline Items **plus earlier planned results** as occupancy;
5. if every result preflights successfully, combine them into one `PlacementPlan`;
6. commit once through YMM4 native Undo.

Therefore overlapping alternatives may move to another free layer according to the existing saved one-direction layer policy.

If any alternative cannot be materialized or placed inside the saved layer policy, the entire fan-out fails with **zero writes**. Never place only a subset.

## Source support

The behavior applies to both current Placement Source kinds:

- Template;
- registered Tachie Preset Source.

A materialized Source is never reused as the same mutable Item instances for several results. Each result owns independent pending Items.

## Bounds

Distinct ambiguity results are bounded to **32** for one operation.

More than 32 distinct results is treated as an unsupported ambiguous case and performs zero writes. This prevents a pathological Timeline state from turning one confirmation into an unexpectedly huge fan-out operation.

## Existing strict API

The existing single-result `IntentRelationResolver.Resolve` remains strict for callers that require exactly one result.

A new multi-result resolver is the placement authority. This keeps old validation callers explicit while allowing the product placement path to use result fan-out.

## Failure / safety rules

- first ambiguous execution: zero write;
- changed state before second execution: zero write and re-arm current ambiguity;
- failed alternative preflight: zero write;
- successful fan-out: one native Undo unit;
- no custom undo stack;
- no fuzzy candidate choice;
- no partial success.

## Exit

- equal-position candidates with one distinct result place immediately;
- multiple distinct results stop on first execution;
- exact same ambiguity places all distinct results on second execution;
- changed selection / scene / Set / tile / settings invalidates confirmation;
- alternatives avoid one another through existing occupancy/layer planning;
- Template and registered Tachie Preset Sources both own independent pending Items;
- fan-out commit is one native Undo.
