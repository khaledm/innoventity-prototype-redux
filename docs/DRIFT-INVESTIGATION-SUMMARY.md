# Schema Drift Investigation Summary

**Date:** 2026-04-19
**Issue:** Production /auth/register endpoint returning 500 errors
**Deployment Run:** [24637286789](https://github.com/khaledm/innoventity-prototype-redux/actions/runs/24637286789) (TESTING FIX)

---

## Investigation Results

### Root Cause Identified ✅

**Schema Drift:** Database migration history out of sync with actual schema

**Evidence from Deployment Run 24617441134:**
```
info: Microsoft.EntityFrameworkCore.Migrations[20405]
      No migrations were applied. The database is already up to date.
```

**The Problem:**
1. `__EFMigrationsHistory` table contains migration `20260209204020_AddActorEntity`
2. BUT: `Actors` table does NOT exist in database
3. EF Core sees migration as "applied" and skips table creation
4. Application tries to query `Actors` table → SqlException

**How It Happened:**
- Database was manually reset or modified
- OR: Terraform recreated SQL database from partial backup
- Migration history table preserved but actual tables lost
- Pipeline trusted history blindly without validation

---

## Systematic Solution Implemented

### 1. Root Cause Analysis Document ✅
**File:** [docs/MIGRATION-FAILURE-ANALYSIS.md](../../docs/MIGRATION-FAILURE-ANALYSIS.md)

- Comprehensive evidence analysis
- Impact assessment (CRITICAL severity)
- Prevention mechanisms
- Lessons learned

### 2. Automated Drift Detection ✅
**File:** [.github/workflows/deploy.yml](../../.github/workflows/deploy.yml)

**New Step (Before Migrations):**
```yaml
- name: Detect and remediate schema drift
```

**Actions:**
- Queries database for migration record vs table existence
- Automatically detects drift condition
- Executes remediation SQL if needed
- Provides audit trail in deployment logs

### 3. Remediation SQL Script ✅
**File:** [infrastructure/scripts/fix-schema-drift.sql](../../infrastructure/scripts/fix-schema-drift.sql)

**Safe Remediation Process:**
1. Verifies drift condition exists
2. Backs up `__EFMigrationsHistory` to `__EFMigrationsHistory_Backup`
3. Removes orphaned migration record
4. Allows EF Core to re-apply migration

### 4. Post-Migration Validation ✅
**File:** [.github/workflows/deploy.yml](../../.github/workflows/deploy.yml)

**New Step (After Migrations):**
```yaml
- name: Validate schema integrity
```

**Checks:**
- ✅ Actors table exists
- ✅ Innovations table exists
- ✅ Industries table exists
- ✅ Bids table exists
- ✅ Migration count ≥ 9
- ❌ Fails deployment if validation fails

### 5. Local Remediation Tool ✅
**File:** [infrastructure/scripts/remediate-schema-drift.ps1](../../infrastructure/scripts/remediate-schema-drift.ps1)

**Features:**
- Cross-platform PowerShell
- Dry-run mode for testing
- Color-coded diagnostics
- Connection string masking
- Comprehensive logging

---

## Testing the Fix

### Deployment Workflow Triggered ✅

**Run:** [24637286789](https://github.com/khaledm/innoventity-prototype-redux/actions/runs/24637286789)
**Branch:** `002-frontend-cicd`
**Status:** In Progress

### Monitor Progress

```powershell
# Watch workflow execution
gh run watch 24637286789

# Check for drift detection
gh run view 24637286789 --log | Select-String "drift|remediation|validation" -Context 3
```

### Expected Outcomes

**If Drift Exists:**
```
Checking for schema drift...
⚠️ Schema drift detected - applying remediation
✓ Remediation complete - migration will be re-applied
---
Applying migration '20260209204020_AddActorEntity'
---
✓ All critical tables present
✓ Migration history contains 9 migrations
✓ Schema validation passed
```

**If No Drift:**
```
Checking for schema drift...
✓ No schema drift detected - proceeding with normal migration
---
No migrations were applied. The database is already up to date.
---
✓ All critical tables present
✓ Schema validation passed
```

---

## Verification Steps

### 1. Check Deployment Logs (AFTER workflow completes)

```powershell
gh run view 24637286789 --log-failed
```

**Look for:**
- ✅ Drift detection output
- ✅ Migration application success
- ✅ Schema validation passed

### 2. Test API Endpoint

```powershell
curl -X POST https://innoventity-dev-api.azurewebsites.net/auth/register `
  -H "Content-Type: application/json" `
  -d '{
    "email": "test.drift.fix@example.com",
    "password": "Test123!@#",
    "actorType": "IdeaGenerator",
    "organizationName": "Test Org"
  }'
```

**Expected:** HTTP 200/201 (NOT 500)

### 3. Verify Application Insights (AFTER testing endpoint)

Check for NEW exceptions in Application Insights:
- ❌ Should NOT see "Invalid object name 'Actors'" anymore
- ✅ May see validation errors if data is invalid (expected)

---

## Best Practices Followed

### ✅ No Ad-Hoc Fixes
- All remediation automated in pipeline
- No manual database access required
- Reproducible on every deployment

### ✅ Comprehensive Documentation
- Root cause analysis with evidence
- Implementation guide with examples
- Troubleshooting procedures
- Rollback plan

### ✅ Defense in Depth
1. **Detection:** Automated drift checking
2. **Remediation:** Self-healing pipeline
3. **Validation:** Post-migration integrity checks
4. **Audit:** Full logging in deployment runs

### ✅ Prevention Mechanisms
- Schema validation on every deployment
- Early warning system for future drift
- Backup before remediation
- Clear diagnostic output

### ✅ Maintainability
- Well-commented code
- Modular design (separate scripts)
- PowerShell tool for local debugging
- Update guidance in documentation

---

## Next Actions

### Immediate (Today)

1. ✅ **DONE:** Committed schema drift remediation
2. ✅ **DONE:** Pushed to `002-frontend-cicd` branch
3. ✅ **DONE:** Triggered deployment workflow
4. ⏳ **PENDING:** Wait for workflow completion
5. ⏳ **PENDING:** Verify /auth/register endpoint works

### Short-Term (This Week)

- [ ] Monitor deployment logs for drift patterns
- [ ] Clean up old `__EFMigrationsHistory_Backup` tables (if any)
- [ ] Document any issues encountered during first deployment
- [ ] Add Application Insights alert for SQL object_not_found errors

### Long-Term (This Sprint)

- [ ] Evaluate schema comparison tools (Redgate SQL Compare, SSDT)
- [ ] Implement idempotent migration script generation
- [ ] Add Pester tests for database schema validation
- [ ] Review Terraform state management practices

---

## Rollback Plan

If deployment fails or causes issues:

1. **Revert workflow changes:**
   ```powershell
   git revert 6d33201
   git push origin 002-frontend-cicd
   ```

2. **Manual database fix (if needed):**
   - Add temporary firewall rule
   - Run `remediate-schema-drift.ps1` locally
   - Apply migrations manually

3. **Restore migration history backup:**
   ```sql
   DELETE FROM __EFMigrationsHistory;
   INSERT INTO __EFMigrationsHistory
   SELECT * FROM __EFMigrationsHistory_Backup;
   ```

---

## Documentation

- **📄 Root Cause Analysis:** [docs/MIGRATION-FAILURE-ANALYSIS.md](../../docs/MIGRATION-FAILURE-ANALYSIS.md)
- **📘 Implementation Guide:** [docs/SCHEMA-DRIFT-IMPLEMENTATION.md](../../docs/SCHEMA-DRIFT-IMPLEMENTATION.md)
- **🔧 Remediation SQL:** [infrastructure/scripts/fix-schema-drift.sql](../../infrastructure/scripts/fix-schema-drift.sql)
- **💻 PowerShell Tool:** [infrastructure/scripts/remediate-schema-drift.ps1](../../infrastructure/scripts/remediate-schema-drift.ps1)

---

## Key Takeaways

1. **Trust but Verify:** `__EFMigrationsHistory` is metadata, not source of truth
2. **Automate Everything:** Manual fixes are not reproducible
3. **Validate After Changes:** Post-deployment checks catch issues early
4. **Document Thoroughly:** Future you will thank present you
5. **Defense in Depth:** Multiple layers of protection prevent recurrence

---

**Status:** ✅ Solution Implemented - Testing in Progress
**Commit:** 6d33201
**Author:** Infrastructure Team
**Review Date:** 2026-04-19
