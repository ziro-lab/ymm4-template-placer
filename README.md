# YMM4 Template Placer — v0.3.0 implemented / v0.4 designed

YMM4の登録済みItem Templateを、編集作業で使いやすい形に整理・配置するための支援Pluginです。

## Current state

**v0.3.0 is implemented and verified.** GitHub Actionsのnative Windows runner上の実YMM4 4.55.1.1 Liteで、Voice一覧 / Character対応Face Template / 配置 / Excel / Safety / 標準Undo/RedoまでP0〜P8を通しています。

**v0.4 is the current implementation target.** v0.4では、v0.3のVoice→Faceだけを一般化するのではなく、YMM4 TemplateをPlugin Library / Paletteで整理し、有限のsemantic Placement Profileで安全に配置する構造へ拡張します。正本は [DESIGN](docs/DESIGN.md)、実装・実機検証順は [ROADMAP](docs/ROADMAP.md) です。

v0.3で実証済みの実装詳細と検証境界は [IMPLEMENTATION](docs/IMPLEMENTATION.md) / [VERIFICATION](docs/VERIFICATION.md) に残しています。

## v0.4 product direction

```text
YMM4 Template
    ↓ reference only
Plugin Library
    ↓ short display name / optional Character
Palette
    ↓
Placement Profile + Preset
    ↓
preflight Placement Plan
    ↓
Timeline
```

主な操作は次の4系統です。

- **表情一覧** — VoiceごとにCharacter Paletteの表情Templateを割り当てる。Excel / AI bridgeもここに限定。
- **選択配置** — 選択ItemへTarget Companion / Point Emphasis / Selection Range / Boundaryなどの定型関係でTemplateを置く。
- **Character Palette** — VoiceItem / TachieFaceItemの単体選択からCharacterを一時自動判定し、そのCharacter用Templateだけを表示。Context終了後は以前の手動Paletteへ戻る。
- **Style Palette** — 明るい / 暗い / 戦闘など、現在使いたい演出語彙を手動で切り替える。

Palette Entryの**ダブルクリックは現在再生位置へのQuick Drop**です。Quick DropはTemplate本来のLengthで追加し、Targetとの関連付けは作りません。

Character PaletteのQuick Dropでは、Layer位置を次から選べる設計です。

```text
基準
前面（大きいLayer番号）
背面（小さいLayer番号）
```

PSD立ち絵Pluginで表情Itemを複数Layerへ重ねる用途を想定しています。既存Itemを動かさず、予定区間全体を見て空きLayerを探索します。

## Lightweight resync

v0.4ではFinal Cut型の常時Connected Clipは作りません。

Target Voiceへ関連付き配置した場合だけ、Remarkへ単純な連番を目印として追加し、ユーザーが明示的に「再同期」した時だけ現在のPresetで関係を再計算します。

```text
ID一致
→ Character一致
→ 一意なら再同期
→ 0件 / 複数件なら推測せずSkip
```

Quick Dropは再同期対象外です。通常Placementも既存Itemを勝手に削除しません。削除はYMM4標準操作で行います。

## v0.3 usage

現在配布済みのv0.3.0では、対象Sceneを開いてツールからYMM4 Template Placerを開き、Voice一覧のTemplateを選んで配置します。

```text
対象Sceneを開く
→ YMM4 Template Placer
→ Voice一覧のTemplateを選ぶ
→ 配置
```

Excelは任意のサブ経路です。

```text
Excelへ出力
→ Template列を編集
→ Excelから読み込み
→ 一覧を確認
→ 配置
```

.xlsxの生成・読込にMicrosoft ExcelやCOM Automationは不要です。

## Build baseline

Windows、.NET 10 SDK、YMM4 4.55.1.1 Liteを使用します。

```powershell
dotnet build src/Ymm4TemplatePlacer/Ymm4TemplatePlacer.csproj -c Release `
  "-p:YMM4DirPath=C:\Tools\YMM4\" -p:Ymm4Proof=false --nologo -warnaserror
```

Targetは `net10.0-windows10.0.19041.0`、ExcelライブラリはOpen XML SDK 3.5.1です。native実機テストと配布生成は `.github/workflows/native-yymm4-proof.yml` を正本とします。

## Design boundaries

v0.4では次を作りません。

- YMM4 Template本体をコピーする第二のTemplate DB
- Generic Rule Engine / DSL / Node Editor
- 自動全削除→再配置
- 常時同期 / Voice移動event監視
- Copy/Paste ID自動修復や曖昧Target推測
- 過去Preset Snapshot
- Persistent Connected Clip
- Multi-item Template一般化
- Protected Intro/Outro retiming
- Parent / Follow / Track Matte
- Beat / 音声解析 / word timing
- AI API直接連携

重いCIはソース・XAML・プロジェクト・テスト・fixture・workflow変更時と手動実行時だけ動かします。**docs-only変更ではYMM4のダウンロード・ビルド・起動を行いません。**
