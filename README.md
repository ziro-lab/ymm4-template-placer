# YMM4 Template Placer

YMM4の登録済みテンプレートを、選んだアイテムとの位置・長さの関係ごと使い回すTool Pluginです。

```text
基準アイテムを選ぶ、またはタイムラインの時間位置を指定
→ 必要ならセットを切り替える
→ 演出タイルをクリック
```

## 現在のmain

2026-09-20に、Hands-on確認済みの **Round 4 A/B/C** をPR #16でmainへ統合しました。

現在のmain基準:

- compact placement UI / Voice navigation;
- global presentation / position shortcuts / common Voice row height;
- protected automatic Settings persistence + `今回の変更を戻す`;
- Focused / Checkpoint / Releaseの段階的検証。

main mergeは `7dc30dcb887a0d57f3079d72ffe89dc62bedd9fa`、main Release run #227 `35508468265` でフルNative回帰・実配布DLL smoke・verified packageまでPASSしています。

次の実装はDraft PR #19 **Experimental expression preset source** です。

現在仕様の正本は `docs/CURRENT_ARCHITECTURE.md`。用語は `docs/GLOSSARY.md`、未実装案は `docs/BACKLOG.md`、互換/旧経路の扱いは `docs/LEGACY_COMPATIBILITY_MAP.md` を参照してください。

## 主な機能

配置画面は「対象の状況 → セット → タイル」で操作します。アイテムの種類・選択数・キャラクターに合うセットを表示し、汎用配置では再生位置を基準にします。セットを決めるときに配置関係を設定しておけば、普段の編集では同じ判断を繰り返す必要がありません。

タイル全体のドラッグによる並べ替え、色・形・別名の設定、Auto/固定列の表示、全セット共通の位置ショートカットを備えています。ショートカットは初期OFFで、明示したキーだけが固定列の配置画面で動きます。

「表情をまとめて」ではVoice一覧から表情テンプレートを選択し、関連する表情を即時配置・置換できます。Voiceの追加・変更を自動検出し、行を選ぶと再生位置・Preview・タイムライン表示を同期します。Excelによる割り当ては補助経路です。

複数アイテムのテンプレートは内部の時間差・レイヤー差・長さを保つBundleとして扱います。汎用配置には対応できるテンプレート種別の制約があります。具体的な使い方は `docs/USAGE.md` を参照してください。

## 安全性と互換性

元データはYMM4のテンプレートです。プラグインは参照を保存し、欠損・重複を推測修復しません。配置前に全件を検証し、無関係な既存アイテムを移動・短縮・削除して空きを作ることはありません。

表情の置換・削除は厳密に特定できたPlugin管理の関連Bundleだけが対象です。手動表情は推測削除しません。配置はYMM4のUndo/Redoで戻せます。旧設定データは保持しますが、通常画面の旧互換ワークスペース入口は削除済みです。

## 配布と検証

回帰検証環境は **YMM4 4.55.1.1 Lite / Windows / .NET 10**。製品側は必要なAPIの有無を確認し、バージョン番号だけで機能全体を停止しません。ただし将来版やすべての第三者プラグインとの互換性を保証するものではありません。

現在mainの基準はPR #16 merge `7dc30dcb887a0d57f3079d72ffe89dc62bedd9fa`。main Release run #227 `35508468265` でフルNative回帰、配布DLL smoke、verified packageを通しています。検証は `docs/VALIDATION_STRATEGY.md` のFocused / Checkpoint / Releaseに従います。

ReleaseではRelease/Proofのコンパイラ警告・エラー0件、全Checkpoint回帰、独立した証拠検査と異常系、最終配布DLLのnative smokeを維持します。普段のDraft編集はFocusedで軽量化します。`.ymme`の内部ルートは常に `Ymm4TemplatePlacer/` です。

実PSD素材、他プラグイン、DPI・テーマの組み合わせは実環境での確認が必要です。自動検証の成功と、あらゆる環境での表示一致は区別しています。未実装の操作改善・Placement Recipe案は `docs/BACKLOG.md` に集約しています。
