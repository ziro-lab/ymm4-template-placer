param([Parameter(Mandatory=$true)][string]$OutputDir, [switch]$SelfTest,
 [string]$ExpectedSourceHead, [string]$ExpectedCheckoutTree, [string]$ExpectedRunId, [string]$ExpectedRunAttempt)
$ErrorActionPreference='Stop'
Set-StrictMode -Version Latest
$specs=[ordered]@{appearance=@('A',10); shortcuts=@('B',15); settings=@('C',8); layer=@('D',12); freshness=@('E',10); navigation=@('F',15)}
$ids=@(foreach($s in $specs.Values){1..([int]$s[1]) | ForEach-Object { "$($s[0])$_" }}) + @(1..9 | ForEach-Object {"G$_"})
$phaseNames=@($specs.Keys | ForEach-Object {"hands-on-round3-$_.json"})
if (-not $ExpectedSourceHead) {
 if ($env:GITHUB_EVENT_NAME -eq 'pull_request') { $ExpectedSourceHead=(Get-Content -Raw $env:GITHUB_EVENT_PATH | ConvertFrom-Json).pull_request.head.sha }
 else { $ExpectedSourceHead=$env:GITHUB_SHA }
}
if (-not $ExpectedCheckoutTree) { $ExpectedCheckoutTree=git rev-parse 'HEAD^{tree}'; if($LASTEXITCODE -ne 0){throw 'Cannot resolve tested tree'} }
if (-not $ExpectedRunId) { $ExpectedRunId=$env:GITHUB_RUN_ID }
if (-not $ExpectedRunAttempt) { $ExpectedRunAttempt=$env:GITHUB_RUN_ATTEMPT }
if ($ExpectedSourceHead -cnotmatch '^[a-f0-9]{40}$' -or $ExpectedCheckoutTree -cnotmatch '^[a-f0-9]{40}$' -or
 $ExpectedRunId -notmatch '^\d+$' -or $ExpectedRunAttempt -notmatch '^[1-9]\d*$') { throw 'Round3 evidence requires explicit current source/tree/run/attempt context' }
