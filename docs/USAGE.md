# YMM4 Template Placer v0.4.0

YMM4に登録したTemplateを短い名前と棚で整理し、再生位置や選択Itemとの関係で配置するPluginです。最初はプロジェクトのコピーで試してください。

## インストール

検証対象はWindows / YMM4 4.55.1.1 Lite / .NET 10です。`.ymme`をYMM4の通常のPluginインストール手順で導入し、YMM4を再起動してください。

手動導入では、YMM4を終了してから配布packageの次の4ファイルを `YMM4/user/plugin/Ymm4TemplatePlacer/` へコピーします。旧DLLを別のPluginフォルダへ重複して残さないでください。

```text
Ymm4TemplatePlacer.dll
Ymm4TemplatePlacer.deps.json
DocumentFormat.OpenXml.dll
DocumentFormat.OpenXml.Framework.dll
```

本体DLLだけではExcel機能の依存DLLが足りません。Microsoft Excel本体やExcel COM automationは不要です。Microsoft.Windows.SDK.NET.dll / WinRT.Runtime.dllはYMM4側の共通部品を使うため、Pluginへ重複コピーしません。

YMM4本体・キャラクター素材・CI専用fixtureは通常配布DLLへ含めません。README、THIRD_PARTY_NOTICES、provenance、v04-acceptanceは説明と検証情報です。ソースZIPは開発用で、通常インストールには不要です。

## 最初に使う画面

対象Sceneを開き、ツールメニューから **YMM4 Template Placer** を開きます。主画面は4つです。

- **表情一覧**: Voiceごとに同CharacterのFace Templateを割り当てて一括配置。
- **選択配置**: YMM4 Timelineで選んだItemを基準に、飾りや強調を配置。
- **パレット**: Character / Styleの棚からダブルクリックでQuick Drop。
- **Library**: YMM4 Templateへの参照、短い表示名、任意のCharacter対応を登録。

PluginはTimelineを所有しません。**配置は追加、再同期は既存の関連表情の更新、削除はYMM4標準操作**です。v0.3の自動削除・一括置換は行いません。未選択の表情行は何もせず、全行未選択でも既存Itemを削除しません。

## Libraryとパレット

まずYMM4で、Itemを1つだけ含むTemplateを登録してください。Libraryで一覧を更新し、元TemplateとPlugin表示名を選んで新規登録します。元Templateの長い管理名は変更されません。Libraryは参照だけを保存し、Template本体を複製しません。

Libraryと棚は別です。同じLibrary登録を複数の棚へ追加できます。棚を作るにはパレットの編集欄でCharacter / Styleの種類、名前、必要なCharacterを設定します。Character棚への登録では対応Characterを揃えてください。同じ短い名前があっても、Characterや元Template名を確認して選べます。

元Templateの削除・名前変更・移動などで参照が切れたら、Libraryで対象登録と正しい元Templateを選んで **再リンク** します。同名や同じ参照情報の候補が複数あれば自動で選びません。YMM4側で区別してから再リンクしてください。登録解除や棚の削除は、元TemplateやTimeline Itemを削除しません。

Character棚はVoiceItem / TachieFaceItemをTimelineで単体選択したときだけ一時切替します。選択解除、複数選択、対象外Itemでは以前の手動Character棚へ戻ります。Style棚は手動切替で、Character選択によって勝手に変わりません。

## Quick Drop

パレットの項目をダブルクリックすると、**現在の再生位置**へTemplateに保存されている長さで追加します。選択したVoiceの開始や長さには合わせません。関連付けIDは新設せず、コピー元に関連付けタグがあっても生成cloneからだけ外します。Quick Dropは再同期対象ではありません。

Character棚では基準 / 前面 / 背面を選べます。

- **基準**: TemplateのLayer、または棚に保存したLayer範囲・優先番号。
- **前面**: 同区間に重なる同Character関連Itemより大きいLayer番号側へ探索。
- **背面**: 同Character関連Itemより小さいLayer番号側へ探索。

同Characterの関連Itemがなければ基準へ戻ります。無関係なCharacterは前後の基準を変えませんが、どのItemも候補Layerの障害物にはなります。開始frameだけでなく配置予定の全長で衝突を確認します。前面・背面は指定方向だけを探し、空きがなくても反対側へ回り込みません。

範囲探索の上限9999はPlugin側の安全上限で、YMM4の上限という意味ではありません。TemplateのLayerをそのまま使う基準モードと、範囲探索は別です。Layer条件の編集後は保存してください。未保存のまま配置しません。

## 表情一覧とExpression Preset

Voiceと同じCharacterのTachieFaceItemを1つだけ含むTemplateが候補です。Character棚へ登録したLibrary項目は候補の先頭で短い名前を使えます。長いセリフはマウスを置くと全文を確認できます。

表情Presetを選び、各行のTemplateを指定して **配置（追加）** を押します。Presetは複製して複数保存できます。

- **Voiceと同じ**: 現在Voiceの区間を基準にします。
- **次の同Character Voiceまで**: 現在Voiceの終了から次の同Character Voiceの開始までがMaxGap以内なら、その開始まで延長します。次Voiceが重なっていても、それだけを理由に現在Voiceより短くしません。

開始・終了offsetはその基準区間へ加算します。負の終了offsetなど、明示したoffsetによる短縮は別です。時間の単位はframe。LayerはTemplateそのまま、または最小・最大・優先番号の範囲で指定します。既存Itemだけでなく、同じ操作でこれから追加するItemの区間も予約してから配置します。

1行でも必要な配置を安全に計画できなければ、操作全体を追加前に停止します。既存の関連表情が明確に見つかったVoiceへは勝手に置換・重複追加しません。現在の関連表情を揃え直す場合は再同期、消す場合はYMM4の削除操作を使ってください。

