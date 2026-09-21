# UI Micro Polish — corrective implementation status

Status: **C0-C4 IMPLEMENTED / CHECKPOINT GREEN / RELEASE PENDING**

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

Native Round4 presentation proof covers:

- quiet wheel Status;
- popup closing on actual owner-window deactivation;
- an explicit activation-transfer precondition so a CI desktop cannot falsely count a no-op `Activate()` as C4 evidence;
- wide/narrow Fixed grid geometry;
- real mouse Voice-row boundary drag;
- numeric height Enter/Esc/invalid rejection.

`USAGE.md` and `PackageVerified.ps1` describe/require the corrective UI rather than rejected U4/top-grip behavior.

Public-release preparation is also carried forward from main:

- the Japanese public README and `LICENSE.md` are present;
- the verified `.ymme` package now includes `LICENSE.md` alongside `THIRD_PARTY_NOTICES.md`;
- the source archive explicitly requires the license entry.

## Native revalidation

The earlier Actions startup blocker was not a product/test failure. Before the repository became public, hosted jobs were being created and failing before any workflow step. After public visibility removed the private-repository usage limit, hosted runners resumed normally.

### First resumed Release attempt

Release run #299 `35524227916`, attempt 3:

- checkout / .NET / YMM4 download: PASS;
- Distribution build: PASS;
- Proof build: PASS;
- Native proof reached Round4 B;
- C0-C3 passed;
- C4 owner-deactivation assertion failed once.

The failure was isolated to the activation proof: the old test did not establish whether its probe Window had actually taken activation from the owner before judging product behavior.

### Strengthened C4 proof

Test change:

- `e44133da9e4b638de8ef59eb5d785d804305a42f`

Checkpoint run #302 `35550873470`:

- level: Checkpoint;
- full Native validation: PASS;
- `HANDS_ON_ROUND4_A=PASS`;
- `HANDS_ON_ROUND4_B=PASS`;
- `HANDS_ON_ROUND4_C=PASS`;
- C4 trace proved owner active -> probe active -> owner `Deactivated` once -> popup closed -> toggle false.

The stronger assertion is retained because it prevents hosted-desktop activation flakiness from being mistaken for a product result.

### License-in-package checkpoint

Latest product/test/package change:

- `7f89c9744d8f2ab8e54a277a5a305a891d6f443b`

Checkpoint run #303 `35551157812`:

- level: Checkpoint;
- **1,443 assertions PASS / 0 FAIL**;
- Release build: 0 warnings / 0 errors;
- Proof build: 0 warnings / 0 errors;
- `HANDS_ON_ROUND4_A/B/C=PASS`;
- license packaging gate included in the tested source.

This is the current corrective Native-green checkpoint.

## Next action

1. fast-forward `work/v0.4-native-validation` to the current corrective HEAD;
2. run Release;
3. require exact distribution-DLL native smoke + verified `.ymme` / source / provenance package;
4. produce the corrected Hands-on `.ymme`;
5. owner Hands-on;
6. only then merge PR #20;
7. refresh paused Tachie Preset PR #19 from accepted main.
