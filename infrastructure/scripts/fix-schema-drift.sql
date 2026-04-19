-- ====================================================================
-- Database Schema Drift Remediation
-- ====================================================================
-- Purpose: Fix inconsistency between __EFMigrationsHistory and actual schema
-- Issue: Migration 20260209204020_AddActorEntity recorded but Actors table missing
-- Approach: Remove orphaned migration record, allow EF Core to re-apply
-- ====================================================================

SET NOCOUNT ON;
GO

DECLARE @MigrationId NVARCHAR(150) = '20260209204020_AddActorEntity';
DECLARE @TableName NVARCHAR(128) = 'Actors';
DECLARE @DriftDetected BIT = 0;

PRINT '=================================================================';
PRINT 'Schema Drift Detection and Remediation';
PRINT '=================================================================';
PRINT '';

-- Step 1: Check if migration is recorded
PRINT '1. Checking migration history...';
IF EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = @MigrationId)
BEGIN
    PRINT '   ✓ Migration "' + @MigrationId + '" found in history';
END
ELSE
BEGIN
    PRINT '   ✗ Migration "' + @MigrationId + '" NOT in history';
    PRINT '   ERROR: Expected migration record missing - manual investigation required';
    THROW 50001, 'Migration record not found', 1;
END

PRINT '';

-- Step 2: Check if table exists
PRINT '2. Checking table existence...';
IF OBJECT_ID(@TableName, 'U') IS NULL
BEGIN
    PRINT '   ✗ Table "' + @TableName + '" does NOT exist';
    PRINT '   DRIFT DETECTED: Migration recorded but table missing';
    SET @DriftDetected = 1;
END
ELSE
BEGIN
    PRINT '   ✓ Table "' + @TableName + '" exists';
    PRINT '   No drift detected - schema is consistent';
END

PRINT '';

-- Step 3: Remediate if drift detected
IF @DriftDetected = 1
BEGIN
    PRINT '3. Remediating schema drift...';
    
    -- Backup current migration history
    PRINT '   Creating backup of migration history...';
    IF OBJECT_ID('__EFMigrationsHistory_Backup_' + FORMAT(GETDATE(), 'yyyyMMddHHmmss'), 'U') IS NOT NULL
    BEGIN
        DROP TABLE __EFMigrationsHistory_Backup;
    END
    
    SELECT * 
    INTO __EFMigrationsHistory_Backup
    FROM __EFMigrationsHistory;
    
    PRINT '   ✓ Backup created: __EFMigrationsHistory_Backup';
    
    -- Remove orphaned migration record
    PRINT '   Removing orphaned migration record...';
    DELETE FROM __EFMigrationsHistory 
    WHERE MigrationId = @MigrationId;
    
    DECLARE @RowsDeleted INT = @@ROWCOUNT;
    PRINT '   ✓ Removed ' + CAST(@RowsDeleted AS NVARCHAR(10)) + ' migration record(s)';
    
    PRINT '';
    PRINT '=================================================================';
    PRINT 'REMEDIATION SUCCESSFUL';
    PRINT '=================================================================';
    PRINT 'Next steps:';
    PRINT '  1. Run: dotnet ef database update';
    PRINT '  2. Verify Actors table creation';
    PRINT '  3. Test /auth/register endpoint';
    PRINT '=================================================================';
END
ELSE
BEGIN
    PRINT '3. No remediation needed';
    PRINT '';
    PRINT '=================================================================';
    PRINT 'SCHEMA VALIDATION PASSED';
    PRINT '=================================================================';
    PRINT 'Database schema is consistent with migration history.';
    PRINT '=================================================================';
END

GO
