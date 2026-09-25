# Placement Quick Settings Acceptance

Status: **P0-P2 NATIVE ACCEPTED / P3 OWNER HANDS-ON PENDING**

## P0 — shared-state architecture

Accept only if:

1. Targeted placement quick settings resolve the exact current Set into the existing `IntentSettingsSession`.
2. Controls edit the existing `IntentPaletteDraft`; there is no second placement Draft/schema.
3. Opening/closing without edits is zero-write.
4. The existing `SettingsEditTransaction` and protected automatic persistence remain authoritative.
5. External conflicts fail closed without rebasing or overwriting.
6. Generic Sets do not gain a misleading Targeted relation editor.

## P1 — finite quick actions

For every shipped action:

1. the exact existing fields changed are documented;
2. unrelated fields are preserved;
3. action output builds as a valid Settings Draft;
4. the accepted Behavior Preview changes from the same Draft;
5. normal Settings shows the exact resulting values;
6. no hidden numeric default is invented.

Initial candidates:

- start / center / end result alignment;
- target-span / template duration;
- bounded next-same-character-start recipe if applicable;
- one-layer up / one-layer down.

## P2 — commit/placement ordering

Native acceptance must prove:

1. a quick action updates Preview without Timeline mutation;
2. a valid quick action commits through the existing protected Settings path;
3. the next tile placement uses the newly committed relation;
4. an outside click that closes Quick Settings is not swallowed;
5. that outside placement cannot execute using the old relation;
6. Settings conflict/dirty-session admission prevents a quick action from bypassing the protected session;
7. one native Undo still reverts the resulting placement action only; Settings history remains the Settings transaction responsibility.

## Native acceptance evidence

Exact tested product/test/package source: `9dae643cb9c29780783dde265d41dc4a90f01a62`.

Release #770 / run `36076134931`:

- **1,277 ASSERT PASS / 0 FAIL**;
- `PLACEMENT_QUICK_SETTINGS_P0=PASS`;
- `PLACEMENT_QUICK_SETTINGS_P1=PASS`;
- `PLACEMENT_QUICK_SETTINGS_P2=PASS`;
- distribution build and proof build: **0 warnings / 0 errors**;
- exact distribution-DLL smoke PASS;
- PackageVerified PASS;
- Release artifact `10840163556`, uploaded artifact SHA256 `fc938506cb949bdbfd0c6427da88b70b16d7f1c9e0724528d08b5473ff6713e0`;
- distribution DLL SHA256 `e9505f9b140aedb8decee9cc812d46d4beb8c7ca000c7653a496401bda2d1a29`.

P0-P2 therefore no longer depend on design inference. P3 remains an owner hands-on UX decision.

## P3 — hands-on

Hands-on should answer:

- Can the common correction be made faster than opening normal Settings?
- Is the Preview sufficient to verify the result before placing?
- Are the visible quick choices small enough to scan immediately?
- Is any candidate action ambiguous about what it changes?
- Does the existing `⚙ 簡易設定` container still feel coherent with placement and presentation responsibilities separated?

If the first slice starts growing toward full `どう置く？`, stop and move that control back to normal Settings.