$context=@{source=$ExpectedSourceHead; tree=$ExpectedCheckoutTree; run=$ExpectedRunId; attempt=$ExpectedRunAttempt}
function Assert-Round3Evidence {
 param([string[]]$Lines,$Manifest,$Phases,$Playback,$Digests,[string]$PlaybackDigest)
 foreach($marker in (@('HANDS_ON_ROUND3=PASS') + @('A','B','C','D','E','F' | ForEach-Object {"HANDS_ON_ROUND3_$_=PASS"}))) {
  if (@($Lines | Where-Object {$_ -ceq $marker}).Count -ne 1) { throw "Missing/duplicate Round3 stage: $marker" }
 }
 if (@($Lines | Where-Object {$_ -cmatch '^ASSERT FAIL:|^FAIL(?:\s|$)'}).Count) { throw 'Failed native proof cannot be packaged' }
 if ($null -eq $Manifest -or $Manifest.schema -cne 'YMM4-Template-Placer-Hands-On-Round3/1' -or
     $Manifest.version -cne '0.5.0' -or $Manifest.host -cne 'YMM4 4.55.1.1 Lite' -or $Manifest.result -cne 'PASS') { throw 'Round3 manifest identity mismatch' }
 if ($Manifest.source_head -cne $context.source -or $Manifest.checkout_tree -cne $context.tree -or
     [string]$Manifest.run_id -cne $context.run -or [string]$Manifest.run_attempt -cne $context.attempt) { throw 'Stale Round3 source/tree/run/attempt' }
 if (@($Manifest.checks).Count -ne 79 -or (@($Manifest.checks.id | Sort-Object) -join ',') -cne (($ids | Sort-Object) -join ',')) { throw 'Missing/duplicate Round3 check IDs' }
 foreach($check in $Manifest.checks) {
  if ($check.result -cne 'PASS' -or [string]::IsNullOrWhiteSpace($check.evidence)) { throw 'Incomplete Round3 assertion' }
  $line="ASSERT PASS: R3 $($check.id): $($check.evidence)"
  if (@($Lines | Where-Object {$_ -ceq $line}).Count -ne 1) { throw "No unique matching native assertion: $($check.id)" }
 }
 if (@($Manifest.phases).Count -ne 6 -or (($Manifest.phases.file | Sort-Object) -join ',') -cne (($phaseNames | Sort-Object) -join ',')) { throw 'Missing/duplicate phase references' }
 foreach($suffix in $specs.Keys) {
  $spec=$specs[$suffix]; $file="hands-on-round3-$suffix.json"; $phase=$Phases[$file]
  $ref=@($Manifest.phases | Where-Object {$_.file -ceq $file})[0]
  if ($null -eq $phase -or $phase.schema -cne 'YMM4-Template-Placer-Round3-Phase/1' -or $phase.phase -cne $spec[0] -or
      $phase.host -cne 'YMM4 4.55.1.1 Lite' -or $phase.result -cne 'PASS') { throw "Wrong phase identity: $file" }
  $phaseIds=@(1..([int]$spec[1]) | ForEach-Object {"$($spec[0])$_"})
  if (@($phase.checks).Count -ne $spec[1] -or (@($phase.checks.id | Sort-Object) -join ',') -cne (($phaseIds | Sort-Object) -join ',')) { throw "Incomplete phase checks: $file" }
  if ($ref.sha256 -cnotmatch '^[a-f0-9]{64}$' -or $ref.sha256 -cne $Digests[$file]) { throw "Phase bytes do not match native manifest: $file" }
  foreach($check in $phase.checks) {
   $same=@($Manifest.checks | Where-Object {$_.id -ceq $check.id})[0]
   if ($check.result -cne 'PASS' -or $check.evidence -cne $same.evidence) { throw "Phase/native assertion mismatch: $($check.id)" }
  }
 }
 if ($Manifest.playback.file -cne 'hands-on-round3-playback-observation.json' -or $Manifest.playback.sha256 -cne $PlaybackDigest -or
     $null -eq $Playback -or $Playback.schema -cne 'YMM4-Template-Placer-Round3-Playback/1' -or $Playback.result -cne 'PASS' -or
     $Playback.route -cne 'PreviewViewModel.SeekAsync(int)' -or $Playback.viewport -cne 'ContainFrameInViewport(int)/ScrollFrame(int)' -or
     $Playback.target -ne 180 -or @($Playback.timeline_only).Count -eq 0 -or @($Playback.product_navigation).Count -eq 0) { throw 'Missing/invalid raw playback evidence' }
 if ([Math]::Abs([long]$Playback.timeline_only[0] - 180) -le 15 -or [Math]::Abs([long]$Playback.product_navigation[0] - 180) -gt 15) {
  throw 'Playback evidence does not distinguish stale Timeline-only from synchronized product start'
 }
}
$lines=@(Get-Content (Join-Path $OutputDir 'proof-log.txt'))
$manifest=Get-Content -Raw (Join-Path $OutputDir 'hands-on-round3.json') | ConvertFrom-Json
$phases=@{}; $digests=@{}
foreach($file in $phaseNames) {
 $path=Join-Path $OutputDir $file
 $phases[$file]=Get-Content -Raw $path | ConvertFrom-Json
 $digests[$file]=(Get-FileHash $path -Algorithm SHA256).Hash.ToLowerInvariant()
}
$playPath=Join-Path $OutputDir 'hands-on-round3-playback-observation.json'
$playback=Get-Content -Raw $playPath | ConvertFrom-Json
$playDigest=(Get-FileHash $playPath -Algorithm SHA256).Hash.ToLowerInvariant()
Assert-Round3Evidence $lines $manifest $phases $playback $digests $playDigest
if ($SelfTest) {
 $tests=[Collections.Generic.List[object]]::new()
 $cases=@('missing stage','duplicate stage','missing manifest','stale source','stale tree','stale run','stale attempt','wrong host',
  'failed check','missing check','duplicate ID','unmatched assertion','missing phase','duplicate phase','failed phase','phase hash mismatch',
  'missing playback','unsynchronized playback','fabricated baseline','playback hash mismatch')
 foreach($name in $cases) {
  $m=$manifest | ConvertTo-Json -Depth 25 | ConvertFrom-Json
  $p=@{}; foreach($file in $phaseNames){$p[$file]=$phases[$file] | ConvertTo-Json -Depth 20 | ConvertFrom-Json}
  $b=$playback | ConvertTo-Json -Depth 10 | ConvertFrom-Json; $l=@($lines)
  switch($name) {
   'missing stage' {$l=@($l | Where-Object {$_ -cne 'HANDS_ON_ROUND3=PASS'})}
   'duplicate stage' {$l+=@('HANDS_ON_ROUND3=PASS')}
   'missing manifest' {$m=$null}
   'stale source' {$m.source_head='0000000000000000000000000000000000000000'}
   'stale tree' {$m.checkout_tree='0000000000000000000000000000000000000000'}
   'stale run' {$m.run_id='0'}
   'stale attempt' {$m.run_attempt='0'}
   'wrong host' {$m.host='unverified'}
   'failed check' {$m.checks[0].result='FAIL'}
   'missing check' {$m.checks=@($m.checks | Select-Object -Skip 1)}
   'duplicate ID' {$m.checks[1].id=$m.checks[0].id}
   'unmatched assertion' {$m.checks[0].evidence='invented evidence'}
   'missing phase' {$p[$phaseNames[0]]=$null}
   'duplicate phase' {$m.phases[1].file=$m.phases[0].file}
   'failed phase' {$p[$phaseNames[0]].checks[0].result='FAIL'}
   'phase hash mismatch' {$m.phases[0].sha256=('0'*64)}
   'missing playback' {$b=$null}
   'unsynchronized playback' {$b.product_navigation=@(0,2,4)}
   'fabricated baseline' {$b.timeline_only=@(180,182,184)}
   'playback hash mismatch' {$m.playback.sha256=('0'*64)}
  }
  $rejected=$false
  try {Assert-Round3Evidence $l $m $p $b $digests $playDigest} catch {$rejected=$true}
  if (-not $rejected) {throw "Round3 guard accepted invalid evidence: $name"}
  $tests.Add([ordered]@{name=$name;result='PASS_REJECTED'})
 }
 [ordered]@{schema='YMM4-Template-Placer-Round3-Evidence-Guard/1';result='PASS';positive_checks=1;negative_checks=$tests.Count;checks=$tests} |
  ConvertTo-Json -Depth 10 | Set-Content (Join-Path $OutputDir 'round3-evidence-guard-tests.json')
 Write-Host "Round3 evidence guard: 1 valid input and $($tests.Count) rejection tests PASS; all historical evidence gates remain independent requirements"
}
$manifest
