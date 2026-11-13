<#
    Publish-FollowerForge.ps1 - produces a shareable Windows build.

    Self-contained: the person you send it to does NOT need .NET installed. Native
    dependencies (NiflySharp's niflycpp, SQLite) are extracted at run time, which is why
    single-file publishing needs IncludeNativeLibrariesForSelfExtract.
#>
[CmdletBinding()]
param(
    [string]$Version = '3.8.0',
    [switch]$SkipTests
)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
if (-not $root) { $root = (Get-Location).Path }
$src = Join-Path $root 'src'
$sln = Join-Path $src 'FollowerForge.slnx'
if (-not (Test-Path $sln)) { throw "Solution not found: $sln" }

$dist = Join-Path $root 'dist'
$stage = Join-Path $dist "FollowerForge $Version"
$zip = Join-Path $dist "FollowerForge-$Version-win-x64.zip"

Write-Host "== FollowerForge $Version - publish ==" -ForegroundColor Cyan

# A running instance holds FollowerForge.exe open and Compress-Archive then fails with
# PermissionDenied *after* the build work is done. Close them first and prove none remain.
$running = Get-Process -Name 'FollowerForge*' -ErrorAction SilentlyContinue
if ($running) {
    Write-Host "-- closing $($running.Count) running FollowerForge process(es)" -ForegroundColor DarkGray
    $running | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 800
    $still = Get-Process -Name 'FollowerForge*' -ErrorAction SilentlyContinue
    if ($still) { throw "FollowerForge is still running (PID $($still.Id -join ', ')); close it and retry." }
}

# Regenerate the app icon so the shipped exe always matches the source art.
& (Join-Path $root 'tools\New-AppIcon.ps1') -OutPath (Join-Path $src 'Ui\Assets\appicon.ico')

if (-not $SkipTests) {
    Write-Host '-- tests' -ForegroundColor DarkGray
    dotnet test (Join-Path $src 'Tests') -c Release --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw "tests failed ($LASTEXITCODE)" }
}

if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Force $stage | Out-Null

$common = @(
    '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true', '--nologo',
    '-p:PublishSingleFile=true',
    '-p:IncludeNativeLibrariesForSelfExtract=true',
    '-p:EnableCompressionInSingleFile=true',
    '-p:DebugType=none',
    "-p:Version=$Version",
    "-p:FileVersion=$Version.0",
    "-p:InformationalVersion=$Version",
    "-p:AssemblyVersion=$Version.0"
)

Write-Host '-- app' -ForegroundColor DarkGray
dotnet publish (Join-Path $src 'Ui') @common -o $stage
if ($LASTEXITCODE -ne 0) { throw "app publish failed ($LASTEXITCODE)" }

Write-Host '-- cli' -ForegroundColor DarkGray
dotnet publish (Join-Path $src 'Cli') @common -o (Join-Path $stage 'cli')
if ($LASTEXITCODE -ne 0) { throw "cli publish failed ($LASTEXITCODE)" }

# Skia/HarfBuzz ship ~100 MB of native debug symbols that nobody downloading a mod tool needs.
Get-ChildItem $stage -Recurse -Filter *.pdb | Remove-Item -Force

# Docs the user actually needs next to the exe.
foreach ($doc in 'README.md', "NEXUS-CHANGELOG-$Version.txt") {
    $p = Join-Path $root $doc
    if (Test-Path $p) { Copy-Item $p $stage }
}
$changelog = Join-Path $root 'CHANGELOG.txt'
if (-not (Test-Path $changelog)) {
    $changelog = Join-Path (Split-Path $root -Parent) 'CHANGELOG.txt'
}
if (Test-Path $changelog) { Copy-Item $changelog $stage }

$exe = Join-Path $stage 'FollowerForge.exe'
if (-not (Test-Path $exe)) { throw "expected exe missing: $exe" }

# Boot check: start it, confirm the window survives a few seconds, then close it.
Write-Host '-- boot check' -ForegroundColor DarkGray
$p = Start-Process $exe -PassThru -WindowStyle Hidden
Start-Sleep -Seconds 12
$alive = Get-Process -Id $p.Id -ErrorAction SilentlyContinue
if (-not $alive) { throw 'published exe exited immediately' }
$alive | Stop-Process -Force
# Stop-Process returns before the OS drops the file handle, and Compress-Archive then fails with
# PermissionDenied on the exe. Wait for the process to actually disappear.
Wait-Process -Id $p.Id -Timeout 20 -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1
Write-Host '   window stayed up' -ForegroundColor Green

if (Test-Path $zip) { Remove-Item $zip -Force }
# Retry the archive: antivirus and the shell's own handle release can still hold the exe briefly.
$attempt = 0
while ($true) {
    $attempt++
    try {
        Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -ErrorAction Stop
        break
    }
    catch {
        if ($attempt -ge 4) { throw "could not write $zip after $attempt attempts: $($_.Exception.Message)" }
        Write-Host "-- zip attempt $attempt failed, retrying" -ForegroundColor DarkYellow
        if (Test-Path $zip) { Remove-Item $zip -Force -ErrorAction SilentlyContinue }
        Start-Sleep -Seconds 2
    }
}

# Reopen the archive and prove it carries what the release notes claim. A Compress-Archive that
# half-writes, or a stage missing a file, must fail here rather than in the changelog.
Add-Type -AssemblyName System.IO.Compression.FileSystem
$expected = @('FollowerForge.exe', 'cli\FollowerForge.Cli.exe', 'README.md', 'CHANGELOG.txt')
$archive = [System.IO.Compression.ZipFile]::OpenRead($zip)
try {
    $entries = $archive.Entries | ForEach-Object { $_.FullName.Replace('/', '\') }
    foreach ($want in $expected) {
        $match = $archive.Entries | Where-Object { $_.FullName.Replace('/', '\') -eq $want }
        if (-not $match) { throw "zip is missing $want" }
        if ($match.Length -le 0) { throw "zip entry $want is empty" }
    }
    $count = $archive.Entries.Count
} finally { $archive.Dispose() }

$hash = (Get-FileHash -Path $zip -Algorithm SHA256).Hash.ToLowerInvariant()
$size = (Get-Item $zip).Length
$mb = [math]::Round($size / 1MB, 1)

Write-Host ''
Write-Host 'PUBLISH SUCCEEDED' -ForegroundColor Green
Write-Host "  folder : $stage"
Write-Host "  zip    : $zip  ($mb MB)"
Write-Host "  entries: $count  (verified: $($expected -join ', '))"
Write-Host "  size   : $size bytes"
Write-Host "  sha256 : $hash"
Write-Host "  exe    : FollowerForge.exe  (self-contained - no .NET needed)"
exit 0
