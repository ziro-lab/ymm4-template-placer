# v0.4.2 final Hands-on polish

Status: **FROZEN FOR IMPLEMENTATION**

Base: performance Release candidate `286a8db58ad5ce03fcff731ce3d0aa96fcc7b995`.

This pass contains only the two owner Hands-on findings below.

## H1 — Set deletion discoverability

Observed:

- Item-owned / Generic Set deletion exists only inside the collapsed `セットの管理` expander.
- The owner had to ask how an obsolete Item Set is deleted.

Change:

- keep the existing protected delete command and confirmation;
- add a directly visible `削除` affordance beside the Set picker/create control;
- apply to both Item-owned and Generic Set contexts through the same command;
- retain the existing delete button inside `セットの管理` for management consistency.

Safety:

- delete only the selected Set membership/configuration;
- never delete source YMM4 templates, Library source identity, or Timeline items;
- preserve the existing confirmation copy;
- automatic protected settings commit remains the only persistence path.

Acceptance:

- selected Set exposes visible direct delete without expanding management;
- no Set selected => delete cannot execute;
- Item-owned and Generic Sets both use the same authoritative command;
- cancel confirmation is zero-write;
- confirm removes exactly the selected Set and leaves source template / Timeline unchanged.

## H2 — stale wheel target after inner Settings wheel use

Observed:

- with the Settings source/template section expanded, wheel scrolling can intermittently stop even when the pointer visually sits outside the inner control;
- moving the mouse vertically makes wheel scrolling work again;
- this is especially noticeable after using the wheel over inner settings controls.

Current cause candidate:

- `NestedWheelRouting.Wheel()` routes from `MouseWheelEventArgs.OriginalSource`;
- WPF can retain a stale mouse-over/directly-over element when layout/content moves under a stationary pointer;
- the existing ComboBox/RangeBase ownership guard can therefore reject parent scrolling using an element that is no longer actually under the pointer.

Change:

- on every wheel event, perform a fresh hit-test at the current pointer coordinates inside the root Settings ScrollViewer;
- use that current hit as the routing source;
- only fall back to the event source when a valid current in-root hit cannot be obtained;
- preserve ComboBox / RangeBase wheel ownership when they are actually under the pointer;
- preserve nested ScrollViewer direction-aware bubbling;
- no global mouse hook, polling, timer, or mouse capture.

Acceptance:

- stale historical source cannot block the root when current hit is ordinary Settings content;
- a ComboBox/RangeBase actually under the pointer still owns its wheel;
- nested child ScrollViewer scrolls while it has room, parent takes over at its boundary;
- moving the pointer is not required to recover wheel routing;
- unrelated tabs/input routes are unchanged.

## Out of scope

Do not change:

- expression performance architecture;
- Set ownership model;
- placement semantics;
- expression preset PR #19;
- shortcut behavior;
- row-height drag;
- quick-settings popup behavior;
- settings schema.

## Validation

1. focused native proof for both H1/H2;
2. full Checkpoint regression including expression performance proof;
3. Release exact DLL smoke/package/provenance;
4. owner Hands-on.
