-- ====================================================================
-- Database Schema Drift Remediation
-- ====================================================================
-- Purpose: Fix inconsistency between __EFMigrationsHistory and actual schema
-- Issue: Migrations recorded but critical tables missing (complete schema wipeout)
-- Approach: Reset migration history, allow EF Core to re-apply all migrations
-- ====================================================================

SET NOCOUNT ON;
GO

DECLARE @DriftDetected BIT = 0;
DECLARE @MissingTables NVARCHAR(500) = '';

PRINT '=================================================================';
PRINT 'Schema Drift Detection and Remediation';
PRINT '=================================================================';
PRINT '';

-- Step 1: Check migration history count
PRINT '1. Checking migration history...';
DECLARE @MigrationCount INT;
SELECT @MigrationCount = COUNT(*) FROM __EFMigrationsHistory;
PRINT '   Migration history contains ' + CAST(@MigrationCount AS NVARCHAR) + ' migrations';
PRINT '';

-- Step 2: Check if critical tables exist
PRINT '2. Checking critical table existence...';

IF OBJECT_ID('Actors', 'U') IS NULL
BEGIN
    SET @MissingTables = @MissingTables + 'Actors, ';
    PRINT '   ✗ Table "Actors" does NOT exist';
END
ELSE
    PRINT '   ✓ Table "Actors" exists';

IF OBJECT_ID('Innovations', 'U') IS NULL
BEGIN
    SET @MissingTables = @MissingTables + 'Innovations, ';
    PRINT '   ✗ Table "Innovations" does NOT exist';
END
ELSE
    PRINT '   ✓ Table "Innovations" exists';

IF OBJECT_ID('Industries', 'U') IS NULL
BEGIN
    SET @MissingTables = @MissingTables + 'Industries, ';
    PRINT '   ✗ Table "Industries" does NOT exist';
END
ELSE
    PRINT '   ✓ Table "Industries" exists';

IF OBJECT_ID('Bids', 'U') IS NULL
BEGIN
    SET @MissingTables = @MissingTables + 'Bids, ';
    PRINT '   ✗ Table "Bids" does NOT exist';
END
ELSE
    PRINT '   ✓ Table "Bids" exists';

PRINT '';

-- Step 3: Determine drift status
IF LEN(@MissingTables) > 0
BEGIN
    SET @DriftDetected = 1;
    PRINT '   ⚠️  DRIFT DETECTED: Missing tables = ' + LEFT(@MissingTables, LEN(@MissingTables) - 1);
    PRINT '';
END
ELSE
BEGIN
    PRINT '   ✓ No drift detected - all critical tables exist';
    PRINT '';
END

PRINT '';

-- Step 4: Remediate if drift detected
IF @DriftDetected = 1
BEGIN
    PRINT '3. Remediating complete schema drift...';
    
    -- Backup current migration history with timestamp
    DECLARE @BackupTableName NVARCHAR(200) = '__EFMigrationsHistory_Backup_' + FORMAT(GETDATE(), 'yyyyMMddHHmmss');
    PRINT '   Creating backup: ' + @BackupTableName;
    
    DECLARE @BackupSQL NVARCHAR(MAX) = 
        'SELECT * INTO [' + @BackupTableName + '] FROM __EFMigrationsHistory';
    EXEC sp_executesql @BackupSQL;
    
    PRINT '   ✓ Backup created successfully';
    
    -- Truncate migration history to force complete re-application
    PRINT '   Truncating migration history to allow full re-application...';
    TRUNCATE TABLE __EFMigrationsHistory;
    
    PRINT '   ✓ Migration history reset - EF Core will now re-apply all migrations';
    PRINT '';
    PRINT '   ACTION REQUIRED: Run "dotnet ef database update" to re-create all tables';
    PRINT '';
    PRINT '   Recovery: If needed, restore from backup table: ' + @BackupTableName;
    PRINT '';
    PRINT '=================================================================';
    PRINT 'REMEDIATION COMPLETE';
    PRINT '=================================================================';
END
ELSE
BEGIN
    PRINT '3. No remediation needed';
    PRINT '';
    PRINT '=================================================================';
    PRINT 'Schema is consistent - no action required';
    PRINT '=================================================================';
END

GO
