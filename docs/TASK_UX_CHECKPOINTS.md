# v0.4 Task UX checkpoints

Regression source: `c3fc36837508b59d09026a74b5799141eae00a7a`; baseline native run `34878781993`, 384 assertions. UX work is on `feature/v0.4-task-ux`, PR #8 into the Candidate branch. Do not merge main.

## WUX1 — Direct Template addition (verified)

Finding: first use required learning Library registration and a second Palette membership step. The task is simply to collect a frequently used YMM4 Template and place it.

Change: the Palette is selected on first opening. An always-visible `＋ テンプレートを追加` action opens a focused, same-tool source picker. A single save creates a strict reference plus membership, or reuses exactly one existing registration. An empty installation gets its first Palette with the first successful addition. Source Character is read automatically. The destination is frozen when the task opens so a Timeline selection cannot silently redirect the operation.

Recurring costs removed: separate management navigation, remembering the Library/Palette dependency, duplicate naming for an existing source, initial empty-Palette setup.

Trade-offs: the simple initial Palette name can be changed/organized later; strict ambiguous sources and multiple registrations remain blocking errors. Existing aliases are shared, so this task does not rename a reused Library entry. Cancel abandons only the transient add draft. No Template bodies are stored and the placement engines are unchanged.

Native verification in actual YMM4 4.55.1.1 Lite:
- Source: `7c25e675ed2532dfe7a6b77a77390f226230b8c4`
- Run: `34884431457`; job: `104111424440`; evidence artifact: `10364173110`
- **414 assertions PASS**, including WUX1 and the complete pre-existing ladder; all 18 integrated acceptance requirements PASS.
- Release / proof: **0 warnings / 0 errors**; Open XML, exact release DLL native smoke, package verification PASS.
- Proved via actual bound WPF buttons and native Timeline: empty setup -> source selection -> short name -> add -> Quick Drop -> native Undo/Redo; exact registration reuse across two Palettes; duplicate plugin registrations, ambiguous live sources and removed source rejected without persistence/Timeline mutation or unnecessary draft loss.

Screenshot review: `ux-add-template-normal.png` and `ux-add-template-narrow.png` were actually inspected. At 360 x 360, source/name inputs and Add/Back actions remain reachable without scrolling the whole task. The fixture's programmatic source selection needs explicit scroll-into-view for a clearer later evidence capture. Old global success text and global Resync remain visible; these are known WUX4/WUX6 work, not declared solved by WUX1. The old mode selector and primary management tab likewise remain until their own units.

Boundary: synthetic native fixtures and WPF AutomationPeer commands prove product behavior, not physical mouse interaction, every asset/theme/DPI or installer UI. This is an intermediate checkpoint, not the final UX Candidate.
