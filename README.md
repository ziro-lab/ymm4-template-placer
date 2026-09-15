# YMM4 Template Placer v0.4.1 Candidate

YukkuriMovieMaker4で、登録済みテンプレートを「よく使うパレット」「音声に対応する表情」「選択アイテムとの関係配置」として素早く再利用するためのTool Pluginです。

現在のv0.4.1 UX Workflow Candidateは、v0.4.0 Task UX Candidateを土台に、実利用で残っていた反復負担と途中作業消失を改善しています。

## v0.4.1の主な改善

- **Tool close/reopenの途中作業保持** — 同一YMM4セッションで厳密に同一と確認できるdraft・表情選択・追加途中を復元。変更済み対象は推測復元しません。
- **パレットへ複数Templateを一括追加** — 1件/N件を同じBatch Preflight/Commitで処理。1件でも曖昧・欠損なら全件zero-write。
- **パレットを自分順へ並び替え** — 専用Drag Handleまたは↑↓。既存`LibraryEntryIds`順序をそのまま正本とし、別Paletteには影響しません。
- **破壊操作の意味を明示** — Palette削除とLibrary登録解除は、件数・影響範囲・削除しないものを確認してから実行。Cancelはzero-write。
- **Intentを言葉で分離** — `キャンセル` / `戻る` / `シーン更新` / `一覧更新` を作用に合わせて区別。
- **同名項目の識別** — 普段は短い名前のまま、衝突時だけCharacterや元Template名を補助表示。

## 既存の主要機能

- Palette-firstの直接Template追加
- Character連動 / 手動Paletteを1つのPickerで扱う
- Quick Drop（再生位置へ配置）
- Base / Front / Backの全時間幅Layer計画
- 表情一覧の音声単位割り当て
- Expression multi-presets / Next Same Character / MaxGap / offsets
- Selection-scoped Association / Resync
- Target Companion / Point Emphasis / Selection Range / Boundary
- Excel Bridge
- Native Undo / Redo
- strict TemplateLocator / no fuzzy recovery
- add-only PlacementPlan / Preflight

## 安全性の方針

Template Placerは既存Timelineアイテムを自動削除・置換・短縮しません。

配置は、

1. strictな参照解決
2. 配置予定の作成
3. full-span collision / Layer予約の事前検証
4. 全件が成立した場合だけ一括commit

の順で行います。

YMM4 Template本体はSource of Truthとして外部参照し、プラグイン設定へTemplate bodyを複製保存しません。

曖昧・欠損・Character不一致時は、似たものを推測して続行せず停止します。

## 検証

基準YMM4: **4.55.1.1 Lite**

v0.4.1 Candidateは以下を同じNative laneで要求します。

- P1-P9
- W3-W12 / V04
- Original v0.4 Acceptance 18/18
- Task UX WUX1-WUX7 / 12 requirements
- UX Workflow WUX8-WUX13 / 10 requirements
- Release / Proof build Warning 0 / Error 0
- Open XML validation
- exact distribution DLL native smoke
- `.ymme` / source / provenance package verification

詳細は `docs/USAGE.md`、設計は `docs/V0.4.1_UX_WORKFLOW_DESIGN.md` を参照してください。

## Intentional boundaries

v0.4.1では、AI/audio analysis、汎用rule engine、node editor、continuous sync、fuzzy recovery、scene-wide resync、新Profile family、自動sort rule、app-restart後の未保存draft recoveryは追加していません。

表情一覧での同Character複数行一括割り当ても今回は見送り、Excel Bridgeを低リスクなbulk pathとして維持しています。

## Branch policy

開発中のv0.4.1 Workflowは `feature/v0.4.1-ux-workflow` / PR #9 で進め、最終Native PASS後にだけ `feature/v0.4-integrated-candidate` へ統合します。

`main` はユーザー実機受入が完了するまで変更しません。PR #6もDraftを維持します。
