# Schema Drift Prevention and Remediation - Implementation Guide

## Overview

This implementation provides **automated detection and remediation** of database schema drift in the CI/CD pipeline, addressing the production issue where `__EFMigrationsHistory` was out of sync with actual database schema.

---

## What Was Implemented

### 1. Root Cause Analysis Document
**File:** `docs/MIGRATION-FAILURE-ANALYSIS.md`

Comprehensive analysis including:
- Evidence from deployment logs
- Impact assessment
- Verification procedures
- Prevention mechanisms
- Lessons learned

### 2. Remediation SQL Script
**File:** `infrastructure/scripts/fix-schema-drift.sql`

T-SQL script that:
- ✅ Detects migration record vs table existence mismatch
- ✅ Backs up `__EFMigrationsHistory` before remediation
- ✅ Removes orphaned migration record
- ✅ Prepares database for EF Core re-application
- ✅ Provides detailed diagnostic output

### 3. PowerShell Remediation Tool
**File:** `infrastructure/scripts/remediate-schema-drift.ps1`

Cross-platform PowerShell script for:
- ✅ Automated drift detection
- ✅ Dry-run mode for testing
- ✅ Detailed logging with color-coded output
- ✅ Safe execution with connection string masking

### 4. Enhanced CI/CD Pipeline
**File:** `.github/workflows/deploy.yml`

Added three new steps to `migrate` job:

#### Step 1: Detect and Remediate Schema Drift (BEFORE migrations)
```yaml
- name: Detect and remediate schema drift
```

**Actions:**
- Installs SQL command-line tools
- Checks if migration exists in history but table is missing
- Automatically executes remediation SQL if drift detected
- Logs all actions for audit trail

#### Step 2: Apply Migrations (EXISTING - now enhanced)
```yaml
- name: Apply migrations
```

**Enhancement:**
- Now runs AFTER drift remediation
- Will correctly create missing tables that were previously skipped

#### Step 3: Validate Schema Integrity (AFTER migrations)
```yaml
- name: Validate schema integrity
```

**Actions:**
- Verifies all critical tables exist (Actors, Innovations, Industries, Bids)
- Counts migrations in history table
- Fails deployment if validation detects issues
- Provides early warning of schema problems

---

## How It Works

### Normal Deployment Flow (No Drift)

```
┌─────────────────────────────────────┐
│ 1. Detect Schema Drift              │
│    ✓ No drift detected              │
└─────────────────────────────────────┘
            ↓
┌─────────────────────────────────────┐
│ 2. Apply Migrations                 │
│    ✓ dotnet ef database update     │
└─────────────────────────────────────┘
            ↓
┌─────────────────────────────────────┐
│ 3. Validate Schema                  │
│    ✓ All tables present             │
│    ✓ Migration count correct        │
└─────────────────────────────────────┘
```

### Drift Detected Flow

```
┌─────────────────────────────────────┐
│ 1. Detect Schema Drift              │
│    ⚠️  Migration exists but table   │
│       missing - DRIFT DETECTED      │
└─────────────────────────────────────┘
            ↓
┌─────────────────────────────────────┐
│    Execute Remediation SQL          │
│    • Backup __EFMigrationsHistory   │
│    • Remove orphaned record         │
│    ✓ Ready for re-application       │
└─────────────────────────────────────┘
            ↓
┌─────────────────────────────────────┐
│ 2. Apply Migrations                 │
│    ✓ Creates missing Actors table   │
│    ✓ Updates migration history      │
└─────────────────────────────────────┘
            ↓
┌─────────────────────────────────────┐
│ 3. Validate Schema                  │
│    ✓ Actors table now present       │
│    ✓ Schema consistent              │
└─────────────────────────────────────┘
```

---

## Testing the Implementation

### Option 1: Test via GitHub Actions (Recommended)

**Why:** Simulates real deployment environment, no firewall issues

1. **Commit and push changes:**
   ```powershell
   git add .
   git commit -m "feat: Add automated schema drift detection and remediation
   
   - Add drift detection step before migrations
   - Implement remediation SQL script
   - Add post-migration schema validation
   - Fixes #[issue-number] - Actors table missing in production"
   
   git push origin 002-frontend-cicd
   ```

2. **Trigger deployment workflow:**
   ```powershell
   gh workflow run deploy.yml --ref 002-frontend-cicd
   ```

3. **Monitor execution:**
   ```powershell
   gh run watch
   ```

4. **Check logs for remediation:**
   ```powershell
   gh run view --log | Select-String "drift|remediation|validation" -Context 2
   ```

**Expected Output (If Drift Exists):**
```
⚠️ Schema drift detected - applying remediation
✓ Remediation complete - migration will be re-applied
✓ All critical tables present
✓ Schema validation passed
```

### Option 2: Test Locally with Firewall Rule (Alternative)

