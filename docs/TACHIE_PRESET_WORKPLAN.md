# Tachie Preset source — workplan

Status: FROZEN

## P0 — close product-bridge gaps in canonical Lab

Do not repeat the generic capability survey.

Add one small exact-host bridge experiment proving only:

1. current Character -> active tachie plugin/configuration resolution;
2. modern public ItemProperty binding construction;
3. fresh TachieFaceItem + fresh applied FaceParameter roundtrip;
4. editor cleanup/cancellation;
5. bounded deterministic state-fingerprint feasibility.

Exit: **DONE.** P0 markers/artifact are recorded in the handoff. Design D10 approves the bounded public-state fingerprint for proved candidate routes.

Pinned proof: Lab PR #62 / source `bfe13c6eb304e70401f5d6ba6d467a1ea4d8e1ca` / run `35619789546` / marker `PASS_TACHIE_PRESET_PRODUCT_BRIDGE_P0`.

## P1 — source-mode foundation

Add:

- session-local source mode;
- source switch UI;
- Template default;
- zero-write mode transitions;
- trial/load cancellation;
- protected pending-work block;
- Excel disabled/hidden in TachiePreset.

No detector yet.

Validation: **DONE.** The proof-file addition escalated this slice to Checkpoint rather than Focused.

Pinned proof:

- source `676491583c54e91e8278d69e1c0b31984feaf7d1`;
- run `35621563372`;
- job `106405620298`;
- artifact `10650011921`;
- artifact SHA256 `c06d6cbc3288dbda10acd02abf92482cc5c66e893bc989a360eac35e95817ca2`;
- marker `TACHIE_PRESET_SOURCE_MODE_P1=PASS`;
- existing Round 4 + Expression Performance: PASS;
- full Checkpoint semantic regression/evidence guards: PASS.

Exit: **DONE.** Source switching is session-local, Template-default, zero-write, cancellation-safe and pending-work-safe. Excel remains Template-only. No detector was added.

## P2 — immutable capability model

Add product types for:

- capability fingerprint;
- route kind;
- immutable TachiePreset candidate descriptor;
- Strong/Experimental/None result.

No Timeline mutation.

Validation: **DONE.**

- source `7d60808f02bae9450760be8b07aa67ef44a07a95`;
- Focused run `35622493374`;
- job `106408713620`;
- artifact `10649933330`;
- artifact SHA256 `36a8f04a5db1fc3b758968c5108f6c58f9712a8e31cfd35f4e9abda4363f34df`;
- marker `FOCUSED_NATIVE=PASS`.

Exit: **DONE.** The model contains bounded immutable identity only and no host/plugin/WPF/Timeline references. Duplicate candidate identities fail closed.

## P3 — bounded UI-affine capability coordinator

Implement exact Character/plugin resolution and fresh-parameter probing.

Requirements:

- distinct Character scope;
- session cache;
- generation/cancellation;
- temporary visual host only if needed;
- always clear bindings/remove staging visuals;
- immutable output only.

Validation: **DONE.** See `docs/TACHIE_PRESET_P3_CHECKPOINT.md`.

- source `16608d6e2621b8bedea76d04e3498fa6dcc8c2c0`;
- Checkpoint run `35630553092`;
- markers `TACHIE_PRESET_CAPABILITY_P3=PASS`, `TACHIE_PRESET_GUARDS_P3=PASS`.

Exit: **DONE.** Exact Character/plugin resolution, fresh-parameter probing, bounded editor routes, session cache, cancellation/latest-wins and unconditional cleanup are native-green.

## P4 — expression-load integration

Extend candidate generation so the existing Rows collection can project Template or immutable TachiePreset choices.

Keep:

- existing Voice snapshot capture;
- latest-wins background row preparation;
- row reuse;
- one Reset/range publish rule;
- cached aggregates.

Add structural counters for preset scans/cache hits/stale discards.

Validation: **DONE.** See `docs/TACHIE_PRESET_P4_P5_CHECKPOINT.md`.

- source `7cdc3355858851635a8dca85ba349326c3237427`;
- Checkpoint run `35661119036`;
- marker `TACHIE_PRESET_ROWS_P4=PASS`.

Exit: **DONE.** One Rows/DataGrid/load coordinator handles both sources; immutable preset descriptors stay inside the existing preparation/publication pipeline and candidate inspection remains zero-write.

## P5 — source-aware choice model

Minimally extend the historical choice model without creating a second row model.

Required states:

- none;
- Template;
- TachiePreset;
- valid current other-source association;
- unavailable same-source choice;
- invalid association.

Workbook remains Template-only.

Validation: **DONE.** See `docs/TACHIE_PRESET_P4_P5_CHECKPOINT.md`.

- source `7cdc3355858851635a8dca85ba349326c3237427`;
- Checkpoint run `35661119036`;
- marker `TACHIE_PRESET_CHOICE_MODEL_P5=PASS`;
- retained `HANDS_ON_ROUND2_E=PASS` and full Checkpoint regression PASS.

