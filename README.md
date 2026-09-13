# YMM4 Template Placer — v0.3.0

YMM4の現在SceneからVoiceを読み、Characterに合う登録済みTemplateを選んで配置する支援Pluginです。CURRENTの **VoiceItem → TachieFaceItemを1個含むTemplate** を実装しています。

**P0〜P8のGolden Pathを、GitHub Actionsのnative Windows runner上の実YMM4 4.55.1.1 Liteで確認済みです。** 設計資料だけでなく、Plugin画面・Excel入出力・標準Undo/Redo・配布物生成まで実装されています。

## 普段の操作

```text
対象Sceneを開く → ツールからYMM4 Template Placerを開く
→ Voice一覧のTemplateを選ぶ → 配置
```

Template未選択は「配置しない」です。Characterが同じで、TachieFaceItemを1個だけ含む登録Templateが候補になります。Plugin独自の表情DBは持ちません。

再配置は、このSceneの `Remark == "CWT_TPL:face"` の表情Itemだけを置き換えます。手動Itemと他種Itemは保持します。Frame / LengthはVoiceに合わせ、LayerはTemplate内の表情Itemを使います。重なり・参照切れ等は配置前に停止します。YMM4標準のUndo 1回で一括配置を戻せます。

Excelは任意のサブ経路です。

```text
Excelへ出力 → Template列を編集 → Excelから読み込み
→ 一覧を確認 → 同じ配置ボタン
```

.xlsxの生成・読込にMicrosoft ExcelやCOM Automationは不要です。Character別プルダウン、非表示Catalog、Snapshot検証を備えます。読み込みだけではTimelineを変更しません。VoiceやTemplateをYMM4側で変更した場合は再出力してください。

## 導入

Actionsの成功した `native-yymm4-proof` artifactに、次を収録します。

- `Ymm4TemplatePlacer-v0.3.0.ymme` — Pluginインストール用
- `package/` — 本体DLL、Open XML依存DLL、deps.json、利用手順、ライセンス通知
- `Ymm4TemplatePlacer-source.zip` — ビルド可能なソースと自動テスト
- ビルドログ、実機Assertion、Plugin画面、Excel fixture、最終Timeline状態、SHA256、provenance

通常は `.ymme` をインストールしてYMM4を再起動します。本体DLL単体ではExcelの依存DLLが足りません。詳細は [利用手順](docs/USAGE.md) を参照してください。

YMM4本体、Windows SDK DLL、キャラクター素材は配布物に含めません。通常DLLにfixture生成・検証コードは入りません。

## ビルド

Windows、.NET 10 SDK、YMM4 4.55.1.1 Liteを使用します。

```powershell
dotnet build src/Ymm4TemplatePlacer/Ymm4TemplatePlacer.csproj -c Release `
  "-p:YMM4DirPath=C:\Tools\YMM4\" -p:Ymm4Proof=false --nologo -warnaserror
```

Targetは `net10.0-windows10.0.19041.0`、ExcelライブラリはOpen XML SDK 3.5.1です。実機テストと配布パッケージ生成は [workflow](.github/workflows/native-yymm4-proof.yml) を正本とします。

## 検証・設計

[検証結果と境界](docs/VERIFICATION.md) / [テスト実行方法](tests/README.md) / [実装上の選択](docs/IMPLEMENTATION.md) / [現行設計](docs/DESIGN.md) / [先行実装調査](docs/RESEARCH.md) / [検証順](docs/ROADMAP.md)

汎用Rule Engine、Condition DSL、AI API直接連携、独自表情DB、永続Voice ID、Template hash、同期・差分マージ、複数Scene一括処理、PSD固有処理はCURRENTに含めません。

重いCIはソース・プロジェクト・テスト・fixture・workflowの変更時と手動実行時だけ動かします。PR更新についても直前commitとの差分を確認し、docs-only更新ではYMM4のダウンロード・ビルド・起動をスキップします。
