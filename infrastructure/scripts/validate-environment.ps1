# infrastructure/scripts/validate-environment.ps1
# Invokes the full Pester test suite against a live environment
#
# Usage:
#   ./infrastructure/scripts/validate-environment.ps1 -Environment dev
#   ./infrastructure/scripts/validate-environment.ps1 -Environment dev -TestFile HealthCheck
#
# Prerequisites:
#   - Pester v5 installed: Install-Module Pester -Force
#   - Azure CLI authenticated
#   - Environment must be provisioned (create-environment.ps1 has been run)
#   - Terraform outputs available in environments/{env}/core/

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet("dev", "test", "prod")]
    [string]$Environment,

    [ValidateSet("All", "AppService", "SqlDatabase", "Secrets", "HealthCheck")]
    [string]$TestFile = "All"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
$CoreDir  = Join-Path $RepoRoot "infrastructure/environments/$Environment/core"
$PesterDir = Join-Path $RepoRoot "infrastructure/tests/pester"

# ─────────────────────────────────────────────────────────────────
# Collect environment outputs
# ─────────────────────────────────────────────────────────────────

# data/ outputs: SQL_SERVER_NAME (required by SqlDatabase.Tests.ps1)
$DataDir = Join-Path $RepoRoot "infrastructure/environments/$Environment/data"
Write-Host "Reading Terraform outputs from $Environment/data ..." -ForegroundColor Cyan
Push-Location $DataDir
try {
    $env:SQL_SERVER_NAME = terraform output -raw sql_server_name
}
finally {
    Pop-Location
}

# core/ outputs: APP_SERVICE_NAME, RESOURCE_GROUP_NAME
Write-Host "Reading Terraform outputs from $Environment/core ..." -ForegroundColor Cyan
Push-Location $CoreDir
try {
    $env:APP_SERVICE_NAME    = terraform output -raw app_service_name
    $env:RESOURCE_GROUP_NAME = terraform output -raw resource_group_name
}
finally {
    Pop-Location
}

# ENVIRONMENT — used by SqlDatabase.Tests.ps1 to conditionally skip/check AllowLocalDevelopment
$env:ENVIRONMENT = $Environment

Write-Host "  APP_SERVICE_NAME    = $($env:APP_SERVICE_NAME)"
Write-Host "  RESOURCE_GROUP_NAME = $($env:RESOURCE_GROUP_NAME)"
Write-Host "  SQL_SERVER_NAME     = $($env:SQL_SERVER_NAME)"
Write-Host "  ENVIRONMENT         = $($env:ENVIRONMENT)"

# ─────────────────────────────────────────────────────────────────
# Run Pester -CI
# -CI flag: exits with non-zero on any test failure (required for pipeline use)
# ─────────────────────────────────────────────────────────────────
$pesterArgs = @{
    CI = $true
}

if ($TestFile -eq "All") {
    $pesterArgs.Path = $PesterDir
    Write-Host "`nRunning all Pester tests in $PesterDir" -ForegroundColor Cyan
} else {
    $pesterArgs.Path = Join-Path $PesterDir "$TestFile.Tests.ps1"
    Write-Host "`nRunning $TestFile.Tests.ps1" -ForegroundColor Cyan
}

Invoke-Pester @pesterArgs
