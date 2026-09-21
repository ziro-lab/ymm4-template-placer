# Glossary

This glossary fixes product terminology without requiring a mass rename of existing source files.

## Current user/product terms

### Template

A live YMM4 Item Template referenced as source content.

Template Placer does not copy Template bodies into a second database.

### Library Entry

A thin plugin-side reference to one live YMM4 Template, plus plugin-local display metadata.

### Set

The user-visible collection selected for one placement intent/context.

A Set owns applicability and the saved placement relation used by its tiles.

For normal targeted editing, **one Item type owns the Set**. Settings navigation is therefore:

```text
Item type -> its Sets -> tiles
```

A Set copied to another Item type is a one-time snapshot with a new identity, not a shared/live-linked Set. Existing historical multi-type Sets remain compatibility data and are not the normal creation model.

Generic Sets use their existing separate model.

Do not introduce a second user-visible “purpose” layer above Set unless a concrete workflow requires it.

### Tile

One concrete action in a Set, normally backed by a Library Entry/Template.

Tile appearance such as alias/color/shape is not placement semantics.

### Context

The explicit Timeline state read at the beginning of an operation.

Examples:

- current playback position;
- selected item(s);
- selected item types/count;
- Character;
- selection range;
- explicit boundary pair;
- explicit finite Neighbor result.

Context is read-only input to planning.

### Anchor

The target-side point/span used by a placement rule.

Examples:

- target start/end/center;
- selection-range start/end;
- pair boundary;
- explicit related-item edge.

### Layer Policy

The finite rule used to choose Layer.

Current examples include template-relative/target-relative behavior and Generic numeric target with bounded one-direction search.

Future proposed addition: Absolute Layer.

### Placement Plan

The complete preflighted mutation plan committed through YMM4 native Undo.

A required planning failure means Timeline mutation = 0.

### Bundle

A Template containing multiple items whose internal time/layer/length/content relationship is preserved when cloned and moved as one planned unit.

### Managed expression

An expression item/bundle created by the specialist expression workflow with an exact Plugin-managed association that permits later exact replacement/removal.

Manual/unassociated expressions are not managed expressions.

## “Preset” disambiguation

The word **Preset** is overloaded in both the current codebase and YMM4/tachie plugins. Use the qualified terms below in new docs/code/comments.

### Placement Preset

A saved finite set of placement parameters.

Examples:

- Voice span vs next same Character;
- offsets;
- layer range/preference;
- placement geometry.

Existing source types such as `ExpressionPreset` belong to this placement-geometry meaning unless a feature document says otherwise.

### Tachie Preset

A preset exposed by the current YMM4 tachie plugin/FaceParameter editor that changes expression content.

Examples include built-in PSD/Animation tachie preset choices and compatible third-party plugin preset editors.

The experimental PR #19 works with **Tachie Presets**.

Do not call a Tachie Preset merely “ExpressionPreset” in new feature code when that would be confused with the existing placement type.

### Preset source mode

The experimental expression-workspace mode that obtains expression content from Tachie Presets instead of saved Template candidates.

This is not a Placement Preset.

## Future working vocabulary

These terms describe backlog concepts. They do not imply implementation.

### Placement Recipe

A finite saved placement action that may contain one or more independent Placement Steps.

Current single-template placement can be understood conceptually as a one-Step Recipe, but existing code does not need to be renamed merely to match this vocabulary.

### Placement Step

One finite new-content placement within a future Recipe.

A Step may select:

- source Template;
- target Anchor;
- Template Pivot;
- offsets/duration;
- Layer Policy.

Every Step reads the same immutable operation-start Context. A Step cannot use a previous Step’s result as its input.

### Template Pivot

The point inside the cloned Template/Bundle aligned to the target Anchor.

Proposed finite values:

- start;
- center;
- end;
- explicit offset from Template/Bundle start.

For a Bundle, the natural proposed span is from minimum member Frame to maximum member end.

### Absolute Layer

A future Layer Policy whose base target is explicit Layer N instead of template/target-relative placement.

Collision behavior may still use the existing finite do-not-place / one-direction search semantics.

### Composite Placement Steps

A future Recipe containing multiple independent Steps that are all preflighted together and committed as one operation.

This is not a script, pipeline, loop or branching workflow.

### Fan-out

A future finite operation that applies one Recipe to each explicit selected target or explicit boundary.

All generated placements must still be planned/preflighted as one operation before mutation.

### Neighbor selector

A finite saved rule that resolves one explicit related Timeline item such as next specified type, previous any item, or next same-Character Voice.

Failure/ambiguity stops or follows an explicit saved fallback; never fuzzy-guesses.

## Historical/internal names

Terms such as Palette, Profile, Intent, Quick Drop, Selection Placement and legacy Preset names appear in older code and historical documents.

Do not mass-rename them only for terminology consistency.

When touching an old area:

- preserve serialized compatibility;
- prefer current user-facing terms in new UI/docs;
- explain the mapping locally if ambiguity matters.
