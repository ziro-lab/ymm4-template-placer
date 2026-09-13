# Fixtures

Keep runtime fixtures minimal, deterministic, and redistribution-safe.

For the initial Voice -> Face Template profile, fixture visuals do not need meaningful artwork. A trivial synthetic/black image is sufficient when a YMM4 object requires an image asset.

Fixture goals:

- at least two test characters
- multiple VoiceItems with distinct Frame / Length values
- at least two Face Templates per character
- one manually placed FaceItem that must survive plugin placement
- one invalid/missing-template case

Prefer constructing deterministic test state inside YMM4 during CI when that avoids brittle GUI project-open automation.
