# Backlog

This is the single collection point for **unimplemented or deferred product ideas**.

Being listed here is not implementation approval or priority commitment.

## Active

## Portable settings storage

Status: **IMPLEMENTED CANDIDATE / RELEASE GREEN — PR #27; OWNER HANDS-ON BEFORE MERGE**

User problem:

YMM4 can be kept as a lightweight portable folder, but Template Placer currently stores `settings-v04.json` under `%LOCALAPPDATA%/Ymm4TemplatePlacer/`. Copying the YMM4 folder therefore does not carry Template Placer Sets, tile presentation, position shortcuts and other plugin settings with it.

Desired direction:

- make Template Placer settings travel with the YMM4 folder;
- preferred candidate: `<YMM4>/user/plugin/Ymm4TemplatePlacer/Data/settings-v04.json`;
- keep the existing protected settings-store guarantees: schema validation, 1 MiB guard, digest/external-change protection, cross-instance lock and atomic replacement;
- preserve the current stable plugin install root.

Host evidence:

- public Lab Draft PR #85, experiment `ymm4-ymme-update-preservation`;
- native YMM4 4.55.1.1 Lite run #6 passed the real `.ymme` v1 -> v2 update path;
- user-created files under the plugin folder, including nested `Data/` files, survived the update;
- a sibling data file under `<YMM4>/user/` also survived;
- package-owned files with matching paths were replaced by v2;
- package files omitted from v2 were not automatically removed.

Migration direction:

1. if the new portable settings file exists, load it;
2. otherwise, if the old `%LOCALAPPDATA%` settings file exists, validate/read it and migrate safely to the portable location;
3. do not delete the old file automatically during the first migration;
4. never choose between two divergent valid files silently — surface the conflict and preserve both;
5. migration must not weaken the existing fail-closed settings behavior.

Packaging caution:

Because the observed `.ymme` updater preserves files that are omitted from a later package, future package-layout changes must explicitly account for stale plugin files. Do not rely on update installation to clean old files automatically.

This portability work is independent from placement semantics and should not broaden the active placement feature PR.

Implementation candidate:

- authoritative path: `<YMM4>/user/plugin/Ymm4TemplatePlacer/Data/settings-v04.json`;
- valid legacy-only LocalAppData settings migrate byte-for-byte after validation;
- legacy file remains in place;
- a small legacy digest baseline prevents the retained backup from becoming a false conflict after normal Portable edits;
- divergent valid files or a legacy file changed after migration fail closed and preserve both;
- corrupt Portable never silently falls back to legacy;
- Release packaging rejects any packaged `Data/` payload.

Release #525 (`35797613825`) at source `e0f8128f63a687f90e8491fa0340e724c0229cd5`: **1,702 Native assertions PASS**, exact distribution-DLL smoke PASS, verified `.ymme` / source / provenance packaging PASS. Public Lab PR #85 remains the real YMM4 `.ymme` update-preservation host evidence.


## Built-in Tachie Preset loading performance — deferred

Status: **DEFERRED / MEASURE FIRST**

User problem:

Built-in/standard Tachie Preset loading works, but Hands-on use feels noticeably heavier than expected.

Boundary:

- functionality is accepted in v0.5.0;
- do not reopen the Tachie Preset feature architecture merely because the built-in route feels slow;
- first profile discovery/editor construction/session-cache behavior on the real built-in path;
- preserve the distinct-Character scaling, cancellation/latest-wins behavior and Template-mode performance gates;
- optimize only the measured hot path.

This is a performance-polish item, not a merge blocker for v0.5.0.

## Compact Settings visibility polish — planned

Status: **PLANNED / BOUNDED UI POLISH**

User problem:

The compact Settings surface is allowed to be vertically long. The more important problem is discoverability: first-time users should be able to see what can be configured without opening several first-level expanders. The compact surface should remain useful for quick edits even after the separate Full Settings Workspace exists.

Primary rule:

> Prefer visible first-level settings over hiding them to save vertical space. Keep only genuinely detailed or exceptional controls folded.

First-level sections that should be visible by default and may stop being Expanders entirely:

- `どのアイテムで使うか`;
- `どう置く？`;
- `演出と並び順`;
- `セットの管理`;
- `テンプレートをまとめて追加`.

Reasons:

- these sections define or manage the Set in ordinary use;
- hiding them can make important capabilities undiscoverable;
- `セットの管理` is small and contains important actions such as Set deletion/copy/reorder;
- Template/tile addition is performed through `テンプレートをまとめて追加`, so its existence should be obvious;
- vertical length itself is not considered a defect for the compact Settings surface.

Controls that should remain folded because they are genuinely detailed/low-frequency:

- `対象の詳細`;
- `細かく調整`;
- `選択した演出だけの微調整`;
- `全体の表示・操作`.

`全体の表示・操作` may move lower in the compact Settings flow because it is global presentation/operation state rather than the current Set's core definition.

### Template bulk-add scrolling polish

Keep `テンプレートをまとめて追加` visible, but reduce nested-scroll friction.

Direction:

