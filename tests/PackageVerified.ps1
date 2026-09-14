param([string]$OutputDir='out')
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$version='0.4.0'
$package=Join-Path $OutputDir 'package'
$logPath=Join-Path $OutputDir 'proof-log.txt'
$log=Get-Content $logPath
foreach ($stage in @('P1','P2','P3','P4','P5','P6','P7','P8','P9','W3','W4','W5','W6','W7','W8','W9','W10','W11','W12_UI','W12','V04','WUX1','WUX2','WUX3','WUX4','WUX5','WUX6','WUX7','UX_ACCEPTANCE')) {
 if ($log -cnotcontains "$stage=PASS") { throw "Missing native success stage: $stage" }
}
$acceptance=Get-Content -Raw (Join-Path $OutputDir 'v04-acceptance.json') | ConvertFrom-Json
if ($acceptance.schema -ne 'YMM4-Template-Placer-Acceptance/1' -or $acceptance.version -ne $version -or $acceptance.result -ne 'PASS' -or
    $acceptance.profile_families -ne 5 -or @($acceptance.checks).Count -ne 18 -or @($acceptance.checks | Where-Object { $_.result -ne 'PASS' }).Count -ne 0) { throw 'Incomplete v0.4 acceptance manifest' }
if ((@($acceptance.checks.id | Sort-Object) -join ',') -ne ((1..18) -join ',')) { throw 'Acceptance IDs are missing or duplicated' }
$ux=Get-Content -Raw (Join-Path $OutputDir 'ux-acceptance.json') | ConvertFrom-Json
if ($ux.schema -ne 'YMM4-Template-Placer-Task-UX/1' -or $ux.version -ne $version -or $ux.result -ne 'PASS' -or
    @($ux.checks).Count -ne 12 -or @($ux.checks | Where-Object { $_.result -ne 'PASS' }).Count -ne 0 -or
    (@($ux.checks.id | Sort-Object) -join ',') -ne ((1..12) -join ',')) { throw 'Incomplete Task UX acceptance manifest' }
