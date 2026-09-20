# UI Micro Polish — implementation preparation

## Branch / scope

Branch:

`work/ui-micro-polish-prep`

Base:

`7dc30dcb887a0d57f3079d72ffe89dc62bedd9fa`

This preparation pass is documentation-only. Product implementation starts only after the design/acceptance/workplan are reviewed.

Tachie Preset Draft PR #19 remains paused and separate.

## Read order

1. `docs/CURRENT_ARCHITECTURE.md`
2. `docs/GLOSSARY.md`
3. `docs/UI_MICRO_POLISH_DESIGN.md`
4. `docs/UI_MICRO_POLISH_ACCEPTANCE.md`
5. `docs/UI_MICRO_POLISH_WORKPLAN.md`
6. `docs/VALIDATION_STRATEGY.md`
7. `docs/LEGACY_COMPATIBILITY_MAP.md`
8. this file

## Existing implementation facts

### Generic layer

Current product already has:

- `GenericLayerTargetDraft`;
- text target draft;
- finite occupied behavior list;
- Apply/Reset commands;
- dirty/invalid admission;
- protected save;
- bounded one-direction layer planner.

UI currently hides it behind `GenericLayerButton + Popup`.

The polish should move/re-present the same state, not reimplement it.

### Panel presentation

Current global durable model already contains:

- `LayoutMode`;
- `FixedColumns`;
- `ShortcutsEnabled`;
- `PositionShortcuts`;
- `ExpressionRowHeight`;
- `ViewportFollow`.

Do not create Set-local copies.

`PalettePresentationDraft` already supplies validation and editable shortcut rows.

### Set-wide shape

`ChangeIntentSetShape` already performs:

- current-Set validation;
- protected settings write;
- no Timeline mutation;
- no inherited/default shape model.

Reuse this behavior.

### Row height

`ExpressionRowHeight` already persists arbitrary integer values accepted by `PalettePresentationSettings.Validate()` in 32-96.

Current ComboBox only exposes preset values, but the stored setting is not limited to those preset values.

Therefore the drag UI can support every integer 32-96 without a settings migration.

### Keyboard lifetime

`PaletteShortcutInputRouter` already owns the bounded application-input lifetime for placement shortcuts and excludes editable/menu/modal input.

Optional direct Generic numeric typing must extend/reuse this one route rather than attach another global key listener.

## Expected source touch map

Required:

- `IntentPalettePanel.xaml[.cs]`
- `GenericLayerTargetPanel.xaml[.cs]` or one compact visual sibling
- `PlacerView.xaml[.cs]`
- `CompactPresentation.cs`

Likely for quick settings:

- `PalettePresentationDraft.cs`
- `IntentSetAppearance.cs`
- `PalettePositionShortcuts.cs`
- one new small quick-settings panel/root partial

Optional direct digit route:

- `PalettePositionShortcuts.cs` input router
- small View focus-forwarding hook if absolutely necessary

Avoid touching:

- PlacementPlan/planners;
- Template resolver/clone code;
- association tags;
- Excel;
- Tachie Preset files;
- serialized settings shape unless evidence proves necessary.

## Validation sizing

Do not make every acceptance sentence a permanent separate regression stage.

During implementation:

- targeted native assertions prove the new interaction;
- reuse stable safety tests for Timeline zero-write/settings conflict/shortcut input ownership;
- Focused for ordinary source iterations;
- one Checkpoint at feature completion.

After acceptance, consolidate UI-only evidence where it protects the same invariant.

## Stop conditions

Stop/review instead of widening the design if:

- always-visible Generic controls cannot fit usefully at the pinned 360 minimum without adding a new full-width row;
- direct digit entry requires another global keyboard hook or steals active shortcuts/editors;
- quick settings requires duplicating the complete IntentSettingsSession;
- row-height drag requires per-row heights or breaks virtualization;
- a UI shortcut requires weakening settings conflict/digest protection;
- implementation starts modifying placement semantics to make the UI easier.
