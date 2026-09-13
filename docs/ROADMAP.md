# ROADMAP — native YMM4 proof ladder

Keep each step small. A later phase must not hide failure in an earlier YMM4 integration point.

## P0 — Plugin load

- build on Windows runner
- install DLL into YMM4 `user/plugin`
- launch real YMM4 4.55.1.1 Lite
- prove plugin callback executes

Exit: deterministic load marker PASS.

## P1 — Live Template catalog

- read `ItemSettings.Default.Templates`
- enumerate names/count
- detect a deterministic test Face Template

Exit: template catalog evidence PASS.

## P2 — VoiceItem access

- create or expose a tiny deterministic test Timeline
- enumerate VoiceItems
- assert Character / Frame / Length / Serif

Exit: Voice snapshot PASS.

## P3 — Template clone and placement

- clone one Face Template item
- set Frame / Length / marker
- insert into Timeline
- assert resulting Timeline state

Exit: core YMM4 placement path PASS.

## P4 — YMM4 assignment UI

- show current Voice rows
- Character-filtered Template dropdown
- place selected assignments using the P3 engine

Exit: primary YMM4-only Golden Path PASS.

## P5 — Excel export

- generate `.xlsx` without Excel COM automation
- Voice snapshot sheet
- hidden Template catalog
- Character-specific dropdowns

Exit: workbook structural assertions PASS.

## P6 — Excel import

- edit fixture workbook programmatically
- import to shared Assignment model
- place through the same engine as P4

Exit: secondary Excel/AI Golden Path PASS.

## P7 — Safety / repeatability

- re-import / re-place does not multiply `CWT_TPL:face`
- manual FaceItems survive
- missing Template / invalid workbook causes zero Timeline mutation

Exit: repeatability and atomic validation PASS.

## P8 — Undo

- place multiple generated FaceItems
- one YMM4 Undo returns the Timeline to the pre-placement state

Exit: Undo integration PASS.

## CI policy

Native Windows YMM4 proof is the main automated lane.

Heavy CI triggers only on:

- `src/**/*.cs`
- `src/**/*.csproj`
- test/fixture code once added
- the native YMM4 workflow itself
- manual dispatch

Documentation-only commits must not download or launch YMM4.

Linux + Wine is not a normal validation lane for this repository.
