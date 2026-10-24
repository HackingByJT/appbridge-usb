[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$projectRoot = Split-Path -Parent $PSScriptRoot
$outputDirectory = Join-Path $projectRoot 'build\app'
$compilerPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not [Environment]::Is64BitOperatingSystem -or -not (Test-Path -LiteralPath $compilerPath -PathType Leaf)) {
    throw 'Build requires 64-bit Windows with the .NET Framework C# compiler.'
}
$frameworkRelease = Get-ItemPropertyValue -LiteralPath 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' -Name Release
if ($frameworkRelease -lt 528040) {
    throw 'Build requires .NET Framework 4.8 or later.'
}

# Prefer the 4.8 targeting pack; Windows also supplies the installed 4.x runtime
# assemblies, which are updated in place to the installed .NET Framework release.
$referenceDirectory = Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8'
if (-not (Test-Path -LiteralPath (Join-Path $referenceDirectory 'mscorlib.dll') -PathType Leaf)) {
    $referenceDirectory = Split-Path -Parent $compilerPath
    Write-Host 'Using installed .NET Framework assemblies (4.8 targeting pack was not found).'
}
$referenceNames = @(
    'mscorlib.dll', 'System.dll', 'System.Core.dll', 'System.Drawing.dll',
    'System.Windows.Forms.dll', 'System.Net.Http.dll', 'System.IO.Compression.dll',
    'System.IO.Compression.FileSystem.dll', 'System.Xml.dll'
)
$compilerArguments = @('/nologo', '/noconfig', '/nostdlib+', '/target:winexe', '/platform:x64', '/langversion:5', '/optimize+', '/utf8output')
$compilerArguments += '/win32manifest:' + (Join-Path $projectRoot 'installer\app.manifest')
foreach ($referenceName in $referenceNames) {
    $referencePath = Join-Path $referenceDirectory $referenceName
    if (-not (Test-Path -LiteralPath $referencePath -PathType Leaf)) {
        throw ('Required .NET Framework reference is missing: ' + $referencePath)
    }
    $compilerArguments += '/reference:' + $referencePath
}

# Keep both entry points separate. Do not replace these lists with a source glob.
$guiSources = @('Program.cs', 'AppBridgeForm.cs', 'AdbService.cs', 'Models.cs', 'RuntimeManager.cs')
$controllerSources = @('AppBridgeControls.cs')
$sourceSets = @(
    @{ Name = 'AppBridge.exe'; Sources = $guiSources },
    @{ Name = 'AppBridgeControls.exe'; Sources = $controllerSources }
)
foreach ($sourceSet in $sourceSets) {
    foreach ($sourceName in $sourceSet.Sources) {
        $sourcePath = Join-Path (Join-Path $projectRoot 'src') $sourceName
        if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
            throw ('Source is missing: ' + $sourcePath)
        }
    }
}

New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
$compileDirectory = Join-Path (Join-Path $projectRoot 'build') ('compile-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $compileDirectory | Out-Null
try {
    foreach ($sourceSet in $sourceSets) {
        $arguments = @($compilerArguments)
        $arguments += '/out:' + (Join-Path $compileDirectory $sourceSet.Name)
        foreach ($sourceName in $sourceSet.Sources) {
            $arguments += Join-Path (Join-Path $projectRoot 'src') $sourceName
        }
        & $compilerPath @arguments
        if ($LASTEXITCODE -ne 0) { throw ('Compilation failed: ' + $sourceSet.Name) }
    }
    $applicationConfig = @'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <startup>
    <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.8" />
  </startup>
</configuration>
'@
    foreach ($sourceSet in $sourceSets) {
        Copy-Item -LiteralPath (Join-Path $compileDirectory $sourceSet.Name) -Destination (Join-Path $outputDirectory $sourceSet.Name) -Force
        [IO.File]::WriteAllText((Join-Path $outputDirectory ($sourceSet.Name + '.config')), $applicationConfig, [Text.UTF8Encoding]::new($false))
    }
}
finally {
    # The temporary directory only receives these two explicitly named binaries.
    foreach ($sourceSet in $sourceSets) {
        $temporaryBinary = Join-Path $compileDirectory $sourceSet.Name
        if (Test-Path -LiteralPath $temporaryBinary -PathType Leaf) { Remove-Item -LiteralPath $temporaryBinary -Force }
    }
    Remove-Item -LiteralPath $compileDirectory -Force
}
Write-Host ('Build output: ' + $outputDirectory)
