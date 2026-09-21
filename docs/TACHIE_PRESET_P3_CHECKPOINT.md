# Tachie Preset P3 checkpoint

Status: **P3 NATIVE GREEN / P4-P5 NEXT**

This checkpoint supersedes the stale P3-next status in the older feature handoff. The frozen design and acceptance boundaries are unchanged.

## Exact evidence

- Product source: `16608d6e2621b8bedea76d04e3498fa6dcc8c2c0`
- Checkout commit: `64a5f19a472d733ba3eebd05bac028ef438833c2`
- Checkout tree: `ebe63cc9c88b1e174c7a95b25a7d5adac671c4ef`
- Checkpoint run #416: `35630553092`
- Native job: `106435424311`
- Artifact: `10654059148` (`native-yymm4-checkpoint`)
- Downloaded ZIP SHA256: `9d02df965807e407f8c17bc064113c4e8c4317f9dca5c51db666fdc6aff78f4c`
- Host: YMM4 4.55.1.1 Lite / Windows / .NET 10
- Markers: `TACHIE_PRESET_CAPABILITY_P3=PASS`, `TACHIE_PRESET_GUARDS_P3=PASS`

Artifact bytes were downloaded and their SHA256 verified. Both JSON manifests match the exact source, checkout tree and run. Capability manifest: 29 checks. Guard manifest: 10 checks. Both are PASS. Full Checkpoint regression and evidence guards passed; distribution/proof compilation had zero warnings and errors. This is not Release or owner Hands-on acceptance.

## Implemented scope

`TachiePresetCapabilityCoordinator`, `TachiePresetDiscovery`, `TachiePresetEditorSession` and `TachiePresetPublicState` implement bounded UI-affine discovery and immutable results.

Proved routes include synthetic direct/legacy/modern editor fixtures and the real built-in Animation/PSD plugins. Both built-ins expose their two named fixture candidates as Strong and exclude Custom. Current Character/config/default face and Timeline remain unchanged.

The native cases cover distinct-Character work for 1,000 Voices, session cache invalidation, latest-wins serialization, cancellation/disposal while bound, cleanup failures, shared-face rejection, duplicate identities, unstable/cyclic/throwing/oversized state, and off-thread rejection.

## Remaining integration

P4 connects immutable results to the existing expression capture/preparation/publication pipeline. P5 extends the existing row choice model without fake Templates, a second DataGrid, or a second freshness engine.

Candidate inspection must remain explicitly non-mutating until the exact source-union association and mutation/trial seams (P6-P8) are implemented and proved. Do not present an unconnected candidate selection as a successful placement.