- make the inner Template source list slightly narrower rather than changing wheel ownership first;
- reserve usable outer-scroll escape space on **both** sides of the list;
- prefer a somewhat wider escape margin on the **left**, because the right side already contains the inner list scrollbar and may naturally be used for direct inner scrolling;
- do not shrink the list aggressively; the goal is only to make outer Settings scrolling easy across different YMM4 panel widths/layout arrangements;
- keep the current nested-wheel routing unless hands-on testing shows width/spacing alone is insufficient.

### Set picker row alignment

The Set picker row should look like one coherent control group.

Direction:

- align the vertical size of the Set ComboBox, `＋` button and adjacent `削除` button;
- prefer matching the buttons to the ComboBox height unless hands-on appearance shows the reverse is better;
- keep this local to the Set picker row rather than changing the global Button/ComboBox styles.

Scope boundary:

- this is not a redesign of compact Settings;
- do not add search/filter/diagnostic workspace features here — those belong to the Full Settings Workspace;
- do not change what can be configured or the settings model;
- validate the final spacing and first-level visibility in actual YMM4 at narrow and normal Tool widths.

## Full Settings Workspace — planned UX direction

Status: **PLANNED / DESIGN CANDIDATE**

User problem:

The current compact Settings surface works for small day-to-day edits, but becomes cramped when Sets and tiles grow. The product needs a larger settings-only workspace that is easy to understand on first use without requiring explanatory documentation.

Core rule:

- keep the current compact Settings UI as the ordinary quick-edit surface; bounded visibility/spacing polish may still improve it;
- the large workspace edits the **same settings model and same staged Settings Draft**;
- do not create a second settings schema, separate feature set, or alternate persistence path;
- the large mode changes presentation/navigation only, not what can ultimately be configured.

Primary UX structure:

```text
left navigation        center structure         right inspector
Item type          ->  Sets / tiles         ->  selected settings
```

Suggested roles:

- **Left:** Generic / Voice / Text / Image / Shape / other Item contexts;
- **Center:** all Sets for the selected context and their tiles, using the same recognizable tile names/colors/shapes as the normal placement surface;
- **Right:** settings for the currently selected Set or tile.

Discoverability goals:

- the current location should be visually obvious without reading help text;
- adding a Set should happen beside the Set collection;
- adding a Template/tile should happen beside that Set's tile collection;
- selecting an object should reveal its editable properties in the right inspector;
- avoid a deep TreeView, tab maze, or management menu that hides basic actions.

Search and filtering:

- keep one always-visible search field near the top;
- search may match Set name, tile alias, source Template name and Character where available;
- search is supplemental — normal browsing must remain possible without it;
- begin with a small number of visible filters only, such as Character, expression-candidate status and problem/broken-reference status;
- active filters must remain visible and easy to clear;
- filtering must only change what is shown, never mutate organization or settings data.

Progressive disclosure:

- show the frequently used settings directly;
- keep the existing human-readable `このセットの動き` summary prominent;
- a small schematic/diagram of target/placement relation may be used if it improves first-look comprehension;
- advanced numeric/detail controls should be behind **one** bounded disclosure level such as `細かく調整`;
- avoid nested expanders beyond that where practical.

Management-assist fit:

This workspace is the natural future home for read-only management aids such as:

- broken-reference/problem filtering;
- "where is this Template used?" usage information;
- unused/unassigned visibility;
- Set/tile organization assistance.

These are not required for the first implementation. The first milestone should prove that the **existing settings become easier to find, compare and edit** in the large workspace before adding broader management features.

Safety / architecture:

- reuse the existing staged draft, validation, auto-commit, conflict detection and `今回の変更を戻す` semantics;
- both compact Settings and the large workspace must converge on the same authoritative settings state;
- no Timeline mutation belongs in this workspace;
- no placement engine, Template body ownership or normal product boundary changes are implied.

## UI polish — active preparation

Status: **ACTIVE — UI Micro Polish prep**

### Generic layer controls always visible

Current popup adds one click before a high-frequency operation.

Desired compact header concept:

```text
汎用・時間配置                         レイヤー操作
再生位置にテンプレートの長さで配置
```

Within the existing header height, keep layer target and occupied-layer behavior directly operable.

Desired interaction direction:

- numeric layer target visible without opening a popup;
- occupied-layer behavior reachable in the same compact region;
- ideally, when Generic placement is active and no text editor owns input, direct number typing can enter the layer target;
- Enter applies a valid numeric draft;
- Esc restores the saved target;
- do not steal keys from normal text/ComboBox/DataGrid editing or position shortcuts.

This is a UX idea only; exact focus/key admission should be designed later.


### Voice row-height drag

Replace the coarse preset-only row-height choice with a direct global resize gesture.

Desired direction:

- one common row height remains authoritative for every Voice row;
- a compact drag grip adjusts the common height continuously on screen;
- valid range remains 32-96;
- dragging does not persist on every pixel movement;
- release commits the final height once through the protected settings store;
- failed persistence restores the saved height;
- do not introduce per-row heights or break DataGrid virtualization.

### Generic layer mouse-wheel adjustment

When the pointer is directly over the Generic numeric layer field:

