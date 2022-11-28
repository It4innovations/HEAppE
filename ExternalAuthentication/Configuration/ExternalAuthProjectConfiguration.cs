namespace HEAppE.ExternalAuthentication.Configuration
{
    public sealed class ExternalAuthProjectConfiguration
    {
        #region Properties
        /// <summary>
        /// UUID
        /// </summary>
        public string UUID { get; set; }

        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Group name in HEAppE DB
        /// </summary>
        public string HEAppEGroupName { get; set; }
        #endregion
    }
}
