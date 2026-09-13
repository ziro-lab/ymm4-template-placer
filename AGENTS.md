# AGENTS.md

## Project intent

YMM4 Template Placer is a small YukkuriMovieMaker4 plugin for placing registered YMM4 item templates according to target-item conditions.

CURRENT profile only:

```text
VoiceItem -> TachieFaceItem Template
```

The product name is intentionally broader than the first use case, but implementation must stay narrow until the current profile is complete.

## Product constraints

- Primary path is YMM4-only: list VoiceItems, choose a template from a dropdown, place it.
- Excel is a secondary bridge for bulk human/AI assignment, not the primary UX.
- YMM4 `ItemSettings.Default.Templates` is the template source of truth.
- Do not create a plugin-owned expression database in CURRENT.
- Do not add a generic rule engine, DSL, multi-scene batch, AI API integration, persistent Voice IDs, sync engine, or PSD-specific logic unless explicitly promoted from FUTURE.
- Exported Excel is a snapshot. If YMM4 items/templates change after export, re-export instead of reconciling.
- Plugin-created face items are identified only by a simple Remark marker: `CWT_TPL:face`.
- Re-placement may replace all plugin-marked face items for the current scope; differential reconciliation is not required.
- Preserve manually placed face items.
- Prefer YMM4-native Undo/Redo over custom rollback machinery.

## Technical target

- YMM4 4.55.1.1 Lite
- .NET 10
- `net10.0-windows10.0.19041.0`
- WPF plugin
- Native Windows GitHub Actions as the primary automated runtime proof

## CI rules

- Heavy YMM4 runtime CI must run only when plugin source/project files or the workflow itself change, plus manual dispatch.
- Documentation-only changes must not trigger YMM4 download/launch.
- Keep fixtures tiny, deterministic, and redistribution-safe. A trivial black image is sufficient when pixels are irrelevant.
- Prefer direct state assertions over screenshots.

## Implementation order

Follow `docs/ROADMAP.md`. Do not skip directly to Excel or generic abstractions before P1-P3 establish the YMM4 runtime path.

## External references

Existing YMM4 projects are implementation evidence, not a license to copy blindly. Review source licenses before copying code. Prefer reimplementing the small required behavior against YMM4 APIs.
