# Tachie Preset source — design

Status: FROZEN / P0 + P1 + P2 GREEN / P3 READY

This feature adds Tachie Preset content as an experimental second source in the existing high-throughput expression workspace.

## Goals

- use presets exposed by the current Character's tachie plugin without a per-plugin allowlist as the primary route;
- keep Template mode unchanged and default;
- reuse the accepted expression row/performance/navigation/Undo architecture;
- support exact managed replacement between Template and Tachie-Preset sources;
- contain incompatible plugins locally.

## Non-goals

- universal compatibility with every tachie plugin;
- reverse-engineering private preset storage;
- persistent learned compatibility database;
- moving preset work into normal placement Sets;
- Excel support for Tachie Presets in this round;
- a new placement engine or custom Undo;
- fuzzy managed-association repair;
- eager capability scanning outside the active Tachie Preset source mode.

## D1 — source mode

Add one session-local source mode with two values: Template and TachiePreset.

The last successfully selected source mode is persisted in the existing protected product settings and restored by the next Tool instance. Existing settings that predate this field default to Template.

Switching mode:

- closes any open expression trial;
- cancels source-specific in-flight work;
- writes no Timeline content;
- persists only the selected source-mode preference after the switch is accepted;
- entering TachiePreset is blocked while protected pending Template/Excel work exists;
- returning Template is always allowed after safe cancellation.

## D2 — one row engine

There remains one Rows collection, one Voice identity model and one publication coordinator.

Do not create PresetRows, a second DataGrid or a second freshness system.

The historical internal TemplateChoice may be minimally extended with an optional Tachie-Preset candidate descriptor plus source-kind/HasCandidate state. Do not create fake FaceTemplate objects.

Template-only consumers such as Workbook remain explicitly guarded to Template mode.

## D3 — capability coordinator

Preset discovery runs only while:

    activeTask == expression
    and sourceMode == TachiePreset

Discovery scope is distinct current Voice Characters, not every Voice and not every loaded plugin.

For each Character:

1. resolve the exact current tachie plugin/configuration;
2. create fresh CharacterParameter/FaceParameter objects as required by the proved route;
3. inspect only public properties/attributes on those local objects;
4. discover direct-named or YMM4 property-editor candidates;
5. dry-run on the fresh FaceParameter;
6. emit immutable descriptors;
7. tear down temporary bindings/controls.

WPF/editor/plugin-created objects stay UI-thread-affine. The coordinator yields between bounded operations and supports generation/cancellation. No mutable host/plugin object enters background preparation.

## D4 — immutable candidate descriptor

A row candidate stores bounded identity only, never live WPF/plugin objects.

Descriptor fields include at least:

- Character/config identity;
- plugin runtime type;
- FaceParameter runtime type;
- plugin module MVID;
- route kind;
- property/editor identity;
- candidate identity/label;
- confidence.

Duplicate or ambiguous candidate identities are rejected for that Character.

## D5 — session cache

Cache capability results by a fingerprint containing host + plugin + FaceParameter + module MVID + bounded Character/config identity.

Cache is session-local. Invalidate when the fingerprint changes or the Character/plugin cannot be re-resolved.

A failure for one Character is local and must not disable Template mode or other Characters.

## D6 — performance integration

Once capability discovery produces immutable descriptors, the existing expression preparation pipeline may build row choices off-thread from those descriptors.

The existing latest-wins generation remains publication authority.

Preset discovery itself is not moved to Task.Run and never dereferences mutable host/plugin state from background work.

## D7 — placement rule

Template mode keeps Set-owned placement relation semantics unchanged.

Tachie Preset mode uses the already-persisted ExpressionPreset model as its placement rule for time/span, while layer placement follows the same mental model as ordinary Voice-targeted Template placement.

Layer modes are finite:

1. **Voice Setと同じ** — default. Resolve the first applicable expression Set for that Voice/Character in authoritative Set order and reuse its RelativeLayerPolicy (up/down, offset, bounded same-direction collision search).
2. **Voiceの上／下を指定** — override with an explicit RelativeLayerPolicy owned by the ExpressionPreset.
3. **レイヤー番号を指定** — override with an explicit absolute LayerPolicy and occupied-layer behavior: do not place, search up, search down, or retained legacy bounded search.

