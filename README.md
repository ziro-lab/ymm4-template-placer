# YMM4 Template Placer v0.4.0 Candidate

YMM4の登録済みItem Templateを、短い名前・Character / Styleの棚で整理し、再生位置や選択Itemとの関係で安全に配置するWPF Pluginです。

**使い方・インストール:** [ユーザーガイド](docs/USAGE.md)  
**配置の数値の意味:** [選択配置](docs/SELECTION_PLACEMENT.md)  
**検証方法:** [テスト](tests/README.md) / [v0.4 Acceptance](docs/V0.4_ACCEPTANCE.md)

## このbranchについて

v0.4の統合Candidateです。PR #6はDraftのまま維持し、mainへのmergeは行いません。検証済みcheckpointは各Wの記録とPR本文を参照してください。個々のstaging commitは検証完了を意味しません。

最終配布物は実YMM4内のP1〜P9、W3〜W12、全18Acceptance、release/proof Warning 0 / Error 0、通常DLLの実ホストロード、package内容・hash確認を通したときだけ生成します。`provenance.json`のsource/runと`v04-acceptance.json`を確認してください。

v0.3.1は保持すべき回帰baselineです。[IMPLEMENTATION](docs/IMPLEMENTATION.md) / [VERIFICATION](docs/VERIFICATION.md) はv0.3の歴史的記録であり、v0.4の現在の使い方ではありません。特に旧版の削除・置換操作は廃止しています。[DESIGN](docs/DESIGN.md) / [ROADMAP](docs/ROADMAP.md) はv0.4の設計と検証順の正本です。

## 主な操作

| 画面 | 用途 |
| --- | --- |
| 表情一覧 | VoiceへCharacter対応Face Templateを割り当て、保存済みExpression Presetで追加する。Excelはこの割り当て専用のサブ経路。 |
| 選択配置 | 1 ItemのTarget Companion / Point Emphasis、複数ItemのSelection Range、2 ItemのBoundary。 |
| パレット | Character棚の一時自動切替・手動Style棚・ダブルクリックQuick Drop。 |
| Library | YMM4 Templateへの厳密な参照、短い表示名、Character対応、再リンク・登録解除。 |

LibraryはTemplate本体を保存しません。同じ登録を複数の棚へ置けます。参照先が0件・複数件なら推測せず、明示的な再リンクへ案内します。

Quick Dropは現在再生位置とTemplate本来のLengthを使います。Character棚は基準 / 前面（大きいLayer番号） / 背面（小さいLayer番号）を選択でき、予定区間の全長でLayer衝突を確認します。既存Itemを空けるために動かしたり短くしたりしません。

表情PresetはVoiceと同じ、またはNext Same Character + MaxGap、開始/終了offset、Layer範囲・優先番号を保存できます。次Voiceが重なっていることだけを理由に現在Voiceより短縮しません。

## 追加・再同期・削除

```text
配置   = 全体を計画してから追加
再同期 = 選択した関連表情へ現在のExpression Presetを再適用
削除   = YMM4標準操作
```

関連付けは表情一覧からのVoice Expression配置だけです。Remark本文を残して単純な連番を付け、手動再同期時に連番＋実際のCharacterでTargetが一意な場合だけ更新します。特定不能はスキップし、成功分は1回のnative Undoへまとめます。Quick Dropと選択配置は独立Itemで、関連付けを持ちません。

通常配置はadd-onlyです。全行未選択でも既存Itemを削除しません。Presetの未保存編集、入力不正、空きLayerなし、Template参照切れなどでは追加前に停止します。

## ビルドと配布

検証baselineはWindows / .NET 10 SDK / YMM4 4.55.1.1 Liteです。

```powershell
dotnet build src/Ymm4TemplatePlacer/Ymm4TemplatePlacer.csproj -c Release `
  "-p:YMM4DirPath=C:\Tools\YMM4\" -p:Ymm4Proof=false --nologo -warnaserror
```

Targetは`net10.0-windows10.0.19041.0`、ExcelはOpen XML SDK 3.5.1です。Excel本体やCOM automationは不要です。通常DLLにはproofコードを含めません。

`native-yymm4-proof` artifactの主要成果物:

- `Ymm4TemplatePlacer-v0.4.0.ymme`: Pluginと必要なOpen XML DLL、説明・検証metadata。
- `Ymm4TemplatePlacer-source.zip`: 検証したcheckoutのソース一式。
- `provenance.json` / `SHA256.json` / `package-checks.json`: 対応するsource、run、DLL、archiveの照合情報。
- `proof-log.txt` / `v04-acceptance.json` / buildログ / release smoke記録 / UI画像: 実行Evidence。

YMM4本体や第三者のキャラクター素材は同梱しません。通常DLLが実YMM4でロードされたことと、`.ymme`内DLLがそのDLLと一致することを検査します。物理的なインストーラ操作やユーザー固有PSD素材の見た目まで検証済みとは主張しません。

## 境界

汎用Rule Engine / DSL、Template本体の第二DB、常時同期、copy/paste ID修復、曖昧Target推測、過去Preset Snapshot、自動削除・再生成、複数Item Template一般化、音声解析、AI API直接連携は実装しません。

CIの重い処理はソース・XAML・project・test・fixture・workflow変更または明示的な手動実行時だけです。**docs-onlyの通常変更ではYMM4をダウンロード・起動しません。** 個別staging編集をまとめる場合も、次のWへ進む前にそのW全体のnative proofを必要とします。
