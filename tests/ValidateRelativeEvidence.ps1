param([Parameter(Mandatory=$true)][string]$OutputDir, [switch]$SelfTest)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest

# Independent consumer contract: never trust a producer's PASS or its own stage list alone.
$required=@('R1=PASS','R2=PASS','R3=PASS','R4=PASS','R5=PASS','R6=PASS','R7=PASS','R8=PASS',
 'R9_CORE=PASS','R9_UI=PASS','R10=PASS','R11=PASS','R12=PASS','R13=PASS','RELATIVE_UIUX=PASS','R14_NATIVE=PASS',
 'V04=PASS','UX_ACCEPTANCE=PASS','UX_WORKFLOW_ACCEPTANCE=PASS')
function Assert-RelativeEvidence {
 param([string[]]$Lines, $Manifest, $UiuxManifest)
 foreach ($marker in ($required + @('V042_ACCEPTANCE=PASS'))) {
  if (@($Lines | Where-Object { $_ -ceq $marker }).Count -ne 1) { throw "Missing or duplicate native stage: $marker" }
 }
 if (@($Lines | Where-Object { $_ -cmatch '^ASSERT FAIL:|^FAIL(?:\s|$)' }).Count) { throw 'Native failure appears in the proof log' }
 if ($null -eq $Manifest -or $Manifest.schema -cne 'YMM4-Template-Placer-Relative-Acceptance/1' -or
     $Manifest.version -cne '0.4.2' -or $Manifest.result -cne 'PASS' -or $Manifest.host -cne 'YMM4 4.55.1.1 Lite') {
  throw 'Relative acceptance identity/version/result/host mismatch'
 }
 if (@($Manifest.checks).Count -ne 20 -or @($Manifest.checks | Where-Object { $_.result -cne 'PASS' -or
     [string]::IsNullOrWhiteSpace($_.requirement) -or [string]::IsNullOrWhiteSpace($_.evidence) }).Count) { throw 'Incomplete relative acceptance checks' }
 if ((@($Manifest.checks.id | Sort-Object) -join ',') -cne ((1..20) -join ',')) { throw 'Relative acceptance IDs missing or duplicated' }
 if (@($Manifest.required_native_stages).Count -ne $required.Count -or
     @(Compare-Object ($required | Sort-Object) ($Manifest.required_native_stages | Sort-Object) -CaseSensitive).Count) {
  throw 'Relative manifest stage contract is incomplete'
 }
 if ($null -eq $UiuxManifest -or $UiuxManifest.schema -cne 'YMM4-Template-Placer-Relative-UIUX/1' -or
     $UiuxManifest.version -cne '0.4.2' -or $UiuxManifest.result -cne 'PASS' -or $UiuxManifest.host -cne 'YMM4 4.55.1.1 Lite') {
  throw 'Relative UI/UX acceptance identity/version/result/host mismatch'
 }
 if (@($UiuxManifest.checks).Count -ne 10 -or @($UiuxManifest.checks | Where-Object { $_.result -cne 'PASS' -or
     [string]::IsNullOrWhiteSpace($_.requirement) -or [string]::IsNullOrWhiteSpace($_.evidence) }).Count) { throw 'Incomplete relative UI/UX acceptance checks' }
 if ((@($UiuxManifest.checks.id | Sort-Object) -join ',') -cne ((1..10) -join ',')) { throw 'Relative UI/UX acceptance IDs missing or duplicated' }
}
$lines=@(Get-Content (Join-Path $OutputDir 'proof-log.txt'))
$relative=Get-Content -Raw (Join-Path $OutputDir 'v042-acceptance.json') | ConvertFrom-Json
$uiux=Get-Content -Raw (Join-Path $OutputDir 'v042-uiux-acceptance.json') | ConvertFrom-Json
Assert-RelativeEvidence $lines $relative $uiux
if ($SelfTest) {
 $tests=[Collections.Generic.List[object]]::new()
 $mutations=[ordered]@{
  'missing R13 stage' = { param($m,$u) }
  'missing UIUX stage' = { param($m,$u) }
  'missing final success' = { param($m,$u) }
  'duplicate native stage' = { param($m,$u) }
  'old manifest version' = { param($m,$u) $m.version='0.4.1' }
  'failed manifest check' = { param($m,$u) $m.checks[0].result='FAIL' }
  'missing manifest check' = { param($m,$u) $m.checks=@($m.checks | Select-Object -Skip 1) }
  'duplicate requirement ID' = { param($m,$u) $m.checks[1].id=$m.checks[0].id }
  'weakened manifest stages' = { param($m,$u) $m.required_native_stages=@($m.required_native_stages | Where-Object { $_ -cne 'R13=PASS' }) }
  'wrong native host' = { param($m,$u) $m.host='unverified-host' }
  'failed UIUX check' = { param($m,$u) $u.checks[0].result='FAIL' }
  'missing UIUX check' = { param($m,$u) $u.checks=@($u.checks | Select-Object -Skip 1) }
  'explicit failure in log' = { param($m,$u) }
 }
 foreach ($name in $mutations.Keys) {
  $copy=$relative | ConvertTo-Json -Depth 20 | ConvertFrom-Json
  $uiuxCopy=$uiux | ConvertTo-Json -Depth 20 | ConvertFrom-Json
  & $mutations[$name] $copy $uiuxCopy
  $testLines=@($lines)
  switch ($name) {
   'missing R13 stage' { $testLines=@($lines | Where-Object { $_ -cne 'R13=PASS' }) }
   'missing UIUX stage' { $testLines=@($lines | Where-Object { $_ -cne 'RELATIVE_UIUX=PASS' }) }
   'missing final success' { $testLines=@($lines | Where-Object { $_ -cne 'V042_ACCEPTANCE=PASS' }) }
   'duplicate native stage' { $testLines=$lines + @('R13=PASS') }
   'explicit failure in log' { $testLines=$lines + @('ASSERT FAIL: deliberate negative fixture') }
  }
  $rejected=$false
  try { Assert-RelativeEvidence $testLines $copy $uiuxCopy } catch { $rejected=$true }
  if (-not $rejected) { throw "Evidence guard accepted invalid input: $name" }
  $tests.Add([ordered]@{name=$name; result='PASS_REJECTED'})
 }
 [ordered]@{schema='YMM4-Template-Placer-Evidence-Guard/1'; result='PASS'; positive_checks=1;
  negative_checks=$tests.Count; checks=$tests} | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $OutputDir 'evidence-guard-tests.json')
 Write-Host "Relative evidence guard: 1 valid input and $($tests.Count) rejection tests PASS"
}
$relative
