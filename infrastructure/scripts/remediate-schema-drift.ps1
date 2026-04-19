#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Diagnose and remediate database schema drift

.DESCRIPTION
    Detects when __EFMigrationsHistory is out of sync with actual database schema.
    Executes remediation SQL script to fix drift, then re-applies migrations.
    
    This script is designed to run in GitHub Actions but can also run locally
    with appropriate Azure SQL credentials.

.PARAMETER ConnectionString
    Azure SQL connection string (will be masked in CI/CD logs)

.PARAMETER DryRun
    If specified, only diagnoses drift without applying remediation

.EXAMPLE
    ./remediate-schema-drift.ps1 -ConnectionString "Server=..." -DryRun

.EXAMPLE
    # In GitHub Actions:
    $conn = terraform output -raw connection_string
    ./remediate-schema-drift.ps1 -ConnectionString $conn

.NOTES
    Author: Infrastructure Team
    Date: 2026-04-19
    Related: docs/MIGRATION-FAILURE-ANALYSIS.md
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ConnectionString,
    
    [Parameter(Mandatory = $false)]
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ============================================================================
# Functions
# ============================================================================

function Write-LogHeader {
    param([string]$Message)
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host " $Message" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""
}

function Write-LogSuccess {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Write-LogError {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor Red
}

function Write-LogWarning {
    param([string]$Message)
    Write-Host "⚠ $Message" -ForegroundColor Yellow
}

function Write-LogInfo {
    param([string]$Message)
    Write-Host "  $Message" -ForegroundColor Gray
}

function Invoke-SqlQuery {
    param(
        [string]$Query,
        [string]$ConnectionString
    )
    
    try {
        $connection = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
        $connection.Open()
        
        $command = $connection.CreateCommand()
        $command.CommandText = $Query
        $command.CommandTimeout = 30
        
        $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($command)
        $dataset = New-Object System.Data.DataSet
        [void]$adapter.Fill($dataset)
        
        $connection.Close()
        
        return $dataset.Tables[0]
    }
    catch {
        Write-LogError "SQL query failed: $_"
        throw
    }
}

function Invoke-SqlScript {
    param(
        [string]$ScriptPath,
        [string]$ConnectionString
    )
    
    try {
        $connection = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
        $connection.Open()
        
        $script = Get-Content -Path $ScriptPath -Raw
        
        # Split on GO statements
        $batches = $script -split '\r?\nGO\r?\n'
        
        foreach ($batch in $batches) {
            if ([string]::IsNullOrWhiteSpace($batch)) { continue }
            
            $command = $connection.CreateCommand()
            $command.CommandText = $batch
            $command.CommandTimeout = 60
            
            $reader = $command.ExecuteReader()
            
            # Capture PRINT statements
            while ($reader.Read()) {
                if ($reader.FieldCount -gt 0) {
                    for ($i = 0; $i -lt $reader.FieldCount; $i++) {
                        Write-LogInfo $reader.GetValue($i)
                    }
                }
            }
            $reader.Close()
        }
        
        $connection.Close()
        
        Write-LogSuccess "SQL script executed successfully"
    }
    catch {
        Write-LogError "SQL script execution failed: $_"
        throw
    }
}

# ============================================================================
# Main Script
# ============================================================================

Write-LogHeader "Database Schema Drift Remediation"

# Validate connection string
if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    Write-LogError "Connection string cannot be empty"
    exit 1
}

# Parse connection details for logging (mask password)
if ($ConnectionString -match 'Server=([^;]+).*Database=([^;]+)') {
    $server = $matches[1]
    $database = $matches[2]
    Write-LogInfo "Target: $server/$database"
}

Write-Host ""

# ============================================================================
# Step 1: Diagnose drift
# ============================================================================

Write-Host "Step 1: Diagnosing schema drift..." -ForegroundColor Yellow
Write-Host ""

try {
    # Check migration history
    $migrationCheckQuery = @"
SELECT CASE 
    WHEN EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20260209204020_AddActorEntity')
    THEN 1 ELSE 0 
END AS MigrationExists
"@
    
    $migrationResult = Invoke-SqlQuery -Query $migrationCheckQuery -ConnectionString $ConnectionString
    $migrationExists = $migrationResult.Rows[0]['MigrationExists'] -eq 1
    
    if ($migrationExists) {
        Write-LogSuccess "Migration record found in __EFMigrationsHistory"
    }
    else {
        Write-LogWarning "Migration record NOT found - unexpected state"
    }
    
    # Check table existence
    $tableCheckQuery = @"
SELECT CASE 
    WHEN OBJECT_ID('Actors', 'U') IS NOT NULL 
    THEN 1 ELSE 0 
END AS TableExists
"@
    
    $tableResult = Invoke-SqlQuery -Query $tableCheckQuery -ConnectionString $ConnectionString
    $tableExists = $tableResult.Rows[0]['TableExists'] -eq 1
    
    if ($tableExists) {
        Write-LogSuccess "Actors table exists in database"
    }
    else {
        Write-LogWarning "Actors table MISSING from database"
    }
    
    Write-Host ""
    
    # Determine drift state
    $driftDetected = $migrationExists -and (-not $tableExists)
    
    if ($driftDetected) {
        Write-LogError "SCHEMA DRIFT DETECTED"
        Write-LogInfo "Migration recorded but table missing - remediation required"
    }
    else {
        Write-LogSuccess "No schema drift detected"
        if (-not $migrationExists -and -not $tableExists) {
            Write-LogInfo "Migration and table both missing - normal state before first migration"
        }
        elseif ($migrationExists -and $tableExists) {
            Write-LogInfo "Schema is consistent with migration history"
        }
    }
}
catch {
    Write-LogError "Drift diagnosis failed: $_"
    exit 1
}

Write-Host ""

# ============================================================================
# Step 2: Remediate (if needed and not dry-run)
# ============================================================================

if ($driftDetected) {
    if ($DryRun) {
        Write-LogWarning "DRY RUN mode - skipping remediation"
        Write-LogInfo "Run without -DryRun to apply remediation"
        exit 2  # Exit code 2 = drift detected but not fixed
    }
    
    Write-Host "Step 2: Applying remediation..." -ForegroundColor Yellow
    Write-Host ""
    
    $scriptPath = Join-Path $PSScriptRoot "fix-schema-drift.sql"
    
    if (-not (Test-Path $scriptPath)) {
        Write-LogError "Remediation script not found: $scriptPath"
        exit 1
    }
    
    try {
        Invoke-SqlScript -ScriptPath $scriptPath -ConnectionString $ConnectionString
        
        Write-Host ""
        Write-LogSuccess "Remediation completed successfully"
        Write-LogInfo "Migration record removed from __EFMigrationsHistory"
        Write-LogInfo "Ready for EF Core to re-apply migration"
    }
    catch {
        Write-LogError "Remediation failed: $_"
        exit 1
    }
}
else {
    Write-LogInfo "No remediation needed - schema is healthy"
}

Write-Host ""
Write-LogHeader "Remediation Complete"

exit 0
