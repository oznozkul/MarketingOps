# Downloads the Tailwind CSS v3 standalone CLI for Windows into this folder.
$ErrorActionPreference = 'Stop'
$version = if ($env:TAILWIND_VERSION) { $env:TAILWIND_VERSION } else { 'v3.4.17' }
$out = Join-Path $PSScriptRoot 'tailwindcss.exe'
if (Test-Path $out) { exit 0 }
$arch = if ($env:PROCESSOR_ARCHITECTURE -eq 'ARM64') { 'arm64' } else { 'x64' }
$url = "https://github.com/tailwindlabs/tailwindcss/releases/download/$version/tailwindcss-windows-$arch.exe"
Write-Host "Downloading $url"
Invoke-WebRequest -Uri $url -OutFile $out
