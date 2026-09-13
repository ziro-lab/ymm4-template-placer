# CI fixtures

fixtureはtests内で生成します。外部のキャラクター素材やプロジェクトは不要です。

TestA / TestBにNeutralとSmileの単一TachieFaceItem Template、候補なしのTestCを用意します。VoiceはFrame / Length / Serifを固定し、長い日本語と数式に見える文字列も含めます。手動Faceと、同じMarkerを持つ他種Itemを置き、削除条件を検査します。

Templateの違いはLayer 3 / 4 / 5 / 6でも識別可能にし、選択されたTemplate、独立Clone、座標、再配置、Undoを決定論的に照合します。YMM4の実Template constructorで作ったFace Templateも検査します。

画像・音声を配布せず、TachieFaceItemのデータとTimeline状態を検証します。描画結果やPSD表情の見栄えはCURRENTの検証対象ではありません。

成功artifactに生成したGoldenPath.xlsxと、共有文字列・並べ替えを含むWorkbookを保存します。これらはCI Snapshotであり、利用者の別Sceneへ直接Importするための汎用サンプルではありません。実際に使う場合は自分のSceneからExportしてください。
