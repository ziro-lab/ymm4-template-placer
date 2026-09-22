param([Parameter(Mandatory=$true)][string]$OutputDir, [switch]$SelfTest)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest

# Independent consumer contract: never trust a producer's PASS or its own stage list alone.
$required=@('R1=PASS','R2=PASS','R3=PASS','R4=PASS','R5=PASS','R6=PASS','R7=PASS','R8=PASS',
 'R9_CORE=PASS','R9_UI=PASS','R10=PASS','R11=PASS','R12=PASS','R13=PASS','TEMPLATE_FIDELITY=PASS','RELATIVE_UIUX=PASS','R14_NATIVE=PASS',
 'V04=PASS','UX_ACCEPTANCE=PASS','UX_WORKFLOW_ACCEPTANCE=PASS')
$handsOnIds=@(1..9 | ForEach-Object { "A$_" }) + @(1..11 | ForEach-Object { "B$_" }) +
 @(1..12 | ForEach-Object { "C$_" }) + @(1..3 | ForEach-Object { "D$_" }) + @(1..8 | ForEach-Object { "E$_" })
$round2Ids=@(1..12 | ForEach-Object { "A$_" }) + @(1..10 | ForEach-Object { "B$_" }) +
 @(1..10 | ForEach-Object { "C$_" }) + @(1..10 | ForEach-Object { "D$_" }) + @(1..12 | ForEach-Object { "E$_" }) +
 @(1..13 | ForEach-Object { "F$_" }) + @(1..8 | ForEach-Object { "G$_" }) + @(1..4 | ForEach-Object { "H$_" })

function Assert-RelativeEvidence {
 param([string[]]$Lines, $Manifest, $UiuxManifest, $HandsOnManifest, $Round2Manifest)
 foreach ($marker in ($required + @('V042_ACCEPTANCE=PASS','HANDS_ON_UX_POLISH=PASS',
  'HANDS_ON_ROUND2_A=PASS','HANDS_ON_ROUND2_B=PASS','HANDS_ON_ROUND2_C=PASS','HANDS_ON_ROUND2_D=PASS','HANDS_ON_ROUND2_E=PASS','HANDS_ON_ROUND2=PASS'))) {
  if (@($Lines | Where-Object { $_ -ceq $marker }).Count -ne 1) { throw "Missing or duplicate native stage: $marker" }
 }
 if (@($Lines | Where-Object { $_ -cmatch '^ASSERT FAIL:|^FAIL(?:\s|$)' }).Count) { throw 'Native failure appears in the proof log' }
 if ($null -eq $Manifest -or $Manifest.schema -cne 'YMM4-Template-Placer-Relative-Acceptance/1' -or
     $Manifest.version -cne '0.5.0' -or $Manifest.result -cne 'PASS' -or $Manifest.host -cne 'YMM4 4.55.1.1 Lite') {
  throw 'Relative acceptance identity/version/result/host mismatch'
 }
 if (@($Manifest.checks).Count -ne 21 -or @($Manifest.checks | Where-Object { $_.result -cne 'PASS' -or
     [string]::IsNullOrWhiteSpace($_.requirement) -or [string]::IsNullOrWhiteSpace($_.evidence) }).Count) { throw 'Incomplete relative acceptance checks' }
 if ((@($Manifest.checks.id | Sort-Object) -join ',') -cne ((1..21) -join ',')) { throw 'Relative acceptance IDs missing or duplicated' }
 if (@($Manifest.required_native_stages).Count -ne $required.Count -or
     @(Compare-Object ($required | Sort-Object) ($Manifest.required_native_stages | Sort-Object) -CaseSensitive).Count) {
  throw 'Relative manifest stage contract is incomplete'
 }
 if ($null -eq $UiuxManifest -or $UiuxManifest.schema -cne 'YMM4-Template-Placer-Relative-UIUX/1' -or
     $UiuxManifest.version -cne '0.5.0' -or $UiuxManifest.result -cne 'PASS' -or $UiuxManifest.host -cne 'YMM4 4.55.1.1 Lite') {
  throw 'Relative UI/UX acceptance identity/version/result/host mismatch'
 }
 if (@($UiuxManifest.checks).Count -ne 10 -or @($UiuxManifest.checks | Where-Object { $_.result -cne 'PASS' -or
     [string]::IsNullOrWhiteSpace($_.requirement) -or [string]::IsNullOrWhiteSpace($_.evidence) }).Count) { throw 'Incomplete relative UI/UX acceptance checks' }
 if ((@($UiuxManifest.checks.id | Sort-Object) -join ',') -cne ((1..10) -join ',')) { throw 'Relative UI/UX acceptance IDs missing or duplicated' }
 if ($null -eq $HandsOnManifest -or $HandsOnManifest.schema -cne 'YMM4-Template-Placer-Hands-On-UX-Polish/1' -or
     $HandsOnManifest.version -cne '0.5.0' -or $HandsOnManifest.result -cne 'PASS' -or $HandsOnManifest.host -cne 'YMM4 4.55.1.1 Lite') {
  throw 'Hands-on UX polish identity/version/result/host mismatch'
 }
 if (@($HandsOnManifest.checks).Count -ne $handsOnIds.Count -or @($HandsOnManifest.checks | Where-Object {
     $_.result -cne 'PASS' -or [string]::IsNullOrWhiteSpace($_.id) -or [string]::IsNullOrWhiteSpace($_.evidence) }).Count) {
  throw 'Incomplete hands-on UX polish checks'
 }
 if ((@($HandsOnManifest.checks.id | Sort-Object) -join ',') -cne (($handsOnIds | Sort-Object) -join ',')) {
  throw 'Hands-on UX polish IDs missing or duplicated'
 }
 if ($null -eq $Round2Manifest -or $Round2Manifest.schema -cne 'YMM4-Template-Placer-Hands-On-Round2/1' -or
     $Round2Manifest.version -cne '0.5.0' -or $Round2Manifest.result -cne 'PASS' -or $Round2Manifest.host -cne 'YMM4 4.55.1.1 Lite') {
  throw 'Hands-on Round 2 identity/version/result/host mismatch'
 }
 if (@($Round2Manifest.checks).Count -ne $round2Ids.Count -or @($Round2Manifest.checks | Where-Object {
     $_.result -cne 'PASS' -or [string]::IsNullOrWhiteSpace($_.id) -or [string]::IsNullOrWhiteSpace($_.evidence) }).Count) {
  throw 'Incomplete Hands-on Round 2 checks'
 }
 if ((@($Round2Manifest.checks.id | Sort-Object) -join ',') -cne (($round2Ids | Sort-Object) -join ',')) {
  throw 'Hands-on Round 2 IDs missing or duplicated'
 }
}

