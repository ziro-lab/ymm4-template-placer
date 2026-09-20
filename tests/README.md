# Automated native integration tests — v0.4

主自動テストはWindows上の本物のYMM4 4.55.1.1 Liteを使用します。登録Template、現在Timeline、実WPF Plugin画面、標準Undo/Redoを直接検査し、API stubを成功根拠にしません。

## 実行

通常はGitHub Actionsの`native-yymm4-proof`を使用します。対象ソース変更のpush/PR、または明示的なworkflow_dispatchで実行します。docs-only変更は`CiScope.ps1`によってダウンロード・ビルド・起動を抑止します。

ローカルでは普段のYMM4・編集中プロジェクトを使わず、**別のWindowsテストユーザーと空のYMM4ディレクトリ**を用意してください。Plugin設定はLocalAppData、YMM4のTemplateはホスト設定へ保存されるため、ディレクトリを分けるだけでは設定まで隔離されません。fixtureを登録し、起動したプロセスを停止するテストです。

```powershell
$ymm = 'C:\IsolatedTest\YMM4\'
dotnet build src/Ymm4TemplatePlacer/Ymm4TemplatePlacer.csproj -c Release "-p:YMM4DirPath=$ymm" -p:Ymm4Proof=false -o out/package --nologo -warnaserror
dotnet build src/Ymm4TemplatePlacer/Ymm4TemplatePlacer.csproj -c Release "-p:YMM4DirPath=$ymm" -p:Ymm4Proof=true -o out/proof --nologo -warnaserror
$dest = Join-Path $ymm 'user/plugin/Ymm4TemplatePlacer'
New-Item -ItemType Directory -Force $dest | Out-Null
Copy-Item out/proof/Ymm4TemplatePlacer.dll,out/proof/DocumentFormat.OpenXml.dll,out/proof/DocumentFormat.OpenXml.Framework.dll,out/proof/Ymm4TemplatePlacer.deps.json $dest
./tests/RunNative.ps1 -Ymm4Dir $ymm -OutputDir (Join-Path $PWD 'out') -DistributionDir (Join-Path $PWD 'out/package')
```

この例はnative functional proofです。release smoke、buildログ保存、GitHub run metadata付きpackageまで再現する場合はworkflowを使用します。`PackageVerified.ps1`はCIのログ・環境変数を要求し、不足時に成功したふりをしません。

CI差分判定だけを検査する場合:

```powershell
./tests/CiScope.ps1 -SelfTest
```

## Proofの構成

| ファイル | 検証 |
| --- | --- |
| NativeProof / GoldenPathProof / NativeUiProof | P1-P9、実host readiness、Template、割り当てUI、add-only/Undo、hide/reopen |
| WorkbookVariantProof | shared string、行並び替え、空Template、Open XML回帰 |
| NativeLibraryProof | W3、独立表示名・厳密参照・明示再リンク・設定保護 |
| NativePaletteProof | W4、棚の分離・共有登録・Character context復帰 |
| NativeQuickDropProof | W5/W6、ダブルクリック、再生位置、長さ、Front/Back、全長衝突 |
| NativeExpressionPresetProof | W7、MaxGap、overlap、offset、band予約、Excel/current Preset |
| NativeAssociationProof / NativeResyncProof | W8、Remark保持、一意target、現在Preset、部分成功Undo |
| NativeSelectionProof | W9、Target Companion / Point Emphasis、実Save/Preview/Place |
| NativeRangeProof | W10、複数選択の全区間、neutral Template、設定移行 |
| NativeBoundaryProof | W11、exact pair、許容差、明示基準位置、推測禁止 |
| NativeFinalUiProof | W12、数値保存、Style切替、検索、binding、4タブの通常/狭幅画像 |
| NativeAcceptanceProof | 完了stage・assembly版・全18Acceptance manifest |
| RunNative.ps1 | 起動したPIDだけの操作・時間上限・functional gate・release DLL identity |
| PackageVerified.ps1 | build clean、manifest、version、通常DLLと.ymmeの一致、payload/source検査 |

C#検証コードは`Ymm4Proof=true`でだけコンパイルします。通常配布DLLからNativeProof型が除外されることをPE metadataで検査します。製品の配置・Excel・UIコードは両ビルドで共通です。

## 成功の読み方

`proof-result.txt`の`PASS P1 P2 P3 P4 P5 P6 P7 P8 P9`は回帰結果です。**v0.4の完成判定にはこれだけでは足りません。** `proof-log.txt`のP1-P9、W3-W11、W12_UI、W12、V04、`v04-acceptance.json`の全18項目が必要です。W1はbaseline、W2は既存P経路内のadd-only/atomicity検査で、架空のW1/W2完了markerを要求しません。

配布ゲートはrelease/proofの0 Warning / 0 Error、release-plugin-loadedのbuild=distributionとSHA256、assembly0.4.0.0、`.ymme`の許可済み8ファイル、archive内DLLの同一性、source ZIPの必須entryを別途検査します。途中失敗時には段階・例外を残し、最終packageを生成しません。

`provenance.json`にはsource HEADとcheckout commit/treeを区別して保存します。PR実行のmerge checkoutとstaging pushが混同されないようにしてください。再実行ではrun_attemptも照合します。HTTP 504など取得前の失敗を、native assertion失敗と混同しないでください。

## Evidenceと境界

artifactはproofログ、acceptance、buildログ、Excel fixture、Timeline状態、通常DLLのロード記録、`.ymme`、source ZIP、SHA256、UI画像を含みます。UI画像は実hostのUserControl renderです。通常幅・狭幅・空欄表示を画像でもレビューし、寸法のAssertionだけで「切れていない」と断定しません。

全18項目とテストの対応は[Acceptance](../docs/V0.4_ACCEPTANCE.md)を参照してください。第三者PSD素材の全描画、物理マウス入力、デスクトップExcelの全操作、全DPI/テーマ、未検証YMM4版まで検証したとは主張しません。GitHub ActionのNode deprecation noticeと、コンパイラのWarning 0は別です。
