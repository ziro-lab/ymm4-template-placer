# Experimental Tachie Preset source — refreshed feature handoff

Status: **P0-P5 GREEN ON ACCEPTED v0.4.2 / READY FOR P6**

This handoff replaces the pre-performance assumptions of the original PR #19 preparation while retaining the canonical Lab evidence.

## Accepted base

Current accepted main before this feature:

- docs main: `817fdfd86539a28b7e8df5ce87eb54333550f259`;
- product merge: `de85c312347ea35371d1c58a92992b50f64cdeb6`;
- final product candidate: `f626e7c71385998b22a6d29e43a3fa349cf03f18`;
- Release run #399 `35608381259` / job `106361040827`;
- **1,518 Native assertions PASS / 0 FAIL**.

Do not re-open accepted UI/performance architecture as part of this feature.

## Canonical Lab evidence

Public Lab: `ziro-lab/chat-native-work-lab-001`, Draft PR #58.

Pinned result:

- source `621cff8199c6fe6daf54aa5ccd7c44e35e3c0ce7`;
- run `35488799463`;
- job `106019954003`;
- artifact `10598378796`;
- artifact SHA256 `150ea81535196330b53fbbf636f62b3c79c54f19ad420c877e81a4c8b98ffc37`;
- marker `PASS_GENERIC_EXPRESSION_PRESET_CAPABILITY_SURVEY`.

The Lab proved useful structural routes for:

- exact writable face-side `Preset : string` plus coherent CharacterParameter definitions;
- built-in Animation tachie preset editor;
- built-in PSD tachie preset editor;
- synthetic expanded-state preset editor;
- modern `PropertyEditorAttribute2 + IPropertyEditorForTachieParameterAttribute` contract shape;
- unrelated Preset-name noise rejection;
- local containment of a throwing editor.

It does **not** prove every third-party tachie plugin, perceptual correctness, or every product-lifecycle detail.

### P0 product-bridge evidence

Public Lab: `ziro-lab/chat-native-work-lab-001`, Draft PR #62.

Pinned final result:

- source `bfe13c6eb304e70401f5d6ba6d467a1ea4d8e1ca`;
- run `35619789546`;
- job `106399676909`;
- checkout `320ee6f5597925431949de5970d3ec1054717ee6`;
- tree `080d731d469b5bef3f0ca267503e715e4ddc8bb9`;
- artifact `10647189723`;
- artifact SHA256 `b45d7a3fb03d1c9e2b69c23ad284a9c6ee21a6b243f53e1e9d2b7ea32296e208`;
- marker `PASS_TACHIE_PRESET_PRODUCT_BRIDGE_P0`.

P0 closed the remaining bridge questions on exact YMM4 4.55.1.1 Lite:

- exact current Character -> active Animation/PSD tachie plugin + current CharacterParameter resolution;
- real public modern `ItemProperty[]` binding using public `ItemProperty(object, object, PropertyInfo, PropertiesCache)` plus public `PropertiesCache()`;
- deterministic bounded FaceParameter state fingerprints for built-in Animation, built-in PSD and the synthetic expanded-state modern-editor fixture;
- fresh `TachieFaceItem` retaining the applied fresh FaceParameter state for Animation/PSD;
- staging PropertyEditor cleanup after success, intentional post-bind failure and cancellation.

Built-in Animation/PSD application used the public legacy `SetBindings(FrameworkElement, object, object, PropertyInfo)` route; the separate modern fixture proved the public `ItemProperty[]` route. No private editor state, UI Automation, arbitrary assembly scan or Timeline mutation was required.

The bounded state fingerprint is approved for the first implementation when used only with a proved bounded candidate route. This is not a claim that every third-party Tachie plugin exposes complete semantic state through public properties.

### P1 source-mode evidence

Product branch source:

- source `676491583c54e91e8278d69e1c0b31984feaf7d1`;
- source tree `511926528df2766f2dd8a96a2b70ec1a1baa4c02`;
- Checkpoint run `35621563372`;
- native job `106405620298`;
- artifact `10650011921`;
- artifact SHA256 `c06d6cbc3288dbda10acd02abf92482cc5c66e893bc989a360eac35e95817ca2`;
- marker `TACHIE_PRESET_SOURCE_MODE_P1=PASS`.

The exact YMM4 4.55.1.1 Lite run proved:

- every live Tool session starts in Template source mode;
- switching into Tachie Preset cancels in-flight Template preparation and gates Template-only placement/refresh/Excel/resync/navigation paths;
- cached Template choices and Excel are not presented as Tachie Preset content;
- canceled/stale Template preparation cannot publish after the switch;
- source switching and switching back are Timeline/settings zero-write;
- a fresh ViewModel starts Template, so source mode remains session-local;
- real imported Template/Excel pending work blocks entry into Tachie Preset without discarding work;
- existing Round 4 and Expression Performance proofs remain green;
- full Checkpoint semantic regression/evidence guards pass.

P1 deliberately contains no Tachie Preset capability detector. Tachie Preset mode currently exposes only the source/placement-rule boundary and a truthful preparation state; P2/P3 add the immutable model and bounded detector.

