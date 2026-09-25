# Backlog

This is the collection point for **unimplemented or deferred product ideas**.

Being listed here is not implementation approval or priority commitment. Current implementation order is controlled by `docs/PRODUCT_ROADMAP.md`.

Current roadmap state: **Behavior Preview and native Undo/Redo are accepted; Placement Quick Settings first slice is Release GREEN with owner hands-on NEXT.** The “what I want” checklist and Full Settings Workspace are deferred for need-based re-evaluation. Do not use this Backlog to silently reorder those phases.

## Candidate triage snapshot

This is a lightweight decision aid for the **still-open** candidates below. It does not override `PRODUCT_ROADMAP.md`, and completed/current closeout work is not ranked here.

Primary axes:

- **Effect** — expected reduction in editing time, repeated actions, or cognitive load.
- **Implementation** — expected product/UI/host-evidence/validation cost.

Secondary filters:

- **Reach** — how often / how broadly the benefit applies.
- **Fit** — whether the idea strengthens Template Placer's core role of **showing and placing reusable content**, is merely adjacent, or introduces a separate editing responsibility.
- **Future cost** — ongoing compatibility, maintenance, UI/state complexity, and regression surface.

Ratings are deliberately coarse: **H / M / L**. Implementation uses **L / M / H / ?** where measurement or host evidence is required first.

A useful candidate should not be promoted from score alone. Each candidate also has a **promotion trigger**: the concrete user problem/evidence that should exist before implementation starts.

### Tier A — strong payoff / revisit first when the trigger appears

| Candidate | Effect | Implementation | Reach | Fit | Future cost | Promotion trigger |
| --- | --- | --- | --- | --- | --- | --- |
| Set display / workspace profiles | H at scale | M | M-H | Core | L-M | Set-switch shortcuts exist, but the number of Sets still creates meaningful recognition/navigation cost in real projects |
| Visual grouping / separators on tile surfaces | M | L | H | Core | L | Real Sets become hard to scan even though their tile order is already good |
| Tile/action search | M now / H at scale | M | M-H | Core | M | Normal browsing/filtering becomes materially slower once Set/tile counts grow |
| Repeat last Template Placer action | H if used repeatedly | M | M-H | Core-adjacent | M | Hands-on use shows a common repeated-placement loop where re-resolving current Context is predictable and clearly faster than choosing the tile again |
| Item-type custom ordering | L-M | L | M | Core | L | Default Item-type ordering itself becomes a repeated navigation irritation |
| Persistent shortcut strip / utility tiles | M-H | L-M | H | Core-adjacent | M | Real use shows a small set of actions should remain one-click reachable across Set/context changes without consuming the main tile grid |

### Tier B — valuable, but evidence-gated or structurally heavier

| Candidate | Effect | Implementation | Reach | Fit | Future cost | Promotion trigger |
| --- | --- | --- | --- | --- | --- | --- |
| Built-in Tachie Preset loading performance polish | H if the hot path is real | ? | M | Core | M | Profiling identifies a measurable built-in preset hot path that materially affects ordinary use |
| Quick Palette near editing location | M-H | M-H | M-H | Core | M-H | Existing Set/tile shortcuts still leave a meaningful pointer-travel / surface-access cost |
| Previous / next relevant Item selection | M | L-M | M-H | Adjacent | L | Repeated navigation between relevant Timeline items is measured as a common Template Placer workflow bottleneck |
| Stronger Neighbor selectors | M-H | M | M | Core | M | Current finite selectors cannot express recurring real placement cases after ambiguity fan-out is already solved |
| Stronger Target conditions | M-H | M-H | M | Core | M | Repeated real projects need explicit conditions such as count/type/role that cannot be represented safely today |
| Fan-out over explicit selections / boundaries | H for batch workflows | H | M | Core | M-H | Users repeatedly perform the same safe placement once per explicit selected target/boundary |
| Composite Placement Steps | H for compound workflows | H | L-M | Core | H | Multiple independent placements are repeatedly performed together and one atomic tile operation would materially reduce work |
| Full Settings Workspace | M at large scale | H | L-M | Core | H | Compact Settings + Preview + filtering/search no longer scale to real Set/tile libraries |
| “What I want” checklist | L-M after Preview | M-H | L-M | Core | H | Real users still cannot reliably express/understand common placement behavior using Preview + Quick Settings |

### Tier C — useful ideas, but separate editing responsibility

These may still be good tools, but they should not be promoted merely because the shared tile surface can launch them.