$lines=@(Get-Content (Join-Path $OutputDir 'proof-log.txt'))
$relative=Get-Content -Raw (Join-Path $OutputDir 'v042-acceptance.json') | ConvertFrom-Json
$uiux=Get-Content -Raw (Join-Path $OutputDir 'v042-uiux-acceptance.json') | ConvertFrom-Json
$handsOn=Get-Content -Raw (Join-Path $OutputDir 'hands-on-ux-polish.json') | ConvertFrom-Json
$round2=Get-Content -Raw (Join-Path $OutputDir 'hands-on-round2.json') | ConvertFrom-Json
Assert-RelativeEvidence $lines $relative $uiux $handsOn $round2

if ($SelfTest) {
 $tests=[Collections.Generic.List[object]]::new()
 $mutations=[ordered]@{
  'missing R13 stage' = { param($m,$u,$h,$r) }
  'missing template fidelity stage' = { param($m,$u,$h,$r) }
  'missing UIUX stage' = { param($m,$u,$h,$r) }
  'missing final success' = { param($m,$u,$h,$r) }
  'duplicate native stage' = { param($m,$u,$h,$r) }
  'old manifest version' = { param($m,$u,$h,$r) $m.version='0.4.1' }
  'failed manifest check' = { param($m,$u,$h,$r) $m.checks[0].result='FAIL' }
  'missing manifest check' = { param($m,$u,$h,$r) $m.checks=@($m.checks | Select-Object -Skip 1) }
  'duplicate requirement ID' = { param($m,$u,$h,$r) $m.checks[1].id=$m.checks[0].id }
  'weakened manifest stages' = { param($m,$u,$h,$r) $m.required_native_stages=@($m.required_native_stages | Where-Object { $_ -cne 'TEMPLATE_FIDELITY=PASS' }) }
  'wrong native host' = { param($m,$u,$h,$r) $m.host='unverified-host' }
  'failed UIUX check' = { param($m,$u,$h,$r) $u.checks[0].result='FAIL' }
  'missing UIUX check' = { param($m,$u,$h,$r) $u.checks=@($u.checks | Select-Object -Skip 1) }
  'explicit failure in log' = { param($m,$u,$h,$r) }
  'missing hands-on stage' = { param($m,$u,$h,$r) }
  'duplicate hands-on stage' = { param($m,$u,$h,$r) }
  'stale hands-on manifest' = { param($m,$u,$h,$r) $h.version='0.4.1' }
  'failed hands-on check' = { param($m,$u,$h,$r) $h.checks[0].result='FAIL' }
  'missing hands-on check' = { param($m,$u,$h,$r) $h.checks=@($h.checks | Select-Object -Skip 1) }
  'duplicate hands-on ID' = { param($m,$u,$h,$r) $h.checks[1].id=$h.checks[0].id }
  'missing hands-on manifest' = { param($m,$u,$h,$r) }
  'missing round2 stage' = { param($m,$u,$h,$r) }
  'duplicate round2 stage' = { param($m,$u,$h,$r) }
  'stale round2 manifest' = { param($m,$u,$h,$r) $r.version='0.4.1' }
  'failed round2 check' = { param($m,$u,$h,$r) $r.checks[0].result='FAIL' }
  'missing round2 check' = { param($m,$u,$h,$r) $r.checks=@($r.checks | Select-Object -Skip 1) }
  'duplicate round2 ID' = { param($m,$u,$h,$r) $r.checks[1].id=$r.checks[0].id }
  'missing round2 manifest' = { param($m,$u,$h,$r) }
 }
 foreach ($name in $mutations.Keys) {
  $copy=$relative | ConvertTo-Json -Depth 20 | ConvertFrom-Json
  $uiuxCopy=$uiux | ConvertTo-Json -Depth 20 | ConvertFrom-Json
  $handsCopy=$handsOn | ConvertTo-Json -Depth 20 | ConvertFrom-Json
  $roundCopy=$round2 | ConvertTo-Json -Depth 20 | ConvertFrom-Json
  & $mutations[$name] $copy $uiuxCopy $handsCopy $roundCopy
  if ($name -eq 'missing hands-on manifest') { $handsCopy=$null }
  if ($name -eq 'missing round2 manifest') { $roundCopy=$null }
  $testLines=@($lines)
  switch ($name) {
   'missing R13 stage' { $testLines=@($lines | Where-Object { $_ -cne 'R13=PASS' }) }
   'missing template fidelity stage' { $testLines=@($lines | Where-Object { $_ -cne 'TEMPLATE_FIDELITY=PASS' }) }
   'missing UIUX stage' { $testLines=@($lines | Where-Object { $_ -cne 'RELATIVE_UIUX=PASS' }) }
   'missing final success' { $testLines=@($lines | Where-Object { $_ -cne 'V042_ACCEPTANCE=PASS' }) }
   'duplicate native stage' { $testLines=$lines + @('TEMPLATE_FIDELITY=PASS') }
   'explicit failure in log' { $testLines=$lines + @('ASSERT FAIL: deliberate negative fixture') }
   'missing hands-on stage' { $testLines=@($lines | Where-Object { $_ -cne 'HANDS_ON_UX_POLISH=PASS' }) }
   'duplicate hands-on stage' { $testLines=$lines + @('HANDS_ON_UX_POLISH=PASS') }
   'missing round2 stage' { $testLines=@($lines | Where-Object { $_ -cne 'HANDS_ON_ROUND2=PASS' }) }
   'duplicate round2 stage' { $testLines=$lines + @('HANDS_ON_ROUND2=PASS') }
  }
  $rejected=$false
  try { Assert-RelativeEvidence $testLines $copy $uiuxCopy $handsCopy $roundCopy } catch { $rejected=$true }
  if (-not $rejected) { throw "Evidence guard accepted invalid input: $name" }
  $tests.Add([ordered]@{name=$name; result='PASS_REJECTED'})
 }
 [ordered]@{schema='YMM4-Template-Placer-Evidence-Guard/1'; result='PASS'; positive_checks=1;
  negative_checks=$tests.Count; checks=$tests} | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $OutputDir 'evidence-guard-tests.json')
 Write-Host "Relative/hands-on/Round2 evidence guard: 1 valid input and $($tests.Count) rejection tests PASS"
}
$relative
