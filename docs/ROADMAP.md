# ROADMAP — native YMM4 proof ladder

CURRENT v0.3.0: P0〜P8を実装し、native YMM4で全段階の通過を確認。証拠・検証境界は [VERIFICATION.md](VERIFICATION.md) を参照。

後の段階で前のAPI失敗を隠さず、次の順序を維持します。

| Phase | Exit condition |
|---|---|
| P0 Plugin load | Windowsでbuild、実YMM4起動、callback marker |
| P1 Live Template catalog | 実ItemSettingsの単一Face TemplateとCharacter別候補 |
| P2 VoiceItem access | 現在TimelineからCharacter / Frame / Length / Serif |
| P3 Clone and placement | 独立Clone、元Template保持、配置後のnative Item状態 |
| P4 Assignment UI | 実YMM4内のVoice一覧、dropdown選択、配置ボタン |
| P5 Excel Export | COMを使わない.xlsx、非表示Catalog、Character別候補、schema検査 |
| P6 Excel Import | fixtureのTemplate列編集、Assignment読込、同じEngineで配置 |
| P7 Safety | 再配置、手動Face保持、無効Workbook・Template欠落でゼロ変更 |
| P8 Undo | 複数Faceの配置を標準Undo 1回で復元、Redoも確認 |

## CI policy

Native Windows YMM4 proofが主検証経路です。YMM4 4.55.1.1 LiteをSHA256固定で取得します。Linux / Wineは通常レーンにしません。

重い処理の対象変更:

- `src/**/*.cs`, `src/**/*.csproj`, `src/**/*.xaml`
- `tests/**/*.cs`, `tests/**/*.csproj`, `tests/**/*.ps1`
- `fixtures/**/*.json`, `fixtures/**/*.ymmp`, `fixtures/**/*.png`, `fixtures/**/*.xlsx`
- native workflow自身
- 明示的なworkflow_dispatch

**Documentation-only commits must not download or launch YMM4.** push / pull_requestのpath filterに加え、PR synchronizeでは直前headからの差分をCiScope.ps1で検査します。PR全体に過去のコード変更が含まれていても、最新更新がdocsだけならnative処理をスキップします。対象判定自体に小さい自動テストがあります。

成功artifactに通常配布物、source、native結果、UI、Workbook、provenanceを保存します。YMM4本体と検証DLLは配布パッケージへ入れません。