If no applicable expression Set exists, the default mode uses the normal RelativeLayerPolicy default (Voiceの上へ1レイヤー, range 0-99). It does not fall back to a generated TachieFaceItem constructor Layer.

This keeps:

- CharacterExpressionProfile.Span for time geometry;
- the existing Voice-targeted relative layer convention;
- LayerPlanner for explicit absolute layer placement;
- current validation and protected settings persistence.

Historical ExpressionPreset settings are migrated without schema broadening: the untouched historical default becomes Voice-Set inheritance; customized historical numeric layer settings are preserved as an absolute override.

In Tachie Preset UI this is called 配置ルール so it is not confused with the Tachie content preset.

No settings schema version bump is required for this round.

## D8 — application

At user selection, re-resolve everything from the immutable candidate descriptor.

Never reuse the dry-run FaceParameter.

Application creates a fresh FaceParameter, applies the candidate, validates it, creates a fresh TachieFaceItem for the current Character, attaches the result, applies the current placement rule and fully preflights the mutation.

A stale candidate or fingerprint fails before Timeline mutation.

## D9 — managed source union

Keep the existing Voice serial and PlacementEngine marker.

Source-specific descriptors:

- Template source -> existing IntentAssociationTag unchanged;
- Tachie Preset source -> new versioned TachiePresetAssociationTag.

Generalize only the common managed-expression reader/index so it returns one source kind plus exact members.

Rules:

- exactly one source kind per managed group;
- mixed/missing/duplicate/copy ambiguity fails closed;
- manual/unassociated expressions are never adopted;
- cross-source replacement is allowed only from an exact valid managed group.

## D10 — state fingerprint

P0 approved a deterministic bounded public-state FaceParameter fingerprint on exact YMM4 4.55.1.1 Lite for:

- built-in Animation;
- built-in PSD;
- the synthetic expanded-state modern `ItemProperty[]` editor fixture.

For proved bounded candidate routes, store the applied state fingerprint in the Tachie Preset association and validate it before destructive replacement/removal.

The hash must be derived from a canonical bounded public-state projection, never `ToString()`, object identity or private editor state. Treat this as a first-implementation safety check, not proof that arbitrary third-party plugins expose every semantically relevant value through public properties. If a plugin cannot produce a trustworthy bounded state projection, do not promote that route to Strong/manual-edit-safe behavior.

## D11 — source switching and current state

A valid current managed association from the other source is not corruption.

Rows distinguish:

- no managed expression;
- same-source current candidate;
- valid current managed expression from the other source;
- same-source candidate disappeared;
- invalid/ambiguous managed association.

Mode switch alone does not alter current expression content.

## D12 — Excel

Excel stays Template-only.

In Tachie Preset mode:

- Export/Import are disabled or hidden with a clear explanation;
- entering TachiePreset is blocked if protected pending Workbook/Template assignments exist;
- no preset identity is written into the existing Workbook schema.

## D13 — reflection boundary

Product reflection is bounded to public metadata on the exact resolved Character/plugin-created parameter/editor objects.

Do not:

- scan all loaded assemblies for candidate plugin types;
- read private fields;
- bind internal ViewModels by name;
- use UI Automation or fake clicks.

Known public YMM4 editor contracts should be referenced directly where product compilation permits.

## D14 — responsiveness

Preset mode may show a nonmodal character-level state such as:

    立ち絵プリセットを確認中… 2/4キャラクター

Other Tool tabs remain usable. Leaving expression/preset mode cancels remaining discovery. Unchanged session fingerprints reuse the cache.

## Stop conditions

Stop implementation and return to design/Lab if any of these become necessary:

- mutable YMM4/plugin objects must be read off UI thread;
- a global input hook or UI Automation is needed;
- private editor state is required;
- per-plugin allowlists become the primary route;
- source switching requires a second row/freshness engine;
- preset mode requires changing Template Set semantics;
- safe cross-source replacement cannot be proved with exact managed identity;
- capability probing mutates Timeline or existing items.