| Candidate | Effect | Implementation | Reach | Fit | Future cost | Current direction |
| --- | --- | --- | --- | --- | --- | --- |
| Proportional split | M | M | M | Separate Item Action | M | Keep separate from normal Placement; prefer a host-native split route |
| Effect Template apply / replace | H for effect-heavy workflows | H | M | Separate Item Action | H | Strong candidate for a bounded Item Action/effect executor, not a Placement Source |
| Fit playback speed to exact span | M-H | M-H | L-M | Separate Item Action | M-H | Require a concrete compatible-media workflow before promotion |
| Extend / trim to exact boundary | H in matching workflows | M-H | M | Separate Item Action | H | Destructive semantics need their own explicit target/safety design |
| Match Item duration | M | M | M | Separate Item Action | M | Keep distinct from playback-rate fitting |
| Fixed-gap multi-Item arrangement | M-H | H | L-M | Separate Item Action | H | Lower priority until a bounded Item Action responsibility is proven |
| Generic finite Action Tiles | M | M-H | M | Adjacent / scope-expanding | H | Add concrete actions only; do not build a generic action framework first |

### Tier D — do not broaden Template Placer for these

- general automation / scripting language;
- arbitrary C# expressions, DSLs, node graphs, loops or previous-step branching;
- AI/fuzzy target selection;
- general launcher behavior for external apps/scripts/macros;
- native commands such as play/pause, delete, copy/paste merely because they can be surfaced as tiles, unless a Template-Placer-specific workflow problem is first demonstrated.

### Reading the tiers

The default preference is:

1. **high effect + low/medium implementation + Core fit + low future cost**;
2. then evidence-gated Core improvements;
3. keep separate editing responsibilities separate unless a clear shared execution boundary proves worthwhile;
4. do not implement generic extensibility in advance of a concrete workflow.

This means a small Tier A improvement may outrank a much more powerful Tier B/C idea if it removes frequent friction without widening the product boundary.

## Performance polish

### Built-in Tachie Preset loading performance

Status: **DEFERRED / MEASURE FIRST**

Built-in/standard Tachie Preset loading works, but Hands-on use can feel heavier than the Template route.

Before changing architecture:

- profile the real built-in discovery/editor-construction/session-cache path;
- keep discovery scoped to distinct current Voice Characters while expression + TachiePreset is active;
- preserve cancellation/latest-wins behavior;
- preserve Template-mode performance;
- change only the measured hot path.

This is performance polish, not a reason to reopen the accepted Placement Source architecture.

## Set display profiles / workspace profiles

Status: **CANDIDATE / NEED-DRIVEN SCALING**

As Template Placer makes it practical to keep more Templates and Sets, the next cognitive-cost problem may become the number of Sets visible for a given production.

Candidate concept:

> Save and switch named **Set display configurations** for different kinds of videos or workflows.

Examples may include normal episodes, battle-heavy episodes, explanation videos, shorts, or individual series.

A profile should primarily describe the **view/composition of existing Sets**, not duplicate the Sets themselves. Candidate profile state may include:

- which Sets are visible;
- Set display/order within the profile;
- optionally the initially selected Set;
- only presentation state that materially reduces navigation cost.

Core boundary:

- one Set remains one authoritative Set with one placement rule/settings body;
- the same Set may appear in multiple profiles;
- editing a Set updates that shared Set everywhere it is referenced;
- profiles must not become copied Settings databases or a second Set hierarchy;
- ordinary Context -> Set -> tile behavior remains unchanged after a profile chooses the visible Set population.

User problem:

> Keep a large reusable Set library without forcing every production to expose every Set at once.

This is intentionally not required for the current feature-complete line. Promote it only when real use shows that Set count itself has become a meaningful recognition/navigation cost after Set-switch shortcuts are available.

## Full Settings Workspace

Status: **DEFERRED / NEED-DRIVEN AFTER QUICK-SETTINGS EVIDENCE**

The compact Settings surface remains the ordinary quick-edit surface. A later larger workspace should edit the **same Settings Draft, same schema and same protected persistence path**.

Candidate spatial structure:

```text
left navigation        center structure         right inspector
Item type          ->  Sets / tiles         ->  selected settings
```

Goals:

- make current location obvious without help text;
- make Sets/tiles easier to compare and manage at scale;
- keep add/remove actions near the collections they affect;
- reuse the same recognizable tile names/colors/shapes;
- reuse `このセットの動き`, Behavior Preview and the checklist rather than reimplementing them.

Search/filtering may later match Set name, tile alias, source name, Character and broken-reference state, but browsing must remain usable without search.