- wheel up/down adjusts by one numeric layer step;
- bounds are respected;
- the changed complete number is applied immediately through the existing Generic target command/path;
- wheel elsewhere keeps normal panel/outer scrolling;
- no modified-wheel global interception.

### Bottom-right panel quick settings

Current bottom-right Settings button only jumps to the Settings tab, which is already one direct tab click away.

Replace that duplicate navigation role with a **panel quick-settings flyout**.

Candidate quick settings:

- Set-wide tile shape (rounded / square / circle);
- global Auto / Fixed layout;
- fixed column count;
- position shortcuts on/off;
- position shortcut assignments;
- other small appearance/operation controls proven useful during placement.

Boundary:

- quick settings = how this placement panel looks/operates;
- full Settings tab = what the Set means and how it places things.

Do not duplicate structural Set creation/deletion/applicability/relation editing into the flyout.

## Placement Recipe extensions — collect before implementation

Status: **COLLECTING**

Architecture boundary is defined in `docs/CURRENT_ARCHITECTURE.md`.

### 1. Template Pivot

Priority: **very high**

Align Template/Bundle start / center / end / explicit internal offset to the chosen target Anchor.

High value because current target Anchors are already rich, while the source-side alignment point is comparatively limited.

### 2. Absolute Layer

Priority: **very high**

Add an explicit Layer N base policy.

Reuse existing finite collision behavior where possible:

- place only if free;
- search one direction;
- never move/shorten/delete existing items.

### 3. Composite Placement Steps

Priority: **high**

One tile/Recipe may place several independent sources with different finite rules.

Example:

```text
強調

Frame:
  target span
  target-relative layer

Flash:
  target center
  Template Pivot = center
  different target-relative layer

SE:
  target center - 3f
  Absolute Layer = 50
```

Every Step must read the same immutable initial Context. Step A output cannot become Step B input.

All Steps preflight together; any required failure means zero write; success commits once with native Undo.

### 4. Stronger Target conditions

Priority: **medium**

Possible finite conditions:

- same Character across selected targets;
- explicit count per Item type;
- explicit before/after roles;
- finite mixed-type signatures.

Do not add arbitrary boolean trees, regex predicates or a generic rule language.

### 5. Fan-out

Priority: **medium**

Possible explicit scopes:

- each selected item;
- each explicit adjacent boundary.

All fan-out results must be one complete preflighted operation.

Deduplication of identical generated anchors must be specified before implementation.

### 6. Stronger Neighbor selectors

Priority: **medium**

Finite additions may include:

- next specified Item type;
- previous specified Item type;
- previous/next any Item;
- next same-Character Voice.

No fuzzy “nearest suitable thing” behavior.

## Action Tile extensions — collect before implementation

Status: **COLLECTING**

### YMM4 standard command tiles

User problem:

Frequently used YMM4 editing operations still require remembering keyboard shortcuts or leaving the context-sensitive Template Placer action surface. Some of those operations could live beside placement tiles when they are directly useful in the same editing flow.

Candidate operations:

- split;
- delete;
- copy / paste;
- undo / redo;
- play / pause;
- other high-frequency YMM4 standard commands proven useful during placement/editing.

Product boundary:

- keep this limited to operations closely tied to Template Placer's editing workflow;
- do not turn Template Placer into a general-purpose launcher;
- external tools, arbitrary macros, file/folder launchers and ToolBox-style addon hosting remain out of scope;
- coexist with ToolBox rather than duplicating its general launcher responsibility.

Architecture direction:

- do not encode commands as fake or nullable Template/Library entries;
- generalize the tile execution surface explicitly, e.g. an action kind such as `TemplatePlacement` / `YmmCommand`;
- keep tile presentation (label, color, shape, ordering) independent from the action payload where practical;
- position shortcuts should resolve the current slot and invoke the same tile execution path as a click;
- use YMM4's standard command route where available rather than synthesizing key input.

Safety / implementation gate:

- this is a future extension candidate, not approval to broaden the current active PR;
- first keep Preset/UI work and the existing placement architecture stable;
- before implementation, define the finite supported command set and native-test command availability / focus behavior.

## Deferred / out of scope

### Existing-item transformation

Keep outside Template Placer normal placement:

- move existing item;
- change existing Length;
- delete existing item;
- change Effect/property values;
- change volume;
- arbitrary retiming.

If this becomes a product, it belongs in a separate Transformer/Automation responsibility.

### General automation language

Do not add:

- arbitrary C# expressions;
- scripting DSL;
- node graph;
- loops;
- previous-Step conditional branching;
- AI-selected ambiguous targets.

### Item-type custom ordering

Status: **DEFERRED**

Potentially useful, but lower frequency than the placement and panel ideas above. Keep separate until hands-on usage justifies it.

## Backlog discipline

When adding an idea:

1. state the user problem first;
2. state whether it changes UI, Context, planning axes, output multiplicity, or product boundary;
3. state the safety boundary;
4. do not create implementation tasks until related ideas have been collected/reviewed;
5. move an idea into a feature handoff/spec only when implementation is intentionally starting.

Do not use this file as a reason to broaden the current active PR automatically.
