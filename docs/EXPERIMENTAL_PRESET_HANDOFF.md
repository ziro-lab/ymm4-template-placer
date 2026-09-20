# Experimental Tachie Preset source — feature handoff

> Terminology: **Tachie Preset** means expression/content presets exposed by a YMM4 tachie plugin. It is distinct from the existing **Placement Preset** / `ExpressionPreset` placement-geometry type. See `docs/GLOSSARY.md`.

This document carries the Tachie Preset work that was deliberately split out of PR #16 after Round 4 A/B/C and validation right-sizing were completed.

## Start condition

Do not re-research the generic Tachie Preset surface before implementation. Reuse the public Lab evidence below and the frozen product boundaries in this handoff.

Start from main after the completed A/B/C candidate is accepted and merged.

## Canonical Lab evidence

Public Lab: `ziro-lab/chat-native-work-lab-001`, Draft PR #58.

Pinned result:

- source `621cff8199c6fe6daf54aa5ccd7c44e35e3c0ce7`
- run `35488799463`
- job `106019954003`
- artifact `10598378796`
- artifact SHA256 `150ea81535196330b53fbbf636f62b3c79c54f19ad420c877e81a4c8b98ffc37`
- marker `PASS_GENERIC_EXPRESSION_PRESET_CAPABILITY_SURVEY`

The Lab proved structural discovery/application for:

- direct writable face-side `Preset : string` plus coherent character-side definitions;
- built-in AnimationTachie preset editor;
- built-in PSD tachie preset editor;
- synthetic arbitrary expanded-state preset editor;
- modern `PropertyEditorAttribute2 + IPropertyEditorForTachieParameterAttribute` shape;
- unrelated Preset-name noise rejection;
- local containment of a throwing editor.

It does not prove every third-party tachie plugin or perceptual correctness for every renderer.

## Product surface

`表情をまとめて` gains a global source switch:

```text
[ テンプレート ]  [ プリセット（実験的） ]
```

- each Tool/session starts in Template mode;
- switching mode alone mutates no Timeline content;
- Template mode keeps the accepted A/B/C behavior unchanged;
- protected pending Excel/import assignments block entering Tachie Preset mode;
- Excel import/export remains Template-only.

## Capability detector

No plugin-name allowlist as the primary design.

Resolve only the current Character's tachie plugin and current CharacterParameter. Probe only a fresh plugin-created FaceParameter.

Recognize:

1. **Direct named preset**
   - exact public writable face-side `Preset : string`;
   - coherent character-side Preset definition/list;
   - candidate enumeration succeeds;
   - dry-run candidate mutation changes the fresh FaceParameter.

2. **YMM4 property-editor preset**
   - Preset context comes from the local face property/display/editor;
   - the YMM4 tachie-aware property editor can receive CharacterParameter;
   - the editor can be created/bound against the fresh FaceParameter;
   - choices can be enumerated from the bounded editor surface;
   - selecting a candidate may mutate any plugin-specific face fields.

Support both observed host contracts:

- legacy `PropertyEditorForTachieParameterAttribute`;
- modern `PropertyEditorAttribute/2 + IPropertyEditorForTachieParameterAttribute`.

Do not scan loaded assemblies for arbitrary Preset types.

## Confidence

- **Strong**: candidate enumeration plus dry-run selection causes a semantic fresh-FaceParameter mutation.
- **Experimental**: coherent candidate/choices exist, but isolated dry-run cannot prove final mutation.
- **None**: no coherent Tachie-Preset capability.

A substring such as `CompressionPreset` alone is insufficient.

One broken candidate must not disable Template mode or other Characters.

## Placement

Tachie Preset mode creates a fresh bare `TachieFaceItem` for the row Character.

Reuse existing geometry:

- `ExpressionPreset`;
- `CharacterExpressionProfile.Span`;
- `LayerPlanner`;
- shared `PlacementPlan`;
- native Undo/Redo;
- existing latest-wins Voice/Preview navigation.

At actual selection time, re-resolve the plugin, create a fresh FaceParameter, re-apply the selected candidate, verify the expected state for Strong candidates, then plan/commit.

No existing/manual item is used as a probe target.

## Managed association

Do not fake an Intent palette/entry identity for Tachie-Preset-generated expressions.

Add a small versioned Tachie-Preset-source descriptor/tag containing enough information to identify:

- the Plugin-managed group/source;
- capability/plugin fingerprint;
- candidate identity/label;
- applied FaceParameter state hash.

Extract only the common exact managed-bundle discovery/removal seam needed for source switching.

Required behavior:

- template-source -> Tachie-Preset-source replacement is atomic;
- Tachie-Preset-source -> template-source replacement is atomic;
- exact removal works;
- copied/missing/ambiguous tags fail closed;
- candidate disappearance becomes explicit unavailable state;
- manual/unassociated expressions are untouched;
- native Undo remains one logical trial operation.

## Compatibility cache

Round scope is session-local only.

Fingerprint includes at least:

- YMM4 version;
- tachie plugin runtime type;
- FaceParameter runtime type;
- plugin module MVID;
- bounded Character/config identity.

Do not add a persistent learned-compatibility database in the first implementation.

## Safety boundary

Allowed:

- exact current tachie registry/plugin resolution;
- current CharacterParameter as read-only context;
- fresh plugin-created FaceParameter;
- public members/attributes on that local face object;
- public YMM4 property-editor contracts;
- bounded staging WPF visual host when the editor requires it.

Not allowed:

- arbitrary assembly-wide reflection;
- private fields/internal editor ViewModels by name;
- UI Automation/fake input;
- Timeline mutation during probing;
- a second placement engine;
- a custom Undo stack;
- proximity/text/order repair of ambiguous expression associations.

## Validation strategy

Use the tiered validation model from `docs/VALIDATION_STRATEGY.md`.

- ordinary preset implementation iterations: Focused;
- capability/placement checkpoint completion: Checkpoint;
- whole candidate ready for promotion: Release.

Do not make every D/E experiment part of the permanent Focused stable core. Add only high-risk invariants.

## Acceptance carried forward

Capability acceptance:

- source switch exists and starts Template;
- mode switch is zero-write;
- direct named route works when coherent;
- legacy and modern editor routes work;
- built-in Animation/PSD fixtures enumerate and mutate fresh parameters;
- arbitrary expanded-state editor works;
- unrelated Preset noise is rejected;
- throwing editor is contained;
- confidence is deterministic;
- cache is session-local.

Placement/association acceptance:

- existing ExpressionPreset geometry is reused;
- fresh TachieFaceItem/current Character/fresh FaceParameter are used;
- complete preflight precedes replacement;
- exact versioned preset association is written;
- template<->preset replacement is atomic;
- exact removal works;
- ambiguous/copy/missing state fails closed;
- candidate disappearance is explicit;
- manual expressions are untouched;
- one logical native Undo/trial is preserved;
- Preview confirmation uses the common navigation coordinator;
- pending Excel blocks Tachie Preset mode;
- Excel remains Template-only;
- Resync truthfully skips unsupported preset associations;
- one incompatible plugin does not disable other modes;
- no UI Automation/fake input/private-state path/new placement engine is introduced.
