param([string]$OutputDir='out')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$version='0.4.2'
$installFolder='Ymm4TemplatePlacer'
$project=[xml](Get-Content -Raw 'src/Ymm4TemplatePlacer/Ymm4TemplatePlacer.csproj')
if ($project.Project.PropertyGroup.Version -cne $version) { throw 'Project/package version mismatch' }
$relative = & "$PSScriptRoot/ValidateRelativeEvidence.ps1" -OutputDir $OutputDir -SelfTest
$relativeUiux=Get-Content -Raw (Join-Path $OutputDir 'v042-uiux-acceptance.json') | ConvertFrom-Json
$handsOn=Get-Content -Raw (Join-Path $OutputDir 'hands-on-ux-polish.json') | ConvertFrom-Json
$round2=Get-Content -Raw (Join-Path $OutputDir 'hands-on-round2.json') | ConvertFrom-Json
$guard=Get-Content -Raw (Join-Path $OutputDir 'evidence-guard-tests.json') | ConvertFrom-Json
if ($guard.result -cne 'PASS' -or $guard.positive_checks -ne 1 -or $guard.negative_checks -ne 28 -or @($guard.checks | Where-Object { $_.result -cne 'PASS_REJECTED' }).Count) { throw 'Relative/hands-on/Round2 evidence guard self-test is incomplete' }
$package=Join-Path $OutputDir 'package'
$logPath=Join-Path $OutputDir 'proof-log.txt'
$log=Get-Content $logPath
if ((Get-Content -Raw (Join-Path $OutputDir 'proof-result.txt')).Trim() -cne 'PASS P1 P2 P3 P4 P5 P6 P7 P8 P9') { throw 'Native result is not a complete PASS' }
$stages=@('P1','P2','P3','P4','P5','P6','P7','P8','P9','W3','W4','W5','W6','W7','W8','W9','W10','W11','W12_UI','W12_SELECTORS','W12','V04') + (1..13 | ForEach-Object { "WUX$_" }) + @('UX_ACCEPTANCE','UX_WORKFLOW_ACCEPTANCE','TEMPLATE_FIDELITY','HANDS_ON_H1_H2','HANDS_ON_H3_H4_H5','HANDS_ON_UX_POLISH','HANDS_ON_ROUND2_A','HANDS_ON_ROUND2_B','HANDS_ON_ROUND2_C','HANDS_ON_ROUND2_D','HANDS_ON_ROUND2_E','HANDS_ON_ROUND2')
foreach ($stage in $stages) {
 if ($log -cnotcontains "$stage=PASS") { throw "Missing native success stage: $stage" }
}
$acceptance=Get-Content -Raw (Join-Path $OutputDir 'v04-acceptance.json') | ConvertFrom-Json
if ($acceptance.schema -ne 'YMM4-Template-Placer-Acceptance/1' -or $acceptance.version -ne $version -or $acceptance.result -ne 'PASS' -or
    $acceptance.profile_families -ne 5 -or @($acceptance.checks).Count -ne 18 -or @($acceptance.checks | Where-Object { $_.result -ne 'PASS' }).Count -ne 0) { throw 'Incomplete v0.4.2 core acceptance manifest' }
if ((@($acceptance.checks.id | Sort-Object) -join ',') -ne ((1..18) -join ',')) { throw 'Acceptance IDs are missing or duplicated' }
$ux=Get-Content -Raw (Join-Path $OutputDir 'ux-acceptance.json') | ConvertFrom-Json
if ($ux.schema -ne 'YMM4-Template-Placer-Task-UX/1' -or $ux.version -ne '0.4.0' -or $ux.result -ne 'PASS' -or
    @($ux.checks).Count -ne 12 -or @($ux.checks | Where-Object { $_.result -ne 'PASS' }).Count -ne 0 -or
    (@($ux.checks.id | Sort-Object) -join ',') -ne ((1..12) -join ',')) { throw 'Incomplete retained Task UX acceptance manifest' }
