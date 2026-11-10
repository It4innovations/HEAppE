-- =====================================================================================
-- Step 2 (run against the QScheduler database, PostgreSQL) — QScheduler MUST BE STOPPED.
--
-- Renames this HEAppE instance's QScheduler projects from "<AccountingString>" to
-- "<prefix>.<AccountingString>". Tasks and sessions reference projects by id, so all history and
-- consumed_ms stay with the renamed project.
--
-- Why QScheduler must be stopped: it caches projects in memory and looks them up by name.
-- On start it reloads projects, sessions and tasks from the database.
--
-- Run with:  psql -v ON_ERROR_STOP=1 -f 02_qscheduler_rename_projects.sql
-- The script stops at the first failed check and changes nothing.
-- =====================================================================================

BEGIN;

CREATE TEMP TABLE rename_map (
    old_name TEXT PRIMARY KEY,
    new_name TEXT NOT NULL UNIQUE
) ON COMMIT DROP;

-- Paste the RenameMapRow values from 01_heappe_generate_rename_map.sql (rows of ONE cluster only).
-- Remove the trailing comma from the last row.
INSERT INTO rename_map (old_name, new_name) VALUES
  ('OPEN-00-00', 'my-heappe-instance.OPEN-00-00')
;

-- -------------------------------------------------------------------------------------
-- Report: what will be renamed (review the output).
-- -------------------------------------------------------------------------------------
SELECT m.old_name, m.new_name, p.id, p.consumed_ms, p.limit_ms, p.active
FROM rename_map m
LEFT JOIN projects p ON p.name = m.old_name
ORDER BY m.old_name;

-- -------------------------------------------------------------------------------------
-- Report: projects used by more than one notifier, i.e. probably shared with another
-- HEAppE instance or another client. Only one of them can keep the history; the others
-- will start from zero under their own prefix. Decide before committing.
-- Work submitted without a notifier (callbacks off, or notifier removed) cannot be
-- attributed and shows as NULL.
-- -------------------------------------------------------------------------------------
SELECT p.name,
       array_agg(DISTINCT COALESCE(n.name, '<no notifier>')) AS notifiers
FROM projects p
JOIN rename_map m ON m.old_name = p.name
LEFT JOIN (
    SELECT project_id, notifier_id FROM tasks WHERE project_id IS NOT NULL
    UNION
    SELECT project_id, notifier_id FROM sessions
) usage ON usage.project_id = p.id
LEFT JOIN notifiers n ON n.id = usage.notifier_id
GROUP BY p.name
HAVING count(DISTINCT usage.notifier_id) > 1;

-- -------------------------------------------------------------------------------------
-- Safety checks. Any failure aborts the transaction.
-- -------------------------------------------------------------------------------------
DO $$
DECLARE
    missing TEXT;
    taken   TEXT;
    too_long TEXT;
BEGIN
    SELECT string_agg(m.old_name, ', ') INTO missing
    FROM rename_map m
    WHERE NOT EXISTS (SELECT 1 FROM projects p WHERE p.name = m.old_name);
    IF missing IS NOT NULL THEN
        RAISE NOTICE 'Not in QScheduler (nothing to rename, HEAppE will create them): %', missing;
    END IF;

    SELECT string_agg(m.new_name, ', ') INTO taken
    FROM rename_map m
    JOIN projects p ON p.name = m.new_name;
    IF taken IS NOT NULL THEN
        RAISE EXCEPTION 'Target project names already exist: %. Was the new HEAppE already started?', taken;
    END IF;

    SELECT string_agg(m.new_name, ', ') INTO too_long
    FROM rename_map m
    WHERE length(m.new_name) > 250;
    IF too_long IS NOT NULL THEN
        RAISE EXCEPTION 'Target names longer than 250 characters: %', too_long;
    END IF;
END $$;

-- -------------------------------------------------------------------------------------
-- Rename.
-- -------------------------------------------------------------------------------------
UPDATE projects p
SET name = m.new_name
FROM rename_map m
WHERE p.name = m.old_name;

SELECT p.id, p.name, p.consumed_ms, p.limit_ms, p.active
FROM projects p
JOIN rename_map m ON m.new_name = p.name
ORDER BY p.name;

-- Review the output above. To apply, keep COMMIT; to try it out first, change it to ROLLBACK.
COMMIT;