**更新**は現在SceneのVoice / Templateを読み直します。表情一覧の選択を引き継ぐ操作ではありません。Scene変更後やExcel Snapshotが古い場合は、更新・再出力してください。

## 手動再同期

表情一覧から配置したVoiceと生成表情には、ユーザーのRemark本文を残したまま弱い目印を付けます。

```text
Voice: CWT_TPL:V=<連番>
表情:  CWT_TPL:S=<連番>;P=expression
```

Voiceを移動したあと、TimelineでそのVoiceまたは関連表情を選び、**選択した表情を再同期**を押します。関連表情選択ならそのItemだけ、Voice選択ならそのVoice IDを参照する表情が対象です。

再同期は **現在保存しているExpression Preset** を使います。古いPresetのSnapshotは保存しません。未保存の編集があれば保存または編集を戻してから実行してください。

連番と実際のCharacterが一致し、Targetが1つだけ見つかるときに更新します。Targetがない、コピーでIDが重複した、タグが不正などの場合は推測せずスキップします。Frame・セリフ・並び順から復旧しません。Layer衝突などで変更できないItemも理由を表示して残します。変更できた分はまとめてYMM4のUndo 1回で戻せます。

常時追従、Voice削除の自動処理、コピーしたIDの自動修復、Scene全体一括同期はありません。Quick Dropと選択配置は独立Itemなので対象外です。プラグインのタグ部分を手動で改変すると関連を解決できなくなる場合があります。

## 選択配置

表情一覧の行ではなく、**YMM4 TimelineのItem**を選びます。配置関係、Preset、Library Templateを選択し、条件を保存、予定を確認、配置の順です。予定確認はTimelineを変更せず、配置時に最新状態で再計算します。対象を動かしたら予定を再確認してください。

| 配置関係 | 必要な選択 | 配置範囲 |
| --- | --- | --- |
| 対象に合わせる | 1 Item | 対象開始＋開始offset ～ 対象終了＋終了offset |
| 基準点を強調 | 1 Item | 開始 / 25% / 50% / 75% / 終了の基準点＋offset、固定長 |
| 選択範囲を覆う | 2 Item以上 | 全選択の最小開始−前余白 ～ 最大終了＋後余白 |
| 2Itemの境界 | ちょうど2 Item | 許容差を確認し、先に始まるItemの終了＋offset、固定長 |

割合の端数frameは切り捨てです。終了はexclusive endで、Frame 100 / Length 40なら140です。範囲配置は選択順に関係なく、全Itemを覆う1つのItemを追加します。前後の余白は0以上です。

境界は `abs(先のItem.End − 後のItem.Start) <= 許容差` の場合だけ使います。開始位置が同じなら前後を推測せず停止します。小さな隙間・重なりを許容しても基準位置は先のItemの終了のままで、中間点へ変更しません。近くの別Itemや別カットを探すこともありません。初期設定は許容差0、offset −15、固定長30です。

Character付きTemplateとCharacter関連Targetが一致しなければ停止します。複数Characterを含む範囲へ共通の飾りを置く場合は、Characterを持たないTemplateを使用してください。

## Excel

**Excelへ出力 → AssignmentsのTemplate列を選択 → 保存 → Excelから読み込み → 表情一覧を確認 → 配置**の順です。読み込みだけではTimelineを変更しません。配置にはExcel出力時ではなく、現在保存しているExpression Presetを使用します。

ExcelはVoice Expressionの割り当て専用です。選択配置の汎用編集画面ではありません。Template列以外のNo、Character、Frame、Length、Serifや非表示シートを編集しないでください。空欄は配置しない意味です。AIへ選択を依頼する場合もTemplate列だけを対象にしてください。PluginからAI APIを直接呼び出す機能はありません。

WorkbookはSnapshotです。VoiceやTemplateをYMM4側で変更した場合は再出力してください。CatalogのT番号はそのWorkbook内だけで有効です。同Characterの同名Templateは区別してから使ってください。

Voice / Template各50,000件、読み込み16 MiBの上限は性能保証ではありません。通常の.xlsxのみ対応し、数式評価やマクロ実行は行いません。

## エラーと設定

**未保存**なら条件を保存するか編集を戻します。**空きLayerなし**なら範囲や優先番号、TemplateのLayerを見直します。既存Itemは自動移動・短縮しません。**Template参照切れ・曖昧**ならLibraryで再リンク、**Snapshot不一致**なら表情一覧の更新・Excel再出力を行ってください。

負の開始、長さ0以下、整数範囲外、不正入力では追加前に止まります。失敗理由は画面下に表示し、長い場合はスクロールできます。

設定は `%LOCALAPPDATA%/Ymm4TemplatePlacer/settings-v04.json` です。壊れた設定や別Toolからの変更を勝手に上書きしません。設定エラー時は元ファイルを退避・確認し、Toolを開き直してください。旧設定に新Profileを追加する処理は読み込み時にはメモリ上だけで行い、明示的な保存まで元ファイルを変更しません。

## 検証情報

配布と一緒に `provenance.json`、`v04-acceptance.json`、SHA256、実YMM4の段階別Assertion、buildログ、release DLLロード記録、通常幅・狭い幅の画面画像を保存します。`V04=PASS`と全18Acceptanceが揃わない場合は最終packageを生成しません。

検証は固定版YMM4と小さな人工fixtureによる実WPF操作・状態検査です。任意のYMM4将来版、ユーザー固有のPSD素材の見た目、物理マウス入力や全DPI環境まで保証するものではありません。通常配布DLLの実ホストロードと、`.ymme`内DLLの一致は別途検査します。
