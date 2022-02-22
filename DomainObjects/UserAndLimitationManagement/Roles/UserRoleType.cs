namespace HEAppE.ServiceTier.UserAndLimitationManagement.Roles
{
    /// <summary>
    /// User role enum.
    /// </summary>
    public enum UserRoleType : long
    {
        /// <summary>
        /// HEAppE administrator role with access to the entire system.
        /// </summary>
        Administrator = 1,
        
        /// <summary>
        /// HEAppE maintainer role for getting information about actual HEAppE status.
        /// </summary>
        Maintainer = 2,
        
        /// <summary>
        /// Standard user, can submit and check his own jobs.
        /// </summary>
        Submitter = 3,

        /// <summary>
        /// Users with this role can watch other jobs in the same group.
        /// </summary>
        Reporter = 4
    }
}
