# YMM4 Template Placer v0.3.0

## インストール

検証対象はYMM4 4.55.1.1 Lite / .NET 10です。`.ymme`をYMM4の通常のPluginインストール手順で導入し、YMM4を再起動してください。

手動で導入する場合は、配布package内の次の4ファイルを `YMM4/user/plugin/Ymm4TemplatePlacer/` へコピーしてください。

```text
Ymm4TemplatePlacer.dll
Ymm4TemplatePlacer.deps.json
DocumentFormat.OpenXml.dll
DocumentFormat.OpenXml.Framework.dll
```

本体DLLだけではExcel機能の依存DLLが足りません。Microsoft Excel本体のインストールは不要です。ソースからビルドした際に出力されるMicrosoft.Windows.SDK.NET.dll / WinRT.Runtime.dllはYMM4側の共通部品を使用するため、Pluginへ重複コピーしません。

YMM4本体・キャラクター素材は同梱しません。通常DLLにはCI用fixtureやテストコードを含めません。provenance.jsonとSHA256は配布物と検証結果の対応を確認するための情報です。

## YMM4内で使う

1. 対象Sceneを開き、ツールメニューから **YMM4 Template Placer** を開きます。
2. Voice一覧のTemplateを選びます。Characterが同じで、表情Item（TachieFaceItem）を1個だけ含む登録Templateが候補です。フォルダを含む登録名で表示します。
3. **配置** を押します。FrameとLengthはVoiceに合わせ、LayerはTemplate内の表情Itemをそのまま使います。
4. 選択を変えて再度配置すると、このSceneの `Remark == CWT_TPL:face` の表情Itemだけを置き換えます。手動の表情や他種Itemは削除しません。
5. YMM4標準のUndo / Redoを使用できます。Plugin独自の履歴はありません。

未選択の行には配置しません。全行未選択で再配置する場合は、既存のPlugin表情だけを削除する確認を表示します。Remarkを手動で変更すると、そのItemはPluginの管理対象から外れます。

**更新** は現在のVoiceとTemplateを読み直します。選択がある場合は、選択をクリアする前に確認します。Scene切替時は前のSceneの選択を引き継ぎません。長いセリフは省略表示し、マウスを置くと全文を確認できます。列幅は調節可能で、多数の行は仮想化表示します。

## Excel / AI連携

**Excelへ出力 → AssignmentsのTemplate列を編集 → 保存 → Excelから読み込み → 一覧を確認 → 配置** の順です。読み込みだけではTimelineを変更しません。

Template列はCharacterに応じたプルダウンです。空欄は「配置しない」です。No、Character、Frame、Length、Serifや非表示シートは編集しないでください。AIにもTemplate列だけを選ばせてください。CatalogのT番号はそのWorkbook内だけで有効です。

ExcelはSnapshotです。VoiceやTemplateをYMM4側で変更した場合は、もう一度出力してください。永続ID、Template hash、同期、差分マージは行いません。同一Characterの同名Templateは曖昧になるため、Excelを使う際は名前を区別してください。

## エラーの対処

- **候補なし**: 同じCharacterの表情Itemを1個だけ含むTemplateをYMM4に登録し、更新してください。
- **配置先が重なる**: Templateを空いているLayerに置いて登録し直し、更新してください。手動Itemを自動で移動・短縮しません。エラーのLayerはAPIの内部番号です。
- **Voice / Snapshotが一致しない**: 正しいSceneを開き直し、更新またはExcelを再出力してください。
- **Templateがない / 曖昧**: 登録名とCharacterを確認して再出力してください。参照先が不明なままTimelineを変更しません。
- **不正なWorkbook / ZIP / Central Directory**: 通常の.xlsxとして保存されているか確認し、Pluginから再出力してください。別形式の拡張子を.xlsxへ変えるだけでは読み込めません。
- **ファイルを保存できない**: Excel等でファイルを開いていないか、保存先のアクセス権を確認してください。

1回のExcel入出力はVoice / Templateそれぞれ50,000件、読み込みファイル16 MiBまでです。この上限は性能保証ではありません。通常の.xlsxだけに対応し、数式を値として評価する機能やマクロ実行はありません。

## 検証

`native-yymm4-proof` artifactにビルドログ、P1〜P8の段階別Assertion、実YMM4内のPlugin画面、Excel fixture、最終Timeline状態、配布DLLのロード確認を保存します。テストの実行方法と検証境界は `tests/README.md` および `docs/VERIFICATION.md` を参照してください。
