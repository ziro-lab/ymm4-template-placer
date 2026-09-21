# UI Micro Polish — owner Hands-on Round 2 acceptance

Status: **FROZEN**

Authority:

1. `UI_MICRO_POLISH_HANDS_ON_ROUND2_FEEDBACK.md`
2. `UI_MICRO_POLISH_HANDS_ON_ROUND2_DESIGN.md`
3. this document

## A. Quick settings light-dismiss

- A1 opening `⚙ 簡易設定` still opens the current Set's quick settings without switching tabs.
- A2 clicking a shape button inside quick settings keeps the Popup open.
- A3 opening and using the layout ComboBox keeps the Popup open.
- A4 editing quick-settings TextBoxes/CheckBoxes/shortcut controls keeps the Popup open.
- A5 clicking elsewhere in the owning YMM4 Window closes the Popup.
- A6 the outside click is not marked handled by Template Placer.
- A7 owner Window deactivation closes the Popup.
- A8 owner reactivation does not reopen it.
- A9 Tool hide/unload/DataContext replacement/different logical Set still close it.
- A10 no polling, application-global InputManager mouse hook or Win32 global mouse hook is introduced.

## B. Item-owned normal Sets

- B1 every newly created normal targeted Set has exactly one `ItemTypeKey`.
- B2 every newly created normal targeted Set uses `UniformType`.
- B3 selecting an Item type parent shows only Sets owned by that Item type.
- B4 expanding `セットの管理` does not broaden the Set list.
- B5 no normal "show all targeted Sets" mode remains.
- B6 the normal Set editor does not expose a multi-type checkbox matrix or `ExactMixedTypes` selector.
- B7 minimum/maximum selection count remains editable.
- B8 CharacterName restriction remains editable.
- B9 Generic Sets remain separate and unchanged.
- B10 normal placement filtering continues to use the selected runtime Item context and CharacterName as before.

## C. Item-local names and order

- C1 two different Item owners may each contain a Set with the same display name.
- C2 same-owner creation/duplicate still resolves name collisions deterministically with numeric suffixes.
- C3 `↑ / ↓` changes order only among Sets owned by the current Item type.
- C4 moving one Item type's Set does not change another Item type's visible Set order.
- C5 switching Item parent restores a deterministic selected Set without making a clean Settings session dirty.

## D. Same-owner duplicate

- D1 `複製` creates a new Guid under the same Item owner.
- D2 duplicated relation, conditions, entries, entry order and appearance match the source at copy time.
- D3 later source edits do not alter the duplicate.
- D4 later duplicate edits do not alter the source.
- D5 duplicate is Settings-only and writes zero Timeline items.

## E. Copy to another Item type

- E1 a normal targeted Set exposes a destination Item-type choice excluding Generic and the current owner.
- E2 copy validates the current complete source draft before creating anything.
- E3 copy creates a new Guid.
- E4 copied target contains exactly the chosen destination Item type.
- E5 copied target uses `UniformType`.
- E6 relation, selection-count conditions, CharacterName restriction, expression-candidate flag, entries, aliases/colors/shapes and entry overrides equal the source snapshot.
- E7 copy creates no live link between source and destination.
- E8 the destination Set uses the source name when that name is free in the destination.
- E9 destination-local name collision uses the numeric suffix convention.
- E10 after copy, Settings navigates to the destination Item parent and selects the new Set.
- E11 copy is persisted only through the protected Settings path.
- E12 copy writes zero Timeline items.
- E13 later source edits do not alter destination.
- E14 later destination edits do not alter source.

## F. Creation from context

- F1 `＋` under a manually selected Item type creates a Set owned by that Item type.
- F2 current selection containing multiple items of one runtime type may create a Set with that one owner and the current selection-count condition.
- F3 a mixed-runtime-type current selection cannot create a new shared multi-type Set.
- F4 mixed current selection gives an actionable message to choose one Item parent.
- F5 no hidden fallback chooses an arbitrary first type.

## G. Existing multi-type compatibility

- G1 existing multi-type Sets load without mutation.
- G2 existing multi-type Sets retain their existing runtime matching/execution semantics.
- G3 existing multi-type Sets do not appear under any arbitrary single Item owner.
- G4 when at least one exists, a bounded `複数種類` compatibility context appears.
- G5 when none exists, that compatibility context is absent.
- G6 normal creation cannot add a new multi-type Set.
- G7 a legacy multi-type Set can be copied into a new single-owner Set.
- G8 loading/navigation alone does not rewrite old settings bytes.
- G9 protected save without semantic edits preserves the multi-type Set content.
- G10 no Settings revision bump is introduced unless implementation proves it unavoidable and the design is reopened.

## H. Set-to-Settings navigation

- H1 tile right-click / Set settings opens the exact Set under its single Item owner.
- H2 entry-specific navigation selects the exact requested entry.
- H3 this route never enables or depends on a global all-Set mode.
- H4 a legacy multi-type Set routes to the `複数種類` context.
- H5 navigation writes zero Timeline items.

## I. Preserved safety and regressions

- I1 normal placement remains add-only.
- I2 Template identity and bundle fidelity remain unchanged.
- I3 relation/collision planning remains unchanged.
- I4 PlacementPlan/native Undo remain unchanged.
- I5 exact managed-expression replacement remains unchanged.
- I6 Excel pending assignment behavior remains unchanged.
- I7 Settings validation/atomic replacement/digest conflict/lock behavior remains unchanged.
- I8 C0-C4 corrective acceptance remains green.
- I9 public README/license/package gates remain green.
- I10 Tachie Preset PR #19 remains separate.

## Owner Hands-on gate

Before PR #20 merge, owner Hands-on must specifically confirm:

- quick settings disappears when clicking elsewhere in YMM4 but stays open while actually configuring it;
- selecting `ボイス`, `テキスト`, `図形`, etc. shows only that Item's Sets;
- Set management no longer turns into a global list;
- copying a Set to another Item type feels like a one-time clone, not a shared link;
- editing the copied Set never changes the original;
- the Settings surface feels simpler rather than adding another management hierarchy.