### P2 immutable capability model evidence

Product source:

- source `7d60808f02bae9450760be8b07aa67ef44a07a95`;
- Focused run `35622493374`;
- native job `106408713620`;
- artifact `10649933330`;
- artifact SHA256 `36a8f04a5db1fc3b758968c5108f6c58f9712a8e31cfd35f4e9abda4363f34df`;
- marker `FOCUSED_NATIVE=PASS`.

P2 adds one pure immutable model file only:

- `TachiePresetCapabilityFingerprint`;
- `TachiePresetRouteDescriptor` + bounded route kind;
- `TachiePresetCandidateDescriptor`;
- `TachiePresetCapabilityResult` with `Strong / Experimental / None`.

The model stores only bounded strings, `Guid` and enums. It has no YMM4/plugin/WPF/Timeline object reference. Candidate identity duplicates fail closed to `None`; candidates with mixed confidence keep the overall result Experimental.

P2 does not discover, cache or apply presets. Those UI-affine operations remain P3.

### P3 capability-coordinator evidence

P3 is pinned separately in `docs/TACHIE_PRESET_P3_CHECKPOINT.md`.

- source `16608d6e2621b8bedea76d04e3498fa6dcc8c2c0`;
- Checkpoint run `35630553092`;
- job `106435424311`;
- artifact `10654059148`;
- artifact SHA256 `9d02df965807e407f8c17bc064113c4e8c4317f9dca5c51db666fdc6aff78f4c`;
- markers `TACHIE_PRESET_CAPABILITY_P3=PASS` and `TACHIE_PRESET_GUARDS_P3=PASS`.

The exact-host proof covers distinct-Character discovery, session caching/invalidation, direct/legacy/modern routes, built-in Animation/PSD named Strong candidates, cleanup, cancellation/latest-wins, bounded state capture and zero Timeline writes.

### P4/P5 row integration and choice-model evidence

P4/P5 are pinned in `docs/TACHIE_PRESET_P4_P5_CHECKPOINT.md`.

- source `7cdc3355858851635a8dca85ba349326c3237427`;
- checkout merge `793bf2d4effa8cff843469038f464582e240668e`;
- Checkpoint run `35661119036`;
- job `106536681752`;
- artifact `10667463789`;
- artifact SHA256 `c3c04b7768ffa5564fda2fccc2664b15e7e44cab08a39926f2f9b71d6780e311`;
- markers `TACHIE_PRESET_ROWS_P4=PASS` and `TACHIE_PRESET_CHOICE_MODEL_P5=PASS`;
- retained `HANDS_ON_ROUND2_E=PASS`, `EXPRESSION_PERFORMANCE=PASS`, full Checkpoint semantic regression/evidence guards PASS.

P4 reuses the existing Rows/DataGrid and background preparation path. P5 explicitly proves none, Template, TachiePreset, valid current other-source, unavailable same-source and invalid-association states. Candidate inspection remains zero-write. No Tachie-Preset mutation is connected yet; P6 is the next boundary.

## Product surface

`表情をまとめて` gains one session-local source switch:

```text
[ テンプレート ] [ 立ち絵プリセット（実験） ]
```

Rules:

- every Tool/session starts in Template mode;
- source switching itself is Timeline/settings zero-write;
- Template mode is behaviorally identical to the accepted baseline;
- protected pending Template/Excel assignments block entering Tachie Preset mode;
- Excel import/export remains Template-only;
- mode change closes the current expression trial boundary and cancels stale source work.

## Placement semantics after the v0.4.2 refresh

The old handoff assumed generic `ExpressionPreset` geometry before the current Set-owned Template workflow was finalized.

The refreshed rule is explicit:

- **Template mode:** keep the accepted Set-owned relation from each Template candidate.
- **Tachie Preset mode:** the Tachie Preset supplies expression content only; reuse the existing persisted `ExpressionPreset` + `CharacterExpressionProfile` + `LayerPlanner` as the placement rule.
- expose that existing model in Tachie Preset mode as **配置ルール**, not as an unqualified “preset”.
- do not create a fake Intent Palette/Library entry merely to obtain geometry.
- do not create a second placement engine or new settings schema.

## Capability detector

No plugin-name allowlist is the primary route.

Resolve only the current Voice Character's tachie plugin / CharacterParameter. Probe only a fresh plugin-created FaceParameter.

Recognize bounded routes:

1. **Direct named preset**
   - exact public writable face-side `Preset : string`;
   - coherent CharacterParameter preset definition/list;
   - candidate enumeration succeeds;
   - dry-run candidate application changes the fresh FaceParameter.

2. **YMM4 property-editor preset**
   - Preset context comes from the local FaceParameter public property/display/editor;
   - use the public tachie-aware property-editor contracts proved by the Lab;
   - create/bind only against the fresh FaceParameter;
   - enumerate bounded choices from that editor surface;
   - selecting a candidate may mutate arbitrary plugin-specific public face state.

Do not scan all loaded assemblies for arbitrary Preset types. Do not inspect private editor fields/ViewModels.

Confidence:

- **Strong:** enumeration + dry-run semantic mutation observed.
- **Experimental:** coherent bounded candidate route exists but mutation cannot be proved in isolation.
- **None:** no coherent route.

Incidental names such as `CompressionPreset` alone are insufficient.

## Performance integration

The accepted expression performance pass remains authoritative.

Tachie Preset capability work is:

- lazy: only while expression task + Tachie Preset mode are active;
- per distinct current Character/plugin fingerprint, not per Voice;
- session-cached;
- cancelable/latest-wins;
- UI-thread-affine for WPF/editor/plugin-created mutable objects;
- yielding between bounded editor operations;
- converted to immutable candidate descriptors before background row preparation;
- discarded if source mode, Timeline, Character/plugin fingerprint or generation becomes stale.

Do not run plugin editor controls inside `Task.Run`. Do not capture mutable YMM4/plugin objects into `ExpressionPreparation`.

## Candidate identity

Store no live editor/control/FaceParameter in a row candidate.

An immutable Tachie-Preset descriptor contains bounded identity only:

- Character logical name / bounded configuration identity;
- plugin runtime type;
- FaceParameter runtime type;
- plugin module MVID;
- route kind;
- preset property/editor attribute type;
- candidate label/identity;
- confidence.

Ambiguous duplicate candidate identities fail closed instead of selecting by first match/order.

## Placement/application

At actual selection time:

1. revalidate current Timeline/Voice/source mode;
2. re-resolve current Character/plugin/configuration;
3. create a **fresh** FaceParameter;
4. re-resolve and re-apply the selected candidate through the bounded route;
5. verify Strong-candidate mutation/state identity;
6. create a fresh bare `TachieFaceItem` for the current Character;
7. attach the applied FaceParameter;
8. plan Frame/Length through the selected existing `ExpressionPreset` placement rule;
9. plan Layer through existing `LayerPlanner`;
10. fully preflight replacement/removal;
11. commit through native Undo/trial.

No existing/manual Timeline item is used as a probe target.

## Choice / row model

Keep one authoritative Voice row collection and one load coordinator.

Do not create a second preset-only DataGrid/row engine.

A source-aware row choice may extend the historical internal `TemplateChoice` type, but it must never fake a `FaceTemplate` or Intent source for a Tachie Preset. Existing Template-only APIs and Workbook paths remain explicitly Template-only.

Cross-source current associations are represented explicitly, not misclassified as corrupt:

- Template mode can truthfully show “current expression comes from Tachie Preset”;
- Tachie Preset mode can truthfully show “current expression comes from Template”;
- choosing a new source performs exact managed replacement;
- mode switching alone does not replace anything.

## Managed association

Keep the existing Voice serial / placement marker contract.

Generalize only the source-specific managed descriptor seam:

- existing Template source keeps `IntentAssociationTag` unchanged;
- Tachie Preset source gets a small versioned `TachiePresetAssociationTag`;
- a managed group has exactly one valid source kind;
- copied/missing/mixed/ambiguous tags fail closed.

The common reader/index owns exact group discovery and returns a source-kind discriminated descriptor. It never repairs by proximity/text/order.

A Tachie Preset descriptor should contain:

- managed group/source version;
- capability/plugin fingerprint;
- bounded candidate identity;
- applied bounded FaceParameter state fingerprint for proved candidate routes.

## Compatibility cache

Session-local only.

Fingerprint includes at least:

- YMM4 host/version identity;
- tachie plugin runtime type;
- FaceParameter runtime type;
- module MVID;
- bounded Character/config identity.

No persistent learned compatibility database in the first implementation.

## P0 product-bridge result

P0 is green and pinned above. Product implementation may proceed without reopening the broad generic capability survey.

The approved product boundary is:

1. exact current Character -> active tachie plugin / current CharacterParameter;
2. public modern `ItemProperty[]` construction where required, with the public legacy route retained for built-ins that expose it;
3. bounded deterministic public-state fingerprint for proved candidate routes;
4. fresh `TachieFaceItem` + fresh applied FaceParameter;
5. unconditional staging-editor cleanup on success, failure and cancellation.

## Safety boundary

Allowed:

- exact current tachie registry/plugin resolution;
- current CharacterParameter as read-only context;
- fresh plugin-created FaceParameter;
- public members/attributes on that local object;
- public YMM4 property-editor contracts;
- bounded temporary WPF staging host when an editor requires layout/binding.

Not allowed:

- arbitrary assembly-wide plugin discovery as the product route;
- private editor fields/internal ViewModels;
- UI Automation/fake input;
- Timeline mutation during probing;
- persistent learned-compatibility DB;
- second placement engine/custom Undo;
- fake Template/Intent identity for presets;
- fuzzy repair of managed associations.

## Validation

Follow:

- `docs/TACHIE_PRESET_ACCEPTANCE.md`;
- `docs/TACHIE_PRESET_WORKPLAN.md`;
- `docs/VALIDATION_STRATEGY.md`.

Ordinary implementation iterations use Focused. Capability/association milestone uses Checkpoint. Whole candidate uses exact Release then owner Hands-on.