Safety:

- no second Settings schema;
- no second persistence route;
- no Timeline mutation from the Settings workspace;
- no duplicate placement engine.

## Placement Recipe extensions

Status: **COLLECTING / AFTER PREVIEW UNLESS REPRIORITIZED**

The current start/center/end alignment and Absolute Layer model are already accepted. Additional axes need separate justification.

### Composite Placement Steps

One tile/Recipe may eventually place several independent sources with different finite rules.

Every Step must read the same immutable operation-start Context. Step A output must not become Step B input.

All Steps preflight together; required failure means zero write; success commits once with native Undo.

### Stronger Target conditions

Possible finite conditions include:

- same Character across selected targets;
- explicit count per Item type;
- explicit before/after roles;
- finite mixed-type signatures.

Do not add arbitrary boolean trees, regex predicates or a generic rule language.

### Fan-out

Possible explicit scopes:

- each explicitly selected Item;
- each explicit adjacent boundary.

All results must be preflighted as one operation. Deduplication rules must be defined before implementation.

### Neighbor ambiguity result fan-out — IMPLEMENTED / RELEASE #775 GREEN

Equal-ranked Neighbor candidates are now resolved by **distinct final placement result** for normal Targeted tile placement:

- one distinct result -> immediate placement;
- several distinct results -> first execution stops / exact second execution places all;
- all alternatives preflight atomically and success is one native Undo;
- no fuzzy candidate choice.

This does **not** implement the stronger selector vocabulary below.

### Stronger Neighbor selectors

Possible finite additions:

- next/previous specified Item type;
- previous/next any Item;
- next same-Character Voice.

No fuzzy “nearest suitable thing” behavior.

## Action Tile / shortcut candidates

Status: **PARTLY PROMOTED — NATIVE UNDO/REDO COMPLETE; OTHER IDEAS REMAIN COLLECTING**

These ideas were previously parked in Draft PR #33 and are incorporated here so they do not need a long-lived stale PR.

### Native Undo / Redo — COMPLETE / ROADMAP PHASE 5B

Current accepted hypothesis:

> Native Undo / Redo may be the strongest always-available shortcut for Template Placer because it supports the normal try -> inspect -> revert -> try another tile loop.

UI direction:

- place two compact persistent Undo / Redo controls in the existing top context/Set strip;
- **do not increase the strip height**;
- prefer a fixed right-side position;
- preserve useful width for current context and Set selection;
- disabled state follows actual native Undo/Redo availability.

Boundary:

- use **YMM4 native Undo / Redo**;
- do not create a Template-Placer-only history;
- do not search for “the last Template Placer item” and delete it;
- verify the supported native command route and enabled-state behavior in real YMM4 before implementation.

### Other command candidates

Compare, but do not automatically promote:

- previous / next relevant Item selection;
- repeat last Template Placer action with strict current-context resolution;
- play / pause;
- split;
- delete;
- copy / paste.

Ordinary one-command operations should remain YMM4 command/action candidates rather than bespoke transformation engines.

### Placement quick settings — PROMOTED TO ROADMAP PHASE 5C

Investigate a compact fast path for the small placement axes that are repeatedly adjusted during normal editing.

Boundary:

- edit the existing staged Settings Draft directly;
- use the accepted Behavior Preview as immediate read-only feedback;
- do not reproduce the complete Settings surface;
- do not add a second configuration model, validation path or persistence route;
- choose the final finite control set from hands-on frequency rather than from all available placement options.

### Persistent shortcut strip / utility tiles

Status: **CANDIDATE / HIGH-REACH SURFACE**

Candidate concept:

> Add an optional always-visible one-row shortcut strip, likely along the bottom edge of the Placement surface, whose contents are explicitly configured by the user.

This is separate from the dedicated persistent YMM4-native Undo / Redo controls.

Possible properties:

- one compact strip / band;
- globally ON/OFF;
- fixed small number of slots or bounded overflow behavior;
- slots keep their configured actions while Sets and ordinary placement tiles change;
- reuse existing tile visual language where practical;
- ordinary Set/tile layout remains unchanged when the strip is OFF.

The strip is a **surface**, not permission to create a generic automation framework.

Preferred action sources:

- existing Template Placer actions that already have a safe execution route;
- bounded YMM4-native commands only when repeated hands-on use demonstrates clear value;
- future bounded Item Actions only after those actions have their own safety/execution design.

Do not add arbitrary scripts, external-app launchers, macros, or a general command-discovery system merely because the strip can host buttons.

