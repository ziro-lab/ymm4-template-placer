# UI Micro Polish — owner Hands-on Round 2 corrective design

Status: **FROZEN FOR IMPLEMENTATION**

Feedback authority: `UI_MICRO_POLISH_HANDS_ON_ROUND2_FEEDBACK.md`.

This pass addresses F8-F10 only. It follows the Release-green corrective candidate from run #305 and must preserve all C0-C4 accepted behavior.

---

## 1. Design summary

The normal targeted-Set mental model becomes:

```text
Item type
  -> Sets owned by that Item type
      -> relation / conditions / entries
```

Reuse across Item types becomes explicit snapshot copy:

```text
source Item / Set
  -> copy to another Item type
  -> new independent Set
```

Quick settings gains owner-window light-dismiss:

```text
inside quick settings -> stay open
click elsewhere in owning YMM4 Window -> close
owner Window deactivates -> close
owner reactivates -> stay closed
```

Generic Sets remain a separate existing model.

---

## 2. F8 — owner-window light-dismiss for quick settings

### 2.1 Required behavior

While `⚙ 簡易設定` is open:

- clicks inside the quick-settings Popup keep it open;
- opening/using its layout ComboBox, TextBoxes, CheckBox, Expander and shortcut controls keep it open;
- a mouse click elsewhere in the same owning YMM4 Window closes it;
- the outside click is not swallowed and continues to the clicked YMM4 control;
- owner `Window.Deactivated` still closes it;
- Tool hide/unload, DataContext replacement and different logical Set still close it;
- activation never reopens it.

### 2.2 Implementation boundary

Keep `Popup.StaysOpen=True`.

Reason: quick settings contains nested interactive controls, including a ComboBox that owns another WPF Popup. Relying on `StaysOpen=False` would make nested-popup behavior part of the contract.

Instead, while quick settings is open:

1. observe the owning WPF Window;
2. attach one scoped owner `PreviewMouseDown` handler with handled events visible;
3. if the event comes from the owner Window and is not the quick-settings toggle itself, close quick settings by clearing the toggle;
4. do not mark the outside event handled;
5. detach the handler when the Popup closes, Tool unloads, owner changes or DataContext changes.

Clicks inside the quick-settings Popup live on the Popup's separate WPF presentation source and therefore do not traverse the owner Window route.

The existing owner `Deactivated` subscription remains.

### 2.3 Out of scope

Do not add:

- `InputManager` application-global pointer hooks;
- Win32 global mouse hooks;
- polling;
- fake clicks;
- close-after-every-control-action behavior.

---

## 3. F9 — one normal targeted Set belongs to one Item type

### 3.1 Normal Set ownership

A normal targeted `IntentPalette` is Item-owned when:

```text
Target.ItemTypeKeys.Count == 1
TypeMatch == UniformType
```

The sole `ItemTypeKey` is the Set owner.

Do **not** add a second serialized `OwnerTypeKey` field. That would create two competing sources of truth.

The current serialized `ItemTypeKeys` shape remains for compatibility, but normal creation/editing produces exactly one owner type.

### 3.2 Settings navigation

The top Item buttons remain the parent navigation.

For a selected Item type:

- the Set picker shows only Sets owned by that Item type;
- Set management operates only on that Item type's Sets;
- expanding `セットの管理` never changes the filter;
- there is no normal "show every targeted Set" mode;
- switching Item type switches the visible Set collection immediately.

This removes the current `ShowAllSets` mental model from the normal surface.

### 3.3 Normal Set editor

The owner Item type is chosen by the parent navigation, not by checkboxes inside the Set.

Remove the normal editable type matrix / `ExactMixedTypes` choice from `対象の詳細・複数種類`.

Retain Set-local conditions that are still meaningful under one owner:

- optional CharacterName restriction;
- minimum/maximum selection count;
- relation;
- neighbor/fallback;
- layer rule;
- expression-candidate flag;
- entries and entry appearance.

Multiple selected items of the **same owner type** remain supported through selection-count conditions.

### 3.4 Same names across different Item types

Set identity remains Guid-based.

A Set name needs to be disambiguated only inside its owner Item type.

Therefore:

- `ボイス / 強調` and `テキスト / 強調` may both exist;
- same-owner duplicate/copy uses the existing numbered suffix behavior.

### 3.5 Ordering

Set order is owner-local from the user's point of view.

`↑ / ↓` moves the Set among Sets owned by the current Item type only. It must not move through another Item type's visible order.

The serialized global list may remain the backing store, but owner-local order must be deterministic and stable.

---

## 4. Existing multi-type Sets

The current schema already permits `ExactMixedTypes` and multiple `ItemTypeKeys`. Existing settings must not be silently destroyed or broadened.

### 4.1 Compatibility rule

Existing multi-type Sets:

- remain stored losslessly;
- remain executable under their existing matching semantics;
- are not silently split into one Set per Item type;
- are not placed under an arbitrary first Item owner;
- cannot be newly created through the normal Item-owned editor.