**Prerequisites:**
- Azure SQL firewall allows your IP
- Terraform state configured

```powershell
# Navigate to repo root
cd C:\Users\mahmu\source\repos\innoventity-prototype-redux

# Get connection string from Terraform
cd infrastructure/environments/dev/data
terraform init
$conn = terraform output -raw connection_string

# Run remediation in dry-run mode first
cd ../../..
.\infrastructure\scripts\remediate-schema-drift.ps1 `
  -ConnectionString $conn `
  -DryRun

# If drift detected, run actual remediation
.\infrastructure\scripts\remediate-schema-drift.ps1 `
  -ConnectionString $conn

# Apply migrations manually
cd src/Innoventity.API
dotnet ef database update --connection $conn
```

---

## Verifying the Fix

### 1. Check Deployment Logs

```powershell
# View latest deployment run
gh run list --workflow deploy.yml --limit 1

# Check for successful migration
gh run view <RUN_ID> --log | Select-String "Apply migrations" -Context 5
```

**Success indicators:**
- ✅ `Applying migration '20260209204020_AddActorEntity'`
- ✅ `✓ Schema validation passed`
- ✅ `✓ All critical tables present`

### 2. Test API Endpoint

```powershell
# Test registration endpoint
curl -X POST https://innoventity-dev-api.azurewebsites.net/auth/register `
  -H "Content-Type: application/json" `
  -d '{
    "email": "test@example.com",
    "password": "Test123!@#",
    "actorType": "IdeaGenerator",
    "organizationName": "Test Org"
  }'
```

**Expected:** HTTP 200/201 (not 500)

### 3. Verify Table in Database

```powershell
# Using Azure Data Studio or sqlcmd:
SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Actors'
```

**Expected:** 1 row returned

---

## Rollback Plan

If remediation causes issues:

1. **Restore migration history backup:**
   ```sql
   DELETE FROM __EFMigrationsHistory;
   INSERT INTO __EFMigrationsHistory 
   SELECT * FROM __EFMigrationsHistory_Backup;
   ```

2. **Revert workflow changes:**
   ```powershell
   git revert HEAD
   git push origin 002-frontend-cicd
   ```

3. **Manual investigation:**
   Review `docs/MIGRATION-FAILURE-ANALYSIS.md` for alternative approaches

---

## Long-Term Benefits

### Prevents Future Drift
- ✅ Automated detection on every deployment
- ✅ No silent failures
- ✅ Audit trail in deployment logs

### Improves Reliability
- ✅ Self-healing pipeline
- ✅ Reduces manual intervention
- ✅ Catches issues before production

### Enhances Observability
- ✅ Clear diagnostic output
- ✅ Schema validation checkpoints
- ✅ Early warning system

---

## Maintenance

### Regular Reviews

**Monthly:**
- Review drift detection logs
- Check for patterns indicating systemic issues

**Quarterly:**
- Audit `__EFMigrationsHistory_Backup` tables (can be cleaned up)
- Review migration count threshold (currently 9)

### Updates Required When:

**New critical tables added:**
Update validation step in `.github/workflows/deploy.yml`:
```yaml
CASE WHEN OBJECT_ID('NewTable', 'U') IS NULL THEN 'NewTable,' ELSE '' END +
```

**Migration count changes:**
Update threshold check:
```yaml
if [ "$MIGRATION_COUNT" -lt "10" ]; then  # Update number
```

---

## Troubleshooting

### Issue: "sqlcmd: command not found"

**Cause:** SQL tools installation failed in GitHub Actions

**Fix:** Check Ubuntu version compatibility in workflow:
```yaml
sudo add-apt-repository "$(wget -qO- https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/prod.list)"
```

### Issue: Drift detected on every run

**Cause:** Remediation not completing successfully

**Debug:**
1. Check SQL script syntax
2. Verify connection string format
3. Review backup table creation logs

### Issue: Validation fails after migration

**Cause:** Migration applied but table still missing

**Debug:**
1. Check EF Core migration file for syntax errors
2. Verify database user has CREATE TABLE permissions
3. Review Azure SQL DTU/resource constraints

---

## Related Documentation

- **Root Cause Analysis:** `docs/MIGRATION-FAILURE-ANALYSIS.md`
- **EF Core Migrations:** https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/
- **Azure SQL Firewall:** https://learn.microsoft.com/en-us/azure/azure-sql/database/firewall-configure

---

## Next Steps

1. ✅ **Immediate:** Commit and deploy changes to fix production issue
2. ⏳ **Short-term:** Monitor deployment logs for drift detection patterns
3. ⏳ **Long-term:** Consider implementing:
   - Pre-deployment schema comparison tools (Redgate, SSDT)
   - Idempotent migration script generation
   - Pester tests for schema validation

---

**Author:** Infrastructure Team  
**Date:** 2026-04-19  
**Status:** Ready for deployment
