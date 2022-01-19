namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.PbsPro
{
    public static class PbsProNodeUsageAttributes
    {
        public const string RESOURCES_AVAILABLE_NCPUS = "resources_available.ncpus";
        public const string RESOURCES_AVAILABLE_NODES = "resources_available.nodes";
        public const string RESOURCES_ASSIGNED_MPIPROCS = "resources_assigned.mpiprocs";
        public const string RESOURCES_ASSIGNED_NCPUS = "resources_assigned.ncpus";
        public const string RESOURCES_ASSIGNED_NODECT = "resources_assigned.nodect";

        public const string QUEUE_TYPE_PRIORITY = "Priority";
        public const string QUEUE_TYPE_TOTAL_JOBS = "total_jobs";
        public const string QUEUE_TYPE_STATE_COUNT = "state_count";
    }
}