Key UX questions before implementation:

- does a bottom strip materially reduce pointer travel / Set-switching friction;
- does it remain useful without stealing too much vertical Timeline/Tool space;
- should it disappear completely when disabled;
- whether slot order alone is enough, or named groups/separators are needed later;
- whether keyboard shortcuts for strip positions are useful or redundant with the existing position-shortcut model.

Promotion trigger:

> A stable small set of actions is repeatedly wanted regardless of the current Set/context, and keeping them in the normal Set tile population creates avoidable switching or recognition cost.

### Interaction-surface candidates

Potential surfaces that should reuse the existing tile/action execution route:

- **Quick Palette** near the pointer/editing location;
- **tile/action search** over currently exposed Template Placer actions;
- visual grouping/separators;
- finite **Action Tiles**.

Do not turn Template Placer into a general launcher for external apps, arbitrary scripts or macros.

## Item Action / Transformer candidates

Status: **COLLECTING / SEPARATE EXECUTION RESPONSIBILITY**

These ideas were previously parked in Draft PR #29 and are incorporated here.

Use an Item Action only when one ordinary YMM4 command cannot directly express the intended edit.

### Proportional split

- split the selected Item at 1/2 of its current span;
- consider finite 1/3 or 2/3 variants only if hands-on use justifies them;
- define deterministic odd-frame rounding;
- prefer YMM4's native split route after deriving the exact point.

### Effect Template apply

Candidates:

- append one registered Video Effect Template;
- append one registered Audio Effect Template;
- replace the selected Item's complete relevant VideoEffects/AudioEffects chain;
- possibly clear the relevant chain if later justified.

Boundary:

- Effect application is **not** a Placement Source;
- keep YMM4 as owner of Effect Template bodies;
- store thin live references rather than a second effect-body database;
- use a separate bounded Item Action / Effect executor behind the shared tile surface.

### Fit playback speed to an exact target span

- derive playback rate from an explicit target duration/reference;
- use only supported compatible media Item types;
- prefer host-native timing/property semantics;
- do not infer a vague “good” duration.

### Extend or trim to an exact boundary

Candidates:

- set selected Item end to the next exact Item/Voice start;
- set start/end to another explicitly selected target boundary.

No fuzzy target selection. Distinguish destructive trim from safe extension.

### Match Item duration

- match one selected Item to one explicit reference Item;
- multi-selection variants such as shortest/longest remain optional future policies.

Do not mix this with playback-speed fitting: equal timeline Length and content retiming are different actions.

### Fixed-gap multi-Item arrangement

- operate only on an explicit selected Item set;
- use one finite gap;
- preflight every resulting Frame;
- commit as one native Undo operation.

Keep this lower priority than single-Item actions until the Item Action boundary is proven.

### Shared Item Action safety

Any future Item Action should keep:

- explicit target(s);
- finite action kinds;
- no general property editor or scripting layer;
- complete preflight before multi-write mutation;
- one native Undo unit;
- failure before commit = zero mutation;
- host-native operations where they preserve semantics.

Normal Placement remains add-only even if Placement and Item Actions share a tile surface.

## Deferred / out of scope

### General automation language

Do not add:

- arbitrary C# expressions;
- scripting DSL;
- node graph;
- loops;
- previous-Step conditional branching;
- AI-selected ambiguous targets.

### Item-type custom ordering

Status: **DEFERRED**

Potentially useful, but lower frequency than the current roadmap work.

## Completed references

Completed work belongs in its accepted authority/history rather than occupying the active Backlog:

- Portable Settings -> `CURRENT_ARCHITECTURE.md` / `PRODUCT_ROADMAP.md`;
- Placement Source unification -> `PLACEMENT_SOURCE_UNIFICATION_*.md`;
- bounded Placement Rule completion -> `PLACEMENT_RULE_COMPLETION_*.md`;
- Compact Settings friction pass -> `COMPACT_SETTINGS_FRICTION_*.md`;
- Baseline Simplification -> `BASELINE_SIMPLIFICATION_*.md`.

Historical PRs/runs remain evidence; this file only needs the still-open product questions.

## Backlog discipline

When adding an idea:

1. state the user problem first;
2. state whether it changes UI, Context, planning axes, output multiplicity, or product boundary;
3. state the safety boundary;
4. do not create implementation tasks until related ideas have been collected/reviewed;
5. move an idea into a feature DESIGN / ACCEPTANCE / WORKPLAN only when implementation intentionally starts.

Do not use this file as a reason to broaden the current active phase automatically.