$workflow=Get-Content -Raw (Join-Path $OutputDir 'ux-workflow-acceptance.json') | ConvertFrom-Json
if ($workflow.schema -ne 'YMM4-Template-Placer-UX-Workflow/1' -or $workflow.version -ne $version -or $workflow.result -ne 'PASS' -or
    @($workflow.checks).Count -ne 10 -or @($workflow.checks | Where-Object { $_.result -ne 'PASS' }).Count -ne 0 -or
    (@($workflow.checks.id | Sort-Object) -join ',') -ne ((1..10) -join ',')) { throw 'Incomplete v0.4.2 UX workflow acceptance manifest' }
foreach ($build in @('build-release.txt','build-proof.txt')) {
 $text=Get-Content -Raw (Join-Path $OutputDir $build)
 if ($text -notmatch '(?m)^\s*0 Warning\(s\)' -or $text -notmatch '(?m)^\s*0 Error\(s\)') { throw "Build is not warning/error clean: $build" }
}
$dll=Join-Path $package 'Ymm4TemplatePlacer.dll'
$dllHash=(Get-FileHash $dll -Algorithm SHA256).Hash.ToLowerInvariant()
if ([Reflection.AssemblyName]::GetAssemblyName((Resolve-Path $dll).Path).Version.ToString() -ne '0.4.2.0') { throw 'Distribution assembly version mismatch' }
$smoke=Get-Content -Raw (Join-Path $OutputDir 'release-plugin-loaded.txt')
if ($smoke -notmatch '(?m)^build=distribution\r?$' -or $smoke -notmatch "(?m)^sha256=$dllHash\r?`$") { throw 'Release smoke does not identify this exact distribution DLL' }
Copy-Item docs/USAGE.md (Join-Path $package 'README.md')
Copy-Item THIRD_PARTY_NOTICES.md $package
Copy-Item (Join-Path $OutputDir 'v04-acceptance.json') $package
Copy-Item (Join-Path $OutputDir 'ux-acceptance.json') $package
Copy-Item (Join-Path $OutputDir 'ux-workflow-acceptance.json') $package
Copy-Item (Join-Path $OutputDir 'v042-acceptance.json') $package
Copy-Item (Join-Path $OutputDir 'v042-uiux-acceptance.json') $package
Copy-Item (Join-Path $OutputDir 'hands-on-ux-polish.json') $package
Copy-Item (Join-Path $OutputDir 'hands-on-round2.json') $package
if ((Get-Content -Raw (Join-Path $package 'README.md')) -notmatch '^# YMM4 Template Placer v0\.4\.2') { throw 'Obsolete package usage documentation' }
Remove-Item (Join-Path $package '*.pdb') -ErrorAction SilentlyContinue
$event=Get-Content -Raw $env:GITHUB_EVENT_PATH | ConvertFrom-Json
$sourceHead=if ($env:GITHUB_EVENT_NAME -eq 'pull_request') { $event.pull_request.head.sha } else { $env:GITHUB_SHA }
[ordered]@{
 schema='YMM4-Template-Placer-Provenance/1'; repository=$env:GITHUB_REPOSITORY; plugin_version=$version
 source_head=$sourceHead; checkout_commit=(git rev-parse HEAD); checkout_tree=(git rev-parse 'HEAD^{tree}')
 run_id=$env:GITHUB_RUN_ID; run_attempt=$env:GITHUB_RUN_ATTEMPT; run_url="https://github.com/$env:GITHUB_REPOSITORY/actions/runs/$env:GITHUB_RUN_ID"
 ymm4_version='4.55.1.1 Lite'; ymm4_zip_sha256='125860147cc33b831fc1a6d6ea996958001c2ead3b0d37f7d900251d5617db9b'
 result=(Get-Content -Raw (Join-Path $OutputDir 'proof-result.txt')).Trim(); v04_result=$acceptance.result
 native_assertions=@(Select-String -Path $logPath -Pattern '^ASSERT PASS:').Count; acceptance_requirements=18
 base_task_ux_version=$ux.version; base_task_ux_requirements=12; base_task_ux_result=$ux.result
 ux_workflow_version=$workflow.version; ux_workflow_requirements=10; ux_workflow_result=$workflow.result
 relative_version=$relative.version; relative_result=$relative.result; relative_requirements=@($relative.checks).Count
 relative_uiux_version=$relativeUiux.version; relative_uiux_result=$relativeUiux.result; relative_uiux_requirements=@($relativeUiux.checks).Count
 relative_native_stages=$relative.required_native_stages; template_fidelity_result=if($log -ccontains 'TEMPLATE_FIDELITY=PASS'){'PASS'}else{'FAIL'}; evidence_guard_negative_checks=$guard.negative_checks
 hands_on_ux_version=$handsOn.version; hands_on_ux_result=$handsOn.result; hands_on_ux_requirements=@($handsOn.checks).Count
 hands_on_round2_version=$round2.version; hands_on_round2_result=$round2.result; hands_on_round2_native_requirements=@($round2.checks).Count
 distribution_dll_sha256=$dllHash; ymme_install_folder=$installFolder
} | ConvertTo-Json | Set-Content (Join-Path $OutputDir 'provenance.json')
Copy-Item (Join-Path $OutputDir 'provenance.json') $package
$expected=@('Ymm4TemplatePlacer.dll','Ymm4TemplatePlacer.deps.json','DocumentFormat.OpenXml.dll','DocumentFormat.OpenXml.Framework.dll','README.md','THIRD_PARTY_NOTICES.md','provenance.json','v04-acceptance.json','ux-acceptance.json','ux-workflow-acceptance.json','v042-acceptance.json','v042-uiux-acceptance.json','hands-on-ux-polish.json','hands-on-round2.json')
$files=@(Get-ChildItem $package -File -Recurse)
if ($files.Count -ne $expected.Count -or @($files | Where-Object { $_.Name -notin $expected -or $_.Directory.FullName -ne (Resolve-Path $package).Path }).Count -ne 0) { throw 'Unexpected, nested or missing distributable content' }

