> **Historical design note**
>
> This document preserves the v0.4 target design that led to the current implementation.
> It is no longer the primary authority for current behavior.
> Read `docs/CURRENT_ARCHITECTURE.md` first and use the active feature documents for current work.
>
> Do not rewrite this historical document merely to mirror later UI passes.

# DESIGN — TARGET v0.4

## Status

- **Implemented baseline:** v0.3.1. Native YMM4 4.55.1.1 Liteでv0.3.0のVoice一覧 / Face Template配置 / Excel / Safety / Undo/Redoを維持し、P9としてTimeline Toolの非表示→再表示とTimeline不変まで検証済み。
- **Historical target:** v0.4。
- 現在仕様の正本は `docs/CURRENT_ARCHITECTURE.md`。本書のProfile/Palette設計は履歴・背景として保持する。

## Product definition

YMM4 Template Placerは、YMM4に登録されたItem TemplateをPlugin側で使いやすく整理し、Voice・選択Item・選択範囲・Item境界などとの**意味のある配置関係**として安全に配置するPlugin。

```text
YMM4 Template
    ↓ reference only
Plugin Library
    ↓
Palette
    ↓
Placement Profile + Preset
    ↓
Placement Plan
    ↓ preflight
Timeline
```

自由なRule Engineを作ることは目的ではない。Profileは用途ごとに有限個とし、新Profileは既存Placement Coreの薄い組み合わせ＋小さなdomain ruleで実装できるものだけ採用する。

## Core operating rule

PluginはTimelineを所有しない。

```text
配置      = 新しいItemを追加する
再同期    = 既存Itemへ現在の配置関係を再適用する
削除      = YMM4標準操作でユーザーが行う
```

Plugin生成物を一括削除して再生成する旧v0.3方式はv0.4では廃止する。既存Itemを勝手に削除・移動・短縮しない。

## Safety / atomicity

全操作は可能な限り次の順で行う。

```text
Create placement requests
→ resolve Frame / Length
→ resolve Layer candidates
→ check existing occupancy
→ reserve already-planned occupancy
→ complete all PlacementPlans
→ if any required plan fails: Timeline mutation = 0
→ commit successful operation as one native Undo unit
```

Commit後にLayer再探索やcollision resolveを行わない。Previewと実結果を一致させる。

---

# 1. Template source and Plugin Library

## YMM4 is the template source of truth

Template本体は引き続き `ItemSettings.Default.Templates` 等から取得する。PluginはYMM4 Template本体を複製保存しない。

Plugin側は薄いLibraryを持つ。

```text
LibraryEntry
- Id                    // plugin-local stable id
- Ymm4TemplateRef       // live YMM4 Templateを解決するための参照
- DisplayName           // Plugin上の短い名前
- CharacterRef?         // Character Palette用。任意
```

例:

```text
YMM4:   小夜_PSD_v3_表情_どや顔_差分A
Plugin: どや
```

Character Palette内ではCharacter名をTemplate表示名へ重複させる必要はない。同じ `通常` / `笑顔` / `どや` というDisplayNameを複数Characterで利用可能。

## Library and Palette are separate

LibraryEntryは複数Paletteから参照できる。

```text
Library: 白Flash
   ├─ 明るいPalette
   ├─ 戦闘Palette
   └─ Transition Palette
```

PaletteEntryは原則として次だけを持つ。

```text
PaletteEntry
- LibraryEntryId
- SortOrder
- SuggestedProfileId? / SuggestedPresetId?   // optional
```

Template本体をPaletteへ複製しない。

## Broken reference

YMM4側でTemplateを削除・変更し、LibraryEntryが解決できない場合は自動推測しない。

```text
⚠ どや
元のYMM4 Templateが見つかりません。
[再リンク] [登録解除]
```

同名候補が複数あっても自動接続しない。壊れたEntryも勝手に削除しない。

---

# 2. Palettes

## Character Palette

Characterに紐づいたTemplate棚。

```text
小夜
[通常] [笑顔] [どや] [困り]
[汗] [赤面] [青ざめ] [手]
```

Pluginは `基本表情 / Overlay / 目 / 手` などの意味分類を強制しない。PSD立ち絵Pluginの用途では同じ時間帯にLayerを変えて複数のTachieFaceItemを置けるため、Paletteから自由に重ねられることを優先する。

