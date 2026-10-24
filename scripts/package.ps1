[CmdletBinding()]
param(
    [string]$MakeNsisPath = $env:APPBRIDGE_MAKENSIS,
    [ValidatePattern('^\d+\.\d+\.\d+(?:-[a-z0-9][a-z0-9.-]*)?$')]
    [string]$Version = '0.1.0-beta',
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$projectRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($MakeNsisPath)) {
    $makeNsisCommand = Get-Command 'makensis.exe' -ErrorAction SilentlyContinue
    if ($null -ne $makeNsisCommand) { $MakeNsisPath = $makeNsisCommand.Source }
}
if ([string]::IsNullOrWhiteSpace($MakeNsisPath) -or -not (Test-Path -LiteralPath $MakeNsisPath -PathType Leaf)) {
    throw 'Provide -MakeNsisPath pointing to the extracted NSIS makensis.exe. See installer/TOOLCHAIN.md.'
}
$MakeNsisPath = (Resolve-Path -LiteralPath $MakeNsisPath).ProviderPath
if (-not $SkipBuild) { & (Join-Path $PSScriptRoot 'build.ps1') }

$applicationDirectory = Join-Path $projectRoot 'build\app'
# This is the complete distributable payload. Never package the workspace tree.
$payload = @(
    @{ Source = (Join-Path $applicationDirectory 'AppBridge.exe'); Name = 'AppBridge.exe' },
    @{ Source = (Join-Path $applicationDirectory 'AppBridge.exe.config'); Name = 'AppBridge.exe.config' },
    @{ Source = (Join-Path $applicationDirectory 'AppBridgeControls.exe'); Name = 'AppBridgeControls.exe' },
    @{ Source = (Join-Path $applicationDirectory 'AppBridgeControls.exe.config'); Name = 'AppBridgeControls.exe.config' },
    @{ Source = (Join-Path $projectRoot 'README.md'); Name = 'README.md' },
    @{ Source = (Join-Path $projectRoot 'README.es.md'); Name = 'README.es.md' },
    @{ Source = (Join-Path $projectRoot 'LICENSE'); Name = 'LICENSE' },
    @{ Source = (Join-Path $projectRoot 'THIRD_PARTY_NOTICES.md'); Name = 'THIRD_PARTY_NOTICES.md' }
)
foreach ($entry in $payload) {
    if (-not (Test-Path -LiteralPath $entry.Source -PathType Leaf)) { throw ('Payload file is missing: ' + $entry.Name) }
}

$distributionDirectory = Join-Path $projectRoot 'dist'
New-Item -ItemType Directory -Path $distributionDirectory -Force | Out-Null
$artifactPrefix = 'AppBridgeUSB-' + $Version + '-win64'
$portablePath = Join-Path $distributionDirectory ($artifactPrefix + '-portable.zip')
$installerPath = Join-Path $distributionDirectory ($artifactPrefix + '-setup.exe')
$manifestPath = Join-Path $distributionDirectory ($artifactPrefix + '-payload.json')
$checksumPath = Join-Path $distributionDirectory 'SHA256SUMS.txt'
$stagingDirectory = Join-Path (Join-Path $projectRoot 'build') ('package-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stagingDirectory | Out-Null
try {
    $manifestFiles = @()
    foreach ($entry in $payload) {
        $destinationPath = Join-Path $stagingDirectory $entry.Name
        Copy-Item -LiteralPath $entry.Source -Destination $destinationPath
        $manifestFiles += [ordered]@{
            path = $entry.Name
            bytes = (Get-Item -LiteralPath $destinationPath).Length
            sha256 = (Get-FileHash -LiteralPath $destinationPath -Algorithm SHA256).Hash.ToLowerInvariant()
        }
    }
    $payloadManifest = [ordered]@{
        schema = 'appbridge-usb.payload.v1'
        version = $Version
        platform = 'windows-x64'
        runtimeIncluded = $false
        files = $manifestFiles
    }
    [IO.File]::WriteAllText($manifestPath, ($payloadManifest | ConvertTo-Json -Depth 5) + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    if (Test-Path -LiteralPath $portablePath -PathType Leaf) { Remove-Item -LiteralPath $portablePath -Force }
    [IO.Compression.ZipFile]::CreateFromDirectory($stagingDirectory, $portablePath, [IO.Compression.CompressionLevel]::Optimal, $false)

    $numericVersion = ($Version -split '-')[0] + '.0'
    $installerArguments = @(
        '/V3', '/WX', '/NOCONFIG', '/INPUTCHARSET', 'UTF8',
        ('/DAPP_VERSION=' + $Version), ('/DAPP_VERSION_NUMERIC=' + $numericVersion),
        ('/DPAYLOAD_DIR=' + $stagingDirectory), ('/DOUTPUT_FILE=' + $installerPath),
        (Join-Path $projectRoot 'installer\AppBridgeUSB.nsi')
    )
    & $MakeNsisPath @installerArguments
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $installerPath -PathType Leaf)) { throw 'NSIS compilation failed.' }

    $checksumLines = @()
    foreach ($artifactPath in @($installerPath, $portablePath, $manifestPath)) {
        $checksumLines += (Get-FileHash -LiteralPath $artifactPath -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + [IO.Path]::GetFileName($artifactPath)
    }
    [IO.File]::WriteAllLines($checksumPath, $checksumLines, [Text.UTF8Encoding]::new($false))
}
finally {
    # Only remove the known files we copied. An unexpected file prevents directory removal.
    foreach ($entry in $payload) {
        $stagedFile = Join-Path $stagingDirectory $entry.Name
        if (Test-Path -LiteralPath $stagedFile -PathType Leaf) { Remove-Item -LiteralPath $stagedFile -Force }
    }
    Remove-Item -LiteralPath $stagingDirectory -Force
}
Write-Host ('Installer: ' + $installerPath)
Write-Host ('Portable ZIP: ' + $portablePath)
Write-Host ('Payload manifest: ' + $manifestPath)
Write-Host 'Compilation completed. Installation and uninstallation have not been exercised by this script.'
