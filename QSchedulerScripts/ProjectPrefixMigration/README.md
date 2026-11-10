# QScheduler project prefix migration

HEAppE now registers its projects in QScheduler as `<prefix>.<AccountingString>`, so several HEAppE
instances can share one QScheduler without overwriting each other's limits. HEAppE itself keeps the
plain accounting string; the prefix exists only in QScheduler.

The prefix comes from the cluster's `CustomConfiguration`:

1. `QSchedulerProjectPrefix`, if set (recommended: set it explicitly),
2. otherwise `QSchedulerNotifier`,
3. otherwise `InstanceIdentifierPath` from appsettings.

The prefix must not contain `.` and must never change afterwards. A new prefix points HEAppE to new,
empty QScheduler projects.

Existing QScheduler projects still have the unprefixed names. These scripts rename them once, so
their consumption history stays attached. This is a temporary manual procedure until the migration is
available in the HEAppE management admin panel.

## Order of steps

The order matters. If the new HEAppE runs before the rename, it creates empty prefixed projects
and the rename then fails on the name clash.

1. Stop HEAppE QScheduler submissions (maintenance window).
2. Run `01_heappe_generate_rename_map.sql` against the HEAppE database. Check that every row has
   status `OK`.
3. **Stop QScheduler.** It caches projects in memory by name.
4. Paste one cluster's `RenameMapRow` values into `02_qscheduler_rename_projects.sql` and run it
   against that cluster's QScheduler database:
   `psql -v ON_ERROR_STOP=1 -f 02_qscheduler_rename_projects.sql`.
   Review the "shared projects" output. A project used by several notifiers was shared between
   instances, and only one of them can keep its history.
5. Start QScheduler.
6. Deploy the new HEAppE (with `QSchedulerProjectPrefix` set) and restart it.

To do a dry run, change the final `COMMIT;` to `ROLLBACK;`.

> [!WARNING]
> After the rename, HEAppE sets each project's QScheduler limit to the sum of **all** its allocations,
> including non-quantum ones. Check that QScheduler projects carry only QPU allocations. See
> [the QScheduler accounting notes](../../HpcConnectionFramework/SchedulerAdapters/QScheduler/README.md).
