# Tachie Preset P7/P8 checkpoint

Status: **P7 + P8 NATIVE GREEN / P9 ACTIVE**

## Exact evidence

- Product source: `8af10fe7c2043d3df590f27e0bb10c6e2bccf64e`
- Checkpoint run #450: `35681188429`
- Native job: `106598311305`
- Artifact: `10674818960` (`native-yymm4-checkpoint`)
- Artifact ZIP SHA256: `027f44694c8b089f769301be8a5152c88870471be8bb460ca5f8f542475d954f`
- Host: YMM4 4.55.1.1 Lite / Windows / .NET 10
- Markers:
  - `TACHIE_PRESET_PLANNING_P7=PASS`
  - `TACHIE_PRESET_IMMEDIATE_P8=PASS`
  - retained `HANDS_ON_ROUND2_E=PASS`
  - retained `EXPRESSION_PERFORMANCE=PASS`
  - full Checkpoint semantic regression/evidence guards: PASS

## P7 proved

- current Character/plugin/configuration is re-resolved from the immutable candidate;
- a fresh FaceParameter is created and the exact Strong candidate is re-applied;
- Experimental candidates remain inspection-only;
- stale capability identity fails before mutation;
- a fresh TachieFaceItem receives the newly-applied FaceParameter;
- existing ExpressionPreset span and LayerPlanner semantics are reused;
- exact managed replacement/removal is preflighted through PlacementPlan;
- existing TachiePreset StateHash is checked before destructive replacement;
- planning/revalidation is Timeline/settings zero-write.

## P8 proved

- Template -> TachiePreset exact replacement;
- TachiePreset -> TachiePreset exact whole-bundle replacement;
- TachiePreset -> none exact removal;
- TachiePreset -> Template exact replacement;
- valid other-source state is preserved across source switching;
- repeated same-row immediate preset changes keep authoritative rows without redundant reload;
- one logical native trial Undo restores the initial state and Redo restores the final state;
- unrelated external edits are not captured into the expression trial;
- candidate identity/source mode is not persisted into the settings schema.

## Regression found and fixed

The first P8 run exposed a lifecycle race: an owned managed-expression replacement changed `Timeline.Items`, which triggered a redundant expression-row reload. A second preset choice made while that reload was active was ignored.

The fix suppresses only Voice-freshness handling for **owned expression Timeline mutations**. External Timeline changes, Voice changes, plugin/config changes and source changes remain observable. The final proof explicitly requires the row to remain authoritative and immediately ready for the next preset choice.

## Next boundary

P9 owns failure/unavailable UX only. It should not redesign the row model or placement engine.
