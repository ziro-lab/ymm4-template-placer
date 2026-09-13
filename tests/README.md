# Automated native integration tests

このリポジトリの主自動テストは、Windows上の**本物のYMM4**の現在Timeline・登録Template・標準Undo・実Plugin画面を使います。API stubを成功根拠にしません。

## Run

通常はGitHub Actionsの `native-yymm4-proof` を使用します。workflow_dispatch、または対象ソース変更のあるpush / PRで実行されます。

ローカルで同じテストを行う場合は、ユーザーの普段のYMM4とは別に、空のYMM4 4.55.1.1 Liteディレクトリを用意してください。fixtureを登録しプロセスを停止するため、普段の編集中プロジェクトに対して実行しないでください。

```powershell
$ymm = 'C:\IsolatedTest\YMM4\'
dotnet build src/Ymm4TemplatePlacer/Ymm4TemplatePlacer.csproj -c Release "-p:YMM4DirPath=$ymm" -p:Ymm4Proof=false -o out/package --nologo -warnaserror
dotnet build src/Ymm4TemplatePlacer/Ymm4TemplatePlacer.csproj -c Release "-p:YMM4DirPath=$ymm" -p:Ymm4Proof=true -o out/proof --nologo -warnaserror
$dest = Join-Path $ymm 'user/plugin/Ymm4TemplatePlacer'
New-Item -ItemType Directory -Force $dest | Out-Null
Copy-Item out/proof/Ymm4TemplatePlacer.dll,out/proof/DocumentFormat.OpenXml.dll,out/proof/DocumentFormat.OpenXml.Framework.dll,out/proof/Ymm4TemplatePlacer.deps.json $dest
./tests/RunNative.ps1 -Ymm4Dir $ymm -OutputDir (Join-Path $PWD 'out') -DistributionDir (Join-Path $PWD 'out/package')
```

CI scopeだけのテスト:

```powershell
./tests/CiScope.ps1 -SelfTest
```

## Files

- NativeProof.cs: readiness、native host adapter、登録constructor検査、結果・Assertion出力
- NativeUiProof.cs: native ToolArea表示、実ComboBox操作、ButtonAutomationPeer、画面取得
- GoldenPathProof.cs: P1〜P8、保持・失敗時無変更・標準Undo/Redo
- WorkbookVariantProof.cs: shared string、行の並べ替え、Template空欄
- RunNative.ps1: process-scoped起動、既知の起動dialog処理、時間上限、配布DLL identity検査
- CiScope.ps1: 重いnative処理を走らせる対象差分とそのself-test

すべてのC#検証コードは `Ymm4Proof=true` のビルドだけに含めます。通常配布DLLからNativeProof型が除外されていることをPE metadataで確認します。製品の配置・Excel・UIロジックはどちらのビルドも同じです。

## Evidence

`proof-result.txt` は全段階成功時だけ `PASS P1 P2 P3 P4 P5 P6 P7 P8` になります。途中のAssertionが失敗した場合は、段階と例外を出します。C#の実機Assertion数は `provenance.json` に記録します。

`proof-log.txt`、build-release / build-proof、native-plugin-ui.png、GoldenPath.xlsx、SharedStringsSorted.xlsx、final-timeline.txt、release-plugin-loaded.txtを保存します。Screenshotは透明背景のUserControl renderなので、画像ビューアでは白背景で表示すると読みやすくなります。

検証の限界と、物理キーボード・ExcelデスクトップUI・第三者キャラクター素材を検証したという主張をしないことは、[検証記録](../docs/VERIFICATION.md)に明記しています。
