# Behavior Preview Design

Status: **ACTIVE — P0 DESCRIPTION PROJECTION**

## Goal

Make current placement Settings understandable visually without creating another placement engine.

The phase is intentionally split:

1. **Behavior Preview** first: read-only explanation of the current Settings Draft;
2. **“What I want” checklist** afterward: an editing projection that writes back to the same Draft.

Undo/Redo shortcut work stays outside this phase until Preview/Checklist reaches a stable stopping point.

## Core architecture

```text
current Settings Draft
        ↓
PlacementBehaviorDescription
        ├─ existing text: 「このセットの動き」
        └─ read-only Behavior Preview

current Settings / source + live Timeline context
        ↓
existing resolver / geometry / PlacementPlan
        ↓
native Undo
```

`PlacementBehaviorDescription` is a **read-only meaning projection**. It must not inspect live Timeline state, resolve a real neighbor/layer, materialize a source, mutate Settings/Timeline, or duplicate `IntentRelationResolver`, `BundleLayerPlanner` or `PlacementPlan`.

The Preview explains the rule. Existing resolver/geometry code still decides the actual operation for a concrete Context.

## P0 — common description projection

Move the natural-language Summary behind one typed projection shared by Targeted and Generic Settings Drafts.

Targeted description carries the current finite vocabulary: target Item types / Character restriction, anchor, start/center/end alignment, duration, neighbor + edge, fallback, offsets/bounded numeric details, relative vs. absolute layer, direction and range.

Generic description carries Template-layer legacy behavior, bounded numeric layer target/range and occupied-layer behavior.

The current Summary wording is a compatibility/UX baseline. P0 must not intentionally change it.

Incomplete numeric Draft text must remain projectable. The projection may expose a nullable parsed value, but it must never invent a replacement value.

## P1 — read-only Preview

Render a small schematic from `PlacementBehaviorDescription`.

Initial visual vocabulary is finite: target/context block, anchor marker, source/output span, start/center/end relationship, duration mode, relative/absolute layer, search direction, and neighbor/fallback indicator when relevant.

The first Preview is not a scale-accurate Timeline simulator. Prefer a stable schematic that communicates relationships over fake precision.

Do not add drag/edit gestures in P1.

## Source behavior

Template and registered Tachie Preset sources share the same Set-owned placement description where placement semantics are shared. Source kind may be shown only when it explains availability/content identity; it must not fork the geometry explanation.

Unregistered direct Tachie Preset compatibility may retain its bounded separate rule until that compatibility path is intentionally removed.

## Draft behavior

Preview reads the **currently edited Settings Draft**, including uncommitted valid edits.

For incomplete/invalid numeric text, keep the editor text intact, do not persist or normalize it, show a bounded unknown/incomplete state where necessary, and never silently fall back to the previously persisted number.

## Non-goals

This phase does not implement Preview drag editing, a second placement resolver, Full Settings Workspace, Undo/Redo shortcut tiles, Composite Steps/Fan-out/stronger Target grammar, or Item Action/Transformer execution.
