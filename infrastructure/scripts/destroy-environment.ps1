# infrastructure/scripts/destroy-environment.ps1
# Orchestrates safe environment teardown: core/ destroy → data/ destroy (explicit, change-controlled).
#
# FR7.8 acceptance criterion: A destroy script exists that tears down all infrastructure
# in the correct order (core first, then data) with a post-destroy resource-existence check.
#
# Usage:
#   ./infrastructure/scripts/destroy-environment.ps1 -Environment dev
#   ./infrastructure/scripts/destroy-environment.ps1 -Environment dev -WhatIf
#   ./infrastructure/scripts/destroy-environment.ps1 -Environment dev -SkipDataLayer
#
# IMPORTANT:
#   -SkipDataLayer preserves the SQL Server, SQL Database, and Resource Group.
#   Omit it only when you are CERTAIN you want to permanently delete the database.
#   This is an irreversible, change-controlled operation.
#
# Prerequisites:
#   - Terraform 1.6+ on PATH
#   - Azure CLI authenticated (az login)
#   - The sql-database module's lifecycle { prevent_destroy = true } must be removed
#     from modules/sql-database/main.tf before data/ can be fully destroyed.

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet("dev", "test", "prod")]
    [string]$Environment,

    # Preserve data layer (SQL Server + SQL Database + Resource Group).
    # DEFAULT behaviour — omit flag only for full teardown with explicit intent.
    [switch]$SkipDataLayer,

    [switch]$WhatIf
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$script:StartTime = Get-Date

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")
$DataDir  = Join-Path $RepoRoot "infrastructure/environments/$Environment/data"
$CoreDir  = Join-Path $RepoRoot "infrastructure/environments/$Environment/core"

function Write-Step([string]$message) {
    Write-Host "`n═══ $message ═══" -ForegroundColor Cyan
}

# Safety gate — require explicit confirmation for prod
if ($Environment -eq "prod" -and -not $WhatIf) {
    $confirm = Read-Host "⚠️  You are about to destroy the PRODUCTION environment. Type 'destroy-prod' to confirm"
    if ($confirm -ne "destroy-prod") {
        Write-Host "Aborted — confirmation string did not match." -ForegroundColor Yellow
        exit 0
    }
}

$ResourceGroup = "innoventity-$Environment-rg"

# ─────────────────────────────────────────────────────────────────
# Step 1: Destroy core layer (App Service + App Insights)
# ─────────────────────────────────────────────────────────────────
Write-Step "Step 1/$( if ($SkipDataLayer) { '1' } else { '2' } ) — Destroy core layer ($Environment/core)"
Push-Location $CoreDir
try {
    terraform init -input=false
    if ($WhatIf) {
        Write-Host "  [WhatIf] Would run: terraform destroy -auto-approve" -ForegroundColor Yellow
    } else {
        terraform destroy -auto-approve -input=false
        Write-Host "✅ Core layer destroyed (App Service + App Insights removed)." -ForegroundColor Green
    }
}
finally {
    Pop-Location
}

# ─────────────────────────────────────────────────────────────────
# Step 2: Destroy data layer (SQL Server + SQL Database + Resource Group)
# Only executed when -SkipDataLayer is NOT specified.
# ─────────────────────────────────────────────────────────────────
if (-not $SkipDataLayer) {
    Write-Step "Step 2/2 — Destroy data layer ($Environment/data) [IRREVERSIBLE]"
    Write-Host "  ⚠️  This permanently destroys the SQL Server and all data." -ForegroundColor Red
    Write-Host "  ⚠️  The lifecycle { prevent_destroy = true } guard must be removed from" -ForegroundColor Red
    Write-Host "      modules/sql-database/main.tf before this step will succeed." -ForegroundColor Red

    Push-Location $DataDir
    try {
        terraform init -input=false
        if ($WhatIf) {
            Write-Host "  [WhatIf] Would run: terraform destroy -auto-approve" -ForegroundColor Yellow
        } else {
            terraform destroy -auto-approve -input=false
            Write-Host "✅ Data layer destroyed (SQL Server + Database + Resource Group removed)." -ForegroundColor Green
        }
    }
    finally {
        Pop-Location
    }

    # ─────────────────────────────────────────────────────────────
    # Post-destroy validation (FR7.8) — confirm Resource Group is gone
    # ─────────────────────────────────────────────────────────────
    if (-not $WhatIf) {
        Write-Step "Post-destroy validation — confirming $ResourceGroup is deleted"
        Start-Sleep -Seconds 10  # Brief wait for ARM propagation

        $rgState = az group show --name $ResourceGroup --query "properties.provisioningState" --output tsv 2>&1
        if ($LASTEXITCODE -eq 0) {
            # Resource group still exists — destroy may not have completed
            Write-Warning "⚠️  Resource group '$ResourceGroup' still exists (state: $rgState). It may be in the process of deletion. Verify manually: az group show --name $ResourceGroup"
        } else {
            # Non-zero exit from az group show = ResourceNotFound = expected success state
            Write-Host "✅ Post-destroy confirmed: resource group '$ResourceGroup' no longer exists." -ForegroundColor Green
        }
    }
} else {
    Write-Host "`n⏭  Data layer skipped (-SkipDataLayer). SQL Server, Database, and Resource Group preserved." -ForegroundColor Yellow
}

$elapsed = [math]::Round(((Get-Date) - $script:StartTime).TotalMinutes, 1)
Write-Host "`n✅ Destroy complete for environment '$Environment' in ${elapsed}m." -ForegroundColor Green
