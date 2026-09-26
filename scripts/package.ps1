param(
    [string]$OutputDirectory,
    [string]$Version = '301.1'
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $repoRoot 'RetakesAllocator/bin/Release/net10.0' }
$OutputDirectory = (Resolve-Path -LiteralPath $OutputDirectory).Path
$artifacts = Join-Path $repoRoot 'artifacts'
$staging = Join-Path $artifacts ('package-' + [guid]::NewGuid().ToString('N'))
$pluginTarget = Join-Path $staging 'addons/counterstrikesharp/plugins/RetakesAllocator'
New-Item -ItemType Directory -Path $pluginTarget -Force | Out-Null

foreach ($file in Get-ChildItem -LiteralPath $OutputDirectory -File) {
    if ($file.Name -in @('CounterStrikeSharp.API.dll', 'RetakesPluginShared.dll')) {
        throw "Host/shared assembly must not be packaged: $($file.Name)"
    }
    if ($file.Extension -in @('.dll', '.json', '.pdb')) {
        Copy-Item -LiteralPath $file.FullName -Destination $pluginTarget
    }
}
Copy-Item -LiteralPath (Join-Path $OutputDirectory 'lang') -Destination $pluginTarget -Recurse
$nativeTarget = Join-Path $pluginTarget 'runtimes/linux-x64'
New-Item -ItemType Directory -Path (Split-Path $nativeTarget -Parent) -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $OutputDirectory 'runtimes/linux-x64') -Destination $nativeTarget -Recurse
Copy-Item -LiteralPath (Join-Path $repoRoot 'INSTALLATION-DE.md') -Destination $staging
Copy-Item -LiteralPath (Join-Path $repoRoot 'TESTPLAN-301.md') -Destination $staging
Copy-Item -LiteralPath (Join-Path $repoRoot 'CHANGELOG-301.md') -Destination $staging
Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE') -Destination (Join-Path $pluginTarget 'LICENSE')
Copy-Item -LiteralPath (Join-Path $repoRoot 'THIRD-PARTY.md') -Destination (Join-Path $pluginTarget 'THIRD-PARTY.md')

Copy-Item -LiteralPath (Join-Path $repoRoot 'licenses') -Destination $pluginTarget -Recurse

$required = @(
    'RetakesAllocator.dll', 'RetakesAllocatorCore.dll', 'RetakesAllocator.deps.json',
    'SQLitePCLRaw.batteries_v2.dll', 'SQLitePCLRaw.core.dll', 'Microsoft.Data.Sqlite.dll',
    'runtimes/linux-x64/native/libe_sqlite3.so', 'lang/en.json'
)
foreach ($relative in $required) {
    if (-not (Test-Path -LiteralPath (Join-Path $pluginTarget $relative))) { throw "Missing package file: $relative" }
}
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = Join-Path $artifacts ("cs2-retakes-allocator-$Version-linux-x64.zip")
$stream = [IO.File]::Open($zip, [IO.FileMode]::Create)
$archive = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Create)
try {
    foreach ($file in Get-ChildItem -LiteralPath $staging -File -Recurse) {
        $name = $file.FullName.Substring($staging.Length + 1).Replace([char]92, [char]47)
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $name, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
}
finally { $archive.Dispose(); $stream.Dispose() }
Write-Host "Package: $zip"
