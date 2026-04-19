# Database Migration Failure - Root Cause Analysis

**Date:** 2026-04-19  
**Severity:** CRITICAL  
**Impact:** Production /auth/register endpoint returning 500 errors

---

## Executive Summary

The production API is failing with `SqlException: Invalid object name 'Actors'` despite deployment pipeline showing successful migration execution. Root cause is **schema drift** where EF Core migration history is out of sync with actual database schema.

---

## Root Cause

### What Happened

1. ✅ Deployment pipeline migrate job executed successfully (run 24617441134)
2. ✅ EF Core connected to database and checked `__EFMigrationsHistory` table
3. ❌ EF Core reported: **"No migrations were applied. The database is already up to date."**
4. ❌ `Actors` table does not exist in database despite migration `20260209204020_AddActorEntity` being recorded as applied

### Evidence

**Deployment Log Analysis** (run 24617441134, timestamp 2026-04-19T00:45:06):
```
info: Microsoft.EntityFrameworkCore.Migrations[20405]
      No migrations were applied. The database is already up to date.
```

**Production Error** (Application Insights):
```
SqlException: Invalid object name 'Actors'
  at Register.cs:155
  at dbContext.Actors.SingleOrDefaultAsync(a => a.Email == request.Email)
```

### How This Occurs

**Schema Drift Scenario:**
```
__EFMigrationsHistory Table:
  ✅ 20260209002036_InitialCreate
  ✅ 20260209204020_AddActorEntity  ← Recorded as applied
  ✅ 20260209205322_RemoveAccountStatusDefault
  ... (all 9 migrations present)

Actual Database Schema:
  ❌ Actors table: MISSING
  ✅ Other tables: Present (from earlier migrations)
```

**Common Causes:**
1. Database manually reset without clearing `__EFMigrationsHistory`
2. Terraform `apply` recreated SQL database from partial backup
3. Manual SQL script dropped table but not history entry
4. Previous migration failure left inconsistent state

---

## Impact Assessment

### Affected Systems
- ✅ **API Endpoint:** `POST /auth/register` (500 errors)
- ⚠️ **Potential:** Any feature using Actor entity (login, authentication flows)
- ✅ **Infrastructure:** Database schema integrity compromised

### Business Impact
- **Severity:** CRITICAL - Cannot create new user accounts
- **Users Affected:** All new registrations
- **Workaround:** None available without database fix

---

## Verification Plan

### Step 1: Confirm Migration History
```sql
SELECT MigrationId, ProductVersion 
FROM __EFMigrationsHistory 
ORDER BY MigrationId;
```

**Expected:** All 9 migrations listed including `20260209204020_AddActorEntity`

### Step 2: Verify Table Existence
```sql
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;
```

**Expected Issue:** `Actors` table absent from result

### Step 3: Check Object ID
```sql
SELECT OBJECT_ID('Actors') AS ActorsTableObjectId;
```

