<#
    Scrub-PexHeader.ps1 - replaces the account and computer name a Papyrus compiler writes into
    every .pex header.

    The compiler stamps each script with the Windows user name and the machine name of whoever
    compiled it. The game never reads either field, but the bundled scripts are copied into every
    follower package this app builds, so those two strings would travel to everyone who downloads
    any follower made with it. Run this after recompiling a script in src\BuildPipeline\Resources;
    PexHeaderTests fails the suite if a script still carries anything else.

    Header layout (big-endian): magic u32, major u8, minor u8, game u16, compile time u64, then
    three length-prefixed strings: source file name, user name, machine name. Nothing later in the
    file is addressed by offset, so the two strings can change length safely.
#>
[CmdletBinding()]
param(
    [string]$Path,
    [string]$Token = 'FollowerForge'
)
$ErrorActionPreference = 'Stop'
# Resolved here rather than in the param block: Windows PowerShell 5.1 leaves $PSScriptRoot empty
# while it evaluates parameter defaults.
if (-not $Path) {
    $here = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
    $Path = Join-Path $here '..\src\BuildPipeline\Resources'
}

function Read-Str([byte[]]$bytes, [ref]$offset) {
    $length = ($bytes[$offset.Value] -shl 8) -bor $bytes[$offset.Value + 1]
    $text = [System.Text.Encoding]::ASCII.GetString($bytes, $offset.Value + 2, $length)
    $offset.Value += 2 + $length
    return $text
}

function Write-Str([System.IO.Stream]$stream, [string]$text) {
    $raw = [System.Text.Encoding]::ASCII.GetBytes($text)
    $stream.WriteByte([byte](($raw.Length -shr 8) -band 0xFF))
    $stream.WriteByte([byte]($raw.Length -band 0xFF))
    $stream.Write($raw, 0, $raw.Length)
}

$files = if (Test-Path $Path -PathType Container) { Get-ChildItem $Path -Filter *.pex } else { Get-Item $Path }
foreach ($file in $files) {
    $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
    if ($bytes.Length -lt 22 -or $bytes[0] -ne 0xFA -or $bytes[1] -ne 0x57 -or $bytes[2] -ne 0xC0 -or $bytes[3] -ne 0xDE) {
        throw "$($file.Name) is not a compiled Papyrus script"
    }
    $offset = 16
    $source = Read-Str $bytes ([ref]$offset)
    $user = Read-Str $bytes ([ref]$offset)
    $machine = Read-Str $bytes ([ref]$offset)
    if ($user -eq $Token -and $machine -eq $Token) {
        Write-Host "$($file.Name): already clean"
        continue
    }

    $out = New-Object System.IO.MemoryStream
    $out.Write($bytes, 0, 16)
    Write-Str $out $source
    Write-Str $out $Token
    Write-Str $out $Token
    $out.Write($bytes, $offset, $bytes.Length - $offset)
    [System.IO.File]::WriteAllBytes($file.FullName, $out.ToArray())
    Write-Host "$($file.Name): header scrubbed"
}
