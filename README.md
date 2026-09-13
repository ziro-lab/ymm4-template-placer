# YMM4 Template Placer

YukkuriMovieMaker4 (YMM4) 上で、対象アイテムの条件に応じて登録済みアイテムテンプレートを配置するための支援Pluginです。

初期実装では **VoiceItem → TachieFaceItem Template** に限定し、表情配置を最初のユースケースとして完成させます。

## 基本方針

主経路は **YMM4内完結** です。

```text
YMM4
→ 現在シーンのVoiceItem一覧
→ 各行のプルダウンから対応Templateを選択
→ 配置
→ Timeline
```

Excelは大量の割り当てを人間やAIで編集するためのサブ経路です。

```text
YMM4
→ Excel Export
→ 人間 / AIがTemplate列を編集
→ Excel Import
→ 同じPlacement Engine
→ Timeline
```

Templateの正本はYMM4です。Plugin独自の表情データベースは持ちません。

## CURRENT scope

- 現在SceneのVoiceItem取得
- `ItemSettings.Default.Templates` から表情Template候補を取得
- `VoiceItem.Character` と一致する候補だけをプルダウン表示
- Template内の `TachieFaceItem` をCloneして配置
- `Frame` / `Length` をVoiceItemへ合わせる
- Plugin配置物へ `CWT_TPL:face` のRemarkを付与
- 再配置時はPlugin配置物だけを置き換える
- YMM4 Undo/Redoへ統合
- `.xlsx` Export / Import とCharacter別プルダウン

## 非目標（初期版）

- 汎用Rule Editor / Condition DSL
- 独自表情データベース
- Voiceの永続ID・双方向同期・競合解決
- AI APIのPlugin直接接続
- PSD固有処理
- 複数Scene一括処理

## 開発環境

初期検証Target:

- YMM4 4.55.1.1 Lite
- .NET 10
- `net10.0-windows10.0.19041.0`
- GitHub Actions native Windows runner

GitHub Actionsでは本物のYMM4を起動してPluginロードを検証します。docsだけの変更では重いYMM4 CIを起動しません。

## ドキュメント

- [`docs/DESIGN.md`](docs/DESIGN.md) — Current設計とAcceptance
- [`docs/RESEARCH.md`](docs/RESEARCH.md) — 既存YMM4 Pluginから確認できた実装パターン
- [`docs/ROADMAP.md`](docs/ROADMAP.md) — GitHub実機検証の段階

## Status

Bootstrap / P0: native Windows YMM4 plugin load proof
