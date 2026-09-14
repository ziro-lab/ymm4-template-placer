# v0.4 Native Pre-implementation Validation

This document records native YMM4 4.55.1.1 Lite behavior that was proved in the public `ziro-lab/chat-native-work-lab-001` experiment repository before implementing v0.4.

These results are implementation evidence, not a replacement for regression tests in this repository. Product code should prefer the proved public host surfaces and avoid reintroducing private Timeline ViewModel reflection for the covered behaviors.

## Pinned host

- YMM4: `4.55.1.1 Lite`
- .NET: `10`
- YMM4 ZIP SHA256: `125860147cc33b831fc1a6d6ea996958001c2ead3b0d37f7d900251d5617db9b`
- Host: real `YukkuriMovieMaker.exe` on GitHub-hosted Windows runner
- Fixtures: synthetic / redistribution-safe only

---

## V04-N1 — Timeline selection / Character context

Public lab experiment: `ymm4-timeline-selection-context-002` / PR #2.

Proved in real YMM4:

```text
TimelineToolInfo.Timeline
Timeline.SelectedItem  : IItem
Timeline.SelectedItems : ImmutableList<IItem>
```

`Timeline` emits `INotifyPropertyChanged.PropertyChanged` for selection changes. Native assertions included:

```text
status=PASS_TIMELINE_SELECTION
voice_selected=True
face_selected=True
selection_clear=True
character_readable=True
selection_event_observed=True
property_changed_names=SelectedItems,SelectedItem,GroupedItems,SelectedAndGroupedItems
```

### Product implication

Character Palette context switching can be event-driven through the public `Timeline` supplied to the Timeline Tool:

```text
subscribe Timeline.PropertyChanged
→ selection property changed
→ SelectedItems.Count == 1
→ VoiceItem / TachieFaceItem
→ read Character
→ temporary Character Palette override
→ clear / unsupported / multiple selection
→ remove override and restore prior manual palette
```

No polling or product-side private `TimelineViewModel` reflection is required for this path on the pinned host.

Boundary: the experiment drives the public Timeline selection model directly; it does not prove physical mouse injection.

---

## V04-N2 — Playhead / Quick Drop

Public lab experiment: `ymm4-playhead-quick-drop-003` / PR #3.

Proved public surface:

```text
Timeline.CurrentFrame : int   // public read/write
```

Behavioral proof moved CurrentFrame from `0` to `321`, read it back, cloned a live registered synthetic ItemTemplate item, and inserted the independent clone into the real Timeline.

Assertions:

```text
status=PASS_PLAYHEAD_QUICK_DROP
public_frame_property=CurrentFrame
playhead_frame=321
clone_frame=321
clone_length=37
clone_layer=12
independent_clone=True
placed=True
frame_matches=True
length_preserved=True
source_unchanged=True
```

### Product implication

Quick Drop can use `Timeline.CurrentFrame` directly. Clone Frame changes to the playhead; Template intrinsic Length remains unchanged. Layer resolution remains a separate planning step.

No private Timeline ViewModel access is required for playhead reading on the pinned host.

Boundary: physical playhead mouse movement and Palette double-click UI dispatch were not the purpose of this proof.

---

## V04-N3 — Character Front / Back Layer placement

Public lab experiment: `ymm4-character-layer-placement-004` / PR #4.

Synthetic requested interval:

```text
Frame  = 130
Length = 30

same Character:
Voice L10
Face  L18
Face  L22

other Character:
Face L40

candidate blockers:
L23 starts at Frame 150
L9  starts at Frame 145
```

Both blockers begin after the requested item's start frame, so start-frame-only collision checking would incorrectly treat them as free.

Native result:

```text
status=PASS_CHARACTER_LAYER_PLACEMENT
same_character_min=10
same_character_max=22
front_layer=24
back_layer=8
front_length=30
back_length=30
front_late_blocker_test=True
back_late_blocker_test=True
other_character_layer40_ignored_for_baseline=True
```

### Product implication

Use:

```text
Front baseline = max Layer of overlapping same-Character related items
Back baseline  = min Layer of overlapping same-Character related items
```

Then search in the requested direction. Candidate occupancy is checked against **all** Timeline items across the proposed item's entire time interval.

Other Characters do not change the Character baseline but may block candidate Layers.

Boundary: this proves Timeline/item semantics, not pixel-render z-order. `TachieItem` Character extraction remains optional for initial v0.4 acceptance; VoiceItem and TachieFaceItem are proved.

---

## V04-N4 — ItemTemplate identity / Plugin Library source locator

Public lab experiment: `ymm4-template-identity-005` / PR #5.

Discovery proved:

```text
ItemTemplate public Guid properties:
SceneId only

ItemSettings public persistence surface:
Templates
SortedTemplates
Save()
```

Two same-name Templates can coexist.

The restart proof deliberately saved two distinct Templates with the same:

```text
Name
SceneId
Path
```

Their contained item Lengths were `21` and `22`, allowing the two records to be distinguished as content after restart. After `ItemSettings.Default.Save()`, process termination and YMM4 relaunch, both records were recovered:

```text
status=PASS_TEMPLATE_IDENTITY_RESTART_AMBIGUITY
read_count=2
same_name_after_restart=True
same_scene_id_after_restart=True
same_path_after_restart=True
content_recovered=True
only_public_guid_property_is_scene_id=True
```

### Product implication

`SceneId` is **not** a Template ID. Do not treat any obvious YMM4 field as an API-guaranteed unique ItemTemplate identity.

Plugin settings should keep:

```text
LibraryEntryId          // plugin-owned stable ID
SourceTemplateLocator   // exact YMM4 source metadata, not an identity guarantee
DisplayName
CharacterRef?
```

A SourceTemplateLocator may retain exact fields such as:

```text
Name
Path
Group
SceneId
```

Resolution is intentionally strict:

```text
exact candidates == 1  -> resolved
exact candidates == 0  -> missing / broken; manual relink or remove
exact candidates > 1   -> ambiguous; manual relink or remove
```

Do not fuzzy-match, silently pick the first candidate, or copy the Template body into Plugin settings.

Because YMM4 does not enforce locator uniqueness, practical source Template organization should remain unique enough to resolve exactly one live candidate (normally via its YMM4 Name / Path organization). Ambiguity is a visible state, not something the Plugin guesses through.

Boundary: a renamed/moved Template is intentionally allowed to become unresolved and require relink. Future YMM4 versions may expose different identity surfaces and must be revalidated before changing this rule.

---

## Implementation guidance

The following v0.4 paths now have native pre-implementation evidence:

```text
Character context auto-switch
Quick Drop current-frame acquisition
Template clone intrinsic-Length preservation
Front / Back Layer baseline and full-span collision planning
Plugin Library source-reference ambiguity handling
```

Still require product-repository implementation tests and native regression proof:

```text
Library / Palette persistence UI
actual Palette double-click command wiring
Base Layer preset behavior
Character Expression / Target Companion / Point Emphasis / Selection Range / Boundary Profiles
lightweight association / Resync
v0.3 regression preservation
```

Do not expand the product scope merely because the public lab can probe more host internals. Use the lab only when a concrete YMM4 host uncertainty materially blocks or risks implementation.