$archive=Join-Path $OutputDir "Ymm4TemplatePlacer-v$version.ymme"
$temporary=Join-Path $OutputDir "Ymm4TemplatePlacer-v$version.zip"
$ymmeStage=Join-Path $OutputDir 'ymme-stage'
$ymmeRoot=Join-Path $ymmeStage $installFolder
Remove-Item $ymmeStage -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $temporary -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $ymmeRoot | Out-Null
foreach ($name in $expected) { Copy-Item (Join-Path $package $name) (Join-Path $ymmeRoot $name) }
[IO.Compression.ZipFile]::CreateFromDirectory((Resolve-Path $ymmeStage).Path, $temporary, [IO.Compression.CompressionLevel]::Optimal, $false)
Move-Item $temporary $archive -Force
$zip=[IO.Compression.ZipFile]::OpenRead((Resolve-Path $archive).Path)
$expectedArchive=@($expected | ForEach-Object { "$installFolder/$_" })
try {
 $archiveFiles=@($zip.Entries | Where-Object { -not [string]::IsNullOrEmpty($_.Name) } | ForEach-Object { $_.FullName.Replace('\\','/') })
 if ($archiveFiles.Count -ne $expectedArchive.Count -or @(Compare-Object ($archiveFiles | Sort-Object) ($expectedArchive | Sort-Object)).Count -ne 0) {
  throw "Unexpected .ymme archive layout. Every payload file must live under $installFolder/."
 }
 if ($zip.GetEntry('Ymm4TemplatePlacer.dll') -ne $null) { throw 'Flat .ymme root detected; this would install beside older version-named folders.' }
 $entry=$zip.GetEntry("$installFolder/Ymm4TemplatePlacer.dll"); if ($null -eq $entry) { throw 'Missing archived plugin DLL under stable install folder' }
 $stream=$entry.Open(); $sha=[Security.Cryptography.SHA256]::Create()
 try { $archivedHash=[Convert]::ToHexString($sha.ComputeHash($stream)).ToLowerInvariant() }
 finally { $stream.Dispose(); $sha.Dispose() }
 if ($archivedHash -ne $dllHash) { throw 'Archived DLL does not match native-smoked release DLL' }
} finally { $zip.Dispose() }
Remove-Item $ymmeStage -Recurse -Force -ErrorAction SilentlyContinue