foreach ($build in @('build-release.txt','build-proof.txt')) {
 $text=Get-Content -Raw (Join-Path $OutputDir $build)
 if ($text -notmatch '(?m)^\s*0 Warning\(s\)' -or $text -notmatch '(?m)^\s*0 Error\(s\)') { throw "Build is not warning/error clean: $build" }
}
$dll=Join-Path $package 'Ymm4TemplatePlacer.dll'
$dllHash=(Get-FileHash $dll -Algorithm SHA256).Hash.ToLowerInvariant()
if ([Reflection.AssemblyName]::GetAssemblyName((Resolve-Path $dll).Path).Version.ToString() -ne '0.4.0.0') { throw 'Distribution assembly version mismatch' }
$smoke=Get-Content -Raw (Join-Path $OutputDir 'release-plugin-loaded.txt')
if ($smoke -notmatch '(?m)^build=distribution\r?$' -or $smoke -notmatch "(?m)^sha256=$dllHash\r?`$") { throw 'Release smoke does not identify this exact distribution DLL' }
Copy-Item docs/USAGE.md (Join-Path $package 'README.md')
Copy-Item THIRD_PARTY_NOTICES.md $package
Copy-Item (Join-Path $OutputDir 'v04-acceptance.json') $package
Copy-Item (Join-Path $OutputDir 'ux-acceptance.json') $package
if ((Get-Content -Raw (Join-Path $package 'README.md')) -notmatch '^# YMM4 Template Placer v0\.4\.0') { throw 'Obsolete package usage documentation' }
Remove-Item (Join-Path $package '*.pdb') -ErrorAction SilentlyContinue
$event=Get-Content -Raw $env:GITHUB_EVENT_PATH | ConvertFrom-Json
$sourceHead=if ($env:GITHUB_EVENT_NAME -eq 'pull_request') { $event.pull_request.head.sha } else { $env:GITHUB_SHA }
[ordered]@{
 schema='YMM4-Template-Placer-Provenance/1'; repository=$env:GITHUB_REPOSITORY; plugin_version=$version
 source_head=$sourceHead; checkout_commit=(git rev-parse HEAD); checkout_tree=(git rev-parse 'HEAD^{tree}')
 run_id=$env:GITHUB_RUN_ID; run_attempt=$env:GITHUB_RUN_ATTEMPT; run_url="https://github.com/$env:GITHUB_REPOSITORY/actions/runs/$env:GITHUB_RUN_ID"
 ymm4_version='4.55.1.1 Lite'; ymm4_zip_sha256='125860147cc33b831fc1a6d6ea996958001c2ead3b0d37f7d900251d5617db9b'
 result=(Get-Content -Raw (Join-Path $OutputDir 'proof-result.txt')).Trim(); v04_result=$acceptance.result
 native_assertions=@(Select-String -Path $logPath -Pattern '^ASSERT PASS:').Count; acceptance_requirements=18; ux_requirements=12; ux_result=$ux.result
 distribution_dll_sha256=$dllHash
} | ConvertTo-Json | Set-Content (Join-Path $OutputDir 'provenance.json')
Copy-Item (Join-Path $OutputDir 'provenance.json') $package
$expected=@('Ymm4TemplatePlacer.dll','Ymm4TemplatePlacer.deps.json','DocumentFormat.OpenXml.dll','DocumentFormat.OpenXml.Framework.dll','README.md','THIRD_PARTY_NOTICES.md','provenance.json','v04-acceptance.json','ux-acceptance.json')
$files=@(Get-ChildItem $package -File -Recurse)
if ($files.Count -ne $expected.Count -or @($files | Where-Object { $_.Name -notin $expected -or $_.Directory.FullName -ne (Resolve-Path $package).Path }).Count -ne 0) { throw 'Unexpected, nested or missing distributable content' }
$archive=Join-Path $OutputDir "Ymm4TemplatePlacer-v$version.ymme"
$temporary=Join-Path $OutputDir "Ymm4TemplatePlacer-v$version.zip"
Compress-Archive -Path (Join-Path $package '*') -DestinationPath $temporary -Force
Move-Item $temporary $archive -Force
$zip=[IO.Compression.ZipFile]::OpenRead((Resolve-Path $archive).Path)
try {
 if ($zip.Entries.Count -ne $expected.Count -or @($zip.Entries | Where-Object { $_.FullName -notin $expected }).Count -ne 0) { throw 'Unexpected .ymme archive entries' }
 $entry=$zip.GetEntry('Ymm4TemplatePlacer.dll'); if ($null -eq $entry) { throw 'Missing archived plugin DLL' }
 $stream=$entry.Open(); $sha=[Security.Cryptography.SHA256]::Create()
 try { $archivedHash=[Convert]::ToHexString($sha.ComputeHash($stream)).ToLowerInvariant() }
 finally { $stream.Dispose(); $sha.Dispose() }
 if ($archivedHash -ne $dllHash) { throw 'Archived DLL does not match native-smoked release DLL' }
} finally { $zip.Dispose() }
Copy-Item $dll (Join-Path $OutputDir 'Ymm4TemplatePlacer.dll')
$sourceArchive=Join-Path $OutputDir 'Ymm4TemplatePlacer-source.zip'
git archive --format=zip -o $sourceArchive HEAD
if ($LASTEXITCODE -ne 0) { throw 'Source archive failed' }
$sourceZip=[IO.Compression.ZipFile]::OpenRead((Resolve-Path $sourceArchive).Path)
try {
 foreach ($name in @('AGENTS.md','docs/DESIGN.md','docs/USAGE.md','src/Ymm4TemplatePlacer/Ymm4TemplatePlacer.csproj','tests/NativeV04Proof.cs','tests/NativeAcceptanceProof.cs','tests/NativeTaskUxFinalProof.cs','docs/TASK_UX.md','tests/PackageVerified.ps1')) {
  if ($null -eq $sourceZip.GetEntry($name)) { throw "Missing source archive entry: $name" }
 }
} finally { $sourceZip.Dispose() }
[ordered]@{ result='PASS'; version=$version; native_stages='P1-P9,W3-W12,V04,WUX1-WUX7'; acceptance_requirements=18; ux_requirements=12; payload_files=$expected; archived_dll_sha256=$archivedHash; source_archive='Ymm4TemplatePlacer-source.zip' } |
 ConvertTo-Json | Set-Content (Join-Path $OutputDir 'package-checks.json')
Get-FileHash (Join-Path $OutputDir 'Ymm4TemplatePlacer*') -Algorithm SHA256 | Select-Object @{Name='File';Expression={Split-Path $_.Path -Leaf}},Hash | ConvertTo-Json | Set-Content (Join-Path $OutputDir 'SHA256.json')
Write-Host 'Verified v0.4.0 .ymme / source / provenance packaging: PASS'
