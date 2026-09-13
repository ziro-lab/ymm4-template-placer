# Tests

Automated tests should prefer deterministic state assertions over GUI screenshots.

Planned coverage follows `docs/ROADMAP.md`:

- plugin load
- live template catalog
- VoiceItem snapshot
- Face Template clone / placement
- YMM4 UI assignment path
- Excel export/import
- repeatability / atomic failure
- Undo

Do not make tests depend on copyrighted character assets. Use tiny synthetic fixtures.
