# YMM4 Template Placer v0.4.0 Task UX Candidate

よく使うYMM4テンプレートをパレットにまとめ、選んで置くWPFプラグインです。元テンプレートの管理名や内容を変えず、短い表示名で再利用できます。

**使い方・導入:** [ユーザーガイド](docs/USAGE.md)  
**配置計算:** [選択配置の数値](docs/SELECTION_PLACEMENT.md)  
**設計・検証:** [Task UX](docs/TASK_UX.md) / [テスト](tests/README.md) / [既存18項目Acceptance](docs/V0.4_ACCEPTANCE.md)

## 主要な操作

```text
パレット → ＋ テンプレートを追加 → YMM4テンプレートを選択 → 追加 → ダブルクリックで配置
表情一覧 → セリフを見て表情を選択 → 表情を配置
選択配置 → タイムラインで対象を選択 → 何を／どこに置くか選択 → 配置
```

Primary tabはパレット／表情一覧／選択配置です。内部の登録モデルやCharacter/Styleを理解してから使う必要はありません。テンプレート管理は参照確認・再リンク等のsecondary viewです。候補なしの表情行から直接追加へ進めます。

保存済みの配置条件、表情の再同期、Excel Bridge、基準／前面／背面、全5種類の配置Profileは維持しています。選択配置の予定は必要な入力変更時だけ自動更新し、配置時は最新状態で再計算します。常時監視ではありません。

## 安全性

配置は追加、再同期は選択した既存関連表情の更新、削除はYMM4標準操作です。既存アイテムの自動削除・移動・短縮、曖昧な参照の推測修復はしません。全長の衝突と同時配置の予約を検証し、配置できない場合は追加前に止めます。Undo/RedoはYMM4標準を使います。

Libraryには元データの参照だけを保存し、本体の第二DBは持ちません。元参照が0件・複数件なら明示的な再リンクが必要です。Quick Dropと選択配置は独立アイテム、関連付け・現在条件による再同期は表情一覧の明示操作です。

## Candidateと検証

PR #8のUX作業は `feature/v0.4-integrated-candidate` / PR #6へ統合します。**mainへはまだmergeしません。PR #6は手元受入のためDraftを維持します。**

回帰baselineは `c3fc36837508b59d09026a74b5799141eae00a7a`（384 native assertions）。最新の検証済みcommit/runはPR #6本文と段階別checkpointを確認してください。staging commitやビルドだけでは完了を意味しません。

最終packageはP1-P9、W3-W12、V04、WUX1-WUX7、既存18項目＋必須UX12項目、release/proofのWarning 0 / Error 0、実配布DLLのnative smoke、package hashチェックを通した場合だけ生成します。人工fixtureによる検証であり、任意の実PSD素材・初心者本人・物理インストーラ・全DPIまで検証済みとは主張しません。

## ビルド

固定検証環境はWindows / .NET 10 SDK / YMM4 4.55.1.1 Liteです。

```powershell
dotnet build src/Ymm4TemplatePlacer/Ymm4TemplatePlacer.csproj -c Release `
  "-p:YMM4DirPath=C:\Tools\YMM4\" -p:Ymm4Proof=false --nologo -warnaserror
```

Targetは `net10.0-windows10.0.19041.0`、ExcelはOpen XML SDK 3.5.1を使用し、Excel本体やCOMは不要です。通常DLLはproofコードを含みません。

`native-yymm4-proof` artifactには `.ymme`、source ZIP、provenance / SHA256 / package-checks、v04-acceptance / ux-acceptance、段階別ログ、buildとrelease smoke記録、UI画像とpreview計測が入ります。YMM4本体と第三者の素材は配布しません。

汎用Rule Engine、Node Editor、AI API、音声解析、常時同期、ID修復、過去Preset snapshot、新Profile、複数アイテムTemplateの一般化は追加しません。P2のUI複数行一括割当は保留し、Excelによるまとめ編集を維持しています。

[DESIGN](docs/DESIGN.md) / [ROADMAP](docs/ROADMAP.md) はCore設計と歴史的実装順、[TASK_UX](docs/TASK_UX.md) と[USAGE](docs/USAGE.md) が今回の操作・UIの正本です。v0.3のIMPLEMENTATION / VERIFICATIONは歴史的記録で、旧削除・置換方式は使いません。docs-only変更では重いYMM4検証を走らせず、最終統合では全native laneを要求します。
