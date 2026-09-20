param([switch]$SelfTest)
$ErrorActionPreference='Stop'
function Test-NativePath([string]$Path) {
 return $Path -match '^(src/.+\.(cs|csproj|xaml)|tests/.+\.(cs|csproj|ps1)|fixtures/.+\.(json|ymmp|png|xlsx)|\.github/workflows/native-yymm4-proof\.yml)$'
}
function Test-ValidationInfrastructurePath([string]$Path) {
 return $Path -match '^(tests/|fixtures/|\.github/workflows/native-yymm4-proof\.yml$)'
}
function Normalize-Level([string]$Value) {
 switch ($Value.ToLowerInvariant()) {
  'focused' { return 'focused' }
  'checkpoint' { return 'checkpoint' }
  'release' { return 'release' }
  default { throw "Unknown validation level: $Value" }
 }
}
$cases=@{
 'README.md'=$false; 'docs/DESIGN.md'=$false; 'tests/README.md'=$false; 'fixtures/README.md'=$false
 'src/Ymm4TemplatePlacer/PlacerView.xaml'=$true; 'src/Ymm4TemplatePlacer/PluginEntry.cs'=$true
 'src/Ymm4TemplatePlacer/Ymm4TemplatePlacer.csproj'=$true; 'tests/NativeProof.cs'=$true
 'tests/RunNative.ps1'=$true; 'fixtures/example.xlsx'=$true; '.github/workflows/native-yymm4-proof.yml'=$true
}
foreach ($path in $cases.Keys) { if ((Test-NativePath $path) -ne $cases[$path]) { throw "CI scope self-test failed: $path" } }
foreach ($path in @('tests/NativeProof.cs','fixtures/example.xlsx','.github/workflows/native-yymm4-proof.yml')) {
 if (-not (Test-ValidationInfrastructurePath $path)) { throw "Validation-infrastructure self-test failed: $path" }
}
if (Test-ValidationInfrastructurePath 'src/Ymm4TemplatePlacer/PlacerView.xaml') { throw 'Product source must not force checkpoint validation by itself.' }
foreach ($level in @('focused','checkpoint','release')) { if ((Normalize-Level $level) -cne $level) { throw "Level self-test failed: $level" } }
Write-Host "CI scope / validation-level self-tests: $($cases.Count + 7) PASS"
if ($SelfTest) { exit 0 }

$event=Get-Content -Raw $env:GITHUB_EVENT_PATH | ConvertFrom-Json
$run=$true; $changed=@(); $requested=[string]$env:REQUESTED_VALIDATION_LEVEL
if (-not [string]::IsNullOrWhiteSpace($requested)) {
 $level=Normalize-Level $requested
} elseif ($env:GITHUB_EVENT_NAME -eq 'push' -and $env:GITHUB_REF -eq 'refs/heads/main') {
 $level='release'
} elseif ($env:GITHUB_EVENT_NAME -eq 'push') {
 $level='checkpoint'
} elseif ($env:GITHUB_EVENT_NAME -eq 'pull_request') {
 if ($event.action -eq 'ready_for_review') {
  $level='checkpoint'
 } else {
  $base=if($event.action -eq 'synchronize') {[string]$event.before} else {[string]$event.pull_request.base.sha}
  $head=[string]$event.pull_request.head.sha
  if ($base -match '^[0-9a-f]{40}$' -and $head -match '^[0-9a-f]{40}$') {
   git cat-file -e "$base^{commit}" 2>$null; $haveBase=$LASTEXITCODE -eq 0
   git cat-file -e "$head^{commit}" 2>$null; $haveHead=$LASTEXITCODE -eq 0
   if ($haveBase -and $haveHead) {
    $changed=@(git diff --name-only $base $head)
    if ($LASTEXITCODE -ne 0) { throw 'Unable to determine changed paths' }
    $relevant=@($changed | Where-Object { Test-NativePath $_ })
    $run=$relevant.Count -gt 0
    $infra=@($changed | Where-Object { Test-ValidationInfrastructurePath $_ })
    $level=if($infra.Count -gt 0){'checkpoint'}else{'focused'}
    Write-Host "Changed paths: $($changed.Count); native-relevant: $($relevant.Count); validation-infrastructure: $($infra.Count)"
   } else {
    Write-Host 'Comparison commit unavailable; conservatively choose checkpoint.'
    $level='checkpoint'
   }
  } else { $level='checkpoint' }
 }
} else {
 $level='checkpoint'
}
"run=$($run.ToString().ToLowerInvariant())" | Out-File $env:GITHUB_OUTPUT -Append -Encoding utf8
"level=$level" | Out-File $env:GITHUB_OUTPUT -Append -Encoding utf8
Write-Host "Native validation enabled: $run; level=$level"
