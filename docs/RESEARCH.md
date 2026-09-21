# RESEARCH — external references and precedents

This file is a **Reference Registry** for external YMM4 projects, samples and documentation that can guide discovery and design.

External material is not canonical YMM4 host-behavior evidence by itself.

Use these categories consistently:

- **Reference / precedent** — an external repository, sample, API document or community implementation shows that an approach exists or suggests a useful API/pattern.
- **Lab evidence** — the canonical Lab reproduces a host claim against an exact YMM4/runtime environment and records the run/artifact.
- **Product Native evidence** — this repository's isolated native proof validates the product behavior and retained semantics.

Promotion rule:

> Reference -> hypothesis/design input -> Lab/native proof -> version-scoped evidence -> product authority.

Do not collapse those steps merely because another plugin already does something similar.

When adding a reference, record what was observed, what it is useful for, and what it does **not** prove. Record/check the source license before copying code or assets.

## 1. ToolBoxPlugin — Voice-based placement and Timeline mutation

Repository: `Dolphin-kun/ToolBoxPlugin`

Classification: **Reference / precedent**

Relevant patterns observed:

- enumerate selected `VoiceItem`s;
- read `voiceItem.Character`, `Frame`, `Length`, `Layer`;
- construct another YMM4 item from the VoiceItem;
- add items with `Timeline.TryAddItems(...)`;
- call `UndoRedoManager.Record()`;
- reposition groups/items and refresh Timeline state;
- clone / serialize / restore arbitrary `IItem`s.

Useful for:

- discovering placement/Undo APIs and common community implementation shapes;
- identifying concrete behavior worth reproducing in the Lab or product Native proof.

Does not prove:

- that the same route is currently supported or semantically identical on the pinned product host;
- that its mutation/Undo strategy is appropriate for Template Placer's stricter zero-write/preflight contract.

This remains one of the strongest external precedents for placement and Undo integration, but current product behavior is authoritative only after our own proof.

## 2. YMM4Clipboard — live template catalog

Repository: `Dolphin-kun/YMM4Clipboard`

Classification: **Reference / precedent**

Observed pattern:

```csharp
var itemSettings = ItemSettings.Default;
var templates = itemSettings.Templates;
```

The plugin reads templates from the running YMM4 instance and can observe collection changes.

Useful for:

- discovering the live in-memory Template catalog surface;
- supporting the design hypothesis that runtime Template access should prefer `ItemSettings.Default.Templates` over direct file parsing.

Does not by itself prove the exact current-host contract. Template Placer's live-catalog behavior and strict source-resolution semantics are validated by this repository's own native evidence.

## 3. kakiniwa-ymm4-plugin — applying YMM4 item templates

Repository: `RyuuNeko1107/kakiniwa-ymm4-plugin`

Classification: **Reference / precedent**

Observed pattern:

- search `ItemSettings.Default.Templates`;
- locate the needed item inside a Template;
- call `GetClone()` to obtain an independent item;
- override placement/content properties before Timeline insertion.

Some parts of this project use reflection into YMM4 internals.

Useful for:

- locating direct/public Template and clone surfaces worth validating;
- comparing bounded reflection needs against public alternatives.

Do not treat another project's reflection as permission to reflect broadly. Template Placer should continue to use direct/public types wherever possible and admit reflection only at a narrow, named, fail-closed compatibility boundary backed by Lab/native evidence.

## 4. YMM4TemplateEditor — persistent template file structure

Repository: `bluemistel/YMM4TemplateEditor`

Classification: **Reference / precedent**

Observed persistent structure:

- item templates live in YMM4 ItemSettings;
- template records contain names/paths/groups and `Items`;
- Template contents can be inspected as item collections.

Useful as a file-format/fallback reference.

It does not replace the runtime authority of the live YMM4 Template catalog and does not prove compatibility with every current persistent format/version.

## 5. YMM4EmotionMekerKIT — Voice / Face project semantics

Repository: `bluemistel/YMM4EmotionMekerKIT`

Classification: **Reference / precedent**

Observed external-project patterns:

- detect `VoiceItem` and `TachieFaceItem`;
- use Character / Frame / Length / Layer / Serif;
- construct/replace face items.

Useful for:

- confirming that similar Voice/Face transformations are practical enough to investigate;
- discovering terminology and candidate host surfaces.

This is not product evidence for Template Placer's exact association, replacement or safety semantics. Its code/license must not be copied into this repository without an explicit license decision.

## Current technical conclusion

External references show that the following operations are common and worth investigating:

```text
VoiceItem read
live template enumeration
template item clone
Timeline insertion
Undo recording
IItem serialization/restore
```

Do **not** label those operations "proven" merely because an external plugin uses them.

For current host-sensitive claims, consult in this order:

1. `docs/CURRENT_ARCHITECTURE.md`;
2. the canonical Lab/native evidence referenced by the active feature;
3. this registry for discovery context and external precedent.

Template Placer's accepted integration remains:

```text
Voice list
+ live Template catalog
+ YMM4 Tool UI
+ optional Excel assignment bridge
+ shared Placement Engine
+ protected Settings/Undo/safety boundaries
```

## Reference-registry rule

For a new external source, prefer a compact entry containing:

- source/repository/document name;
- what was observed;
- why it may matter;
- whether it is public API, community precedent or file-format context;
- what it does **not** prove;
- license/reuse status when code/assets might be copied;
- version/date when the claim is version-sensitive.

If a reference reveals a potentially important host behavior, move that question into the canonical Lab instead of upgrading the reference itself to evidence.

## License rule

Do not copy substantial code, assets or fixtures from reference projects by default. Reimplement the smallest required behavior against proved YMM4 APIs. If code reuse becomes attractive, inspect and record that source's license first.
