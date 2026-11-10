-- =====================================================================================
-- Step 1 (run against the HEAppE database, SQL Server)
--
-- Lists the QScheduler projects of THIS HEAppE instance and the prefixed names they get.
-- Copy the generated INSERT lines into 02_qscheduler_rename_projects.sql.
--
-- The prefix is resolved the same way as in HEAppE (QSchedulerSchedulerAdapter.ResolveProjectPrefix):
--   cluster CustomConfiguration 'QSchedulerProjectPrefix'
--   -> cluster CustomConfiguration 'QSchedulerNotifier'
--   -> InstanceIdentifierPath from appsettings (not in the DB: set @InstanceIdentifierPath below).
-- =====================================================================================

DECLARE @InstanceIdentifierPath NVARCHAR(256) = N'';   -- HPCConnectionFrameworkSettings ... InstanceIdentifierPath
DECLARE @Separator NCHAR(1) = N'.';

WITH QSchedulerProjects AS (
    SELECT
        c.Id   AS ClusterId,
        c.Name AS ClusterName,
        p.Id   AS ProjectId,
        LTRIM(RTRIM(p.AccountingString)) AS AccountingString,
        LTRIM(RTRIM(COALESCE(
            NULLIF(JSON_VALUE(c.CustomConfiguration, '$.QSchedulerProjectPrefix'), N''),
            NULLIF(JSON_VALUE(c.CustomConfiguration, '$.QSchedulerNotifier'), N''),
            NULLIF(@InstanceIdentifierPath, N'')))) AS Prefix
    FROM Project p
    JOIN ClusterProject cp ON cp.ProjectId = p.Id AND cp.IsDeleted = 0
    JOIN Cluster c ON c.Id = cp.ClusterId AND c.IsDeleted = 0
    WHERE c.SchedulerType = 32      -- QScheduler
      AND p.UsageType = 4           -- QPUSeconds (only these projects are registered in QScheduler)
)
SELECT
    ClusterId,
    ClusterName,
    ProjectId,
    AccountingString AS OldName,
    Prefix + @Separator + AccountingString AS NewName,
    CASE
        WHEN Prefix IS NULL THEN N'ERROR: no prefix configured'
        WHEN CHARINDEX(@Separator, Prefix) > 0 THEN N'ERROR: prefix contains the separator'
        WHEN LEN(Prefix + @Separator + AccountingString) > 250 THEN N'ERROR: name longer than 250 characters'
        ELSE N'OK'
    END AS Status,
    N'  (''' + REPLACE(AccountingString, N'''', N'''''') + N''', '''
        + REPLACE(Prefix + @Separator + AccountingString, N'''', N'''''') + N'''),' AS RenameMapRow
FROM QSchedulerProjects
ORDER BY ClusterId, AccountingString;

-- One HEAppE cluster = one QScheduler instance. Run the rename script against each QScheduler
-- instance with only the rows of its cluster.
