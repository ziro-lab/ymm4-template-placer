param(
 [Parameter(Mandatory=$true)][string]$Ymm4Dir,
 [Parameter(Mandatory=$true)][string]$OutputDir,
 [string]$DistributionDir,
 [ValidateSet('focused','checkpoint','release')][string]$Profile='checkpoint',
 [switch]$ReleaseSmoke,
 [int]$TimeoutSeconds=300
)
$ErrorActionPreference='Stop'
if ($TimeoutSeconds -lt 1) { throw 'TimeoutSeconds must be at least 1.' }
Add-Type -TypeDefinition @'
using System; using System.Text; using System.Runtime.InteropServices;
public static class PlacerWin32 {
 public delegate bool EnumWindowsProc(IntPtr h, IntPtr l);
 [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc callback, IntPtr l);
 [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
 [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
 [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint p);
 [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint m, IntPtr w, IntPtr l);
}
'@
New-Item -ItemType Directory -Force $OutputDir | Out-Null
$marker=Join-Path $OutputDir $(if($ReleaseSmoke){'release-plugin-loaded.txt'}else{'plugin-loaded.txt'})
$result=Join-Path $OutputDir 'proof-result.txt'
Remove-Item $marker -ErrorAction SilentlyContinue
$env:YMM4_TEMPLATE_PLACER_CI_MARKER=$marker
$env:YMM4_TEMPLATE_PLACER_DIST_DIR=$DistributionDir
$env:YMM4_TEMPLATE_PLACER_NATIVE_PROFILE=$Profile
if ($ReleaseSmoke) { Remove-Item Env:YMM4_TEMPLATE_PLACER_PROOF_DIR -ErrorAction SilentlyContinue }
else {
 $env:YMM4_TEMPLATE_PLACER_PROOF_DIR=$OutputDir
 $event=Get-Content -Raw $env:GITHUB_EVENT_PATH | ConvertFrom-Json
 $env:YMM4_TEMPLATE_PLACER_SOURCE_HEAD=if($env:GITHUB_EVENT_NAME -eq 'pull_request'){$event.pull_request.head.sha}else{$env:GITHUB_SHA}
 $env:YMM4_TEMPLATE_PLACER_CHECKOUT_TREE=git rev-parse 'HEAD^{tree}'
 if($LASTEXITCODE -ne 0){throw 'Cannot identify native source tree'}
 Get-ChildItem $OutputDir -Filter 'hands-on-round3*.json' -File | Remove-Item
 Get-ChildItem $OutputDir -Filter 'hands-on-round4*.json' -File | Remove-Item
 Remove-Item (Join-Path $OutputDir 'round4-checkpoint-guard-tests.json') -ErrorAction SilentlyContinue
 Remove-Item (Join-Path $OutputDir 'round3-evidence-guard-tests.json') -ErrorAction SilentlyContinue
 foreach ($name in @('proof-result.txt','proof-log.txt','v04-acceptance.json','ux-acceptance.json','ux-workflow-acceptance.json','v042-acceptance.json','v042-uiux-acceptance.json','hands-on-ux-polish.json','hands-on-round2-input.json','hands-on-round2-sets.json','hands-on-round2-tiles.json','hands-on-round2-settings.json','hands-on-round2-expression.json','hands-on-round2.json','evidence-guard-tests.json','expression-performance.json','tachie-preset-capability.json','tachie-preset-guards.json','tachie-preset-rows.json','tachie-preset-choice-model.json')) { Remove-Item (Join-Path $OutputDir $name) -ErrorAction SilentlyContinue }
}
$p=Start-Process (Join-Path $Ymm4Dir 'YukkuriMovieMaker.exe') -WorkingDirectory $Ymm4Dir -PassThru
try {
 for ($i=0; $i -lt $TimeoutSeconds; $i++) {
  if (($ReleaseSmoke -and (Test-Path $marker)) -or (-not $ReleaseSmoke -and (Test-Path $result)) -or $p.HasExited) { break }
  $cb=[PlacerWin32+EnumWindowsProc]{
   param([IntPtr]$h,[IntPtr]$l)
   [uint32]$windowPid=0
   [void][PlacerWin32]::GetWindowThreadProcessId($h,[ref]$windowPid)
   if ($windowPid -eq $p.Id -and [PlacerWin32]::IsWindowVisible($h)) {
    $s=New-Object Text.StringBuilder 1024
    [void][PlacerWin32]::GetWindowText($h,$s,1024)
    $t=$s.ToString()
    "$t" | Add-Content (Join-Path $OutputDir 'windows-seen.txt')
    if ($t -like '*Check for updates*' -or $t -like '*About YukkuriMovieMaker*') {
     [void][PlacerWin32]::PostMessage($h,0x0010,[IntPtr]::Zero,[IntPtr]::Zero)
    } elseif ($t -eq 'Confirm') {
     [void][PlacerWin32]::PostMessage($h,0x0100,[IntPtr]0x0D,[IntPtr]::Zero)
     [void][PlacerWin32]::PostMessage($h,0x0101,[IntPtr]0x0D,[IntPtr]::Zero)
    }
   }
   return $true
  }
  [void][PlacerWin32]::EnumWindows($cb,[IntPtr]::Zero)
  Start-Sleep -Seconds 1
 }
} finally { if (-not $p.HasExited) { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue } }

if ($ReleaseSmoke) {
 if (-not (Test-Path $marker)) { throw "Release DLL did not load in native YMM4 within $TimeoutSeconds seconds" }
 $text=Get-Content -Raw $marker
 $installed=Join-Path $Ymm4Dir 'user/plugin/Ymm4TemplatePlacer/Ymm4TemplatePlacer.dll'
 $expected=(Get-FileHash $installed -Algorithm SHA256).Hash.ToLowerInvariant()
 $hashPattern='(?m)^sha256='+[regex]::Escape($expected)+'\r?$'
 if ($text -notmatch '(?m)^build=distribution\r?$' -or $text -notmatch $hashPattern) { throw 'Loaded assembly is not the exact distribution DLL' }
 Get-Content $marker
 return
}

$log=Join-Path $OutputDir 'proof-log.txt'
if (Test-Path $log) { Get-Content $log }
if (-not (Test-Path $result)) { throw "Native proof did not finish within $TimeoutSeconds seconds; inspect windows-seen and build evidence" }
Get-Content $result
if (-not (Select-String -Path $result -Pattern '^PASS P1 P2 P3 P4 P5 P6 P7 P8 P9$')) { throw 'Native functional proof failed' }

$presetProofs=@(
 @{File='tachie-preset-capability.json'; Schema='YMM4-Template-Placer-Tachie-Preset-Capability/1'; Marker='TACHIE_PRESET_CAPABILITY_P3=PASS'},
 @{File='tachie-preset-guards.json'; Schema='YMM4-Template-Placer-Tachie-Preset-Guards/1'; Marker='TACHIE_PRESET_GUARDS_P3=PASS'},
 @{File='tachie-preset-rows.json'; Schema='YMM4-Template-Placer-Tachie-Preset-Rows/1'; Marker='TACHIE_PRESET_ROWS_P4=PASS'},
 @{File='tachie-preset-choice-model.json'; Schema='YMM4-Template-Placer-Tachie-Preset-Choice-Model/1'; Marker='TACHIE_PRESET_CHOICE_MODEL_P5=PASS'}
)
foreach ($proof in $presetProofs) {
 if (-not (Select-String -Path $log -Pattern ('^'+[regex]::Escape($proof.Marker)+'$'))) { throw "Missing preset proof marker: $($proof.Marker)" }
 $data=Get-Content -Raw (Join-Path $OutputDir $proof.File) | ConvertFrom-Json
 if ($data.schema -cne $proof.Schema -or $data.result -cne 'PASS' -or $data.host -cne 'YMM4 4.55.1.1 Lite' -or
     $data.sourceHead -cne $env:YMM4_TEMPLATE_PLACER_SOURCE_HEAD -or $data.checkoutTree -cne $env:YMM4_TEMPLATE_PLACER_CHECKOUT_TREE -or
     $data.runId -cne $env:GITHUB_RUN_ID -or @($data.checks).Count -eq 0) { throw "Incomplete or stale preset proof: $($proof.File)" }
}

if ($Profile -eq 'focused') {
 if (-not (Select-String -Path $log -Pattern '^FOCUSED_NATIVE=PASS$')) { throw 'Focused stable-core native proof is incomplete' }
 Write-Host 'Focused native validation: stable core + current Round 4 checkpoints PASS'
 return
}

if (-not (Select-String -Path $log -Pattern '^V04=PASS$')) { throw 'Integrated v0.4 native proof is incomplete' }
if (-not (Select-String -Path $log -Pattern '^UX_ACCEPTANCE=PASS$')) { throw 'Task UX acceptance is incomplete' }
if (-not (Select-String -Path $log -Pattern '^UX_WORKFLOW_ACCEPTANCE=PASS$') -or -not (Select-String -Path $log -Pattern '^WUX13=PASS$')) { throw 'v0.4.2 UX workflow acceptance is incomplete' }
if (-not (Select-String -Path $log -Pattern '^HANDS_ON_UX_POLISH=PASS$')) { throw 'Hands-on UX polish native acceptance is incomplete' }
if (-not (Select-String -Path $log -Pattern '^HANDS_ON_ROUND2=PASS$')) { throw 'Hands-on Round 2 native acceptance is incomplete' }
if (-not (Select-String -Path $log -Pattern '^EXPRESSION_PERFORMANCE=PASS$')) { throw 'Expression performance proof is incomplete' }
$perfPath=Join-Path $OutputDir 'expression-performance.json'
if (-not (Test-Path $perfPath)) { throw 'Expression performance evidence is missing' }
$perf=Get-Content -Raw $perfPath | ConvertFrom-Json
$perfVoices=@($perf.sizes | ForEach-Object { [int]$_.Voices })
if ($perf.version -ne '0.4.2' -or $perf.result -ne 'PASS' -or @($perf.sizes).Count -ne 3 -or ($perfVoices -join ',') -ne '100,500,1000') {
 throw 'Expression performance evidence is incomplete or stale'
}
$null = & "$PSScriptRoot/ValidateRelativeEvidence.ps1" -OutputDir $OutputDir
$null = & "$PSScriptRoot/ValidateRound3Evidence.ps1" -OutputDir $OutputDir
$null = & "$PSScriptRoot/ValidateRound4Checkpoint.ps1" -OutputDir $OutputDir -Phases A,B,CT,CS,C
$acceptance=Get-Content -Raw (Join-Path $OutputDir 'v04-acceptance.json') | ConvertFrom-Json
if ($acceptance.version -ne '0.4.2' -or $acceptance.result -ne 'PASS' -or @($acceptance.checks).Count -ne 18 -or @($acceptance.checks | Where-Object { $_.result -ne 'PASS' }).Count) { throw 'Incomplete v0.4.2 core acceptance evidence' }
$ux=Get-Content -Raw (Join-Path $OutputDir 'ux-acceptance.json') | ConvertFrom-Json
if ($ux.version -ne '0.4.0' -or $ux.result -ne 'PASS' -or @($ux.checks).Count -ne 12 -or @($ux.checks | Where-Object { $_.result -ne 'PASS' }).Count) { throw 'Incomplete retained Task UX acceptance evidence' }
$workflow=Get-Content -Raw (Join-Path $OutputDir 'ux-workflow-acceptance.json') | ConvertFrom-Json
if ($workflow.version -ne '0.4.2' -or $workflow.result -ne 'PASS' -or @($workflow.checks).Count -ne 10 -or @($workflow.checks | Where-Object { $_.result -ne 'PASS' }).Count) { throw 'Incomplete v0.4.2 UX workflow acceptance evidence' }
Write-Host 'Checkpoint native validation: full semantic regression and evidence guards PASS'