## Style Palette

編集者が現在使いたい演出によって手動切替する。

例:

```text
明るい: [白Flash] [キラキラ] [黄色強調] [Light Wipe]
暗い:   [暗転] [赤Flash] [ノイズ] [警告]
戦闘:   [被弾] [爆発] [集中線] [高速Wipe]
```

「シリーズ」はv0.4 Coreの固定概念にしない。同じ動画内でも演出Styleは切り替わる。

## Character context auto-switch

Character関連ItemをTimelineで**単体選択した時だけ**Character Paletteを一時自動切替する。

必須対象:

```text
VoiceItem
TachieFaceItem
```

TachieItem等もCharacterを安全に取得できるなら対応してよいが、初期Acceptanceでは必須にしない。

挙動:

```text
Manual palette = ミコ
↓ 小夜Voiceを単体選択
Context palette = 小夜
↓ Character contextが終了
Manual palette = ミコ に戻る
```

内部では `ManualCharacterPalette` と `ContextCharacterPalette` を分離する。複数Item選択時は自動切替しない。

---

# 3. Quick Drop

Palette Entryをダブルクリックすると、全Palette共通で**現在再生位置へ即配置**する。

```text
Frame  = Timeline.CurrentFrame
Length = Templateに保存されているintrinsic Length
```

TargetのFrame / Lengthには合わせない。Targetとの関係で置きたい場合は「表情一覧」または「選択配置」を使う。

Quick DropはTargetとのAssociationを作らない。

```text
Target IDなし
Source IDなし
再同期対象外
```

PSD立ち絵の重ね表情もQuick Dropで自由に追加できる。

配置後は軽いFeedbackを出す。

```text
「手」を Frame 1520 / Layer 23 に配置しました。
```

---

# 4. Character Quick Drop layer policy

Character PaletteではQuick Drop時に3状態を選べる。

```text
○ 基準
○ 前面（大きいLayer番号）
○ 背面（小さいLayer番号）
```

選択状態はユーザーが変更するまで維持する。

## Base

Preset / Paletteの通常Layer Policyを使う。

例:

```text
Band 10..19
Preferred 15
```

## Front

Quick Drop予定区間と時間的に重なる、同CharacterのCharacter関連Itemを調べる。

対象候補:

```text
VoiceItem
TachieFaceItem
TachieItem（Characterが安全に取得できる場合）
```

第一候補:

```text
max(CharacterRelated.Layer) + 1
```

使用不能ならさらに大きい番号へ探索する。

例:

```text
Voice       L10
立ち絵      L15
笑顔        L18
身体Effect  L22
↓ Front Quick Drop
手          L23
```

## Back

第一候補:

```text
min(CharacterRelated.Layer) - 1
```

使用不能ならさらに小さい番号へ探索する。

## No character-related item

Front / Backでも同Character Itemが予定区間に存在しない場合はBaseへfallbackする。

## Collision range

開始Frameだけでなく、Quick Drop Itemの予定Length全体で衝突判定する。Layer上限/下限まで空きがなければ失敗し、反対方向へ回り込まない。

---

# 5. Placement Profiles

Profileは有限のsemantic strategy。UIへresolver primitiveを直接公開しない。

## A. Character Expression

```text
VoiceItem → Face Template
```

Duration preset:

```text
Voiceと同じ
次の同Character Voiceまで
```

Next Same Character:

```text
BaseEnd = CurrentVoice.End
if NextSameCharacter.Start >= BaseEnd
   and NextSameCharacter.Start - BaseEnd <= MaxGap:
    End = NextSameCharacter.Start + EndOffset
else:
    End = BaseEnd + EndOffset
```

次Voiceが現在Voiceへ重なっていても現在Voiceより短くしない。

代表設定:

```text
MaxGap
StartOffset
EndOffset
LayerPolicy
```

## B. Target Companion

Targetと同じSpan、またはTail付き。

```text
Start = Target.Start + HeadOffset
End   = Target.End + Tail
```

枠、装飾、背景、Highlight、reaction hold等。

## C. Point Emphasis

```text
Anchor = Start | 25% | 50% | 75% | End
Start  = Anchor + Offset
Length = FixedDuration
```

