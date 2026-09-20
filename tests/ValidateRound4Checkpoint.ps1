param([Parameter(Mandatory=$true)][string]$OutputDir,[ValidateSet('A','B','C','D','E')][string[]]$Phases=@('A'))
$ErrorActionPreference='Stop'
$counts=@{A=12;B=20;C=22;D=22;E=24}
$log=Get-Content -Raw (Join-Path $OutputDir 'proof-log.txt')
function Assert-Phase($doc,[string]$phase,[string]$nativeLog) {
 if($null-eq$doc-or$doc.schema-cne'YMM4-Template-Placer-Round4-Phase/1'-or$doc.phase-cne$phase-or$doc.result-cne'PASS'-or$doc.host-cne'YMM4 4.55.1.1 Lite'){throw 'Invalid R4 phase header'}
 if($doc.sourceHead-cne$env:YMM4_TEMPLATE_PLACER_SOURCE_HEAD-or$doc.checkoutTree-cne$env:YMM4_TEMPLATE_PLACER_CHECKOUT_TREE-or$doc.runId-cne$env:GITHUB_RUN_ID-or$doc.runAttempt-cne$env:GITHUB_RUN_ATTEMPT){throw 'Stale R4 source/run identity'}
 if([string]$doc.sourceHead-cnotmatch'^[0-9a-f]{40}$'-or[string]$doc.checkoutTree-cnotmatch'^[0-9a-f]{40}$'){throw 'Missing R4 source identity'}
 $expected=@(1..$counts[$phase]|ForEach-Object{"$phase$_"})
 $checks=@($doc.checks)
 if($checks.Count-ne$expected.Count-or@($checks.id|Sort-Object -Unique).Count-ne$expected.Count){throw 'Missing or duplicate R4 checks'}
 foreach($id in $expected){
  $check=@($checks|Where-Object{$_.id-ceq$id})
  if($check.Count-ne1-or$check[0].result-cne'PASS'-or[string]::IsNullOrWhiteSpace([string]$check[0].evidence)){throw "R4 check rejected: $id"}
  $line='ASSERT PASS: R4 '+$id+': '+[string]$check[0].evidence
  if([regex]::Matches($nativeLog,'(?m)^'+[regex]::Escape($line)+'\r?$').Count-ne1){throw "R4 assertion log does not match: $id"}
 }
 if([regex]::Matches($nativeLog,'(?m)^HANDS_ON_ROUND4_'+$phase+'=PASS\r?$').Count-ne1){throw 'Missing or duplicate R4 checkpoint marker'}
}
$negative=0
foreach($phase in $Phases){
 $text=Get-Content -Raw (Join-Path $OutputDir ('hands-on-round4-'+$phase.ToLowerInvariant()+'.json'))
 $valid=$text|ConvertFrom-Json
 Assert-Phase $valid $phase $log
 foreach($kind in @('source','tree','run','attempt','header','failed','missing','duplicate','blank','log','marker')){
  $bad=$text|ConvertFrom-Json; $badLog=$log
  switch($kind){
   source {$bad.sourceHead='0'*40}
   tree {$bad.checkoutTree='0'*40}
   run {$bad.runId='stale'}
   attempt {$bad.runAttempt='stale'}
   header {$bad.host='unknown'}
   failed {$bad.checks[0].result='FAIL'}
   missing {$bad.checks=@($bad.checks|Select-Object -Skip 1)}
   duplicate {$bad.checks[1].id=$bad.checks[0].id}
   blank {$bad.checks[0].evidence=''}
   log {$badLog=$log.Replace('ASSERT PASS: R4 '+$phase+'1: ','ASSERT REMOVED: ')}
   marker {$badLog=$log+"`nHANDS_ON_ROUND4_$phase=PASS`n"}
  }
  $rejected=$false
  try{Assert-Phase $bad $phase $badLog}catch{$rejected=$true}
  if(-not$rejected){throw "R4 evidence negative fixture accepted: $phase/$kind"}
  $negative++
 }
}
[ordered]@{schema='YMM4-Template-Placer-Round4-Checkpoint-Guard/1';result='PASS';phases=$Phases;valid=$Phases.Count;rejected=$negative;sourceHead=$env:YMM4_TEMPLATE_PLACER_SOURCE_HEAD;runId=$env:GITHUB_RUN_ID;runAttempt=$env:GITHUB_RUN_ATTEMPT}|ConvertTo-Json|Set-Content (Join-Path $OutputDir 'round4-checkpoint-guard-tests.json')
Write-Host "Round4 checkpoint evidence: $($Phases.Count) valid, $negative invalid fixtures rejected"