Exit: **DONE.** All six required source-aware row states are explicit without a second row model or Workbook change.

## P6 — common managed-expression seam

**DONE.** Pinned evidence: `docs/TACHIE_PRESET_P6_CHECKPOINT.md` / source `0a2710457ff635038f7a6cd83cf36418bd649b35` / Checkpoint `35662905394` / marker `TACHIE_PRESET_ASSOCIATION_P6=PASS`.

Keep existing IntentAssociationTag bytes unchanged.

Add:

- versioned TachiePresetAssociationTag;
- discriminated managed source descriptor;
- source-union association reader/index.

Prove current Template association fixtures unchanged before preset mutation.

Exit: **DONE.** Existing Template bytes/reader behavior are retained, preset tags use a separate versioned descriptor, live/background readers use one discriminated source union, and all mixed/missing/duplicate/copied cases fail closed before mutation.

## P7 — TachiePreset mutation planning

**DONE.** Pinned evidence: `docs/TACHIE_PRESET_P7_P8_CHECKPOINT.md` / source `8af10fe7c2043d3df590f27e0bb10c6e2bccf64e` / Checkpoint `35681188429` / marker `TACHIE_PRESET_PLANNING_P7=PASS`.

Implement:

- descriptor re-resolution;
- fresh FaceParameter;
- candidate apply/verify;
- fresh TachieFaceItem;
- existing ExpressionPreset geometry;
- exact preflight;
- PlacementPlan/native Undo.

No immediate UI replacement until planning is green.

Exit: **DONE.** Fresh re-resolution/application, ExpressionPreset/LayerPlanner planning, StateHash protection and PlacementPlan preflight are native-green.

## P8 — immediate trial and cross-source replacement

**DONE.** Pinned evidence: `docs/TACHIE_PRESET_P7_P8_CHECKPOINT.md` / source `8af10fe7c2043d3df590f27e0bb10c6e2bccf64e` / Checkpoint `35681188429` / marker `TACHIE_PRESET_IMMEDIATE_P8=PASS`.

Wire source-aware selection into existing trial session.

Prove:

- Template -> TachiePreset;
- TachiePreset -> Template;
- TachiePreset -> another TachiePreset;
- TachiePreset -> none/remove;
- one logical Undo initial -> final;
- unrelated mutation cannot be captured.

Exit: **DONE.** All four cross-source replacement/removal paths, one logical trial Undo/Redo and unrelated-edit isolation are native-green.

## P9 — failure/unavailable UX

**DONE.**

Pinned evidence:

- source `d47dadd98427163415ecfa1ab5a86eb283542643`;
- Checkpoint run `35691993002`;
- marker `TACHIE_PRESET_FAILURE_UX_P9=PASS`;
- artifact `10679101228`;
- artifact SHA256 `efcb4760d9470cc11f285dfbdb03c54d9314b973badb3a5ff27f39444b81172e`.

Covers:

- no-capability state;
- Experimental confidence indication;
- candidate-disappeared state;
- stale fingerprint message;
- current-other-source label;
- broken-plugin containment;
- failure/unavailable inspection paths remain Timeline/settings zero-write.

## P10 — performance checkpoint

**DONE.**

Pinned evidence:

- source `c958693e6b49ff9466b346c281a2a3bca7832abc`;
- Checkpoint run `35692347894`;
- marker `TACHIE_PRESET_PERFORMANCE_P10=PASS`;
- retained `EXPRESSION_PERFORMANCE=PASS`;
- artifact `10679561524`;
- artifact SHA256 `b3c7a47cec38b137ed4e8e8ade402073142f9aa1b6435324ab6755c202fcef47`.

Covers all existing expression performance invariants plus:

- scan work scales with distinct Characters, not Voice count;
- cancellation while editor discovery is in flight;
- zero preset work outside active mode;
- zero background mutable-host access;
- cross-source association negatives;
- no wall-time threshold is used.

## P11 — Release + owner Hands-on

**Release validation: GREEN. Owner Hands-on remains the merge gate.**

Release evidence:

- exact source `7ca758d3777e548743abc3cd88a52691f0ef1c48`;
- Release run `35693397211`;
- job `106634951593`;
- artifact `10679109416` (`native-yymm4-release`);
- artifact SHA256 `859855d51afb672c360076a5c4aebea7a69a0702637db1ca3a87c73f7f257e4d`;
- full Native: PASS;
- exact distribution DLL identity smoke: PASS;
- verified `.ymme` / source / provenance packaging: PASS.

Hands-on found one real compatibility gap: a third-party PSD tachie plugin exposes usable expression presets in YMM4, but the current direct-name fallback parser can report no candidates for its definition syntax.

Do not merge until owner accepts the refined expression-preset discovery path below.

## P12 — expression-item-first discovery + assisted calibration

### Product position

This is an **optional convenience source**, not a mandatory compatibility layer.

