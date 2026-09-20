# W10 native checkpoint

Verified source: `de071b85bdd74bf51e4ceaa5d15b26ccdaca2ca0`.
Native Windows YMM4 4.55.1.1 Lite run: `34862976929`.

- 285 native assertions PASS, preserving W1-W9.
- Release/proof builds: 0 warnings / 0 errors, warnings treated as errors.
- P1-P9, Open XML, release DLL smoke and packaging PASS.
- Artifact `10356237153`, SHA256 `8c761d04c569d2089c357e0c05bc9d2c5ce9d9b751255794999ab15beefa6dd8`.
- Distribution DLL SHA256 `74c23edcb3f7b971c692fa34c01ad528be4c47ccacca9438295f890320861e49`.

## Proven behavior

Selection Range uses min(selected.Frame) minus nonnegative HeadPadding and max(selected.End) plus nonnegative TailPadding. Selection order and last-starting item do not determine the end. Exactly one independent native TextItem overlay covers the range across multiple Characters when the Template is Character-neutral.

Actual WPF padding editor, Save, Preview and Place are verified. A blocker starting inside the future range forces the existing LayerPlanner to use the next free Layer. The existing PlacementPlan commits the operation; native Undo/Redo is one unit. No new Timeline mutation engine was added.

Negative padding, negative result start, end overflow, layer exhaustion, incompatible Character association, changed target geometry, no selection and one-target misuse are rejected without partial mutation. Existing targets, Template and unrelated manual items are preserved.

Older W9 settings add the new default profile in memory only, preserving user names, current preset and on-disk bytes until explicit save. Reload does not duplicate presets. Unknown future revisions and missing old profile families are not silently repaired.

## Usage

Select two or more Timeline Items, open Selection Placement, choose Selection Range and a Library Template. Edit nonnegative before/after padding and the Layer policy, save, preview and place. This is add-only, independent placement, not automatic connected clips or expression Resync.

W11 Boundary and W12 final UX/version/docs/package remain. The package is still intermediate 0.3.1, not final v0.4. Keep PR #6 Draft and main unchanged.