**Expected:** NULL (confirms table doesn't exist)

---

## Remediation Strategy

### Option A: Idempotent Migration Reset (RECOMMENDED)

**Approach:** Remove problematic migration record, re-apply migration

**Advantages:**
- ✅ Preserves existing data
- ✅ Works through GitHub Actions (no firewall issues)
- ✅ Auditable via deployment logs
- ✅ Reproducible

**Steps:**

1. **Create SQL remediation script** (`infrastructure/scripts/fix-actors-table.sql`):
```sql
-- Step 1: Verify current state
IF EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20260209204020_AddActorEntity')
    PRINT 'Migration record exists'
ELSE
    RAISERROR('Migration record missing - investigate further', 16, 1)

IF OBJECT_ID('Actors', 'U') IS NULL
    PRINT 'Actors table missing (expected drift)'
ELSE
    RAISERROR('Actors table exists - no drift detected', 16, 1)

-- Step 2: Remove migration history entry for AddActorEntity
DELETE FROM __EFMigrationsHistory 
WHERE MigrationId = '20260209204020_AddActorEntity';

PRINT 'Migration record removed - ready for re-application'
```

2. **Add workflow step to deploy.yml** (before `dotnet ef database update`):
```yaml
- name: Fix schema drift if detected
  working-directory: infrastructure/environments/dev/data
  run: |
    set -euo pipefail
    CONN=$(terraform output -json | jq -re '.connection_string.value')
    echo "::add-mask::$CONN"
    
    # Check if Actors table exists
    dotnet ef dbcontext script --project "$GITHUB_WORKSPACE/src/Innoventity.API/Innoventity.API.csproj" \
      --connection "$CONN" | grep -q "CREATE TABLE.*Actors" || {
      echo "Schema drift detected - running remediation"
      # Execute remediation SQL
      # (Use sqlcmd or Azure SQL CLI here)
    }
```

3. **Re-run deployment pipeline** - migrations will now apply correctly

### Option B: Manual Database Repair (FALLBACK)

**Use when:** Option A fails or requires immediate fix

**Steps:**
1. Add temporary firewall rule for operator IP
2. Run remediation SQL via Azure Data Studio/SSMS
3. Manually trigger `dotnet ef database update`
4. Remove firewall rule

**Disadvantages:**
- ❌ Not reproducible
- ❌ Requires manual access
- ❌ No audit trail in deployment logs

---

## Prevention Mechanisms

### 1. Migration Health Check Job

Add to `deploy.yml` **after** migrate job:

```yaml
verify-migrations:
  name: Verify database schema matches migrations
  needs: migrate
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v6
    - name: Check schema integrity
      run: |
        # Generate expected schema from migrations
        dotnet ef migrations script --idempotent --output expected-schema.sql
        
        # Query actual database schema
        # Compare against expected schema
        # Fail if discrepancies found
```

### 2. Idempotent Migration Scripts

**Current issue:** `dotnet ef database update` trusts `__EFMigrationsHistory` blindly

**Solution:** Generate and apply idempotent SQL scripts:

```yaml
- name: Apply migrations (idempotent)
  run: |
    dotnet ef migrations script --idempotent --output migrations.sql \
      --project src/Innoventity.API/Innoventity.API.csproj
    
    # Apply script via sqlcmd (creates objects only if missing)
    sqlcmd -S $SERVER -d $DB -U $USER -P $PASS -i migrations.sql
```

### 3. Pre-deployment Schema Validation

Add Pester test in `infrastructure/tests/pester/`:

```powershell
Describe "Database Schema Integrity" {
    It "Should have all tables referenced in code" {
        $tables = Invoke-Sqlcmd -Query "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES"
        $tables.TABLE_NAME | Should -Contain 'Actors'
    }
    
    It "Should have migration history matching codebase" {
        $migrations = Get-ChildItem "src/Innoventity.API/Infrastructure/Persistence/Migrations/*.cs"
        # Verify each migration in __EFMigrationsHistory corresponds to actual schema
    }
}
```

### 4. Deployment Pipeline Improvements

```yaml
migrate:
  steps:
    # BEFORE applying migrations
    - name: Backup migration history
      run: |
        sqlcmd -Q "SELECT * INTO __EFMigrationsHistory_Backup FROM __EFMigrationsHistory"
    
    # AFTER applying migrations
    - name: Validate schema consistency
      run: |
        # Check for drift
        # Rollback if validation fails
```

---

## Decision Matrix

| Approach | Time to Fix | Risk | Reproducibility | Audit Trail |
|----------|-------------|------|-----------------|-------------|
| **Option A: Automated Reset** | 15-30 min | Low | High | ✅ Full |
| **Option B: Manual Fix** | 5-10 min | Medium | None | ❌ Manual |
| **Option C: Full DB Reset** | 1-2 hours | High | High | ⚠️ Data loss |

**Recommendation:** **Option A** - Implement automated remediation in deployment pipeline

---

## Action Items

### Immediate (Today)
- [ ] Implement Option A remediation script
- [ ] Test remediation in isolated environment (if available)
- [ ] Execute remediation via GitHub Actions workflow
- [ ] Verify `/auth/register` endpoint functionality

### Short-term (This Week)
- [ ] Add migration health check job to deploy.yml
- [ ] Implement idempotent migration script generation
- [ ] Create Pester test for schema validation
- [ ] Document migration troubleshooting procedures

### Long-term (This Sprint)
- [ ] Evaluate schema comparison tools (SQL Server Data Tools, Redgate)
- [ ] Implement pre-deployment schema validation gate
- [ ] Add Application Insights alert for SQL object_not_found errors
- [ ] Review Terraform state management practices

---

## Lessons Learned

1. **Trust but Verify:** `__EFMigrationsHistory` is metadata, not source of truth
2. **Idempotency Matters:** Migrations should be safe to re-run
3. **Validate After Deploy:** Schema checks should be part of deployment verification
4. **Firewall-Free Operations:** CI/CD should never require ad-hoc firewall rules

---

## References

- Migration failure logs: [GitHub Actions run 24617441134](https://github.com/khaledm/innoventity-prototype-redux/actions/runs/24617441134)
- EF Core Migrations: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/
- Idempotent Scripts: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying#idempotent-sql-scripts

---

**Status:** Draft - Awaiting approval for Option A implementation  
**Next Review:** After remediation execution
