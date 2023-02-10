namespace HEAppE.OpenStackAPI.DTO
{
    public class OpenStackProjectDTO
    {
        #region Properties
        public string Name { get; set; }

        public string UID { get; set; }

        public long? HEAppEProjectId { get; set; }

        public OpenStackDomainDTO Domain { get; set; }

        public OpenStackCredentialsDTO Credentials { get; set; }
        #endregion
    }
}