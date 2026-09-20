# v0.4 work log

## W1: frozen regression

Base main: fd2c2bfebdbf736d47c9f591290d162c68e4e178.
Native baseline: run 34842291705, source f716199c8f936bebcfa032f207504416f2b2df6c.
63 native assertions, P1-P9, distribution smoke and package PASS.
The difference from that source to base main is limited to AGENTS.md, README.md and docs/DESIGN.md.

## W2: add-only plan foundation — PASS

Source de3fab5969481b04ceaddb3e6837c02c69353c0c / native run 34844383866.
All selected outputs are independently cloned and full-span checked against existing and already planned items before a single native Items swap. Empty assignment is a no-op. No product delete/rebuild path remains.
The old P8 delete-empty-assignment expectation is intentionally replaced by preservation. Golden-path fixture changes explicitly delete old test items through the native host before testing a new assignment; this is not product behavior.
P1-P9, release identity smoke and package all PASS.

## W3: Library — PASS

Source 92056017ee8943af54b809fa4ed26b8889ba6bde / native run 34845631516.
80 native assertions. Short display name, real UI registration/relink, strict source ambiguity/missing handling, persistence reload, corrupt-settings preservation and native regressions PASS. Release/proof builds pass with warnings treated as errors.
Character references currently use exact names with an actual-object uniqueness guard when used as palette context; duplicate names do not select a guessed Character. Template locators store exact Name, serialized Path and SceneId (not claimed unique). No Template body is persisted.

## W4: Palettes and public selection context — native proof pending

Separate Library and palette membership. Manual Character selection is persisted independently of a temporary context override. Public Timeline selection events only; no frame/voice-move polling. Style remains manual. One Library entry can be used in multiple palettes. Settings writes have a cooperating-instance exclusive lock plus external-edit digest check.
