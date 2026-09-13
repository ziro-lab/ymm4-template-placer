param([switch]$SelfTest)
$ErrorActionPreference='Stop'
function Test-NativePath([string]$Path) {
 return $Path -match '^(src/.+\.(cs|csproj|xaml)|tests/.+\.(cs|csproj|ps1)|fixtures/.+\.(json|ymmp|png|xlsx)|\.github/workflows/native-yymm4-proof\.yml)$'
}
$cases=@{
 'README.md'=$false; 'docs/DESIGN.md'=$false; 'tests/README.md'=$false; 'fixtures/README.md'=$false
 'src/Ymm4TemplatePlacer/PlacerView.xaml'=$true; 'src/Ymm4TemplatePlacer/PluginEntry.cs'=$true
 'src/Ymm4TemplatePlacer/Ymm4TemplatePlacer.csproj'=$true; 'tests/NativeProof.cs'=$true
 'tests/RunNative.ps1'=$true; 'fixtures/example.xlsx'=$true; '.github/workflows/native-yymm4-proof.yml'=$true
}
foreach ($path in $cases.Keys) { if ((Test-NativePath $path) -ne $cases[$path]) { throw "CI scope self-test failed: $path" } }
Write-Host "CI scope self-tests: $($cases.Count) PASS"
if ($SelfTest) { exit 0 }
$run=$true
$event=Get-Content -Raw $env:GITHUB_EVENT_PATH | ConvertFrom-Json
# PR path filters cover the whole PR, not only its latest commit. This second gate
# prevents documentation-only synchronize events from downloading/running YMM4.
if ($env:GITHUB_EVENT_NAME -eq 'pull_request' -and $event.action -eq 'synchronize') {
 $before=[string]$event.before
 $head=[string]$event.pull_request.head.sha
 if ($before -match '^[0-9a-f]{40}$' -and $head -match '^[0-9a-f]{40}$') {
  git cat-file -e "$before^{commit}" 2>$null
  $haveBefore=$LASTEXITCODE -eq 0
  git cat-file -e "$head^{commit}" 2>$null
  $haveHead=$LASTEXITCODE -eq 0
  if ($haveBefore -and $haveHead) {
   $changed=@(git diff --name-only $before $head)
   if ($LASTEXITCODE -ne 0) { throw 'Unable to determine changed paths' }
   $relevant=@($changed | Where-Object { Test-NativePath $_ })
   $run=$relevant.Count -gt 0
   Write-Host "Changed paths: $($changed.Count); native-relevant: $($relevant.Count)"
  } else { Write-Host 'Prior commit is unavailable; conservatively run native proof.' }
 }
}
"run=$($run.ToString().ToLowerInvariant())" | Out-File $env:GITHUB_OUTPUT -Append -Encoding utf8
Write-Host "Native download / build / launch enabled: $run"