Start Emphasis / Mid Reaction / Exit Accent / Pre-rollを同じProfileへまとめる。任意式は使わない。

## D. Selection Range

複数選択Item全体を覆う。

```text
Start = min(selected.Start) - HeadPadding
End   = max(selected.End) + TailPadding
```

## E. Boundary

隣接する2 Itemの境界を基準にする。

```text
Exactly 2 Items
abs(A.End - B.Start) <= Tolerance
Boundary = agreed cut position
```

条件不成立なら近い境界を推測しない。

---

# 6. Placement Presets

同じProfileへ複数Presetを保存可能。

例:

```text
Character Expression
A: Voiceと同じ / Template Layer
B: Next Same Character / MaxGap 90 / Band 10..19 / Preferred 15
C: Next Same Character / MaxGap 45 / Band 20..29 / Preferred 25
```

Profileコードは増えない。Presetはparameterだけ。

再同期には**現在のPreset**を使う。過去Preset Snapshotは保存しない。

---

# 7. Placement is add-only

v0.4では新規Placementは既存Itemを自動削除しない。

```text
Place  = Add
Resync = Update existing related item geometry/relation
Delete = YMM4 standard operation
```

Target-based placementについて、同じVoice / Profileにすでに関連Itemが存在することを明確に特定できる場合は、勝手に置換・複製せず案内して停止してよい。

Quick DropはAssociationを持たないため複数配置を制限しない。

---

# 8. Lightweight association for resync

Final Cut型の常時Connected Clipは作らない。再同期用の**弱い目印**だけ持つ。

Target Voiceに関連付き配置が必要になった時だけRemarkへPlugin tagを追加する。

```text
CWT_TPL:V=1042
```

生成Item:

```text
CWT_TPL:S=1042;P=expression
```

既存Remark本文は保持し、Plugin tag部分だけ追加・更新する。

## ID

Character別ではなくPlugin全体の単純連番でよい。

```text
1041 小夜
1042 小夜
1043 ミコ
```

ID自体に意味を持たせない。`NextAssociationId` 程度の設定でよい。

## Resync lookup

```text
Source ID一致
→ Character一致で絞る
→ exactly 1 target: use
→ 0: skip
→ 2以上: skip
```

Frame / Serif / Layer / 順番などで推測しない。

## Resync scope

- 関連Itemを選択: そのItemだけ
- VoiceItemを選択: そのVoice IDをSourceとする関連Item
- Scene全体一括再同期: v0.4では不要

## Best effort

再同期は部分成功を許容する。

```text
10件対象
8件更新
2件Target不明でSkip
```

特定不能Itemは変更しない。変更できたItem群はYMM4 Undo 1回で戻せる一操作にまとめる。

Quick Drop ItemはAssociationなしなので再同期しない。

---

# 9. UI

主画面:

```text
[表情一覧] [選択配置] [パレット] [Library]
```

## Expression list

v0.3のVoice一覧を維持する。

```text
Expression Preset [B ▼]
Character | Serif | Frame | Length | Template
小夜 | ... | 100 | 80 | 笑顔
小夜 | ... | 220 | 60 | どや
ミコ | ... | 310 | 70 | 困り
[配置]
[Excelへ出力] [Excelから読み込み]
```

Template候補はYMM4全Templateではなく、そのCharacter Paletteへ登録されたLibraryEntryを優先して表示する。

## Selection placement

Selection contextで意味のあるProfileだけ表示する。

```text
1 Item          → Target Companion / Point Emphasis
multiple Items  → Selection Range
adjacent 2      → Boundary
```

使用不能Profileを大量にDisable表示しない。

## Palette

Character PaletteはContext自動切替、Style Paletteは手動切替。

Palette EntryダブルクリックはQuick Drop。

## Library

YMM4に登録済みTemplateからLibraryへ追加し、最低限 `DisplayName` と必要なら `Character` を設定する。高度なタグDB、自動分類、独自Template editorは作らない。

---

# 10. Excel

Excelは引き続きVoice Expression Assignment専用のサブ経路。

```text
Voice snapshot
→ Excel
→ Template選択
→ Import
→ current Character Expression Presetで配置
```

