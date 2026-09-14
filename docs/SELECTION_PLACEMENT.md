# 選択配置

YMM4 Timelineで対象を選び、Pluginの［選択配置］を開きます。表情一覧の行選択は対象ではありません。

1. 配置関係を選ぶ。
2. その関係のPresetと、Libraryに登録した単一Item Templateを選ぶ。
3. 条件を変える場合はPresetの編集を開き、保存する。
4. ［予定を確認］でFrame / Length / Layerを確認する。
5. ［選択対象へ配置（追加）］を押す。既存Itemは削除・移動・短縮されません。

予定確認は読み取り専用です。配置時には最新Timelineを使って再計算します。対象の移動や追加などがあった場合は、予定をもう一度確認してください。実際の配置結果は下部に表示されます。配置はYMM4のUndo 1回で戻せます。

## 対象に合わせる（Target Companion）

TimelineのItemを1つ選択します。

- 開始 = 対象の開始 + 開始offset
- 終了 = 対象の終了 + 終了offset

開始offsetを負にすると手前へ延長します。終了offsetを正にすると後ろへ延長します。初期値0/0なら対象と同じ範囲です。

## 基準点を強調（Point Emphasis）

TimelineのItemを1つ選択します。基準点は開始 / 25% / 50% / 75% / 終了の5つです。

- 基準点 = 対象の開始 + floor(対象Length × 割合)
- 開始 = 基準点 + 開始offset
- 長さ = 固定長

割合の端数frameは切り捨てます。終了は最後に表示されるframeそのものではなく、対象のexclusive endです。例えばFrame 100、Length 40の終了は140です。

## 共通の制約

時間の単位はframe。負の開始、長さ0以下、整数範囲外、空きLayerなし、Template参照切れ・曖昧参照では追加前に停止します。Layer範囲は優先Layer、より大きい番号、最小から優先直前の順に探索します。候補の全Lengthを確認します。

Characterを持つTemplateとCharacter関連TargetのCharacterが違う場合は停止します。Characterを書き換えて流用しません。

Presetは同じ関係に複数保存できます。最後の1つは削除できません。未保存の編集があれば配置・他Presetへの切替を停止します。［編集を戻す］は保存済み条件へ戻す操作です。

選択配置は独立Itemの追加で、軽い関連付けタグを新設しません。コピー元に関連付けがあれば生成cloneからだけ外します。v0.4の手動再同期は表情一覧からVoiceに関連付けた表情専用です。Quick Dropも関連付けなしです。

W9 implementation notes; native result is recorded separately in the verified checkpoint. This document alone is not a completion claim.
