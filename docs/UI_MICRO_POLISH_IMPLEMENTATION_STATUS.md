# UI Micro Polish — implementation status

Status: **AUTOMATED CANDIDATE COMPLETE / HANDS-ON PENDING**

## Candidate identity

Product implementation checkpoint:

- source before docs-only closeout: `9a09d53f8d1b827e44eb3c9779e7312994e4bbe5`
- Checkpoint run #255: `35520073021`
- job: `106102514624`
- native assertions: **1,437 PASS / 0 FAIL**
- `HANDS_ON_ROUND4_A=PASS`
- `HANDS_ON_ROUND4_B=PASS`
- `HANDS_ON_ROUND4_C=PASS`
- full semantic Checkpoint/evidence guards: PASS
- Distribution build: 0 warnings / 0 errors
- Proof build: 0 warnings / 0 errors

Docs-only closeout commits after this checkpoint do not alter product code or native tests.

## U1 — Generic inline layer controls

DONE.

- layer target and occupied behavior are always visible in the compact Generic header;
- no normal popup click is required;
- Enter applies / Esc restores;
- dirty/invalid draft blocks placement;
- wheel directly over the numeric field changes ±1 through the existing protected apply path;
- wheel stays inside saved bounds;
- incomplete/stale drafts fail closed;
- no planner semantics changed.

## U2 — panel quick settings

DONE.

Bottom-right duplicate Settings-tab navigation was replaced by local `⚙ 簡易設定`.

Quick settings expose:

- current-Set Set-wide shape;
- global Auto / Fixed;
- fixed columns;
- position shortcut enable/assignments.

Structural Set semantics remain in the full Settings tab.

The flyout:

- does not switch tabs;
- opening/closing is zero-write;
- uses the existing protected settings store;
- blocks conflicting full-Settings drafts;
- stays open across same-Set refresh;
- closes on real Set/context lifetime changes.

## U3 — Voice row-height drag

DONE.

- one common row height remains authoritative;
- drag range is 32-96;
- DragDelta changes presentation only;
- no settings write occurs during drag;
- release persists the final height once;
- Rows are not reconstructed;
- Timeline is unchanged;
- Serif font size is unchanged;
- no per-row height model was introduced.

## U4 — direct Generic digit entry

DONE.

The existing bounded palette application-input route is reused; no second keyboard hook was added.

Priority:

1. a reserved position shortcut wins;
2. otherwise an unmodified digit may start Generic layer entry;
3. text/ComboBox/DataGrid/editor/menu/modal ownership still blocks admission;
4. Enter commits through the existing Generic protected apply path.

## Remaining gate

Only real-user Hands-on remains before promotion.

Recommended checks:

- Generic header still feels compact at normal and narrow Tool widths;
- wheel direction/step feels natural;
- direct digit entry does not surprise during normal editing;
- `⚙ 簡易設定` is more useful than the old Settings jump;
- Set-wide shape/layout/shortcut changes feel immediate;
- row-height grip feels easy to grab and does not jitter.

After owner acceptance:

1. run Release candidate validation from the accepted PR HEAD;
2. merge PR #20 to main;
3. verify the main Release run;
4. refresh paused Tachie Preset PR #19 from the new main before implementation resumes.
