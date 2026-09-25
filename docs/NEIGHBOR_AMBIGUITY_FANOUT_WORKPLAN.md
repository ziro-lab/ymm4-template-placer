# Neighbor Ambiguity Fan-out Workplan

Status: **ACTIVE**

## P0 — resolver

- add deterministic nearest-group result enumeration;
- apply MaximumNeighborGap before ambiguity evaluation;
- deduplicate final resolved times;
- keep strict single-result API;
- bound distinct fan-out results.

## P1 — independent Source forks

- add a guarded normalized Source fork;
- preserve Source semantic/current-state validation;
- use fresh Item references for every result.

## P2 — multi-result execution plan

- plan every resolved time with cumulative planned occupancy;
- combine all plans only after full preflight;
- expose a stable ambiguity signature and captured context for confirmation.

## P3 — bounded second-execution confirmation

- retain only an in-memory pending ambiguity token/context;
- first execution arms and stops;
- second exact execution commits all results;
- changed relevant state re-arms rather than confirming.

## P4 — native validation

- resolver semantics;
- zero-write first execution;
- stale-confirmation rejection;
- exact full fan-out + Undo/Redo;
- failed-alternative atomicity;
- Template and registered Tachie Preset parity.

## P5 — owner hands-on

Do not add more Neighbor selectors while evaluating this friction fix.
