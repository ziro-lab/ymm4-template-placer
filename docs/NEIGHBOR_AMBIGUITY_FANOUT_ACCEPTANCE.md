# Neighbor Ambiguity Fan-out Acceptance

Status: **ACTIVE**

## P0 — result resolver

Native proof must establish:

1. nearest same-frame candidates can resolve to one distinct result and no longer fail merely because candidate count > 1;
2. different candidate ends under an end-based relation produce multiple distinct results;
3. saved MaximumNeighborGap may remove one tied previous candidate before ambiguity is evaluated;
4. the existing strict `Resolve` API still rejects multiple distinct results;
5. result count is bounded and deterministic.

## P1 — first / second execution

For a real targeted tile:

1. first multi-result execution writes neither Timeline nor settings;
2. user receives a concise second-execution explanation;
3. same Set/tile/settings/selection/scene/result signature on the next execution confirms;
4. changed selection, Neighbor geometry, settings, Set or tile cannot consume the old confirmation;
5. there is no time-based double-click window.

## P2 — atomic fan-out

Native proof must establish:

1. every distinct result gets independent Source Items;
2. later results plan against earlier planned occupancy;
3. overlapping results move only through the existing saved layer policy;
4. one failed alternative causes zero writes;
5. success commits all alternatives as **one native Undo**;
6. one Redo restores the exact full fan-out result.

## P3 — Placement Source parity

Prove the multi-result path for:

- Template Source;
- registered Tachie Preset Source.

No Source kind may reuse the same pending mutable Item instance across alternatives.

## P4 — owner hands-on

Check:

- first-stop notice is understandable;
- second press feels intentional rather than surprising;
- deleting the unwanted result is faster than manual Template placement;
- the behavior does not make accidental duplicate placement easy.
