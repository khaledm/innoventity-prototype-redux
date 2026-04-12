# infrastructure/scripts/create-environment.ps1
# Orchestrates the full environment lifecycle: data/ apply → core/ apply → migrations → validate
#
# Usage:
#   ./infrastructure/scripts/create-environment.ps1 -Environment dev
#   ./infrastructure/scripts/create-environment.ps1 -Environment dev -SkipMigrations
#
# Prerequisites:
#   - Terraform 1.6+ on PATH
#   - Azure CLI authenticated (az login)
#   - dotnet CLI on PATH (for migrations)
#   - terraform.tfvars in environments/{env}/data/ and environments/{env}/core/ (never committed)

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet("dev", "test", "prod")]
    [string]$Environment,

    [switch]$SkipMigrations,
    [switch]$WhatIf
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$script:StartTime = Get-Date  # H5: timing gate — elapsed time reported at completion

$RepoRoot    = Resolve-Path (Join-Path $PSScriptRoot "../..")
$DataDir     = Join-Path $RepoRoot "infrastructure/environments/$Environment/data"
$CoreDir     = Join-Path $RepoRoot "infrastructure/environments/$Environment/core"
$ApiProject  = Join-Path $RepoRoot "src/Innoventity.API"

function Write-Step([string]$message) {
    Write-Host "`n═══ $message ═══" -ForegroundColor Cyan
}

# ─────────────────────────────────────────────────────────────────
# Step 1: Apply data layer (Resource Group + SQL)
# ─────────────────────────────────────────────────────────────────
Write-Step "Step 1/5 — Apply data layer ($Environment/data)"
Push-Location $DataDir
try {
    terraform init -input=false
    terraform plan -out=tfplan -input=false
    if (-not $WhatIf) {
        terraform apply tfplan
    }
    $ConnectionString = terraform output -raw connection_string
    Write-Host "✅ Data layer applied. Connection string captured." -ForegroundColor Green
}
finally {
    Pop-Location
}

# ─────────────────────────────────────────────────────────────────
# Step 2: Apply core layer (App Service + App Insights)
# ─────────────────────────────────────────────────────────────────
Write-Step "Step 2/5 — Apply core layer ($Environment/core)"

# jwt_secret_key must be provided; read from tfvars or prompt
$JwtKey = $env:JWT_SECRET_KEY
if (-not $JwtKey) {
    Write-Warning "JWT_SECRET_KEY env var not set — generating random key for this session only"
    $JwtKey = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
}

Push-Location $CoreDir
try {
    terraform init -input=false
    terraform plan -out=tfplan -input=false `
        -var "connection_string=$ConnectionString" `
        -var "jwt_secret_key=$JwtKey"
    if (-not $WhatIf) {
        terraform apply tfplan
    }
    $AppHostname = terraform output -raw app_service_hostname
    $AppName     = terraform output -raw app_service_name
    Write-Host "✅ Core layer applied. App Service: $AppName ($AppHostname)" -ForegroundColor Green
}
finally {
    Pop-Location
}

# ─────────────────────────────────────────────────────────────────
# Step 3: Apply EF Core migrations
# ─────────────────────────────────────────────────────────────────
Write-Step "Step 3/5 — Apply EF Core migrations"
if (-not $SkipMigrations -and -not $WhatIf) {
    Push-Location $RepoRoot
    try {
        dotnet ef database update `
            --project $ApiProject `
            --connection $ConnectionString
        Write-Host "✅ Migrations applied." -ForegroundColor Green
    }
    finally {
        Pop-Location
    }
} else {
    Write-Host "⏭  Migrations skipped (-SkipMigrations or -WhatIf)." -ForegroundColor Yellow
}

# ─────────────────────────────────────────────────────────────────
# Step 4: Validate
# ─────────────────────────────────────────────────────────────────
Write-Step "Step 4/5 — Validate environment"
if (-not $WhatIf) {
    $HealthUrl = "https://$AppHostname/health"
    Write-Host "Polling $HealthUrl ..."
    $attempts = 0
    do {
        Start-Sleep -Seconds 10
        $attempts++
        try {
            $response = Invoke-RestMethod -Uri $HealthUrl -ErrorAction Stop
            if ($response.status -eq "Healthy") {
                Write-Host "✅ Environment healthy after $($attempts * 10)s: $HealthUrl" -ForegroundColor Green
                break
            }
        } catch {
            Write-Host "  Attempt $attempts — not ready yet ($_)"
        }
    } while ($attempts -lt 18)  # 3 minutes max

    if ($attempts -ge 18) {
        Write-Host "  Fetching recent App Service logs for diagnosis:" -ForegroundColor Yellow
        az webapp log tail `
            --name $AppName `
            --resource-group (terraform -chdir=$CoreDir output -raw resource_group_name) `
            --provider application `
            --filter Error `
            --timeout 10 2>&1 | Select-Object -Last 20 | ForEach-Object { Write-Host "    $_" }
        throw "❌ Environment '$Environment' did not become healthy within 3 minutes. " +
              "Check App Service logs above or run: az webapp log tail --name $AppName"
    }
}

Write-Host "`n✅ Environment '$Environment' created successfully." -ForegroundColor Green
Write-Host "   Run Pester tests: .\infrastructure\scripts\validate-environment.ps1 -Environment $Environment"

# ─────────────────────────────────────────────────────────────────
# Step 5: Idempotency check (H5) — second plan must show no changes
# terraform plan -detailed-exitcode returns:
#   exit 0 = no changes (idempotent ✔)
#   exit 2 = changes pending (drift detected ✘)
#   exit 1 = error
# ─────────────────────────────────────────────────────────────────
Write-Step "Step 5/5 — Idempotency check"
if (-not $WhatIf) {
    Write-Host "  Running idempotency plan for data/ ..."
    Push-Location $DataDir
    try {
        terraform plan -detailed-exitcode -input=false | Out-Null
        if ($LASTEXITCODE -eq 2) {
            throw "❌ Idempotency failure in data/: terraform plan detected pending changes after apply. Check for non-deterministic resources."
        }
        Write-Host "  ✅ data/ is idempotent (no pending changes)" -ForegroundColor Green
    } finally { Pop-Location }

    Write-Host "  Running idempotency plan for core/ ..."
    Push-Location $CoreDir
    try {
        terraform plan -detailed-exitcode -input=false `
            -var "connection_string=$ConnectionString" `
            -var "jwt_secret_key=$JwtKey" | Out-Null
        if ($LASTEXITCODE -eq 2) {
            throw "❌ Idempotency failure in core/: terraform plan detected pending changes after apply. Check for non-deterministic resources."
        }
        Write-Host "  ✅ core/ is idempotent (no pending changes)" -ForegroundColor Green
    } finally { Pop-Location }
}

$elapsed = [math]::Round(((Get-Date) - $script:StartTime).TotalMinutes, 1)
Write-Host "`n✅ Wave complete. Environment '$Environment' created and validated in ${elapsed}m." -ForegroundColor Green
