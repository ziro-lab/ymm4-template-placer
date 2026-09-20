# UI Micro Polish — corrective implementation status

Status: **C0-C4 IMPLEMENTED / NATIVE REVALIDATION BLOCKED BY ACTIONS STARTUP**

## Accepted evidence before corrective pass

The first UI Micro Polish Hands-on candidate remains a valid historical checkpoint:

- source `e58d267286399d16c1ffe19bbd7411af58039f55`
- Release run #262 `35521238124`
- 1,437 assertions PASS / 0 FAIL
- exact distribution DLL smoke PASS
- verified package PASS

Owner Hands-on then produced F1-F7 in `UI_MICRO_POLISH_HANDS_ON_FEEDBACK.md`.

That Release does **not** prove the corrective changes below.

## Corrective authority

- `UI_MICRO_POLISH_CORRECTIVE_DESIGN.md`
- `UI_MICRO_POLISH_CORRECTIVE_ACCEPTANCE.md`
- `UI_MICRO_POLISH_CORRECTIVE_WORKPLAN.md`

## Implemented corrective scope

### C0 — rejected U4 removal / public cleanup

Implemented:

- removed direct application-level Generic digit interception;
- restored palette input router to explicit position shortcuts only;
- removed U4-only focus forwarding/proof;
- simplified awkward handled assignment;
- removed no-op `EndPanelQuickSettings()`;
- documented intentional fail-soft compatibility catches in `ExpressionNavigationHost`.

### C1 — quiet wheel success

Implemented:

- wheel still uses the same protected Generic apply/store path;
- routine wheel success does not replace bottom Status per notch;
- explicit apply may still announce success;
- errors remain visible.

### C2 — common Voice row height from row boundaries

Implemented:

- removed top standalone drag grip;
- any realized Voice row bottom edge is a bounded common-height resize gesture;
- one common 32-96 height remains authoritative;
- DragDelta is presentation-only;
- release persists once;
- top numeric field supports direct height entry with Enter/Esc;
- no per-row height state or DataGridRow template replacement.

### C3 — responsive Fixed grid

Implemented:

- Auto retains 104x104 cells;
- Fixed preserves exact saved column count and slot order;
- Fixed square cells grow to use wider viewport width;
- cells never shrink below 104x104;
- no settings schema change.

### C4 — quick-settings owner lifetime

Implemented:

- placement panel observes its owning WPF Window while loaded;
- owner Window `Deactivated` closes quick settings;
- no polling/global activation hook.

## Updated proof/package expectations

Native Round4 presentation proof has been revised to cover:

- quiet wheel Status;
- popup closing on owner deactivation;
- wide/narrow Fixed grid geometry;
- real mouse Voice-row boundary drag;
- numeric height Enter/Esc/invalid rejection.

`USAGE.md` and `PackageVerified.ps1` now describe/require the corrective UI rather than rejected U4/top-grip behavior.

## Current validation blocker

Corrective branch current source is not yet Native-accepted.

Latest corrective product/source change before documentation-only authority updates:

- `1bbe5e5db17bdbbee8cd4fd36b71bd183ea4f76e`

Current branch/documentation HEAD continues beyond that without changing product semantics.

Attempted Actions validation:

- PR run #291 `35523841897`
- dedicated Release run #292 `35523849044`, including rerun attempt #2
- dedicated Release run #299 `35524227916`

These jobs failed before executing **any workflow step**; job step lists are empty. This is an Actions/runner-start condition, not product/test failure evidence.

Earlier Release #290 did start normally and exposed only a missing proof-source `VisualTreeHelper` import; that proof compile issue was corrected before the later corrective commits.

Do not label the corrective candidate Native-green until a real Windows/YMM4 run executes.

## Next action when Actions can start

1. run Checkpoint/Release from current corrective HEAD;
2. fix only evidence-backed product/test failures;
3. when green, produce a new corrected Hands-on `.ymme`;
4. owner Hands-on;
5. only then merge PR #20;
6. refresh paused Tachie Preset PR #19 from accepted main.
