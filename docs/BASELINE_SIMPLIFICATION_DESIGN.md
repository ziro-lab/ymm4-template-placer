# Baseline Simplification Design

Status: **RELEASE CANDIDATE — P1-P4 COMPLETE / P5 PROMOTION ACTIVE**

## Goal

Reduce maintenance cost before Behavior Preview by removing compatibility that keeps superseded product paths executable, while retaining compatibility that protects real user data or existing Timeline projects.

This is a baseline simplification phase, not a feature phase.

## Context

Template Placer has not been publicly announced and the only known user is the current developer/user. This makes the current pre-publication baseline the safest point to remove compatibility paths that would otherwise become long-lived obligations.

The final Phase 4 baseline is main merge commit `5a44e45f7ac1966c51e5bb41125d086f18119c54`, with main Release #657 GREEN.

Validation growth has also crossed the repository's own review threshold: Checkpoint/Release Native assertions grew from the reviewed 1,428 baseline to 1,822 assertions (~27.6%). The maintenance target is therefore both runtime architecture and validation history.

## Compatibility policy

Separate **old data readability** from **old product executability**.

Preferred direction:

```text
old persisted data
    ↓ bounded importer/migrator when still needed
current Settings model
    ↓
current UI / current placement engine only
```

Do not preserve an entire old UI or old placement workflow merely because old serialized fields once existed.

## Compatibility classes

### A — retain strongly

Retain compatibility that can otherwise make an existing Timeline project unsafe or ambiguous:

- historical Plugin-managed association-tag readers still needed to identify previously placed managed bundles;
- current Template identity / strict resolution compatibility;
- native Undo / association safety boundaries;
- corrupt/future/external-change Settings rejection.

### B — retain temporarily as migration-only

May remain only as bounded import/migration code when it still protects the current user's existing settings:

- LocalAppData -> Portable settings migration;
- settings revisions whose old form may still exist in the current user's retained backups;
- old serialized names where changing them provides little maintenance benefit.

These paths should not keep a second UI/runtime workflow alive.

### C — remove executable compatibility

Remove compatibility that preserves superseded product behavior rather than data:

- `LegacyWorkspace` session switch and serialized flag;
- legacy Palette / Selection workspace switching once all current callers use Context -> Set -> tile;
- historical UI navigation whose only purpose is returning to a superseded workspace.

### D — audit after P1

After LegacyWorkspace removal is GREEN, audit:

- SelectionPreset runtime/editor/UI families;
- legacy Palette UI/editor families while retaining any current generic-set data/model pieces;
- old standalone ExpressionPreset geometry/editor path, noting that current TachiePreset compatibility may still depend on a bounded subset;
- multi-step revision migrations that can be collapsed to current-only validation or one dedicated importer;
- Round2/3/4 and older UX tests that protect the same current invariant.

## P1 — LegacyWorkspace removal — COMPLETE

Remove the old workspace as an executable product path.

Required result:

- normal startup and every product operation use the current Context -> Set -> tile surface;
- `PlacerView` no longer swaps Palette/Selection tabs to legacy surfaces;
- `UseLegacyWorkspace`, `SetLegacyWorkspace`, open/close legacy commands and transient-work legacy mode identity disappear;
- current expression source behavior remains intact;
- current Generic placement, shortcuts, pointer routing, auto-commit and Settings behavior no longer branch on legacy mode;
- old JSON containing `LegacyWorkspace` is accepted as an unknown field by the current serializer and naturally disappears on a subsequent save;
- no Timeline mutation semantics change.

## P2 — executable legacy UI audit — COMPLETE

After P1:

1. prove which legacy View/ViewModel families have zero current product reachability;
2. remove them when they are not reused by current features;
3. keep data-only models only when needed for migration or a current compatibility operation;
4. do not remove reusable planners merely because their original UI was legacy.

Likely candidates include legacy Selection and Palette UI families. `QuickDropPlanner` is not presumed removable because current Generic placement reuses it.

## P3 — migration-chain simplification — COMPLETE

After current runtime paths are singular:

- decide the oldest settings shape that still needs support before first public release;
- collapse unnecessary incremental revision chains;
- prefer one bounded old->current conversion over permanent v0->v1->v2->... logic;
- once a format is intentionally unsupported, fail closed with the original file untouched.

## P4 — validation consolidation — COMPLETE

Consolidate historical UI/Round proofs into current invariant tests where the repository's retirement rules allow it.

Do not reduce data-loss / association / Undo / settings-conflict / host-boundary coverage.

Primary targets:

- repeated legacy-workspace switching proofs;
- repeated current-vs-legacy UI visibility checks;
- historical Hands-on tests whose only surviving contract is now a current invariant;
- repeated zero-Timeline-mutation checks for the same Settings operation.

## Implemented maintenance result

The current distribution has one normal product runtime path. `LegacyWorkspace` is no longer a persisted or executable product mode, while old Timeline association/source identity and protected Settings compatibility remain bounded current responsibilities.

The legacy-family audit intentionally did **not** turn into a filename purge. Selection/Palette/QuickDrop/ExpressionPreset pieces remain only where current reuse, secondary behavior, migration/association compatibility or historical proof still gives them a concrete responsibility.

Validation consolidation replaces mandatory historical workspace-specific UX ladders with current named invariants. Checkpoint #691 at candidate `d227e4c0fcb502e5a3abc970527ce447d0926ae5` passed **1,198 Native assertions / 0 failures**, down from the 1,822-assertion accepted Phase 4 baseline (~34.2% reduction).

Release promotion remains the final gate before Behavior Preview.

## Explicit non-goals

Do not remove in this phase without separate evidence:

- historical association readers needed by existing Timeline items;
- strict source identity;
- Settings atomic/digest/lock safety;
- native Undo safety;
- Template fidelity;
- current Tachie Preset source compatibility;
- current Portable settings authority.

## Exit

Baseline simplification completes when:

- one current product runtime path remains for normal placement/settings/expression operation;
- retained backward compatibility is documented as data migration or Timeline-identification compatibility, not executable duplicate UI;
- Checkpoint/Release are GREEN after consolidation;
- validation growth is reviewed and duplicate historical coverage is reduced where safe;
- Behavior Preview can start without building on top of known legacy runtime branches.
