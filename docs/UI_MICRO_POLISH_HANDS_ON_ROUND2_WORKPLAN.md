# UI Micro Polish — owner Hands-on Round 2 workplan

Status: **READY FOR IMPLEMENTATION**

Base branch: `work/ui-micro-polish-prep`

Authority:

1. `UI_MICRO_POLISH_HANDS_ON_ROUND2_FEEDBACK.md`
2. `UI_MICRO_POLISH_HANDS_ON_ROUND2_DESIGN.md`
3. `UI_MICRO_POLISH_HANDS_ON_ROUND2_ACCEPTANCE.md`
4. this workplan

## R2C0 — quick-settings light-dismiss

Implement owner-window outside-click dismissal without changing the quick-settings data model.

Primary files:

- `IntentPalettePanel.xaml.cs`
- `IntentPalettePanel.xaml`
- Round4 presentation proof

Keep:

- `StaysOpen=True`;
- existing owner `Deactivated` behavior;
- same-Set refresh stability.

Add:

- popup-open-scoped owner `PreviewMouseDown` subscription;
- ignore the quick-settings toggle itself;
- close on any other owner-window mouse click;
- never mark that click handled;
- deterministic detach on Popup close/unload/owner replacement.

Validation:

- internal Button / ComboBox / TextBox interaction stays open;
- native outside click closes;
- deactivation still closes;
- activation does not reopen.

## R2C1 — remove global targeted-Set management mode

Remove `ShowAllSets` from the normal Settings navigation model.

Primary files:

- `IntentSettingsNavigation.cs`
- `IntentSettingsPanel.xaml`
- `SettingsSessionIntegration.cs`
- `IntentTileEditing.cs`

Required behavior:

- `セットの管理` expansion is presentation-only;
- `VisiblePalettes` is always scoped to the current Item owner/compatibility context;
- rollback/session recreation no longer preserves a show-all flag;
- tile -> Settings navigation resolves the owner context directly.

Update old proof that currently requires `ShowAllSets=true`.

## R2C2 — single-owner normal Set creation/editor

Tighten normal targeted Set creation to one owner type.

Primary files:

- `IntentSettingsDraft.cs`
- `IntentSettingsNavigation.cs`
- `IntentSettingsPanel.xaml`
- `IntentPaletteModels.cs` helper logic if needed

Required behavior:

- manually selected Item parent -> one owner type;
- same-runtime-type current selection -> one owner type + count/Character defaults;
- mixed runtime selection -> reject new shared Set with actionable message;
- remove normal Item-type checkbox matrix / `ExactMixedTypes` choice;
- retain selection count and Character restriction.

Do not change persisted field names or revision in this step.

## R2C3 — owner-local name and ordering

Make names/order behave as children of the owner Item type.

Primary files:

- `IntentSettingsDraft.cs`
- possibly small helpers in `IntentSettingsNavigation.cs`

Required behavior:

- uniqueness/suffix check is owner-local;
- move up/down moves within the current owner's filtered sequence;
- other owners retain their relative order.

Do not rewrite the whole palette list merely to group types visually if local deterministic movement is sufficient.

## R2C4 — cross-Item snapshot copy

Add explicit copy-to-another-Item operation.

Primary files:

- `IntentSettingsViewModel.cs`
- `IntentSettingsDraft.cs`
- `IntentSettingsPanel.xaml`

Recommended UI:

```text
セットの管理
[複製] [↑] [↓] [削除]

他のアイテムへコピー
[コピー先 ▼] [コピー]
```

Behavior:

- destination list = known single Item contexts excluding Generic/current owner;
- build/validate current draft first;
- deep copy;
- new Guid;
- exactly one destination type;
- `UniformType`;
- preserve all other snapshot state;
- destination-local name disambiguation;
- protected auto-commit path;
- navigate/select destination copy.

Use existing same-owner `複製` for same-owner cloning.

## R2C5 — bounded legacy multi-type compatibility

Preserve existing multi-type Sets without letting the normal editor create new ones.

Primary files:

- `IntentSettingsNavigation.cs`
- `IntentSettingsDraft.cs`
- `IntentSettingsPanel.xaml`
- tests for old settings roundtrip

Add `複数種類` context only when multi-type Sets exist.

Compatibility context:

- shows only multi-type Sets;
- preserves current serialized target content;
- may copy one of them into a normal single-owner Set;
- does not silently assign an owner;
- does not invent/split semantics.

Do not broaden this into a new multi-type editor redesign.

## R2C6 — focused proof update

Update proof before full Checkpoint.

Must replace historical expectations that:

- Set management intentionally exposes all Sets;
- mixed/multiple target editing is a normal advanced path.

Add targeted proof for:

- Item-owned filters;
- same names across owners;
- owner-local move;
- same-owner duplicate;
- cross-owner snapshot copy and independence;
- mixed-selection create rejection;
- legacy multi-type compatibility context;
- exact tile -> owner Settings navigation;
- quick-settings light-dismiss.

Expected likely test files:

- `NativeHandsOnUxPolishProof.cs`
- `NativeHandsOnRound3SettingsProof.cs`
- `NativeHandsOnRound4PresentationProof.cs`
- `NativeHandsOnRound4SettingsProof.cs` / session/transaction proofs as needed
- core Intent proof for compatibility semantics

Do not delete historical safety proof merely because its UI wording changed; update or retire only the superseded assertion.

## R2C7 — docs/package wording

Update:

- `docs/USAGE.md`
- `docs/CURRENT_ARCHITECTURE.md`
- `docs/GLOSSARY.md` if Set ownership terminology needs an authority note
- package verification strings that explicitly expect superseded UI wording

Do not add a settings schema migration section unless code actually changes schema.

## R2C8 — Checkpoint

Run full Checkpoint.

Require:

- all new R2 acceptance;
- all retained Round4 A/B/C;
- previous placement/Undo/fidelity/settings transaction gates;
- zero warnings/errors.

Fix only evidence-backed failures.

## R2C9 — Release candidate

After Checkpoint green:

1. move dedicated validation branch to exact candidate HEAD;
2. run Release;
3. require exact distribution-DLL native smoke;
4. require verified .ymme/source/provenance package;
5. produce new owner Hands-on .ymme;
6. record hashes in PR #20;
7. owner Hands-on;
8. merge only after owner acceptance.

## Stop conditions

Stop and reopen design instead of expanding scope if:

- single-owner behavior requires a second serialized owner field;
- preserving old multi-type data would require destructive migration;
- outside-click dismiss requires an application-global or OS-global hook;
- copy needs a second Settings persistence path;
- owner-local ordering cannot be represented deterministically without schema change;
- implementation starts changing placement relation semantics rather than ownership/Settings organization.
