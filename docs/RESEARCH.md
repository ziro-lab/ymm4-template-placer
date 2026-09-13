# RESEARCH — existing YMM4 implementation evidence

This project should reuse proven YMM4 patterns rather than rediscovering them by trial and error.

## 1. ToolBoxPlugin — Voice-based placement and Timeline mutation

Repository: `Dolphin-kun/ToolBoxPlugin`

Relevant patterns observed:

- enumerate selected `VoiceItem`s
- read `voiceItem.Character`, `Frame`, `Length`, `Layer`
- construct another YMM4 item from the VoiceItem
- add items with `Timeline.TryAddItems(...)`
- call `UndoRedoManager.Record()`
- reposition groups/items and refresh Timeline state
- clone / serialize / restore arbitrary `IItem`s

This is the strongest reference for our Placement Engine and Undo integration.

## 2. YMM4Clipboard — live template catalog

Repository: `Dolphin-kun/YMM4Clipboard`

Observed pattern:

```csharp
var itemSettings = ItemSettings.Default;
var templates = itemSettings.Templates;
```

The plugin reads templates from the running YMM4 instance and can observe collection changes. This supports using the live in-memory YMM4 template catalog rather than treating `ItemSettings.json` as the primary runtime source.

## 3. kakiniwa-ymm4-plugin — applying YMM4 item templates

Repository: `RyuuNeko1107/kakiniwa-ymm4-plugin`

Observed pattern:

- search `ItemSettings.Default.Templates`
- locate the needed item inside a template
- call `GetClone()` to obtain an independent item
- override placement/content properties before Timeline insertion

Some parts of this project use reflection into YMM4 internals. Our CURRENT design should use public/direct types wherever possible and add reflection only when evidence shows it is necessary.

## 4. YMM4TemplateEditor — persistent template file structure

Repository: `bluemistel/YMM4TemplateEditor`

Observed persistent structure:

- item templates live in YMM4 ItemSettings
- template records contain names/paths/groups and `Items`
- Template contents can be inspected as item collections

Useful as fallback/file-format evidence, but runtime access should prefer `ItemSettings.Default.Templates`.

## 5. YMM4EmotionMekerKIT — Voice / Face project semantics

Repository: `bluemistel/YMM4EmotionMekerKIT`

Observed external-project patterns:

- detect `VoiceItem` and `TachieFaceItem`
- use Character / Frame / Length / Layer / Serif
- construct/replace face items

This is evidence that the requested data transformation is practical. Its code/license must not be copied into this repository without an explicit license decision; treat it primarily as behavioral evidence.

## Current technical conclusion

The risky YMM4 operations are not novel:

```text
VoiceItem read                 proven
live template enumeration      proven
template item clone            proven
Timeline insertion             proven
Undo recording                 proven
IItem serialization/restore    proven
```

Our new work is primarily the thin integration between:

```text
Voice list
+ live Template catalog
+ YMM4 dropdown UI
+ optional Excel assignment bridge
+ shared Placement Engine
```

## License rule

Do not copy substantial code from reference projects by default. Reimplement the small required behavior against YMM4 APIs. If code reuse becomes attractive, inspect and record that source's license first.
