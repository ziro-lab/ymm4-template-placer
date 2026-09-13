# CURRENT implementation notes

既存のDESIGN v0.2の製品定義・非目標は維持しています。以下はv0.3.0で実APIに合わせて具体化した実装上の選択です。

## Live API and state

`PlacerViewModel`はYMM4標準の`ITimelineToolViewModel` / `IToolViewModel`を実装し、`TimelineToolInfo`から現在TimelineとUndoRedoManagerを受け取ります。Sceneが変わると一覧と選択を更新します。製品コードにprivate reflectionはありません。

`Character`は文字列ではなくYMM4のCharacterオブジェクトです。候補の照合はそのオブジェクト、画面とExcelへの表示は`CharacterName`を使います。Template名はフォルダを含むYMM4の登録名です。

VoiceSnapshotは開いている一覧の検証用です。永続IDではありません。Voice数・参照・CharacterName・Frame・Length・Layer・Serifが変わった場合は更新またはExcelの再出力を要求します。

## Placement and native Undo

1. 現在VoiceとSnapshotを検証する。
2. 選択された全Templateを現在のCatalogと照合し、GetCloneで独立したFaceを作る。
3. Frame / Length / Remarkを設定し、Groupを0にする。LayerはTemplate内のFaceが持つ値を使う。
4. 手動Itemと新規Item、新規Item同士のLayer上の重なりを検証する。
5. 現在Sceneのmarked Faceだけを除いたリストへ新しいFaceを追加し、YMM4のUndo対応`Timeline.Items`を一度で置き換える。
6. native UndoRedoManagerで変更を記録する。

YMM4の`Timeline.Items`は公開のImmutableListプロパティです。削除と追加を逐次実行する代わりに、検証後に次の一覧へ入れ替えることで、途中まで削除した状態を作りません。手動Itemを自動で移動・短縮する衝突解決APIは呼びません。

配置前のRecordは、それ以前の未記録編集と今回の配置を分ける境界です。配置後のRecordにより今回の差分を記録します。実機で、複数Faceの入替えがUndo 1回・Redo 1回にまとまることを検証しています。独自Undo履歴はありません。

Markerの削除条件は厳密に `item is TachieFaceItem && item.Remark == "CWT_TPL:face"` です。Marker文字列だけが同じ別種Itemは削除しません。

## Workbook

Open XML SDK 3.5.1を使用します。Assignments / _Catalog / _Snapshot / _Meta / 使い方を生成し、Catalog・Snapshot・Metaは非表示です。

Template列はCharacterを参照するDataValidationです。名前付き範囲はWorkbook内だけで有効な`tpl_group_*`を使います。T番号もWorkbook内の簡易IDであり、永続Template IDではありません。

ImportはSchema / Scene名 / Voice Snapshot / Noの一意性 / Catalog内候補 / 現在のTemplateを検証し、その後にAssignmentを返します。共有文字列・inline文字列に対応し、数式は実行せず拒否します。XML文字数、ZIP展開サイズ、ファイルサイズ等に上限を設けています。

Exportは同じ保存先ディレクトリの一時ファイルを完成させてから置き換えます。ImportにはTimeline変更処理を含めません。配置はYMM4内選択と同じEngineを使います。

## Packaging and proof

通常ビルドにtests/*.csは入りません。`-p:Ymm4Proof=true`のときだけCI用のnative adapter・fixture・Assertionを組み込みます。

配布パッケージは本体DLL、DocumentFormat.OpenXml、DocumentFormat.OpenXml.Framework、deps.jsonと文書だけです。YMM4が提供するWindows SDK / WinRT共通DLLを重複配布せず、この最小構成をnative CIで起動します。

配布DLLのSHA256は成果物の同一性確認用です。VoiceやTemplateの同期・hash trackingとは無関係です。
