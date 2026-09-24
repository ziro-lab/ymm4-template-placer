# Baseline Simplification Acceptance

Status: **RELEASE ACCEPTED — MAIN PROMOTION PENDING**

## B1 — current runtime is singular

There is no user-accessible or session-only switch that changes the product back to the legacy Palette/Selection workspace.

## B2 — LegacyWorkspace persistence no longer controls runtime

The current settings model does not persist or consume a `LegacyWorkspace` runtime flag.

A historical JSON property named `LegacyWorkspace` must not reactivate old behavior.

## B3 — current surfaces remain authoritative

Placement, Settings and expression work continue through the current Intent/Set surfaces and current expression routes.

## B4 — current input behavior is unchanged

Timeline pointer routing, position shortcuts, Generic placement and whole-Tool Settings wheel behavior remain available under their existing current-task admission rules.

## B5 — current Settings safety is unchanged

Automatic protected persistence, rollback, corruption handling, external-change rejection, cross-instance locking and Portable authority remain unchanged.

## B6 — no Timeline semantic change

P1 performs no placement-model change. Existing current placement preflight, add-only behavior, managed expression replacement and native Undo contracts remain unchanged.

## B7 — old serialized flag is harmless

A settings JSON containing an extra historical `LegacyWorkspace` property loads without enabling a legacy workspace and can be saved back as current settings without that property.

## B8 — compatibility readers are not removed accidentally

P1 does not remove historical managed-association readers or current source identity compatibility.

## B9 — current Native regression is GREEN

Checkpoint must pass after P1. Release is required before P1 is promoted as the new simplification baseline.

## B10 — further deletion is evidence-driven

Legacy Selection/Palette/ExpressionPreset families are not removed merely by name. P2 must prove current reachability and data consequences first.


## Consolidation acceptance — current candidate

The pre-Preview maintenance candidate is accepted at Checkpoint when all of the following hold:

- distribution builds contain no serialized or executable `LegacyWorkspace` product path;
- an old JSON `LegacyWorkspace` property cannot reactivate old behavior;
- current Context -> Set -> tile / Settings / expression routes remain authoritative;
- retained association/source identity and Settings safety compatibility remain intact;
- historical workspace-specific UX contracts may retire only when equal-or-stronger current named invariants remain;
- workflow evidence is validated by schema + required current native stages, not by a historical fixed check count;
- the full current Checkpoint has zero Native assertion failures.

Checkpoint #691 satisfies the semantic gate with **1,198 assertions PASS / 0 FAIL**.

Release #707 (`35982411699`) at exact source `800b9bd261180f016ab3c1f26e67df1b55fd1eda` satisfies the Release gate: **1,198 PASS / 0 FAIL**, exact distribution-DLL identity smoke PASS, and verified `.ymme` / source / provenance packaging PASS. Release artifact SHA256 is `5391a61783b4333b2abd7e419cb895e622b14fb47e997bb222ed16a05a32943b`.

Main merge and main Release remain required before Baseline Simplification is marked complete.