Copy-Item $dll (Join-Path $OutputDir 'Ymm4TemplatePlacer.dll')
$sourceArchive=Join-Path $OutputDir 'Ymm4TemplatePlacer-source.zip'
git archive --format=zip -o $sourceArchive HEAD
if ($LASTEXITCODE -ne 0) { throw 'Source archive failed' }
$sourceZip=[IO.Compression.ZipFile]::OpenRead((Resolve-Path $sourceArchive).Path)
try {
 foreach ($name in @('AGENTS.md','docs/DESIGN.md','docs/USAGE.md','docs/USAGE_V0.4.1.md','docs/V0.4.2_RELATIVE_PALETTE_DESIGN.md','docs/V0.4.2_UIUX_MENTAL_MODEL.md','docs/V0.4.2_ROADMAP.md','docs/V0.4.2_CANDIDATE.md','docs/V0.4.1_UX_WORKFLOW_DESIGN.md','src/Ymm4TemplatePlacer/Ymm4TemplatePlacer.csproj','tests/NativeV04Proof.cs','tests/NativeAcceptanceProof.cs','tests/NativeTaskUxFinalProof.cs','tests/NativeWorkflowAcceptanceProof.cs','docs/TASK_UX.md','tests/NativeRelativeFinalProof.cs','tests/NativeRelativeUiUxProof.cs','tests/NativeTemplateFidelityProof.cs','tests/NativeHandsOnUxPolishProof.cs','tests/NativeHandsOnWorkflowProof.cs','tests/NativeHandsOnRound2InputProof.cs','tests/NativeHandsOnRound2SetProof.cs','tests/NativeHandsOnRound2SettingsProof.cs','tests/NativeHandsOnRound2TileProof.cs','tests/NativeHandsOnRound2ExpressionProof.cs','tests/NativeHandsOnRound2Proof.cs','tests/ValidateRelativeEvidence.ps1','tests/PackageVerified.ps1','docs/V0.4.2_HANDS_ON_UX_POLISH_WORKPLAN.md','docs/V0.4.2_HANDS_ON_UX_POLISH_ACCEPTANCE.md','docs/V0.4.2_HANDS_ON_UX_POLISH_P0_HOST_SURFACE.md','docs/V0.4.2_HANDS_ON_ROUND2_DESIGN.md','docs/V0.4.2_HANDS_ON_ROUND2_HOST_EVIDENCE.md','docs/V0.4.2_HANDS_ON_ROUND2_WORKPLAN.md','docs/V0.4.2_HANDS_ON_ROUND2_ACCEPTANCE.md','docs/V0.4.2_HANDS_ON_ROUND2_IMPLEMENTATION_PREP.md')) {
  if ($null -eq $sourceZip.GetEntry($name)) { throw "Missing source archive entry: $name" }
 }
} finally { $sourceZip.Dispose() }
[ordered]@{
 result='PASS'; version=$version; native_stages='P1-P9,W3-W12,V04,WUX1-WUX13,R1-R14,TEMPLATE_FIDELITY,RELATIVE_UIUX,V042_ACCEPTANCE,HANDS_ON_UX_POLISH,HANDS_ON_ROUND2_A-E,HANDS_ON_ROUND2'; acceptance_requirements=18
 relative_requirements=21; relative_result=$relative.result; relative_uiux_requirements=10; relative_uiux_result=$relativeUiux.result; template_fidelity_result='PASS'; evidence_guard_negative_checks=$guard.negative_checks
 hands_on_ux_requirements=@($handsOn.checks).Count; hands_on_ux_result=$handsOn.result; hands_on_round2_native_requirements=@($round2.checks).Count; hands_on_round2_result=$round2.result
 base_task_ux_requirements=12; ux_workflow_requirements=10; payload_files=$expected; archived_dll_sha256=$archivedHash
 source_archive='Ymm4TemplatePlacer-source.zip'; ymme_install_folder=$installFolder; ymme_file_entries=$expectedArchive
} | ConvertTo-Json | Set-Content (Join-Path $OutputDir 'package-checks.json')
Get-FileHash (Join-Path $OutputDir 'Ymm4TemplatePlacer*') -Algorithm SHA256 | Select-Object @{Name='File';Expression={Split-Path $_.Path -Leaf}},Hash | ConvertTo-Json | Set-Content (Join-Path $OutputDir 'SHA256.json')
Write-Host "Verified v0.4.2 .ymme stable install folder '$installFolder' / source / provenance packaging: PASS"
