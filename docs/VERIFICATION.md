# Native verification — v0.3.0

## Confirmed completion

最初のP1〜P8全成功は **source commit `053698e0946113b50d4092d1433c38bc899e4f2e` / native run #9 (`34771149816`)** です。

https://github.com/ziro-lab/ymm4-template-placer/actions/runs/34771149816

この実行では53件のnative Assertion、通常DLLと検証DLLの各ビルド（Warning 0 / Error 0）、通常配布DLLの別プロセスでのロード確認、.ymmeとソースZIPの生成が成功しました。

その後、共有文字列形式・並べ替えたExcel・未選択Import、native Template constructor、配布DLLの実ロードSHA256照合、docs-only CI gateを追加しています。**個々の配布物に対応する最終結果は同梱 `provenance.json` と `proof-log.txt` を参照してください。** provenanceにはcheckout commit/tree、source head、run ID、Assertion数、DLL SHA256を記録します。

## What the proof checks

| Phase | Deterministic verification |
|---|---|
| P0 | 実YMM4のPlugin callback。通常配布DLLは別起動し、build種別とSHA256を照合 |
| P1 | 実際のItemSettings.Default.Templates、単一Faceの絞り込み、Character別候補、候補なし |
| P2 | 実際の現在TimelineからCharacter / Serif / Frame / Lengthを取得 |
| P3 | 独立Clone、元Template保持、Frame / Length / Layer / Character / Remark |
| P4 | YMM4がホストした実Plugin画面、ComboBoxの候補・双方向選択、WPF配置ボタン経由の配置 |
| P5 | .xlsx出力、Open XML schema validation、Character別DataValidation、非表示Catalog、数式風文字列の安全な出力 |
| P6 | Template編集後のImport、Timelineを変えない選択プレビュー、同じPlacement Engineでの配置 |
| P7 | 重複配置防止、手動Face保持、同じRemarkを持つ他種Item保持、無効入力のゼロ変更 |
| P8 | 複数Itemの一括配置を標準Undo 1回で復元、Redo 1回、全件未選択による削除とそのUndo |

P7はCharacter不一致、No重複、数式セル、削除済みTemplate、Voice変更、Layer衝突、不正ZIPを検査します。失敗前後のTimeline Item一覧の参照と内容を照合し、Import失敗では以前のAssignmentも保持することを確認します。

## Boundaries

- **Windows runner上の実YMM4 4.55.1.1 Lite** で検証します。Linux/WineやAPIの偽物で代替していません。他バージョンの互換性は未保証です。
- 機能テストは検証コードを有効にしたPluginビルドで実施します。配置・UI・Excelの製品コードは通常ビルドと共通です。通常配布DLLに検証型が存在しないことを検査し、別の実YMM4プロセスでそのDLL自体をロードします。
- Plugin画面は実際にYMM4のツール領域へ表示します。native ToolAreaの表示状態を有効にし、実ComboBoxを操作し、ButtonAutomationPeer経由で配置コマンドを実行します。画面を別アプリで模倣していません。
- Undo/RedoはYMM4標準UndoRedoManagerへの1操作を検証しています。物理キーボードによるCtrl+Z入力の自動化ではありません。
- ExcelはOpen XMLの構造検査とファイル編集・再読込で検証します。**Microsoft Excelデスクトップ版でプルダウンをマウスクリックするテストは行いません。** Excel本体を必要としないことが仕様です。
- fixtureはTestA / TestB / TestCの自作データです。実キャラクター素材、音声生成、PSDの見た目、動画出力品質は対象外です。
- `.ymme`は検証済み配布DLL一式のZIPパッケージです。nativeの機能検証はPluginフォルダへの配置で行い、インストーラー画面のクリック自動化は行いません。

## Failures were classified before fixing

実装途中では、hostの取得・native Tool menuの階層は**テストadapter側**の修正、C#の構文/nullabilityは**ビルド時**の修正、Font子要素の順序は**Excel出力側**の修正でした。P3の成功やPluginの起動成功まで取り消す判断はしていません。

最初のUI画面確認で狭いドッキング領域の右端切れを検出し、可変列幅と「候補なし」のセル内表示へ修正しました。
