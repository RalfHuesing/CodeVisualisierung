[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Release",

    [ValidateSet("win-x64", "win-arm64")]
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

$deploymentDirectory = Join-Path $distributionRoot "csharp-cli"
$resolvedDistributionRoot = (Resolve-Path -LiteralPath $distributionRoot).Path.TrimEnd([IO.Path]::DirectorySeparatorChar)
$resolvedDeploymentDirectory = [IO.Path]::GetFullPath($deploymentDirectory).TrimEnd([IO.Path]::DirectorySeparatorChar)
$deploymentPrefix = "$resolvedDistributionRoot$([IO.Path]::DirectorySeparatorChar)"

if ($resolvedDeploymentDirectory -eq $resolvedDistributionRoot -or
    -not $resolvedDeploymentDirectory.StartsWith($deploymentPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to delete deployment path outside dist: $resolvedDeploymentDirectory"
}

if (Test-Path -LiteralPath $deploymentDirectory) {
    Write-Host "Removing previous local C# CLI deployment: $deploymentDirectory"
    Remove-Item -LiteralPath $deploymentDirectory -Recurse -Force
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

$analysisInputPath = Join-Path $repositoryRoot "adapters\csharp\CodeVisualisierung.CSharp.slnx"
$graphPath = Join-Path $deploymentDirectory "graph-universe.json"

if (-not (Test-Path -LiteralPath $analysisInputPath -PathType Leaf)) {
    throw "C# adapter solution not found: $analysisInputPath"
}

Write-Host "Generating graph JSON from $analysisInputPath"
& $executablePath $analysisInputPath --output $graphPath

if ($LASTEXITCODE -ne 0) {
    throw "C# CLI analysis failed with exit code $LASTEXITCODE"
}

if (-not (Test-Path -LiteralPath $graphPath -PathType Leaf)) {
    throw "Graph JSON was not created: $graphPath"
}

Write-Host "Local C# CLI deployment ready: $executablePath"
Write-Host "Graph JSON ready: $graphPath"
Write-Output $executablePath
Write-Output $graphPath
