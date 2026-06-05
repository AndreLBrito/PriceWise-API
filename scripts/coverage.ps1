param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$resultsDirectory = Join-Path $root "artifacts/TestResults"

New-Item -ItemType Directory -Force -Path $resultsDirectory | Out-Null

dotnet test (Join-Path $root "PriceWise.slnx") `
    --configuration $Configuration `
    --collect:"XPlat Code Coverage" `
    --results-directory $resultsDirectory

Write-Host "Relatorio de cobertura gerado em: $resultsDirectory"
