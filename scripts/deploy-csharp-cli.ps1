[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Release",

    [string] $Runtime = "win-x64"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$projectPath = Join-Path $repositoryRoot "adapters\csharp\src\CodeVisualisierung.CSharp.Cli\CodeVisualisierung.CSharp.Cli.csproj"
$distributionRoot = Join-Path $repositoryRoot "dist"

if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
    throw "C# CLI project not found: $projectPath"
}

New-Item -ItemType Directory -Path $distributionRoot -Force | Out-Null

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$deploymentDirectory = Join-Path $distributionRoot "csharp-cli-$stamp"
$suffix = 1

while (Test-Path -LiteralPath $deploymentDirectory) {
    $deploymentDirectory = Join-Path $distributionRoot ("csharp-cli-{0}-{1:D2}" -f $stamp, $suffix)
    $suffix++
}

New-Item -ItemType Directory -Path $deploymentDirectory | Out-Null

$publishArguments = @(
    "publish"
    $projectPath
    "--configuration"
    $Configuration
    "--runtime"
    $Runtime
    "--self-contained"
    "true"
    "--output"
    $deploymentDirectory
    "--nologo"
)

Write-Host "Publishing C# CLI to $deploymentDirectory"
& dotnet @publishArguments

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$executablePath = Join-Path $deploymentDirectory "codegraph-csharp.exe"
if (-not (Test-Path -LiteralPath $executablePath -PathType Leaf)) {
    throw "Published executable not found: $executablePath"
}

Write-Host "Local C# CLI deployment ready: $executablePath"
Write-Output $executablePath