If at least one such Set exists, Settings exposes a bounded compatibility context:

```text
複数種類
```

Only those existing Sets appear there.

This context is absent when no multi-type Set exists.

### 4.2 Escape path

A multi-type compatibility Set may use the same **copy to another Item type** operation.

The copy becomes a new normal single-owner Set.

Editing the legacy Set's existing multi-type target semantics is not expanded in this pass. Preserve it or expose only the minimum fields required to avoid data loss.

No settings revision bump is required solely for this UI/ownership tightening.

---

## 5. F10 — copy a Set to another Item type

### 5.1 User operation

Inside `セットの管理`, for a normal targeted Set:

```text
[複製] [↑] [↓] [削除]

他のアイテムへコピー
[コピー先 Item type ▼] [コピー]
```

`複製` remains the same-owner operation.

`他のアイテムへコピー` is the cross-owner operation.

Generic is not a destination because Generic Sets use a different data model and placement contract.

### 5.2 Snapshot semantics

When Copy is invoked:

1. build/validate the current complete source Set;
2. deep-copy that exact Set state;
3. create a new Guid;
4. replace target type with exactly the destination Item type;
5. set `TypeMatch=UniformType`;
6. retain all other Set state at that moment;
7. insert/select the new Set under the destination Item parent;
8. persist through the existing protected Settings transaction.

Copied state includes:

- relation;
- selection-count conditions;
- CharacterName restriction;
- expression-candidate flag;
- entries and ordering;
- aliases/colors/shapes;
- entry-level offsets/overrides.

The copy is intentionally not linked to the source.

Later edits to source or destination never propagate.

### 5.3 Name behavior

Prefer the same Set name in the destination Item type.

If that destination already has the same name, use the existing local numbered form:

```text
強調
強調 2
強調 3
```

Do not force global name uniqueness across unrelated Item types.

### 5.4 Destination condition notice

Snapshot copy does not silently delete conditions.

If a copied Character restriction or another condition is unlikely to match the destination type, keep the copied condition and select the new Set immediately so the user can adjust it.

A concise status/notice may say that target-specific conditions were copied as-is.

Do not make hidden semantic edits in the name of convenience.

---

## 6. Creation behavior

Pressing `＋` while an Item type parent is selected creates a Set owned by that Item type.

Pressing `＋` while the current Timeline selection context is selected:

- if all selected items have one runtime Item type, create under that type and carry current count/Character conditions as today;
- if the selection contains multiple runtime Item types, do not create a new shared multi-type Set.

For a mixed current selection, direct the user to choose one Item type parent first.

This deliberately prevents new shared cross-Item ownership.

Generic creation remains unchanged.

---

## 7. Open-settings routes

Routes such as tile right-click -> `Setの設定を開く` must no longer fall back to a global `ShowAllSets` mode.

For a normal targeted Set:

- resolve its sole owner Item type;
- navigate Settings to that Item parent;
- select the exact Set and requested entry.

For a legacy multi-type Set:

- navigate to the bounded `複数種類` compatibility context.

No fuzzy type selection.

---

## 8. Preserved architecture and safety

This pass must not change:

- live YMM4 Template as source of truth;
- TemplateLocator strict resolution;
- placement Context semantics;
- relation planning;
- bundle geometry;
- collision behavior;
- PlacementPlan;
- native Undo;
- add-only normal placement;
- exact managed-expression replacement;
- Excel pending-assignment behavior;
- protected atomic Settings persistence;
- C0-C4 accepted UI behavior;
- Tachie Preset PR #19 scope.

Legacy `SelectionPreset` / `ExpressionPreset` families are not broadly refactored.

---

## 9. Settings schema strategy

Prefer **no schema migration**.

Keep the current `IntentTargetContext.ItemTypeKeys` / `TypeMatch` serialization so existing data loads exactly.

The product-level tightening is enforced by:

- normal creation producing one type;
- normal Settings navigation owning Sets by that one type;
- normal editor no longer creating multi-type target combinations;
- compatibility-only handling for already-existing multi-type Sets.

If implementation discovers that this cannot be done without ambiguous ownership or destructive migration, stop and review before adding a new serialized owner field.

---

## 10. Validation strategy

Native proof must cover:

- quick settings internal interaction stays open;
- owner-window outside click closes it;
- outside click is not swallowed;
- owner deactivation still closes;
- activation does not reopen;
- each Item parent shows only its own Sets even when Set management is expanded;
- no normal global all-Set mode remains;
- new Set creation produces exactly one owner type;
- same-owner duplicate stays under the same Item;
- cross-Item copy creates a new independent Guid and exact snapshot except owner;
- source edits after copy do not affect destination and vice versa;
- same Set name may exist under different Item types;
- reorder is owner-local;
- normal UI cannot create new multi-type Sets;
- existing multi-type data roundtrips unchanged and appears only in the compatibility context;
- tile -> Settings navigation lands on the correct owner without `ShowAllSets`;
- all current Release/Checkpoint safety gates remain green.

Real owner Hands-on is required again before PR #20 merge.