汎用Placement Editorへしない。ImportだけではTimelineを変更しない。

---

# 11. Minimal implementation structure

過剰な抽象化を避ける。

```text
Core
├─ PlacementEngine
├─ PlacementPlanner
├─ PlacementPlan
├─ PlacementMath
├─ LayerPlanner
└─ AssociationTag

Profiles
├─ CharacterExpressionProfile
├─ TargetCompanionProfile
├─ PointEmphasisProfile
├─ SelectionRangeProfile
└─ BoundaryProfile

Library
├─ TemplateLibrary
├─ LibraryEntry
└─ TemplateResolver

Palettes
├─ CharacterPalette
├─ StylePalette
└─ PaletteEntry

Settings
├─ Profile Presets
└─ NextAssociationId
```

`ITimeResolver`群、Rule Graph、DSL、Generic Condition Treeは作らない。必要になってから抽象化する。

---

# 12. v0.4 CURRENT scope

## Implement

- Plugin Library: YMM4 Template参照 / DisplayName / optional Character / re-link
- LibraryとPaletteの分離、多対多利用
- Character Palette / Style Palette
- VoiceItem / TachieFaceItem単体選択によるCharacter Palette一時切替と復帰
- Palette double-click Quick Drop
- Character Quick Drop: Base / Front / Back
- Placement Profiles: Character Expression / Target Companion / Point Emphasis / Selection Range / Boundary
- 複数Placement Preset
- Layer Band / deterministic layer search
- Existing + planned collision preflight
- Add-only Placement
- Voiceへの軽いassociation ID
- Best-effort manual Resync
- Existing Voice list / Excel / native Undo/Redo regression

## Explicit non-goals

- Series Set
- Plugin内へのYMM4 Template本体複製
- Generic Rule Engine / DSL / Node Editor / arbitrary predicate
- Plugin生成物の自動全削除 / 全削除→再配置
- 常時同期 / Voice移動event監視
- Copy/Paste ID自動修復
- Scene全体一括再同期
- 過去Preset Snapshot
- Persistent Connected Clip
- Multi-item Template一般化
- Protected Intro/Outro retiming
- Parent / Follow / Track Matte
- Beat detection / 音声解析 / word timing
- AI API直接連携

---

# 13. Acceptance

v0.4完了には少なくとも以下をnative Windows上の実YMM4で確認する。

1. v0.3.1のVoice一覧 / Excel / Safety / native Undo/Redo主要Golden PathとTool hide/reopen(P9)が回帰しない。
2. Library上のDisplayNameをYMM4 Template名と独立して設定できる。
3. 同じLibraryEntryを複数Paletteで利用できる。
4. YMM4 Template参照切れを自動推測せず、再リンク/登録解除へ案内できる。
5. VoiceItem / TachieFaceItem単体選択でCharacter Paletteが一時自動切替し、Context終了後に以前の手動Paletteへ戻る。
6. Quick DropはCurrentFrameへTemplate intrinsic Lengthで配置する。
7. Quick DropはAssociationを作らない。
8. Character Quick DropのFrontは同Character関連Itemより大きいLayer、Backは小さいLayerへ配置する。
9. Front / Back探索は予定Itemの全Lengthで衝突を確認する。
10. 同時間帯に複数TachieFaceItemを別Layerへ重ねて配置できる。
11. Character ExpressionのNext Same Character / MaxGapが期待通り動き、重複Voiceで現在Voiceより短縮しない。
12. Layer BandはExisting + planned occupancyを考慮し、結果をCommit前に確定する。
13. Target-based Voice placementに軽いassociation IDを付与できる。
14. ID + Characterで一意なTargetだけ再同期し、特定不能Itemは変更しない。
15. Resyncは現在Presetを使う。
16. 部分成功したResyncの変更分をUndo 1回で戻せる。
17. Pluginは既存Itemを自動削除しない。
18. Invalid input / no free Layer / broken Template referenceではTimelineを不必要に変更しない。

## Product boundary

v0.4の価値は、YMM4 Templateを第二の独自Template DBへ移すことではなく、**YMM4 Templateを短い名前とPaletteで整理し、Character Contextや演出Styleから素早く呼び出し、意味のあるPlacement Profileで安全に置き、必要なら軽く揃え直せること**にある。