- The existing Template source remains the complete fallback and must stay fully usable.
- The goal is high practical coverage at low complexity, **not 100% expression-preset compatibility**.
- If a plugin cannot be recognized safely and cheaply, stop and let the user use Template.
- Some false-positive candidates are acceptable because the user can preview the placed expression item immediately and remove/Undo it.

User-facing terminology: **表情プリセット**.

### Primary automatic route: expression item first

The primary abstraction is the expression item that the user actually edits in YMM4, not the plugin's Character-side preset serialization.

Preferred route:

1. create a fresh `TachieFaceItem(character)`;
2. use its item-owned fresh FaceParameter;
3. inspect the public YMM4/property-editor surface for a plausible expression-preset selector;
4. enumerate finite candidate choices;
5. apply a candidate to fresh item state;
6. verify bounded state mutation/repeatability;
7. place that resulting item through the existing PlacementPlan/native Undo path.

Discovery priority:

1. expression-item public preset editor / selector;
2. direct preset capability on the item-owned FaceParameter;
3. Character-side public preset definitions as fallback only;
4. assisted calibration;
5. unsupported -> use Template.

Do not add plugin-name allowlists as the primary path.

### Detection policy: favor useful recall, keep mutation safety strict

Candidate discovery may be broader than the current Strong-only semantic filter.

- **Strong**: repeatable on fresh items, stable after editor cleanup, current fingerprint/route re-resolves.
- **Experimental**: plausible expression-preset surface and candidate can be exercised, but complete repeatability/semantic certainty is not proved.
- **None**: candidate/route cannot be exercised safely or is ambiguous beyond the accepted boundary.

It is acceptable to expose a plausible Experimental candidate and let the user judge it in Preview.

Do **not** relax the safety boundary:

- probing uses fresh uncommitted expression items;
- never probe by mutating existing Timeline content;
- Character default state must remain unchanged;
- discovery is bounded;
- temporary editor bindings/visuals must be cleaned;
- one plugin/Character failure remains local;
- placement remains reversible through existing native Undo.

### Assisted calibration: one user demonstration, then bulk recognition

If automatic discovery fails, provide a lightweight action such as:

`［表情プリセットを認識させる］`

Flow:

1. Template Placer places one clearly marked fresh expression item for the target Character;
2. ask the user to change **only its expression preset** to any other preset using normal YMM4 UI;
3. capture the marked item's before/after public FaceParameter state;
4. search plausible public editor/selector routes on fresh items;
5. exercise their candidates and find a route+choice that reproduces the demonstrated state;
6. when the match is unique enough, treat that route as the plugin's expression-preset adapter;
7. enumerate the selector's full current candidate list;
8. validate candidates on fresh items before normal use.

Do not record screen coordinates, fake input, UI Automation paths, or private editor/ViewModel fields.

### Learned adapter scope

The learned result is primarily **plugin/surface-level**, not a per-Character expression snapshot.

Persist/reuse a route descriptor keyed strongly enough to detect incompatible plugin changes, for example:

- plugin runtime type;
- plugin module MVID;
- CharacterParameter runtime type;
- FaceParameter runtime type;
- property/editor/selector route identity.

For each Character, re-enumerate that Character's current preset list through the learned adapter. Do not assume all Characters share preset names/content.

Before reuse, perform a lightweight validation on a fresh expression item. Plugin update / MVID / route incompatibility invalidates the learned adapter.

### Character-specific exception policy

Do **not** build a full Character-override system now.

If a learned plugin adapter fails on one Character:

1. retry normal automatic discovery for that Character;
2. allow assisted calibration for that Character;
3. only add a Character/config-fingerprint override later if real-world cases prove it necessary.

The architecture should leave room for this fallback without making it part of the initial implementation.

### Evidence already supporting this direction

Public Lab PR #94 proves the item-first bridge for YMM4 4.55.1.1 built-in Animation and PSD tachie.

- source `0950d6fac017b2cb400c6a605b65124392c5cd98`;
- run `35726212617`;
- job `106740366332`;
- artifact `10693248982`;
- artifact SHA256 `94652c5d3d8a4f61d499a3468a9205a7d456f4802dc5a5858e056ba84f83b587`;
- marker `PASS_EXPRESSION_ITEM_PRESET_SURFACE`.

The proof shows that a fresh `TachieFaceItem(character)` owns a fresh FaceParameter, exposes the expected preset choices through the public editor route, mutates only item-owned state, leaves the Character default untouched, converges to the same state across repeated fresh items, and retains the applied state after editor cleanup.

### Exit target

Stop this refinement when:

- common expression-item preset surfaces are discovered with useful coverage;
- one-demonstration assisted calibration works for otherwise missed compatible surfaces;
- learned adapters are invalidated safely on incompatible plugin changes;
- unsupported/ambiguous plugins fail locally and clearly;
- Template remains the reliable fallback.

Do not chase the last few percent of compatibility if doing so requires invasive reflection, plugin-specific branches, fragile UI automation, or a second placement/settings architecture.
