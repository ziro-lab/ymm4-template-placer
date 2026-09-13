# DESIGN — CURRENT v0.2

## Product definition

YMM4上のTarget Itemを読み、条件に対応する登録済みYMM4 Item Templateを所定の配置規則でTimelineへ置く支援Plugin。

CURRENTは **VoiceItem → TachieFaceItem Template** のみ実装する。

## Primary flow: YMM4 only

```text
Current Scene
-> VoiceItem list
-> Character-compatible Face Template dropdown
-> Assignment
-> Placement Engine
-> Timeline
```

UI最小構成:

```text
[更新] [配置]
[Excelへ出力] [Excelから読み込み]
```

各Voice行には Character / Serif / Frame / Length / Template dropdown を表示する。Template未選択は「配置しない」。

## Secondary flow: Excel / AI bridge

```text
YMM4
-> .xlsx snapshot
-> human / AI edits Template column
-> import
-> same Assignment model
-> same Placement Engine
```

Excelは同期対象ではない。Export後にYMM4側を変更した場合は再Exportする。

## Template catalog

Source of truth:

```csharp
ItemSettings.Default.Templates
```

CURRENTで候補とするTemplate:

```text
Items.Count == 1
AND Items[0] is TachieFaceItem
AND FaceItem.Character == VoiceItem.Character
```

Pluginは表情データベースやTemplate管理UIを持たない。新しい表情はYMM4でItem Templateとして登録する。

## Assignment model

概念:

```text
Assignment
- Target VoiceItem
- Character
- Frame
- Length
- Serif
- Selected Template reference
```

YMM4 UIとExcel ImportはどちらもAssignmentを生成し、Placement Engineへ渡す。

## Placement

選択Template内のTachieFaceItemを独立Cloneし、CURRENTでは主に以下をTarget Voiceへ合わせる。

```text
Frame  = Voice.Frame
Length = Voice.Length
Remark = "CWT_TPL:face"
```

LayerはまずYMM4 Template側の設定を尊重する。必要性が確認されるまでCharacter別Layer設定やVoice-relative offset policyは追加しない。

## Re-placement

配置前に入力を完全検証する。

```text
Validate all assignments
-> build new items
-> remove current-scope items whose Remark == CWT_TPL:face
-> add new items
-> record YMM4 Undo
```

差分同期は行わない。手動配置したFaceItemは触らない。

## Excel workbook

Main sheet:

| No | Character | Frame | Length | Serif | Template |
|---:|---|---:|---:|---|---|

Template列はCharacter別dropdown。

Hidden catalog example:

```text
TemplateId | Character | TemplateName
T001       | 小夜      | 小夜/通常
T002       | 小夜      | 小夜/笑顔
```

TemplateIdはWorkbook内だけで有効な簡易ID。永続IDやhash trackingは不要。

Metadata:

```text
schema_version = 1
export_time
scene_name
template catalog
voice snapshot
```

Import時に参照Templateが現在のYMM4に存在しない場合はTimelineを変更せず停止し、再Exportを案内する。

## Internal responsibilities

```text
YMM4 Adapter
- current Timeline
- VoiceItem enumeration
- item add/remove
- Undo/Redo integration

Template Catalog
- ItemSettings.Default.Templates
- Face Template filtering
- Character candidate lists

Assignment Service
- YMM4 UI selections
- Excel import

Placement Engine
- clone
- Frame/Length transform
- marker
- Timeline placement

Workbook Bridge
- .xlsx export/import
- dropdowns
- hidden catalog
- validation
```

These are responsibility boundaries, not permission to build a framework.

## Acceptance

1. Current Scene VoiceItems appear in the plugin list.
2. Each Voice row shows only Character-compatible Face Templates.
3. Template can be selected and placed entirely inside YMM4.
4. Placed FaceItem Frame/Length match the VoiceItem.
5. Plugin-created items have `CWT_TPL:face`.
6. Manual FaceItems are preserved.
7. Re-placement does not multiply plugin-created items.
8. One placement operation can be reverted with YMM4 Undo.
9. Voice snapshot and template choices export to `.xlsx`.
10. Excel has Character-specific Template dropdowns.
11. Excel assignments import through the same Placement Engine.
12. Invalid workbook or missing Template causes no Timeline mutation.
13. Golden Paths work on real YMM4 4.55.1.1 Lite.

## Explicit non-goals

- plugin-owned expression database
- persistent Voice IDs
- bidirectional synchronization
- conflict resolution
- Template hashes
- direct AI API integration
- generic Rule Editor / Condition DSL
- multi-scene batch
- PSD-specific behavior
- preview-image correctness testing
