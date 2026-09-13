param([Parameter(Mandatory=$true)][string]$Ymm4Dir, [Parameter(Mandatory=$true)][string]$OutputDir, [string]$DistributionDir, [switch]$ReleaseSmoke)
$ErrorActionPreference='Stop'
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
if ($ReleaseSmoke) { Remove-Item Env:YMM4_TEMPLATE_PLACER_PROOF_DIR -ErrorAction SilentlyContinue }
else { $env:YMM4_TEMPLATE_PLACER_PROOF_DIR=$OutputDir; Remove-Item $result -ErrorAction SilentlyContinue }
$p=Start-Process (Join-Path $Ymm4Dir 'YukkuriMovieMaker.exe') -WorkingDirectory $Ymm4Dir -PassThru
try {
 for ($i=0; $i -lt 150; $i++) {
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
 if (-not (Test-Path $marker)) { throw 'Release DLL did not load in native YMM4' }
 $text=Get-Content -Raw $marker
 $installed=Join-Path $Ymm4Dir 'user/plugin/Ymm4TemplatePlacer/Ymm4TemplatePlacer.dll'
 $expected=(Get-FileHash $installed -Algorithm SHA256).Hash.ToLowerInvariant()
 if ($text -notmatch '(?m)^build=distribution\r?$' -or $text -notmatch "(?m)^sha256=$expected\r?`$") { throw 'Loaded assembly is not the exact distribution DLL' }
 Get-Content $marker
} else {
 if (Test-Path (Join-Path $OutputDir 'proof-log.txt')) { Get-Content (Join-Path $OutputDir 'proof-log.txt') }
 if (-not (Test-Path $result)) { throw 'Native proof did not finish; inspect windows-seen and build evidence' }
 Get-Content $result
 if (-not (Select-String -Path $result -Pattern '^PASS P1 P2 P3 P4 P5 P6 P7 P8$')) { throw 'Native functional proof failed' }
}
